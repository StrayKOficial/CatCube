using Silk.NET.OpenGL;
using System.Numerics;
using CatCube.Rendering;
using CatCube.Input;
using CatCube.Engine.Physics; // Engine.Physics
using CatCube.Shared;
using BepuPhysics;
using BepuPhysics.Collidables;

namespace CatCube.Entities;

public class Player : IDisposable
{
    private readonly GL _gl;
    
    public Vector3 Position { get; private set; }
    public float Rotation { get; private set; }
    
    private Vector3 _velocity = Vector3.Zero;
    private bool _isGrounded = true;
    private float _targetRotation = 0f;
    
    // Animation
    public float WalkCycle { get; private set; } = 0f;
    private float _walkSpeed = 0f;
    private const float WalkCycleSpeed = 12f;
    private const float MaxLimbAngle = 0.7f; // ~40 degrees
    
    // Physics constants
    private const float MoveSpeed = 16f; // Slightly faster run
    private const float Acceleration = 150f; // Instant start
    private const float Deceleration = 120f; // Instant stop (no sliding)
    private const float JumpForce = 18f;
    private const float Gravity = 30f;
    private const float GroundY = 1.0f;
    private const float RotationSpeed = 12f;
    private const float CoyoteTime = 0.12f;
    
    private float _coyoteTimer = 0f;
    
    // Body parts - stored as reference for animation
    private PlayerPart _torso;
    private PlayerPart _head;
    private PlayerPart _leftArm;
    private PlayerPart _rightArm;
    private PlayerPart _leftLeg;
    private PlayerPart _rightLeg;

    // Physics
    private PhysicsSpace _physics;
    public BodyHandle BodyHandle { get; private set; } // Public for Game.cs registration
    private TypedIndex _shapeIndex;

    // Animation States
    private AnimState _animState = AnimState.Idle;
    public AnimState State => _animState;
    private float _animTimer = 0f;
    
    // Smooth transitions
    private float _armAngle = 0f;
    private float _legAngle = 0f;
    private Vector3 _centerOffset = Vector3.Zero;
    
    // State persistence
    private float _jumpLockTimer = 0f;
    private float _fallLockTimer = 0f;
    private bool _wasGroundedLastFrame = true;
    private float _landingEffectTimer = 0f;
    private readonly AvatarData _avatar;

    public Player(GL gl, Vector3 startPosition, PhysicsSpace physics, AvatarData avatar)
    {
        _gl = gl;
        _physics = physics;
        Position = startPosition;
        _avatar = avatar;
        
        // Create Physics Body (Capsule)
        var capsule = new Capsule(0.5f, 1.0f); // Height 2.0
        _shapeIndex = _physics.Simulation.Shapes.Add(capsule);
        
        var inertia = capsule.ComputeInertia(80);
        var pose = new RigidPose(startPosition, Quaternion.Identity);
        inertia.InverseInertiaTensor = new BepuUtilities.Symmetric3x3(); 
        
        BodyHandle = _physics.Simulation.Bodies.Add(BodyDescription.CreateDynamic(pose, inertia, new CollidableDescription(_shapeIndex, 0.1f), new BodyActivityDescription(0.01f)));
        
        // Setup Body Parts based on AvatarData
        UpdateParts(avatar);
    }

    private void UpdateParts(AvatarData avatar)
    {
        Vector3 skinColor = HexToVector3(avatar.SkinColor);
        Vector3 shirtColor = HexToVector3(avatar.ShirtColor);
        Vector3 pantsColor = HexToVector3(avatar.PantsColor);
        
        float widthScale = 1.0f;
        float depthScale = 1.0f;
        
        if (avatar.BodyType == 1) { widthScale = 0.8f; depthScale = 0.8f; }
        else if (avatar.BodyType == 2) { widthScale = 1.2f; depthScale = 1.1f; }

        // Final Calibrated Offsets
        _torso = new PlayerPart("Torso", new Vector3(0, 2.0f, 0), new Vector3(2.0f * widthScale, 2.0f, 1.0f * depthScale), shirtColor, Vector3.Zero);
        _head = new PlayerPart("Head", new Vector3(0, 3.0f, 0), new Vector3(1.0f, 1.0f, 1.0f), skinColor, new Vector3(0, 0.5f, 0));
        
        float armOffset = 1.5f * widthScale;
        _leftArm = new PlayerPart("LeftArm", new Vector3(-armOffset, 3.0f, 0), new Vector3(1.0f * widthScale, 2.0f, 1.0f * depthScale), skinColor, new Vector3(0, -1.0f, 0));
        _rightArm = new PlayerPart("RightArm", new Vector3(armOffset, 3.0f, 0), new Vector3(1.0f * widthScale, 2.0f, 1.0f * depthScale), skinColor, new Vector3(0, -1.0f, 0));
        
        float legOffset = 0.5f * widthScale;
        _leftLeg = new PlayerPart("LeftLeg", new Vector3(-legOffset, 1.0f, 0), new Vector3(1.0f * widthScale, 2.0f, 1.0f * depthScale), pantsColor, new Vector3(0, -1.0f, 0));
        _rightLeg = new PlayerPart("RightLeg", new Vector3(legOffset, 1.0f, 0), new Vector3(1.0f * widthScale, 2.0f, 1.0f * depthScale), pantsColor, new Vector3(0, -1.0f, 0));
    }

