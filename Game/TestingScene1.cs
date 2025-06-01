using System.Net.Mime;
using System.Numerics;
using RedLight;
using RedLight.Graphics;
using RedLight.Input;
using RedLight.Scene;
using RedLight.Utils;
using Serilog;
using Silk.NET.Core.Native;
using Silk.NET.Input;
using Silk.NET.Maths;

namespace Game;

public class TestingScene1 : RLScene, RLKeyboard
{
    public RLGraphics Graphics { get; set; }
    public SceneManager SceneManager { get; set; }
    public ShaderManager ShaderManager { get; set; }
    public TextureManager TextureManager { get; set; }
    public RLEngine Engine { get; set; }

    private Transformable<Mesh> mesh;
    
    // The quad vertices data. Now with Texture coordinates!
    float[] vertices =
    {
    //       aPosition     | aTexCoords
        0.5f,  0.5f, 0.0f,  1.0f, 1.0f,
        0.5f, -0.5f, 0.0f,  1.0f, 0.0f,
        -0.5f, -0.5f, 0.0f,  0.0f, 0.0f,
        -0.5f,  0.5f, 0.0f,  0.0f, 1.0f
    };
        
    uint[] indices =
    {
        0u, 1u, 3u,
        1u, 2u, 3u
    };

    public void OnLoad()
    {
        Log.Information("Scene 1 Loaded");
        
        ShaderManager.TryAdd(
            "basic",
            new RLShader(Graphics, ShaderType.Vertex, RLConstants.RL_BASIC_SHADER_VERT),
            new RLShader(Graphics, ShaderType.Fragment, RLConstants.RL_BASIC_SHADER_FRAG)
        );
        
        TextureManager.TryAdd(
            "no-texture",
            new RLTexture(Graphics, RLConstants.RL_NO_TEXTURE)
        );
        
        Graphics.EnableDepth();
        
        mesh = Graphics.CreateMesh(
            vertices, indices, 
            ShaderManager.Get("basic").vertexShader, 
            ShaderManager.Get("basic").fragmentShader)
            .MakeTransformable();
        mesh
            .Reset()
            .Rotate(RLMath.DegreesToRadians(-55.0f), Vector3.UnitX)
            .Translate(new Vector3(1.0f, 0, 0))
            .Project(RLMath.DegreesToRadians(45.0f), (float)Engine.Window.Window.Size.X / Engine.Window.Window.Size.Y,
                0.1f, 100.0f);
    }

    public void OnUpdate(double deltaTime)
    {
        mesh = mesh
            .Rotate(RLMath.DegreesToRadians(1.0f), Vector3.UnitX)
            .Rotate(RLMath.DegreesToRadians(1.0f), Vector3.UnitY)
            .Rotate(RLMath.DegreesToRadians(1.0f), Vector3.UnitZ)
            .Translate(new Vector3(1.0f, 0, 0))
            .Scale(new Vector3(1.0f, 1.0f, 1.0f));
    }

    public void OnRender(double deltaTime)
    {
        Graphics.Clear();
        Graphics.ClearColour(new RLGraphics.Colour { r = 100f/256, g = 146f/256, b = 237f/256, a = 1f });

        Graphics.ActivateTexture();
        Graphics.BindTexture(TextureManager.Get("no-texture"));
        
        Graphics.Use(mesh);
        
        Graphics.UpdateModel(mesh);
        Graphics.UpdateView(mesh);
        Graphics.UpdateProjection(mesh);

        var view = mesh.View;
        Matrix4x4.Invert(view, out var inverseView);
        Matrix4x4.Decompose(inverseView, out _, out _, out var cameraPos);
        Log.Verbose("Camera position: {X}, {Y}, {Z}", cameraPos.X, cameraPos.Y, cameraPos.Z);
        
        Graphics.Draw(indices.Length);
        var err = Graphics.OpenGL.GetError();
        if (err != 0)
            Log.Error("GL Error: {Error}", err);
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