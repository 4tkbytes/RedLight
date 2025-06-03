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

    private Camera camera;
    private IMouse mouse;
    private bool isCaptured = true;

    private Transformable<RLModel> _rlModel;
    
    public void OnLoad()
    {
        Log.Information("Scene 1 Loaded");
        Graphics.EnableDepth();

        ShaderManager.TryAdd(
            "basic",
            new RLShader(Graphics, ShaderType.Vertex, RLConstants.RL_BASIC_SHADER_VERT),
            new RLShader(Graphics, ShaderType.Fragment, RLConstants.RL_BASIC_SHADER_FRAG)
        );

        TextureManager.TryAdd(
            "no-texture",
            new RLTexture(Graphics, RLFiles.GetEmbeddedResourcePath("RedLight.Resources.Textures.thing.png"), RLTextureType.Diffuse)
        );

        camera = new Camera(new Vector3D<float>(0, 0, 3),
            new Vector3D<float>(0, 0, -1),
            new Vector3D<float>(0, 1, 0),
            float.DegreesToRadians(99.0f), (float)800 / 600, 0.1f, 100.0f).SetSpeed(0.05f);

        _rlModel = new RLModel(Graphics, RLFiles.GetEmbeddedResourcePath("RedLight.Resources.Models.cube.model")).MakeTransformable();
    }


    public void OnUpdate(double deltaTime)
    {
        Engine.Window.FramesPerSecond = 1.0 / deltaTime;

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
        Graphics.Begin();
        {
            Graphics.Clear();
            Graphics.ClearColour(RLConstants.RL_COLOUR_CORNFLOWER_BLUE);

            Graphics.Use(_rlModel);
            Graphics.BindTexture(TextureManager.Get("no-texture"));
            Graphics.UpdateView(camera, _rlModel);
            Graphics.UpdateProjection(camera, _rlModel);
            Graphics.UpdateModel(_rlModel);
            Graphics.Draw(_rlModel);
            Graphics.CheckGLErrors();
        }
        Graphics.End();
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