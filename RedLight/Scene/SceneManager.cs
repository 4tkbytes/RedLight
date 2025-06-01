using RedLight.Core;
using RedLight.Graphics;
using RedLight.Input;

namespace RedLight.Scene;

public class SceneManager
{
    private Dictionary<string, RLScene> scenes;
    private Dictionary<string, RLKeyboard> keyboards;
    private RLEngine engine;
    private ShaderManager shaderManager;
    
    private string currentSceneId;
    private RLScene currentScene;
    private RLKeyboard currentKeyboard;

    public SceneManager(RLEngine engine, ShaderManager shaderManager)
    {
        scenes = new Dictionary<string, RLScene>();
        keyboards = new Dictionary<string, RLKeyboard>();
        this.engine = engine;
        this.shaderManager = shaderManager;
    }

    public void Add(string id, RLScene scene, ShaderManager shaderManager, RLKeyboard keyboard)
    {
        if (scenes.ContainsKey(id))
        {
            throw new Exception($"ID [{id}] is already registered");
        }
        scenes.Add(id, scene);
        
        this.shaderManager = shaderManager;
        keyboards.Add(id, keyboard);
        
        if (currentScene == null)
        {
            currentSceneId = id;
            currentScene = scene;
            currentKeyboard = keyboard;
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
            engine.UnsubscribeFromKeyboard(currentKeyboard);
        }

        currentSceneId = id;
        currentScene = scenes[id];
        currentKeyboard = keyboards[id];
        engine.Window.SubscribeToEvents(currentScene);

        currentScene.Engine = engine;
        currentScene.Graphics = engine.Graphics;
        currentScene.SceneManager = this;
        currentScene.ShaderManager = shaderManager;
    
        engine.SubscribeToKeyboard(currentKeyboard);

        currentScene.OnLoad();
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