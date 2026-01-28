using LiteNetLib;
using LiteNetLib.Utils;
using System.Numerics;

namespace CatCube.Shared;

public class NetworkConfig
{
    public const int Port = 9050;
    public const string Key = "CatCube_Key";
}

// Packet Types
public enum PacketType : byte
{
    Join,
    PlayerState,
    WorldState,
    PlayerLeft
}

// Data Structures
public enum AnimState : byte { Idle, Walk, Jump, Fall }

public struct PlayerState : INetSerializable
{
    public int Id;
    public string Username;
    public AvatarData Avatar; // Avatar visuals
    public float X, Y, Z;
    public float Rotation;
    public float WalkCycle; // For animation sync
    public AnimState State;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Id);
        writer.Put(Username ?? "Unknown");
        Avatar.Serialize(writer); // Nested serialization
        writer.Put(X);
        writer.Put(Y);
        writer.Put(Z);
        writer.Put(Rotation);
        writer.Put(WalkCycle);
        writer.Put((byte)State);
    }

    public void Deserialize(NetDataReader reader)
    {
        Id = reader.GetInt();
        Username = reader.GetString();
        Avatar = new AvatarData();
        Avatar.Deserialize(reader);
        X = reader.GetFloat();
        Y = reader.GetFloat();
        Z = reader.GetFloat();
        Rotation = reader.GetFloat();
        WalkCycle = reader.GetFloat();
        State = (AnimState)reader.GetByte();
    }
}

public struct AvatarData : INetSerializable
{
    public string ShirtColor;
    public string PantsColor;
    public string SkinColor;
    public int BodyType;
    public int HairStyle;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(ShirtColor ?? "#CC3333");
        writer.Put(PantsColor ?? "#264073");
        writer.Put(SkinColor ?? "#FFD9B8");
        writer.Put(BodyType);
        writer.Put(HairStyle);
    }

    public void Deserialize(NetDataReader reader)
    {
        ShirtColor = reader.GetString();
        PantsColor = reader.GetString();
        SkinColor = reader.GetString();
        BodyType = reader.GetInt();
        HairStyle = reader.GetInt();
    }
}
