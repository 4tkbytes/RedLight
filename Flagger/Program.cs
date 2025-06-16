// Editor for the RedLight Game Engine
// Flagger Editor

using RedLight;

namespace Flagger;

class Program
{
    static void Main(string[] args)
    {
        var mainScene = new MainScene();
        var engine = new RLEngine(1280, 720, "Flagger Editor for RedLight", mainScene, args);
        
        engine.AddScene("mainScene", mainScene);

        engine.Run();
    }
}