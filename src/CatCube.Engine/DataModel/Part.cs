using System.Numerics;
using MoonSharp.Interpreter;
using BepuPhysics;
using BepuPhysics.Collidables;
using CatCube.Engine.Physics;
using System;
using CatCube.Engine; // For Part

namespace CatCube.Engine;

[MoonSharpUserData]
public class Part : Instance
{
    private Vector3 _position;
    private Vector3 _size = new Vector3(4, 1, 2);
    private bool _anchored = true;
    
    // Physics
    private BodyHandle? _bodyHandle;
    private StaticHandle? _staticHandle;
    private TypedIndex _shapeIndex;
    
    public Vector3 Position 
    { 
        get => _position; 
        set 
        { 
            _position = value; 
            SyncToPhysics(); 
        } 
    }
    
    public Vector3 Size 
    { 
        get => _size; 
        set 
        { 
            _size = value;
            RefreshPhysics();
        } 
    }
    
    public Vector3 Color { get; set; } = new Vector3(0.63f, 0.64f, 0.63f);
    
    public bool Anchored 
    { 
        get => _anchored; 
        set 
        { 
            if (_anchored == value) return;
            _anchored = value;
            RefreshPhysics();
        } 
    }
    
    private bool _canCollide = true;
    public bool CanCollide 
    { 
        get => _canCollide;
        set 
        {
            _canCollide = value;
            // No need to refresh body, NarrowPhaseCallbacks handles this check in real-time
        }
    }
    public float Transparency { get; set; } = 0.0f; // 0 = Opaque, 1 = Invisible
    public bool Physical { get; set; } = true; // If false, no automatic physics body creation
    public Vector3 Rotation { get; set; } // Simplified for now (Euler)
    
    // Physics Properties
    public float Friction { get; set; } = 1.0f;
    public float Elasticity { get; set; } = 0.0f; // 0 = No bounce, 1 = Full bounce

    public Part()
    {
        Name = "Part";
    }

    protected override void OnParentChanged()
    {
        base.OnParentChanged();
        
        var workspace = DataModel.Current.Workspace;
        if (IsDescendantOf(workspace) && Physical)
        {
            CreatePhysicsBody(workspace.Physics);
        }
        else if (!IsDescendantOf(workspace))
        {
            DestroyPhysicsBody(workspace.Physics);
        }
    }

    // Event for Lua
    public event Action<Part> Touched;

    public void RefreshPhysics()
    {
        var workspace = DataModel.Current.Workspace;
        if (IsDescendantOf(workspace) && Physical)
        {
            DestroyPhysicsBody(workspace.Physics);
            CreatePhysicsBody(workspace.Physics);
        }
    }

    private void CreatePhysicsBody(PhysicsSpace physics)
    {
        if (_bodyHandle.HasValue || _staticHandle.HasValue) return;

        var box = new Box(Size.X, Size.Y, Size.Z);
        _shapeIndex = physics.Simulation.Shapes.Add(box);
        
        var pose = new RigidPose(Position, Quaternion.Identity); // TODO: Rotation support

        if (Anchored)
        {
            _staticHandle = physics.Simulation.Statics.Add(new StaticDescription(pose, _shapeIndex));
            physics.RegisterStatic(_staticHandle.Value, this);
        }
        else
        {
            // Calculate inertia for a 1-unit density box
            float mass = Size.X * Size.Y * Size.Z;
            if (mass < 0.001f) mass = 1f;
            
            var inertia = box.ComputeInertia(mass); 
            
            var bodyDesc = BodyDescription.CreateDynamic(pose, inertia, new CollidableDescription(_shapeIndex, 0.1f), new BodyActivityDescription(0.01f));
            _bodyHandle = physics.Simulation.Bodies.Add(bodyDesc);
            physics.RegisterBody(_bodyHandle.Value, this);
        }
    }

    private void DestroyPhysicsBody(PhysicsSpace physics)
    {
        if (_bodyHandle.HasValue)
        {
            physics.UnregisterBody(_bodyHandle.Value);
            physics.Simulation.Bodies.Remove(_bodyHandle.Value);
            _bodyHandle = null;
        }
        
        if (_staticHandle.HasValue)
        {
            physics.UnregisterStatic(_staticHandle.Value);
            physics.Simulation.Statics.Remove(_staticHandle.Value);
            _staticHandle = null;
        }
    }
    
    // Internal method to trigger event from Workspace
    public void OnTouched(Part other)
    {
        Touched?.Invoke(other);
    }

    private void SyncToPhysics()
    {
        // If we have a body/static, teleport it
        var physics = DataModel.Current.Workspace.Physics;
        
        if (_bodyHandle.HasValue)
        {
            var body = physics.Simulation.Bodies.GetBodyReference(_bodyHandle.Value);
            body.Pose.Position = _position;
            body.Velocity.Linear = Vector3.Zero; // Stop movement (Rez sickness fix)
            body.Awake = true;
        }
        else if (_staticHandle.HasValue)
        {
            var stat = physics.Simulation.Statics.GetStaticReference(_staticHandle.Value);
            stat.Pose.Position = _position;
        }
    }
    
    // Called by Engine Loop to update C# property from Physics Simulation
    public void SyncFromPhysics()
    {
        if (_bodyHandle.HasValue)
        {
            var physics = DataModel.Current.Workspace.Physics;
            var body = physics.Simulation.Bodies.GetBodyReference(_bodyHandle.Value);
            _position = body.Pose.Position;
            // _rotation = ...
        }
    }
    
    // Allow external systems (Game.cs) to attach a real physics body to this Part proxy
    public void AttachPhysicsBody(BodyHandle handle)
    {
        _bodyHandle = handle;
    }
}