    private static Vector3 HexToVector3(string? hex)
    {
        if (string.IsNullOrEmpty(hex)) return new Vector3(1, 1, 1);
        if (hex.StartsWith("#")) hex = hex.Substring(1);
        if (hex.Length != 6) return new Vector3(1, 1, 1);
        
        float r = Convert.ToInt32(hex.Substring(0, 2), 16) / 255f;
        float g = Convert.ToInt32(hex.Substring(2, 2), 16) / 255f;
        float b = Convert.ToInt32(hex.Substring(4, 2), 16) / 255f;
        return new Vector3(r, g, b);
    }

    public void Update(float deltaTime, InputManager input, Camera camera)
    {
        // Calculate movement direction based on camera and input
        Vector3 moveDirection = Vector3.Zero;
        
        if (input.Movement != Vector2.Zero)
        {
            Vector3 forward = camera.GetForwardDirection();
            Vector3 right = camera.GetRightDirection();
            
            // Flatten forward (remove Y) for walking
            forward = Vector3.Normalize(new Vector3(forward.X, 0, forward.Z));
            
            moveDirection = forward * input.Movement.Y + right * input.Movement.X;
            if (moveDirection.LengthSquared() > 0)
                moveDirection = Vector3.Normalize(moveDirection);
            
            // Calculate target rotation to face movement direction
            _targetRotation = MathF.Atan2(moveDirection.X, moveDirection.Z);
        }
        
        // Smooth rotation
        if (moveDirection != Vector3.Zero)
        {
            float angleDiff = _targetRotation - Rotation;
            while (angleDiff > MathF.PI) angleDiff -= 2 * MathF.PI;
            while (angleDiff < -MathF.PI) angleDiff += 2 * MathF.PI;
            Rotation += angleDiff * RotationSpeed * deltaTime;
        }
        
        // Get Body Reference
        var body = _physics.Simulation.Bodies.GetBodyReference(BodyHandle);
        
        // Movement Logic using Physics Velocity
        // We set Linear Velocity X/Z directly for responsiveness (Arcade style)
        Vector3 currentVel = body.Velocity.Linear;
        Vector3 targetVel = moveDirection * MoveSpeed;
        
        // Apply acceleration/deceleration to X/Z only
        float accel = moveDirection != Vector3.Zero ? Acceleration : Deceleration;
        
        float newX = Approach(currentVel.X, targetVel.X, accel * deltaTime);
        float newZ = Approach(currentVel.Z, targetVel.Z, accel * deltaTime);
        
        // Jumping
        // Simple raycast or check vertical velocity approximately 0 could work for now
        // Or check contact constraints (complex). 
        // For simplicity: If Y velocity is close to 0 (and below some height?), assume grounded.
        // Better: Use collision callback. But for now, simple check:
        // Or keep internal timer.
        
        if (Math.Abs(currentVel.Y) < 0.01f)
        {
            _coyoteTimer = CoyoteTime;
            _isGrounded = true;
        }
        else
        {
            _coyoteTimer -= deltaTime;
            _isGrounded = false;
        }
        
        float newY = currentVel.Y;
        
        if (input.Jump && _coyoteTimer > 0)
        {
            newY = JumpForce;
            _coyoteTimer = 0;
            _isGrounded = false;
        }
        
        // Apply to body
        body.Velocity.Linear = new Vector3(newX, newY, newZ);
        body.Awake = true; // Wake up
        
        // Sync Position from Body
        Position = body.Pose.Position;
        
        // Animation
        float horizontalSpeed = new Vector2(newX, newZ).Length();
        _walkSpeed = Approach(_walkSpeed, horizontalSpeed / MoveSpeed, deltaTime * 8f);
        
        if (_walkSpeed > 0.1f && _isGrounded)
        {
            WalkCycle += deltaTime * WalkCycleSpeed * _walkSpeed;
        }
        else
        {
            WalkCycle = Approach(WalkCycle, 0, deltaTime * 8f);
        }
    }

    public void Render(Renderer renderer)
    {
        UpdateAnimationState();
    
        Quaternion bodyRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, Rotation);
        
