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
    private RLImGui controller;
    private float cameraSpeed = 2.5f;

    public void OnLoad()
    {
        Graphics.Enable();
        Graphics.ShutUp = true;

        var plane = new Plane(Graphics, TextureManager, ShaderManager, 20f, 20f).Default();

        controller = new RLImGui(Graphics, Engine.Window, InputManager, ShaderManager, TextureManager, SceneManager);
        Engine.InitialiseLogger(controller.Console);

        var size = Engine.Window.Window.Size;
        camera = new Camera(size);

        var maxwell = Graphics.CreateModel("RedLight.Resources.Models.Maxwell.maxwell_the_cat.glb", TextureManager, ShaderManager, "maxwell")
            .Rotate(float.DegreesToRadians(-90.0f), Vector3D<float>.UnitX)
            .Scale(new Vector3D<float>(0.05f, 0.05f, 0.05f));

        Graphics.AddModels(ObjectModels, controller, maxwell);
        Graphics.AddModels(ObjectModels, controller, plane.Model);
    }

    public void OnUpdate(double deltaTime)
    {
        camera = camera.SetSpeed(cameraSpeed * (float)deltaTime);
        
        if (InputManager.isCaptured)
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
        
        InputManager.ChangeCaptureToggle(key);
    }

    public void OnKeyUp(IKeyboard keyboard, Key key, int keyCode)
    {
        PressedKeys.Remove(key);
        InputManager.ChangeCaptureToggleReset(key);
    }

    public void OnMouseMove(IMouse mouse, Vector2 mousePosition)
    {
        InputManager.IsCaptured(mouse);
        if (InputManager.isCaptured)
            camera.FreeMove(mousePosition);
    }
}