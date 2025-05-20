using RedLight;
using RedLight.Core;
using RedLight.Scene;

namespace Game;
class Program
{
    static void Main(string[] args)
    {
        TestingScene1 scene = new TestingScene1();
        RLEngine rlEngine = new(800, 600, "Sample Game || RedLight Engine", scene);
        RLWindow window = rlEngine.GetWindow();
        
        SceneManager sceneManager = new SceneManager();
        
        scene.SceneManager = sceneManager;
        var scene2 = new TestingScene2();
        scene2.SceneManager = sceneManager;
        sceneManager.AddScene("TestScene1", scene, window);
        sceneManager.AddScene("TestScene2", scene2, window);
        
        rlEngine.Run();
    }
}