        // 1. Physical Tilt Calculation
        float walkLean = _walkSpeed * 0.15f; // Forward tilt
        Quaternion tiltRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, walkLean);
        Quaternion finalBaseRotation = bodyRotation * tiltRotation;

        // Interpolate animation values
        float targetArmAngle = 0;
        float targetLegAngle = 0;
        Vector3 targetCenterOffset = Vector3.Zero;
        
        // --- Landing Effect ---
        if (_landingEffectTimer > 0)
        {
            float intensity = (_landingEffectTimer / 0.25f);
            targetCenterOffset += new Vector3(0, -0.4f * intensity, 0); // Crouch down
        }

        if (_animState == AnimState.Walk)
        {
            float swing = MathF.Sin(WalkCycle) * MaxLimbAngle * _walkSpeed;
            targetLegAngle = swing;
            targetArmAngle = swing; 
            
            // Bounce and Side-sway
            float bounce = MathF.Abs(MathF.Cos(WalkCycle)) * 0.15f; 
            float sway = MathF.Sin(WalkCycle) * 0.1f;
            targetCenterOffset += new Vector3(sway, bounce, 0);
        }
        else if (_animState == AnimState.Jump)
        {
            targetArmAngle = 2.8f; 
            targetLegAngle = 0.5f;
            targetCenterOffset = new Vector3(0, 0.2f, 0);
        }
        else if (_animState == AnimState.Fall)
        {
            float flail = MathF.Sin(_animTimer * 12) * 0.25f;
            targetArmAngle = 2.4f + flail;
            targetLegAngle = flail * 0.5f;
        }
        
        float lerpSpeed = 15f * 0.016f; 
        _armAngle = Lerp(_armAngle, targetArmAngle, lerpSpeed);
        _legAngle = Lerp(_legAngle, targetLegAngle, lerpSpeed);
        _centerOffset = Vector3.Lerp(_centerOffset, targetCenterOffset, lerpSpeed);
        
        // 2. Render Body Parts (SMOOTH MODE 2)
        // Torso
        DrawSmoothPart(renderer, _torso, finalBaseRotation, Quaternion.Identity, _centerOffset);
        // Head (follows body tilt slightly or stays level? Let's stay with body)
        DrawSmoothPart(renderer, _head, finalBaseRotation, Quaternion.Identity, _centerOffset);
        
        // Iconic Face (Eyes and Smile - Black mini cubes)
        Vector3 black = new Vector3(0.1f, 0.1f, 0.1f);
        Vector3 headWorldPos = GetPartWorldPos(_head, finalBaseRotation, Quaternion.Identity, _centerOffset);
        
        // Face relative to head assembly
        // Center of head is current headWorldPos. 
        // We need to transform face components by finalBaseRotation
        Vector3 rEyeOffset = Vector3.Transform(new Vector3(0.2f, 0.2f, 0.51f), finalBaseRotation);
        renderer.DrawCube(headWorldPos + rEyeOffset, new Vector3(0.15f, 0.15f, 0.05f), black, finalBaseRotation);
        
        Vector3 lEyeOffset = Vector3.Transform(new Vector3(-0.2f, 0.2f, 0.51f), finalBaseRotation);
        renderer.DrawCube(headWorldPos + lEyeOffset, new Vector3(0.15f, 0.15f, 0.05f), black, finalBaseRotation);
        
        Vector3 smileOffset = Vector3.Transform(new Vector3(0.0f, -0.15f, 0.51f), finalBaseRotation);
        renderer.DrawCube(headWorldPos + smileOffset, new Vector3(0.5f, 0.1f, 0.05f), black, finalBaseRotation);

        // Arms (Counter-swing + Shoulder Sway)
        float shoulderSway = (_animState == AnimState.Walk) ? MathF.Sin(WalkCycle) * 0.2f : 0f;
        var leftArmRot = Quaternion.CreateFromAxisAngle(Vector3.UnitX, _armAngle) * Quaternion.CreateFromAxisAngle(Vector3.UnitY, shoulderSway);
        var rightArmRot = Quaternion.CreateFromAxisAngle(Vector3.UnitX, -_armAngle) * Quaternion.CreateFromAxisAngle(Vector3.UnitY, -shoulderSway);
        
        DrawSmoothPart(renderer, _leftArm, finalBaseRotation, leftArmRot, _centerOffset);
        DrawSmoothPart(renderer, _rightArm, finalBaseRotation, rightArmRot, _centerOffset);
        
        // Legs (Walk cycle)
        float leftLegTarget = -_legAngle;
        float rightLegTarget = _legAngle;
        
        if (_animState == AnimState.Jump) { leftLegTarget = 0.4f; rightLegTarget = -0.2f; }
        else if (_animState == AnimState.Fall) { leftLegTarget = _legAngle; rightLegTarget = -_legAngle; }

        var leftLegRot = Quaternion.CreateFromAxisAngle(Vector3.UnitX, leftLegTarget);
        var rightLegRot = Quaternion.CreateFromAxisAngle(Vector3.UnitX, rightLegTarget);

        DrawSmoothPart(renderer, _leftLeg, finalBaseRotation, leftLegRot, _centerOffset);
        DrawSmoothPart(renderer, _rightLeg, finalBaseRotation, rightLegRot, _centerOffset);
    }
    
    private void DrawSmoothPart(Renderer renderer, PlayerPart part, Quaternion bodyRotation, Quaternion localRotation, Vector3 extraOffset)
    {
        // 1. Pivot Offset (Relative to Pivot Point) rotated by limb swing
        Vector3 rotatedPivotOffset = Vector3.Transform(part.PivotOffset, localRotation);
        
        // 2. Pivot Position (relative to root) + Extra offset (bounce/tilt)
        Vector3 pivotPos = part.Offset + extraOffset;
        
        // 3. World Position: Combine them and transform by body rotation
        Vector3 finalLocalPos = pivotPos + rotatedPivotOffset;
        Vector3 worldPos = Position + Vector3.Transform(finalLocalPos, bodyRotation);
        
        Quaternion finalRotation = bodyRotation * localRotation;
        renderer.DrawCube(worldPos, part.Size, part.Color, finalRotation, renderMode: 2); 
    }

    private Vector3 GetPartWorldPos(PlayerPart part, Quaternion bodyRotation, Quaternion localRotation, Vector3 extraOffset)
    {
        Vector3 rotatedPivotOffset = Vector3.Transform(part.PivotOffset, localRotation);
        Vector3 finalLocalPos = part.Offset + extraOffset + rotatedPivotOffset;
        return Position + Vector3.Transform(finalLocalPos, bodyRotation);
    }
    
    private void UpdateAnimationState()
    {
        float dt = 0.016f; // Assumption for state logic
        _animTimer += dt;
        
        // Update Timers
        if (_jumpLockTimer > 0) _jumpLockTimer -= dt;
        if (_fallLockTimer > 0) _fallLockTimer -= dt;
        if (_landingEffectTimer > 0) _landingEffectTimer -= dt;

        var body = _physics.Simulation.Bodies.GetBodyReference(BodyHandle);
        float verticalVel = body.Velocity.Linear.Y;
        
        // 1. Landing detection
        if (_isGrounded && !_wasGroundedLastFrame)
        {
            _landingEffectTimer = 0.25f; // Crouch for 0.25s
            _jumpLockTimer = 0; 
            _fallLockTimer = 0;
        }
        _wasGroundedLastFrame = _isGrounded;

        // 2. State selection (Hysteresis & Persistence)
        if (verticalVel > 2.0f || _jumpLockTimer > 0) 
        {
            if (verticalVel > 2.0f) _jumpLockTimer = 0.35f; 
            _animState = AnimState.Jump;
        }
        // Fall trigger: higher threshold if not falling, persistence if already falling
        else if ((verticalVel < -6.0f || (_animState == AnimState.Fall && _fallLockTimer > 0)) && !_isGrounded)
        {
            if (verticalVel < -6.0f) _fallLockTimer = 0.25f;
            _animState = AnimState.Fall;
        }
        else if (_walkSpeed > 0.1f && _isGrounded)
            _animState = AnimState.Walk;
        else if (_isGrounded)
            _animState = AnimState.Idle;
    }
    
    private float Lerp(float a, float b, float t) => a + (b - a) * t;

    private void DrawPartWithOffset(Renderer renderer, PlayerPart part, Quaternion bodyRotation, Quaternion localRotation, Vector3 extraOffset)
    {
        // 1. Pivot Rotation
        Vector3 rotatedPivotOffset = Vector3.Transform(part.PivotOffset, localRotation);
        
        // 2. Final Local Pos
        Vector3 finalLocalPos = part.Offset + rotatedPivotOffset + extraOffset;
        
        // 3. World Pos
        Vector3 worldOffset = Vector3.Transform(finalLocalPos, bodyRotation);
        Vector3 worldPos = Position + worldOffset;
        
        // 4. Final Rotation
        Quaternion finalRotation = bodyRotation * localRotation;
        
        renderer.DrawCube(worldPos, part.Size, part.Color, finalRotation);
    }

    private static float Approach(float current, float target, float delta)
    {
        if (current < target)
            return MathF.Min(current + delta, target);
        else
            return MathF.Max(current - delta, target);
    }

    public void Dispose()
    {
        // Nothing to dispose - mesh is managed by renderer
    }
}
