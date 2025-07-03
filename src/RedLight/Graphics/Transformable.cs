using System.Numerics;
using Serilog;
using RedLight.Entities;

namespace RedLight.Graphics;

public enum Axis
{
    X,
    Y,
    Z
}

/// <summary>
/// This class makes any class transformable. Typically used with Meshes or RLModels, this class can allow you to
/// translate the model, rotate the model or change its scale. 
/// </summary>
/// <typeparam name="T"><seealso cref="RLModel"/><seealso cref="Mesh"/></typeparam>
public abstract class Transformable<T>
{
    private T target;
    private bool defaultSet;
    private Matrix4x4 modelDefault = Matrix4x4.Identity;

    public Vector3 eulerAngles = new Vector3(0, 0, 0);
    public Matrix4x4 ModelMatrix { get; private set; } = Matrix4x4.Identity;

    /// <summary>
    /// The target, specifically what you put in when you initialised a Transformable, or it
    /// could be an RLModel/Mesh depending on how the program feels. 
    /// </summary>
    public T Target
    {
        get { return target; }
        set { target = value; }
    }

    public Transformable(T target)
    {
        this.target = target;
    }

    /// <summary>
    /// Translate the target by a Vector3D float. 
    /// </summary>
    /// <param name="translation"><see cref="Vector3"/></param>
    /// <returns><see cref="Transformable{T}"/></returns>
    public Transformable<T> Translate(Vector3 translation)
    {
        ModelMatrix = Matrix4x4.Multiply(Matrix4x4.CreateTranslation(translation), ModelMatrix);
        Log.Verbose("Translated mesh");
        return this;
    }

    /// <summary>
    /// Rotates the target by a radians and its axis (typically Vector3's UnitX, UnitY or UnitZ).
    /// </summary>
    /// <param name="radians">float</param>
    /// <param name="axis"><see cref="Vector3"/></param>
    /// <returns><see cref="Transformable{T}"/></returns>
    public Transformable<T> Rotate(float radians, Axis axis)
    {
        var normAxis = axis switch
        {
            Axis.X => Vector3.UnitX,
            Axis.Y => Vector3.UnitY,
            Axis.Z => Vector3.UnitZ,
            _ => Vector3.UnitY
        };
        var rotation = Matrix4x4.CreateFromAxisAngle(normAxis, radians);
        ModelMatrix = Matrix4x4.Multiply(ModelMatrix, rotation);
        return this;
    }

    /// <summary>
    /// Sets the rotation of the model, not to be confused by <see cref="Rotate"/>
    /// where you can rotate, this resets the model and rotates. Rerunning the
    /// rotation 
    /// </summary>
    /// <param name="radians"></param>
    /// <param name="axis"></param>
    /// <returns></returns>
    public Transformable<T> SetRotation(float radians, Axis axis)
    {
        var normAxis = axis switch
        {
            Axis.X => Vector3.UnitX,
            Axis.Y => Vector3.UnitY,
            Axis.Z => Vector3.UnitZ,
            _ => Vector3.UnitY
        };
        ModelMatrix = Matrix4x4.CreateFromAxisAngle(normAxis, radians);
        return this;
    }

    /// <summary>
    /// Changes the scale of the model. Takes an input of scale and multiplies the model matrix by the scale. 
    /// </summary>
    /// <param name="scale"><see cref="Vector3"/></param>
    /// <returns><see cref="Transformable{T}"/></returns>
    public Transformable<T> SetScale(Vector3 scale)
    {
        ModelMatrix = Matrix4x4.Multiply(Matrix4x4.CreateScale(scale), ModelMatrix);
        Log.Verbose("Scaled mesh");
        return this;
    }

    public Transformable<T> SetPosition(Vector3 position)
    {
        // Create a new model matrix preserving rotation and scale, but with new position
        Matrix4x4 newModel = ModelMatrix;

        // Update only the translation components
        newModel.M41 = position.X;
        newModel.M42 = position.Y;
        newModel.M43 = position.Z;

        ModelMatrix = newModel;

        Log.Verbose("Set position to {Position}", position);
        return this;
    }

    /// <summary>
    /// Resets the model to its matrix identity. 
    /// </summary>
    /// <returns><see cref="Transformable{T}"/></returns>
    public Transformable<T> AbsoluteReset()
    {
        ModelMatrix = Matrix4x4.Identity;
        defaultSet = false;
        Log.Verbose("Absolute reset the mesh model");
        return this;
    }

