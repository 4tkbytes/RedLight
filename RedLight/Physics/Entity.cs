using RedLight.Graphics;
using Serilog;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace RedLight.Physics;

// fuck you, you are such a pain in the ass -tk
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

    private uint vbo = 0;
    private uint vao = 0;
    
    // general physics shenanigans
    public Vector3D<float> Velocity { get; set; }
    public Vector3D<float> Acceleration { get; set; }
    public float Mass { get; set; } = 1.0f;

    // bounding box
    public Vector3D<float> BoundingBoxMin { get; set; }
    public Vector3D<float> BoundingBoxMax { get; set; }
    public Vector3D<float> DefaultBoundingBoxMin { get; set; }
    public Vector3D<float> DefaultBoundingBoxMax { get; set; }

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
    
        // Set default bounding box offsets
        DefaultBoundingBoxMin = new Vector3D<float>(-0.5f, 0.0f, -0.5f);
        DefaultBoundingBoxMax = new Vector3D<float>(0.5f, 2.0f, 0.5f);
    
        // Initialize actual bounding box
        BoundingBoxMin = position + DefaultBoundingBoxMin;
        BoundingBoxMax = position + DefaultBoundingBoxMax;
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
    /// Updates the bounding box of the hitbox of the entity. 
    /// </summary>
    public virtual void UpdateBoundingBox()
    {
        Vector3D<float> currentPosition = Vector3D<float>.Zero;
        if (target is Transformable<RLModel> tModel)
        {
            currentPosition = tModel.Position;
        }
        else if (target is Transformable<Mesh> tMesh)
        {
            currentPosition = tMesh.Position;
        }
        else if (target is Transformable<object> tObj)
        {
            currentPosition = tObj.Position;
        }

        // Update the bounding box coordinates
        BoundingBoxMin = currentPosition + DefaultBoundingBoxMin;
        BoundingBoxMax = currentPosition + DefaultBoundingBoxMax;
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
    /// Sets a hitbox default. It can be edited by the hitboxMin and hitboxMax. 
    /// </summary>
    /// <param name="hitboxMin"><see cref="Vector3D"/></param>
    /// <param name="hitboxMax"><see cref="Vector3D"/></param>
    public void SetHitboxDefault(Vector3D<float> hitboxMin, Vector3D<float> hitboxMax)
    {
        if (target is Transformable<RLModel> tModel)
        {
            DefaultBoundingBoxMin = hitboxMin;
            DefaultBoundingBoxMax = hitboxMax;
        }
        else
        {
            Log.Error("Unable to set hitbox default due to not recognising type of entity, target is {Target}",
                Target.GetType().ToString());
        }
    }
    
    /// <summary>
    /// Automatically calculates and sets the hitbox dimensions based on the model's actual vertices.
    /// </summary>
    /// <param name="padding">Optional padding to add around the calculated bounds (default: 0.1f)</param>
    /// <returns>This entity instance for method chaining</returns>
    public virtual Entity<T> AutoMapHitboxToModel(float padding = 0.1f)
    {
        if (target is Transformable<RLModel> tModel)
        {
            var model = tModel.Target;
            
            // Initialize with extreme values
            Vector3D<float> min = new Vector3D<float>(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3D<float> max = new Vector3D<float>(float.MinValue, float.MinValue, float.MinValue);

            // Use the fact that we at least have the meshes to calculate rough bounds
            Log.Debug("Scanning {MeshCount} meshes for model bounds", model.Meshes.Count);
            
            // For now, just set reasonable default bounds if we can't access vertices directly
            // You can modify this based on the model's scale
            DefaultBoundingBoxMin = new Vector3D<float>(-1.0f, -1.0f, -1.0f);
            DefaultBoundingBoxMax = new Vector3D<float>(1.0f, 1.0f, 1.0f);
            
            // Consider model scale (important for scaled models)
            var scale = tModel.Scale;
            DefaultBoundingBoxMin *= scale;
            DefaultBoundingBoxMax *= scale;
            
            // Apply padding
            DefaultBoundingBoxMin -= new Vector3D<float>(padding, padding, padding);
            DefaultBoundingBoxMax += new Vector3D<float>(padding, padding, padding);
            
            // Update the actual bounding box
            UpdateBoundingBox();
            
            Log.Debug("Auto-mapped hitbox for model: Min={Min}, Max={Max}", DefaultBoundingBoxMin, DefaultBoundingBoxMax);
            return this;
        }
        else if (target is Transformable<Mesh> tMesh)
        {
            Log.Warning("Auto-mapping for Mesh type not yet implemented");
        }
        else
        {
            Log.Error("Cannot auto-map hitbox: unsupported target type {Type}", target?.GetType().Name);
        }

        return this;
    }
    
    /// <summary>
    /// Draws the bounding box edges in red using OpenGL lines with proper camera transformations.
    /// </summary>
    public virtual void DrawBoundingBox(RLGraphics graphics, RLShaderBundle shaderBundle, Camera camera)
    {
        if (!isHitboxShown) return;
        
        var gl = graphics.OpenGL;

        // Get current entity position and update bounding box
        Vector3D<float> currentPosition = Vector3D<float>.Zero;
        if (target is Transformable<RLModel> tModel)
        {
            currentPosition = tModel.Position;
        }
        else if (target is Transformable<object> tObj)
        {
            currentPosition = tObj.Position;
        }

        // Add this diagnostic log:
        if (float.IsNaN(currentPosition.X) || float.IsNaN(currentPosition.Y) || float.IsNaN(currentPosition.Z))
        {
            // Identify which entity is causing this, you might want to add a Name property to Entity or check its type
            Log.Error($"Entity.DrawBoundingBox: currentPosition contains NaN. Position: {currentPosition}. Target type: {target?.GetType().FullName}");
        }

        // Calculate bounding box based on current position
        var min = currentPosition + DefaultBoundingBoxMin; 
        var max = currentPosition + DefaultBoundingBoxMax;
        
        // Create vertices for a wireframe box (just the 8 corners)
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

        // Indices for wireframe lines (each edge of the cube)
        uint[] lineIndices = {
            // Bottom face edges
            0, 1, 1, 2, 2, 3, 3, 0,
            // Top face edges  
            4, 5, 5, 6, 6, 7, 7, 4,
            // Vertical edges connecting bottom to top
            0, 4, 1, 5, 2, 6, 3, 7
        };

        if (vao == 0)
        {
            vao = gl.GenVertexArray();
            if (!graphics.ShutUp)
                Log.Debug("any errors after gen vao?");
            graphics.CheckGLErrors();
            
            vbo = gl.GenBuffer();
            if (!graphics.ShutUp)
                Log.Debug("any errors after gen vbo?");
            graphics.CheckGLErrors();
        }

        gl.BindVertexArray(vao);
        if (!graphics.ShutUp)
            Log.Debug("any errors after bind vertex array?");
        graphics.CheckGLErrors();
        
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
        if (!graphics.ShutUp)
            Log.Debug("any errors after bind buffer?");
        graphics.CheckGLErrors();

        unsafe
        {
            fixed (float* vtx = vertices)
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)), vtx,
                    BufferUsageARB.StreamDraw);
                if (!graphics.ShutUp)
                    Log.Debug("any errors after buffer data?");
                graphics.CheckGLErrors();
            }
        }

        gl.EnableVertexAttribArray(0); 
        if (!graphics.ShutUp)
            Log.Debug("any errors after enable vertex attrib array");
        graphics.CheckGLErrors();
        unsafe
        {
            gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);
            if (!graphics.ShutUp)
                Log.Debug("any errors after vertex attrib pointer?");
            graphics.CheckGLErrors();
        }

        gl.UseProgram(shaderBundle.program.ProgramHandle); 
        if (!graphics.ShutUp)
            Log.Debug("any errors after use program?");
        graphics.CheckGLErrors();

        // Set transformation matrices
        unsafe
        {
            // Model matrix (identity since we're using world coordinates)
            var modelMatrix = Matrix4X4<float>.Identity;
            float* modelPtr = (float*)&modelMatrix;
            int modelLoc = gl.GetUniformLocation(shaderBundle.program.ProgramHandle, "model");
            if (modelLoc != -1)
                gl.UniformMatrix4(modelLoc, 1, false, modelPtr);
            if (!graphics.ShutUp)
                Log.Debug("any errors after model matrix?");
            graphics.CheckGLErrors();
            
            // View matrix
            var viewMatrix = camera.View;
            float* viewPtr = (float*)&viewMatrix;
            int viewLoc = gl.GetUniformLocation(shaderBundle.program.ProgramHandle, "view");
            if (viewLoc != -1)
                gl.UniformMatrix4(viewLoc, 1, false, viewPtr);
            if (!graphics.ShutUp)
                Log.Debug("any errors after view matrix?");
            graphics.CheckGLErrors();
            
            // Projection matrix
            var projMatrix = camera.Projection;
            float* projPtr = (float*)&projMatrix;
            int projLoc = gl.GetUniformLocation(shaderBundle.program.ProgramHandle, "projection");
            if (projLoc != -1)
                gl.UniformMatrix4(projLoc, 1, false, projPtr);
            if (!graphics.ShutUp)
                Log.Debug("any errors after proj matrix?");
            graphics.CheckGLErrors();
        }

        // Set the color uniform (red for hitbox)
        int colorLoc = gl.GetUniformLocation(shaderBundle.program.ProgramHandle, "uColor");
        if (colorLoc != -1) // This check prevents error if uColor is not found
            gl.Uniform4(colorLoc, 1.0f, 0.0f, 0.0f, 1.0f);
        if (!graphics.ShutUp)
            Log.Debug("any errors after setting colour uniform?");
        graphics.CheckGLErrors();

        // Set line width for thicker lines
        gl.LineWidth(1.0f);
        
        if (!graphics.ShutUp)
            Log.Debug("any errors after setting line width?");
        
        graphics.CheckGLErrors();

        // Draw the wireframe using line segments
        unsafe
        {
            fixed (uint* indices = lineIndices)
            {
                gl.DrawElements(PrimitiveType.Lines, (uint)lineIndices.Length, DrawElementsType.UnsignedInt, indices);
                if (!graphics.ShutUp)
                    Log.Debug("any errors after drawing line?");
                graphics.CheckGLErrors();
            }
        }

        gl.BindVertexArray(0);
        if (!graphics.ShutUp)
            Log.Debug("any errors after unbinding vao?");
        graphics.CheckGLErrors();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        if (!graphics.ShutUp)
            Log.Debug("any errors after unbinding vbo?");
        graphics.CheckGLErrors();

        graphics.CheckGLErrors();
    }
}

/// <summary>
/// This class is used to conveniently overcomplicate everything. Due to the default <see cref="Entity{T}"/> class 
/// being an abstract, we need to store the Entities virtual functions.
/// <para>
/// This class is used to solve the issue (CS0144). 
/// Despite being inconvenient and annoying, this is the only way (afaik) that you can
/// create a new Entity class.
/// </para>
/// </summary>
/// <typeparam name="T"><see cref="Transformable{T}"/></typeparam>
public class ConcreteEntity<T> : Entity<T>
{
    public ConcreteEntity(T target) : base(target) { }
}