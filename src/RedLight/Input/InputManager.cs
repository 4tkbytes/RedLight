using RedLight.Core;
using Serilog;
using Silk.NET.Input;

namespace RedLight.Input;

public class InputManager
{
    private static InputManager _instance;
    public static InputManager Instance
    {
        get
        {
            if (_instance == null)
                throw new InvalidOperationException("InputManager is not initialized. Do InputManager.Initialise() first.");
            return _instance;
        }
    }

    public static void Initialise(RLWindow window)
    {
        if (_instance != null)
            throw new InvalidOperationException("InputManager is already initialized.");
        _instance = new InputManager(window);
    }

    public IInputContext Context;
    private RLWindow window;
    public bool isCaptured { get; set; } = true;
    private bool togglePressed = true;
    private bool fullscreenTogglePressed = false;

    public Dictionary<string, RLKeyboard> Keyboards { get; set; } = new();
    public Dictionary<string, RLMouse> Mice { get; set; } = new();
    public RLKeyboard Keyboard { get; set; }
    public RLMouse Mouse { get; set; }

    private InputManager(RLWindow window)
    {
        this.window = window;
    }

    public InputManager CreateInput()
    {
        Context = window.Window.CreateInput();
        return this;
    }

    public void SubscribeToInputs(RLKeyboard keyboardManager, RLMouse mouseManager)
    {
        if (Context == null)
            return;

        if (keyboardManager != null)
        {
            foreach (var kb in Context.Keyboards)
            {
                kb.KeyDown += (keyboard, key, arg3) =>
                {
                    if (keyboardManager.PressedKeys == null)
                        keyboardManager.PressedKeys = new HashSet<Key>();

                    ChangeFullscreenToggle(key);
                };
                kb.KeyDown += keyboardManager.OnKeyDown;

                kb.KeyUp += (keyboard, key, arg3) =>
                {
                    keyboardManager.PressedKeys.Remove(key);

                    ChangeFullscreenToggleReset(key);
                };
                kb.KeyUp += keyboardManager.OnKeyUp;
            }
        }
        else
        {
            Log.Error("Keyboard Manager is null");
        }

        if (mouseManager != null)
        {
            foreach (var mouse in Context.Mice)
            {
                mouse.MouseMove += (mouse1, vector2) =>
                {
                    IsCaptured(mouse);
                };
                mouse.MouseMove += mouseManager.OnMouseMove;
            }
        }
        else
        {
            Log.Error("Mouse Manager is null");
        }

        Keyboard = keyboardManager;
        Mouse = mouseManager;
        Log.Debug("Subscribed to keyboard (InputManager)");
    }

    public void UnsubscribeFromInputs(RLKeyboard keyboardManager, RLMouse mouseManager)
    {
        if (Context == null)
            return;

        if (keyboardManager != null)
        {
            foreach (var kb in Context.Keyboards)
            {
                kb.KeyDown -= keyboardManager.OnKeyDown;
                kb.KeyUp -= keyboardManager.OnKeyUp;
            }
        }

        if (mouseManager != null)
        {
            foreach (var mouse in Context.Mice)
            {
                mouse.MouseMove -= mouseManager.OnMouseMove;
            }
        }

        Keyboard = null;
        Mouse = null;
        Log.Debug("Unsubscribed from keyboard");
    }

    public void ChangeCaptureToggle(Key key)
    {
        if (key == Key.F1)
        {
            isCaptured = !isCaptured;

            foreach (var mouse in Context.Mice)
            {
                IsCaptured(mouse, isCaptured);
            }

            Log.Debug("Changing mouse capture mode [{A}]", isCaptured);
        }
    }

    /// <summary>
    /// Checks if a mouse is captured and changes the cursor mode.
    ///
    /// If the mouse is captured, it will change it to CursorMode.Disabled. If it
    /// is not disabled, it will change it to CursorMode.Normal.  
    /// </summary>
    /// <param name="mouse">IMouse</param>
    /// <param name="isCaptured">bool</param>
    public void IsCaptured(IMouse mouse, bool isCaptured)
    {
        if (!isCaptured)
            mouse.Cursor.CursorMode = CursorMode.Normal;

        if (isCaptured)
            mouse.Cursor.CursorMode = CursorMode.Disabled;
    }

    public void IsCaptured(IMouse mouse)
    {
        if (!isCaptured)
            mouse.Cursor.CursorMode = CursorMode.Normal;

        if (isCaptured)
            mouse.Cursor.CursorMode = CursorMode.Disabled;
    }

    public void ChangeCaptureToggleReset(Key key)
    {
        if (key == Key.F1)
        {
            togglePressed = false;
        }
    }

    public void ChangeFullscreenToggle(Key key)
    {
        if (key == Key.F11 && !fullscreenTogglePressed)
        {
            fullscreenTogglePressed = true;

            // Get the engine instance through SceneManager
            var engine = Scene.SceneManager.Instance.GetCurrentScene()?.Engine;
            if (engine != null)
            {
                engine.ToggleFullscreen();
                Log.Debug("Toggled fullscreen via F11");
            }
        }
    }

    public void ChangeFullscreenToggleReset(Key key)
    {
        if (key == Key.F11)
        {
            fullscreenTogglePressed = false;
        }
    }
}