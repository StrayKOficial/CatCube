using Silk.NET.OpenGL;
using System.Numerics;

namespace CatCube.Rendering;

public class Renderer : IDisposable
{
    private readonly GL _gl;
    private Shader? _shader;
    private Shader? _shadowShader;
    private Mesh? _cubeMesh;
    private Mesh? _wireframeCubeMesh;
    private Mesh? _gridMesh;
    private int _gridSize;
    
    // Shadow Mapping
    private uint _shadowFbo;
    private uint _shadowDepthMap;
    private const uint ShadowWidth = 2048;
    private const uint ShadowHeight = 2048;
    private Matrix4x4 _lightSpaceMatrix;

    private Matrix4x4 _view;
    private Matrix4x4 _projection;
    private Vector3 _viewPos;

    public Renderer(GL gl)
    {
        _gl = gl;
    }

    public void Initialize()
    {
        // ... (Shader path logic remains same, just loading shadow shader too)
        string? shaderPath = GetShaderPath();
        if (shaderPath == null) throw new Exception("Shaders not found");

        _shader = new Shader(_gl, 
            Path.Combine(shaderPath, "basic.vert"),
            Path.Combine(shaderPath, "basic.frag"));
            
        _shadowShader = new Shader(_gl,
            Path.Combine(shaderPath, "shadow.vert"),
            Path.Combine(shaderPath, "shadow.frag"));

        _cubeMesh = Mesh.CreateCube(_gl);
        _wireframeCubeMesh = Mesh.CreateWireframeCube(_gl);
        _gridMesh = CreateGridMesh(100, 4.0f);
        
        InitializeShadowBuffer();
    }

    private Mesh CreateGridMesh(int size, float spacing)
    {
        _gridSize = size;
        List<float> vertices = new List<float>();
        float length = size * spacing;

        for (int i = -size; i <= size; i++)
        {
            // X lines
            vertices.Add(-length); vertices.Add(0); vertices.Add(i * spacing);
            vertices.Add(length);  vertices.Add(0); vertices.Add(i * spacing);

            // Z lines
            vertices.Add(i * spacing); vertices.Add(0); vertices.Add(-length);
            vertices.Add(i * spacing); vertices.Add(0); vertices.Add(length);
        }

        return Mesh.CreateLines(_gl, vertices.ToArray());
    }

