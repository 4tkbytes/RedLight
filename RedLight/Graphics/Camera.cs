using System.Numerics;
using RedLight.Entities;
using Serilog;
using Silk.NET.Input;
using Silk.NET.Maths;

namespace RedLight.Graphics;

public class Camera
{
    // changable variables
    public Matrix4x4 View { get; set; } = Matrix4x4.Identity;
    public Matrix4x4 Projection { get; private set; } = Matrix4x4.Identity;
    public Vector3 Position { get; set; }
    public Vector3 Front { get; private set; }
    public Vector3 Up { get; private set; }
    public float Yaw { get; set; }
    public float Pitch { get; set; }

    // constants
    public float Speed { get; private set; } = 2.5f;
    public float Sensitivity { get; private set; } = 0.1f;

    // internal variables
    private Vector3 cameraTarget;
    private float lastX = 0;
    private float lastY = 0;
    private bool firstMouse = true;
    
    public float FOV { get; private set; }
    public float Near { get; private set; }
    public float Far { get; private set; }

    public Camera(Vector3 cameraPosition, Vector3 cameraFront, Vector3 cameraUp,
        float fov, float aspect, float near, float far)
    {
        Position = cameraPosition;
        Front = Vector3.Normalize(cameraFront);
        Up = Vector3.Normalize(cameraUp);
        Projection = Matrix4x4.Add(Projection, Matrix4x4.CreatePerspectiveFieldOfView(fov, aspect, near, far));
        
        FOV = fov;
        Near = near;
        Far = far;

        cameraTarget = Position + Front;
        View = Matrix4x4.CreateLookAt(
            Position,
            cameraTarget,
            Up
        );
        Log.Verbose("Created new Camera class");
    }

    public Camera(Vector2 screenSize)
        : this(new Vector3(0, 0, 3),
            new Vector3(0, 0, -1),
            new Vector3(0, 1, 0),
            float.DegreesToRadians(90.0f),
            (float)screenSize.X / screenSize.Y,
            0.1f,
            100.0f)
    {
    }

    public Camera UpdateCamera(bool shutup = true)
    {
        cameraTarget = Position + Vector3.Normalize(Front);
        LookAt(Position, cameraTarget, Up);
        
        if (!shutup)
            Log.Verbose("Updated camera target");
        return this;
    }

    public Camera LookAt(Vector3 position, Vector3 cameraTarget, Vector3 cameraUpVector, bool shutup = true)
    {
        View = Matrix4x4.CreateLookAt(
            position,
            cameraTarget,
            cameraUpVector
        );

        if (!shutup)
            Log.Verbose("Looking at new view");
        return this;
    }

    public void FreeMove(Vector2 mousePosition)
    {
        var camera = this;

        float xpos = mousePosition.X;
        float ypos = mousePosition.Y;

        if (firstMouse)
        {
            lastX = xpos;
            lastY = ypos;
            firstMouse = false;
        }

        float xoffset = xpos - lastX;
        float yoffset = lastY - ypos;
        lastX = xpos;
        lastY = ypos;

        xoffset *= camera.Sensitivity;
        yoffset *= camera.Sensitivity;
        camera.Yaw += xoffset;    // yaw
        camera.Pitch += yoffset;  // pitch

        if (camera.Pitch > 89.0f)
            camera.Pitch = 89.0f;
        if (camera.Pitch < -89.0f)
            camera.Pitch = -89.0f;

        Vector3 direction = new();
        direction.X = float.Cos(float.DegreesToRadians(camera.Yaw)) * float.Cos(float.DegreesToRadians(camera.Pitch));
        direction.Y = float.Sin(float.DegreesToRadians(camera.Pitch));
        direction.Z = float.Sin(float.DegreesToRadians(camera.Yaw)) * float.Cos(float.DegreesToRadians(camera.Pitch));
        camera = camera.SetFront(direction);
    }

    public Camera KeyMap(HashSet<Key> PressedKeys)
    {
        if (PressedKeys.Contains(Key.W))
            MoveForward();
        if (PressedKeys.Contains(Key.S))
            MoveBack();
        if (PressedKeys.Contains(Key.A))
            MoveLeft();
        if (PressedKeys.Contains(Key.D))
            MoveRight();
        if (PressedKeys.Contains(Key.ShiftLeft))
            MoveDown();
        if (PressedKeys.Contains(Key.Space))
            MoveUp();
        
        UpdateCamera();

        return this;
    }
    
