using System.Numerics;
using RedLight.Utils;

namespace RedLight.Graphics;

public class Transformable<T>
{
    private readonly T target;

    public Matrix4x4 Model { get; private set; } = Matrix4x4.Identity;
    public Matrix4x4 View { get; private set; } = Matrix4x4.Identity;
    public Matrix4x4 Projection { get; private set; } = Matrix4x4.Identity;

    public Transformable(T target)
    {
        this.target = target;
    }

    public Transformable<T> Translate(Vector3 translation)
    {
        View = RLMath.Translate(View, translation);
        return this;
    }

    public Transformable<T> Project(float fov, float aspect, float near, float far)
    {
        Projection = RLMath.Perspective(Projection, fov, aspect, near, far);
        return this;
    }

    public Transformable<T> Rotate(float radians, Vector3 axis)
    {
        Model = RLMath.Rotate(Model, radians, axis);
        return this;
    }

    public Transformable<T> Scale(Vector3 scale)
    {
        Model = RLMath.Scale(Model, scale);
        return this;
    }
    
    public Transformable<T> Reset()
    {
        Model = RLMath.Mat4(1.0f);
        View = RLMath.Mat4(1.0f);
        Projection = RLMath.Mat4(1.0f);
        return this;
    }

    public T Target => target;
}