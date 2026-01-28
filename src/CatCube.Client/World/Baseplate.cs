using Silk.NET.OpenGL;
using System.Numerics;
using CatCube.Rendering;

namespace CatCube.World;

public class Baseplate : IDisposable
{
    private readonly GL _gl;
    
    private readonly Vector3 _position = new Vector3(0, -0.5f, 0);
    private readonly Vector3 _size = new Vector3(100f, 1f, 100f);
    private readonly Vector3 _color = new Vector3(0.63f, 0.64f, 0.63f); // Roblox classic grey

    public Baseplate(GL gl)
    {
        _gl = gl;
    }

    public void Render(Renderer renderer)
    {
        renderer.DrawCube(_position, _size, _color);
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}
