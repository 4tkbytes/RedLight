using RedLight;
using RedLight.Entities;
using RedLight.Graphics;
using RedLight.Input;
using RedLight.Scene;
using RedLight.UI;
using RedLight.Utils;
using Serilog;
using Silk.NET.Input;
using System.Numerics;
using RedLight.Graphics.Primitive;
using Camera = RedLight.Graphics.Camera;
using Plane = RedLight.Graphics.Primitive.Plane;

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
    public PhysicsSystem PhysicsSystem { get; set; }

    private Camera camera;
    private RLImGui controller;
    private float cameraSpeed = 2.5f;

    private Player player;
    private Plane plane;
    private Camera playerCamera;
    private Camera debugCamera;
    private bool useDebugCamera = false;

    private List<Entity<Transformable<RLModel>>> ObjectModels = new();

    private int counter = 0;

    public void OnLoad()
    {
        Graphics.Enable();
        Graphics.EnableDebugErrorCallback();

        // Initialize physics system
        PhysicsSystem = new PhysicsSystem();

        plane = new Plane(Graphics, 20f, 20f).Default();

        controller = new RLImGui(Graphics, Engine.Window);
        Engine.InitialiseLogger(controller.Console);

        var size = Engine.Window.Size;
        camera = new Camera(size);

        var maxwell = Graphics.CreateModel("RedLight.Resources.Models.Maxwell.maxwell_the_cat.glb", "maxwell")
            .Rotate(float.DegreesToRadians(-90.0f), Vector3.UnitX)
            .SetScale(new Vector3(0.05f, 0.05f, 0.05f));

        var cube = new Cube(Graphics, TextureManager, ShaderManager, "collision_cube", false);
        cube.Translate(new Vector3(1));
        playerCamera = new Camera(size);
        debugCamera = new Camera(size);

        player = Graphics.MakePlayer(playerCamera, maxwell);
        player.SetPOV(PlayerCameraPOV.ThirdPerson);

        ObjectModels.Add(cube);
        ObjectModels.Add(plane);
        ObjectModels.Add(player);

        // Initialize physics for all entities
        foreach (var entity in ObjectModels)
        {
            entity.InitPhysics(PhysicsSystem);
        }
    }

    public void OnUpdate(double deltaTime)
    {
        counter += 1;
        camera = camera.SetSpeed(cameraSpeed * (float)deltaTime);

        if (InputManager.isCaptured)
            camera.KeyMap(PressedKeys);

        if (PressedKeys.Contains(Key.F2))
        {
            player.ToggleHitbox();
            plane.ToggleHitbox();
        }        
        if (PressedKeys.Contains(Key.F6))
        {
            useDebugCamera = !useDebugCamera;
            Log.Debug("Debug Camera is set to {A}", useDebugCamera);
        }

        PhysicsSystem.Update((float)deltaTime);

        if (useDebugCamera)
        {
            debugCamera = debugCamera.SetSpeed(cameraSpeed * (float)deltaTime);
            if (InputManager.isCaptured)
                debugCamera.KeyMap(PressedKeys);
        }

        if (!useDebugCamera)
        {
            player.Update(PressedKeys, (float)deltaTime);
            plane.Update((float)deltaTime);
        }
    }

    public void OnRender(double deltaTime)
    {
        Graphics.Begin();
        {
            Graphics.Clear();
            Graphics.ClearColour(RLConstants.RL_COLOUR_CORNFLOWER_BLUE);

            Camera activeCamera = useDebugCamera ? debugCamera : player.Camera;
            foreach (var model in ObjectModels)
            {
                Graphics.Use(model.Target);
                Graphics.Update(activeCamera, model.Target);
                Graphics.Draw(model.Target);
            }

            if (player.IsHitboxShown)
            {
                player.DrawBoundingBox(Graphics, ShaderManager.Get("hitbox"), activeCamera);
            }
            if (plane.IsHitboxShown)
            {
                plane.DrawBoundingBox(Graphics, ShaderManager.Get("hitbox"), activeCamera);
            }
        }
        Graphics.End();

        controller.Render(deltaTime, useDebugCamera ? debugCamera : player.Camera);
    }

    public void OnKeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        PressedKeys.Add(key);
        if (key == Key.Escape)
        {
            Engine.Window.Window.Close();
        }
        if (key == Key.R)
        {
            player.ResetPhysics();
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
        {
            if (useDebugCamera)
                debugCamera.FreeMove(mousePosition);
            else
                player.Camera.FreeMove(mousePosition);
        }
    }
}