using System.Numerics;
using MoonSharp.Interpreter;

namespace CatCube.Engine;

[MoonSharpUserData]
public class SpawnLocation : Part
{
    public SpawnLocation()
    {
        Name = "SpawnLocation";
        Color = new Vector3(0.1f, 0.7f, 0.1f); // Classic Green Spawn
        Size = new Vector3(12, 1, 12);
        Anchored = true;
    }
}
