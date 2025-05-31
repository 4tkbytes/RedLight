using System;
using RedLight.Core;
using RedLight.Input;
using RedLight.Scene;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace RedLight;

public class RLEngine
{
    public RLWindow Window { get; private set; }
    private RLKeyboard _keyboard;
    
    public RLEngine(int width, int height, string title, RLScene startingScene, RLKeyboard keyboard)
    {
        WindowOptions options = WindowOptions.Default;
        options.Size = new Vector2D<int>(width, height);
        options.Title = title;
        
        _keyboard = keyboard;

        Window = new RLWindow(options, startingScene);

        Window.Window.Load += OnLoad;
    }

    private void OnLoad()
    {
        IInputContext input = Window.Window.CreateInput();
        for (int i = 0; i < input.Keyboards.Count; i++)
            input.Keyboards[i].KeyDown += _keyboard.OnKeyDown;
    }

    public void Run()
    {
        Window.Window.Run();
    }
}