using RedLight.Scene;
using Silk.NET.Windowing;

namespace RedLight.Core;

public class RLWindow
{
    public IWindow Window { get; set; }
    
    public RLWindow(WindowOptions options, RLScene scene)
    {
        Window = Silk.NET.Windowing.Window.Create(options);
        SubscribeToEvents(scene);
    }

    internal void SubscribeToEvents(RLScene scene)
    {
        Window.Load += scene.OnLoad;
        Window.Render += scene.OnRender;
        Window.Update += scene.OnUpdate;
    }
    
    internal void UnsubscribeFromEvents(RLScene scene)
    {
        Window.Load += scene.OnLoad;
        Window.Render += scene.OnRender;
        Window.Update += scene.OnUpdate;
    }
}