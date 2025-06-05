using System.Numerics;
using RedLight;
using RedLight.Graphics;
using RedLight.Input;
using RedLight.Scene;
using Silk.NET.Input;

namespace PointLonely;

public class Bedroom : RLScene, RLKeyboard, RLMouse
{
    public RLEngine Engine { get; set; }
    public RLGraphics Graphics { get; set; }
    public SceneManager SceneManager { get; set; }
    public ShaderManager ShaderManager { get; set; }
    public TextureManager TextureManager { get; set; }
    public InputManager InputManager { get; set; }

    public void OnLoad()
    {

    }

    public void OnUpdate(double deltaTime)
    {

    }

    public void OnRender(double deltaTime)
    {

    }

    public HashSet<Key> PressedKeys { get; set; }
    public void OnMouseMove(IMouse mouse, Vector2 mousePosition)
    {

    }
}