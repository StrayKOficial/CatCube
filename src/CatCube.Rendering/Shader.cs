using Silk.NET.OpenGL;
using System.Numerics;

namespace CatCube.Rendering;

public class Shader : IDisposable
{
    private readonly GL _gl;
    private readonly uint _program;
    public uint ProgramHandle => _program;

    public Shader(GL gl, string vertexPath, string fragmentPath)
    {
        _gl = gl;
        
        string vertexSource = File.ReadAllText(vertexPath);
        string fragmentSource = File.ReadAllText(fragmentPath);
        
        uint vertexShader = CompileShader(ShaderType.VertexShader, vertexSource);
        uint fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentSource);
        
        _program = _gl.CreateProgram();
        _gl.AttachShader(_program, vertexShader);
        _gl.AttachShader(_program, fragmentShader);
        _gl.LinkProgram(_program);
        
        _gl.GetProgram(_program, ProgramPropertyARB.LinkStatus, out int status);
        if (status == 0)
        {
            throw new Exception($"Shader link error: {_gl.GetProgramInfoLog(_program)}");
        }
        
        _gl.DeleteShader(vertexShader);
        _gl.DeleteShader(fragmentShader);
    }

    private uint CompileShader(ShaderType type, string source)
    {
        uint shader = _gl.CreateShader(type);
        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);
        
        _gl.GetShader(shader, ShaderParameterName.CompileStatus, out int status);
        if (status == 0)
        {
            throw new Exception($"Shader compile error ({type}): {_gl.GetShaderInfoLog(shader)}");
        }
        
        return shader;
    }

    public void Use()
    {
        _gl.UseProgram(_program);
    }

    public void SetMatrix4(string name, Matrix4x4 value)
    {
        int location = _gl.GetUniformLocation(_program, name);
        if (location >= 0)
        {
            unsafe
            {
                _gl.UniformMatrix4(location, 1, false, (float*)&value);
            }
        }
    }

    public void SetVector3(string name, Vector3 value)
    {
        int location = _gl.GetUniformLocation(_program, name);
        if (location >= 0)
        {
            _gl.Uniform3(location, value.X, value.Y, value.Z);
        }
    }

    public void SetFloat(string name, float value)
    {
        int location = _gl.GetUniformLocation(_program, name);
        if (location >= 0)
        {
            _gl.Uniform1(location, value);
        }
    }

    public void SetInt(string name, int value)
    {
        int location = _gl.GetUniformLocation(_program, name);
        if (location >= 0)
        {
            _gl.Uniform1(location, value);
        }
    }

    public void Dispose()
    {
        _gl.DeleteProgram(_program);
    }
}
