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
using RedLight.UI;
using Camera = RedLight.Graphics.Camera;
using Plane = RedLight.Entities.Plane;

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
    private RLImGuiEditor _editor;
    private LightingCube lightingCube;
    private List<Entity> ObjectModels = new();
    
    public void OnLoad()
    {
        Graphics.Enable();
        Graphics.EnableDebugErrorCallback();
        
        debugCamera = new Camera(Engine.Window.Size);
        
        InitEditor();
        CreatePlane();
        CreatePlayer();

        var cube = CreateCube();

        CreateLight();
        
        ObjectModels.Add(plane);
        ObjectModels.Add(player);
        ObjectModels.Add(cube);
        ObjectModels.Add(lightingCube.Cube);
        
        AddPhysics();
        
        player.ResetPhysics();
    }

    private int counter;
    public void OnUpdate(double deltaTime)
    {
        PhysicsSystem.Update((float)deltaTime);
        
        lightingCube.Update();
    
        if (useDebugCamera)
        {
            debugCamera.KeyMap(PressedKeys, (float)deltaTime);
        }

        if (!useDebugCamera)
        {
            player.Update((float)deltaTime, PressedKeys);
        }
        
        _editor.Update((float)deltaTime);
        counter++;
    }

    public void OnRender(double deltaTime)
    {
        Graphics.Clear();
        
        Camera activeCamera = useDebugCamera ? debugCamera : player.Camera;

        if (_editor.IsEditorMode)
        {
            _editor.SetModelList(ObjectModels);
        }

        if (_editor.IsEditorMode)
        {
            _editor.GameFramebuffer.Bind();
            
            var viewportSize = _editor.ViewportSize;
            if (viewportSize.X > 0 && viewportSize.Y > 0)
            {
                activeCamera.UpdateAspectRatio(viewportSize.X / viewportSize.Y);
            }
        }
        else
        {
            var windowSize = Engine.Window.Window.FramebufferSize;
            if (windowSize.X > 0 && windowSize.Y > 0)
            {
                activeCamera.UpdateAspectRatio((float)windowSize.X / windowSize.Y);
            }
        }
        
        Graphics.Clear();
        Graphics.ClearColour(Color.CornflowerBlue);

        foreach (var model in ObjectModels)
        {
            if (model == lightingCube.Cube)
            {
                lightingCube.Render(activeCamera);
                continue;
            }

            Graphics.Use(model);
            LightManager.ApplyLightsToShader(activeCamera.Position);
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
        
        if (_editor.IsEditorMode)
        {
            _editor.GameFramebuffer.Unbind();
            
            Graphics.OpenGL.Viewport(Engine.Window.Window.FramebufferSize);
        }
        
        _editor.Render();
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
        player.SetRotationX(float.DegreesToRadians(-90.0f));
        player.MoveSpeed = 5f;
    }

    private Cube CreateCube()
    {
        // create cube model with name
        var cube = new Cube(Graphics, "colliding_cube");
        
        // model translations
        cube.Translate(new Vector3(3f, 10f, 0f));
        
        // physics system todo: fix this shit up
        cube.FrictionCoefficient = 5.0f;

        return cube;
    }

    private void CreateLight()
    {
        // initialise the LightManager class
        LightManager = new LightManager();

        // create new light cube
        lightingCube = new LightingCube(Graphics, LightManager, "lightCube", "light_cube", Color.White, LightType.Point);

        // set intensity
        lightingCube.Light.Intensity = 2.5f;
    }

    private void AddPhysics()
    {
        // iterate through entity list
        foreach (var entity in ObjectModels)
        {
            // init entitys physics system
            entity.PhysicsSystem = PhysicsSystem;
            
            // skip if type is a light
            if (entity == lightingCube.Cube)
                continue;
            
            // add the entity to the physics system
            PhysicsSystem.AddEntity(entity);
        }
    }
    #endregion
}