using System.Numerics;
using RedLight;
using RedLight.Graphics;
using RedLight.Input;
using RedLight.Scene;
using Serilog;
using Silk.NET.Input;

namespace Game;

public class TestingScene2 : RLScene, RLKeyboard, RLMouse
{
    public RLEngine Engine { get; set; }
    public RLGraphics Graphics { get; set; }
    public SceneManager SceneManager { get; set; }
    public ShaderManager ShaderManager { get; set; }
    public TextureManager TextureManager { get; set; }

    public void OnLoad()
    {
        Log.Information("Scene 2 has been loaded");
    }

    public void OnUpdate(double deltaTime)
    {

    }

    public void OnRender(double deltaTime)
    {
        Graphics.Clear();
        Graphics.ClearColour(new RLGraphics.Colour { r = 0f, g = 0f, b = 0f, a = 0f });
    }

    public HashSet<Key> PressedKeys { get; } = new();

    public void OnKeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        if (key == Key.Right)
        {
            SceneManager.SwitchScene("testing_scene_1");
        }
    }

    public void OnMouseMove(IMouse mouse, Vector2 mousePosition)
    {

    }
}