using System.Numerics;
using System.Drawing;
using RedLight.Utils;

namespace RedLight.Lighting;

public class RLLight
{
    public string Name { get; set; }
    public LightType Type { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Direction { get; set; }
    public Vector3 Colour { get; set; }
    public float Intensity { get; set; }
    
    public bool IsEnabled { get; set; } = true;
    
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
}