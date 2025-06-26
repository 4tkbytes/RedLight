using System.Numerics;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using RedLight.Graphics;
using RedLight.Lighting;
using RedLight.Physics;
using RedLight.Scene;
using Serilog;
using Silk.NET.OpenGL;

namespace RedLight.Entities;

/// <summary>
/// Base class for all entities in the game world.
/// Provides physics, collision detection, and transformation capabilities.
/// </summary>
public abstract class Entity
{
    private RLModel _model;

    private Matrix4x4 _modelMatrix = Matrix4x4.Identity;
    private bool _defaultSet;
    private Matrix4x4 _modelDefault = Matrix4x4.Identity;

    protected Vector3 _positionDefault;
    protected Vector3 _rotationDefault;
    protected Vector3 _scaleDefault;

    public Vector3 Velocity { get; set; } = Vector3.Zero;
    public bool ApplyGravity { get; set; } = true;
    public bool EnablePhysics { get; set; } = true;
    public float Mass { get; set; } = 1f;
    public float FrictionCoefficient { get; set; } = 1.0f;

    public Vector3 BoundingBoxMin { get; set; }
    public Vector3 BoundingBoxMax { get; set; }
    public Vector3 DefaultBoundingBoxMin { get; set; }
    public Vector3 DefaultBoundingBoxMax { get; set; }
    public HitboxConfig HitboxConfig { get; protected set; } = new();
    public HashSet<CollisionSide> ObjectCollisionSides { get; set; } = new();
    public bool IsColliding { get; internal set; }
    public bool IsHitboxShown { get; private set; }

    public string Name { get; private set; }

    private uint vbo;
    private uint vao;

    public PhysicsSystem PhysicsSystem;
    private HashSet<string> _registeredEntityNames = new();

    public bool UseImGuizmo { get; set; } = false;
    public ImGuizmoOperation GuizmoOperation { get; set; } = ImGuizmoOperation.Translate;
    public ImGuizmoMode GuizmoMode { get; set; } = ImGuizmoMode.World;

    public ModelType ModelType { get; set; }

    public bool EnableReflection { get; set; }
    public float Reflectivity { get; set; } = 0.3f;

    public bool EnableRefraction { get; set; }
    public float RefractiveIndex { get; set; } = Lighting.RefractiveIndex.Air;

    /// <summary>
    /// Direct access to the underlying RLModel
    /// </summary>
    public RLModel Model
    {
        get => _model;
        protected set => _model = value;
    }

    /// <summary>
    /// The transformation matrix for this entity
    /// </summary>
    public Matrix4x4 ModelMatrix
    {
        get => _modelMatrix;
        private set => _modelMatrix = value;
    }

    /// <summary>
    /// Current position of the entity
    /// </summary>
    public Vector3 Position
    {
        get => new Vector3(ModelMatrix.M41, ModelMatrix.M42, ModelMatrix.M43);
        set => SetPosition(value);
    }

    /// <summary>
    /// Current scale of the entity
    /// </summary>
    public Vector3 Scale
    {
        get
        {
            var scaleX = new Vector3(ModelMatrix.M11, ModelMatrix.M12, ModelMatrix.M13).Length();
            var scaleY = new Vector3(ModelMatrix.M21, ModelMatrix.M22, ModelMatrix.M23).Length();
            var scaleZ = new Vector3(ModelMatrix.M31, ModelMatrix.M32, ModelMatrix.M33).Length();
            return new Vector3(scaleX, scaleY, scaleZ);
        }
        set => SetScale(value);
    }

