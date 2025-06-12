namespace RedLight.Graphics.Primitive;

using RedLight.Entities;
using Silk.NET.Maths;
using System.Numerics;

public abstract class SimpleShape : Entity<Transformable<RLModel>>
{
    protected SimpleShape(Transformable<RLModel> transformable) : base(transformable)
    {
        
    }
}