using RedLight.Core;
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
        
        this._rlScene = rlScene;

        window = new(options, rlScene);
        window.window.Load += OnLoad;
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