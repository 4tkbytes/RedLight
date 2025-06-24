using System.Numerics;

namespace RedLight.Lighting;

public enum FogType
{
    Linear = 0,
    Exponential = 1,
    ExponentialSquared = 2
}

public class Fog
{
    public float Density { get; set; } = 0.0f;         // 0 = no fog
    public Vector3 Color { get; set; } = new(0.5f, 0.5f, 0.5f); // Gray fog
    public float Start { get; set; } = 1.0f;           // Linear fog start
    public float End { get; set; } = 100.0f;           // Linear fog end
    public FogType Type { get; set; } = FogType.Linear;

    public bool IsEnabled => Density > 0.0f;

    public static Fog CreateLinearFog(Vector3 color, float start, float end, float density = 0.1f)
    {
        return new Fog
        {
            Color = color,
            Start = start,
            End = end,
            Density = density,
            Type = FogType.Linear
        };
    }

    public static Fog CreateExponentialFog(Vector3 color, float density)
    {
        return new Fog
        {
            Color = color,
            Density = density,
            Type = FogType.Exponential
        };
    }

    public static Fog CreateExponentialSquaredFog(Vector3 color, float density)
    {
        return new Fog
        {
            Color = color,
            Density = density,
            Type = FogType.ExponentialSquared
        };
    }

    public void Disable()
    {
        Density = 0.0f;
    }

    public void Enable(float density = 0.1f)
    {
        Density = density;
    }
}