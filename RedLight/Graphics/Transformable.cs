using Silk.NET.Maths;
using RedLight.Utils;
using Serilog;

namespace RedLight.Graphics;

public class Transformable<T>
{
    private readonly T target;
    private bool defaultSet = false;

    private Matrix4X4<float> modelDefault = Matrix4X4<float>.Identity;

    public Matrix4X4<float> Model { get; private set; } = Matrix4X4<float>.Identity;

    public Transformable(T target)
    {
        this.target = target;
        // make it right way up
        Rotate(float.DegreesToRadians(180.0f), Vector3D<float>.UnitZ);
    }

    public Transformable<T> Translate(Vector3D<float> translation)
    {
        Model = Matrix4X4.Multiply(Matrix4X4.CreateTranslation(translation), Model);
        Log.Verbose("Translated mesh");
        return this;
    }

    public Transformable<T> Rotate(float radians, Vector3D<float> axis)
    {
        var normAxis = Vector3D.Normalize(axis);
        var rotation = Matrix4X4.CreateFromAxisAngle(normAxis, radians);
        Model = Matrix4X4.Multiply(rotation, Model);
        Log.Verbose("Rotated mesh");
        return this;
    }

    public Transformable<T> Scale(Vector3D<float> scale)
    {
        Model = Matrix4X4.Multiply(Matrix4X4.CreateScale(scale), Model);
        Log.Verbose("Scaled mesh");
        return this;
    }

    public Transformable<T> AbsoluteReset()
    {
        Model = Matrix4X4<float>.Identity;
        defaultSet = false;
        Log.Verbose("Absolute reset the mesh model");
        return this;
    }

    public Transformable<T> AbsoluteReset(float scalar)
    {
        Model = Matrix4X4<float>.Identity * scalar;
        defaultSet = false;
        Log.Verbose("Absolute reset mesh model");
        return this;
    }

    public Transformable<T> Reset()
    {
        if (!defaultSet)
            throw new Exception("Unable to reset as a lock state has not been created");
        Model = modelDefault;
        Log.Verbose("Resetted the mesh model");
        return this;
    }

    public Transformable<T> SetDefault()
    {
        modelDefault = Model;
        defaultSet = true;
        Log.Verbose("Default set the mesh model");
        return this;
    }

    public Transformable<T> SetModel(Matrix4X4<float> model)
    {
        Model = model;
        return this;
    }

    public T Target => target;
}