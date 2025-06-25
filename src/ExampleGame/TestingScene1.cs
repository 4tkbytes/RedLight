using System.Drawing;
using RedLight;
using RedLight.Entities;
using RedLight.Graphics;
using RedLight.Input;
using RedLight.Scene;
using Serilog;
using Silk.NET.Input;
using System.Numerics;
using RedLight.Lighting;
using RedLight.Physics;
using RedLight.UI;
using RedLight.UI.ImGui;
using RedLight.Utils;
using Camera = RedLight.Graphics.Camera;
using Plane = RedLight.Entities.Plane;

namespace ExampleGame;

public class TestingScene1 : RLScene, RLKeyboard, RLMouse
{
    public override RLGraphics Graphics { get; set; }
    public override SceneManager SceneManager { get; set; }
    public override ShaderManager ShaderManager { get; set; }
    public override TextureManager TextureManager { get; set; }
    public override InputManager InputManager { get; set; }
    public override RLEngine Engine { get; set; }
    public HashSet<Key> PressedKeys { get; set; } = new();
    public override PhysicsSystem PhysicsSystem { get; set; }
    public override LightManager LightManager { get; set; }
    public override TextManager TextManager { get; set; }

    private Player player;
    private Plane plane;
    private Camera playerCamera;
    private Camera debugCamera;
    private bool useDebugCamera;
    private RLImGuiEditor _editor;
    private LightingCube lampLight;
    private LightingCube flashLight;
    private LightingCube sunLight;
    private CubeMap skybox;
    private Font font;
    public override void OnLoad()
    {
        Graphics.Enable();
        Graphics.EnableDebugErrorCallback();

        debugCamera = new Camera(Engine.Window.Size);

        InitEditor();
        CreatePlane();
        CreatePlayer();
        var cube = CreateCube();
        CreateLight();
        CreateSkybox();
        
        InitText();

        AddToLists(plane);
        AddToLists(player);
        AddToLists(cube);
        // AddToLists(lampLight);
        // AddToLists(flashLight);
        AddToLists(sunLight);

        player.ResetPhysics();
    }

    public override void OnUpdate(double deltaTime)
    {
        PhysicsSystem.Update((float)deltaTime);

        // lampLight.Update(player.Camera);
        // flashLight.Update(player.Camera);
        sunLight.Update(player.Camera);

        if (useDebugCamera)
        {
            debugCamera.Update((float)deltaTime, PressedKeys);
        }

        if (!useDebugCamera)
        {
            player.Update((float)deltaTime, PressedKeys);
        }

        _editor.Update((float)deltaTime);
    }

    public override void OnRender(double deltaTime)
    {
        Graphics.Clear();
        Camera activeCamera = useDebugCamera ? debugCamera : player.Camera;

        BeforeEditorRender(_editor, activeCamera);

        Graphics.Clear();
        Graphics.ClearColour(Color.CornflowerBlue);

        skybox?.Render(activeCamera);

        RenderModel(activeCamera, skybox);

        LightManager.RenderAllLightCubes(activeCamera);

        foreach (var model in ObjectModels)
        {
            if (model.IsHitboxShown)
            {
                model.DrawBoundingBox(activeCamera);
            }
        }
        
        // Render more text if needed
        TextManager.Instance.RenderText(
            Graphics,
            $"FPS: {(int)Engine.Window.FramesPerSecond}",
            new Vector2(0, 0),
            0.5f,
            Color.Yellow
        );


        AfterEditorRender(_editor);
    }

    public void OnKeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        bool isNewKeyPress = PressedKeys.Add(key);

