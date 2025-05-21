using RedLight.Scene;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace RedLight.Core;

public class RLWindow
{
    public IWindow window;
    private Scene.RLScene _rlScene;
    
    public RLWindow(WindowOptions options, Scene.RLScene rlScene)
    {
        window = Window.Create(options);
        _rlScene = rlScene;
    }

    public void SetScene(RLScene rlScene)
    {
        // Unsubscribe previous handlers
        window.Load -= _rlScene.OnLoad;
        window.Update -= _rlScene.OnUpdate;
        window.Render -= _rlScene.OnRender;

        // Set new scene
        _rlScene = rlScene;

        // Subscribe new handlers
        window.Load += _rlScene.OnLoad;
        window.Update += _rlScene.OnUpdate;
        window.Render += _rlScene.OnRender;
    }
}