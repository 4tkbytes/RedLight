using System;
using RedLight.Core;
using RedLight.Graphics;
using RedLight.Input;
using RedLight.Scene;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace RedLight;

public class RLEngine
{
    public RLWindow Window { get; private set; }
    public RLGraphics Graphics { get; private set; }
    public RLKeyboard Keyboard { get; set; }
    
    public RLEngine(int width, int height, string title, RLScene startingScene)
    {
        WindowOptions options = WindowOptions.Default;
        options.Size = new Vector2D<int>(width, height);
        options.Title = title;
        
        Window = new RLWindow(options, startingScene);
        Graphics = new RLGraphics();
        
        Window.Window.Load += () =>
        {
            Graphics.OpenGL = Window.Window.CreateOpenGL();
            
            if (startingScene != null)
            {
                startingScene.Graphics = Graphics;
                startingScene.Engine = this;
                Window.SubscribeToEvents(startingScene);
                startingScene.OnLoad();
            }
            
            Keyboard = startingScene as RLKeyboard;
            
            IInputContext input = Window.Window.CreateInput();
            for (int i = 0; i < input.Keyboards.Count; i++)
                input.Keyboards[i].KeyDown += Keyboard.OnKeyDown;
        };
        Window.Window.FramebufferResize += OnFramebufferResize;
    }
    
    private void OnFramebufferResize(Vector2D<int> newSize)
    {
        Graphics.OpenGL.Viewport(newSize);
    }

    public void Run()
    {
        Window.Window.Run();
    }
}