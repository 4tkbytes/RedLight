using RedLight.Graphics;
using Serilog;
using Silk.NET.Input;
using System.Numerics;

namespace RedLight.Entities;

public class Player: Entity<Transformable<RLModel>>
{
    public Camera Camera { get; set; }
    
    /// <summary>
    /// Camera toggle changes between first and third person POV's. 
    /// 1 = First Person
    /// 3 = Third Person
    /// Default = Third Person
    /// </summary>
    /// <see cref="PlayerCameraPOV"/>
    public PlayerCameraPOV CameraToggle = PlayerCameraPOV.ThirdPerson;

    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }
    public Vector3 Scale { get; set; }

    public float MoveSpeed { get; set; } = 2.5f;

    private Vector3 lastModelPosition;

    public Player(Camera camera, Transformable<RLModel> model, bool autoMapHitbox = true) : base(model)
    {
        Camera = camera;
        Target = model;
        Position = model.Position;
        Rotation = model.Rotation;
        Scale = model.Scale;
        lastModelPosition = Position;
        if (autoMapHitbox)
            AutoMapHitboxToModel();
        Log.Debug("[Player] Created with initial position: {Position}, rotation: {Rotation}, scale: {Scale}", Position, Rotation, Scale);
        // apply sum correction for the maxwell model, will have more models to be added. 
        ApplyHitboxCorrection();

        ApplyGravity = true;
    }

    public Player(Vector2 screenSize, Transformable<RLModel> model) : this(new Camera(screenSize), model) { }

    /// <summary>
    /// Updates player logic
    /// </summary>
    public override void Update(float deltaTime, HashSet<Key> pressedKeys, bool isUsingDebugCamera = false, bool silent = true)
    {
        if (pressedKeys == null && isUsingDebugCamera == false)
        {
            throw new ArgumentNullException("PressedKeys should not be null if the class is a player");
        }
        var prevPos = Position;

        // apply movement
        if (!isUsingDebugCamera)
            HandleMovement(pressedKeys, deltaTime);

        // IMPORTANT: Get updated position from physics system
        if (PhysicsSystem != null && PhysicsSystem.TryGetBodyHandle(this, out var bodyHandle))
        {
            // Update position from physics engine
            var pose = PhysicsSystem.Simulation.Bodies.GetBodyReference(bodyHandle).Pose;
            Position = new Vector3(pose.Position.X, pose.Position.Y, pose.Position.Z);

            // Get velocity for other effects if needed
            var velocity = PhysicsSystem.Simulation.Bodies.GetBodyReference(bodyHandle).Velocity;
            Velocity = new Vector3(velocity.Linear.X, velocity.Linear.Y, velocity.Linear.Z);

            if (!silent) Log.Debug("[Player] Position updated from physics: {Position}", Position);
        }

        if (prevPos != Position)
            Log.Verbose("[Player] Position changed: {Prev} -> {Current}", prevPos, Position);

        UpdateCameraPosition();
        UpdateBoundingBox();
        SyncModelTransform();
    }

    private void HandleMovement(HashSet<Key> pressedKeys, float deltaTime)
    {
        Vector3 direction = Vector3.Zero;

        // Calculate direction based on camera orientation
        var forward = Vector3.Normalize(new Vector3(Camera.Front.X, 0, Camera.Front.Z));
        var right = Vector3.Normalize(Vector3.Cross(Camera.Front, Camera.Up));
        var up = Camera.Up;

        if (pressedKeys.Contains(Key.W))
            direction += forward;
        if (pressedKeys.Contains(Key.S))
            direction -= forward;
        if (pressedKeys.Contains(Key.A))
            direction -= right;
        if (pressedKeys.Contains(Key.D))
            direction += right;
        if (pressedKeys.Contains(Key.Space))
            direction += up;
        if (pressedKeys.Contains(Key.ShiftLeft))
            direction -= up;

        if (direction != Vector3.Zero)
        {
            direction = Vector3.Normalize(direction);

            if (PhysicsSystem != null && PhysicsSystem.TryGetBodyHandle(this, out var bodyHandle))
            {
                // Check current velocity before applying impulse
                var bodyRef = PhysicsSystem.Simulation.Bodies.GetBodyReference(bodyHandle);
                var currentVel = bodyRef.Velocity.Linear;

                // Use a lower force multiplier with deltaTime scaling
                float forceMultiplier = 5.0f * deltaTime;
                var impulse = new Vector3(direction.X, direction.Y, direction.Z) * MoveSpeed * forceMultiplier;

                // Apply force rather than directly setting velocity
                PhysicsSystem.ApplyImpulse(this, impulse);

                // Check if body needs to be awakened
                if (!bodyRef.Awake)
                {
                    Log.Debug("[Player] Waking up physics body");
                    bodyRef.Awake = true;
                }

                Log.Debug("[Player] Applied impulse {Impulse}, Current velocity: {Velocity}, Position: {Position}",
                    impulse, currentVel, bodyRef.Pose.Position);
            }
            else
            {
                // Fallback if physics not initialized
                Position += direction * MoveSpeed * deltaTime;
            }
        }
    }

    private void ApplyHitboxCorrection()
    {
        if (Target?.Target?.Name == "maxwell")
        {
            // Values determined by examining the maxwell model's actual dimensions
            float heightCorrection = -0.5f; // Lower the hitbox to ground level
            Vector3 centerCorrection = new Vector3(0, heightCorrection, 0);

            // Apply corrections to default bounding box
            Vector3 size = DefaultBoundingBoxMax - DefaultBoundingBoxMin;
            Vector3 center = (DefaultBoundingBoxMin + DefaultBoundingBoxMax) * 0.5f;

            // Center horizontally, adjust height
            center += centerCorrection;

            // Reduce height by 50% and adjust other dimensions as needed
            size.Y *= 0.5f;

            // Recalculate bounds from center and size
            DefaultBoundingBoxMin = center - size * 0.5f;
            DefaultBoundingBoxMax = center + size * 0.5f;

            Log.Debug("[Player] Applied hitbox correction for maxwell model: Min={Min}, Max={Max}",
                DefaultBoundingBoxMin, DefaultBoundingBoxMax);

            // Update physics if already initialized
            if (PhysicsSystem != null && PhysicsSystem.TryGetBodyHandle(this, out var _))
            {
                PhysicsSystem.RemoveEntity(this);
                PhysicsSystem.AddEntity(this);
            }

            // Update current bounding box with new defaults
            UpdateBoundingBox();
        }
    }

    /// <summary>
    /// Resets the player's physics state for debugging
    /// </summary>
    public void ResetPhysics()
    {
        if (PhysicsSystem != null && PhysicsSystem.TryGetBodyHandle(this, out var bodyHandle))
        {
            var bodyRef = PhysicsSystem.Simulation.Bodies.GetBodyReference(bodyHandle);

            // Reset velocity
            bodyRef.Velocity.Linear = new Vector3(0, 0, 0);
            bodyRef.Velocity.Angular = new Vector3(0, 0, 0);

            // Reset position to a known good value (slightly above ground)
            bodyRef.Pose.Position = new Vector3(0, 1, 0);

            // Ensure body is awake
            bodyRef.Awake = true;

            // Sync our representation
            Position = new Vector3(0, 1, 0);
            Velocity = Vector3.Zero;

            Log.Information("[Player] Physics state reset");
        }
    }

    /// <summary>
    /// Toggle between first and third person camera.
    /// </summary>
    public void ToggleCamera()
    {
        var prevPOV = CameraToggle;
        if (CameraToggle == PlayerCameraPOV.FirstPerson)
        {
            CameraToggle = PlayerCameraPOV.ThirdPerson;
        }
        else
        {
            CameraToggle = PlayerCameraPOV.FirstPerson;
        }
        Log.Debug("[Player] Camera POV toggled: {Prev} -> {Current}", prevPOV, CameraToggle);
        UpdateCameraPosition();
    }

    /// <summary>
    /// Updates the camera's position based on the current perspective.
    /// </summary>
    private void UpdateCameraPosition()
    {
        if (CameraToggle == PlayerCameraPOV.FirstPerson)
        {
            // first person
            Camera.Position = Position;
            Log.Verbose("[Player] Camera set to first person at {Position}", Position);
        }
        else
        {
            // third person
            float thirdPersonDistance = 5.0f;
            Vector3 cameraOffset = new Vector3(0, 2, 0);
            Camera.Position = Position - Camera.Front * thirdPersonDistance + cameraOffset;

            var prevPos = Position;
            if (Position != lastModelPosition)
            {
                Rotation = new Vector3(Rotation.X, -float.DegreesToRadians(Camera.Yaw), Rotation.Z);
                lastModelPosition = Position;
            }

            if (prevPos != Position)
                Log.Verbose("[Player] Player's position changed: {Prev} -> {Current}", prevPos, Position);

            SyncModelTransform();

            Log.Verbose("[Player] Camera set to third person at {Position}", Camera.Position);
            Log.Verbose("[Player] Player's rotation set to {Rotation}", Rotation);
        }
        Camera.UpdateCamera();
    }

    /// <summary>
    /// Syncs the model's transform with the player's position, rotation, and scale.
    /// </summary>
    private void SyncModelTransform()
    {
        var scaleMatrix = Matrix4x4.CreateScale(Scale.X, Scale.Y, Scale.Z);
        var rotationMatrix = Matrix4x4.CreateRotationX(Rotation.X) 
            * Matrix4x4.CreateRotationY(Rotation.Y) 
            * Matrix4x4.CreateRotationZ(Rotation.Z);
        var translationMatrix = Matrix4x4.CreateTranslation(Position.X, Position.Y, Position.Z);
        var modelMatrix = scaleMatrix * rotationMatrix * translationMatrix;
        Target.SetModel(modelMatrix);
        Log.Verbose("[Player] Model transform updated. Position: {Position}, Rotation: {Rotation}, Scale: {Scale}", Position, Rotation, Scale);
    }

    /// <summary>
    /// Example free roam method (can be expanded).
    /// </summary>
    public void FreeRoam()
    {
        Log.Debug("[Player] FreeRoam called.");
        // Implement free roam logic if needed
    }

    /// <summary>
    /// Sets the point of view of the players camera. 
    /// </summary>
    /// <param name="cameraPOV"><see cref="PlayerCameraPOV"/></param>
    public void SetPOV(PlayerCameraPOV cameraPOV)
    {
        CameraToggle = cameraPOV;
    }

    /// <summary>
    /// Sets the point of view of the players camera. This function overload uses an int, which is then
    /// type casted into the PlayerCameraPOV. It is recommended to use the <see cref="SetPOV(RedLight.Graphics.Primitive.PlayerCameraPOV)"/>
    /// function to avoid any exceptions. 
    /// </summary>
    /// <param name="cameraPOV"><see cref="int"/> 1 for first person or 3 for third person </param>
    /// <exception cref="Exception"></exception>
    public void SetPOV(int cameraPOV)
    {
        if (cameraPOV != 1 || cameraPOV != 3)
            throw new Exception("Camera POV value is not valid (1|3)");
        CameraToggle = (PlayerCameraPOV)cameraPOV;
    }
}

/// <summary>
/// Enum changing between the different player camera point of views. 
/// </summary>
public enum PlayerCameraPOV
{
    FirstPerson = 1,
    ThirdPerson = 3,
}