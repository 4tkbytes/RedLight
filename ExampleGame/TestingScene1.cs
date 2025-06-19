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
    private Sun sun;
    private List<Entity> ObjectModels = new();
    
    public void OnLoad()
    {
        Graphics.Enable();
        Graphics.EnableDebugErrorCallback();
        
        LightManager = new LightManager();
        
        _editor = new RLImGuiEditor(Graphics, Engine.Window.Window, InputManager.Context, Engine);
        _editor.Load();
        
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
        ObjectModels.Add(sun.SunSphere);
        
        foreach (var entity in ObjectModels)
        {
            entity.PhysicsSystem = PhysicsSystem;
            if (entity == sun.SunSphere)
                continue;
            PhysicsSystem.AddEntity(entity);
        }
        
        player.ResetPhysics();
    }

    public void OnUpdate(double deltaTime)
    {
        PhysicsSystem.Update((float)deltaTime);
        
        sun.Update();
    
        if (useDebugCamera)
        {
            debugCamera.KeyMap(PressedKeys, (float)deltaTime);
        }

        if (!useDebugCamera)
        {
            player.Update((float)deltaTime, PressedKeys);
        }
        
        _editor.Update((float)deltaTime);
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
}