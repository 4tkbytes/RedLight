using RedLight.Graphics;
using Serilog;
using Silk.NET.Input;
using System.Numerics;
using BepuUtilities;

namespace RedLight.Entities;

public class Player: Entity
{
    public Camera Camera { get; set; }
    
    /// <summary>
    /// <para>Camera toggle changes between first and third person POV's.</para>
    /// 1 = First Person
    /// 3 = Third Person
    /// Default = Third Person
    /// </summary>
    /// <see cref="PlayerCameraPOV"/>
    public PlayerCameraPOV CameraToggle = PlayerCameraPOV.ThirdPerson;

    public float MoveSpeed { get; set; } = 2.5f;

    private Vector3 lastModelPosition;
    private bool isMoving = false;
    bool IsSprinting = false;

    public Player(Camera camera, Transformable<RLModel> model, HitboxConfig hitboxConfig = null) : base(model.Target)
    {
        Camera = camera;
    
        SetModel(model.ModelMatrix);
        lastModelPosition = Position;
    
        if (hitboxConfig == null)
        {
            HitboxConfig = HitboxConfig.ForPlayer(
                width: -0.5f,
                height: -0.5f,
                length: -0.5f,
                groundOffset: 0.5f
            );
        }
        else
        {
            HitboxConfig = hitboxConfig;
        }

        ApplyHitboxConfig();

    
        Log.Debug("[Player] Created with initial position: {Position}, rotation: {Rotation}, scale: {Scale}",
            Position, Rotation, Scale);
        Log.Debug("[Player] Hitbox: Min={Min}, Max={Max}", DefaultBoundingBoxMin, DefaultBoundingBoxMax);

        SetDefault(saveRotation: true, saveScale: true);
        ApplyGravity = true;
    }

    public Player(Vector2 screenSize, Transformable<RLModel> model) : this(new Camera(screenSize), model) { }

    /// <summary>
    /// Updates player logic
    /// </summary>
    public void Update(float deltaTime, HashSet<Key> pressedKeys, bool isUsingDebugCamera = false, bool silent = true)
    {
        if (pressedKeys == null && isUsingDebugCamera == false)
        {
            throw new ArgumentNullException("PressedKeys should not be null if the class is a player");
        }
        var prevPos = Position;

        // apply movement - removed silent parameter
        if (!isUsingDebugCamera)
            HandleMovement(pressedKeys, deltaTime, silent);

        // IMPORTANT: Get updated position from physics system
        if (PhysicsSystem != null && PhysicsSystem.TryGetBodyHandle(this, out var bodyHandle))
        {
            // Update position from physics engine
            var pose = PhysicsSystem.Simulation.Bodies.GetBodyReference(bodyHandle).Pose;
            SetPosition(new Vector3(pose.Position.X, pose.Position.Y, pose.Position.Z));

            // Get velocity for other effects if needed
            var velocity = PhysicsSystem.Simulation.Bodies.GetBodyReference(bodyHandle).Velocity;
            Velocity = new Vector3(velocity.Linear.X, velocity.Linear.Y, velocity.Linear.Z);

            if (!silent) Log.Debug("[Player] Position updated from physics: {Position}", Position);
        }

        if (prevPos != Position)
            Log.Verbose("[Player] Position changed: {Prev} -> {Current}", prevPos, Position);

        if (Position.Y < -60)
        {
            Log.Warning(
                "Looks like you went too far down. Don't worry, I'll reset it for you. No need to thank me, heh~");
            ResetPhysics();
        }
        
        UpdateCameraPosition();
    }
    
