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
    
    private Vector3D<float>[] cubePositions =
    {
        new Vector3D<float>( 3.0f,  0.0f,  3.0f), 
        new Vector3D<float>( 5.0f,  5.0f, 15.0f), 
        new Vector3D<float>(2.5f, -2.2f, 3.5f),  
        new Vector3D<float>(4.8f, -2.0f, 12.3f),  
        new Vector3D<float>( 6.4f, -0.4f, 3.5f),  
        new Vector3D<float>(4.7f,  3.0f, 7.5f),  
        new Vector3D<float>( 5.3f, -2.0f, 3.5f),  
        new Vector3D<float>( 5.5f,  2.0f, 3.5f), 
        new Vector3D<float>( 5.5f,  0.2f, 3.5f), 
        new Vector3D<float>(3.3f,  1.0f, 3.5f) 
    };
    
    float[] vertices = {
        -0.5f, -0.5f, -0.5f,  0.0f, 0.0f,
         0.5f, -0.5f, -0.5f,  1.0f, 0.0f,
         0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
         0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
        -0.5f,  0.5f, -0.5f,  0.0f, 1.0f,
        -0.5f, -0.5f, -0.5f,  0.0f, 0.0f,
    
        -0.5f, -0.5f,  0.5f,  0.0f, 0.0f,
         0.5f, -0.5f,  0.5f,  1.0f, 0.0f,
         0.5f,  0.5f,  0.5f,  1.0f, 1.0f,
         0.5f,  0.5f,  0.5f,  1.0f, 1.0f,
        -0.5f,  0.5f,  0.5f,  0.0f, 1.0f,
        -0.5f, -0.5f,  0.5f,  0.0f, 0.0f,
    
        -0.5f,  0.5f,  0.5f,  1.0f, 0.0f,
        -0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
        -0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
        -0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
        -0.5f, -0.5f,  0.5f,  0.0f, 0.0f,
        -0.5f,  0.5f,  0.5f,  1.0f, 0.0f,
    
         0.5f,  0.5f,  0.5f,  1.0f, 0.0f,
         0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
         0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
         0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
         0.5f, -0.5f,  0.5f,  0.0f, 0.0f,
         0.5f,  0.5f,  0.5f,  1.0f, 0.0f,
    
        -0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
         0.5f, -0.5f, -0.5f,  1.0f, 1.0f,
         0.5f, -0.5f,  0.5f,  1.0f, 0.0f,
         0.5f, -0.5f,  0.5f,  1.0f, 0.0f,
        -0.5f, -0.5f,  0.5f,  0.0f, 0.0f,
        -0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
    
        -0.5f,  0.5f, -0.5f,  0.0f, 1.0f,
         0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
         0.5f,  0.5f,  0.5f,  1.0f, 0.0f,
         0.5f,  0.5f,  0.5f,  1.0f, 0.0f,
        -0.5f,  0.5f,  0.5f,  0.0f, 0.0f,
        -0.5f,  0.5f, -0.5f,  0.0f, 1.0f
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
            "fuckass-angus",
            new RLTexture(Graphics, RLFiles.GetEmbeddedResourceBytes("RedLight.Resources.Textures.thing.png")));
        
        Graphics.EnableDepth();
        
        camera = new Camera(new Vector3D<float>(0,0,3),
            new Vector3D<float>(0,0,-1),
            new Vector3D<float>(0,1,0),
            float.DegreesToRadians(99.0f), (float)800/600, 0.1f, 100.0f).SetSpeed(0.05f);
        
        mesh1 = Graphics.CreateMesh(
            vertices, indices, 
            ShaderManager.Get("basic").vertexShader, 
            ShaderManager.Get("basic").fragmentShader)
            .MakeTransformable();
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
    }

    public void OnRender(double deltaTime)
    {
        Graphics.Clear();
        Graphics.ClearColour(new RLGraphics.Colour { r = 100f/256, g = 146f/256, b = 237f/256, a = 1f });
        
        Graphics.ActivateTexture();
        Graphics.BindTexture(TextureManager.Get("no-texture"));
        Graphics.Use(mesh1);

        foreach (var position in cubePositions)
        {
            mesh1.AbsoluteReset();
            mesh1.SetModel(Matrix4X4.CreateTranslation(position));
            Graphics.UpdateModel(mesh1);
            Graphics.Draw();
        }

        mesh1.AbsoluteReset();
        Graphics.BindTexture(TextureManager.Get("fuckass-angus"));
        Graphics.UpdateModel(mesh1);
        Graphics.UpdateView(camera, mesh1);
        Graphics.UpdateProjection(camera, mesh1);
        Graphics.Draw();

        var err = Graphics.OpenGL.GetError();
        if (err != 0)
            Log.Error("GL Error: {Error}", err);
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
    }

    public void OnKeyUp(IKeyboard keyboard, Key key, int keyCode)
    {
        PressedKeys.Remove(key);
    }

    public void OnMouseMove(IMouse mouse, Vector2 mousePosition)
    {
        var lastPosition = Engine.Window.Window.Size;
        float lastX = lastPosition.X, lastY = lastPosition.Y;
        float xoffset = mousePosition.X - lastX;
        float yoffset = mousePosition.Y - lastY;
        lastX = mousePosition.X;
        lastY = mousePosition.Y;

        xoffset *= camera.Sensitivity;
        yoffset *= camera.Sensitivity;
        camera.Yaw += xoffset;    // yaw
        camera.Pitch += yoffset;    // pitch
        
        if(camera.Pitch > 89.0f)
            camera.Pitch =  89.0f;
        if(camera.Pitch < -89.0f)
            camera.Pitch = -89.0f;

        Vector3D<float> direction = new();
        direction.X = float.Cos(float.DegreesToRadians(camera.Yaw)) * float.Cos(float.DegreesToRadians(camera.Pitch));
        direction.Y = float.Sin(float.DegreesToRadians(camera.Pitch));
        direction.Z = float.Sin(float.DegreesToRadians(camera.Yaw)) * float.Cos(float.DegreesToRadians(camera.Pitch));
        camera = camera.SetFront(direction);
    }
}