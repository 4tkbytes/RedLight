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

    private Vector2D<int> windowedSize;
    private Vector2D<int> windowedPosition;
    private bool isFullscreen = false;
    private bool bug = false;
    private bool isMaximised = false;

    public RLEngine(int width, int height, string title, RLScene startingScene, string[] args)
    {
        ParseArguments(args);
        InitialiseLogger();

        WindowOptions options = WindowOptions.Default;
        options.Title = title;

        windowedSize = new Vector2D<int>(width, height);

        if (isFullscreen)
        {
            options.WindowState = WindowState.Fullscreen;
            isFullscreen = true;
        }
        else
        {
            options.Size = new Vector2D<int>(width, height);
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

            SetupFullscreenToggle(input.input);

            if (startingScene != null)
            {
                SceneManager.SwitchScene(startingScene);
                input.SubscribeToInputs(startingScene as RLKeyboard, startingScene as RLMouse);
            }

            startingScene.TextureManager.TryAdd(
                "no-texture",
                new RLTexture(Graphics, RLFiles.GetResourcePath(RLConstants.RL_NO_TEXTURE_PATH))
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
                if (keyboard.IsKeyPressed(Key.AltLeft) && keyboard.IsKeyPressed(Key.Enter))
                {
                    if (bug)
                    {
                        switch (Window.Window.WindowState)
                        {
                            case WindowState.Normal:
                                Window.Window.WindowState = WindowState.Fullscreen;
                                Log.Debug("Window state set to {A}", WindowState.Fullscreen.ToString());

                                break;
                            case WindowState.Minimized:
                                break;
                            case WindowState.Maximized:
                                Window.Window.WindowState = WindowState.Fullscreen;
                                Log.Debug("Window state set to {A}", WindowState.Fullscreen.ToString());
                                break;
                            case WindowState.Fullscreen:
                                Window.Window.WindowState = WindowState.Normal;
                                Log.Debug("Window state set to {A}", WindowState.Normal.ToString());
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    else
                    {
                        switch (Window.Window.WindowState)
                        {
                            case WindowState.Normal:
                                Window.Window.WindowState = WindowState.Fullscreen;
                                Log.Debug("Window state set to {A}", WindowState.Fullscreen.ToString());
                                break;
                            case WindowState.Minimized:
                                break;
                            case WindowState.Maximized:
                                Window.Window.WindowState = WindowState.Normal;
                                Log.Debug("Window state set to {A}", WindowState.Normal.ToString());

                                Window.Window.WindowState = WindowState.Fullscreen;
                                Log.Debug("Window state set to {A}", WindowState.Fullscreen.ToString());

                                break;
                            case WindowState.Fullscreen:
                                Window.Window.WindowState = WindowState.Normal;
                                Log.Debug("Window state set to {A}", WindowState.Normal.ToString());
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
            };
        }
    }

    private void OnWindowStateChanged(WindowState newState)
    {
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
                isFullscreen = true;
            }
            else if (arg.Equals("--Maximised", StringComparison.OrdinalIgnoreCase))
            {
                isMaximised = true;
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
            .WriteTo.Debug()
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

    public SceneManager CreateSceneManager()
    {
        var shaderManager = new ShaderManager();
        var textureManager = new TextureManager();
        var sceneManager = new SceneManager(this, shaderManager, textureManager);
        SceneManager = sceneManager;
        return SceneManager;
    }

    public void Run()
    {
        try
        {
            Window.Window.Run();
        }
        catch (Exception e)
        {
            Log.Error("An error occurred while running App: {A}", e);
            throw;
        }
        finally
        {
            Log.Information("Exiting RedLight Engine now");
        }
    }
}