using RedLight;
using RedLight.Graphics;
using RedLight.Graphics.Primitive;
using RedLight.Input;
using RedLight.Scene;
using RedLight.UI;
using RedLight.Utils;
using Serilog;
using Silk.NET.Input;
using Silk.NET.Maths;
using System.Numerics;
using RedLight.Physics;
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
    public List<Transformable<RLModel>> ObjectModels { get; set; } = new();
    public RLEngine Engine { get; set; }
    public HashSet<Key> PressedKeys { get; set; } = new();

    private Camera camera;
    private RLImGui controller;
    private float cameraSpeed = 2.5f;

    private Player player;
    private Plane plane;
    private Camera playerCamera;
    private Camera debugCamera;
    private bool useDebugCamera = false;

    public void OnLoad()
    {
        Graphics.Enable();
        Graphics.EnableDebugErrorCallback();

        plane = new Plane(Graphics, TextureManager, ShaderManager, 20f, 20f).Default();

        controller = new RLImGui(Graphics, Engine.Window, InputManager, ShaderManager, TextureManager, SceneManager);
        Engine.InitialiseLogger(controller.Console);

        var size = Engine.Window.Window.Size;
        camera = new Camera(size);

        var maxwell = Graphics.CreateModel("RedLight.Resources.Models.Maxwell.maxwell_the_cat.glb", TextureManager, ShaderManager, "maxwell")
            .Rotate(float.DegreesToRadians(-90.0f), Vector3D<float>.UnitX)
            .SetScale(new Vector3D<float>(0.05f, 0.05f, 0.05f));

        playerCamera = new Camera(size);
        debugCamera = new Camera(size);
        
        player = Graphics.MakePlayer(playerCamera, maxwell);
        player.SetPOV(PlayerCameraPOV.ThirdPerson);

        var cube = new Cube(Graphics, TextureManager, ShaderManager, "test_cube");
        cube.Target.AttachTexture(new RLTexture(Graphics, "RedLight.Resources.Textures.thing.png"));
        cube.Translate(new Vector3D<float>(1f));
        
        Graphics.AddModels(ObjectModels, controller, plane);
        Graphics.AddModels(ObjectModels, controller, player);
        Graphics.AddModels(ObjectModels, controller, cube);
        shitfuck.Add(plane);
    }

    List<Entity<RLModel>> shitfuck = new();
    // public void OnUpdate(double deltaTime)
    // {
    //     camera = camera.SetSpeed(cameraSpeed * (float)deltaTime);
    //
    //     if (InputManager.isCaptured)
    //         camera.KeyMap(PressedKeys, player);
    //
    //     if (PressedKeys.Contains(Key.F2))
    //     {
    //         player.ToggleHitbox();
    //         plane.ToggleHitbox();
    //     }
    //     if (PressedKeys.Contains(Key.F5))
    //     {
    //         // first person doesnt work for shit we gotta work on that
    //         
    //         // player.ToggleCamera();
    //         // Log.Debug("Camera POV has been toggled to {A}", player.CameraToggle);
    //     }
    //     if (PressedKeys.Contains(Key.F6))
    //     {
    //         useDebugCamera = !useDebugCamera;
    //         Log.Debug("Debug Camera is set to {A}", useDebugCamera);
    //     }
    //     
    //     if (useDebugCamera)
    //     {
    //         debugCamera = debugCamera.SetSpeed(cameraSpeed * (float)deltaTime);
    //         if (InputManager.isCaptured)
    //             debugCamera.KeyMap(PressedKeys);
    //     }
    //     
    //     if (!useDebugCamera)
    //     {
    //         player.Update(PressedKeys, (float)deltaTime);
    //         plane.Update((float)deltaTime);
    //
    //         player.CheckCollisionAndResolve(shitfuck, false);
    //     }
    // }
    
    public void OnUpdate(double deltaTime)
    {
        // Update non-player entities first
        plane.Update((float)deltaTime);
        
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

        if (!useDebugCamera)
        {
            // First, check for collisions with existing position
            player.CheckCollisionAndResolve(shitfuck, false);

            // Then handle input (using collision sides from previous check)
            var oldPosition = player.Position;
        
            // Update player with the current collision information
            player.Update(PressedKeys, (float)deltaTime);
        
            // Check collisions again after movement
            player.CheckCollisionAndResolve(shitfuck, false);
        }
        else
        {
            // Debug camera movement
            debugCamera.KeyMap(PressedKeys);
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
                Graphics.Use(model);
                Graphics.Update(activeCamera, model);
                Graphics.Draw(model);
            }            
            
            if (player.isHitboxShown)
            {
                player.DrawBoundingBox(Graphics, ShaderManager.Get("hitbox"), activeCamera);
            }
            if (plane.isHitboxShown)
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