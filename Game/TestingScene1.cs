using RedLight;
using RedLight.Graphics;
using RedLight.Input;
using RedLight.Scene;
using RedLight.Utils;
using Silk.NET.Input;

namespace Game;

public class TestingScene1 : RLScene, RLKeyboard
{
    public RLGraphics Graphics { get; set; }
    public SceneManager SceneManager { get; set; }
    public ShaderManager ShaderManager { get; set; }
    public RLEngine Engine { get; set; }

    private Mesh mesh;
    
    float[] vertices =
    {
        0.5f,  0.5f, 0.0f,
        0.5f, -0.5f, 0.0f,
        -0.5f, -0.5f, 0.0f,
        -0.5f,  0.5f, 0.0f
    };
        
    uint[] indices =
    {
        0u, 1u, 3u,
        1u, 2u, 3u
    };

    public void OnLoad()
    {
        Console.WriteLine("Scene 1 Loaded");
        
        ShaderManager.TryAdd(
            "basic",
            new RLShader(Graphics, ShaderType.Vertex, RLConstants.RL_BASIC_SHADER_VERT),
            new RLShader(Graphics, ShaderType.Fragment, RLConstants.RL_BASIC_SHADER_FRAG)
            );

        mesh = Graphics.CreateMesh(vertices, indices, ShaderManager.Get("basic").vertexShader, ShaderManager.Get("basic").fragmentShader);
    }

    public void OnUpdate(double deltaTime)
    {
        
    }

    public void OnRender(double deltaTime)
    {
        Graphics.Clear();
        Graphics.ClearColour(new RLGraphics.Colour { r = (float)100/256, g = (float)146/256, b = (float)237/256, a = 1.0f });
        
        Graphics.Bind(mesh);
        
        Graphics.Draw(indices.Length);
    }

    public void OnKeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        if (key == Key.Escape)
        {
            Engine.Window.Window.Close();
        }
        
        if (key == Key.Right)
        {
            SceneManager.SwitchScene("testing_scene_2");
        }
    }
}