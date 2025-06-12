using RedLight.Physics;

namespace RedLight.Graphics.Primitive;

using Silk.NET.Maths;
using System.Numerics;

public abstract class SimpleShape : Entity<Transformable<RLModel>>
{
    protected SimpleShape(Transformable<RLModel> transformable) : base(transformable)
    {
        
    }
}