    private string? GetShaderPath()
    {
        string[] possiblePaths = {
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "shaders"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "shaders"),
            Path.Combine(AppContext.BaseDirectory, "..", "shaders"),
            Path.Combine(AppContext.BaseDirectory, "shaders"),
            Path.GetFullPath("../shaders"),
            Path.GetFullPath("shaders"),
        };
        foreach (var path in possiblePaths) if (Directory.Exists(path)) return path;
        return null;
    }

    private unsafe void InitializeShadowBuffer()
    {
        _shadowFbo = _gl.GenFramebuffer();
        _shadowDepthMap = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, _shadowDepthMap);
        _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.DepthComponent, ShadowWidth, ShadowHeight, 0, PixelFormat.DepthComponent, PixelType.Float, null);
        
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);
        float[] borderColor = { 1.0f, 1.0f, 1.0f, 1.0f };
        fixed (float* p = borderColor) { _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, p); }

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _shadowFbo);
        _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, _shadowDepthMap, 0);
        _gl.DrawBuffer(DrawBufferMode.None);
        _gl.ReadBuffer(ReadBufferMode.None);
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void BeginShadowPass(Vector3 lightPos)
    {
        // Shadow pass: Render from light point of view
        _gl.Viewport(0, 0, ShadowWidth, ShadowHeight);
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _shadowFbo);
        _gl.Clear(ClearBufferMask.DepthBufferBit);

        // Light Projection (Orthographic for sun)
        float size = 150.0f;
        Matrix4x4 lightProjection = Matrix4x4.CreateOrthographicOffCenter(-size, size, -size, size, 1.0f, 300.0f);
        Matrix4x4 lightView = Matrix4x4.CreateLookAt(lightPos, Vector3.Zero, Vector3.UnitY);
        _lightSpaceMatrix = lightView * lightProjection;

        _shadowShader!.Use();
        _shadowShader.SetMatrix4("lightSpaceMatrix", _lightSpaceMatrix);
    }

    public void EndShadowPass(int screenWidth, int screenHeight)
    {
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        _gl.Viewport(0, 0, (uint)screenWidth, (uint)screenHeight);
    }

    public void Begin(Matrix4x4 view, Matrix4x4 projection, Vector3 viewPos)
    {
        _view = view;
        _projection = projection;
        _viewPos = viewPos;
        
        _shader!.Use();
        _shader.SetMatrix4("view", view);
        _shader.SetMatrix4("projection", projection);
        _shader.SetVector3("viewPos", viewPos);
        _shader.SetVector3("lightPos", new Vector3(50, 150, 50)); // Fixed Light Pos
        _shader.SetMatrix4("lightSpaceMatrix", _lightSpaceMatrix);
        
        // Bind shadow map to texture unit 1
        _gl.ActiveTexture(TextureUnit.Texture1);
        _gl.BindTexture(TextureTarget.Texture2D, _shadowDepthMap);
        _shader.SetInt("shadowMap", 1);
    }

    public void DrawCube(Vector3 position, Vector3 scale, Vector3 color, Quaternion rotation = default, int renderMode = 0)
    {
        if (rotation == default)
            rotation = Quaternion.Identity;
            
        var model = Matrix4x4.CreateScale(scale) *
                    Matrix4x4.CreateFromQuaternion(rotation) *
                    Matrix4x4.CreateTranslation(position);
        
        // If we are in shadow pass, we might want a different logic or just use active shader
        _gl.GetInteger(GetPName.CurrentProgram, out int activeProgram);
        if (_shadowShader != null && (uint)activeProgram == _shadowShader.ProgramHandle)
        {
            _shadowShader.SetMatrix4("model", model);
        }
        else
        {
            _shader!.SetInt("renderMode", renderMode); // Now using the parameter
            _shader.SetMatrix4("model", model);
            _shader.SetVector3("objectColor", color);
            _shader.SetVector3("objectScale", scale);
        }
        
        _cubeMesh!.Draw();
    }

    public void DrawSelectionHighlight(Vector3 position, Vector3 scale)
    {
        Vector3 highlightScale = scale + new Vector3(0.05f); // Thinner gap
        Vector3 highlightColor = new Vector3(1.0f, 1.0f, 1.0f); // White wireframe (high contrast)
        
        // Use unlit mode (3) 
        _shader!.SetInt("renderMode", 3);
        
        var model = Matrix4x4.CreateScale(highlightScale) *
                    Matrix4x4.CreateTranslation(position);
                    
        _shader.SetMatrix4("model", model);
        _shader.SetVector3("objectColor", highlightColor);
        _shader.SetVector3("objectScale", highlightScale);
        
        _wireframeCubeMesh!.Draw(PrimitiveType.Lines);
        
        // Also draw a very faint transparent shell to give it "volume"
        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        
        Vector3 shellColor = new Vector3(0.4f, 0.7f, 1.0f);
        // We'd need a transparent render mode if frag shader supports it. 
        // For now, I'll just stick to the professional wireframe as it looks cleaner.
        
        _gl.Disable(EnableCap.Blend);
    }

    public void DrawGrid(int size, float spacing, Vector3 color)
    {
        if (_gridMesh == null) return;
        
        _shader!.SetInt("renderMode", 3); // Unlit
        _shader.SetMatrix4("model", Matrix4x4.Identity);
        _shader.SetVector3("objectColor", color);
        _shader.SetVector3("objectScale", Vector3.One);
        
        _gridMesh.Draw(PrimitiveType.Lines);
    }

    public void DrawRect(Vector2 position, Vector2 size, Vector3 color, Vector2 screenResolution)
    {
        // Simple UI Drawing using Orthographic Projection
        Matrix4x4 ortho = Matrix4x4.CreateOrthographicOffCenter(0, screenResolution.X, screenResolution.Y, 0, -1, 1);
        
        Vector3 worldPos = new Vector3(position.X + size.X/2, position.Y + size.Y/2, 0);
        Matrix4x4 model = Matrix4x4.CreateScale(size.X, size.Y, 0.001f) * 
                        Matrix4x4.CreateTranslation(worldPos);
                        
        _shader!.SetInt("renderMode", 1); // 2D UI (No Fog)
        _shader!.SetMatrix4("view", Matrix4x4.Identity);
        _shader.SetMatrix4("projection", ortho);
        _shader.SetMatrix4("model", model);
        _shader.SetVector3("objectColor", color);
        _shader.SetVector3("objectScale", new Vector3(size.X, size.Y, 1.0f));
        
        _cubeMesh!.Draw();
    }

    public void End()
    {
        // Nothing to do for now
    }

    public void Dispose()
    {
        _shader?.Dispose();
        _shadowShader?.Dispose();
        _cubeMesh?.Dispose();
        _gl.DeleteFramebuffer(_shadowFbo);
        _gl.DeleteTexture(_shadowDepthMap);
    }
}
