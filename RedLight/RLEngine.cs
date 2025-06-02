using System;
using RedLight.Core;
using RedLight.Graphics;
using RedLight.Input;
using RedLight.Scene;
using RedLight.Utils;
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
    public SceneManager SceneManager { private get; set; }
    private IInputContext input;
    private bool coconutBool = false;
    
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
            
            input = Window.Window.CreateInput();
            Log.Debug("Input context created");
            
            if (startingScene != null)
            {
                SceneManager.SwitchScene(startingScene);
                SubscribeToKeyboard(startingScene as RLKeyboard);
            }
            
            startingScene.TextureManager.TryAdd(
                "no-texture",
                new RLTexture(Graphics, RLConstants.RL_NO_TEXTURE)
            );
        };
        Window.Window.FramebufferResize += OnFramebufferResize;
    }
    
    private void OnFramebufferResize(Vector2D<int> newSize)
    {
        Graphics.OpenGL.Viewport(newSize);
    }

    internal void SubscribeToKeyboard(RLKeyboard keyboardManager)
    {
        if (Keyboard == keyboardManager || input == null)
            return;

        foreach (var kb in input.Keyboards)
            kb.KeyDown += keyboardManager.OnKeyDown;

        Keyboard = keyboardManager;
        Log.Debug("Subscribed to keyboard");
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
        if (Keyboard != keyboardManager || input == null)
            return;

        foreach (var kb in input.Keyboards)
            kb.KeyDown -= keyboardManager.OnKeyDown;

        Keyboard = null;
        Log.Debug("Unsubscribed from keyboard");
    }

    public void Run()
    {
        Window.Window.Run();
    }
}