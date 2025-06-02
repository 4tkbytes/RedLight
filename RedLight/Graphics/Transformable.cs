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
        View = Matrix4X4.Add(View, Matrix4X4.CreateTranslation(translation));
        return this;
    }

    public Transformable<T> Project(float fov, float aspect, float near, float far)
    {
        Projection = Matrix4X4.Add(Projection, Matrix4X4.CreatePerspectiveFieldOfView(fov, aspect, near, far));
        return this;
    }

    public Transformable<T> Rotate(float radians, Vector3D<float> axis)
    {
        var fuckass = Matrix4X4.Multiply(Matrix4X4.CreateRotationX(radians), axis.X);
        var fuckass2 = Matrix4X4.Multiply(Matrix4X4.CreateRotationY(radians), axis.Y);
        var fuckass3 = Matrix4X4.Multiply(Matrix4X4.CreateRotationZ(radians), axis.Z);

        Model = Matrix4X4.Add(Model, fuckass);
        Model = Matrix4X4.Add(Model, fuckass2);
        Model = Matrix4X4.Add(Model, fuckass3);

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

    public T Target => target;
}