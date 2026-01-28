using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Input;
using System.Numerics;
using CatCube.Rendering;
using CatCube.Input;
using CatCube.Engine;
using CatCube.Engine.Physics;
using Silk.NET.OpenGL.Extensions.ImGui;
using ImGuiNET;
using BepuPhysics;
using BepuPhysics.Collidables;

namespace CatCube.Studio;

class Program
{
    private static IWindow _window = null!;
    private static GL _gl = null!;
    private static ImGuiController _imGuiController = null!;
    private static Renderer _renderer = null!;
    private static Camera _camera = null!;
    private static InputManager _inputManager = null!;
    private static DataModel _dataModel = null!;
    
    // Viewport State
    private static uint _fbo;
    private static uint _fboColor;
    private static uint _viewportWidth;
    private static uint _viewportHeight;
    private static Vector2 _viewportPos;

    // Gizmo State
    private enum GizmoMode { Move, Scale, Rotate }
    private static GizmoMode _currentGizmo = GizmoMode.Move;
    private static int _movingAxis = -1; // 0=X, 1=Y, 2=Z
    private static Vector3 _dragStartOffset;
    private static Vector3 _initialPartPos;

    // MSAA Buffers
    private static uint _msaaFbo;
    private static uint _msaaColor;
    private static uint _msaaDepth;
    private const int MsaaSamples = 4;
    
    // Camera state
    private static float _camYaw = 0;
    private static float _camPitch = -20;
    private static Vector3 _camPos = new Vector3(0, 10, 30);
    
    // Performance stats
    private static float _fps;
    private static float _statsTimer = 0;
    private static bool _isViewportHovered;
    private static bool _isDraggingCamera;
    private static bool _lastDraggingCamera;
    
    // UI State
    private static Instance? _selectedInstance;
    private static string _searchText = "";

    static void Main(string[] args)
    {
        var options = WindowOptions.Default;
        options.Size = new Silk.NET.Maths.Vector2D<int>(1600, 900);
        options.Title = "CatStudio - Editor de Mapas";
        // Removed explicit APIVersion to match client style and avoid errors

        _window = Window.Create(options);

        _window.Load += OnLoad;
        _window.Render += OnRender;
        _window.Update += OnUpdate;
        _window.Resize += OnResize;
        _window.Closing += OnClosing;

        _window.Run();
    }

    private static void OnLoad()
    {
        _gl = GL.GetApi(_window);
        var input = _window.CreateInput();
        _inputManager = new InputManager(input);
        _imGuiController = new ImGuiController(_gl, _window, input);
        
        _renderer = new Renderer(_gl);
        _renderer.Initialize();
        
        _camera = new Camera(_camPos, _window.Size.X, _window.Size.Y);
        _dataModel = DataModel.Current;
        
        CreateFBO(1024, 768); // Initial size
        DarkTheme();
    }

    private static unsafe void CreateFBO(int width, int height)
    {
        if (width <= 0 || height <= 0) return;
        
        // --- 1. RESOLVE FBO (Normal Texture for ImGui) ---
        if (_fbo != 0)
        {
            _gl.DeleteFramebuffer(_fbo);
            _gl.DeleteTexture(_fboColor);
        }

        _fbo = _gl.GenFramebuffer();
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);

