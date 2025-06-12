using RedLight.Graphics;
using RedLight.Input;
using Serilog;

namespace RedLight.Scene;

public interface RLScene
{
    RLEngine Engine { get; set; }
    RLGraphics Graphics { get; set; }
    SceneManager SceneManager { get; set; }
    ShaderManager ShaderManager { get; set; }
    TextureManager TextureManager { get; set; }
    InputManager InputManager { get; set; }
    PhysicsSystem PhysicsSystem { get; set; }


    void OnLoad();

    // this function is separate to the OnLoad function at the top
    // it is a "jumpstart" for the silk.net.windowing process
    void Load()
    {
        Log.Debug("Scene loaded");
    }

    void OnUpdate(double deltaTime);

    void OnRender(double deltaTime);


}