        if (isNewKeyPress)
        {
            switch (key)
            {
                // absolute basic
                case Key.Escape:
                    Engine.Window.Window.Close();
                    break;
                // keyboard shortcuts
                case Key.R:
                    player.ResetPhysics();
                    break;
                // case Key.F:
                //     flashLight.Light.IsEnabled = !flashLight.Light.IsEnabled;
                //     break;
                case Key.Q:
                    // Toggle fog
                    if (LightManager.Fog?.IsEnabled == true)
                    {
                        LightManager.DisableFog();
                        Log.Information("Fog disabled");
                    }
                    else
                    {
                        LightManager.EnableFog(new Vector3(0.6f, 0.6f, 0.7f), 0.03f, FogType.Exponential);
                        Log.Information("Fog enabled");
                    }
                    break;
                // debug logging keypad
                case Key.Keypad0:
                    Engine.InitialiseLogger(0);
                    break;
                case Key.Keypad1:
                    Engine.InitialiseLogger(1);
                    break;
                case Key.Keypad2:
                    Engine.InitialiseLogger(2);
                    break;
                // func keys
                case Key.F2:
                    foreach (var entity in ObjectModels) entity.ToggleHitbox();
                    break;
                case Key.F6:
                    useDebugCamera = !useDebugCamera;
                    Log.Debug("Debug Camera is set to {A}", useDebugCamera);
                    break;
                case Key.F12:
                    _editor.ToggleEditorMode();
                    Log.Debug("Editor mode toggled via F12: {EditorMode}", _editor.IsEditorMode);
                    break;
            }
            InputManager.ChangeCaptureToggle(key);
        }
    }

    public void OnMouseMove(IMouse mouse, Vector2 mousePosition)
    {
        if (!_editor.IsEditorMode || (_editor.IsEditorMode && _editor.IsViewportFocused))
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

    #region init for docs

    private void InitEditor()
    {
        // create new editor instance
        _editor = new RLImGuiEditor(Graphics, Engine.Window.Window, InputManager.Context, Engine);
        // load it up
        _editor.Load();
    }

    private void CreatePlane()
    {
        // create the plane
        plane = new Plane(Graphics, 50f, 20f).Default();
    }

    private void CreatePlayer()
    {
        // Create a new model
        var maxwell = Graphics.CreateModel("RedLight.Resources.Models.Maxwell.maxwell_the_cat.glb", "maxwell")
            .SetScale(new Vector3(0.05f))
            .Rotate(float.DegreesToRadians(-90.0f), Vector3.UnitX);

        // create a camera for the player
        playerCamera = new Camera(Engine.Window.Size);

        // Create a hitbox config (player default)
        var playerHitbox = HitboxConfig.ForPlayer();
        
        // convert model into player/entity
        player = Graphics.MakePlayer(playerCamera, maxwell, playerHitbox);

        // specific model translations + config
        player.MoveSpeed = 5f;
    }

    private Cube CreateCube()
    {
        // create cube model with name
        var cube = new Cube("colliding_cube");

        // TextureManager.Add("grass", new RLTexture("RedLight.Resources.Textures.grass.png"));

        // cube.Model.AttachTexture(TextureManager.Get("grass"));

        // model translations
        cube.Translate(new Vector3(3f, 10f, 0f));

        // physics system todo: fix this shit up
        cube.FrictionCoefficient = 5.0f;

        // cube.SetReflection(true, 1.0f);
        // cube.SetRefraction(true, RefractiveIndex.Water);

        return cube;
    }

    private void CreateLight()
    {
        // initialise the LightManager class
        LightManager = new LightManager();
        LightManager.SetFog(new Fog());

        // Create any type of light (within the enum)
        // This one is a spotlight, like a flash light
        // flashLight = LightingCube.CreateSpotLightCube(LightManager, "lightCubeSpot", "light_cube", playerCamera,
        //     Color.AntiqueWhite, Attenuation.DefaultValues.Range50);

        // This one is a directional light, such as that of the sun
        sunLight = LightingCube.CreateDirectionalLightCube(LightManager, "lightCubeDirectional", "light_cube",
            RLConstants.RL_SUN_DIRECTION, Color.AntiqueWhite);
        //
        // This one is a point light, like a lamp
        // sunLight = LightingCube.CreatePointLightCube(LightManager, "lightCubePoint", "light_cube", Vector3.Zero,
        //     Color.AntiqueWhite, Attenuation.DefaultValues.Range50); 
    }

    private void CreateSkybox()
    {
        skybox = CubeMap.CreateDefault(Graphics);
    }

    private void InitText()
    {
        try
        {
            // Create font configuration
            var fontConfig = new FontConfig(0, 24);
            
            // Create a new font from your resources - make sure this file exists!
            font = new Font("RedLight.Resources.Fonts.RobotoMono-Regular.ttf", fontConfig);
            
            // Register with TextManager singleton
            TextManager.Instance.AddFont("default", font);
            
            Log.Information("Text rendering initialized successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize text rendering");
        }
    }
    #endregion
}