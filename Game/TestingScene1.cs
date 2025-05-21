using RedLight.Core;
using RedLight.Scene;
using RedLight.Graphics;
using RedLight.Utils;
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
    private TextureManager textureManager;

    public void OnLoad()
    {
        Console.WriteLine("Scene 1 loaded");
        
        gl = GL.GetApi(Window.window);
        shaderManager = new ShaderManager(gl);
        textureManager = new TextureManager(gl);

        shaderManager.Add(
            "basic",
            RLConstants.RL_BASIC_SHADER_VERT,
            RLConstants.RL_BASIC_SHADER_FRAG
            );
        
        var shader = shaderManager.Get("basic");

        float[] vertices = {
            //X    Y      Z
            0.5f,  0.5f, 0.0f,
            0.5f, -0.5f, 0.0f,
            -0.5f, -0.5f, 0.0f,
            -0.5f,  0.5f, 0.5f
        };
        uint[] indices =
        {
            0, 1, 3,
            1, 2, 3
        };
        float[] texcoords =
        {
            0.5f, 0.5f,
            0.5f, 0.0f,
            0.0f, 0.0f,
            0.0f, 0.5f
        };

        textureManager.AddTexture(
            new Texture2D(gl, RLConstants.RL_NO_TEXTURE),
            "no-texture"
            );
        mesh = new Mesh(gl, vertices, indices, texcoords, shader, textureManager.GetTexture("no-texture"));
    }

    public void OnUpdate(double delta) { }

    public void OnRender(double delta)
    {
        gl.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        mesh.Render();
    }

    public void KeyDown(IKeyboard keyboard, Key key, int arg3)
    {
        if (key == Key.Escape)
        {
            Window.window.Close();
        }
    }
}