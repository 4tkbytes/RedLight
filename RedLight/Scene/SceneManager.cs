using RedLight.Graphics;
using RedLight.Input;
using RedLight.Utils;
using Serilog;
using ShaderType = RedLight.Graphics.ShaderType;

namespace RedLight.Scene;

public class SceneManager
{
    public Dictionary<string, RLScene> Scenes { get; }
    internal InputManager input;
    private RLEngine engine;
    private ShaderManager shaderManager;
    private TextureManager textureManager;

    private string currentSceneId;
    private RLScene currentScene;
    private RLKeyboard currentKeyboard;
    private RLMouse currentMouse;

    private static SceneManager _instance;
    public static SceneManager Instance
    {
        get
        {
            if (_instance == null)
                throw new InvalidOperationException("SceneManager is not initialised. Call SceneManager.Initialise() first.");
            return _instance;
        }
    }

    public static void Initialise(RLEngine engine)
    {
        if (_instance != null)
            throw new InvalidOperationException("SceneManager is already initialized.");
        _instance = new SceneManager(engine);
    }

    private SceneManager(RLEngine engine)
    {
        Scenes = new Dictionary<string, RLScene>();
        input = InputManager.Instance;
        this.engine = engine;
        shaderManager = ShaderManager.Instance;
        textureManager = TextureManager.Instance;

        FPSCounter();
    }

    public void Add(string id, RLScene scene, RLKeyboard keyboard, RLMouse mouse)
    {
        if (Scenes.ContainsKey(id))
        {
            throw new Exception($"ID [{id}] is already registered");
        }

        Scenes.Add(id, scene);

        input.Keyboards.Add(id, keyboard);
        input.Mice.Add(id, mouse);

        if (currentScene == null)
        {
            currentSceneId = id;
            currentScene = scene;
            currentKeyboard = keyboard;
            currentMouse = mouse;
        }
    }

    public void Add(string id, RLScene scene)
    {
        Add(id, scene, scene as RLKeyboard, scene as RLMouse);
    }

    public void SwitchScene(RLScene scene)
    {
        var thing = Scenes.FirstOrDefault(x => x.Value == scene).Key;
        if (thing != null)
        {
            SwitchScene(thing);
        }
        else
        {
            throw new Exception($"Scene [{scene}] is not registered");
        }
    }

    public void SwitchScene(string id)
    {
        if (!Scenes.ContainsKey(id))
        {
            throw new Exception($"ID [{id}] does not exist");
        }

        if (currentScene != null)
        {
            Log.Debug("Current scene is not null");
            engine.Window.UnsubscribeFromEvents(currentScene);
            Log.Debug("Unsubscribing from events");
            if (currentKeyboard != null && currentMouse != null)
            {
                Log.Debug("Unsubscribing from keyboard and mouse events");
                input.UnsubscribeFromInputs(currentKeyboard, currentMouse);
            }
            else
            {
                Log.Error("Current keyboard and current mouse is null");
            }
        }
        else
        {
            Log.Error("Current scene is null");
        }

        currentSceneId = id;
        Log.Debug("Scene ID: {A}", currentSceneId);
        currentScene = Scenes[id];
        Log.Debug("Scene: {A}", currentScene);

        currentKeyboard = input.Keyboards[id];
        Log.Debug("Keyboard: {A}", currentKeyboard);
        currentMouse = input.Mice[id];
        Log.Debug("Mice: {A}", currentMouse);

        engine.Window.SubscribeToEvents(currentScene);
        Log.Debug("Subscribing to window events");

        currentScene.Engine = engine;
        if (currentScene.Engine == null)
        {
            Log.Error("Engine is null");
        }
        currentScene.Graphics = engine.Graphics;
        if (currentScene.Graphics == null)
        {
            Log.Error("Graphics is null");
        }
        currentScene.SceneManager = this;
        if (currentScene.SceneManager == null)
        {
            Log.Error("SceneManager is null");
        }

        currentScene.ShaderManager = shaderManager;
        if (currentScene.ShaderManager == null)
        {
            Log.Error("ShaderManager is null");
        }
        currentScene.TextureManager = textureManager;
        if (currentScene.TextureManager == null)
        {
            Log.Error("TextureManager is null");
        }
        currentScene.InputManager = input;
        if (currentScene.InputManager == null)
        {
            Log.Error("InputManager is null");
        }
        if (currentScene.PhysicsSystem == null)
        {
            Log.Debug("Creating new physics system for scene");
            currentScene.PhysicsSystem = new PhysicsSystem();
            Log.Debug("Physics system created and assigned to scene: {IsNull}", currentScene.PhysicsSystem == null);
        }

        // default no texture path
        currentScene.TextureManager.TryAdd(
            "no-texture",
            new RLTexture(currentScene.Graphics, RLFiles.GetResourcePath(RLConstants.RL_NO_TEXTURE_PATH))
        );

        // basic shader, unlit
        currentScene.ShaderManager.TryAdd("basic",
            new RLShader(currentScene.Graphics, ShaderType.Vertex, RLConstants.RL_BASIC_SHADER_VERT),
            new RLShader(currentScene.Graphics, ShaderType.Fragment, RLConstants.RL_BASIC_SHADER_FRAG));

        // hitbox shader for rendering hitboxes
        currentScene.ShaderManager.TryAdd("hitbox",
            new RLShader(currentScene.Graphics, ShaderType.Vertex, RLFiles.GetResourceAsString("RedLight.Resources.Shaders.hitbox.vert")),
            new RLShader(currentScene.Graphics, ShaderType.Fragment, RLFiles.GetResourceAsString("RedLight.Resources.Shaders.hitbox.frag")));
        
        // basic shader with lighting support
        currentScene.ShaderManager.TryAdd("lit",
            new RLShader(currentScene.Graphics, ShaderType.Vertex, RLFiles.GetResourceAsString("RedLight.Resources.Shaders.lit.vert")),
            new RLShader(currentScene.Graphics, ShaderType.Fragment, RLFiles.GetResourceAsString("RedLight.Resources.Shaders.lit.frag")));
        
        // lighting cube
        currentScene.ShaderManager.TryAdd("light_cube",
            new RLShader(currentScene.Graphics, ShaderType.Vertex, RLFiles.GetResourceAsString("RedLight.Resources.Shaders.light_cube.vert")),
            new RLShader(currentScene.Graphics, ShaderType.Fragment, RLFiles.GetResourceAsString("RedLight.Resources.Shaders.light_cube.frag")));

        Log.Debug("Subscribing to keyboard events");
        input.SubscribeToInputs(currentKeyboard, currentMouse);
        
        Log.Debug("Loading current scene");
        currentScene.Load();
        
        currentScene.OnLoad();
        Log.Debug("Scene fully initialized");
    }

    private void FPSCounter()
    {
        engine.Window.Window.Update += (double deltaTime) =>
        {
            engine.Window.FramesPerSecond = 1.0 / deltaTime;
            engine.Window.Window.Title = engine.title + " | " + engine.Window.FramesPerSecond.ToString("F2");
        };
    }

    public void Remove(string id)
    {
        if (currentSceneId == id)
        {
            throw new Exception("Cannot remove the currently active scene");
        }
        Scenes.Remove(id);
    }

    public RLScene GetCurrentScene()
    {
        return currentScene;
    }
}

public enum LoadingState
{
    Loading,
    Completed,
}