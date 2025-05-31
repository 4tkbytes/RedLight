using RedLight;
using RedLight.Input;
using RedLight.Scene;
using Silk.NET.Input;

namespace Game;

public class TestingScene1 : RLScene, RLKeyboard
{
    public SceneManager sceneManager { get; set; }
    public RLEngine engine { get; set; }

    public void OnLoad()
    {
        
    }

    public void OnUpdate(double deltaTime)
    {
        
    }

    public void OnRender(double deltaTime)
    {
        
    }

    public void OnKeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        if (key == Key.Escape)
        {
            engine.Window.Window.Close();
        }
    }
}