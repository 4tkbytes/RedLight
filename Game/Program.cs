using RedLight;
using RedLight.Core;
using RedLight.Scene;

namespace Game;
class Program
{
    static void Main(string[] args)
    {
        // Create engine first, then get the window
        RLEngine rlEngine = new(800, 600, "Sample Game || RedLight Engine", null);
        RLWindow window = rlEngine.GetWindow();
        
        // Create managers
        SceneManager sceneManager = new SceneManager();
        GameInputHandler inputHandler = new();
        
        // Create scene with required properties
        TestingScene1 scene = new TestingScene1() 
        { 
            Window = window,
            SceneManager = sceneManager,
            inputHandler = inputHandler
        };
        
        // Set the scene in the engine
        rlEngine.SetScene(scene);
        
        // Add the scene to the manager
        sceneManager.AddScene("TestScene1", scene, window, inputHandler);
        
        // Start the engine
        rlEngine.Run();
    }
}