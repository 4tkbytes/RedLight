using RedLight;
using RedLight.Graphics;
using RedLight.Scene;
using Serilog;
using Silk.NET.OpenGL;

namespace Game;

class Program
{
    static void Main(string[] args)
    {
        var scene1 = new TestingScene1();
        var scene2 = new TestingScene2();
        var engine = new RLEngine(1280, 720, "Example Game", scene1, args);

        var shaderManager = new ShaderManager();
        var textureManager = new TextureManager();
        var sceneManager = new SceneManager(engine, shaderManager, textureManager);
        engine.SceneManager = sceneManager;

        sceneManager.Add("testing_scene_1", scene1, scene1, scene1);
        sceneManager.Add("testing_scene_2", scene2, scene2, scene2);

        engine.Run();
    }
}