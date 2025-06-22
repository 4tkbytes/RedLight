using System.Numerics;
using System.Drawing;
using RedLight.Graphics;
using RedLight.Utils;

namespace RedLight.Lighting;

public class RLLight
{
    public string Name { get; set; }
    public LightType Type { get; set; }
    public Vector3? Direction { get; set; }
    public Vector3? Position { get; set; }
    public Vector3 Colour { get; set; }
    public float Intensity { get; set; }
    public Attenuation Attenuation { get; set; }
    public float InnerCutoff { get; set; } = 12.5f;
    public float OuterCutoff { get; set; } = 17.5f;
    
    public bool IsEnabled { get; set; } = true;
    
    private RLLight(string name, LightType type, Vector3? direction, Vector3? position, Color color, float intensity = 2.0f)
    {
        Name = name;
        Type = type;
        Direction = direction;
        Position = position;
        Colour = RLUtils.ColorToVector3(color);
        Intensity = intensity;
    }
    
    public static RLLight CreateDirectionalLight(string name, Vector3 direction, Color color, float intensity = 2.0f)
    {
        return new RLLight(name, LightType.Directional, direction, null, color, intensity)
        {
            Direction = Vector3.Normalize(direction)
        };
    }
    
    public static RLLight CreatePointLight(string name, Vector3 position, Color color, float intensity = 2.0f)
    {
        return new RLLight(name, LightType.Point, null, position, color, intensity);
    }
    
    public static RLLight CreateSpotLight(string name, Vector3 direction, Vector3 position, Color color, 
        float intensity = 2.0f, float innerCutoffAngle = 12.5f, float outerCutoffAngle = 17.5f)
    {
        return new RLLight(name, LightType.Spot, direction, position, color, intensity)
        {
            InnerCutoff = innerCutoffAngle
        };
    }
}