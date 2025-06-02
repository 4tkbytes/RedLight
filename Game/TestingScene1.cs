using System.Collections.Generic;
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
using Silk.NET.OpenGL;
using ShaderType = RedLight.Graphics.ShaderType;

namespace Game;

public class TestingScene1 : RLScene, RLKeyboard, RLMouse
{
    public RLGraphics Graphics { get; set; }
    public SceneManager SceneManager { get; set; }
    public ShaderManager ShaderManager { get; set; }
    public TextureManager TextureManager { get; set; }
    public RLEngine Engine { get; set; }
    public HashSet<Key> PressedKeys { get; } = new();

    private Transformable<Mesh> mesh1;
    private Camera camera;
    private IMouse mouse;

    private bool isCaptured = true;
    private readonly List<Transformable<Mesh>> spawnedObjects = new();
    private double spawnTimer = 0.0;
    
   // 8 unique vertices (position + uv)
    float[] vertices = {
        // positions        // uvs
        -0.5f, -0.5f, -0.5f,  0.0f, 0.0f, // 0
        0.5f, -0.5f, -0.5f,  1.0f, 0.0f, // 1
        0.5f,  0.5f, -0.5f,  1.0f, 1.0f, // 2
        -0.5f,  0.5f, -0.5f,  0.0f, 1.0f, // 3
        -0.5f, -0.5f,  0.5f,  0.0f, 0.0f, // 4
        0.5f, -0.5f,  0.5f,  1.0f, 0.0f, // 5
        0.5f,  0.5f,  0.5f,  1.0f, 1.0f, // 6
        -0.5f,  0.5f,  0.5f,  0.0f, 1.0f  // 7
    };

    uint[] indices = {
        // back face
        0, 1, 2, 2, 3, 0,
        // front face
        4, 5, 6, 6, 7, 4,
        // left face
        4, 0, 3, 3, 7, 4,
        // right face
        1, 5, 6, 6, 2, 1,
        // bottom face
        4, 5, 1, 1, 0, 4,
        // top face
        3, 2, 6, 6, 7, 3
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
            "fuckass-angus",
            new RLTexture(Graphics, RLFiles.GetEmbeddedResourceBytes("RedLight.Resources.Textures.thing.png")));
        
        Graphics.EnableDepth();
        
        camera = new Camera(new Vector3D<float>(0,0,3),
            new Vector3D<float>(0,0,-1),
            new Vector3D<float>(0,1,0),
            float.DegreesToRadians(99.0f), (float)800/600, 0.1f, 100.0f).SetSpeed(0.05f);
    }
    

    public void OnUpdate(double deltaTime)
    {
        if (camera == null)
        {
            Log.Error("Camera is null in OnUpdate!");
            return;
        }

        camera = camera.SetSpeed(2.5f * (float)deltaTime);
        if (PressedKeys.Contains(Key.W))
            camera = camera.MoveForward();
        if (PressedKeys.Contains(Key.S))
            camera = camera.MoveBack();
        if (PressedKeys.Contains(Key.A))
            camera = camera.MoveLeft();
        if (PressedKeys.Contains(Key.D))
            camera = camera.MoveRight();
        if (PressedKeys.Contains(Key.ShiftLeft))
            camera = camera.MoveDown();
        if (PressedKeys.Contains(Key.Space))
            camera = camera.MoveUp();
        camera = camera.SetPosition(camera.Position).UpdateCamera();
        
        spawnTimer += deltaTime;
        if (spawnTimer >= 1.0)
        {
            spawnTimer = 0.0;
            var random = new Random();
            var position = new Vector3D<float>(
                (float)(random.NextDouble() * random.Next(-12, 12)),
                (float)(random.NextDouble() * random.Next(-12, 12)),
                (float)(random.NextDouble() * random.Next(-12, 12))
            );
            var newObject = Graphics.CreateMesh(vertices, indices,
                    ShaderManager.Get("basic").vertexShader,
                    ShaderManager.Get("basic").fragmentShader)
                .MakeTransformable()
                .SetModel(Matrix4X4.CreateTranslation(position));
            spawnedObjects.Add(newObject);
            Log.Debug("Spawned object count: {0}", spawnedObjects.Count);
        }
    }

    public void OnRender(double deltaTime)
    {
        Graphics.Clear();
        Graphics.ClearColour(new RLGraphics.Colour { r = 100f/256, g = 146f/256, b = 237f/256, a = 1f });
        
        foreach (var obj in spawnedObjects)
        {
            Graphics.Use(obj);
            Graphics.BindTexture(TextureManager.Get("no-texture"));
            Graphics.UpdateView(camera, obj);
            Graphics.UpdateProjection(camera, obj);
            Graphics.UpdateModel(obj);
            Graphics.Draw(indices.Length);
            Graphics.CheckGLErrors();
        }
    }
    
    public void OnKeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        PressedKeys.Add(key);
        if (key == Key.Escape)
        {
            Engine.Window.Window.Close();
        }
        if (key == Key.Right)
        {
            SceneManager.SwitchScene("testing_scene_2");
        }
        if (key == Key.Left)
        {
            isCaptured = !isCaptured;
            Log.Debug("Changing mouse capture mode [{A}]", isCaptured);
        }
    }

    public void OnKeyUp(IKeyboard keyboard, Key key, int keyCode)
    {
        PressedKeys.Remove(key);
    }

    public void OnMouseMove(IMouse mouse, Vector2 mousePosition)
    {
        this.mouse = mouse;
        
        Graphics.IsCaptured(mouse, isCaptured);
        camera.FreeMove(mousePosition);
    }
}