using System.Numerics;
using RedLight.Core;
using RedLight.Scene;
using RedLight.Graphics;
using RedLight.Utils;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Game;

public class TestingScene1 : RLScene
{
    public required RLWindow Window { get; set; }
    public required SceneManager SceneManager { get; set; }
    public required RLInputHandler inputHandler { get; set; }

    private Mesh? mesh;
    private GL? gl;
    private ShaderManager? shaderManager;
    private TextureManager? textureManager;
    private ModelManager? modelManager;
    private Camera? camera;
    private float rotationSpeed = 1.0f;
    private Cube? cube;

    public void OnLoad()
    {
        Console.WriteLine("Scene 1 loaded");

        gl = GL.GetApi(Window.window);

        shaderManager = new ShaderManager(gl);
        textureManager = new TextureManager(gl);
        modelManager = new ModelManager(gl, textureManager);

        // Create shaders
        shaderManager.Add("3d", RLConstants.RL_BASIC_SHADER_VERT, RLConstants.RL_BASIC_SHADER_FRAG);
        var shader = shaderManager.Get("3d");

        // Load texture from the model's directory
        textureManager.AddTexture(
            new Texture2D(gl, RLFiles.GetAbsolutePath("Resources/Models/LowPolyFerrisRust/rustacean-3d.png")),
            "ferris-texture"
        );

        textureManager.AddTexture(
            new Texture2D(gl, RLConstants.RL_NO_TEXTURE),
            "no-texture"
        );

        // Create a camera
        camera = new Camera();
        camera.Position = new Vector3(0, 0, 5);
        camera.SetAspectRatio(Window.window.Size.X, Window.window.Size.Y);

        // Enable back-face culling and depth testing
        gl.Enable(EnableCap.CullFace);
        gl.CullFace(TriangleFace.Back);
        gl.Enable(EnableCap.DepthTest);
        gl.DepthFunc(DepthFunction.Less);

        // Load the Ferris model
        var ferrisModel = modelManager.LoadModel(
            RLFiles.GetAbsolutePath("Resources/Models/LowPolyFerrisRust/rustacean-3d.obj"), 
            shader,
            textureManager.GetTexture("ferris-texture"));

        if (ferrisModel != null)
        {
            // Create a Ferris entity
            var ferrisEntity = ferrisModel.CreateEntity();
            ferrisEntity.Transform.Scale = new Vector3(0.5f, 0.5f, 0.5f); // Scale it down if needed
            ferrisEntity.Transform.Rotation = new Vector3(0, MathF.PI, 0); // Orient it properly if needed
            Console.WriteLine($"Successfully created Ferris entity with model ID: {ferrisEntity.ModelId}");
        }
        else
        {
            Console.WriteLine("Failed to load Ferris model!");
        }
    }

    public void OnUpdate(double delta)
    {
        if (camera == null) return;
        
        float moveSpeed = 0.1f;
        if (inputHandler.IsKeyDown(Key.W))
            camera.Position += new Vector3(0, 0, -moveSpeed);
        if (inputHandler.IsKeyDown(Key.S))
            camera.Position += new Vector3(0, 0, moveSpeed);
        if (inputHandler.IsKeyDown(Key.A))
            camera.Position += new Vector3(-moveSpeed, 0, 0);
        if (inputHandler.IsKeyDown(Key.D))
            camera.Position += new Vector3(moveSpeed, 0, 0);
    }

    public void OnRender(double delta)
    {
        if (gl == null || shaderManager == null || modelManager == null || camera == null) return;
        
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
    }
}