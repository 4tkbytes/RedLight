using RedLight.Core;
using Silk.NET.Input;
using Silk.NET.Windowing;

namespace RedLight.Scene;

public interface RLScene
{
    public RLWindow? Window { get; set; }
    public RLInputHandler? inputHandler { get; set; }
    public SceneManager? SceneManager { get; set; }
    
    void OnLoad();
    void OnRender(double delta);
    void OnUpdate(double delta);
    void KeyDown(IKeyboard keyboard, Key key, int arg3);
}