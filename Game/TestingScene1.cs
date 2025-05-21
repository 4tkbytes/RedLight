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
    private Camera camera;
    private float rotationSpeed = 1.0f;
    private Cube cube;

    public void OnLoad()
    {
        Console.WriteLine("Scene 1 loaded");

        gl = GL.GetApi(Window.window);
        gl.Enable(EnableCap.DepthTest); // Enable depth testing for 3D
    
        shaderManager = new ShaderManager(gl);
        textureManager = new TextureManager(gl);
        
        string fragmentShaderSource = RLConstants.RL_BASIC_SHADER_FRAG;
        shaderManager.Add("3d", RLConstants.RL_BASIC_SHADER_VERT, RLConstants.RL_BASIC_SHADER_FRAG);
        var shader = shaderManager.Get("3d");

        textureManager.AddTexture(
            new Texture2D(gl, RLConstants.RL_NO_TEXTURE),
            "no-texture"
        );

        // Create a camera
        camera = new Camera();
        camera.Position = new Vector3(0, 0, 3);
        camera.SetAspectRatio(Window.window.Size.X, Window.window.Size.Y);

        // Create a cube with the 3D shader
        cube = new Cube(gl, shader, textureManager.GetTexture("no-texture"));
    }

    public void OnUpdate(double delta)
    {
        // Rotate the cube
        cube.Transform.Rotation += new Vector3(0, MathHelper.DegreesToRadians(rotationSpeed), 0) * (float)delta;
    }

    public void OnRender(double delta)
    {
        gl.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // Set view and projection uniforms
        var shader = shaderManager.Get("3d");
        shader.Use();

        Matrix4x4 view = camera.GetViewMatrix();
        Matrix4x4 projection = camera.GetProjectionMatrix();

        // Use Uniforms property instead of unsafe code
        shader.Uniforms.SetMatrix4("uView", view);
        shader.Uniforms.SetMatrix4("uProjection", projection);

        cube.Render();
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