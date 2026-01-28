using System.Numerics;
using CatCube.Input;

namespace CatCube.Rendering;

public class Camera
{
    public Vector3 Position { get; private set; }
    public Vector3 Target { get; private set; }
    
    private float _yaw = 0f;
    private float _pitch = 20f;
    private float _distance = 8f;
    private float _targetDistance = 8f;
    
    private float _width;
    private float _height;
    
    private const float MinPitch = -30f;
    private const float MaxPitch = 60f;
    private const float MinDistance = 3f;
    private const float MaxDistance = 20f;
    private const float MouseSensitivity = 0.15f;
    private const float ZoomSpeed = 2f;
    private const float SmoothSpeed = 8f;

    public Camera(Vector3 initialPosition, int width, int height)
    {
        Position = initialPosition;
        _width = width;
        _height = height;
    }

    public void Update(float deltaTime, Vector3 targetPosition, InputManager input, Func<Vector3, Vector3, float, float?>? raycast = null)
    {
        // Target is slightly above player (at head level)
        Target = targetPosition + new Vector3(0, 1.5f, 0);
        
        // Rotate camera with mouse ONLY if RMB is held (Standard across Studio and Preview)
        if (input.IsMouseButtonPressed(Silk.NET.Input.MouseButton.Right))
        {
            _yaw -= input.MouseDelta.X * MouseSensitivity;
            _pitch += input.MouseDelta.Y * MouseSensitivity; 
            _pitch = Math.Clamp(_pitch, MinPitch, MaxPitch);
        }
        
        // Zoom with scroll stays active regardless of button
        _targetDistance -= input.ScrollDelta * ZoomSpeed;
        _targetDistance = Math.Clamp(_targetDistance, MinDistance, MaxDistance);
        
        // Smooth zoom interpolation
        _distance = Lerp(_distance, _targetDistance, deltaTime * SmoothSpeed);
        
        // Calculate camera position based on spherical coordinates
        float pitchRad = MathF.PI / 180f * _pitch;
        float yawRad = MathF.PI / 180f * _yaw;
        
        float horizontalDist = _distance * MathF.Cos(pitchRad);
        float verticalDist = _distance * MathF.Sin(pitchRad);
        
        Vector3 offset = new Vector3(
            horizontalDist * MathF.Sin(yawRad),
            verticalDist,
            horizontalDist * MathF.Cos(yawRad)
        );
        
        Vector3 desiredPos = Target + offset;
        
        // --- COLLISION LOGIC ---
        if (raycast != null)
        {
            Vector3 dir = Vector3.Normalize(offset);
            float? hitDist = raycast(Target, dir, _distance);
            if (hitDist.HasValue)
            {
                // Move camera forward by hit distance minus a small cushion
                float adjustedDist = Math.Max(0.8f, hitDist.Value - 0.4f);
                Position = Target + dir * adjustedDist;
                return;
            }
        }

        Position = desiredPos;
    }

    public Matrix4x4 GetViewMatrix()
    {
        return Matrix4x4.CreateLookAt(Position, Target, Vector3.UnitY);
    }

    public Matrix4x4 GetProjectionMatrix()
    {
        return Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI / 180f * 60f, // 60 degree FOV
            _width / _height,
            0.1f,
            1000f
        );
    }

    public void UpdateAspect(int width, int height)
    {
        _width = width;
        _height = height;
    }
    
    /// <summary>
    /// Gets the forward direction of the camera (horizontal only for movement)
    /// </summary>
    public Vector3 GetForwardDirection()
    {
        float yawRad = MathF.PI / 180f * _yaw;
        return new Vector3(-MathF.Sin(yawRad), 0, -MathF.Cos(yawRad));
    }
    
    /// <summary>
    /// Gets the right direction of the camera
    /// </summary>
    public Vector3 GetRightDirection()
    {
        float yawRad = MathF.PI / 180f * _yaw;
        return new Vector3(MathF.Cos(yawRad), 0, -MathF.Sin(yawRad));
    }

    /// <summary>
    /// Gets the up direction of the camera
    /// </summary>
    public Vector3 GetUpDirection()
    {
        // Simple approximation for fixed-up camera, or use Matrix logic
        var view = GetViewMatrix();
        Matrix4x4.Invert(view, out var inv);
        return new Vector3(inv.M21, inv.M22, inv.M23);
    }

    private static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * Math.Clamp(t, 0f, 1f);
    }
}
