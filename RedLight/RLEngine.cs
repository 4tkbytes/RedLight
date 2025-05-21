using RedLight.Core;
using RedLight.Graphics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace RedLight;

public class RLEngine
{
    private RLWindow window;
    private Scene.RLScene? _rlScene;

    private GL? gl;
    private Mesh? mesh;
    
    public RLEngine(int width, int height, string title, Scene.RLScene? rlScene)
    {
        WindowOptions options = WindowOptions.Default;
        options.Size = new Vector2D<int>(width, height);
        options.Title = title;
        
        _rlScene = rlScene;

        window = new(options, rlScene);
        window.window.Load += OnLoad;
        window.window.FramebufferResize += OnFramebufferResize;
        
        if (rlScene != null)
        {
            window.SetScene(rlScene);
        }
        
        Console.WriteLine("Initialised Redlight Engine");
    }      private void OnLoad()
    {
        // Check if scene is valid
        if (_rlScene == null || _rlScene.inputHandler == null)
        {
            Console.WriteLine("Warning: Scene or InputHandler is not properly initialized. Keyboard interactions will be disabled.");
            return;
        }
        
        // Create input context once and share it with the window
        IInputContext input = window.window.CreateInput();
        window.SetInputContext(input);
        
        // Keyboard setup
        for (int i = 0; i < input.Keyboards.Count; i++)
        {
            input.Keyboards[i].KeyDown += _rlScene.inputHandler.OnKeyDown;
            input.Keyboards[i].KeyUp += _rlScene.inputHandler.OnKeyUp;
        }
        
        // Mouse setup
        for (int i = 0; i < input.Mice.Count; i++)
        {
            input.Mice[i].MouseMove += _rlScene.inputHandler.OnMouseMove;
            input.Mice[i].MouseDown += _rlScene.inputHandler.OnMouseDown;
            input.Mice[i].MouseUp += _rlScene.inputHandler.OnMouseUp;
        }
        
        // get opengl api
        gl = GL.GetApi(window.window);
        
        if (gl != null)
        {
            gl.Enable(EnableCap.DepthTest);
        }
    }

    private void OnFramebufferResize(Vector2D<int> newSize)
    {
        if (gl != null)
        {
            gl.Viewport(newSize);
        }
    }

    public void Run()
    {
        Console.WriteLine("Running program");
        window.window.Run();
        Cleanup();
    }

    private void Cleanup()
    {
        Console.WriteLine("Cleaning up");
        window.window.Dispose();
    }

    public RLWindow GetWindow()
    {
        return window;
    }
      public void SetScene(Scene.RLScene scene)
    {
        _rlScene = scene;
        window.SetScene(scene);
        
        // If the window is already loaded, we need to reconnect input handlers
        if (window.window.IsInitialized)
        {
            ReconnectInputHandlers();
        }
    }
    
    private void ReconnectInputHandlers()
    {
        // Check if scene is valid
        if (_rlScene == null || _rlScene.inputHandler == null)
        {
            Console.WriteLine("Warning: Cannot reconnect input handlers. Scene or InputHandler is not properly initialized.");
            return;
        }
        
        // If we already have an input context, reconnect the handlers
        var input = window.GetInputContext();
        if (input != null)
        {
            // Reconnect keyboard handlers
            foreach (var keyboard in input.Keyboards)
            {
                keyboard.KeyDown += _rlScene.inputHandler.OnKeyDown;
                keyboard.KeyUp += _rlScene.inputHandler.OnKeyUp;
            }
            
            // Reconnect mouse handlers
            foreach (var mouse in input.Mice)
            {
                mouse.MouseMove += _rlScene.inputHandler.OnMouseMove;
                mouse.MouseDown += _rlScene.inputHandler.OnMouseDown;
                mouse.MouseUp += _rlScene.inputHandler.OnMouseUp;
            }
            
            Console.WriteLine("Input handlers reconnected successfully");
        }
    }
}