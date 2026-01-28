using System.Numerics;

namespace CatCube.Entities;

/// <summary>
/// Represents a single part of the player's body (head, torso, arm, leg)
/// </summary>
public class PlayerPart
{
    public string Name { get; }
    public Vector3 Offset { get; } // Position relative to parent pivot
    public Vector3 Size { get; }
    public Vector3 Color { get; }
    public Vector3 PivotOffset { get; } // Offset from pivot to mesh center (e.g. Center of arm is (0, -0.5, 0) relative to shoulder)

    public PlayerPart(string name, Vector3 offset, Vector3 size, Vector3 color, Vector3 pivotOffset)
    {
        Name = name;
        Offset = offset;
        Size = size;
        Color = color;
        PivotOffset = pivotOffset;
    }
}
