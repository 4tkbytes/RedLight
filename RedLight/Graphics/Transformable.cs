using System.Numerics;
using Silk.NET.Maths;
using RedLight.Utils;
using Serilog;
using RedLight.Physics;

namespace RedLight.Graphics;

/// <summary>
/// This class makes any class transformable. Typically used with Meshes or RLModels, this class can allow you to
/// translate the model, rotate the model or change its scale. 
/// </summary>
/// <typeparam name="T"><seealso cref="RLModel"/><seealso cref="Mesh"/></typeparam>
public abstract class Transformable<T>
{
    private T target;
    private bool defaultSet;
    private Matrix4X4<float> modelDefault = Matrix4X4<float>.Identity;
    
    public Vector3 eulerAngles = new Vector3(0, 0, 0);
    public Matrix4X4<float> ModelMatrix { get; private set; } = Matrix4X4<float>.Identity;

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
    /// <param name="translation"><see cref="Vector3D"/></param>
    /// <returns><see cref="Transformable{T}"/></returns>
    public Transformable<T> Translate(Vector3D<float> translation)
    {
        ModelMatrix = Matrix4X4.Multiply(Matrix4X4.CreateTranslation(translation), ModelMatrix);
        Log.Verbose("Translated mesh");
        return this;
    }

    /// <summary>
    /// Rotates the target by a radians and its axis (typically Vector3D's UnitX, UnitY or UnitZ).
    /// </summary>
    /// <param name="radians">float</param>
    /// <param name="axis"><see cref="Vector3D"/></param>
    /// <returns><see cref="Transformable{T}"/></returns>
    public Transformable<T> Rotate(float radians, Vector3D<float> axis)
    {
        var normAxis = Vector3D.Normalize(axis);
        var rotation = Matrix4X4.CreateFromAxisAngle(normAxis, radians);
        ModelMatrix = Matrix4X4.Multiply(rotation, ModelMatrix);
        Log.Verbose("Rotated mesh");
        return this;
    }

    /// <summary>
    /// Changes the scale of the model. Takes an input of scale and multiplies the model matrix by the scale. 
    /// </summary>
    /// <param name="scale"><see cref="Vector3D"/></param>
    /// <returns><see cref="Transformable{T}"/></returns>
    public Transformable<T> SetScale(Vector3D<float> scale)
    {
        ModelMatrix = Matrix4X4.Multiply(Matrix4X4.CreateScale(scale), ModelMatrix);
        Log.Verbose("Scaled mesh");
        return this;
    }
    
    public Transformable<T> SetPosition(Vector3D<float> position)
    {
        // Create a new model matrix preserving rotation and scale, but with new position
        Matrix4X4<float> newModel = ModelMatrix;
    
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
        ModelMatrix = Matrix4X4<float>.Identity;
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
        ModelMatrix = Matrix4X4<float>.Identity * scalar;
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
    /// <param name="model"><see cref="Matrix4X4"/></param>
    /// <returns><see cref="Transformable{T}"/></returns>
    public Transformable<T> SetModel(Matrix4X4<float> model)
    {
        ModelMatrix = model;
        return this;
    }

    /// <summary>
    /// This function converts a <see cref="Transformable{T}"/> into an <see cref="Entity{T}"/> to unlock
    /// physics and collisions. 
    /// </summary>
    /// <returns></returns>
    public ConcreteEntity<Transformable<T>> MakeEntity()
    {
        return new ConcreteEntity<Transformable<T>>(this);
    }

    public Vector3D<float> Position
    {
        // ez
        get => new Vector3D<float>(ModelMatrix.M41, ModelMatrix.M42, ModelMatrix.M43);
    }

    public Vector3D<float> Scale
    {
        // this is so confusing holy shit
        get
        {
            var scaleX = new Vector3D<float>(ModelMatrix.M11, ModelMatrix.M12, ModelMatrix.M13).Length;
            var scaleY = new Vector3D<float>(ModelMatrix.M21, ModelMatrix.M22, ModelMatrix.M23).Length;
            var scaleZ = new Vector3D<float>(ModelMatrix.M31, ModelMatrix.M32, ModelMatrix.M33).Length;
            return new Vector3D<float>(scaleX, scaleY, scaleZ);
        }
    }

    public Vector3D<float> Rotation
    {
        // this is so confusing holy shit
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
            return new Vector3D<float>(x, y, z);
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