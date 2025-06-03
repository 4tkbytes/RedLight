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

    public RLEngine(int width, int height, string title, RLScene startingScene, string[] args)
    {
        ParseArguments(args);
        InitialiseLogger();
        WindowOptions options = WindowOptions.Default;
        options.Size = new Vector2D<int>(width, height);
        options.Title = title;
        if (fullscreen)
            options.WindowState = WindowState.Fullscreen;

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
    }

    public void InitialiseLogger()
    {
        var shitfuck = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/log.txt",
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: false);
        if (logStrength == 1)
            shitfuck.MinimumLevel.Debug();
        if (logStrength == 2)
            shitfuck.MinimumLevel.Verbose();

        Log.Logger = shitfuck.CreateLogger();
        Log.Information("Logger has been created");
        Log.Information("Logger is logging at strength [{A}]", logStrength);
    }

    public void Run()
    {
        Window.Window.Run();
        Log.Information("Exiting RedLight Engine now");
    }
}