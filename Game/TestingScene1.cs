using System.Numerics;
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
    private ModelManager modelManager;
    private Camera camera;
    private float rotationSpeed = 1.0f;
    private Cube cube;

    public void OnLoad()
    {
        Console.WriteLine("Scene 1 loaded");

        gl = GL.GetApi(Window.window);
        gl.Enable(EnableCap.DepthTest);

        shaderManager = new ShaderManager(gl);
        textureManager = new TextureManager(gl);
        modelManager = new ModelManager(gl, textureManager);

        // Create shaders
        shaderManager.Add("3d", RLConstants.RL_BASIC_SHADER_VERT, RLConstants.RL_BASIC_SHADER_FRAG);
        var shader = shaderManager.Get("3d");

        // Load texture from the model's directory
        textureManager.AddTexture(
            new Texture2D(gl, "Game.Resources.Models.LowPolyFerrisRust.rustacean-3d.png"),
            "ferris-texture"
        );

        // Create a camera
        camera = new Camera();
        camera.Position = new Vector3(0, 0, 5);
        camera.SetAspectRatio(Window.window.Size.X, Window.window.Size.Y);

        // Load the Ferris model
        var ferrisModel = modelManager.LoadModel("Game.Resources.Models.LowPolyFerrisRust.rustacean-3d.obj", 
            shader,
            textureManager.GetTexture("ferris-texture"));

        // Create a Ferris entity
        var ferrisEntity = ferrisModel.CreateEntity();
        ferrisEntity.Transform.Scale = new Vector3(0.5f, 0.5f, 0.5f); // Scale it down if needed
        ferrisEntity.Transform.Rotation = new Vector3(0, MathF.PI, 0); // Orient it properly if needed
    }

    public void OnUpdate(double delta)
    {
        // Rotate the Ferris model
        if (modelManager.GetModel(0) != null && modelManager.GetModel(0).LinkedEntities.Count > 0)
        {
            var entity = modelManager.GetModel(0).LinkedEntities[0];
            entity.Transform.Rotation += new Vector3(0, (float)delta * rotationSpeed, 0);
        }
    }

    public void OnRender(double delta)
    {
        gl.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // Render all models and their entities
        var shader = shaderManager.Get("3d");
        modelManager.RenderAll(shader, camera);
    }

    public void KeyDown(IKeyboard keyboard, Key key, int arg3)
    {
        if (key == Key.Escape)
        {
            Window.window.Close();
        }
        
        if (key == Key.W)
            camera.Position += new Vector3(0, 0, -0.1f);
        if (key == Key.S)
            camera.Position += new Vector3(0, 0, 0.1f);
        if (key == Key.A)
            camera.Position += new Vector3(-0.1f, 0, 0);
        if (key == Key.D)
            camera.Position += new Vector3(0.1f, 0, 0);
    }
}