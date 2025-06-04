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

    public SceneManager SceneManager { private get; set; }

    private int logStrength = 0;
    private bool fullscreen = false;
    
    // Store windowed mode properties for restoration
    private Vector2D<int> windowedSize;
    private Vector2D<int> windowedPosition;
    private bool isFullscreen = false;

    public RLEngine(int width, int height, string title, RLScene startingScene, string[] args)
    {
        ParseArguments(args);
        InitialiseLogger();
        
        WindowOptions options = WindowOptions.Default;
        options.Size = new Vector2D<int>(width, height);
        options.Title = title;
        
        // Store initial windowed size
        windowedSize = new Vector2D<int>(width, height);
        
        if (fullscreen)
        {
            options.WindowState = WindowState.Fullscreen;
            isFullscreen = true;
        }
        else
        {
            options.WindowState = WindowState.Normal;
            isFullscreen = false;
        }

        Window = new RLWindow(options, startingScene);
        Log.Debug("Window has been created");
        Graphics = new RLGraphics();
        Log.Debug("Graphics has been initialised");

        Window.Window.Load += () =>
        {
            Graphics.OpenGL = Window.Window.CreateOpenGL();
            Log.Information("Backend: OpenGL");

            var input = SceneManager.input.CreateInput();
            Log.Debug("Input context created");

            // Set up fullscreen toggle key handler
            SetupFullscreenToggle(input.input);

            if (startingScene != null)
            {
                SceneManager.SwitchScene(startingScene);
                input.SubscribeToInputs(startingScene as RLKeyboard, startingScene as RLMouse);
            }

            startingScene.TextureManager.TryAdd(
                "no-texture",
                new RLTexture(Graphics, RLFiles.GetEmbeddedResourcePath(RLConstants.RL_NO_TEXTURE_PATH), RLTextureType.Diffuse)
            );
        };
        
        Window.Window.FramebufferResize += OnFramebufferResize;
        Window.Window.StateChanged += OnWindowStateChanged;
    }

    private void SetupFullscreenToggle(IInputContext input)
    {
        foreach (var keyboard in input.Keyboards)
        {
            keyboard.KeyDown += (keyboard, key, scancode) =>
            {
                // F11 or Alt+Enter to toggle fullscreen
                if (key == Key.F11 || (key == Key.Enter && keyboard.IsKeyPressed(Key.AltLeft)))
                {
                    ToggleFullscreen();
                }
            };
        }
    }

    public void ToggleFullscreen()
    {
        if (isFullscreen)
        {
            // Switch to windowed mode
            Window.Window.WindowState = WindowState.Normal;
            Window.Window.Size = windowedSize;
            if (windowedPosition.X != 0 || windowedPosition.Y != 0)
            {
                Window.Window.Position = windowedPosition;
            }
            isFullscreen = false;
            Log.Information("Switched to windowed mode");
        }
        else
        {
            // Store current windowed properties
            if (Window.Window.WindowState == WindowState.Normal)
            {
                windowedSize = Window.Window.Size;
                windowedPosition = Window.Window.Position;
            }
            
            // Switch to fullscreen
            Window.Window.WindowState = WindowState.Fullscreen;
            isFullscreen = true;
            Log.Information("Switched to fullscreen mode");
        }
    }

    private void OnWindowStateChanged(WindowState newState)
    {
        // Update our internal state when window state changes externally
        isFullscreen = (newState == WindowState.Fullscreen);
        Log.Debug("Window state changed to: {State}", newState);
    }

    private void ParseArguments(string[] args)
    {
        foreach (var arg in args)
        {
            if (arg.StartsWith("--Log="))
            {
                var parts = arg.Split('=', 2);
                if (parts.Length == 2 && int.TryParse(parts[1], out var level))
                {
                    logStrength = level;
                }
            }
            else if (arg.Equals("--Fullscreen", StringComparison.OrdinalIgnoreCase))
            {
                fullscreen = true;
            }
        }
    }

    private void OnFramebufferResize(Vector2D<int> newSize)
    {
        Graphics.OpenGL.Viewport(newSize);
        
        // If we're in windowed mode, update our stored windowed size
        if (!isFullscreen && Window.Window.WindowState == WindowState.Normal)
        {
            windowedSize = newSize;
        }
    }

    public void InitialiseLogger()
    {
        var loggerConfig = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/log.txt",
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: false);
                
        if (logStrength == 1)
            loggerConfig.MinimumLevel.Debug();
        if (logStrength == 2)
            loggerConfig.MinimumLevel.Verbose();

        Log.Logger = loggerConfig.CreateLogger();
        Log.Information("Logger has been created");
        Log.Information("Logger is logging at strength [{A}]", logStrength);
    }

    public void Run()
    {
        Window.Window.Run();
        Log.Information("Exiting RedLight Engine now");
    }
}