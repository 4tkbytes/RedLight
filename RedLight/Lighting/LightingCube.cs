using System.Drawing;
using System.Numerics;
using RedLight.Entities;
using RedLight.Graphics;
using RedLight.Graphics.Primitive;

namespace RedLight.Lighting;

public class LightingCube
{
    private RLGraphics graphics;
    private LightManager lightManager;

    public RLLight Light;
    public Entity Cube;
    public string Name;
    
    public LightingCube(RLGraphics graphics, LightManager lightManager, string name, string shaderId, Color colour, LightType lightType)
    {
        this.graphics = graphics;
        this.lightManager = lightManager;
        Name = name;

        Cube = new Cube(graphics, $"{name}_cube", false).SetScale(new Vector3(1.5f));
        Cube.Model.AttachShader(ShaderManager.Instance.Get("light_cube"));

        switch (lightType)
        {
            case LightType.Point:
                Light = RLLight.CreatePointLight($"{name}_light", Cube.Position, colour);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(lightType), lightType, null);
        }
        
        lightManager.AddLightWithVisual(Light, Cube);
    }
    
    public void Render(Camera camera)
    {
        graphics.Use(Cube);
        lightManager.ApplyLightCubeShader($"{Name}_cube", "light_cube", ShaderManager.Instance);
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