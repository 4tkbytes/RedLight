using RedLight.Core;
using RedLight.Graphics;
using RedLight.Input;

namespace RedLight.Scene;

public class SceneManager
{
    private Dictionary<string, RLScene> scenes;
    internal InputManager input;
    private RLEngine engine;
    private ShaderManager shaderManager;
    private TextureManager textureManager;

    private string currentSceneId;
    private RLScene currentScene;
    private RLKeyboard currentKeyboard;
    private RLMouse currentMouse;

    internal bool coconutToggle = false;

    public SceneManager(RLEngine engine, ShaderManager shaderManager, TextureManager textureManager)
    {
        scenes = new Dictionary<string, RLScene>();
        input = new InputManager(engine.Window);
        this.engine = engine;
        this.shaderManager = shaderManager;
        this.textureManager = textureManager;

        FPSCounter();
    }

    public void Add(string id, RLScene scene, RLKeyboard keyboard, RLMouse mouse)
    {
        if (scenes.ContainsKey(id))
        {
            throw new Exception($"ID [{id}] is already registered");
        }

        scenes.Add(id, scene);

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

    public void SwitchScene(RLScene scene)
    {
        var thing = scenes.FirstOrDefault(x => x.Value == scene).Key;
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
        if (!scenes.ContainsKey(id))
        {
            throw new Exception($"ID [{id}] does not exist");
        }

        if (currentScene != null)
        {
            engine.Window.UnsubscribeFromEvents(currentScene);
            if (currentKeyboard != null && currentMouse != null)
            {
                input.UnsubscribeFromInputs(currentKeyboard, currentMouse);
            }
        }

        currentSceneId = id;
        currentScene = scenes[id];

        currentKeyboard = input.Keyboards[id];
        currentMouse = input.Mice[id];

        engine.Window.SubscribeToEvents(currentScene);

        currentScene.Engine = engine;
        currentScene.Graphics = engine.Graphics;
        currentScene.SceneManager = this;
        currentScene.ShaderManager = shaderManager;
        currentScene.TextureManager = textureManager;
        currentScene.InputManager = input;

        input.SubscribeToInputs(currentKeyboard, currentMouse);

        currentScene.OnLoad();
    }

    private void FPSCounter()
    {
        engine.Window.Window.Update += (double deltaTime) =>
        {
            engine.Window.FramesPerSecond = 1.0 / deltaTime;
        };
    }

    public void Remove(string id)
    {
        if (currentSceneId == id)
        {
            throw new Exception("Cannot remove the currently active scene");
        }
        scenes.Remove(id);
    }

    public RLScene GetCurrentScene()
    {
        return currentScene;
    }
}