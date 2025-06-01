using System.Numerics;
using RedLight.Utils;

namespace RedLight.Graphics;

public class Transformable<T>
{
    private readonly T target;

    public Matrix4x4 Transform { get; private set; } = Matrix4x4.Identity;

    public Transformable(T target)
    {
        this.target = target;
    }

    public Transformable<T> Translate(Vector3 translation)
    {
        Transform = RLMath.Translate(Transform, translation);
        return this;
    }

    public Transformable<T> Rotate(float radians, Vector3 axis)
    {
        Transform = RLMath.Rotate(Transform, radians, axis);
        return this;
    }

    public Transformable<T> Scale(Vector3 scale)
    {
        Transform = RLMath.Scale(Transform, scale);
        return this;
    }

    public T Target => target;
}