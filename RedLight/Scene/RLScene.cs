using RedLight.Graphics;
using RedLight.Input;
using RedLight.Physics;
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
    List<Entity<Transformable<RLModel>>> ObjectModels { get; set; }


    void OnLoad();

    void Load()
    {
        Log.Debug("Scene loaded");
    }

    void OnUpdate(double deltaTime);

    void OnRender(double deltaTime);


}