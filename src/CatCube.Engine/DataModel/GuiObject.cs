using System.Numerics;
using MoonSharp.Interpreter;

namespace CatCube.Engine;

[MoonSharpUserData]
public abstract class GuiObject : Instance
{
    public Vector2 Position { get; set; } = Vector2.Zero;
    public Vector2 Size { get; set; } = new Vector2(100, 100);
    public Vector3 Color { get; set; } = Vector3.One;
    public float Transparency { get; set; } = 0.0f;
    public bool Visible { get; set; } = true;

    // TODO: ZIndex, AnchorPoint, etc.
}

[MoonSharpUserData]
public class Frame : GuiObject
{
    public Frame()
    {
        Name = "Frame";
    }
}
