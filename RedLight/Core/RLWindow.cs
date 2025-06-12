using System.Numerics;
using RedLight.Scene;
using RedLight.Utils;
using Serilog;
using Silk.NET.Windowing;

namespace RedLight.Core;

public class RLWindow
{
    public IWindow Window { get; set; }
    public double FramesPerSecond { get; set; } = 0;
    public Vector2 Size { get; private set; }
    
    public RLWindow(WindowOptions options, RLScene scene)
    {
        Window = Silk.NET.Windowing.Window.Create(options);
        Size = RLUtils.SilkVector2DToNumericsVector2(Window.Size);
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