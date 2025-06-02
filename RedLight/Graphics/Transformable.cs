using Silk.NET.Maths;
using RedLight.Utils;

namespace RedLight.Graphics;

public class Transformable<T>
{
    private readonly T target;

    public Matrix4X4<float> Model { get; private set; } = Matrix4X4<float>.Identity;
    public Matrix4X4<float> View { get; private set; } = Matrix4X4<float>.Identity;
    public Matrix4X4<float> Projection { get; private set; } = Matrix4X4<float>.Identity;

    public Transformable(T target)
    {
        this.target = target;
    }

    public Transformable<T> Translate(Vector3D<float> translation)
    {
        View = Matrix4X4.CreateTranslation(-translation);
        return this;
    }

    public Transformable<T> Project(float fov, float aspect, float near, float far)
    {
        Projection = Matrix4X4.Add(Projection, Matrix4X4.CreatePerspectiveFieldOfView(fov, aspect, near, far));
        return this;
    }

    public Transformable<T> Rotate(float radians, Vector3D<float> axis)
    {
        radians = -radians;
        // Normalize axis to avoid scaling issues
        var normAxis = Vector3D.Normalize(axis);
        var rotation = Matrix4X4.CreateFromAxisAngle(normAxis, radians);
        Model = Matrix4X4.Multiply(rotation, Model);
        return this;
    }

    public Transformable<T> Scale(Vector3D<float> scale)
    {
        Matrix4X4.Add(Model, Matrix4X4.CreateScale(scale));
        return this;
    }
    
    public Transformable<T> Reset()
    {
        Model = Matrix4X4<float>.Identity;
        View = Matrix4X4<float>.Identity;
        Projection = Matrix4X4<float>.Identity;
        return this;
    }

    public Transformable<T> Reset(float scalar)
    {
        Model = Matrix4X4<float>.Identity * scalar;
        View = Matrix4X4<float>.Identity * scalar;
        Projection = Matrix4X4<float>.Identity;
        return this;
    }

    public T Target => target;
}