using RedLight;
using RedLight.Graphics;
using RedLight.Input;
using RedLight.Scene;
using Silk.NET.Input;

namespace Game;

public class TestingScene2 : RLScene, RLKeyboard
{
    public RLEngine Engine { get; set; }
    public RLGraphics Graphics { get; set; }
    public SceneManager SceneManager { get; set; }
    public ShaderManager ShaderManager { get; set; }
    public void OnLoad()
    {
        Console.WriteLine("Scene 2 Loaded");
    }

    public void OnUpdate(double deltaTime)
    {
        
    }

    public void OnRender(double deltaTime)
    {
        Graphics.Clear();
        Graphics.ClearColour(new RLGraphics.Colour { r = 0f, g = 0f, b = 0f, a = 0f });
    }

    public void OnKeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        if (key == Key.Right)
        {
            SceneManager.SwitchScene("testing_scene_1");
        }
    }
}