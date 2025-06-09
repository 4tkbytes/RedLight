using RedLight.Graphics;
using Serilog;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace RedLight.Physics;

/// <summary>
/// This class converts any Transformable Model into an Entity, which can unlock
/// any physics based logic, such as collisions and hitboxes. 
/// </summary>
/// <typeparam name="T"><see cref="Transformable{T}"/></typeparam>
public abstract class Entity<T>
{
    protected readonly T target;
    public bool isHitboxShown;
    public T Target => target;

    private static uint vbo = 0;
    private static uint vao = 0;
    
    // general physics shenanigans
    public Vector3D<float> Velocity { get; set; }
    public Vector3D<float> Acceleration { get; set; }
    public float Mass { get; set; } = 1.0f;

    // bounding box
    public Vector3D<float> BoundingBoxMin { get; set; }
    public Vector3D<float> BoundingBoxMax { get; set; }

    // hitbox changing
    public void ShowHitbox() => isHitboxShown = true;
    public void HideHitbox() => isHitboxShown = false;
    public void ToggleHitbox() => isHitboxShown = !isHitboxShown;


    public Entity(T target)
    {
        this.target = target;

        Vector3D<float> position = Vector3D<float>.Zero;
        if (target is Transformable<RLModel> tModel)
        {
            position = tModel.Position;
        }
        else if (target is Transformable<Mesh> tMesh)
        {
            position = tMesh.Position;
        }
        else if (target is Transformable<object> tObj)
        {
            position = tObj.Position;
        }

        BoundingBoxMin = position + new Vector3D<float>(-0.5f, 0.0f, -0.5f);
        BoundingBoxMax = position + new Vector3D<float>(0.5f, 2.0f, 0.5f);
    }

    /// <summary>
    /// Updates the physics state of the entity.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last update (in seconds).</param>
    public virtual void UpdatePhysics(float deltaTime)
    {
        // Basic Euler integration
        Velocity += Acceleration * deltaTime;

        // If T is Transformable, update its position
        if (target is Transformable<RLModel> transformable)
        {
            transformable.SetPosition(Velocity * deltaTime);
        }
        // Reset acceleration after each update (if using force-based system)
        Acceleration = Vector3D<float>.Zero;
    }

    /// <summary>
    /// Applies a force to the entity (F = m * a).
    /// </summary>
    public virtual void ApplyForce(Vector3D<float> force)
    {
        Acceleration += force / Mass;
    }

    /// <summary>
    /// Checks for collision with another entity using the AABB collision method.
    /// 
    /// <see href="https://developer.mozilla.org/en-US/docs/Games/Techniques/3D_collision_detection">
    /// Mozilla 3D Game Dev Documentation about how AABB works</see>
    /// </summary>
    public virtual bool Intersects(Entity<T> otherEntity)
    {
        return (BoundingBoxMin.X <= otherEntity.BoundingBoxMax.X && BoundingBoxMax.X >= otherEntity.BoundingBoxMin.X) &&
               (BoundingBoxMin.Y <= otherEntity.BoundingBoxMax.Y && BoundingBoxMax.Y >= otherEntity.BoundingBoxMin.Y) &&
               (BoundingBoxMin.Z <= otherEntity.BoundingBoxMax.Z && BoundingBoxMax.Z >= otherEntity.BoundingBoxMin.Z);
    }

    /// <summary>
    /// Draws the bounding box edges in red using OpenGL lines.
    /// </summary>
    public virtual void DrawBoundingBox(RLGraphics graphics, RLShaderBundle shaderBundle)
    {
        var gl = graphics.OpenGL;

        var min = BoundingBoxMin;
        var max = BoundingBoxMax;
        float[] vertices = new float[]
        {
            min.X, min.Y, min.Z, // 0
            max.X, min.Y, min.Z, // 1
            max.X, max.Y, min.Z, // 2
            min.X, max.Y, min.Z, // 3
            min.X, min.Y, max.Z, // 4
            max.X, min.Y, max.Z, // 5
            max.X, max.Y, max.Z, // 6
            min.X, max.Y, max.Z  // 7
        };

        // Each face: 2 triangles (6 indices), 6 faces
        int[] indices = {
            // Bottom
            0, 1, 2, 2, 3, 0,
            // Top
            4, 5, 6, 6, 7, 4,
            // Front
            0, 1, 5, 5, 4, 0,
            // Back
            3, 2, 6, 6, 7, 3,
            // Left
            0, 3, 7, 7, 4, 0,
            // Right
            1, 2, 6, 6, 5, 1
        };

        float[] cubeVertices = new float[indices.Length * 3];
        for (int i = 0; i < indices.Length; i++)
        {
            int idx = indices[i];
            cubeVertices[i * 3 + 0] = vertices[idx * 3 + 0];
            cubeVertices[i * 3 + 1] = vertices[idx * 3 + 1];
            cubeVertices[i * 3 + 2] = vertices[idx * 3 + 2];
        }

        if (vao == 0)
        {
            vao = gl.GenVertexArray();
            vbo = gl.GenBuffer();
        }

        gl.BindVertexArray(vao);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);

        unsafe
        {
            fixed (float* vtx = cubeVertices)
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(cubeVertices.Length * sizeof(float)), vtx, BufferUsageARB.StreamDraw);
            }
        }

        gl.EnableVertexAttribArray(0);
        unsafe
        {
            gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);
        }

        gl.UseProgram(shaderBundle.program.ProgramHandle);

        int colorLoc = gl.GetUniformLocation(shaderBundle.program.ProgramHandle, "uColor");
        if (colorLoc != -1)
            gl.Uniform4(colorLoc, 1.0f, 0.0f, 0.0f, 1.0f); // Solid red

        gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)indices.Length);

        gl.BindVertexArray(0);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

        graphics.CheckGLErrors();
    }
}

/// <summary>
/// This class is used to conveniently overcomplicate everything. Due to the default <see cref="Entity{T}"/> class 
/// being an abstract, we need to store the Entities virtual functions. 
/// 
/// 
/// This class is used to solve the issue (specifically CS0144). 
/// Despite being inconvenient and annoying, this is the only way (afaik) that you can
/// create a new Entity class. 
/// </summary>
/// <typeparam name="T"></typeparam>
public class ConcreteEntity<T> : Entity<T>
{
    public ConcreteEntity(T target) : base(target) { }
}