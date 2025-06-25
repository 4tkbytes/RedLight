using System.Drawing;
using System.Numerics;
using RedLight;
using RedLight.Entities;
using RedLight.Graphics;
using RedLight.Input;
using RedLight.Lighting;
using RedLight.Physics;
using RedLight.Scene;
using RedLight.UI;
using Silk.NET.Input;

namespace ExampleGame;

public class PlayerCreation : RLScene, RLKeyboard, RLMouse
{
    public override RLEngine Engine { get; set; }
    public override RLGraphics Graphics { get; set; }
    public override SceneManager SceneManager { get; set; }
    public override ShaderManager ShaderManager { get; set; }
    public override TextureManager TextureManager { get; set; }
    public override InputManager InputManager { get; set; }
    public override PhysicsSystem PhysicsSystem { get; set; }
    public override LightManager LightManager { get; set; }
    public override TextManager TextManager { get; set; }

    private Player player;

    public override void OnLoad()
    {
        // This function enables all graphical functions like back buffers. 
        // For beginners: Required/Recommended function, however can be removed
        Graphics.Enable();

        var maxwell = Graphics.CreateModel("RedLight.Resources.Models.Maxwell.maxwell_the_cat.glb", "maxwell")
            .SetScale(new Vector3(0.05f))
            .Rotate(float.DegreesToRadians(-90.0f), Vector3.UnitX);

        var playerCamera = new Camera(Engine.Window.Size);

        var playerHitbox = HitboxConfig.ForPlayer();

        player = Graphics.MakePlayer(playerCamera, maxwell, playerHitbox);

        player.MoveSpeed = 5f;

        // This function is recommended to add if you want to see your model. Otherwise,
        // the default shader is "lit"
        player.Model.AttachShader(ShaderManager.Get("basic"));

        AddToLists(player);
    }

    public override void OnUpdate(double deltaTime)
    {
        PhysicsSystem.Update((float)deltaTime);

        player.Update((float)deltaTime, PressedKeys); // player specific
        // debugCamera.Update((float)deltaTime, PressedKeys);
        // 👆 for any normal camera
    }

    public override void OnRender(double deltaTime)
    {
        Graphics.Clear();
        Graphics.ClearColour(Color.CornflowerBlue);

        RenderModel(player.Camera);
    }

    public HashSet<Key> PressedKeys { get; set; } = new();

    public void OnMouseMove(IMouse mouse, Vector2 mousePosition)
    {
        if (InputManager.isCaptured)
        {
            player.Camera.FreeMove(mousePosition);
            // debugCamera.FreeMove(mousePosition);
        }
    }

    public void OnKeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        // PressedKeys.Add(key);
        bool isNewKeyPress = PressedKeys.Add(key); // improved version of original

        if (isNewKeyPress)
        {
            InputManager.ChangeCaptureToggle(key, Key.F1);
        }
    }
}