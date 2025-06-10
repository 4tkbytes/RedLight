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
public abstract class Entity<T> : Transformable<T>
{
    public bool isHitboxShown;
    
    // hitbox shaders
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
    
    // collisions
    public HashSet<CollisionSide> ObjectCollisionSides { get; set; } = new();

    public Entity(T transformable) : base(transformable)
    {
        Vector3D<float> position = Vector3D<float>.Zero;
        if (transformable is Transformable<RLModel> tModel)
            position = tModel.Position;
        else if (transformable is Transformable<Mesh> tMesh)
            position = tMesh.Position;
        else if (transformable is Transformable<object> tObj)
            position = tObj.Position;

        DefaultBoundingBoxMin = new Vector3D<float>(-0.5f, 0.0f, -0.5f);
        DefaultBoundingBoxMax = new Vector3D<float>(0.5f, 2.0f, 0.5f);

        BoundingBoxMin = position + DefaultBoundingBoxMin;
        BoundingBoxMax = position + DefaultBoundingBoxMax;
    }

    /// <summary>
    /// Updates the physics state of the entity.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last update (in seconds).</param>
    public void UpdatePhysics(float deltaTime)
    {
        ObjectCollisionSides.Clear();
        UpdateBoundingBox();
        Velocity += Acceleration * deltaTime;
        if (Target is Transformable<RLModel> tModel)
            tModel.SetPosition(Velocity * deltaTime);
        Acceleration = Vector3D<float>.Zero;
    }
    
    /// <summary>
    /// Updates the bounding box of the hitbox of the entity. 
    /// </summary>
    public void UpdateBoundingBox()
    {
        Vector3D<float> currentPosition = Vector3D<float>.Zero;
        if (Target is Transformable<RLModel> tModel)
            currentPosition = tModel.Position;
        else if (Target is Transformable<Mesh> tMesh)
            currentPosition = tMesh.Position;
        else if (Target is Transformable<object> tObj)
            currentPosition = tObj.Position;

        BoundingBoxMin = currentPosition + DefaultBoundingBoxMin;
        BoundingBoxMax = currentPosition + DefaultBoundingBoxMax;
    }

