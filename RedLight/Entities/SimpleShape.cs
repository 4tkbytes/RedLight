using RedLight.Entities;
using RedLight.Graphics;
using RedLight.Utils;

namespace RedLight.Graphics.Primitive;

/// <summary>
/// Base class for simple geometric shapes like cubes, spheres, planes, etc.
/// Provides a simplified API without nested generic types.
/// </summary>
public abstract class SimpleShape : Entity
{
    /// <summary>
    /// Constructor for SimpleShape with HitboxConfig
    /// </summary>
    /// <param name="model">The RLModel for this shape</param>
    /// <param name="hitboxConfig">Optional hitbox configuration</param>
    /// <param name="applyGravity">Whether to apply gravity to this shape</param>
    protected SimpleShape(RLModel model, HitboxConfig hitboxConfig = null, bool applyGravity = true)
        : base(model, applyGravity)
    {
        // If a hitbox config is provided, use it
        if (hitboxConfig != null)
        {
            HitboxConfig = hitboxConfig;
            ApplyHitboxConfig();
        }
    }

    /// <summary>
    /// Constructor for SimpleShape without HitboxConfig (uses default)
    /// </summary>
    /// <param name="model">The RLModel for this shape</param>
    /// <param name="applyGravity">Whether to apply gravity to this shape</param>
    protected SimpleShape(RLModel model, bool applyGravity = true)
        : this(model, null, applyGravity)
    {
    }
}