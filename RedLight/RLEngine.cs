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
    private Scene.RLScene _rlScene;

    private GL gl;
    private Mesh mesh;
    
    public RLEngine(int width, int height, string title, Scene.RLScene rlScene)
    {
        WindowOptions options = WindowOptions.Default;
        options.Size = new Vector2D<int>(width, height);
        options.Title = title;
        
        _rlScene = rlScene;

        window = new(options, rlScene);
        window.window.Load += OnLoad;
        window.window.FramebufferResize += OnFramebufferResize;
        window.SetScene(rlScene);
        
        Console.WriteLine("Initialised Redlight Engine");
    }
    
    private void OnLoad()
    {
        // Setting up keyboard shenanigans
        IInputContext input = window.window.CreateInput();
        for (int i = 0; i < input.Keyboards.Count; i++)
        {
            input.Keyboards[i].KeyDown += _rlScene.KeyDown;
        }
        
        // get opengl api
        gl = GL.GetApi(window.window);
        
        gl.Enable(EnableCap.DepthTest);
    }

    private void OnFramebufferResize(Vector2D<int> newSize)
    {
        gl.Viewport(newSize);
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
}