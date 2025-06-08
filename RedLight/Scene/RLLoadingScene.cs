using System.Numerics;
using RedLight.Graphics;
using RedLight.Input;
using Serilog;
using Silk.NET.Input;

namespace RedLight.Scene;

public abstract class RLLoadingScene : RLScene, RLKeyboard, RLMouse
{
    public RLEngine Engine { get; set; }
    public RLGraphics Graphics { get; set; }
    public SceneManager SceneManager { get; set; }
    public ShaderManager ShaderManager { get; set; }
    public TextureManager TextureManager { get; set; }
    public InputManager InputManager { get; set; }
    public List<Transformable<RLModel>> ObjectModels { get; set; }

    public HashSet<Key> PressedKeys { get; set; } = new();

    private string targetSceneId;
    private LoadingState loadingState = LoadingState.Loading;
    private bool targetSceneLoaded = false;
    private double loadingDelay = 0.1;
    private bool startedLoading = false;

    public RLLoadingScene(string targetSceneId)
    {
        this.targetSceneId = targetSceneId;
    }

    public void OnLoad()
    {
        Log.Debug("Loading scene initialized, target scene: [{A}]", targetSceneId);
        loadingState = LoadingState.Loading;

        Graphics?.Enable();
        Graphics?.ClearColour(new RLGraphics.Colour { r = 0f, g = 0f, b = 0f, a = 1.0f });
    }

    public void OnUpdate(double deltaTime)
    {
        if (SceneManager == null || !SceneManager.Scenes.ContainsKey(targetSceneId))
        {
            Log.Error("SceneManager is null or target scene [{A}] not found", targetSceneId);
            throw new Exception("SceneManager is null or target scene [" + targetSceneId + "] not found");
        }

        if (!startedLoading)
        {
            loadingDelay -= deltaTime;
            if (loadingDelay <= 0)
            {
                startedLoading = true;
            }

            return;
        }

        // If target scene isn't loaded yet, load it
        if (!targetSceneLoaded)
        {
            try
            {
                Log.Debug("Attempting to load scene [{A}]", targetSceneId);
                var targetScene = SceneManager.Scenes[targetSceneId];

                targetScene.Engine = Engine;
                targetScene.Graphics = Graphics;
                targetScene.SceneManager = SceneManager;
                targetScene.ShaderManager = ShaderManager;
                targetScene.TextureManager = TextureManager;
                targetScene.InputManager = InputManager;

                targetScene.OnLoad();
                targetSceneLoaded = true;
                loadingState = LoadingState.Completed;
                Log.Information("Loading scene [{A}] completed", targetSceneId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading target scene [{A}]", targetSceneId);
            }
        }

        if (loadingState == LoadingState.Completed)
        {
            Log.Information("Switching to scene [{A}]", targetSceneId);
            SceneManager.SwitchScene(targetSceneId);
        }
    }

    public void OnRender(double deltaTime)
    {
        if (Graphics != null)
        {
            Graphics.Begin();
            Graphics.ClearColour(new RLGraphics.Colour { r = 0f, g = 0f, b = 0f, a = 1.0f });
            Graphics.Clear();
            RenderContent();
            Graphics.End();
        }
    }

    public abstract void RenderContent();

    public void OnKeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        PressedKeys.Add(key);
        if (key == Key.Escape)
        {
            Engine?.Window?.Window?.Close();
        }
    }

    public void OnKeyUp(IKeyboard keyboard, Key key, int keyCode)
    {
        PressedKeys.Remove(key);
    }

    public void OnMouseMove(IMouse mouse, Vector2 mousePosition)
    {
    }
}