using RedLight;
using RedLight.Entities;
using RedLight.Graphics;
using RedLight.Input;
using RedLight.Scene;
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
    private float cameraSpeed = 2.5f;

    private Player player;
    private Plane plane;
    private Camera playerCamera;
    private Camera debugCamera;
    private bool useDebugCamera = false;

    private List<Entity> ObjectModels = new();

    private int counter = 0;

    public void OnLoad()
    {
        Graphics.Enable();
        Graphics.EnableDebugErrorCallback(); 
        
        PhysicsSystem = new PhysicsSystem();

        plane = new Plane(Graphics, 50f, 20f);
        plane.Translate(new Vector3(0, -0.5f, 0)); // Position slightly below ground level
        plane.Model.AttachTexture(TextureManager.Get("no-texture"));

        var size = Engine.Window.Size;
        camera = new Camera(size);

        var maxwell = Graphics.CreateModel("RedLight.Resources.Models.Maxwell.maxwell_the_cat.glb", "maxwell")
            .SetScale(new Vector3(0.2f))
            .Rotate(float.DegreesToRadians(-90.0f), Vector3.UnitX);

        playerCamera = new Camera(size);
        debugCamera = new Camera(size);
        player = Graphics.MakePlayer(playerCamera, maxwell);
        player.SetPOV(PlayerCameraPOV.ThirdPerson);
        player.SetRotationX(float.DegreesToRadians(-90.0f));

        var cube = new Cube(Graphics, "colliding_cube");
        var cube2 = new Cube(Graphics, "stuck_cube", false);

        ObjectModels.Add(plane);
        ObjectModels.Add(player);
        ObjectModels.Add(cube);
        ObjectModels.Add(cube2);

        foreach (var entity in ObjectModels)
        {
            entity.PhysicsSystem = PhysicsSystem;
            PhysicsSystem.AddEntity(entity);
        }
        
        player.ResetPhysics();

        // Subscribe to collision events
        PhysicsSystem.OnCollisionEnter += OnCollisionEnter;
    }

    public void OnUpdate(double deltaTime)
    {
        counter += 1;
        camera = camera.SetSpeed(cameraSpeed * (float)deltaTime);

        if (InputManager.isCaptured)
            camera.KeyMap(PressedKeys); 
            
        if (PressedKeys.Contains(Key.F2))
        {
            foreach (var entity in ObjectModels)
            {
                entity.ToggleHitbox();
            }
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
            if (counter % 30 == 0)
            {
                Log.Debug("[Player Camera] {CameraYaw}", player.Camera.Yaw);
                Log.Debug("[Player Rotation] {CameraRot}", player.Rotation);
            }
            
            foreach (var entity in ObjectModels)
            {
                if (entity is Player)
                {
                    player.Update((float)deltaTime, PressedKeys);
                }
            }
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

            if (player.IsHitboxShown)
            {
                player.DrawBoundingBox(Graphics, activeCamera);
            }
            if (plane.IsHitboxShown)
            {
                plane.DrawBoundingBox(Graphics, activeCamera);
            }

            foreach (var model in ObjectModels)
            {
                if (model.IsHitboxShown)
                {
                    model.DrawBoundingBox(Graphics, activeCamera);
                }
            }
        }
        Graphics.End();
    }

    public void OnKeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        PressedKeys.Add(key);
        
        switch (key)
        {
            case Key.R:
                player.ResetPhysics();
                break;
            case Key.Escape:
                Engine.Window.Window.Close();
                break;
            case Key.Keypad1:
                Engine.InitialiseLogger(1);
                break;
            case Key.Keypad2:
                Engine.InitialiseLogger(2);
                break;
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

    private void OnCollisionEnter(Entity entityA, Entity entityB, Vector3 contactPoint, Vector3 normal)
    {
        // change this for debug
        bool silent = true;
        
        if (!silent) Log.Debug("[Collision Event] {EntityA} collided with {EntityB} at position {ContactPoint} with normal {Normal}",
            entityA.GetType().Name, entityB.GetType().Name, contactPoint, normal);

        // Add specific collision responses
        if (entityA is Player playerA)
        {
            HandlePlayerCollision(playerA, entityB, contactPoint, normal);
        }
        else if (entityB is Player playerB)
        {
            HandlePlayerCollision(playerB, entityA, contactPoint, normal);
        }

        // Make both entities show their hitboxes when they collide
        entityA.ShowHitbox();
        entityB.ShowHitbox();
    }

    private void HandlePlayerCollision(Player player, Entity otherEntity, Vector3 contactPoint, Vector3 normal)
    {
        // change ts for debug
        bool silent = true;
        
        if (!silent) Log.Information("[Player Collision] Player collided with {OtherEntity}", otherEntity.GetType().Name);

        // example: bouncy
        if (PhysicsSystem.TryGetBodyHandle(player, out var playerHandle))
        {
            var bodyRef = PhysicsSystem.Simulation.Bodies.GetBodyReference(playerHandle);

            // change the val at the end
            Vector3 bounceForce = normal * 0.0f;
            PhysicsSystem.ApplyImpulse(player, bounceForce);

            if (!silent) Log.Debug("[Player Collision] Applied bounce force: {BounceForce}", bounceForce);
        }
    }
}