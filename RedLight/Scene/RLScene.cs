using RedLight.Core;
using Silk.NET.Input;
using Silk.NET.Windowing;

namespace RedLight.Scene;

public interface RLScene
{
    public RLWindow Window { get; set; }
    public SceneManager SceneManager { get; set; }
    
    void OnLoad();
    void OnUpdate(double delta);
    void OnRender(double delta);
    void KeyDown(IKeyboard keyboard, Key key, int arg3);
}