    /// <summary>
    /// Reset the model to its matrix identity multiplied by a scalar. 
    /// </summary>
    /// <param name="scalar"><see cref="float"/></param>
    /// <returns><see cref="Transformable{T}"/></returns>
    public Transformable<T> AbsoluteReset(float scalar)
    {
        ModelMatrix = Matrix4x4.Identity * scalar;
        defaultSet = false;
        Log.Verbose("Absolute reset mesh model");
        return this;
    }

    /// <summary>
    /// Resets the model to a previously changed state. It requires a lock state to have been created, which can be
    /// created using SetDefault(). 
    /// </summary>
    /// <returns><see cref="Transformable{T}"/></returns>
    /// <exception cref="Exception"></exception>
    public Transformable<T> Reset(bool silent = true)
    {
        if (!defaultSet)
        {
            if (!silent)
                Log.Warning("Unable to reset as a lock state has not been created, resetting absolute");
            AbsoluteReset();
        }
        ModelMatrix = modelDefault;
        if (!silent)
            Log.Verbose("Resetted the mesh model");
        return this;
    }

    /// <summary>
    /// Releases the lock state by setting the related boolean to false. To reuse the lock state,
    /// you will have to use recreate another lock state by using <see cref="Transformable{T}.SetDefault"/>
    /// </summary>
    /// <returns><see cref="Transformable{T}"/> Self</returns>
    /// <exception cref="Exception"></exception>
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
    /// <returns><see cref="Transformable{T}"/></returns>
    public Transformable<T> SetDefault()
    {
        modelDefault = ModelMatrix;
        defaultSet = true;
        Log.Verbose("Default set the mesh model");
        return this;
    }

    /// <summary>
    /// Sets the model matrix
    /// </summary>
    /// <param name="model"><see cref="Matrix4x4"/></param>
    /// <returns><see cref="Transformable{T}"/></returns>
    public Transformable<T> SetModel(Matrix4x4 model)
    {
        ModelMatrix = model;
        return this;
    }

    /// <summary>
    /// Extension method to make an RLModel transformable
    /// </summary>
    /// <returns></returns>
    public static ConcreteTransformable<T> MakeTransformable(T target)
    {
        return new ConcreteTransformable<T>(target);
    }

    public Vector3 Position
    {
        get => new Vector3(ModelMatrix.M41, ModelMatrix.M42, ModelMatrix.M43);
    }

    public Vector3 Scale
    {
        get
        {
            var scaleX = new Vector3(ModelMatrix.M11, ModelMatrix.M12, ModelMatrix.M13).Length();
            var scaleY = new Vector3(ModelMatrix.M21, ModelMatrix.M22, ModelMatrix.M23).Length();
            var scaleZ = new Vector3(ModelMatrix.M31, ModelMatrix.M32, ModelMatrix.M33).Length();
            return new Vector3(scaleX, scaleY, scaleZ);
        }
    }

    public Vector3 Rotation
    {
        get
        {
            var scale = Scale;
            var m11 = ModelMatrix.M11 / scale.X;
            var m12 = ModelMatrix.M12 / scale.X;
            var m13 = ModelMatrix.M13 / scale.X;
            var m21 = ModelMatrix.M21 / scale.Y;
            var m22 = ModelMatrix.M22 / scale.Y;
            var m23 = ModelMatrix.M23 / scale.Y;
            var m31 = ModelMatrix.M31 / scale.Z;
            var m32 = ModelMatrix.M32 / scale.Z;
            var m33 = ModelMatrix.M33 / scale.Z;

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
            return new Vector3(x, y, z);
        }
    }
}

/// <summary>
/// Creates a class that is a Transformable if it was a class. 
/// </summary>
/// <typeparam name="T"></typeparam>
public class ConcreteTransformable<T> : Transformable<T>
{
    public ConcreteTransformable(T target) : base(target) { }
}

/// <summary>
/// Extension methods for making objects transformable
/// </summary>
public static class TransformableExtensions
{
    /// <summary>
    /// Extension method to make an RLModel transformable
    /// </summary>
    public static ConcreteTransformable<RLModel> MakeTransformable(this RLModel model)
    {
        return new ConcreteTransformable<RLModel>(model);
    }
}