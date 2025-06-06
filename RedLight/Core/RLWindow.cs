using RedLight.Scene;
using Serilog;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Sdl;

namespace RedLight.Core;

public class RLWindow
{
    public IWindow Window { get; set; }
    public double FramesPerSecond { get; set; } = 0;

    public RLWindow(WindowOptions options, RLScene scene)
    {
        // SdlWindowing.Use();
        Window = Silk.NET.Windowing.Window.Create(options);
    }

    internal void SubscribeToEvents(RLScene scene)
    {
        Window.Load += scene.Load;
        Window.Render += scene.OnRender;
        Window.Update += scene.OnUpdate;
        Log.Debug("Subscribed to window events");
    }

    internal void UnsubscribeFromEvents(RLScene scene)
    {
        Window.Load -= scene.Load;
        Window.Render -= scene.OnRender;
        Window.Update -= scene.OnUpdate;
        Log.Debug("Unsubscribed from window events");
    }
}