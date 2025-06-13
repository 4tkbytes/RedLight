using RedLight.Entities;
using RedLight.Graphics;

namespace RedLight.Graphics.Primitive;

/// <summary>
/// Base class for simple geometric shapes like cubes, spheres, planes, etc.
/// Provides a simplified API without nested generic types.
/// </summary>
public abstract class SimpleShape : Entity
{
    protected SimpleShape(RLModel model, bool applyGravity = true) 
        : base(model, applyGravity) 
    {
    }
}
