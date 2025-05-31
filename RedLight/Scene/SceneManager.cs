using RedLight.Core;

namespace RedLight.Scene;

public class SceneManager
{
    private Dictionary<string, RLScene> scenes;
    private RLEngine engine;
    
    private string currentSceneId;
    private RLScene currentScene;

    public SceneManager(RLEngine engine)
    {
        scenes = new Dictionary<string, RLScene>();
    }

    public void Add(RLScene scene, string id)
    {
        if (scenes.ContainsKey(id))
        {
            throw new Exception($"ID [{id}] is already registered");
        }
        scenes.Add(id, scene);
        
        if (currentScene == null)
        {
            currentSceneId = id;
            currentScene = scene;
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
        }
        
        currentSceneId = id;
        currentScene = scenes[id];
        engine.Window.SubscribeToEvents(currentScene);
        
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