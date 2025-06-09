using System.Numerics;
using Silk.NET.Maths;
using RedLight.Utils;
using Serilog;

namespace RedLight.Graphics;

/// <summary>
/// This class makes any class transformable. Typically used with Meshes or RLModels, this can allow you to
/// translate the model, rotate the model or change its scale. 
/// </summary>
/// <typeparam name="T">Mesh or RLModel</typeparam>
public class Transformable<T>
{
    private readonly T target;
    private bool defaultSet = false;
    private Matrix4X4<float> modelDefault = Matrix4X4<float>.Identity;
    
    public Vector3 eulerAngles = new Vector3(0, 0, 0);
    public Matrix4X4<float> Model { get; private set; } = Matrix4X4<float>.Identity;

    /// <summary>
    /// The target, specifically what you put in when you initialised a Transformable. 
    /// </summary>
    public T Target => target;

    public Transformable(T target)
    {
        this.target = target;
    }

    /// <summary>
    /// Translate the target by a Vector3D float. 
    /// </summary>
    /// <param name="translation">Vector3D</param>
    /// <returns>Transformable</returns>
    public Transformable<T> SetPosition(Vector3D<float> translation)
    {
        Model = Matrix4X4.Multiply(Matrix4X4.CreateTranslation(translation), Model);
        Log.Verbose("Translated mesh");
        return this;
    }

    /// <summary>
    /// Rotates the target by a radians and its axis (typically Vector3D's UnitX, UnitY or UnitZ).
    /// </summary>
    /// <param name="radians">float</param>
    /// <param name="axis">Vector3D</param>
    /// <returns>Transformable</returns>
    public Transformable<T> SetRotation(float radians, Vector3D<float> axis)
    {
        var normAxis = Vector3D.Normalize(axis);
        var rotation = Matrix4X4.CreateFromAxisAngle(normAxis, radians);
        Model = Matrix4X4.Multiply(rotation, Model);
        Log.Verbose("Rotated mesh");
        return this;
    }

    /// <summary>
    /// Changes the scale of the model. Takes an input of scale and multiplies the model matrix by the scale. 
    /// </summary>
    /// <param name="scale">Vector3D</param>
    /// <returns>Transformable</returns>
    public Transformable<T> SetScale(Vector3D<float> scale)
    {
        Model = Matrix4X4.Multiply(Matrix4X4.CreateScale(scale), Model);
        Log.Verbose("Scaled mesh");
        return this;
    }

    /// <summary>
    /// Resets the model to its matrix identity. 
    /// </summary>
    /// <returns>Transformable</returns>
    public Transformable<T> AbsoluteReset()
    {
        Model = Matrix4X4<float>.Identity;
        defaultSet = false;
        Log.Verbose("Absolute reset the mesh model");
        return this;
    }

    /// <summary>
    /// Reset the model to its matrix identity multiplied by a scalar. 
    /// </summary>
    /// <param name="scalar">float</param>
    /// <returns>Transformable</returns>
    public Transformable<T> AbsoluteReset(float scalar)
    {
        Model = Matrix4X4<float>.Identity * scalar;
        defaultSet = false;
        Log.Verbose("Absolute reset mesh model");
        return this;
    }

    /// <summary>
    /// Resets the model to a previously changed state. It requires a lock state to have been created, which can be
    /// created using SetDefault(). 
    /// </summary>
    /// <returns>Transformable</returns>
    /// <exception cref="Exception"></exception>
    public Transformable<T> Reset(bool silent = true)
    {
        if (!defaultSet)
        {
            if (!silent)
                Log.Warning("Unable to reset as a lock state has not been created, resetting absolute");
            AbsoluteReset();
        }
        Model = modelDefault;
        if (!silent)
            Log.Verbose("Resetted the mesh model");
        return this;
    }

    public Transformable<T> Release()
    {
        if (!defaultSet)
            throw new Exception("Unable to release lock state as it has not been created");
        defaultSet = false;
        Log.Debug("Lock state has been released");
        
        return this;
    }

    /// <summary>
    /// "Saves" the previous model state and creates a lock state. It can be reset to its original position at the time
    /// when the lock state was created by using Reset(). 
    /// </summary>
    /// <returns>Transformable</returns>
    public Transformable<T> SetDefault()
    {
        modelDefault = Model;
        defaultSet = true;
        Log.Verbose("Default set the mesh model");
        return this;
    }
    
    /// <summary>
    /// Sets the model
    /// </summary>
    /// <param name="model">Matrix4X4</param>
    /// <returns>Transformable</returns>
    public Transformable<T> SetModel(Matrix4X4<float> model)
    {
        Model = model;
        return this;
    }

    public Vector3D<float> Position
    {
        get => new Vector3D<float>(Model.M41, Model.M42, Model.M43);
    }

    public Vector3D<float> Scale
    {
        get
        {
            // Extract scale from the basis vectors of the matrix
            var scaleX = new Vector3D<float>(Model.M11, Model.M12, Model.M13).Length;
            var scaleY = new Vector3D<float>(Model.M21, Model.M22, Model.M23).Length;
            var scaleZ = new Vector3D<float>(Model.M31, Model.M32, Model.M33).Length;
            return new Vector3D<float>(scaleX, scaleY, scaleZ);
        }
    }

    public Vector3D<float> Rotation
    {
        get
        {
            // Remove scale from the rotation matrix
            var scale = Scale;
            var m11 = Model.M11 / scale.X;
            var m12 = Model.M12 / scale.X;
            var m13 = Model.M13 / scale.X;
            var m21 = Model.M21 / scale.Y;
            var m22 = Model.M22 / scale.Y;
            var m23 = Model.M23 / scale.Y;
            var m31 = Model.M31 / scale.Z;
            var m32 = Model.M32 / scale.Z;
            var m33 = Model.M33 / scale.Z;

            // Extract Euler angles (YXZ order)
            float sy = -m13;
            float cy = MathF.Sqrt(1 - sy * sy);

            float x, y, z;
            if (cy > 1e-6)
            {
                x = MathF.Atan2(m23, m33);
                y = MathF.Asin(sy);
                z = MathF.Atan2(m12, m11);
            }
            else
            {
                x = MathF.Atan2(-m32, m22);
                y = MathF.Asin(sy);
                z = 0;
            }
            return new Vector3D<float>(x, y, z);
        }
    }
}