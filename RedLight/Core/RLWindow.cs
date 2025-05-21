using RedLight.Scene;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using System.Numerics;

namespace RedLight.Core;

public class RLWindow
{
    public IWindow window;
    private RLScene? _rlScene;
    private IMouse? _mouse;
    private IInputContext? _inputContext;
    private bool _cursorCaptured = false;
    
    public RLWindow(WindowOptions options, Scene.RLScene? rlScene = null)
    {
        window = Window.Create(options);
        _rlScene = rlScene;
        
        if (rlScene != null)
        {
            SetScene(rlScene);
        }
        
        // Setup cursor handling
        window.Load += OnLoad;
    }
      private void OnLoad()
    {
        // No need to initialize input context here anymore
        // RLEngine will create the input context and set it via SetInputContext
    }
      public void SetInputContext(IInputContext inputContext)
    {
        _inputContext = inputContext;
        
        if (_inputContext.Mice.Count > 0)
        {
            _mouse = _inputContext.Mice[0];
            Console.WriteLine("Mouse input device initialized successfully");
            
            // If cursor was previously captured, re-apply the setting
            if (_cursorCaptured)
            {
                SetCursorVisible(false);
            }
        }
        else
        {
            Console.WriteLine("Warning: No mouse device found");
        }
    }

    public void SetScene(RLScene rlScene)
    {
        // Unsubscribe previous handlers if there was a previous scene
        if (_rlScene != null)
        {
            window.Load -= _rlScene.OnLoad;
            window.Update -= _rlScene.OnUpdate;
            window.Render -= _rlScene.OnRender;
        }

        // Set new scene
        _rlScene = rlScene;

        // Subscribe new handlers
        window.Load += _rlScene.OnLoad;
        window.Update += _rlScene.OnUpdate;
        window.Render += _rlScene.OnRender;
    }
      public void SetCursorVisible(bool visible)
    {
        if (_mouse == null)
        {
            Console.WriteLine("Cannot set cursor visibility: Mouse is not initialized");
            _cursorCaptured = !visible; // Still track the state even if we can't set it
            return;
        }

        try
        {
            if (_mouse.Cursor != null)
            {
                _mouse.Cursor.CursorMode = visible 
                    ? Silk.NET.Input.CursorMode.Normal 
                    : Silk.NET.Input.CursorMode.Hidden;
                _cursorCaptured = !visible;
                Console.WriteLine($"Cursor visibility set to: {visible}");
            }
            else
            {
                Console.WriteLine("Warning: Mouse cursor is null, cannot set visibility");
                _cursorCaptured = !visible; // Still track the state even if we can't set it
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to set cursor visibility: {ex.Message}");
            _cursorCaptured = !visible; // Still track the state even if the API fails
        }
    }
      public void SetCursorPosition(Vector2 position)
    {
        if (_mouse == null)
        {
            Console.WriteLine("Cannot set cursor position: Mouse is not initialized");
            return;
        }

        try
        {
            _mouse.Position = position;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to set cursor position: {ex.Message}");
        }
    }
    
    public bool IsCursorCaptured => _cursorCaptured;
    
    public Vector2 GetWindowCenter()
    {
        return new Vector2(window.Size.X / 2, window.Size.Y / 2);
    }
    
    public IInputContext? GetInputContext()
    {
        return _inputContext;
    }
}