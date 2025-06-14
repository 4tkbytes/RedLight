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

    private Player player;
    private Plane plane;
    private Camera playerCamera;
    private Camera debugCamera;
    private bool useDebugCamera = false;

    private List<Entity> ObjectModels = new();
    private RLLight playerLamp;

    private int counter = 0;
    private bool justBeQuiet = true;

    public void OnLoad()
    {
        Graphics.Enable();
        Graphics.EnableDebugErrorCallback();
        
        // SIMPLE, WORKING LIGHTING - No crazy values
        LightManager.Instance.Clear();
    
        // Simple sun - just like daylight
        var sun = new RLLight
        {
            Type = LightType.Directional,
            Direction = new Vector3(-0.2f, -1.0f, -0.3f),
            Colour = new Vector3(1.0f, 1.0f, 1.0f), // Pure white light
            Intensity = 1.0f // Normal intensity
        };
        LightManager.Instance.Add(sun);
    
        // Simple player lamp - just enough to see nearby
        playerLamp = new RLLight
        {
            Type = LightType.Point,
            Position = Vector3.Zero,
            Colour = new Vector3(1.0f, 0.9f, 0.8f), // Slightly warm white
            Intensity = 1.0f, // Normal intensity
            Constant = 1.0f,
            Linear = 0.09f,
            Quadratic = 0.032f,
            Range = 10.0f
        };
        LightManager.Instance.Add(playerLamp);

        TextureManager.TryAdd("stone",
            new RLTexture(Graphics, RLFiles.GetResourcePath("ExampleGame.Resources.Textures.576.jpg")));

        plane = new Plane(Graphics, 50f, 20f).Default();
        plane.Model.AttachTexture(TextureManager.Get("stone"));

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
        var cube2 = new Cube(Graphics, "stuck_cube", applyGravity:false);

        ObjectModels.Add(plane);
        ObjectModels.Add(player);
        ObjectModels.Add(cube);
        ObjectModels.Add(cube2);

        // Debug: List all available shaders
        Log.Debug("Available shaders in ShaderManager:");
        foreach (var shaderName in ShaderManager.GetAll()) // You might need to implement this method
        {
            Log.Debug("- {ShaderName}", shaderName);
        }

        // Debug: Check if lit shader exists
        bool litShaderExists = false;
        try
        {
            var litShader = ShaderManager.Get("lit");
            if (litShader.VertexShader != null)
            {
                litShaderExists = true;
                Log.Debug("Lit shader found and loaded successfully");
            }
            else
            {
                Log.Warning("Lit shader is null");
            }
        }
        catch (Exception ex)
        {
            Log.Error("Failed to get lit shader: {Error}", ex.Message);
        }

        foreach (var entity in ObjectModels)
        {
            Log.Debug("Entity {Name} initial shader: {ShaderName}", entity.Name ?? "Unknown", entity.Model.Shader.Name);
            
            if (entity.Model.Shader.Name == "basic")
            {
                if (litShaderExists)
                {
                    try
                    {
                        entity.Model.AttachShader(ShaderManager.Get("lit"));
                        Log.Debug("Successfully switched entity {Name} to lit shader", entity.Name ?? "Unknown");
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Failed to switch entity {Name} to lit shader: {Error}", entity.Name ?? "Unknown", ex.Message);
                    }
                }
                else
                {
                    Log.Warning("Cannot switch entity {Name} to lit shader - shader not available", entity.Name ?? "Unknown");
                }
            }
            
            Log.Debug("Entity {Name} final shader: {ShaderName}", entity.Name ?? "Unknown", entity.Model.Shader.Name);
            PhysicsSystem.AddEntity(entity);
        }

        // Debug: Check total lights in manager
        var allLights = LightManager.Instance.GetLights();
        Log.Debug("Total lights in LightManager: {Count}", allLights.Count);
        foreach (var light in allLights)
        {
            Log.Debug("Light: Type={Type}, Intensity={Intensity}, Color={Color}", 
                light.Type, light.Intensity, light.Colour);
        }
        
        player.ResetPhysics();
    }

    public void OnUpdate(double deltaTime)
    {
        if (playerLamp != null)
        {
            var newPosition = player.Position + new Vector3(0, 2.0f, 0);
            playerLamp.Position = newPosition;
            
            // Debug lamp position every few frames to avoid spam
            counter++;
            if (counter % 60 == 0) // Every 60 frames
            {
                Log.Debug("Player lamp position: {Position}, Player position: {PlayerPos}", 
                    playerLamp.Position, player.Position);
            }
        }

        PhysicsSystem.Update((float)deltaTime);
        if (useDebugCamera)
        {
            debugCamera = debugCamera.SetSpeed(debugCamera.Speed * (float)deltaTime);
            if (InputManager.isCaptured)
                debugCamera.KeyMap(PressedKeys);
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

            Camera activeCamera = useDebugCamera ? debugCamera : player.Camera;
            
            // Debug camera position every few frames
            if (counter % 120 == 0) // Every 120 frames
            {
                Log.Debug("Active camera position: {Position}", activeCamera.Position);
            }
            
            foreach (var model in ObjectModels)
            {
                Graphics.Use(model);
                
                // Debug: Check if Update method is applying lighting
                if (counter % 120 == 0)
                {
                    Log.Debug("Rendering entity {Name} with shader {ShaderName}", 
                        model.Name ?? "Unknown", model.Model.Shader.Name);
                }
                
                Graphics.UpdateAlt(activeCamera, model);
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
                case Key.Keypad0:
                    Engine.InitialiseLogger(0);
                    break;
                case Key.F2:
                    foreach (var entity in ObjectModels)
                    {
                        entity.ToggleHitbox();
                    }
                    break;
                case Key.F3: // Debug key to test lighting manually
                    Log.Debug("F3 pressed - Testing lighting system");
                    var lights = LightManager.Instance.GetLights();
                    Log.Debug("Found {Count} lights", lights.Count);
                    foreach (var light in lights)
                    {
                        Log.Debug(
                            "Light details: Type={Type}, Position={Position}, Direction={Direction}, Color={Color}, Intensity={Intensity}",
                            light.Type, light.Position, light.Direction, light.Colour, light.Intensity);
                    }
                    break;
                case Key.F4: // Debug key to check shader uniforms
                    Log.Debug("F4 pressed - Checking shader uniforms");
                    foreach (var entity in ObjectModels)
                    {
                        Log.Debug("Entity {Name}: Shader={ShaderName}", entity.Name ?? "Unknown",
                            entity.Model.Shader.Name == null ? "No shader" : entity.Model.Shader.Name);

                        // Check what uniforms are available in each mesh
                        foreach (var mesh in entity.Model.Meshes)
                        {
                            Log.Debug("  Mesh program: {Program}", mesh.program);
                            Graphics.CheckUniformsInProgram(mesh.program);
                        }
                    }
                    break;
                case Key.F6: // debug camera
                    useDebugCamera = !useDebugCamera;
                    Log.Debug("Debug Camera is set to {A}", useDebugCamera);
                    break;
                
                case Key.Number1: // Directional only
                    LightingTests.TestDirectionalOnly();
                    break;
                case Key.Number2: // Point light only
                    LightingTests.TestPointLightOnly(player);
                    break;
                case Key.Number3: // Colored lights
                    LightingTests.TestColoredLights();
                    break;
                case Key.Number4: // Dynamic follow light
                    LightingTests.TestDynamicFollowLight(playerLamp, player);
                    break;
                case Key.Number5: // Multiple point lights
                    LightingTests.TestMultiplePointLights();
                    break;
                case Key.Number6: // Realistic daylight
                    LightingTests.TestRealisticDaylight();
                    break;
                case Key.Number0: // Reset to default
                    OnLoad(); // Reload the scene with default lighting
                    Log.Debug("Reset to default lighting");
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
}