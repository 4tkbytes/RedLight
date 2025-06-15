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
using Silk.NET.OpenGL;
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

    private Player player;
    private Plane plane;
    private Camera playerCamera;
    private Camera debugCamera;
    private bool useDebugCamera;
    private Cube lightCube;

    private Vector3 lightPosition;

    private List<Entity> ObjectModels = new();

    private int counter = 0;

    public void OnLoad()
    {
        Graphics.Enable();
        Graphics.EnableDebugErrorCallback();

        TextureManager.Add("stone",
            new RLTexture(Graphics, RLFiles.GetResourcePath("ExampleGame.Resources.Textures.576.jpg")));
        
        ShaderManager.TryAdd("lit",
            new RLShader(Graphics, ShaderType.Vertex, RLFiles.GetResourceAsString("RedLight.Resources.Shaders.lit.vert")),
            new RLShader(Graphics, ShaderType.Fragment, RLFiles.GetResourceAsString("RedLight.Resources.Shaders.lit.frag")));
        
        ShaderManager.TryAdd("light_cube",
            new RLShader(Graphics, ShaderType.Vertex, RLFiles.GetResourceAsString("RedLight.Resources.Shaders.light_cube.vert")),
            new RLShader(Graphics, ShaderType.Fragment, RLFiles.GetResourceAsString("RedLight.Resources.Shaders.light_cube.frag")));

        
        plane = new Plane(Graphics, 50f, 20f).Default();
        plane.Model.AttachTexture(TextureManager.Get("stone"));
        plane.Model.AttachShader(ShaderManager.Get("lit"));
        var size = Engine.Window.Size;

        var maxwell = Graphics.CreateModel("RedLight.Resources.Models.Maxwell.maxwell_the_cat.glb", "maxwell")
            .SetScale(new Vector3(0.05f))
            .Rotate(float.DegreesToRadians(-90.0f), Vector3.UnitX);
        maxwell.Target.AttachShader(ShaderManager.Get("lit"));

        playerCamera = new Camera(size);
        debugCamera = new Camera(size);

        var playerHitbox = HitboxConfig.ForPlayer();
        player = Graphics.MakePlayer(playerCamera, maxwell, playerHitbox);
        player.SetPOV(PlayerCameraPOV.ThirdPerson);
        player.SetRotationX(float.DegreesToRadians(-90.0f));
        player.MoveSpeed = 5f;

        var cube = new Cube(Graphics, "colliding_cube");
        cube.Model.AttachShader(ShaderManager.Get("lit"));
        cube.Translate(new Vector3(3f, 10f, 0f));
        
        var cube2 = new Cube(Graphics, "stuck_cube", applyGravity:false);
        cube2.Translate(new Vector3(0f, -0.5f, 0f));    
        cube2.Model.AttachShader(ShaderManager.Get("lit"));
        
        lightCube = new Cube(Graphics, "light_cube");
        lightCube.Model.AttachShader(ShaderManager.Get("light_cube"));
        lightCube.Translate(new Vector3(0f, 5f, 5f));

        ObjectModels.Add(plane);
        ObjectModels.Add(player);
        ObjectModels.Add(cube);
        ObjectModels.Add(cube2);

        foreach (var entity in ObjectModels)
        {
            entity.PhysicsSystem = PhysicsSystem;
            PhysicsSystem.AddEntity(entity);
        }
        
        lightCube.PhysicsSystem = PhysicsSystem;
        PhysicsSystem.AddEntity(lightCube);
        
        player.ResetPhysics();
    }

    public void OnUpdate(double deltaTime)
    {
        counter += 1;

        PhysicsSystem.Update((float)deltaTime);

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
            Graphics.ClearColour(Color.Black);
            
            Camera activeCamera = useDebugCamera ? debugCamera : player.Camera;

            foreach (var model in ObjectModels)
            {
                Graphics.Use(model);
                ShaderManager.SetUniform("lit", "lightPos", lightCube.Position);
                ShaderManager.SetUniform("lit", "lightColor", new Vector3(1f, 1f, 1f));
                Graphics.Update(activeCamera, model);
                Graphics.Draw(model);
            }
            
            Graphics.Use(lightCube);
            
            ShaderManager.SetUniform("light_cube", "lightColor", new Vector3(100f));
            
            Graphics.Update(activeCamera, lightCube);
            Graphics.Draw(lightCube);
            
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
}