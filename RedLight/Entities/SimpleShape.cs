namespace RedLight.Graphics.Primitive;

using RedLight.Entities;
using Silk.NET.Maths;
using System.Numerics;

//// <summary>
/// Abstract class for any simple shapes like cubes and other objects
/// </summary>
public abstract class SimpleShape : Entity<Transformable<RLModel>>
{
    protected SimpleShape(Transformable<RLModel> transformable) : base(transformable) { }
}