    /// <summary>
    /// Current rotation of the entity (Euler angles)
    /// </summary>
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
        set
        {
            DefaultRotation = value;
            SetRotation(value);
        }
    }

    protected Entity(RLModel model, ModelType modelType, bool applyGravity = true)
    {
        _model = model;
        ModelType = modelType;
        ApplyGravity = applyGravity;
        Name = model.Name;

        // Set default bounding box
        DefaultBoundingBoxMin = new Vector3(-0.5f, 0.0f, -0.5f);
        DefaultBoundingBoxMax = new Vector3(0.5f, 1.0f, 0.5f);

        // Initialize bounding box based on current position
        var currentPosition = Position;
        BoundingBoxMin = currentPosition + DefaultBoundingBoxMin;
        BoundingBoxMax = currentPosition + DefaultBoundingBoxMax;
    }


    /// <summary>
    /// Translate the entity by the specified vector
    /// </summary>
    public Entity Translate(Vector3 translation)
    {
        ModelMatrix = Matrix4x4.Multiply(Matrix4x4.CreateTranslation(translation), ModelMatrix);
        UpdateBoundingBox();
        Log.Verbose("Translated entity by {Translation}", translation);
        return this;
    }

    /// <summary>
    /// Rotate the entity by the specified angle around the given axis
    /// </summary>
    public Entity Rotate(float radians, Vector3 axis)
    {
        var normAxis = Vector3.Normalize(axis);
        var rotation = Matrix4x4.CreateFromAxisAngle(normAxis, radians);
        ModelMatrix = Matrix4x4.Multiply(ModelMatrix, rotation);
        Log.Verbose("Rotated entity by {Radians} radians around {Axis}", radians, axis);
        return this;
    }

    /// <summary>
    /// Set the scale of the entity
    /// </summary>
    public Entity SetScale(Vector3 scale)
    {
        ModelMatrix = Matrix4x4.Multiply(Matrix4x4.CreateScale(scale), ModelMatrix);
        Log.Verbose("Set entity scale to {Scale}", scale);
        return this;
    }

    /// <summary>
    /// Set the position of the entity
    /// </summary>
    public Entity SetPosition(Vector3 position)
    {
        // Create a new model matrix preserving rotation and scale, but with new position
        Matrix4x4 newModel = ModelMatrix;

        // Update only the translation components
        newModel.M41 = position.X;
        newModel.M42 = position.Y;
        newModel.M43 = position.Z;

        ModelMatrix = newModel;
        UpdateBoundingBox();
        Log.Verbose("Set entity position to {Position}", position);
        return this;
    }

    /// <summary>
    /// Set the model matrix directly
    /// </summary>
    public Entity SetModel(Matrix4x4 model)
    {
        ModelMatrix = model;
        UpdateBoundingBox();
        return this;
    }

    /// <summary>
    /// Reset to absolute identity matrix
    /// </summary>
    public Entity AbsoluteReset()
    {
        ModelMatrix = Matrix4x4.Identity;
        _defaultSet = false;
        UpdateBoundingBox();
        Log.Verbose("Absolute reset entity model");
        return this;
    }

    /// <summary>
    /// Save the current model matrix as default
    /// </summary>
    public Entity SetDefault()
    {
        _modelDefault = ModelMatrix;
        _defaultSet = true;
        Log.Verbose("Set default state for entity");
        return this;
    }

    // todo: create docs for this
    public Entity SetDefault(bool savePosition = false, bool saveRotation = false, bool saveScale = false)
    {
        if (savePosition) _positionDefault = Position;
        if (saveRotation) _rotationDefault = Rotation;
        if (saveScale) _scaleDefault = Scale;
        _defaultSet = true;
        Log.Verbose("Set default state for entity {Type}", GetType().Name);
        Log.Verbose("Saved Position: {PosSave}, Rotation: {RotSave}, Scale: {ScaleSave}", savePosition, saveRotation,
            saveScale);
        return this;
    }

    /// <summary>
    /// Reset to the previously saved default state
    /// </summary>
    public Entity Reset(bool silent = true)
    {
        if (!_defaultSet)
        {
            if (!silent)
                Log.Warning("Unable to reset as a lock state has not been created, resetting absolute");
            AbsoluteReset();
        }
        else
        {
            if (_positionDefault != null || _rotationDefault != null || _scaleDefault != null)
            {
                if (_positionDefault != null) Position = _positionDefault;
                if (_rotationDefault != null) Rotation = _rotationDefault;
                if (_scaleDefault != null) Scale = _scaleDefault;
            }
            else
            {
                ModelMatrix = _modelDefault;
            }
            UpdateBoundingBox();
            if (!silent)
                Log.Verbose("Reset entity to default state");
        }
        return this;
    }

    /// <summary>
    /// Update the bounding box based on current position
    /// </summary>
    private void UpdateBoundingBox()
    {
        var currentPosition = Position;
        BoundingBoxMin = currentPosition + DefaultBoundingBoxMin;
        BoundingBoxMax = currentPosition + DefaultBoundingBoxMax;
    }

    /// <summary>
    /// Set the rotation of the entity (Euler angles in radians) while preserving position and scale
    /// </summary>
    public Entity SetRotation(Vector3 rotation)
    {
        // Decompose current matrix
        var currentPosition = Position;
        var currentScale = Scale;

        // Rebuild matrix with new rotation
        var translationMatrix = Matrix4x4.CreateTranslation(currentPosition);
        var rotationMatrix = Matrix4x4.CreateFromYawPitchRoll(rotation.Y, rotation.X, rotation.Z);
        var scaleMatrix = Matrix4x4.CreateScale(currentScale);

        // Combine transformations: Scale * Rotation * Translation
        ModelMatrix = scaleMatrix * rotationMatrix * translationMatrix;

        UpdateBoundingBox();
        Log.Verbose("Set entity rotation to {Rotation}", rotation);
        return this;
    }

    /// <summary>
    /// Set the rotation of the entity using degrees
    /// </summary>
    public Entity SetRotationDegrees(Vector3 rotationDegrees)
    {
        var rotationRadians = new Vector3(
            float.DegreesToRadians(rotationDegrees.X),
            float.DegreesToRadians(rotationDegrees.Y),
            float.DegreesToRadians(rotationDegrees.Z)
        );
        return SetRotation(rotationRadians);
    }

    protected void ApplyHitboxConfig()
    {
        DefaultBoundingBoxMin = HitboxConfig.CalculateMin();
        DefaultBoundingBoxMax = HitboxConfig.CalculateMax();

        Log.Debug("Applied hitbox config for {EntityType}: Min={Min}, Max={Max}",
            GetType().Name, DefaultBoundingBoxMin, DefaultBoundingBoxMax);
    }

    public void SetHitboxConfig(HitboxConfig config)
    {
        HitboxConfig = config;
        ApplyHitboxConfig();
    }

    public Entity EnableImGuizmo(ImGuizmoOperation operation = ImGuizmoOperation.Translate, ImGuizmoMode mode = ImGuizmoMode.World)
    {
        UseImGuizmo = true;
        GuizmoOperation = operation;
        GuizmoMode = mode;
        return this;
    }

    public Entity DisableImGuizmo()
    {
        UseImGuizmo = false;
        return this;
    }

    public void SetReflection(bool enabled, float reflectivity = 0.3f)
    {
        EnableReflection = enabled;
        Reflectivity = Math.Clamp(reflectivity, 0.0f, 1.0f);
    }

    public void SetRefraction(bool enabled, float refractiveIndex = 1.0f)
    {
        EnableRefraction = enabled;
        RefractiveIndex = refractiveIndex;
    }

    /// <summary>
    /// Draws the bounding box edges in red using OpenGL lines with proper camera transformations.
    /// </summary>
    public void DrawBoundingBox(Camera camera)
    {
        var graphics = SceneManager.Instance.GetCurrentScene().Graphics;
        var shaderBundle = ShaderManager.Instance.Get("hitbox");

        if (!IsHitboxShown) return;

        var gl = graphics.OpenGL;

        var currentPosition = Position;

        // Add this diagnostic log:
        if (float.IsNaN(currentPosition.X) || float.IsNaN(currentPosition.Y) || float.IsNaN(currentPosition.Z))
        {
            // Identify which entity is causing this, you might want to add a Name property to Entity or check its type
            Log.Error($"Entity.DrawBoundingBox: currentPosition contains NaN. Position: {currentPosition}. Target type: {GetType().FullName}");
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

        gl.UseProgram(shaderBundle.Program.ProgramHandle);
        if (!graphics.ShutUp)
            Log.Debug("any errors after use program?");
        graphics.CheckGLErrors();

        // Set transformation matrices
        unsafe
        {
            // Model matrix (identity since we're using world coordinates)
            var modelMatrix = Matrix4x4.Identity;
            float* modelPtr = (float*)&modelMatrix;
            int modelLoc = gl.GetUniformLocation(shaderBundle.Program.ProgramHandle, "model");
            if (modelLoc != -1)
                gl.UniformMatrix4(modelLoc, 1, false, modelPtr);
            if (!graphics.ShutUp)
                Log.Debug("any errors after model matrix?");
            graphics.CheckGLErrors();

            // View matrix
            var viewMatrix = camera.View;
            float* viewPtr = (float*)&viewMatrix;
            int viewLoc = gl.GetUniformLocation(shaderBundle.Program.ProgramHandle, "view");
            if (viewLoc != -1)
                gl.UniformMatrix4(viewLoc, 1, false, viewPtr);
            if (!graphics.ShutUp)
                Log.Debug("any errors after view matrix?");
            graphics.CheckGLErrors();

            // Projection matrix
            var projMatrix = camera.Projection;
            float* projPtr = (float*)&projMatrix;
            int projLoc = gl.GetUniformLocation(shaderBundle.Program.ProgramHandle, "projection");
            if (projLoc != -1)
                gl.UniformMatrix4(projLoc, 1, false, projPtr);
            if (!graphics.ShutUp)
                Log.Debug("any errors after proj matrix?");
            graphics.CheckGLErrors();
        }

        // Set the color uniform (red for hitbox)
        int colorLoc = gl.GetUniformLocation(shaderBundle.Program.ProgramHandle, "uColor");
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
                Console.WriteLine("Divider");
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

    /// <summary>
    /// Set rotation around X axis (pitch) in radians
    /// </summary>
    public Entity SetRotationX(float radians)
    {
        var currentRotation = Rotation;
        return SetRotation(new Vector3(radians, currentRotation.Y, currentRotation.Z));
    }

    /// <summary>
    /// Set rotation around Y axis (yaw) in radians
    /// </summary>
    public Entity SetRotationY(float radians)
    {
        var currentRotation = Rotation;
        return SetRotation(new Vector3(currentRotation.X, radians, currentRotation.Z));
    }

    /// <summary>
    /// Set rotation around Z axis (roll) in radians
    /// </summary>
    public Entity SetRotationZ(float radians)
    {
        var currentRotation = Rotation;
        return SetRotation(new Vector3(currentRotation.X, currentRotation.Y, radians));
    }

    /// <summary>
    /// Set scale on X axis only
    /// </summary>
    public Entity SetScaleX(float scaleX)
    {
        var currentScale = Scale;
        return SetScale(new Vector3(scaleX, currentScale.Y, currentScale.Z));
    }

    /// <summary>
    /// Set scale on Y axis only
    /// </summary>
    public Entity SetScaleY(float scaleY)
    {
        var currentScale = Scale;
        return SetScale(new Vector3(currentScale.X, scaleY, currentScale.Z));
    }

    /// <summary>
    /// Set scale on Z axis only
    /// </summary>
    public Entity SetScaleZ(float scaleZ)
    {
        var currentScale = Scale;
        return SetScale(new Vector3(currentScale.X, currentScale.Y, scaleZ));
    }

    /// <summary>
    /// Set uniform scale (all axes the same)
    /// </summary>
    public Entity SetUniformScale(float uniformScale)
    {
        return SetScale(new Vector3(uniformScale, uniformScale, uniformScale));
    }

    /// <summary>
    /// Show the hitbox visualization
    /// </summary>
    public void ShowHitbox() => IsHitboxShown = true;

    /// <summary>
    /// Hide the hitbox visualization
    /// </summary>
    public void HideHitbox() => IsHitboxShown = false;

    /// <summary>
    /// Toggle hitbox visibility
    /// </summary>
    public void ToggleHitbox() => IsHitboxShown = !IsHitboxShown;

    /// <summary>
    /// Checks if a defualt lock is set
    /// </summary>
    public bool IsDefaultSet() => _defaultSet;

    public Vector3 RotationDegrees
    {
        get
        {
            var rotationRadians = Rotation;
            return new Vector3(
                float.RadiansToDegrees(rotationRadians.X),
                float.RadiansToDegrees(rotationRadians.Y),
                float.RadiansToDegrees(rotationRadians.Z)
            );
        }
        set => SetRotationDegrees(value);
    }

    public Vector3 DefaultRotation { get; set; }
}

/// <summary>
/// Collision sides enumeration
/// </summary>
public enum CollisionSide
{
    None,
    Top,
    Bottom,
    Left,
    Right,
    Front,
    Back
}
