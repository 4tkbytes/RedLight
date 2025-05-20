using RedLight.Core;

namespace RedLight.Scene;

public class SceneManager
{
    public Dictionary<string, RLScene> scenes = new();
    public RLScene ActiveRlScene { get; private set; }
    
    public void AddScene(string name, RLScene rlScene, RLWindow window)
    {
        if (scenes.ContainsKey(name))
        {
            throw new Exception($"Scene with name {name} already exists.");
        }

        rlScene.Window = window;
        scenes.Add(name, rlScene);
    }

    public void RemoveScene(string name)
    {
        if (!scenes.ContainsKey(name))
        {
            throw new Exception($"Scene with name {name} already exists.");
        }

        scenes.Remove(name);
        
        if (ActiveRlScene == scenes[name])
        {
            ActiveRlScene = null;
        }
    }
    
    public void SwitchScene(string name)
    {
        if (!scenes.ContainsKey(name))
            throw new Exception($"Scene '{name}' does not exist.");

        ActiveRlScene = scenes[name];
        ActiveRlScene.Window.SetScene(ActiveRlScene);
        ActiveRlScene.OnLoad();
    }
}