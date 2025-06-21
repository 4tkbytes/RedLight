using System.Numerics;
using System.Drawing;
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
    
    public bool IsEnabled { get; set; } = true;
    
    private RLLight(string name, LightType type, Vector3? direction, Vector3? position, Color color, float intensity = 2.0f)
    {
        Name = name;
        Type = type;
        Direction = direction;
        Position = position;
        Colour = RLUtils.ColorToVector3(color);
        Intensity = intensity;
        Direction = Vector3.UnitZ;
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

    public RLLight SetAttenuation(Attenuation attenuation)
    {
        Attenuation = attenuation;
        return this;
    }
}