    /// <summary>
    /// Checks for collision with another entity using the AABB collision method.
    /// 
    /// <see href="https://developer.mozilla.org/en-US/docs/Games/Techniques/3D_collision_detection">
    /// Mozilla 3D Game Dev Documentation about how AABB works</see>
    /// </summary>
    public bool Intersects(Entity<T> otherEntity, bool silent = true)
    {
        ObjectCollisionSides.Clear();

        bool xOverlap = BoundingBoxMin.X <= otherEntity.BoundingBoxMax.X && BoundingBoxMax.X >= otherEntity.BoundingBoxMin.X;
        bool yOverlap = BoundingBoxMin.Y <= otherEntity.BoundingBoxMax.Y && BoundingBoxMax.Y >= otherEntity.BoundingBoxMin.Y;
        bool zOverlap = BoundingBoxMin.Z <= otherEntity.BoundingBoxMax.Z && BoundingBoxMax.Z >= otherEntity.BoundingBoxMin.Z;

        if (xOverlap && yOverlap && zOverlap)
        {
            // Check which sides are colliding
            if (BoundingBoxMax.X >= otherEntity.BoundingBoxMin.X && BoundingBoxMin.X < otherEntity.BoundingBoxMin.X)
            {
                if (!silent)
                    Log.Debug("Colliding on the right");
                ObjectCollisionSides.Add(CollisionSide.Right);
            }
            if (BoundingBoxMin.X <= otherEntity.BoundingBoxMax.X && BoundingBoxMax.X > otherEntity.BoundingBoxMax.X)
            {
                if (!silent)
                    Log.Debug("Colliding on the left");
                ObjectCollisionSides.Add(CollisionSide.Left);
            }

            if (BoundingBoxMax.Y >= otherEntity.BoundingBoxMin.Y && BoundingBoxMin.Y < otherEntity.BoundingBoxMin.Y)
            {
                if (!silent)
                    Log.Debug("Colliding on the up");
                ObjectCollisionSides.Add(CollisionSide.Up);
            }
            if (BoundingBoxMin.Y <= otherEntity.BoundingBoxMax.Y && BoundingBoxMax.Y > otherEntity.BoundingBoxMax.Y)
            {
                if (!silent)
                    Log.Debug("Colliding on the down");
                ObjectCollisionSides.Add(CollisionSide.Down);
            }

            if (BoundingBoxMax.Z >= otherEntity.BoundingBoxMin.Z && BoundingBoxMin.Z < otherEntity.BoundingBoxMin.Z)
            {
                if (!silent)
                    Log.Debug("Colliding on the front");
                ObjectCollisionSides.Add(CollisionSide.Front);
            }
            if (BoundingBoxMin.Z <= otherEntity.BoundingBoxMax.Z && BoundingBoxMax.Z > otherEntity.BoundingBoxMax.Z)
            {
                if (!silent)
                    Log.Debug("Colliding on the back");
                ObjectCollisionSides.Add(CollisionSide.Back);
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Sets a hitbox default. It can be edited by the hitboxMin and hitboxMax. 
    /// </summary>
    /// <param name="hitboxMin"><see cref="Vector3D"/></param>
    /// <param name="hitboxMax"><see cref="Vector3D"/></param>
    public void SetHitboxDefault(Vector3D<float> hitboxMin, Vector3D<float> hitboxMax)
    {
        DefaultBoundingBoxMin = hitboxMin;
        DefaultBoundingBoxMax = hitboxMax;
    }

    public void DontMoveIfColliding(Entity<T> otherEntity, bool silent = true)
    {
        silent = false;
        bool isColliding = Intersects(otherEntity);
        if (!silent)
            Log.Debug("IsColliding = {IsColliding}", isColliding);

        if (!silent)
            Log.Debug("otherEntity is {OtherEntity}", otherEntity);
        if (otherEntity.Target is Transformable<RLModel> tModel)
        {
            foreach (CollisionSide side in ObjectCollisionSides)
                if (!silent)
                    Log.Debug("Colliding on {A}", side);
            
            if (isColliding && ObjectCollisionSides.Contains(CollisionSide.Down))
            {
                // For a bottom collision, ensure the model stays above the other entity's top face
                float otherEntityTopY = otherEntity.BoundingBoxMax.Y;
                if (!silent)
                    Log.Debug("Other entity top Y: {TopY}", otherEntityTopY);
    
                // Place the model so its bottom face rests on the other entity's top face
                float modelBottomY = tModel.Position.Y + DefaultBoundingBoxMin.Y;
                if (!silent)
                    Log.Debug("Model bottom Y: {BottomY}", modelBottomY);
                float adjustment = otherEntityTopY - modelBottomY;
                if (!silent)
                    Log.Debug("Adjustment: {Adjustment}", adjustment);
    
                tModel.SetPosition(new Vector3D<float>(tModel.Position.X, 
                    tModel.Position.Y + adjustment, 
                    tModel.Position.Z));
            }
        }
    }
    
    /// <summary>
    /// Automatically calculates and sets the hitbox dimensions based on the model's actual vertices.
    /// </summary>
    /// <param name="padding">Optional padding to add around the calculated bounds (default: 0.1f)</param>
    /// <returns>This entity instance for method chaining</returns>
    public Entity<T> AutoMapHitboxToModel(float padding = 0.1f)
    {
        if (Target is Transformable<RLModel> tModel)
        {
            var model = tModel.Target;
            DefaultBoundingBoxMin = new Vector3D<float>(-1.0f, -1.0f, -1.0f);
            DefaultBoundingBoxMax = new Vector3D<float>(1.0f, 1.0f, 1.0f);
            
            var scale = tModel.Scale;
            DefaultBoundingBoxMin *= scale;
            DefaultBoundingBoxMax *= scale;

            DefaultBoundingBoxMin -= new Vector3D<float>(padding, padding, padding);
            DefaultBoundingBoxMax += new Vector3D<float>(padding, padding, padding);

            UpdateBoundingBox();

            Log.Debug("Auto-mapped hitbox for model: Min={Min}, Max={Max}", DefaultBoundingBoxMin, DefaultBoundingBoxMax);
        }
        else
        {
            Log.Error("Cannot auto-map hitbox: unsupported Target type {Type}", Target?.GetType().Name);
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
        if (Target is Transformable<RLModel> tModel)
        {
            currentPosition = tModel.Position;
        }
        else if (Target is Transformable<object> tObj)
        {
            currentPosition = tObj.Position;
        }

        // Add this diagnostic log:
        if (float.IsNaN(currentPosition.X) || float.IsNaN(currentPosition.Y) || float.IsNaN(currentPosition.Z))
        {
            // Identify which entity is causing this, you might want to add a Name property to Entity or check its type
            Log.Error($"Entity.DrawBoundingBox: currentPosition contains NaN. Position: {currentPosition}. Target type: {Target?.GetType().FullName}");
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
    public ConcreteEntity(T Target) : base(Target) { }
}

/// <summary>
/// Enum representing which side is being collided. Numerical values are taken inspiration from
/// dice. If you forgot, look for a 3D model of a die. 
/// </summary>
public enum CollisionSide
{
    Left = 3,
    Right = 4,
    Up = 2,
    Down = 5,
    Front = 1,
    Back = 6
}