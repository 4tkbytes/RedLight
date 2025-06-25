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

        // Create engine instance
        var engine = new RLEngine(1280, 720, "RedLight Game Engine Editor", scene1, args);

        // add scenes to scene manager
        SceneManager.Instance.Add("testing_scene_1", scene1);
        SceneManager.Instance.Add("playerCreation", scene1);

        // run
        engine.Run();
    }
}