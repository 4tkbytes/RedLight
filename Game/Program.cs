using RedLight;
using RedLight.Scene;

namespace Game;

class Program
{ 
    static void Main(string[] args)
    {
        var scene1 = new TestingScene1();
        var engine = new RLEngine(800, 600, "Example Game", scene1, scene1);
        var sceneManager = new SceneManager(engine);
        engine.Run();
    }
}