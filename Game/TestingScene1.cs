using RedLight;
using RedLight.Core;
using RedLight.Scene;
using RedLight.Graphics;
using Silk.NET.Input;
using Silk.NET.OpenGL;

namespace Game;

public class TestingScene1 : RLScene
{
    public RLWindow Window { get; set; }
    public SceneManager SceneManager { get; set; }
    
    private Mesh mesh;
    private GL gl;
    private ShaderManager shaderManager;

    public void OnLoad()
    {
        Console.WriteLine("Scene 1 loaded");
        gl = GL.GetApi(Window.window);
        shaderManager = new ShaderManager(gl);

        // Dummy shader sources
        string vertexShaderSource = @"
            #version 330 core
            layout(location = 0) in vec3 aPosition;
            void main() {
                gl_Position = vec4(aPosition, 1.0);
            }
        ";
        string fragmentShaderSource = @"
            #version 330 core
            out vec4 FragColor;
            void main() {
                FragColor = vec4(0.0, 0.0, 1.0, 1.0);
            }
        ";

        shaderManager.Add("basic_green", vertexShaderSource, fragmentShaderSource);
        var shader = shaderManager.Get("basic_green");

        // Dummy triangle data
        float[] vertices = {
            0.0f,  0.5f, 0.0f,
            -0.5f, -0.5f, 0.0f,
            0.5f, -0.5f, 0.0f
        };
        uint[] indices = { 0, 1, 2 };

        mesh = new Mesh(gl, vertices, indices, shader);
    }

    public void OnUpdate(double delta) { }

    public void OnRender(double delta)
    {
        mesh.Render();
    }

    public void KeyDown(IKeyboard keyboard, Key key, int arg3)
    {
        if (key == Key.Escape)
        {
            Window.window.Close();
        }
        else if (key == Key.F5)
        {
            // Toggle between scenes
            if (SceneManager.ActiveRlScene == this)
                SceneManager.SwitchScene("TestScene2");
            else
                SceneManager.SwitchScene("TestScene1");
        }
    }
}