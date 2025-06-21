using System.Numerics;
using System.Drawing;
using RedLight.Utils;

namespace RedLight.Lighting;

public class RLLight
{
    private Vector3 _ambient = new(0.2f);
    private Vector3 _specular = new(1.0f);
    private Vector3 _diffuse = new(0.8f);
    private float _shininess = 32.0f;
    
    public string Name { get; set; }
    public LightType Type { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Direction { get; set; }
    public Vector3 Colour { get; set; }
    public float Intensity { get; set; }
    
    // Point light properties
    public float Constant { get; set; } = 1.0f;
    public float Linear { get; set; } = 0.09f;
    public float Quadratic { get; set; } = 0.032f;
    
    // Spot light properties
    public float InnerCutOff { get; set; } = 12.5f;
    public float OuterCutOff { get; set; } = 17.5f;

    public Vector3 Gamma { get; set; } = new Vector3(0.2f);
    
    public bool IsEnabled { get; set; } = true;

    public Vector3 Ambient
    {
        get => _ambient;
        set => _ambient = value;
    }

    public Vector3 Specular
    {
        get => _specular;
        set => _specular = value;
    }

    public Vector3 Diffuse
    {
        get => _diffuse;
        set => _diffuse = value;
    }

    public float Shininess
    {
        get => _shininess;
        set => _shininess = value;
    }
    
    public RLLight(string name, LightType type, Vector3 position, Color color, float intensity = 1.0f)
    {
        Name = name;
        Type = type;
        Position = position;
        Colour = RLUtils.ColorToVector3(color);
        Intensity = intensity;
        Direction = Vector3.UnitZ;
    }
    
    public static RLLight CreateDirectionalLight(string name, Vector3 direction, Color color, float intensity = 1.0f)
    {
        return new RLLight(name, LightType.Directional, Vector3.Zero, color, intensity)
        {
            Direction = Vector3.Normalize(direction)
        };
    }
    
    public static RLLight CreatePointLight(string name, Vector3 position, Color color, float intensity = 1.0f)
    {
        return new RLLight(name, LightType.Point, position, color, intensity);
    }
    
    public static RLLight CreateSpotLight(string name, Vector3 position, Vector3 direction, Color color, float intensity = 1.0f)
    {
        return new RLLight(name, LightType.Spot, position, color, intensity)
        {
            Direction = Vector3.Normalize(direction)
        };
    }
    
    public RLLight SetAttenuation(float constant, float linear, float quadratic)
    {
        Constant = constant;
        Linear = linear;
        Quadratic = quadratic;
        return this;
    }
    
    public RLLight SetSpotAngle(float innerCutOff, float outerCutOff)
    {
        InnerCutOff = innerCutOff;
        OuterCutOff = outerCutOff;
        return this;
    }
}