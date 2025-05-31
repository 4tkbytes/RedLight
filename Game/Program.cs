using RedLight;
using RedLight.Graphics;
using RedLight.Scene;
using Silk.NET.OpenGL;

namespace Game;

class Program
{ 
    static void Main(string[] args)
    {
        var scene1 = new TestingScene1();
        var engine = new RLEngine(800, 600, "Example Game", scene1);

        var shaderManager = new ShaderManager();
        scene1.ShaderManager = shaderManager;
        scene1.SceneManager = new SceneManager(engine, shaderManager);
        
        engine.Run();
    }
}