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
        // Initialise the logger for extra debugging
        RLEngine.InitialiseLogger();
        
        // Create a new scene and add as starting scene to engine
        var scene1 = new TestingScene1();
        var engine = new RLEngine(800, 600, "Example Game", scene1);

        // Create a global shader manager and attach to scenes
        var shaderManager = new ShaderManager();
        scene1.ShaderManager = shaderManager;
        var sceneManager = new SceneManager(engine, shaderManager);
        scene1.SceneManager = sceneManager;
        // Add scene 1
        sceneManager.Add("testing_scene_1", scene1, shaderManager, scene1);
        
        // Add scene 2
        var scene2 = new TestingScene2();
        sceneManager.Add("testing_scene_2", scene2, shaderManager, scene2);
        
        // Wrap in try-catch block for logging purposes
        try
        {
            engine.Run();
        }
        catch (Exception e)
        {
            Log.Error("An error has been caught while running the engine: {A}", e.Message);
        }
        finally
        {
            // Flush all logs at the end
            Log.CloseAndFlush();
        }
        
    }
}