        _fboColor = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, _fboColor);
        _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint)width, (uint)height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
        _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _fboColor, 0);

        // --- 2. MSAA FBO (Multisampled Buffers) ---
        if (_msaaFbo != 0)
        {
            _gl.DeleteFramebuffer(_msaaFbo);
            _gl.DeleteRenderbuffer(_msaaColor);
            _gl.DeleteRenderbuffer(_msaaDepth);
        }

        _msaaFbo = _gl.GenFramebuffer();
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _msaaFbo);

        // Multisampled Color Buffer
        _msaaColor = _gl.GenRenderbuffer();
        _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _msaaColor);
        _gl.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, (uint)MsaaSamples, InternalFormat.Rgba8, (uint)width, (uint)height);
        _gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, RenderbufferTarget.Renderbuffer, _msaaColor);

        // Multisampled Depth Buffer
        _msaaDepth = _gl.GenRenderbuffer();
        _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _msaaDepth);
        _gl.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, (uint)MsaaSamples, InternalFormat.DepthComponent24, (uint)width, (uint)height);
        _gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, _msaaDepth);

        if (_gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete)
            Console.WriteLine("[Studio Error] MSAA Framebuffer incommplete!");

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        
        _viewportWidth = (uint)width;
        _viewportHeight = (uint)height;
    }

    private static void OnUpdate(double dt)
    {
        _inputManager.Update();
        float deltaTime = (float)dt;

        // Statistics
        _statsTimer += deltaTime;
        if (_statsTimer >= 0.5f)
        {
            _fps = 1.0f / deltaTime;
            _statsTimer = 0;
        }

        // Physics step
        _dataModel.Workspace.StepPhysics(deltaTime);
        SyncPhysicsRecursive(_dataModel.Workspace);

        if (_inputManager.IsKeyJustPressed(Key.Number1) || _inputManager.IsKeyJustPressed(Key.G)) _currentGizmo = GizmoMode.Move;
        if (_inputManager.IsKeyJustPressed(Key.Number2) || _inputManager.IsKeyJustPressed(Key.S)) _currentGizmo = GizmoMode.Scale;
        if (_inputManager.IsKeyJustPressed(Key.Number3) || _inputManager.IsKeyJustPressed(Key.R)) _currentGizmo = GizmoMode.Rotate;
        if (_inputManager.IsKeyJustPressed(Key.P)) LaunchPreview();

        if (IsKeyPressed(Key.Delete) && _selectedInstance != null)
        {
            _selectedInstance.Destroy();
            _selectedInstance = null;
        }

        // --- Duplicate (Ctrl+D) ---
        bool ctrl = IsKeyPressed(Key.ControlLeft) || IsKeyPressed(Key.ControlRight);
        bool dPressed = IsKeyPressed(Key.D);
        if (ctrl && dPressed)
        {
            if (!_duplicateKeyHeld && _selectedInstance is Part original)
            {
                var clone = original is SpawnLocation ? new SpawnLocation() : new Part();
                clone.Name = original.Name + " (Clone)";
                clone.Position = original.Position + new Vector3(original.Size.X, 0, 0);
                clone.Size = original.Size;
                clone.Color = original.Color;
                clone.Transparency = original.Transparency;
                clone.Anchored = original.Anchored;
                clone.Physical = original.Physical;
                clone.Parent = original.Parent;
                _selectedInstance = clone;
                _duplicateKeyHeld = true;
            }
        }
        else
        {
            _duplicateKeyHeld = false;
        }

        // --- Free Camera Logic ---
        bool rmbPressed = _inputManager.IsMouseButtonPressed(MouseButton.Right);
        if (_isViewportHovered && rmbPressed) _isDraggingCamera = true;
        if (!rmbPressed) _isDraggingCamera = false;

        if (_isDraggingCamera)
        {
            // Ignore delta on the very first frame of drag to prevent "Mouse Jump" from center
            if (_lastDraggingCamera)
            {
                float sensitivity = 0.12f;
                _camYaw -= _inputManager.MouseDelta.X * sensitivity;
                _camPitch -= _inputManager.MouseDelta.Y * sensitivity; 
                _camPitch = Math.Clamp(_camPitch, -89f, 89f);
            }
            
            // --- GATED MOVEMENT (ONLY WHILE HOLDING RMB) ---
            float moveSpeed = 60f * deltaTime;
            float yawRad = _camYaw * MathF.PI / 180f;
            Vector3 forward = new Vector3(-MathF.Sin(yawRad), 0, -MathF.Cos(yawRad));
            Vector3 right = new Vector3(MathF.Cos(yawRad), 0, -MathF.Sin(yawRad));

            Vector2 moveInput = _inputManager.Movement;
            _camPos += forward * moveInput.Y * moveSpeed;
            _camPos += right * moveInput.X * moveSpeed;

            if (_inputManager.Jump) _camPos.Y += moveSpeed;
            if (IsKeyPressed(Key.Q)) _camPos.Y -= moveSpeed;
        }
        
        _lastDraggingCamera = _isDraggingCamera;

        // --- Gizmo / Selection Logic ---
        bool lmbJustPressed = _inputManager.IsMouseButtonJustPressed(MouseButton.Left);
        bool lmbPressed = _inputManager.IsMouseButtonPressed(MouseButton.Left);
        
        if (_isViewportHovered)
        {
            if (lmbJustPressed)
            {
                int gizmoHit = RaycastGizmo();
                if (gizmoHit != -1 && _selectedInstance is Part)
                {
                    _movingAxis = gizmoHit;
                    _initialPartPos = ((Part)_selectedInstance).Position;
                }
                else
                {
                    RaycastSelection();
                }
            }
            else if (lmbPressed && _movingAxis != -1 && _selectedInstance is Part p)
            {
                UpdateGizmoDrag(p);
            }
        }
        
        if (!lmbPressed) _movingAxis = -1;
    }

    private static bool _duplicateKeyHeld;

    private static void LaunchPreview()
    {
        Console.WriteLine("[Studio] Exporting scene for preview...");
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("-- CatStudio Generated Preview Map");
        sb.AppendLine("local Workspace = game.Workspace");
        sb.AppendLine();

        foreach (var child in _dataModel.Workspace.GetChildren())
        {
            ExportInstance(child, sb, "Workspace");
        }

        try
        {
            string previewDir = Path.Combine("maps", "_studio_preview", "Workspace");
            Directory.CreateDirectory(previewDir);
            File.WriteAllText(Path.Combine(previewDir, "map.lua"), sb.ToString());
            Console.WriteLine("[Studio] Launching Client Preview...");
            
            // Start client process (standalone or via dotnet)
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "run --project src/CatCube.Client -- --ip 127.0.0.1 --map _studio_preview",
                UseShellExecute = false,
                CreateNoWindow = false
            };
            System.Diagnostics.Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Export Error] {ex.Message}");
        }
    }

    private static void ExportInstance(Instance inst, System.Text.StringBuilder sb, string parentVar)
    {
        string className = inst.GetType().Name;
        // Basic export for Parts and SpawnLocations
        if (inst is not Part && inst is not SpawnLocation && inst.GetChildren().Length == 0) return;

        string varName = $"inst_{Guid.NewGuid().ToString().Replace("-", "")}";
        
        sb.AppendLine($"local {varName} = Instance.new(\"{className}\")");
        sb.AppendLine($"{varName}.Name = \"{inst.Name}\"");
        sb.AppendLine($"{varName}.Parent = {parentVar}");

        if (inst is Part p)
        {
            var pos = p.Position;
            var size = p.Size;
            var col = p.Color;
            sb.AppendLine($"{varName}.Position = Vector3.new({pos.X.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {pos.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {pos.Z.ToString(System.Globalization.CultureInfo.InvariantCulture)})");
            sb.AppendLine($"{varName}.Size = Vector3.new({size.X.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {size.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {size.Z.ToString(System.Globalization.CultureInfo.InvariantCulture)})");
            sb.AppendLine($"{varName}.Color = Vector3.new({col.X.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {col.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {col.Z.ToString(System.Globalization.CultureInfo.InvariantCulture)})");
            sb.AppendLine($"{varName}.Anchored = {(p.Anchored ? "true" : "false")}");
            sb.AppendLine($"{varName}.Transparency = {p.Transparency.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        }

        foreach (var child in inst.GetChildren())
        {
            ExportInstance(child, sb, varName);
        }
    }

    private static bool IsKeyPressed(Key k) => _inputManager.IsKeyPressed(k);

    private static void DrawGizmos(Part p)
    {
        float length = 8.0f;
        float thickness = 0.15f;
        float tipSize = 0.5f;
        
        // X - Red
        _renderer.DrawCube(p.Position + new Vector3(length/2, 0, 0), new Vector3(length, thickness, thickness), new Vector3(1.0f, 0.1f, 0.1f), Quaternion.Identity, 3);
        if (_currentGizmo == GizmoMode.Scale)
            _renderer.DrawCube(p.Position + new Vector3(length, 0, 0), new Vector3(tipSize), new Vector3(1.0f, 0.1f, 0.1f), Quaternion.Identity, 3);

        // Y - Green
        _renderer.DrawCube(p.Position + new Vector3(0, length/2, 0), new Vector3(thickness, length, thickness), new Vector3(0.1f, 1.0f, 0.1f), Quaternion.Identity, 3);
        if (_currentGizmo == GizmoMode.Scale)
            _renderer.DrawCube(p.Position + new Vector3(0, length, 0), new Vector3(tipSize), new Vector3(0.1f, 1.0f, 0.1f), Quaternion.Identity, 3);

        // Z - Blue
        _renderer.DrawCube(p.Position + new Vector3(0, 0, length/2), new Vector3(thickness, thickness, length), new Vector3(0.1f, 0.1f, 1.0f), Quaternion.Identity, 3);
        if (_currentGizmo == GizmoMode.Scale)
            _renderer.DrawCube(p.Position + new Vector3(0, 0, length), new Vector3(tipSize), new Vector3(0.1f, 0.1f, 1.0f), Quaternion.Identity, 3);
    }

    private static int RaycastGizmo()
    {
        if (_selectedInstance is not Part p) return -1;
        float handleLength = 8.0f; // Longer for easier clicking
        float handleThickness = 1.3f; // Thicker for easier clicking
        
        // --- 1. Construct Ray ---
        var mousePos = _inputManager.MousePosition;
        float nx = ((mousePos.X - _viewportPos.X) / _viewportWidth) * 2f - 1f;
        float ny = 1f - ((mousePos.Y - _viewportPos.Y) / _viewportHeight) * 2f;
        if (float.IsNaN(nx) || float.IsNaN(ny)) return -1;
        
        var viewMatrix = GetViewMatrix();
        var projMatrix = _camera.GetProjectionMatrix();
        if (!Matrix4x4.Invert(viewMatrix * projMatrix, out var invVP)) return -1;
        
        Vector4 near = Vector4.Transform(new Vector4(nx, ny, 0, 1), invVP);
        Vector4 far = Vector4.Transform(new Vector4(nx, ny, 1, 1), invVP);
        Vector3 rayStart = new Vector3(near.X/near.W, near.Y/near.W, near.Z/near.W);
        Vector3 rayEnd = new Vector3(far.X/far.W, far.Y/far.W, far.Z/far.W);
        Vector3 rayDir = Vector3.Normalize(rayEnd - rayStart);

        // --- 2. Check each Axis AABB ---
        float minT = float.MaxValue;
        int hitAxis = -1;

        // X - Red
        if (RayAABB(rayStart, rayDir, p.Position + new Vector3(handleLength/2, 0, 0), new Vector3(handleLength, handleThickness, handleThickness), out float tx))
        {
            if (tx < minT) { minT = tx; hitAxis = 0; }
        }
        // Y - Green
        if (RayAABB(rayStart, rayDir, p.Position + new Vector3(0, handleLength/2, 0), new Vector3(handleThickness, handleLength, handleThickness), out float ty))
        {
            if (ty < minT) { minT = ty; hitAxis = 1; }
        }
        // Z - Blue
        if (RayAABB(rayStart, rayDir, p.Position + new Vector3(0, 0, handleLength/2), new Vector3(handleThickness, handleThickness, handleLength), out float tz))
        {
            if (tz < minT) { minT = tz; hitAxis = 2; }
        }

        return hitAxis;
    }

    private static bool RayAABB(Vector3 origin, Vector3 dir, Vector3 center, Vector3 size, out float t)
    {
        Vector3 min = center - size / 2;
        Vector3 max = center + size / 2;
        t = 0;

        float t1 = (min.X - origin.X) / dir.X;
        float t2 = (max.X - origin.X) / dir.X;
        float t3 = (min.Y - origin.Y) / dir.Y;
        float t4 = (max.Y - origin.Y) / dir.Y;
        float t5 = (min.Z - origin.Z) / dir.Z;
        float t6 = (max.Z - origin.Z) / dir.Z;

        float tmin = Math.Max(Math.Max(Math.Min(t1, t2), Math.Min(t3, t4)), Math.Min(t5, t6));
        float tmax = Math.Min(Math.Min(Math.Max(t1, t2), Math.Max(t3, t4)), Math.Max(t5, t6));

        if (tmax < 0 || tmin > tmax) return false;
        t = tmin;
        return true;
    }

    private static Vector2 WorldToScreen(Vector3 worldPos)
    {
        if (_viewportWidth <= 0 || _viewportHeight <= 0) return Vector2.Zero;
        
        var view = GetViewMatrix();
        var proj = _camera.GetProjectionMatrix();
        var vp = view * proj;
        
        Vector4 clip = Vector4.Transform(new Vector4(worldPos, 1), vp);
        if (clip.W <= 0) return Vector2.Zero; // Behind camera
        
        Vector3 ndc = new Vector3(clip.X / clip.W, clip.Y / clip.W, clip.Z / clip.W);
        float screenX = (ndc.X + 1f) / 2f * _viewportWidth;
        float screenY = (1f - ndc.Y) / 2f * _viewportHeight;
        
        return new Vector2(screenX, screenY);
    }

    private static void UpdateGizmoDrag(Part p)
    {
        Vector3 axis = _movingAxis switch {
            0 => Vector3.UnitX,
            1 => Vector3.UnitY,
            2 => Vector3.UnitZ,
            _ => Vector3.Zero
        };
        
        if (axis == Vector3.Zero) return;

        // 1. Project Axis to Screen
        Vector2 screenCenter = WorldToScreen(p.Position);
        Vector2 screenTip = WorldToScreen(p.Position + axis);
        Vector2 screenDir = screenTip - screenCenter;
        float screenLen = screenDir.Length();
        
        if (screenLen < 1.0f) return; // Prevent division by zero or extreme sensitivity
        screenDir = Vector2.Normalize(screenDir);

        // 2. Project Mouse Delta onto Screen Axis
        Vector2 mouseDelta = _inputManager.MouseDelta;
        float dragPixels = Vector2.Dot(mouseDelta, screenDir);
        
        // 3. Convert Pixels to World Units (1 unit = screenLen pixels)
        float worldAmount = dragPixels / screenLen;
        Vector3 delta = axis * worldAmount;

        if (_currentGizmo == GizmoMode.Move)
        {
            p.Position += delta;
        }
        else if (_currentGizmo == GizmoMode.Scale)
        {
            p.Size += delta;
            p.Size = Vector3.Max(p.Size, new Vector3(0.1f));
        }
    }

    private static void RaycastSelection()
    {
        if (_viewportWidth <= 0 || _viewportHeight <= 0) return;

        try
        {
            // 1. Pixel to Ray
            var mousePos = _inputManager.MousePosition;
            float nx = ((mousePos.X - _viewportPos.X) / _viewportWidth) * 2f - 1f;
            float ny = 1f - ((mousePos.Y - _viewportPos.Y) / _viewportHeight) * 2f;
            
            if (float.IsNaN(nx) || float.IsNaN(ny)) return;
            
            var viewMatrix = GetViewMatrix();
            var projMatrix = _camera.GetProjectionMatrix();
            if (!Matrix4x4.Invert(viewMatrix * projMatrix, out var invVP)) return;
            
            // ... (Rest of ray logic)
        
            Vector4 near = Vector4.Transform(new Vector4(nx, ny, 0, 1), invVP);
            Vector4 far = Vector4.Transform(new Vector4(nx, ny, 1, 1), invVP);
            
            Vector3 rayStart = new Vector3(near.X/near.W, near.Y/near.W, near.Z/near.W);
            Vector3 rayEnd = new Vector3(far.X/far.W, far.Y/far.W, far.Z/far.W);
            Vector3 rayDir = Vector3.Normalize(rayEnd - rayStart);

            // 2. Physics Raycast
            var physics = _dataModel.Workspace.Physics;
            if (physics.Raycast(rayStart, rayDir, 1000, out var hit))
            {
                 // CORRECTED MOBILITY CHECK
                 if (hit.Collidable.Mobility == CollidableMobility.Dynamic)
                 {
                     if (physics.BodyToPart.TryGetValue(hit.Collidable.BodyHandle, out var partObj) && partObj is Part p)
                         _selectedInstance = p;
                 }
                 else if (hit.Collidable.Mobility == CollidableMobility.Static)
                 {
                     if (physics.StaticToPart.TryGetValue(hit.Collidable.StaticHandle, out var staticObj) && staticObj is Part sp)
                         _selectedInstance = sp;
                 }
                 else
                 {
                     _selectedInstance = null;
                 }
            }
            else
            {
                _selectedInstance = null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Raycast Error] {ex.Message}");
        }
    }

    private static Matrix4x4 GetViewMatrix()
    {
        Matrix4x4 rotation = Matrix4x4.CreateRotationX(_camPitch * MathF.PI / 180f) * 
                           Matrix4x4.CreateRotationY(_camYaw * MathF.PI / 180f);
        Vector3 lookDir = Vector3.Transform(-Vector3.UnitZ, rotation);
        return Matrix4x4.CreateLookAt(_camPos, _camPos + lookDir, Vector3.UnitY);
    }

    private static void OnRender(double dt)
    {
        _imGuiController.Update((float)dt);

        _gl.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // --- Studio UI Layout ---
        DrawMainMenuBar();
        DrawExplorer();
        DrawProperties();
        DrawViewport();
        DrawStatusBar();

        _imGuiController.Render();
    }

    private static void DrawViewport()
    {
        ImGui.SetNextWindowPos(new Vector2(300, 20), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(_window.Size.X - 600, _window.Size.Y - 45), ImGuiCond.Once);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
        // FIXED: Added NoMove to keep viewport static
        if (ImGui.Begin("Viewport", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoMove))
        {
            Vector2 size = ImGui.GetContentRegionAvail();
            _viewportPos = ImGui.GetWindowPos() + ImGui.GetCursorPos();
            
            if (size.X <= 0 || size.Y <= 0) 
            {
                ImGui.End();
                ImGui.PopStyleVar();
                return;
            }

            if ((uint)size.X != _viewportWidth || (uint)size.Y != _viewportHeight)
            {
                CreateFBO((int)size.X, (int)size.Y);
            }

            // --- 1. SHADOW PASS ---
            Vector3 lightPos = new Vector3(50, 150, 50);
            _renderer.BeginShadowPass(lightPos);
            RenderInstance(_dataModel.Workspace);
            _renderer.EndShadowPass((int)size.X, (int)size.Y);

            // --- 2. MSAA 3D PASS ---
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _msaaFbo);
            _gl.Viewport(0, 0, (uint)size.X, (uint)size.Y);
            _gl.ClearColor(0.15f, 0.15f, 0.18f, 1.0f); // Professional Dark Gray-Blue
            _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            _gl.Enable(EnableCap.DepthTest);
            _gl.Enable(EnableCap.Multisample);

            _camera.UpdateAspect((int)size.X, (int)size.Y);
            
            var view = GetViewMatrix();
            var proj = _camera.GetProjectionMatrix();
            
            _renderer.Begin(view, proj, _camPos);
            
            // Draw Modern Grid (Large grid, stud spacing)
            _renderer.DrawGrid(100, 4.0f, new Vector3(0.25f, 0.25f, 0.25f));
            
            RenderInstance(_dataModel.Workspace);
            
            // --- Selection & Custom Gizmos ---
            if (_selectedInstance is Part p)
            {
                _gl.Disable(EnableCap.DepthTest); // ALWAYS ON TOP

                // 1. Draw Highlight (Blue Shell)
                _renderer.DrawSelectionHighlight(p.Position, p.Size);
                
                // 2. Draw Gizmos (Eixos)
                DrawGizmos(p);

                _gl.Enable(EnableCap.DepthTest);
            }
            
            _renderer.End();

            // --- 4. RESOLVE MSAA -> NORMAL FBO ---
            _gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _msaaFbo);
            _gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _fbo);
            _gl.BlitFramebuffer(0, 0, (int)size.X, (int)size.Y, 0, 0, (int)size.X, (int)size.Y, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);

            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            _gl.Viewport(0, 0, (uint)_window.Size.X, (uint)_window.Size.Y);

            // --- 5. DISPLAY IN IMGUI ---
            // UVs flipped horizontally (standard FBO result)
            ImGui.Image((IntPtr)_fboColor, size, new Vector2(0, 1), new Vector2(1, 0));
            
            _isViewportHovered = ImGui.IsWindowHovered();

            // Status Bar Overlay
            DrawViewportOverlay(size);

            ImGui.End();
        }
        ImGui.PopStyleVar();
    }

    private static void DrawViewportOverlay(Vector2 viewportSize)
    {
        // --- Top Left Stats ---
        ImGui.SetCursorPos(new Vector2(10, 10));
        ImGui.BeginChild("Overlay", new Vector2(250, 100), false, ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoBackground);
        ImGui.TextColored(new Vector4(0.5f, 0.8f, 1.0f, 1.0f), "CATSTUDIO v3.2 OPTIMIZED");
        ImGui.Text($"FPS: {_fps:0}");
        if (_selectedInstance != null)
            ImGui.Text($"Selected: {_selectedInstance.Name}");
        ImGui.EndChild();

        // --- Bottom Toolbar --- 
        float toolbarWidth = 460;
        ImGui.SetCursorPos(new Vector2(viewportSize.X / 2 - toolbarWidth / 2, viewportSize.Y - 60));
        ImGui.BeginChild("Toolbar", new Vector2(toolbarWidth, 50), true, ImGuiWindowFlags.NoScrollbar);
        
        // Play Button
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.6f, 0.2f, 1.0f));
        if (ImGui.Button("PLAY [P]", new Vector2(100, 30))) LaunchPreview();
        ImGui.PopStyleColor();

        ImGui.SameLine();
        ImGui.TextDisabled("|");
        ImGui.SameLine();

        // Move Button
        if (_currentGizmo == GizmoMode.Move) ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.1f, 0.4f, 0.7f, 1.0f));
        if (ImGui.Button("Move [1]", new Vector2(100, 30))) _currentGizmo = GizmoMode.Move;
        if (_currentGizmo == GizmoMode.Move) ImGui.PopStyleColor();
        
        ImGui.SameLine();

        // Scale Button
        if (_currentGizmo == GizmoMode.Scale) ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.1f, 0.4f, 0.7f, 1.0f));
        if (ImGui.Button("Scale [2]", new Vector2(100, 30))) _currentGizmo = GizmoMode.Scale;
        if (_currentGizmo == GizmoMode.Scale) ImGui.PopStyleColor();

        ImGui.SameLine();

        // Rotate Button
        if (_currentGizmo == GizmoMode.Rotate) ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.1f, 0.4f, 0.7f, 1.0f));
        if (ImGui.Button("Rotate [3]", new Vector2(100, 30))) _currentGizmo = GizmoMode.Rotate;
        if (_currentGizmo == GizmoMode.Rotate) ImGui.PopStyleColor();

        ImGui.EndChild();
    }

    private static void SyncPhysicsRecursive(Instance instance)
    {
        if (instance is Part part) part.SyncFromPhysics();
        foreach (var child in instance.GetChildren()) SyncPhysicsRecursive(child);
    }

    private static void RenderInstance(Instance instance)
    {
        if (instance is Part part)
        {
            _renderer.DrawCube(part.Position, part.Size, part.Color);
        }
        foreach (var child in instance.GetChildren())
            RenderInstance(child);
    }

    private static void DrawMainMenuBar()
    {
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("File"))
            {
                if (ImGui.MenuItem("New")) { }
                if (ImGui.MenuItem("Open")) { }
                if (ImGui.MenuItem("Save")) { }
                ImGui.Separator();
                if (ImGui.MenuItem("Exit")) _window.Close();
                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu("Edit"))
            {
                if (ImGui.MenuItem("Undo")) { }
                if (ImGui.MenuItem("Redo")) { }
                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu("Insert"))
            {
                if (ImGui.MenuItem("Part"))
                {
                    var newPart = new Part() { 
                        Name = "Part", 
                        Position = _camPos + new Vector3(0, 0, -10),
                        Size = new Vector3(4, 4, 4),
                        Color = new Vector3(0.5f, 0.5f, 0.5f)
                    };
                    newPart.Parent = _dataModel.Workspace;
                    _selectedInstance = newPart;
                }
                if (ImGui.MenuItem("SpawnLocation"))
                {
                    var sl = new SpawnLocation() {
                        Position = _camPos + new Vector3(0, 0, -10),
                        Size = new Vector3(12, 1, 12)
                    };
                    sl.Parent = _dataModel.Workspace;
                    _selectedInstance = sl;
                }
                ImGui.EndMenu();
            }
            ImGui.EndMainMenuBar();
        }
    }

    private static void DrawExplorer()
    {
        ImGui.SetNextWindowPos(new Vector2(0, 20), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(300, _window.Size.Y - 45), ImGuiCond.Once);
        
        if (ImGui.Begin("Explorer", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse))
        {
            ImGui.InputTextWithHint("##search", "Search...", ref _searchText, 100);
            ImGui.Separator();

            DrawExplorerNode(_dataModel.Workspace);
            DrawExplorerNode(_dataModel.Lighting);
            DrawExplorerNode(_dataModel.CoreGui);
            
            ImGui.End();
        }
    }

    private static void DrawExplorerNode(Instance instance)
    {
        // Simple Search Filter
        bool matches = string.IsNullOrEmpty(_searchText) || instance.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase);
        
        ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;
        if (_selectedInstance == instance) flags |= ImGuiTreeNodeFlags.Selected;
        
        // Count visible children
        var children = instance.GetChildren();
        if (children.Length == 0) flags |= ImGuiTreeNodeFlags.Leaf;

        bool opened = false;
        if (matches || children.Any(c => c.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase)))
        {
            opened = ImGui.TreeNodeEx(instance.Name, flags);
            
            // Interaction
            if (ImGui.IsItemClicked()) _selectedInstance = instance;
            
            // Context Menu
            if (ImGui.BeginPopupContextItem())
            {
                if (ImGui.MenuItem("Duplicate")) DuplicateInstance(instance);
                if (ImGui.MenuItem("Delete")) { instance.Destroy(); _selectedInstance = null; }
                ImGui.EndPopup();
            }

            if (opened)
            {
                foreach (var child in children)
                {
                    DrawExplorerNode(child);
                }
                ImGui.TreePop();
            }
        }
    }

    private static void DuplicateInstance(Instance inst)
    {
        if (inst is Part original)
        {
            var clone = original is SpawnLocation ? new SpawnLocation() : new Part();
            clone.Name = original.Name + " (Clone)";
            clone.Position = original.Position + new Vector3(original.Size.X, 0, 0);
            clone.Size = original.Size;
            clone.Color = original.Color;
            clone.Transparency = original.Transparency;
            clone.Anchored = original.Anchored;
            clone.Physical = original.Physical;
            clone.Parent = original.Parent;
            _selectedInstance = clone;
        }
    }

    private static void DrawProperties()
    {
        ImGui.SetNextWindowPos(new Vector2(_window.Size.X - 300, 20), ImGuiCond.Once);
        ImGui.SetNextWindowSize(new Vector2(300, _window.Size.Y - 45), ImGuiCond.Once);

        if (ImGui.Begin("Properties", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse))
        {
            if (_selectedInstance != null)
            {
                string name = _selectedInstance.Name;
                ImGui.PushItemWidth(-1);
                if (ImGui.InputText("##name", ref name, 100)) _selectedInstance.Name = name;
                ImGui.PopItemWidth();
                ImGui.Separator();
                
                if (_selectedInstance is Part part)
                {
                    if (ImGui.CollapsingHeader("Transform", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        Vector3 pos = part.Position;
                        if (ImGui.DragFloat3("Position", ref pos, 0.1f)) part.Position = pos;
                        
                        Vector3 size = part.Size;
                        if (ImGui.DragFloat3("Size", ref size, 0.1f)) part.Size = size;
                    }

                    if (ImGui.CollapsingHeader("Appearance", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        Vector3 color = part.Color;
                        if (ImGui.ColorEdit3("Color", ref color)) part.Color = color;
                        
                        float trans = part.Transparency;
                        if (ImGui.SliderFloat("Transparency", ref trans, 0, 1)) part.Transparency = trans;
                    }

                    if (ImGui.CollapsingHeader("Physics", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        bool collide = part.CanCollide;
                        if (ImGui.Checkbox("CanCollide", ref collide)) part.CanCollide = collide;
                        
                        bool anchored = part.Anchored;
                        if (ImGui.Checkbox("Anchored", ref anchored)) part.Anchored = anchored;
                    }
                }
            }
            else
            {
                ImGui.TextDisabled("Select an object to see properties");
            }
            
            ImGui.End();
        }
    }

    private static void DrawStatusBar()
    {
        float height = 25;
        ImGui.SetNextWindowPos(new Vector2(0, _window.Size.Y - height));
        ImGui.SetNextWindowSize(new Vector2(_window.Size.X, height));
        
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 2));
        if (ImGui.Begin("StatusBar", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoInputs))
        {
            if (_selectedInstance is Part p)
            {
                ImGui.TextUnformatted($"{_selectedInstance.Name} | Pos: ({p.Position.X:F1}, {p.Position.Y:F1}, {p.Position.Z:F1}) | Size: ({p.Size.X:F1}, {p.Size.Y:F1}, {p.Size.Z:F1})");
            }
            else if (_selectedInstance != null)
            {
                ImGui.TextUnformatted($"{_selectedInstance.Name} ({_selectedInstance.GetType().Name})");
            }
            else
            {
                ImGui.TextUnformatted("Ready");
            }
            
            ImGui.SameLine(ImGui.GetWindowWidth() - 150);
            ImGui.TextUnformatted($"FPS: {_fps:0} | Cam: ({_camPos.X:F0}, {_camPos.Y:F0}, {_camPos.Z:F0})");
            
            ImGui.End();
        }
        ImGui.PopStyleVar();
    }

    private static void OnResize(Silk.NET.Maths.Vector2D<int> size)
    {
        _gl.Viewport(0, 0, (uint)size.X, (uint)size.Y);
        _camera.UpdateAspect(size.X, size.Y);
    }

    private static void OnClosing()
    {
        _imGuiController?.Dispose();
        _renderer?.Dispose();
        
        if (_fbo != 0)
        {
            _gl.DeleteFramebuffer(_fbo);
            _gl.DeleteTexture(_fboColor);
        }

        if (_msaaFbo != 0)
        {
            _gl.DeleteFramebuffer(_msaaFbo);
            _gl.DeleteRenderbuffer(_msaaColor);
            _gl.DeleteRenderbuffer(_msaaDepth);
        }
        
        _gl?.Dispose();
    }

    private static void DarkTheme()
    {
        var style = ImGui.GetStyle();
        style.Colors[(int)ImGuiCol.Text]                   = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
        style.Colors[(int)ImGuiCol.WindowBg]               = new Vector4(0.12f, 0.12f, 0.12f, 0.94f);
        style.Colors[(int)ImGuiCol.Header]                 = new Vector4(0.25f, 0.25f, 0.25f, 1.00f);
        style.Colors[(int)ImGuiCol.HeaderHovered]          = new Vector4(0.35f, 0.35f, 0.35f, 1.00f);
        style.Colors[(int)ImGuiCol.HeaderActive]           = new Vector4(0.10f, 0.40f, 0.75f, 1.00f);
        style.Colors[(int)ImGuiCol.Button]                 = new Vector4(0.20f, 0.20f, 0.20f, 1.00f);
        style.Colors[(int)ImGuiCol.ButtonHovered]          = new Vector4(0.30f, 0.30f, 0.30f, 1.00f);
        style.Colors[(int)ImGuiCol.ButtonActive]           = new Vector4(0.10f, 0.40f, 0.75f, 1.00f);
        style.Colors[(int)ImGuiCol.TitleBg]                = new Vector4(0.08f, 0.08f, 0.08f, 1.00f);
        style.Colors[(int)ImGuiCol.TitleBgActive]          = new Vector4(0.08f, 0.08f, 0.08f, 1.00f);
        style.Colors[(int)ImGuiCol.FrameBg]                = new Vector4(0.15f, 0.15f, 0.15f, 1.00f);
        style.Colors[(int)ImGuiCol.FrameBgHovered]         = new Vector4(0.20f, 0.20f, 0.20f, 1.00f);
        style.Colors[(int)ImGuiCol.FrameBgActive]          = new Vector4(0.25f, 0.25f, 0.25f, 1.00f);
        style.Colors[(int)ImGuiCol.CheckMark]              = new Vector4(0.10f, 0.40f, 0.75f, 1.00f);
        style.Colors[(int)ImGuiCol.SliderGrab]             = new Vector4(0.10f, 0.40f, 0.75f, 1.00f);
        style.Colors[(int)ImGuiCol.SliderGrabActive]       = new Vector4(0.10f, 0.40f, 0.85f, 1.00f);
        
        style.WindowRounding = 4.0f;
        style.FrameRounding = 3.0f;
        style.ScrollbarRounding = 9.0f;
        style.GrabRounding = 3.0f;
    }
}
