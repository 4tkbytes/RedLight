using System.Numerics;

namespace RedLight.Entities;

/// <summary>
/// Class used to create simple hitboxes for physics.
/// <para>
/// Note: After creating a new instance of this class, ensure you call
/// the ApplyHitbox function to set it. 
/// </para>
/// </summary>
public class HitboxConfig
{
    /// <summary>Width of hitbox in X dimension</summary>
    public float Width { get; set; } = 1.0f;

    /// <summary>Height of hitbox in Y dimension</summary>
    public float Height { get; set; } = 1.0f;

    /// <summary>Length of hitbox in Z dimension</summary>
    public float Length { get; set; } = 1.0f;

    /// <summary>
    /// Portion of the hitbox below the model's center point (0.0-1.0)
    /// 0.5 = half below/half above
    /// 1.0 = bottom at ground level
    /// 0.0 = bottom at center level
    /// </summary>
    public float GroundOffset { get; set; } = 0.5f;

    /// <summary>
    /// Offset from the model's center in each dimension
    /// </summary>
    public Vector3 CenterOffset { get; set; } = Vector3.Zero;

    /// <summary>
    /// Calculate the minimum bounding box point based on configuration
    /// </summary>
    public Vector3 CalculateMin()
    {
        float halfWidth = Width * 0.5f;
        float halfLength = Length * 0.5f;
        float bottomY = -Height * GroundOffset;

        return new Vector3(-halfWidth, bottomY, -halfLength) + CenterOffset;
    }

    /// <summary>
    /// Calculate the maximum bounding box point based on configuration
    /// </summary>
    public Vector3 CalculateMax()
    {
        float halfWidth = Width * 0.5f;
        float halfLength = Length * 0.5f;
        float topY = Height * (1.0f - GroundOffset);

        return new Vector3(halfWidth, topY, halfLength) + CenterOffset;
    }

    /// <summary>
    /// Create a hitbox config for a player character
    /// </summary>
    public static HitboxConfig ForPlayer(float width = 0.2f, float height = 0.4f, float length = 0.2f, float groundOffset = 1.0f)
    {
        return new HitboxConfig
        {
            Width = width,
            Height = height,
            Length = length,
            GroundOffset = groundOffset // Player touching ground
        };
    }

    /// <summary>
    /// Create a hitbox config for a standard cube
    /// </summary>
    public static HitboxConfig ForCube(float size = 1.0f, float groundOffset = 0.5f)
    {
        return new HitboxConfig
        {
            Width = size,
            Height = size,
            Length = size,
            GroundOffset = groundOffset // Cube centered
        };
    }

    /// <summary>
    /// Create a hitbox config for a plane/ground
    /// </summary>
    public static HitboxConfig ForPlane(float width, float length, float thickness = 0.1f)
    {
        return new HitboxConfig
        {
            Width = width,
            Height = thickness,
            Length = length,
            GroundOffset = 1.0f // Plane at ground level
        };
    }
}