using RedLight;

namespace PointLonely;

class Program
{
    static void Main(string[] args)
    {
        var loadingScene = new LoadingScene("bedroom_scene");
        var bedroom = new Bedroom();

        var engine = new RLEngine(1280, 720, "PointLonely", loadingScene, args);

        var sceneManager = engine.CreateSceneManager();

        sceneManager.Add("bedroom_scene", bedroom);
        sceneManager.Add("loading", loadingScene);

        engine.Run();
    }
}