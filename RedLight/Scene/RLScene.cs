using RedLight.Graphics;
using RedLight.Input;

namespace RedLight.Scene;

public interface RLScene
{
    RLEngine Engine { get; set; }
    RLGraphics Graphics { get; set; }
    SceneManager SceneManager { get; set; }
    ShaderManager ShaderManager { get; set; }
    TextureManager TextureManager { get; set; }
    InputManager InputManager { get; set; }


    void OnLoad();

    void OnUpdate(double deltaTime);

    void OnRender(double deltaTime);


}