    private void HandleMovement(HashSet<Key> pressedKeys, float deltaTime, bool silent = true)
    {
        Vector3 direction = Vector3.Zero;
        bool shouldJump = false;

        // Calculate direction based on camera orientation
        var forward = Vector3.Normalize(new Vector3(Camera.Front.X, 0, Camera.Front.Z));
        var right = Vector3.Normalize(Vector3.Cross(Camera.Front, Camera.Up));

        if (pressedKeys.Contains(Key.W))
            direction += forward;
        if (pressedKeys.Contains(Key.S))
            direction -= forward;
        if (pressedKeys.Contains(Key.A))
            direction -= right;
        if (pressedKeys.Contains(Key.D))
            direction += right;
        if (pressedKeys.Contains(Key.Space))
            shouldJump = true;
        if (pressedKeys.Contains(Key.ShiftLeft))
            IsSprinting = true;
        if (!pressedKeys.Contains(Key.ShiftLeft))
            IsSprinting = false;

        var horizontalDirection = new Vector3(direction.X, 0, direction.Z);
        isMoving = horizontalDirection.Length() > 0.01f;
        
        float currentActualSpeed = MoveSpeed;
        if (IsSprinting && isMoving)
        {
            currentActualSpeed *= 2f;
        }

        if (isMoving || shouldJump)
        {
            if (PhysicsSystem != null && PhysicsSystem.TryGetBodyHandle(this, out var bodyHandle))
            {
                var bodyRef = PhysicsSystem.Simulation.Bodies.GetBodyReference(bodyHandle);
                var currentVel = bodyRef.Velocity.Linear;

                // Handle horizontal movement
                if (isMoving)
                {
                    if (!silent) Log.Debug("[Player] Movement direction detected: {Direction}", direction);
                    direction = Vector3.Normalize(direction);

                    // Use velocity-based movement for smoother control
                    var targetVelocity = new Vector3(
                        direction.X * currentActualSpeed,
                        currentVel.Y, // Preserve Y velocity for gravity
                        direction.Z * currentActualSpeed
                    );

                    // Apply velocity change for horizontal movement
                    var velocityChange = new Vector3(
                        (targetVelocity.X - currentVel.X) * 0.3f,
                        0, // Don't modify Y velocity here
                        (targetVelocity.Z - currentVel.Z) * 0.3f
                    );

                    bodyRef.Velocity.Linear = new Vector3(
                        currentVel.X + velocityChange.X,
                        currentVel.Y, // Keep Y velocity unchanged
                        currentVel.Z + velocityChange.Z
                    );

                    if (!silent) Log.Debug("[Player] Applied horizontal movement: {VelocityChange}", velocityChange);
                }

                // Handle jumping
                if (shouldJump && IsGrounded())
                {
                    float jumpForce = 8.0f; // Adjust this value to control jump height
                    bodyRef.Velocity.Linear = new Vector3(
                        currentVel.X,
                        jumpForce, // Set upward velocity for jump
                        currentVel.Z
                    );

                    if (!silent) Log.Debug("[Player] Jump applied with force: {JumpForce}", jumpForce);
                }

                if (!bodyRef.Awake)
                {
                    bodyRef.Awake = true;
                }
            }
            else
            {
                // Fallback movement (without physics)
                if (direction != Vector3.Zero)
                {
                    SetPosition(Position + direction * currentActualSpeed * deltaTime);
                }
            }
        }
        else
        {
            // Apply horizontal damping when not moving to reduce sliding
            if (PhysicsSystem != null && PhysicsSystem.TryGetBodyHandle(this, out var bodyHandle))
            {
                var bodyRef = PhysicsSystem.Simulation.Bodies.GetBodyReference(bodyHandle);
                var currentVel = bodyRef.Velocity.Linear;
                
                var horizontalVel = new Vector3(currentVel.X, 0, currentVel.Z);
                if (horizontalVel.Length() > 0.1f)
                {
                    var dampingFactor = 0.1f;
                    var newHorizontalVel = horizontalVel * dampingFactor;
                    
                    bodyRef.Velocity.Linear = new Vector3(
                        newHorizontalVel.X,
                        currentVel.Y, // Preserve Y velocity
                        newHorizontalVel.Z
                    );

                    if (!silent) Log.Debug("[Player] Applied sliding damping, new velocity: {NewVelocity}", bodyRef.Velocity.Linear);
                }
            }
            
            isMoving = false;
        }
    }
    
