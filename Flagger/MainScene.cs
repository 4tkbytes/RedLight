using System.Drawing;
using System.Numerics;
using Hexa.NET.ImGui;
using Hexa.NET.ImNodes;
using RedLight;
using RedLight.Graphics;
using RedLight.Input;
using RedLight.Scene;
using RedLight.UI.ImGui;
using Silk.NET.Input;
using Silk.NET.OpenGL;

namespace Flagger;

public class MainScene : RLScene, RLKeyboard, RLMouse
{
    public RLEngine Engine { get; set; }
    public RLGraphics Graphics { get; set; }
    public SceneManager SceneManager { get; set; }
    public ShaderManager ShaderManager { get; set; }
    public TextureManager TextureManager { get; set; }
    public InputManager InputManager { get; set; }
    public PhysicsSystem PhysicsSystem { get; set; }
    public HashSet<Key> PressedKeys { get; set; }

    private ImGuiController controller;
    private bool showWindow = true;
    
    public void OnLoad()
    {

        controller = new ImGuiController(
            gl: Graphics.OpenGL,
            view: Engine.Window.Window,
            input: InputManager.Context,
            null,
            () => ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.ViewportsEnable | ImGuiConfigFlags.NavEnableKeyboard |
                                               ImGuiConfigFlags.DockingEnable
        );
        
        ImNodes.SetImGuiContext(controller.Context);
        ImNodes.SetCurrentContext(ImNodes.CreateContext());
        ImNodes.StyleColorsDark(ImNodes.GetStyle());
    }

    public void OnUpdate(double deltaTime)
    {
        controller.Update((float)deltaTime);
        
        Graphics.OpenGL.Clear(ClearBufferMask.ColorBufferBit);
        Graphics.OpenGL.ClearColor(Color.FromArgb(255, (int) (.45f * 255), (int) (.55f * 255), (int) (.60f * 255)));
        
        ImGui.DockSpaceOverViewport();
        ImGui.ShowDemoWindow();
        
        ImNodes.BeginNodeEditor();
        ImNodes.BeginNode(0);
        ImGui.Text("Hello, world!");
        ImNodes.EndNode();
        ImNodes.EndNodeEditor();
        
        controller.Render();
    }

    public void OnRender(double deltaTime)
    {
        
    }

    public void OnKeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        if (key == Key.Escape)
        {
            Engine.Window.Quit();
        }
    }

    public void OnMouseMove(IMouse mouse, Vector2 mousePosition)
    {
        InputManager.isCaptured = false;
        InputManager.IsCaptured(mouse);
    }
}