    public Camera KeyMap(HashSet<Key> PressedKeys, float deltaTime)
    {
        float adjustedSpeed = Speed * deltaTime;

        if (PressedKeys.Contains(Key.W))
            Position += adjustedSpeed * Front;
        if (PressedKeys.Contains(Key.S))
            Position -= adjustedSpeed * Front;
        if (PressedKeys.Contains(Key.A))
            Position -= Vector3.Normalize(Vector3.Cross(Front, Up)) * adjustedSpeed;
        if (PressedKeys.Contains(Key.D))
            Position += Vector3.Normalize(Vector3.Cross(Front, Up)) * adjustedSpeed;
        if (PressedKeys.Contains(Key.ShiftLeft))
            Position -= Up * adjustedSpeed;
        if (PressedKeys.Contains(Key.Space))
            Position += Up * adjustedSpeed;
        
        UpdateCamera();

        return this;
    }

    public Camera KeyMap(HashSet<Key> PressedKeys, Player player)
    {
        // Get movement direction based on camera orientation
        Vector3 direction = Vector3.Zero;

        if (PressedKeys.Contains(Key.W))
            direction += Front;
        if (PressedKeys.Contains(Key.S))
            direction -= Front;
        if (PressedKeys.Contains(Key.A))
            direction -= Vector3.Normalize(Vector3.Cross(Front, Up));
        if (PressedKeys.Contains(Key.D))
            direction += Vector3.Normalize(Vector3.Cross(Front, Up));
        if (PressedKeys.Contains(Key.Space))
            direction += Up;
        if (PressedKeys.Contains(Key.ShiftLeft))
            direction -= Up;

        // Normalize direction if it's not zero
        if (direction != Vector3.Zero)
        {
            direction = Vector3.Normalize(direction);

            // If physics system is available, apply movement through physics
            if (player.PhysicsSystem != null)
            {
                // Convert to System.Numerics.Vector3 for BepuPhysics
                var impulse = new System.Numerics.Vector3(
                    direction.X * player.MoveSpeed,
                    direction.Y * player.MoveSpeed,
                    direction.Z * player.MoveSpeed);

                // Apply movement through physics
                player.PhysicsSystem.ApplyImpulse(player, impulse);

                // Camera position will be updated by Player.UpdateCameraPosition()
            }
            else
            {
                // Direct movement as fallback
                Position += direction * Speed;
            }
        }

        UpdateCamera();
        return this;
    }
    
    public Camera UpdateAspectRatio(float aspectRatio)
    {
        Projection = Matrix4x4.CreatePerspectiveFieldOfView(FOV, aspectRatio, Near, Far);
        Log.Verbose("Updated camera aspect ratio to: {AspectRatio}", aspectRatio);
        return this;
    }

    public Camera SetSpeed(float speed)
    {
        Speed = speed;
        return this;
    }

    public Camera SetFront(Vector3 direction)
    {
        Front = Vector3.Normalize(direction);
        return this;
    }

    public Camera MoveForward()
    {
        Position += Speed * Front;
        return this;
    }

    public Camera MoveBack()
    {
        Position -= Speed * Front;
        return this;
    }

    public Camera MoveUp()
    {
        Position += Up * Speed;
        return this;
    }

    public Camera MoveDown()
    {
        Position -= Up * Speed;
        return this;
    }

    public Camera MoveLeft()
    {
        Position -= Vector3.Normalize(Vector3.Cross(Front, Up)) * Speed;
        return this;
    }

    public Camera MoveRight()
    {
        Position += Vector3.Normalize(Vector3.Cross(Front, Up)) * Speed;
        return this;
    }

    public Camera MoveForward(float speed)
    {
        Position += speed * Front;
        return this;
    }

    public Camera MoveBack(float speed)
    {
        Position -= speed * Front;
        return this;
    }

    public Camera MoveUp(float speed)
    {
        Position += Up * speed;
        return this;
    }

    public Camera MoveDown(float speed)
    {
        Position -= Up * speed;
        return this;
    }

    public Camera MoveLeft(float speed)
    {
        var right = Vector3.Normalize(Vector3.Cross(Front, Up));
        Position -= right * speed;
        return this;
    }

    public Camera MoveRight(float speed)
    {
        var right = Vector3.Normalize(Vector3.Cross(Front, Up));
        Position += right * speed;
        return this;
    }

    public Camera SetPosition(Vector3 pos)
    {
        Log.Verbose("Position set [{A}] -> [{B}]", Position, pos);
        Position = pos;
        return this;
    }
}