    private bool IsGrounded()
    {
        if (PhysicsSystem == null || !PhysicsSystem.TryGetBodyHandle(this, out var bodyHandle))
            return false;

        var bodyRef = PhysicsSystem.Simulation.Bodies.GetBodyReference(bodyHandle);
        var currentVel = bodyRef.Velocity.Linear;
    
        // Simple ground check: if Y velocity is very small and player is not falling fast
        // You might want to implement a more sophisticated ground detection using raycasting
        bool isGrounded = Math.Abs(currentVel.Y) < 0.5f && Position.Y <= 1.0f; // Adjust threshold as needed
    
        return isGrounded;
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

            // Sync our representation and reset rotation
            SetPosition(new Vector3(0, 1, 0));
            Velocity = Vector3.Zero;
            // targetYawRotation = 0f; // Reset target rotation
            
            var resetMatrix = Matrix4x4.CreateScale(Scale) * Matrix4x4.CreateTranslation(Position) * Matrix4x4.CreateRotationX(Rotation.X) * Matrix4x4.CreateRotationZ(Rotation.Z);
            SetModel(resetMatrix);

            // Reset();
            Log.Information("[Player] Physics state and rotation reset");
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
            
            // Only update rotation when player is actually moving
            if (Position != lastModelPosition && isMoving)
            {
                // Normalize the camera yaw to be between -180 and 180 degrees
                float normalizedYaw = NormalizeAngle(Camera.Yaw);
                
                // Convert to radians for the Y rotation (yaw)
                float targetYRotation = -float.DegreesToRadians(normalizedYaw);
                
                // Keep X and Z rotations stable - don't let them flip
                float currentXRotation = Rotation.X;
                float currentZRotation = Rotation.Z;
                
                // Normalize current rotations to prevent accumulation
                currentXRotation = NormalizeRadians(currentXRotation);
                currentZRotation = NormalizeRadians(currentZRotation);
                
                SetRotation(new Vector3(_rotationDefault.X, targetYRotation, _rotationDefault.Z));
                
                lastModelPosition = Position;
            }
        
            if (prevPos != Position)
                Log.Verbose("[Player] Player's position changed: {Prev} -> {Current}", prevPos, Position);

            Log.Verbose("[Player] Camera set to third person at {Position}", Camera.Position);
        }
        Camera.UpdateCamera();
    }

    /// <summary>
    /// Normalizes an angle in degrees to be between -180 and 180 degrees
    /// </summary>
    /// <param name="angle">Angle in degrees</param>
    /// <returns>Normalized angle between -180 and 180 degrees</returns>
    private float NormalizeAngle(float angle)
    {
        // Normalize to 0-360 range first
        angle = angle % 360f;

        // Convert to -180 to 180 range
        if (angle > 180f)
            angle -= 360f;
        else if (angle < -180f)
            angle += 360f;
        
        return angle;
    }

    /// <summary>
    /// Normalizes an angle in radians to be between -π and π
    /// </summary>
    /// <param name="angle">Angle in radians</param>
    /// <returns>Normalized angle between -π and π radians</returns>
    private float NormalizeRadians(float angle)
    {
        // Normalize to 0-2π range first
        angle = angle % (2f * MathF.PI);
        
        // Convert to -π to π range
        if (angle > MathF.PI)
            angle -= 2f * MathF.PI;
        else if (angle < -MathF.PI)
            angle += 2f * MathF.PI;
            
        return angle;
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
    /// type casted into the PlayerCameraPOV. It is recommended to use the <see cref="SetPOV(PlayerCameraPOV)"/>
    /// function to avoid any exceptions. 
    /// </summary>
    /// <param name="cameraPOV"><see cref="int"/> 1 for first person or 3 for third person </param>
    /// <exception cref="Exception"></exception>
    public void SetPOV(int cameraPOV)
    {
        if (cameraPOV != 1 && cameraPOV != 3)
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