using Silk.NET.OpenGL;
using System.Numerics;
using CatCube.Rendering;
using CatCube.Shared;

namespace CatCube.Entities;

/// <summary>
/// A remote player controlled by the server (other people)
/// </summary>
public class RemotePlayer : IDisposable
{
    private readonly GL _gl;
    
    public int Id { get; }
    public string? Username { get; private set; } // Nullable, set via packet
    public Vector3 Position { get; set; }
    public float Rotation { get; set; }
    public float WalkCycle { get; set; }
    public AnimState State { get; private set; } = AnimState.Idle;
    
    // Remote animation smoothing
    private Vector3 _targetPosition;
    private float _targetRotation = 0f;
    private float _targetWalkCycle = 0f;
    private AnimState _targetState = AnimState.Idle;
    private AvatarData _targetAvatar;

    private float _armAngle = 0f;
    private float _legAngle = 0f;
    private Vector3 _centerOffset = Vector3.Zero;
    private float _animTimer = 0f;
    
    // Body parts
    private PlayerPart _torso;
    private PlayerPart _head;
    private PlayerPart _leftArm;
    private PlayerPart _rightArm;
    private PlayerPart _leftLeg;
    private PlayerPart _rightLeg;
    
    private const float MaxLimbAngle = 0.7f;

    public RemotePlayer(GL gl, int id, Vector3 startPos)
    {
        _gl = gl;
        Id = id;
        Position = startPos;
        Rotation = 0f;
        _targetPosition = startPos;
        _targetRotation = 0f;
        _targetState = AnimState.Idle;
        _targetAvatar = new AvatarData { ShirtColor="#CC3333", PantsColor="#264073", SkinColor="#FFD9B8" };
        
        // Initialize parts with default colors
        UpdateParts();
    }

    public void UpdateState(Vector3 pos, float rot, float walkCycle, AnimState state, string? username, AvatarData avatar)
    {
        _targetPosition = pos;
        _targetRotation = rot;
        _targetWalkCycle = walkCycle;
        _targetState = state;
        
        // Only update parts if appearance changed
        if (!IsAvatarEqual(_targetAvatar, avatar))
        {
            _targetAvatar = avatar;
            UpdateParts();
        }
        
        if (username != null && Username == null) Username = username;
    }

    private bool IsAvatarEqual(AvatarData a, AvatarData b)
    {
        return a.ShirtColor == b.ShirtColor && 
               a.PantsColor == b.PantsColor && 
               a.SkinColor == b.SkinColor && 
               a.BodyType == b.BodyType && 
               a.HairStyle == b.HairStyle;
    }

    private void UpdateParts()
    {
        // Colors
        Vector3 skinColor = HexToVector3(_targetAvatar.SkinColor);
        Vector3 shirtColor = HexToVector3(_targetAvatar.ShirtColor);
        Vector3 pantsColor = HexToVector3(_targetAvatar.PantsColor);
        
        // Body Type Scaling
        float widthScale = 1.0f;
        float depthScale = 1.0f;
        
        if (_targetAvatar.BodyType == 1) // Slim
        {
            widthScale = 0.8f;
            depthScale = 0.8f;
        }
        else if (_targetAvatar.BodyType == 2) // Blocky
        {
            widthScale = 1.2f;
            depthScale = 1.1f;
        }

        // Torso: 2x2x1 -> Center at +2.0 (goes from +1.0 to +3.0)
        _torso = new PlayerPart("Torso", new Vector3(0, 2.0f, 0), new Vector3(2.0f * widthScale, 2.0f, 1.0f * depthScale), shirtColor, Vector3.Zero);
        
        // Head: 1x1x1 -> Pivot at Neck (Torso Top = +3.0). Center at +3.5
        _head = new PlayerPart("Head", new Vector3(0, 3.0f, 0), new Vector3(1.0f, 1.0f, 1.0f), skinColor, new Vector3(0, 0.5f, 0));
        
        // Arms: 1x2x1 -> Pivot at Shoulder (Torso Top = +3.0). Center at +2.0
        float armOffset = 1.5f * widthScale;
        _leftArm = new PlayerPart("LeftArm", new Vector3(-armOffset, 3.0f, 0), new Vector3(1.0f * widthScale, 2.0f, 1.0f * depthScale), skinColor, new Vector3(0, -1.0f, 0));
        _rightArm = new PlayerPart("RightArm", new Vector3(armOffset, 3.0f, 0), new Vector3(1.0f * widthScale, 2.0f, 1.0f * depthScale), skinColor, new Vector3(0, -1.0f, 0));
        
        // Legs: 1x2x1 -> Pivot at Hip (Torso Bottom = +1.0). Center at 0.0
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

    public void Update(float deltaTime)
    {
        // Smooth interpolation
        float lerpFactor = Math.Clamp(deltaTime * 10f, 0f, 1f);
        
        Position = Vector3.Lerp(Position, _targetPosition, lerpFactor);
        
        // Lerp rotation correctly
        float angleDiff = _targetRotation - Rotation;
        while (angleDiff > MathF.PI) angleDiff -= 2 * MathF.PI;
        while (angleDiff < -MathF.PI) angleDiff += 2 * MathF.PI;
        Rotation += angleDiff * lerpFactor;
        
        WalkCycle = Lerp(WalkCycle, _targetWalkCycle, lerpFactor);
        State = _targetState; // States are discrete
    }

    public void Render(Renderer renderer)
    {
        _animTimer += 0.016f; // Manual tick for animations
        
        Quaternion bodyRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, Rotation);
        
        // 1. Physical Tilt & Animations
        float walkLean = (State == AnimState.Walk) ? 0.15f : 0f;
        Quaternion tiltRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, walkLean);
        Quaternion finalBaseRotation = bodyRotation * tiltRotation;

