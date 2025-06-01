using System;
using RedLight.Core;
using RedLight.Graphics;
using RedLight.Input;
using RedLight.Scene;
using Serilog;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace RedLight;

public class RLEngine
{
    public RLWindow Window { get; private set; }
    public RLGraphics Graphics { get; private set; }
    public RLKeyboard Keyboard { get; set; }
    private IInputContext input;
    
    public RLEngine(int width, int height, string title, RLScene startingScene)
    {
        WindowOptions options = WindowOptions.Default;
        options.Size = new Vector2D<int>(width, height);
        options.Title = title;
        
        Window = new RLWindow(options, startingScene);
        Log.Debug("Window has been created");
        Graphics = new RLGraphics();
        Log.Debug("Graphics has been initialised");
        
        Window.Window.Load += () =>
        {
            Graphics.OpenGL = Window.Window.CreateOpenGL();
            Log.Information("OpenGL is chosen as the backend");
            
            if (startingScene != null)
            {
                startingScene.Graphics = Graphics;
                startingScene.Engine = this;
                Window.SubscribeToEvents(startingScene);
                startingScene.OnLoad();
            }
            
            SubscribeToKeyboard(startingScene as RLKeyboard);
        };
        Window.Window.FramebufferResize += OnFramebufferResize;
    }
    
    private void OnFramebufferResize(Vector2D<int> newSize)
    {
        Graphics.OpenGL.Viewport(newSize);
    }

    internal void SubscribeToKeyboard(RLKeyboard keyboardManager)
    {
        if (input == null)
             input = Window.Window.CreateInput();
        
        for (int i = 0; i < input.Keyboards.Count; i++)
            input.Keyboards[i].KeyDown += keyboardManager.OnKeyDown;
        
        this.Keyboard = keyboardManager;
        Log.Debug("Subscribed to new keyboard");
    }

    public static void InitialiseLogger()
    {
        Log.Logger = new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Debug()
#endif
            .WriteTo.Console()
            .WriteTo.File("logs/log.txt",
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true)
            .CreateLogger();
        Log.Information("Logger has been created");
    }

    internal void UnsubscribeFromKeyboard(RLKeyboard keyboardManager)
    {
        for (int i = 0; i < input.Keyboards.Count; i++)
            input.Keyboards[i].KeyDown -= keyboardManager.OnKeyDown;
        Log.Debug("Unsubscribed to new keyboard");
    }

    public void Run()
    {
        Window.Window.Run();
    }
}