using System.Numerics;

namespace RedLight.Lighting;

public class RLLight
{
    public LightType Type { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Direction { get; set; }
    public Vector3 Colour { get; set; } = Vector3.One;
    public float Intensity { get; set; } = 1.0f;
    public float Range { get; set; } = 10.0f;
    
    // point light stuff
    public float Constant { get; set; } = 1.0f;
    public float Linear { get; set; } = 0.09f;
    public float Quadratic { get; set; } = 0.032f;
}