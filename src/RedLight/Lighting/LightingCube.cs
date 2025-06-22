using System.Drawing;
using System.Numerics;
using RedLight.Entities;
using RedLight.Graphics;
using RedLight.Scene;
using Serilog;

namespace RedLight.Lighting;

public class LightingCube
{
    private RLGraphics graphics = SceneManager.Instance.GetCurrentScene().Graphics;
    private LightManager lightManager;
    private string shaderId;
    private bool _visible = true;

    public RLLight Light;
    public Entity Cube;
    public string Name;
    
    public bool Visible 
    { 
        get => _visible; 
        set => _visible = value; 
    }

    public void SetVisible(bool visible)
    {
        _visible = visible;
    }
    
    private LightingCube(LightManager lightManager, string name, string shaderId, Vector3? position, Vector3? direction, Color colour, LightType lightType, Attenuation? attenuation)
    {
        this.graphics = graphics;
        this.lightManager = lightManager;
        Name = name;
        this.shaderId = shaderId;

        // var hitbox = HitboxConfig.ForCube(0.1f, 0f);
        Cube = new Cube($"{Name}_cube", false).SetScale(new Vector3(0.5f));
        Cube.Model.AttachShader(ShaderManager.Instance.Get(this.shaderId));
        // Cube.SetHitboxConfig(hitbox);

        Cube = Entities.Cube.CreateLightCube($"{Name}_cube", this.shaderId);
        Cube.EnablePhysics = false;

        switch (lightType)
        {
            case LightType.Directional:
                if (direction.HasValue)
                    Light = RLLight.CreateDirectionalLight($"{Name}_light", direction.Value, colour);
                break;
            
            case LightType.Point:
                if (position.HasValue)
                    Light = RLLight.CreatePointLight($"{Name}_light", position.Value, colour);
                if (attenuation.HasValue)
                    Light.Attenuation = attenuation.Value;
                break;
            case LightType.Spot:
                Light = RLLight.CreateSpotLight($"{Name}_light", direction.Value, position.Value, colour);
                Light.Attenuation = attenuation.Value;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(lightType), lightType, null);
        }
        
        lightManager.AddLightWithVisual(Light, Cube);
    }

    public static LightingCube CreateDirectionalLightCube(LightManager lightManager, string name,
        string shaderId, Vector3 direction, Color colour)
    {
        return new LightingCube(lightManager, name, shaderId, null, direction, colour, LightType.Directional, null);
    }
    
    public static LightingCube CreatePointLightCube(LightManager lightManager, string name,
        string shaderId, Vector3 position, Color colour, Attenuation attenuation)
    {
        return new LightingCube(lightManager, name, shaderId, position, null, colour, LightType.Point, attenuation);
    }

    public static LightingCube CreateSpotLightCube(LightManager lightManager, string name,
        string shaderId, Vector3 position, Vector3 direction, Color colour, Attenuation attenuation)
    {
        return new LightingCube(lightManager, name, shaderId, position, direction, colour, LightType.Spot, attenuation);
    }
    
    public static LightingCube CreateSpotLightCube(LightManager lightManager, string name,
        string shaderId, Camera camera, Color colour, Attenuation attenuation)
    {
        return new LightingCube(lightManager, name, shaderId, camera.Position, camera.Front, colour, LightType.Spot, attenuation);
    }
    
    public void Update(Camera camera = null)
    {
        lightManager.UpdateLightPosition($"{Name}_cube", Cube.Position);

        if (Light.Type == LightType.Spot && camera != null)
        {
            Light.Direction = camera.Front;
            Light.Position = camera.Position;
            lightManager.UpdateLightDirection($"{Name}_light", camera.Front);
        }
        else if (Light.Type == LightType.Spot && camera == null)
        {
            Log.Warning($"[SpotLight] Spotlight without camera reference! Direction won't update.");
        }
    }

    public void Render(Camera camera)
    {
        if (!_visible) return;
        graphics.Use(Cube);
        lightManager.ApplyLightCubeShader($"{Name}_cube", shaderId, ShaderManager.Instance);
    
        graphics.Update(camera, Cube);
        graphics.Draw(Cube);
    }

    public void Translate(Vector3 translation)
    {
        Cube.Translate(translation);
    }

    public void Rotate(float axis, Vector3 rotation)
    {
        Cube.Rotate(axis, rotation);
    }

    public void Scale(Vector3 scale)
    {
        Cube.SetScale(scale);
    }
}