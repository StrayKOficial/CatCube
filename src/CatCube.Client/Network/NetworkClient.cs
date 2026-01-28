using LiteNetLib;
using LiteNetLib.Utils;
using CatCube.Shared;
using System.Net;

namespace CatCube.Network;

public class NetworkClient : IDisposable
{
    private EventBasedNetListener _listener;
    private NetManager _client;
    private NetPeer? _serverPeer;
    private NetDataWriter _writer;
    
    // Callbacks
    public Action<int>? OnConnected;
    public Action<int, PlayerState>? OnPlayerStateReceived;
    public Action<int>? OnPlayerLeft;
    public Action? OnDisconnected;

    public NetworkClient()
    {
        _listener = new EventBasedNetListener();
        _client = new NetManager(_listener);
        _writer = new NetDataWriter();
        
        // Corrected signature: 4 arguments
        _listener.NetworkReceiveEvent += (peer, reader, channel, deliveryMethod) =>
        {
            PacketType type = (PacketType)reader.GetByte();
            
            switch (type)
            {
                case PacketType.WorldState:
                    int count = reader.GetInt();
                    for(int i=0; i<count; i++)
                    {
                        PlayerState state = new PlayerState();
                        state.Deserialize(reader);
                        OnPlayerStateReceived?.Invoke(state.Id, state);
                    }
                    break;
                    
                case PacketType.PlayerState:
                    PlayerState pState = new PlayerState();
                    pState.Deserialize(reader);
                    OnPlayerStateReceived?.Invoke(pState.Id, pState);
                    break;
                    
                case PacketType.PlayerLeft:
                    int leftId = reader.GetInt();
                    OnPlayerLeft?.Invoke(leftId);
                    break;
            }
        };
        
        _listener.PeerConnectedEvent += peer =>
        {
            Console.WriteLine("Connected to server!");
            _serverPeer = peer;
            OnConnected?.Invoke(peer.EndPoint.Port); // Use port as local ID
        };
        
        _listener.PeerDisconnectedEvent += (peer, info) =>
        {
            Console.WriteLine("Disconnected from server");
            _serverPeer = null;
            OnDisconnected?.Invoke();
        };
        
        _client.Start();
    }

    public void Connect(string ip, int port, string username)
    {
        Console.WriteLine($"Connecting to {ip}:{port} as {username}...");
        
        NetDataWriter connectData = new NetDataWriter();
        connectData.Put(username);
        
        _client.Connect(ip, port, connectData);
    }

    public void SendState(PlayerState state)
    {
        if (_serverPeer == null) return;
        
        _writer.Reset();
        _writer.Put((byte)PacketType.PlayerState);
        state.Serialize(_writer);
        
        _serverPeer.Send(_writer, DeliveryMethod.Unreliable); // Position updates can be unreliable (fast)
    }

    public void PollEvents()
    {
        _client.PollEvents();
    }

    public void Dispose()
    {
        _client.Stop();
    }
}
