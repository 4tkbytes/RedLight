using System.Numerics;
using System.Drawing;

namespace RedLight.Lighting;

public enum LightType
{
    Directional,
    Point,
    Spot
}

public class RLLight
{
    public string Name { get; set; }
    public LightType Type { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Direction { get; set; }
    public Vector3 Color { get; set; }
    public float Intensity { get; set; }
    
    // Point light properties
    public float Constant { get; set; } = 1.0f;
    public float Linear { get; set; } = 0.09f;
    public float Quadratic { get; set; } = 0.032f;
    
    // Spot light properties
    public float InnerCutOff { get; set; } = 12.5f;
    public float OuterCutOff { get; set; } = 17.5f;
    
    public bool IsEnabled { get; set; } = true;
    
    public RLLight(string name, LightType type, Vector3 position, Vector3 color, float intensity = 1.0f)
    {
        Name = name;
        Type = type;
        Position = position;
        Color = color;
        Intensity = intensity;
        Direction = Vector3.UnitZ;
    }
    
    public static RLLight CreateDirectionalLight(string name, Vector3 direction, Vector3 color, float intensity = 1.0f)
    {
        return new RLLight(name, LightType.Directional, Vector3.Zero, color, intensity)
        {
            Direction = Vector3.Normalize(direction)
        };
    }
    
    public static RLLight CreatePointLight(string name, Vector3 position, Vector3 color, float intensity = 1.0f)
    {
        return new RLLight(name, LightType.Point, position, color, intensity);
    }
    
    // Overload to accept System.Drawing.Color
    public static RLLight CreatePointLight(string name, Vector3 position, Color color, float intensity = 1.0f)
    {
        var colorVec = new Vector3(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f);
        return new RLLight(name, LightType.Point, position, colorVec, intensity);
    }
    
    public static RLLight CreateSpotLight(string name, Vector3 position, Vector3 direction, Vector3 color, float intensity = 1.0f)
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