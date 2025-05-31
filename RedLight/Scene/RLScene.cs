namespace RedLight.Scene;

public interface RLScene
{
    SceneManager sceneManager { get; set; }
    RLEngine engine { get; set; }
    
    void OnLoad();

    void OnUpdate(double deltaTime);

    void OnRender(double deltaTime);
    
    
}