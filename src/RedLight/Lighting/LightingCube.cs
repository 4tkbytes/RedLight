using System.Drawing;
using System.Numerics;
using RedLight.Entities;
using RedLight.Graphics;

namespace RedLight.Lighting;

public class LightingCube
{
    private RLGraphics graphics;
    private LightManager lightManager;
    private string shaderId;

    public RLLight Light;
    public Entity Cube;
    public string Name;
    
    public LightingCube(RLGraphics graphics, LightManager lightManager, string name, string shaderId, Color colour, LightType lightType)
    {
        this.graphics = graphics;
        this.lightManager = lightManager;
        Name = name;
        this.shaderId = shaderId;

        var hitbox = HitboxConfig.ForCube(0.1f, 0f);
        Cube = new Cube(graphics, $"{Name}_cube", false).SetScale(new Vector3(0.5f));
        Cube.Model.AttachShader(ShaderManager.Instance.Get(this.shaderId));
        Cube.SetHitboxConfig(hitbox);

        switch (lightType)
        {
            case LightType.Point:
                Light = RLLight.CreatePointLight($"{Name}_light", Cube.Position, colour);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(lightType), lightType, null);
        }
        
        lightManager.AddLightWithVisual(Light, Cube);

    }
    
    public void Render(Camera camera)
    {
        graphics.Use(Cube);
        lightManager.ApplyLightCubeShader($"{Name}_cube", shaderId, ShaderManager.Instance);
        graphics.Update(camera, Cube);
        graphics.Draw(Cube);
    }

    public void Update()
    {
        lightManager.UpdateLightPosition($"{Name}_cube", Cube.Position);
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