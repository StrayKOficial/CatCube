using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;
using BepuPhysics.Trees;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace CatCube.Engine.Physics;

public class PhysicsSpace : IDisposable
{
    public Simulation Simulation { get; private set; }
    public BufferPool BufferPool { get; private set; }
    public SimpleThreadDispatcher ThreadDispatcher { get; private set; }
    
    // Reverse lookup for events
    public Dictionary<BodyHandle, object> BodyToPart = new Dictionary<BodyHandle, object>();
    public Dictionary<StaticHandle, object> StaticToPart = new Dictionary<StaticHandle, object>();
    
    // Contact Queue
    public ConcurrentQueue<(BodyHandle? bodyA, StaticHandle? staticA, BodyHandle? bodyB, StaticHandle? staticB)> ContactEvents = new ConcurrentQueue<(BodyHandle?, StaticHandle?, BodyHandle?, StaticHandle?)>();

    public PhysicsSpace()
    {
        BufferPool = new BufferPool();
        ThreadDispatcher = new SimpleThreadDispatcher(Environment.ProcessorCount);
        
        // Pass 'this' to callbacks to access queues/maps
        var callbacks = new NarrowPhaseCallbacks { Space = this };
        Simulation = Simulation.Create(BufferPool, callbacks, new PoseIntegratorCallbacks(new Vector3(0, -30, 0)), new SolveDescription(8, 1));
    }
    
    public void RegisterBody(BodyHandle handle, object part) => BodyToPart[handle] = part;
    public void RegisterStatic(StaticHandle handle, object part) => StaticToPart[handle] = part;
    public void UnregisterBody(BodyHandle handle) => BodyToPart.Remove(handle);
    public void UnregisterStatic(StaticHandle handle) => StaticToPart.Remove(handle);

    public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out RayHit lastHit)
    {
        var handler = new SingleRayHitHandler();
        Simulation.RayCast(origin, direction, maxDistance, ref handler);
        lastHit = handler.HitResult;
        return handler.Hit;
    }

    public void Update(float dt)
    {
        // Run single-threaded for stability debug
        Simulation.Timestep(dt, null); 
    }

    public void Dispose()
    {
        Simulation.Dispose();
        BufferPool.Clear();
        ThreadDispatcher.Dispose();
    }
}

public struct RayHit
{
    public CollidableReference Collidable;
    public float T;
    public Vector3 Normal;
}

struct SingleRayHitHandler : IRayHitHandler
{
    public bool Hit;
    public RayHit HitResult;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AllowTest(CollidableReference collidable) => true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AllowTest(CollidableReference collidable, int childIndex) => true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnRayHit(in RayData ray, ref float maximumT, float t, in Vector3 normal, CollidableReference collidable, int childIndex)
    {
        Hit = true;
        maximumT = t;
        HitResult = new RayHit { Collidable = collidable, T = t, Normal = normal };
    }
}

// Updated Callbacks with Vector<T> support for BepuPhysics 2.4+
unsafe struct NarrowPhaseCallbacks : INarrowPhaseCallbacks
{
    public PhysicsSpace Space;

    public void Initialize(Simulation simulation) { }
    public void Dispose() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin)
    {
        // Check CanCollide if we have pointers to parts
        if (Space != null)
        {
            object? partA = null;
            object? partB = null;
            
            if (a.Mobility == CollidableMobility.Dynamic) Space.BodyToPart.TryGetValue(a.BodyHandle, out partA);
            else Space.StaticToPart.TryGetValue(a.StaticHandle, out partA);
            
            if (b.Mobility == CollidableMobility.Dynamic) Space.BodyToPart.TryGetValue(b.BodyHandle, out partB);
            else Space.StaticToPart.TryGetValue(b.StaticHandle, out partB);
            
            if (partA is Part pA && !pA.CanCollide && pA.Physical) return false;
            if (partB is Part pB && !pB.CanCollide && pB.Physical) return false;
        }

        return a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
    {
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial) where TManifold : unmanaged, IContactManifold<TManifold>
    {
        // Default properties
        float friction = 1f;
        float elasticity = 0f;
        float springFrequency = 30f;
        float springDamping = 1f;

        if (Space != null)
        {
            object? objA = null;
            object? objB = null;
            
            // Resolve A
            if (pair.A.Mobility == CollidableMobility.Dynamic) Space.BodyToPart.TryGetValue(pair.A.BodyHandle, out objA);
            else Space.StaticToPart.TryGetValue(pair.A.StaticHandle, out objA);
            
            // Resolve B
            if (pair.B.Mobility == CollidableMobility.Dynamic) Space.BodyToPart.TryGetValue(pair.B.BodyHandle, out objB);
            else Space.StaticToPart.TryGetValue(pair.B.StaticHandle, out objB);
            
            // Combine properties (Multiply Friction, Max Elasticity usually)
            if (objA is Part pA) { friction *= pA.Friction; elasticity = Math.Max(elasticity, pA.Elasticity); }
            if (objB is Part pB) { friction *= pB.Friction; elasticity = Math.Max(elasticity, pB.Elasticity); }
        }

        pairMaterial.FrictionCoefficient = friction;
        pairMaterial.MaximumRecoveryVelocity = elasticity > 0 ? float.MaxValue : 2f; // Allow bounce if elasticity > 0
        pairMaterial.SpringSettings = new SpringSettings(springFrequency, springDamping);
        
        // Dispatch Event if contact exists (Queue logic...)
        if (manifold.Count > 0 && Space != null)
        {
            BodyHandle? bA = pair.A.Mobility == CollidableMobility.Dynamic ? pair.A.BodyHandle : null;
            StaticHandle? sA = pair.A.Mobility == CollidableMobility.Static ? pair.A.StaticHandle : null;
            
            BodyHandle? bB = pair.B.Mobility == CollidableMobility.Dynamic ? pair.B.BodyHandle : null;
            StaticHandle? sB = pair.B.Mobility == CollidableMobility.Static ? pair.B.StaticHandle : null;
            
            Space.ContactEvents.Enqueue((bA, sA, bB, sB));
        }
        
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold)
    {
        return true;
    }
}

struct PoseIntegratorCallbacks : IPoseIntegratorCallbacks
{
    public Vector3 Gravity;
    public Vector3Wide GravityWideDt;
    public float LinearDamping;
    public float AngularDamping;

    public PoseIntegratorCallbacks(Vector3 gravity, float linearDamping = .03f, float angularDamping = .03f)
    {
        Gravity = gravity;
        LinearDamping = linearDamping;
        AngularDamping = angularDamping;
        GravityWideDt = default;
    }

    public void Initialize(Simulation simulation) { }

    public void PrepareForIntegration(float dt)
    {
        // No implicit conversion from Vector3 to Vector3Wide in generic context sometimes, but here we construct it
        GravityWideDt = Vector3Wide.Broadcast(Gravity * dt);
    }

    public void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation, BodyInertiaWide localInertia, Vector<int> integrationMask, int workerIndex, Vector<float> dt, ref BodyVelocityWide velocity)
    {
        // Velocity += Gravity * dt
        velocity.Linear += GravityWideDt; 
    }

    public AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;
    public bool AllowSubstepsForUnconstrainedBodies => false;
    public bool IntegrateVelocityForKinematics => false;
}
