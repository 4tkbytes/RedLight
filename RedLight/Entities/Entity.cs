using System.Numerics;
using RedLight.Graphics;
using Serilog;
using Silk.NET.OpenGL;
using System.Numerics;

namespace RedLight.Entities;

// fuck you, you are such a pain in the ass -tk
/// <summary>
/// This class converts any Transformable Model into an Entity, which can unlock
/// any physics based logic, such as collisions and hitboxes. 
/// </summary>
/// <typeparam name="T"><see cref="Transformable{T}"/></typeparam>
public abstract class Entity<T> : Transformable<T>
{
    // hitbox shaders
    private uint vbo = 0;
    private uint vao = 0;
    
    // general physics shenanigans
    public const float Gravity = 9.81f;
    public Vector3 Velocity { get; set; } = Vector3.Zero;
    public bool ApplyGravity { get; set; }
    public float Mass { get; set; } = 1f;   // default value is 1f gotta create a func to change it

    // bounding box
    public Vector3 BoundingBoxMin { get; set; }
    public Vector3 BoundingBoxMax { get; set; }
    public Vector3 DefaultBoundingBoxMin { get; set; }
    public Vector3 DefaultBoundingBoxMax { get; set; }

    // hitbox changing
    public bool IsHitboxShown { get; private set; }
    public void ShowHitbox() => IsHitboxShown = true;
    public void HideHitbox() => IsHitboxShown = false;
    public void ToggleHitbox() => IsHitboxShown = !IsHitboxShown;
    
    // collisions
    public HashSet<CollisionSide> ObjectCollisionSides { get; set; } = new();
    public bool IsColliding { get; private set; }

    // bepu physics
    public PhysicsSystem PhysicsSystem;
    private HashSet<string> _registeredEntityNames = new();

    public Entity(T transformable, bool applyGravity = true) : base(transformable)
    {
        ApplyGravity = applyGravity;

        Vector3 position = Vector3.Zero;
        if (transformable is Transformable<RLModel> tModel)
            position = tModel.Position;
        else if (transformable is Transformable<Mesh> tMesh)
            position = tMesh.Position;
        else if (transformable is Transformable<object> tObj)
            position = tObj.Position;

        DefaultBoundingBoxMin = new Vector3(-0.5f, 0.0f, -0.5f);
        DefaultBoundingBoxMax = new Vector3(0.5f, 2.0f, 0.5f);

        BoundingBoxMin = position + DefaultBoundingBoxMin;
        BoundingBoxMax = position + DefaultBoundingBoxMax;
    }

    /// <summary>
    /// Initialises the physics system
    /// </summary>
    public virtual void InitPhysics(PhysicsSystem physics)
    {
        PhysicsSystem = physics;
        if (this is Entity<Transformable<RLModel>> modelEntity)
        {
            PhysicsSystem.AddEntity(modelEntity);
        }
    }

    public virtual void Update(float deltaTime, HashSet<Silk.NET.Input.Key> pressedKeys = null, bool isUsingDebugCamera = false, bool silent = true)
    {
        
    }

    

    

    
    
    /// <summary>
    /// Draws the bounding box edges in red using OpenGL lines with proper camera transformations.
    /// </summary>
    public void DrawBoundingBox(RLGraphics graphics, Camera camera)
    {
        if (!IsHitboxShown) return;

        var shaderBundle = ShaderManager.Instance.Get("hitbox");
        
        var gl = graphics.OpenGL;

        // Get current entity position and update bounding box
        Vector3 currentPosition = Vector3.Zero;
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
            var modelMatrix = Matrix4x4.Identity;
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

/// <summary>
/// Configuration class for model hitboxes
/// </summary>
public class HitboxConfig
{
    /// <summary>Width of hitbox in X dimension</summary>
    public float Width { get; set; } = 1.0f;

    /// <summary>Height of hitbox in Y dimension</summary>
    public float Height { get; set; } = 1.0f;

    /// <summary>Length of hitbox in Z dimension</summary>
    public float Length { get; set; } = 1.0f;

    /// <summary>
    /// Portion of the hitbox below the model's center point (0.0-1.0)
    /// 0.5 = half below/half above
    /// 1.0 = bottom at ground level
    /// 0.0 = bottom at center level
    /// </summary>
    public float GroundOffset { get; set; } = 0.5f;

    /// <summary>
    /// Offset from the model's center in each dimension
    /// </summary>
    public Vector3 CenterOffset { get; set; } = Vector3.Zero;
}