        float targetArmAngle = 0;
        float targetLegAngle = 0;
        Vector3 targetCenterOffset = Vector3.Zero;

        if (State == AnimState.Walk)
        {
            float swing = MathF.Sin(WalkCycle) * MaxLimbAngle;
            targetLegAngle = swing;
            targetArmAngle = swing;
            float bounce = MathF.Abs(MathF.Cos(WalkCycle)) * 0.15f;
            float sway = MathF.Sin(WalkCycle) * 0.1f;
            targetCenterOffset = new Vector3(sway, bounce, 0);
        }
        else if (State == AnimState.Jump)
        {
            targetArmAngle = 2.8f;
            targetLegAngle = 0.5f;
            targetCenterOffset = new Vector3(0, 0.2f, 0);
        }
        else if (State == AnimState.Fall)
        {
            float flail = MathF.Sin(_animTimer * 12) * 0.25f;
            targetArmAngle = 2.4f + flail;
            targetLegAngle = flail * 0.5f;
        }

        // Interpolate for smoothness
        float lerpSpeed = 15f * 0.016f;
        _armAngle = Lerp(_armAngle, targetArmAngle, lerpSpeed);
        _legAngle = Lerp(_legAngle, targetLegAngle, lerpSpeed);
        _centerOffset = Vector3.Lerp(_centerOffset, targetCenterOffset, lerpSpeed);

        // 2. Render Body Parts
        DrawPart(renderer, _torso, finalBaseRotation, Quaternion.Identity, _centerOffset);
        DrawPart(renderer, _head, finalBaseRotation, Quaternion.Identity, _centerOffset);

        // Iconic Face (Synced)
        Vector3 black = new Vector3(0.1f, 0.1f, 0.1f);
        Vector3 headWorldPos = GetPartWorldPos(_head, finalBaseRotation, Quaternion.Identity, _centerOffset);
        Vector3 rEyeOffset = Vector3.Transform(new Vector3(0.2f, 0.2f, 0.51f), finalBaseRotation);
        renderer.DrawCube(headWorldPos + rEyeOffset, new Vector3(0.15f, 0.15f, 0.05f), black, finalBaseRotation);
        Vector3 lEyeOffset = Vector3.Transform(new Vector3(-0.2f, 0.2f, 0.51f), finalBaseRotation);
        renderer.DrawCube(headWorldPos + lEyeOffset, new Vector3(0.15f, 0.15f, 0.05f), black, finalBaseRotation);
        Vector3 smileOffset = Vector3.Transform(new Vector3(0.0f, -0.15f, 0.51f), finalBaseRotation);
        renderer.DrawCube(headWorldPos + smileOffset, new Vector3(0.5f, 0.1f, 0.05f), black, finalBaseRotation);

