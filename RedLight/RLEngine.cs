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
/// The entry point to the RedLight Game Engine. TODO: Create better docs
/// </summary>
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
    
    /// <summary>
    /// Log strength used by Serilog. By default, the log strength is 0 (normal) however
    /// you are able to change it with program arguments.
    /// 
    /// 0 = Normal
    /// 1 = Debug
    /// 2 = Verbose
    /// Default = Normal
    /// </summary>
    private int logStrength;

    private Vector2D<int> windowedSize;
    private Vector2D<int> windowedPosition;
    private bool isFullscreen;

    public string title = "";

    public RLEngine(int width, int height, string title, RLScene startingScene, string[] args)
    {
        ParseArguments(args);
        InitialiseLogger(logStrength);

        WindowOptions options = WindowOptions.Default;
        options.Title = title;
        this.title = title;

        windowedSize = new Vector2D<int>(width, height);

        if (isFullscreen)
        {
            options.WindowState = WindowState.Fullscreen;
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

            var input = SceneManager.Instance.input.CreateInput();
            Log.Debug("Input context created");
            
            SetupFullscreen();
            
            if (startingScene != null)
            {
                Log.Debug("Starting scene initialisation: {Scene}", startingScene.GetType);

                if (startingScene.PhysicsSystem == null)
                {
                    Log.Debug("Creating initial physics system for starting scene");
                    startingScene.PhysicsSystem = new PhysicsSystem();
                }
                
                Log.Debug("Scene is switching to {A}", startingScene);
                SceneManager.Instance.SwitchScene(startingScene);
            }
            else
            {
                Log.Error("Starting scene is null");
            }
        };
        
        Window.Window.FramebufferResize += OnFramebufferResize;
        Window.Window.StateChanged += OnWindowStateChanged;

        CreateSceneManager();
    }

    public void AddScene(string id, RLScene scene)
    {
        SceneManager.Instance.Add(id, scene);
    }

    public void AddScene(string id, RLScene scene, RLKeyboard keyboard, RLMouse mouse)
    {
        SceneManager.Instance.Add(id, scene);
    }

    private void OnWindowStateChanged(WindowState newState)
    {
        Log.Debug("Window state changed to: {State}", newState);
        
        // Update isFullscreen based on the new window state
        isFullscreen = newState == WindowState.Fullscreen;
        
        // Store windowed position and size when switching from fullscreen to windowed
        if (!isFullscreen && (newState == WindowState.Normal || newState == WindowState.Maximized))
        {
            windowedPosition = Window.Window.Position;
            if (newState == WindowState.Normal)
            {
                windowedSize = Window.Window.Size;
            }
        }
    }
    private void SetupFullscreen()
    {
        Log.Debug("SetupFullscreen called, isFullscreen: {IsFullscreen}, WindowState: {State}", 
            isFullscreen, Window.Window.WindowState);
          
        if (isFullscreen)
        {
            switch (Window.Window.WindowState)
            {
                case WindowState.Normal:
                case WindowState.Maximized:
                    // Store current windowed state before going fullscreen
                    if (Window.Window.WindowState == WindowState.Normal)
                    {
                        windowedSize = Window.Window.Size;
                    }
                    windowedPosition = Window.Window.Position;
                    Log.Debug("Switching to fullscreen from {State}", Window.Window.WindowState);
                    Window.Window.WindowState = WindowState.Fullscreen;
                    break;
                case WindowState.Minimized:
                    // Don't change to fullscreen from minimized
                    Log.Debug("Not switching to fullscreen from minimized state");
                    break;
                case WindowState.Fullscreen:
                    // Already fullscreen, but ensure viewport is correct
                    Log.Debug("Already in fullscreen mode");
                    // Force viewport update for fullscreen
                    Graphics.OpenGL.Viewport(Window.Window.FramebufferSize);
                    Log.Debug("Forced viewport update to fullscreen size: {Width}x{Height}", 
                        Window.Window.FramebufferSize.X, Window.Window.FramebufferSize.Y);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
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
        }
    }

    private void OnFramebufferResize(Vector2D<int> newSize)
    {
        Graphics.OpenGL.Viewport(newSize);
        Log.Debug("Viewport updated to: {Width}x{Height}", newSize.X, newSize.Y);

        if (!isFullscreen && Window.Window.WindowState == WindowState.Normal)
        {
            windowedSize = newSize;
        }
    }

    public void InitialiseLogger(int value, ConsoleLog console = null)
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

        if (value == 1)
            loggerConfig.MinimumLevel.Debug();
        if (value == 2)
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
    
    public void ToggleFullscreen()
    {
        Log.Debug("Toggling fullscreen. Current state: isFullscreen={IsFullscreen}, WindowState={State}", 
            isFullscreen, Window.Window.WindowState);
    
        if (isFullscreen)
        {
            Log.Debug("Switching to windowed mode. Restoring to size: {Width}x{Height}, position: {X},{Y}", 
                windowedSize.X, windowedSize.Y, windowedPosition.X, windowedPosition.Y);
        
            Window.Window.WindowState = WindowState.Normal;
            Window.Window.Size = windowedSize;
            
            var monitor = Window.Window.Monitor;
            if (monitor != null)
            {
                var monitorSize = monitor.Bounds.Size;
                var centeredPosition = new Vector2D<int>(
                    (monitorSize.X - windowedSize.X) / 2,
                    (monitorSize.Y - windowedSize.Y) / 2
                );
                Window.Window.Position = centeredPosition;
            
                Log.Debug("Switching to windowed mode. Centered at position: {X},{Y} on monitor size: {MonitorWidth}x{MonitorHeight}", 
                    centeredPosition.X, centeredPosition.Y, monitorSize.X, monitorSize.Y);
            }
            else
            {
                // Fallback to stored position if monitor info is unavailable
                Window.Window.Position = windowedPosition;
                Log.Debug("Switching to windowed mode. Using stored position: {X},{Y}", 
                    windowedPosition.X, windowedPosition.Y);
            }
            
            isFullscreen = false;
        }
        else
        {
            windowedSize = Window.Window.Size;
            windowedPosition = Window.Window.Position;
        
            Log.Debug("Switching to fullscreen mode. Stored windowed size: {Width}x{Height}, position: {X},{Y}", 
                windowedSize.X, windowedSize.Y, windowedPosition.X, windowedPosition.Y);
        
            Window.Window.WindowState = WindowState.Fullscreen;
            isFullscreen = true;
        }
    }

    public SceneManager CreateSceneManager()
    {
        var shaderManager = ShaderManager.Instance;
        var textureManager = TextureManager.Instance;
        InputManager.Initialise(Window);
        SceneManager.Initialise(this);
        return SceneManager.Instance;
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