using Silk.NET.Input;
using System.Numerics;

namespace CatCube.Input;

public class InputManager
{
    private readonly IInputContext _input;
    
    public Vector2 Movement { get; private set; }
    public Vector2 MouseDelta { get; private set; }
    public Vector2 MousePosition { get; private set; }
    public float ScrollDelta { get; private set; }
    public bool Jump { get; private set; }
    public bool IsEscapePressed { get; private set; }
    
    private Vector2 _lastMousePos;
    private bool _firstMouse = true;

    private readonly Dictionary<MouseButton, bool> _lastMouseButtons = new();
    private readonly Dictionary<MouseButton, bool> _currentMouseButtons = new();
    private readonly Dictionary<Key, bool> _lastKeys = new();
    private readonly Dictionary<Key, bool> _currentKeys = new();

    public InputManager(IInputContext input)
    {
        _input = input;
        
        // Setup scroll callback
        foreach (var mouse in input.Mice)
        {
            mouse.Scroll += OnScroll;
        }
    }

    private float _scrollAccumulator = 0f;

    private void OnScroll(IMouse mouse, ScrollWheel scroll)
    {
        _scrollAccumulator += scroll.Y;
    }

    public bool IsMouseButtonPressed(MouseButton button) => _currentMouseButtons.GetValueOrDefault(button);
    public bool IsMouseButtonJustPressed(MouseButton button) => _currentMouseButtons.GetValueOrDefault(button) && !_lastMouseButtons.GetValueOrDefault(button);

    public bool IsKeyPressed(Key key) => _currentKeys.GetValueOrDefault(key);
    public bool IsKeyJustPressed(Key key) => _currentKeys.GetValueOrDefault(key) && !_lastKeys.GetValueOrDefault(key);

    public void Update()
    {
        // 1. Sync Last State
        SyncState(_currentMouseButtons, _lastMouseButtons);
        SyncState(_currentKeys, _lastKeys);

        // 2. Poll Current State
        _currentMouseButtons.Clear();
        foreach (var mouse in _input.Mice)
        {
            foreach (MouseButton btn in Enum.GetValues<MouseButton>())
                if (mouse.IsButtonPressed(btn)) _currentMouseButtons[btn] = true;
        }

        _currentKeys.Clear();
        foreach (var kbd in _input.Keyboards)
        {
            foreach (Key key in Enum.GetValues<Key>())
                if (kbd.IsKeyPressed(key)) _currentKeys[key] = true;
        }

        // --- Rest of reset logic ---
        Movement = Vector2.Zero;
        MouseDelta = Vector2.Zero;
        MousePosition = Vector2.Zero;
        ScrollDelta = _scrollAccumulator;
        _scrollAccumulator = 0f;
        Jump = IsKeyPressed(Key.Space);
        IsEscapePressed = IsKeyPressed(Key.Escape);
        
        bool isRmbHeld = IsMouseButtonPressed(MouseButton.Right);

        // Movement polling
        Vector2 move = Vector2.Zero;
        if (IsKeyPressed(Key.W) || IsKeyPressed(Key.Up)) move.Y += 1f;
        if (IsKeyPressed(Key.S) || IsKeyPressed(Key.Down)) move.Y -= 1f;
        if (IsKeyPressed(Key.A) || IsKeyPressed(Key.Left)) move.X -= 1f;
        if (IsKeyPressed(Key.D) || IsKeyPressed(Key.Right)) move.X += 1f;
        if (move != Vector2.Zero) move = Vector2.Normalize(move);
        Movement = move;
        
        // Mouse/Delta polling
        foreach (var mouse in _input.Mice)
        {
            Vector2 currentPos = new Vector2(mouse.Position.X, mouse.Position.Y);
            if (_firstMouse) { _lastMousePos = currentPos; _firstMouse = false; }

            MousePosition = currentPos;
            
            // Calculate delta ALWAYS if cursor is at same Mode (to avoid jumps on mode swap)
            bool isModeTransition = false;

            if (isRmbHeld) 
            {
                if (mouse.Cursor.CursorMode != CursorMode.Disabled)
                {
                    mouse.Cursor.CursorMode = CursorMode.Disabled;
                    _lastMousePos = currentPos; // Reset last pos on transition
                    isModeTransition = true;
                }
            }
            else 
            {
                if (mouse.Cursor.CursorMode != CursorMode.Normal)
                {
                    mouse.Cursor.CursorMode = CursorMode.Normal;
                    _lastMousePos = currentPos; // Reset last pos on transition
                    isModeTransition = true;
                }
            }

            if (!isModeTransition)
            {
                MouseDelta = currentPos - _lastMousePos;
            }
            else
            {
                MouseDelta = Vector2.Zero;
            }
            
            _lastMousePos = currentPos;
        }
    }

    private void SyncState<T>(Dictionary<T, bool> source, Dictionary<T, bool> target) where T : notnull
    {
        target.Clear();
        foreach (var kvp in source) target[kvp.Key] = kvp.Value;
    }
}
