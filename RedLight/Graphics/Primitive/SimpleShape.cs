using RedLight.Physics;

namespace RedLight.Graphics.Primitive;

using Silk.NET.Maths;
using System.Numerics;

public abstract class SimpleShape : Entity<RLModel>
{
    protected SimpleShape(RLModel model) : base(model)
    {
        
    }
}