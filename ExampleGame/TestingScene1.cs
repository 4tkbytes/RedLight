using System.Drawing;
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
using RedLight.Lighting;
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
    public RLEngine Engine { get; set; }
    public HashSet<Key> PressedKeys { get; set; } = new();
    public PhysicsSystem PhysicsSystem { get; set; }
    public LightManager LightManager { get; set; }

    private Player player;
    private Plane plane;
    private Camera playerCamera;
    private Camera debugCamera;
    private bool useDebugCamera;
    
    // private LightingCube lightCube;
    private Sun sun;
    
    private List<Entity> ObjectModels = new();

    private int counter = 0;

    public void OnLoad()
    {
        Graphics.Enable();
        Graphics.EnableDebugErrorCallback();
        
        LightManager = new LightManager();
        
        plane = new Plane(Graphics, 50f, 20f).Default();
        plane.Model.AttachShader(ShaderManager.Get("lit"));
        var size = Engine.Window.Size;

        var maxwell = Graphics.CreateModel("RedLight.Resources.Models.Maxwell.maxwell_the_cat.glb", "maxwell")
            .SetScale(new Vector3(0.05f))
            .Rotate(float.DegreesToRadians(-90.0f), Vector3.UnitX);

        playerCamera = new Camera(size);
        debugCamera = new Camera(size);

        var playerHitbox = HitboxConfig.ForPlayer();
        player = Graphics.MakePlayer(playerCamera, maxwell, playerHitbox);
        player.SetPOV(PlayerCameraPOV.ThirdPerson);
        player.SetRotationX(float.DegreesToRadians(-90.0f));
        player.MoveSpeed = 5f;

        var cube = new Cube(Graphics, "colliding_cube");
        cube.Translate(new Vector3(3f, 10f, 0f));
        cube.FrictionCoefficient = 5.0f;
        
        var cube2 = new Cube(Graphics, "stuck_cube", applyGravity:false);
        cube2.Translate(new Vector3(0f, -0.5f, 0f));
        
        sun = new Sun(Graphics, LightManager, "sun", new Vector3(0.5f, -1f, 0.3f), Color.NavajoWhite);
        sun.Translate(new Vector3(0f, 20f, 0f));
        sun.Light.Intensity = 2.5f;
        
        ObjectModels.Add(plane);
        ObjectModels.Add(player);
        ObjectModels.Add(cube);
        // ObjectModels.Add(cube2);
        // ObjectModels.Add(lightCube.Cube);
        ObjectModels.Add(sun.SunSphere);

        foreach (var entity in ObjectModels)
        {
            entity.PhysicsSystem = PhysicsSystem;
            PhysicsSystem.AddEntity(entity);
        }
        
        player.ResetPhysics();
    }

    public void OnUpdate(double deltaTime)
    {
        counter += 1;

        PhysicsSystem.Update((float)deltaTime);
        
        UpdateDayNightCycle();
        sun.Update();
    
        if (useDebugCamera)
        {
            debugCamera.KeyMap(PressedKeys, (float)deltaTime);
        }

        if (!useDebugCamera)
        {
            player.Update((float)deltaTime, PressedKeys);
        }
    }

    public void OnRender(double deltaTime)
    {
        Graphics.Begin();
        {
            Graphics.Clear();
            Graphics.ClearColour(Color.CornflowerBlue);
            
            // Dynamic background based on sun position
            float sunHeight = sun.SunSphere.Position.Y;
            bool isNight = sunHeight < 10f;
            Color backgroundColor = isNight ? 
                Color.FromArgb(25, 25, 50) : // Dark night sky
                Color.FromArgb(135, 206, 235); // Light day sky
            Graphics.ClearColour(backgroundColor);
            
            Camera activeCamera = useDebugCamera ? debugCamera : player.Camera;

            foreach (var model in ObjectModels)
            {
                if (model == sun.SunSphere)
                {
                    sun.Render(activeCamera);
                    continue;
                }

                Graphics.Use(model);
                LightManager.ApplyLightsToShader("lit", activeCamera.Position);
                Graphics.Update(activeCamera, model);
                Graphics.Draw(model);
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
        bool isNewKeyPress = PressedKeys.Add(key);

        if (isNewKeyPress)
        {
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
                case Key.F6:
                    useDebugCamera = !useDebugCamera;
                    Log.Debug("Debug Camera is set to {A}", useDebugCamera);
                    break;
                case Key.F2:
                    foreach (var entity in ObjectModels) entity.ToggleHitbox();
                    break;
            }
            InputManager.ChangeCaptureToggle(key);
        }
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
    
    private void UpdateDayNightCycle()
    {
        // Calculate sun position (moves in an arc across the sky)
        float sunAngle = (float)(counter * 0.01); // Use counter for consistent movement
    
        // Sun position (moves in an arc across the sky)
        float sunX = MathF.Sin(sunAngle) * 40f;
        float sunY = MathF.Cos(sunAngle) * 20f + 15f; // Keep it above ground
        float sunZ = -30f;
        sun.SunSphere.SetPosition(new Vector3(sunX, sunY, sunZ));
    
        // Update directional light direction to point toward center
        var sunLight = LightManager.GetLight("sun_light");
        if (sunLight != null)
        {
            sunLight.Direction = Vector3.Normalize(-sun.SunSphere.Position); // Point toward origin
            sunLight.Intensity = MathF.Max(0.2f, (sunY - 5f) / 15f); // Vary intensity based on height
        }
    }
}