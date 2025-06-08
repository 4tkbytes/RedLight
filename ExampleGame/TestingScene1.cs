using System.Numerics;
using RedLight;
using RedLight.Graphics;
using RedLight.Input;
using RedLight.Scene;
using RedLight.UI;
using RedLight.Utils;
using Serilog;
using Silk.NET.Assimp;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL.Extensions.ImGui;
using Camera = RedLight.Graphics.Camera;
using Plane = RedLight.Graphics.Primitive.Plane;
using ShaderType = RedLight.Graphics.ShaderType;

namespace ExampleGame;

public class TestingScene1 : RLScene, RLKeyboard, RLMouse
{
    public RLGraphics Graphics { get; set; }
    public SceneManager SceneManager { get; set; }
    public ShaderManager ShaderManager { get; set; }
    public TextureManager TextureManager { get; set; }
    public InputManager InputManager { get; set; }
    public List<Transformable<RLModel>> ObjectModels { get; set; } = new();
    public RLEngine Engine { get; set; }
    public HashSet<Key> PressedKeys { get; set; } = new();

    private Camera camera;
    private bool isCaptured = true;
    private RLImGui controller;
    
    private bool modelListChanged = false;
    private List<Transformable<RLModel>> newModels = new();
    private int oldLength;

    public void OnLoad()
    {
        Graphics.Enable();
        Graphics.ShutUp = true;

        ShaderManager.TryAdd(
            "basic",
            new RLShader(Graphics, ShaderType.Vertex, RLConstants.RL_BASIC_SHADER_VERT),
            new RLShader(Graphics, ShaderType.Fragment, RLConstants.RL_BASIC_SHADER_FRAG)
        );

        TextureManager.TryAdd(
            "no-texture",
            new RLTexture(Graphics, RLFiles.GetResourcePath(RLConstants.RL_NO_TEXTURE_PATH))
        );

        var plane = new Plane(Graphics, TextureManager, ShaderManager, 20f, 20f).Default();

        controller = new RLImGui(Graphics, Engine.Window, InputManager, ShaderManager, TextureManager, SceneManager);
        Engine.InitialiseLogger(controller.Console);

        var size = Engine.Window.Window.Size;
        camera = new Camera(size);

        var fox = Graphics.CreateModel("ExampleGame.Resources.low_poly_fox.glb", TextureManager, ShaderManager, "fox")
            .Rotate((float) -Math.PI/2, Vector3D<float>.UnitX);

        Graphics.AddModels(ObjectModels, controller, fox);
        Graphics.AddModels(ObjectModels, controller, plane.Model);
        
        oldLength = ObjectModels.Count;
    }

    public void OnUpdate(double deltaTime)
    {
        Engine.Window.FramesPerSecond = 1.0 / deltaTime;

        camera = camera.SetSpeed(2.5f * (float)deltaTime);

        if (ObjectModels.Count != oldLength)
        {
            modelListChanged = true;
            
            for (int i = oldLength; i < ObjectModels.Count; i++)
            {
                newModels.Add(ObjectModels[i]);
            }
            
            oldLength = ObjectModels.Count;
        }

        if (isCaptured)
            camera.KeyMap(PressedKeys);
    }

    public void OnRender(double deltaTime)
    {
        Graphics.Begin();
        {
            Graphics.Clear();
            Graphics.ClearColour(RLConstants.RL_COLOUR_CORNFLOWER_BLUE);

            foreach (var model in ObjectModels)
            {
                Graphics.Use(model);
                Graphics.Update(camera, model);
                Graphics.Draw(model);
            }
        }
        Graphics.End();
        
        controller.Render(deltaTime, camera);
    }

    public void OnKeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        PressedKeys.Add(key);
        if (key == Key.Escape)
        {
            Engine.Window.Window.Close();
        }
        if (key == Key.Left)
        {
            var oldCaptured = isCaptured;
            isCaptured = !isCaptured;
            Log.Debug("Changing mouse capture mode [{A} -> {B}]", oldCaptured, isCaptured);
        }
    }

    public void OnKeyUp(IKeyboard keyboard, Key key, int keyCode)
    {
        PressedKeys.Remove(key);
    }

    public void OnMouseMove(IMouse mouse, Vector2 mousePosition)
    {
        Graphics.IsCaptured(mouse, isCaptured);
        if (isCaptured)
            camera.FreeMove(mousePosition);
    }
}