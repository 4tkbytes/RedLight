// This is a scene template. The file (SceneTemplate.cs) will be copied over
// to a dump file, such as Resources. 

using RedLight.Graphics;
using RedLight.Input;
using RedLight.Scene;
using RedLight.UI;
using RedLight.Utils;

using Silk.NET.Input;
using Silk.NET.Maths;
using System.Numerics;
using System.Collections.Generic;

namespace RedLight.Resources.Exported;

public class SceneTemplate : RLScene, RLKeyboard, RLMouse
{
    public RLEngine Engine { get; set; }
    public RLGraphics Graphics { get; set; }
    public SceneManager SceneManager { get; set; }
    public ShaderManager ShaderManager { get; set; }
    public TextureManager TextureManager { get; set; }
    public InputManager InputManager { get; set; }
    public List<Transformable<RLModel>> ObjectModels { get; set; } = new();
    public HashSet<Key> PressedKeys { get; set; } = new();

    private Camera camera;
    private RLImGui controller;
    private float cameraSpeed = 2.5f;

    public void OnLoad()
    {
        Graphics.Enable();
        Graphics.ShutUp = true;
        
        controller = new RLImGui(Graphics, Engine.Window, InputManager, ShaderManager, TextureManager, SceneManager);
        camera = new Camera(Engine.Window.Window.Size);

    }

    public void OnRender(double deltaTime)
    {
        camera = camera.SetSpeed(cameraSpeed * (float)deltaTime);
        
        if (InputManager.isCaptured)
            camera.KeyMap(PressedKeys);
    }

    public void OnUpdate(double deltaTime)
    {
        Graphics.Begin();
        {
            Graphics.Clear();
            Graphics.ClearColour(RLConstants.RL_COLOUR_CORNFLOWER_BLUE);

            foreach (var model in ObjectModels)
            {
                Graphics.Use(model);
                Graphics.Update(camera, model);
                Graphics.Draw(model);
            }
        }
        Graphics.End();

        controller.Render(deltaTime, camera);
    }

    public void OnMouseMove(IMouse mouse, Vector2 mousePosition)
    {
        if (InputManager.isCaptured)
            camera.FreeMove(mousePosition);
    }
}

