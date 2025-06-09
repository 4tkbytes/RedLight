using RedLight.Core;
using RedLight.Graphics;
using RedLight.Input;
using RedLight.Scene;
using RedLight.UI;
using RedLight.Utils;
using Serilog;
using Silk.NET.Assimp;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System;
using System.Diagnostics;
using ShaderType = RedLight.Graphics.ShaderType;

namespace RedLight;

/// <summary>
/// The entry point to the RedLight Game Engine. 
/// </summary>
/// <remarks>
/// The <see cref="RLEngine"/> class serves as the main entry point for initializing and running the
/// application. It manages the window, graphics backend, and scenes, and provides logging capabilities.
/// Use the <see cref="Run"/> method to start the engine's main loop.
/// </remarks>
/// <example>
/// <code>
/// // Initialise scenes
/// var scene1 = new TestingScene1();
/// // Create engine instance
/// var engine = new RLEngine(1280, 720, "RedLight Game Engine Editor", scene1, args);
/// // Create scene manager
/// var sceneManager = engine.CreateSceneManager();
/// // Add scenes to scene manager
/// sceneManager.Add("loading", loadingScene, loadingScene, loadingScene);
/// sceneManager.Add("testing_scene_1", scene1);
/// // Run
/// engine.Run();
/// </code>
/// </example>
public class RLEngine
{
    /// <summary>
    /// The Silk.NET default window
    /// </summary>
    public RLWindow Window { get; private set; }
    /// <summary>
    /// Wrapper class containing different graphics backend APIs
    /// </summary>
    public RLGraphics Graphics { get; private set; }
    public RLImGui ImGui { get; set; }

    public SceneManager SceneManager { private get; set; }

    /// <summary>
    /// Log strength used by Serilog. By default, the log strength is 0 (normal) however
    /// you are able to change it with program arguments.
    /// 
    /// 0 = Normal
    /// 1 = Debug
    /// 2 = Verbose
    /// Default = Normal
    /// </summary>
    private int logStrength = 0;

    private Vector2D<int> windowedSize;
    private Vector2D<int> windowedPosition;
    private bool isFullscreen = false;
    private bool bug = false;
    private bool isMaximised = false;

    public string title = "";

    public RLEngine(int width, int height, string title, RLScene startingScene, string[] args)
    {
        ParseArguments(args);
        InitialiseLogger();

        WindowOptions options = WindowOptions.Default;
        options.Title = title;
        this.title = title;

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
                Log.Debug("Scene is switching to {A}", startingScene);
            }
            else
            {
                Log.Error("Starting scene is null");
            }
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

    public void InitialiseLogger(ConsoleLog console = null)
    {
        var loggerConfig = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.Debug()
            .WriteTo.File("logs/log.txt",
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: false);
            
        if (console != null)
        {
            loggerConfig.WriteTo.ImGuiConsole(console);
        }

        if (logStrength == 1)
            loggerConfig.MinimumLevel.Debug();
        if (logStrength == 2)
            loggerConfig.MinimumLevel.Verbose();

        Log.Logger = loggerConfig.CreateLogger();
        if (console == null)
        {
            Log.Information("Logger has been created");
            Log.Information("Logger is logging at strength [{A}]", logStrength);
        }
        else
        {
            Log.Information("ImGui Logger Sink has been created");
        }
      
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