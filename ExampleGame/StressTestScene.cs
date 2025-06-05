using System.Numerics;
using RedLight;
using RedLight.Graphics;
using RedLight.Graphics.Primitive;
using RedLight.Input;
using RedLight.Scene;
using RedLight.Utils;
using Serilog;
using Silk.NET.Input;

namespace ExampleGame;

public class StressTestScene : RLScene, RLKeyboard, RLMouse
{
    public RLEngine Engine { get; set; }
    public RLGraphics Graphics { get; set; }
    public SceneManager SceneManager { get; set; }
    public ShaderManager ShaderManager { get; set; }
    public TextureManager TextureManager { get; set; }
    public InputManager InputManager { get; set; }

    public HashSet<Key> PressedKeys { get; set; } = new();

    private Camera camera;
    private List<Transformable<RLModel>> objectModels = new();
    private bool isCaptured = false;

    private double objectSpawnInterval = 0.01;
    private double timeSinceLastSpawn = 0.0;

    public void OnLoad()
    {
        Graphics.Enable();

        ShaderManager.TryAdd(
            "basic",
            new RLShader(Graphics, ShaderType.Vertex, RLConstants.RL_BASIC_SHADER_VERT),
            new RLShader(Graphics, ShaderType.Fragment, RLConstants.RL_BASIC_SHADER_FRAG)
        );

        camera = new Camera(Engine.Window.Window.Size);
    }

    public void OnUpdate(double deltaTime)
    {
        camera = camera.SetSpeed(2.5f * (float)deltaTime);

        camera.KeyMap(PressedKeys);

        if (PressedKeys.Contains(Key.Number1))
            SceneManager.SwitchScene("testing_scene_1");

        // Object spawning logic
        timeSinceLastSpawn += deltaTime;
        if (timeSinceLastSpawn >= objectSpawnInterval)
        {
            // Add a new object at a random position
            var newCube = new Cube(Graphics, TextureManager, ShaderManager).Model;

            Random rand = new Random();
            float x = (rand.NextSingle() - 0.5f) * 20f; // Random X between -10 and 10
            float y = (rand.NextSingle() - 0.5f) * 20f; // Random Y between -10 and 10
            float z = (rand.NextSingle() - 0.5f) * 20f; // Random Z between -10 and 10

            newCube.Translate(new Silk.NET.Maths.Vector3D<float>(x, y, z));

            objectModels.Add(newCube);
            timeSinceLastSpawn = 0.0;

            Log.Debug("Added new object. Total objects: {Count}", objectModels.Count);
        }

        if (PressedKeys.Contains(Key.Equal)) // makes spawn rate faster
        {
            objectSpawnInterval = Math.Max(0.1, objectSpawnInterval - 0.1);
            Log.Debug("Spawn interval: {Interval:F1}s", objectSpawnInterval);
        }
        if (PressedKeys.Contains(Key.Minus)) // makes spawn rate slower
        {
            objectSpawnInterval += 0.1;
            Log.Debug("Spawn interval: {Interval:F1}s", objectSpawnInterval);
        }
    }

    public void OnRender(double deltaTime)
    {
        Graphics.Begin();

        Graphics.Clear();
        Graphics.ClearColour(RLConstants.RL_COLOUR_CORNFLOWER_BLUE);

        int counter = 0;
        foreach (var model in objectModels)
        {
            Graphics.Use(model);
            Graphics.Update(camera, model);
            Graphics.Draw(model);
            counter++;
        }

        Graphics.End();
        Log.Debug("Objects rendered: {A} | FPS Count: {B:F2} | Spawn Interval: {C:F1}s",
            counter, Engine.Window.FramesPerSecond, objectSpawnInterval);
    }

    public void OnMouseMove(IMouse mouse, Vector2 mousePosition)
    {
        Graphics.IsCaptured(mouse, isCaptured);
        if (isCaptured)
            camera.FreeMove(mousePosition);
    }

    public void OnKeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        PressedKeys.Add(key);
    }

    public void OnKeyUp(IKeyboard keyboard, Key key, int keyCode)
    {
        PressedKeys.Remove(key);
    }
}