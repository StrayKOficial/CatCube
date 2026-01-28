using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Input;
using CatCube.Shared;

namespace CatCube;

public class Program
{
    private static IWindow? _window;
    private static GL? _gl;
    private static Game? _game;

    public static void Main(string[] args)
    {
        // Parse args
        string ip = "127.0.0.1";
        int port = 9050; // Initialize port
        string map = "Crossroads";
        string username = "Player";
        AvatarData avatar = new AvatarData { ShirtColor="#CC3333", PantsColor="#264073", SkinColor="#FFD9B8" };
        
        for(int i=0; i<args.Length; i++)
        {
            if (args[i] == "--ip" && i + 1 < args.Length)
                ip = args[i+1];
            if (args[i] == "--port" && i + 1 < args.Length)
                int.TryParse(args[i+1], out port);
            if ((args[i] == "--map" || args[i] == "--preview") && i + 1 < args.Length)
                map = args[i+1];
            if (args[i] == "--username" && i + 1 < args.Length)
                username = args[i+1];
            
            // Avatar Args
            if (args[i] == "--shirt" && i + 1 < args.Length) avatar.ShirtColor = args[i+1];
            if (args[i] == "--pants" && i + 1 < args.Length) avatar.PantsColor = args[i+1];
            if (args[i] == "--skin" && i + 1 < args.Length) avatar.SkinColor = args[i+1];
            if (args[i] == "--body" && i + 1 < args.Length && int.TryParse(args[i+1], out int bt)) avatar.BodyType = bt;
            if (args[i] == "--hair" && i + 1 < args.Length && int.TryParse(args[i+1], out int hs)) avatar.HairStyle = hs;
        }
        
        var options = WindowOptions.Default;
        options.Size = new Vector2D<int>(1280, 720);
        options.Title = $"CatCube - Client ({map})";
        options.VSync = true;

        _window = Window.Create(options);
        _window.Load += () => OnLoad(ip, port, map, username, avatar);
        _window.Update += OnUpdate;
        _window.Render += OnRender;
        _window.Closing += OnClose;
        _window.Resize += OnResize;

        _window.Run();
    }

    private static void OnLoad(string ip, int port, string map, string username, AvatarData avatar)
    {
        _gl = GL.GetApi(_window!);
        var input = _window!.CreateInput();
        
        _game = new Game(_gl, input, _window!, ip, port, map, username, avatar);
        _game.Initialize();
        
        // Lock mouse to window
        foreach (var mouse in input.Mice)
        {
            mouse.Cursor.CursorMode = CursorMode.Raw;
        }
    }

    private static void OnUpdate(double deltaTime)
    {
        _game?.Update((float)deltaTime);
    }

    private static void OnRender(double deltaTime)
    {
        _game?.Render();
    }

    private static void OnClose()
    {
        _game?.Dispose();
    }

    private static void OnResize(Vector2D<int> size)
    {
        _gl?.Viewport(size);
        _game?.OnResize(size.X, size.Y);
    }
}