        // Arms (Sway)
        float shoulderSway = (State == AnimState.Walk) ? MathF.Sin(WalkCycle) * 0.2f : 0f;
        var leftArmRot = Quaternion.CreateFromAxisAngle(Vector3.UnitX, _armAngle) * Quaternion.CreateFromAxisAngle(Vector3.UnitY, shoulderSway);
        var rightArmRot = Quaternion.CreateFromAxisAngle(Vector3.UnitX, -_armAngle) * Quaternion.CreateFromAxisAngle(Vector3.UnitY, -shoulderSway);
        DrawPart(renderer, _leftArm, finalBaseRotation, leftArmRot, _centerOffset);
        DrawPart(renderer, _rightArm, finalBaseRotation, rightArmRot, _centerOffset);

        // Hair
        DrawHair(renderer, finalBaseRotation, _centerOffset);

        // Legs
        float leftLegTarget = -_legAngle;
        float rightLegTarget = _legAngle;
        if (State == AnimState.Jump) { leftLegTarget = 0.4f; rightLegTarget = -0.2f; }
        
        var leftLegRot = Quaternion.CreateFromAxisAngle(Vector3.UnitX, leftLegTarget);
        var rightLegRot = Quaternion.CreateFromAxisAngle(Vector3.UnitX, rightLegTarget);
        DrawPart(renderer, _leftLeg, finalBaseRotation, leftLegRot, _centerOffset);
        DrawPart(renderer, _rightLeg, finalBaseRotation, rightLegRot, _centerOffset);
    }

    private void DrawPart(Renderer renderer, PlayerPart part, Quaternion bodyRotation, Quaternion localRotation, Vector3 extraOffset)
    {
        Vector3 rotatedPivotOffset = Vector3.Transform(part.PivotOffset, localRotation);
        Vector3 finalLocalPos = part.Offset + extraOffset + rotatedPivotOffset;
        Vector3 worldPos = Position + Vector3.Transform(finalLocalPos, bodyRotation);
        
        Quaternion finalRotation = bodyRotation * localRotation;
        renderer.DrawCube(worldPos, part.Size, part.Color, finalRotation, renderMode: 2);
    }

    private void DrawHair(Renderer renderer, Quaternion bodyRotation, Vector3 extraOffset)
    {
        if (_targetAvatar.HairStyle <= 0) return;

        Vector3 hairColor = new Vector3(0.2f, 0.1f, 0.05f); // Dark Brown
        Vector3 headWorldPos = GetPartWorldPos(_head, bodyRotation, Quaternion.Identity, extraOffset);
        
        switch (_targetAvatar.HairStyle)
        {
            case 1: // Short
                renderer.DrawCube(headWorldPos + Vector3.Transform(new Vector3(0, 0.51f, 0), bodyRotation), new Vector3(1.1f, 0.2f, 1.1f), hairColor, bodyRotation);
                break;
            case 2: // Long
                renderer.DrawCube(headWorldPos + Vector3.Transform(new Vector3(0, 0.51f, 0), bodyRotation), new Vector3(1.1f, 0.2f, 1.1f), hairColor, bodyRotation);
                renderer.DrawCube(headWorldPos + Vector3.Transform(new Vector3(0, -0.2f, -0.55f), bodyRotation), new Vector3(1.1f, 1.2f, 0.15f), hairColor, bodyRotation);
                break;
            case 3: // Spiky
                renderer.DrawCube(headWorldPos + Vector3.Transform(new Vector3(0, 0.51f, 0), bodyRotation), new Vector3(1.1f, 0.2f, 1.1f), hairColor, bodyRotation);
                for (int i = 0; i < 3; i++) {
                    float offX = (i - 1) * 0.3f;
                    renderer.DrawCube(headWorldPos + Vector3.Transform(new Vector3(offX, 0.7f, 0), bodyRotation), new Vector3(0.2f, 0.4f, 0.2f), hairColor, bodyRotation);
                }
                break;
            case 4: // Afro
                renderer.DrawCube(headWorldPos + Vector3.Transform(new Vector3(0, 0.2f, 0), bodyRotation), new Vector3(1.4f, 1.4f, 1.4f), hairColor, bodyRotation);
                break;
        }
    }

    private Vector3 GetPartWorldPos(PlayerPart part, Quaternion bodyRotation, Quaternion localRotation, Vector3 extraOffset)
    {
        Vector3 rotatedPivotOffset = Vector3.Transform(part.PivotOffset, localRotation);
        Vector3 finalLocalPos = part.Offset + extraOffset + rotatedPivotOffset;
        return Position + Vector3.Transform(finalLocalPos, bodyRotation);
    }

    private static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * Math.Clamp(t, 0f, 1f);
    }

    public void Dispose()
    {
    }
}
