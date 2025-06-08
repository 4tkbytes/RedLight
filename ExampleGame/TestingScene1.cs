using System.Numerics;
using RedLight;
using RedLight.Graphics;
using RedLight.Input;
using RedLight.Scene;
using RedLight.Utils;
using Serilog;
using Silk.NET.Input;
using Silk.NET.OpenGL.Extensions.ImGui;
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
    public RLEngine Engine { get; set; }
    public HashSet<Key> PressedKeys { get; set; } = new();

    private Camera camera;
    private bool isCaptured = true;
    private ImGuiController controller;
    private List<Transformable<RLModel>> objectModels = new();

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

        TextureManager.TryAdd(
            "Carrot_Base_diffuse",
            new RLTexture(Graphics, RLFiles.GetResourcePath("ExampleGame.Resources.Carrot.textures.Carrot_Base_diffuse.png"))
        );

        TextureManager.TryAdd(
            "Carrot_Leaves_diffuse",
            new RLTexture(Graphics, RLFiles.GetResourcePath("ExampleGame.Resources.Carrot.textures.Carrot_Leaves_diffuse.png"))
        );

        var plane = new Plane(Graphics, TextureManager, ShaderManager, 20f, 20f).Default();

        controller = Graphics.ImGuiLoad(Engine.Window, InputManager);

        var size = Engine.Window.Window.Size;
        camera = new Camera(size);

        // var carrot = Graphics.CreateModel("ExampleGame.Resources.Carrot.scene.gltf", TextureManager, ShaderManager,
        //     "carrot");
        // carrot.Target.ApplyTextureFromManager("Carrot_0", TextureManager.Get("Carrot_Base_diffuse"));
        // carrot.Target.ApplyTextureFromManager("Carrot_1", TextureManager.Get("Carrot_Leaves_diffuse"));

        var deer = Graphics.CreateModel("ExampleGame.Resources.Deer_001.fbx", TextureManager, ShaderManager,
            "deer");
        
        objectModels.Add(deer);
        // objectModels.Add(crab);
        // objectModels.Add(carrot);
        objectModels.Add(plane.Model);
    }

    public void OnUpdate(double deltaTime)
    {
        Engine.Window.FramesPerSecond = 1.0 / deltaTime;

        camera = camera.SetSpeed(2.5f * (float)deltaTime);

        camera.KeyMap(PressedKeys);
    }

    public void OnRender(double deltaTime)
    {
        Graphics.Begin();
        {
            Graphics.Clear();
            Graphics.ClearColour(RLConstants.RL_COLOUR_CORNFLOWER_BLUE);

            foreach (var model in objectModels)
            {
                Graphics.Use(model);
                Graphics.Update(camera, model);
                Graphics.Draw(model);
            }
        }
        Graphics.End();

        Graphics.ImGuiRender(controller, deltaTime, objectModels, camera);

    }

    public void OnKeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        PressedKeys.Add(key);
        if (key == Key.Escape)
        {
            Engine.Window.Window.Close();
        }
        if (key == Key.Number2)
        {
            SceneManager.SwitchScene("stress_test");
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