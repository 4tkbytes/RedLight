using RedLight;
using RedLight.Graphics;
using RedLight.Scene;
using Serilog;
using Silk.NET.OpenGL;

namespace ExampleGame;

class Program
{
    static void Main(string[] args)
    {
        // Initialise scenes
        var scene1 = new TestingScene1();
        var loadingScene = new LoadingScene("testing_scene_1");

        // Create engine instance
        var engine = new RLEngine(1280, 720, "Example ExampleGame", loadingScene, args);

        // create scene manager
        var sceneManager = engine.CreateSceneManager();

        // add scenes to scene manager
        sceneManager.Add("loading", loadingScene, loadingScene, loadingScene);
        sceneManager.Add("testing_scene_1", scene1, scene1, scene1);

        // run
        engine.Run();
    }
}