using RedLight.Core;
using Serilog;
using Silk.NET.Input;
using Silk.NET.Windowing;

namespace RedLight.Input;

public class InputManager
{
    public IInputContext input;
    private RLWindow window;

    public Dictionary<string, RLKeyboard> Keyboards { get; set; } = new();
    public Dictionary<string, RLMouse> Mice { get; set; } = new();
    public RLKeyboard Keyboard { get; set; }
    public RLMouse Mouse { get; set; }

    public InputManager(RLWindow window)
    {
        this.window = window;
    }

    public InputManager CreateInput()
    {
        input = window.Window.CreateInput();
        return this;
    }

    public void SubscribeToInputs(RLKeyboard keyboardManager, RLMouse mouseManager)
    {
        if (input == null)
            return;

        if (keyboardManager != null)
        {
            foreach (var kb in input.Keyboards)
            {
                kb.KeyDown += keyboardManager.OnKeyDown;
                kb.KeyUp += keyboardManager.OnKeyUp;
            }
        }

        if (mouseManager != null)
        {
            foreach (var mouse in input.Mice)
            {
                mouse.MouseMove += mouseManager.OnMouseMove;
            }
        }

        Keyboard = keyboardManager;
        Mouse = mouseManager;
        Log.Debug("Subscribed to keyboard");
    }

    public void UnsubscribeFromInputs(RLKeyboard keyboardManager, RLMouse mouseManager)
    {
        if (input == null)
            return;

        if (keyboardManager != null)
        {
            foreach (var kb in input.Keyboards)
            {
                kb.KeyDown += keyboardManager.OnKeyDown;
                kb.KeyUp += keyboardManager.OnKeyUp;
            }
        }

        if (mouseManager != null)
        {
            foreach (var mouse in input.Mice)
            {
                mouse.MouseMove += mouseManager.OnMouseMove;
            }
        }

        Keyboard = null;
        Mouse = null;
        Log.Debug("Unsubscribed from keyboard");
    }
}