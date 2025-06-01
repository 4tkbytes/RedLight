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
    private TextureManager textureManager;
    
    private string currentSceneId;
    private RLScene currentScene;
    private RLKeyboard currentKeyboard;

    internal bool coconutToggle = false;

    public SceneManager(RLEngine engine, ShaderManager shaderManager, TextureManager textureManager)
    {
        scenes = new Dictionary<string, RLScene>();
        keyboards = new Dictionary<string, RLKeyboard>();
        this.engine = engine;
        this.shaderManager = shaderManager;
        this.textureManager = textureManager;
    }

    public void Add(string id, RLScene scene, RLKeyboard keyboard)
    {
        if (scenes.ContainsKey(id))
        {
            throw new Exception($"ID [{id}] is already registered");
        }
        scenes.Add(id, scene);
        
        keyboards.Add(id, keyboard);
        
        if (currentScene == null)
        {
            currentSceneId = id;
            currentScene = scene;
            currentKeyboard = keyboard;
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
        currentScene.TextureManager = textureManager;
    
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