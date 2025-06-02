using Silk.NET.Maths;
using RedLight.Utils;

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
    }
    
    public Transformable<T> Translate(Camera camera, Vector3D<float> translation)
    {
        camera.View = Matrix4X4.CreateTranslation(-translation);
        return this;
    }

    public Transformable<T> Rotate(float radians, Vector3D<float> axis)
    {
        radians = -radians;
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
    
    public Transformable<T> AbsoluteReset()
    {
        Model = Matrix4X4<float>.Identity;
        defaultSet = false;
        return this;
    }

    public Transformable<T> AbsoluteReset(float scalar)
    {
        Model = Matrix4X4<float>.Identity * scalar;
        defaultSet = false;
        return this;
    }
    
    /// <summary>
    /// Resets back to the original state where you set a save. Requires a save state
    /// otherwise the function will throw an exception.
    /// </summary>
    /// <returns>Self</returns>
    /// <exception cref="Exception"></exception>
    public Transformable<T> Reset()
    {
        if (!defaultSet)
            throw new Exception("Unable to reset as a lock state has not been created");
        Model = modelDefault;
        return this;
    }

    /// <summary>
    /// Creates a lock state where you can make edits to the original mesh
    /// and allow you to draw without having to reset and redraw to the original
    /// state.  
    /// </summary>
    /// <returns>Self</returns>
    public Transformable<T> SetDefault()
    {
        modelDefault = Model;
        defaultSet = true;
        return this;
    }

    public void SetModel(Matrix4X4<float> model)
    {
        Model = model;
    }

    public T Target => target;
}