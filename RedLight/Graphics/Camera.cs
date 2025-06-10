using System.Numerics;
using RedLight.Graphics.Primitive;
using RedLight.Physics;
using Serilog;
using Serilog.Core;
using Silk.NET.Input;
using Silk.NET.Maths;

namespace RedLight.Graphics;

public class Camera
{
    // changable variables
    public Matrix4X4<float> View { get; set; } = Matrix4X4<float>.Identity;
    public Matrix4X4<float> Projection { get; private set; } = Matrix4X4<float>.Identity;
    public Vector3D<float> Position { get; set; }
    public Vector3D<float> Front { get; private set; }
    public Vector3D<float> Up { get; private set; }
    public float Yaw { get; set; }
    public float Pitch { get; set; }

    // constants
    public float Speed { get; private set; } = 0.05f;
    public float Sensitivity { get; private set; } = 0.1f;

    // internal variables
    private Vector3D<float> cameraTarget;
    private float lastX = 0;
    private float lastY = 0;
    private bool firstMouse = true;

    public Camera(Vector3D<float> cameraPosition, Vector3D<float> cameraFront, Vector3D<float> cameraUp,
        float fov, float aspect, float near, float far)
    {
        Position = cameraPosition;
        Front = Vector3D.Normalize(cameraFront);
        Up = Vector3D.Normalize(cameraUp);
        Projection = Matrix4X4.Add(Projection, Matrix4X4.CreatePerspectiveFieldOfView(fov, aspect, near, far));

        cameraTarget = Position + Front;
        View = Matrix4X4.CreateLookAt(
            Position,
            cameraTarget,
            Up
        );
        Log.Verbose("Created new Camera class");
    }

    public Camera(Vector2D<int> screenSize)
        : this(new Vector3D<float>(0, 0, 3),
            new Vector3D<float>(0, 0, -1),
            new Vector3D<float>(0, 1, 0),
            float.DegreesToRadians(90.0f),
            (float)screenSize.X / screenSize.Y,
            0.1f,
            100.0f)
    {
    }

    public Camera UpdateCamera(bool shutup = true)
    {
        cameraTarget = Position + Vector3D.Normalize(Front);
        LookAt(Position, cameraTarget, Up);
        
        if (!shutup)
            Log.Verbose("Updated camera target");
        return this;
    }

    public Camera LookAt(Vector3D<float> position, Vector3D<float> cameraTarget, Vector3D<float> cameraUpVector, bool shutup = true)
    {
        View = Matrix4X4.CreateLookAt(
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

        Vector3D<float> direction = new();
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
    
    public Camera KeyMap(HashSet<Key> PressedKeys, Player player)
    {
        if (PressedKeys.Contains(Key.W) && !player.ObjectCollisionSides.Contains(CollisionSide.Front))
            MoveForward();
        if (PressedKeys.Contains(Key.S) && !player.ObjectCollisionSides.Contains(CollisionSide.Back))
            MoveBack();
        if (PressedKeys.Contains(Key.A) && !player.ObjectCollisionSides.Contains(CollisionSide.Left))
            MoveLeft();
        if (PressedKeys.Contains(Key.D) && !player.ObjectCollisionSides.Contains(CollisionSide.Right))
            MoveRight();
        if (PressedKeys.Contains(Key.ShiftLeft) && !player.ObjectCollisionSides.Contains(CollisionSide.Down))
            MoveDown();
        if (PressedKeys.Contains(Key.Space) && !player.ObjectCollisionSides.Contains(CollisionSide.Up))
            MoveUp();
        UpdateCamera();

        return this;
    }

    public Camera SetSpeed(float speed)
    {
        Speed = speed;
        return this;
    }

    public Camera SetFront(Vector3D<float> direction)
    {
        Front = Vector3D.Normalize<float>(direction);
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
        Position -= Vector3D.Normalize(Vector3D.Cross(Front, Up)) * Speed;
        return this;
    }

    public Camera MoveRight()
    {
        Position += Vector3D.Normalize(Vector3D.Cross(Front, Up)) * Speed;
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
        var right = Vector3D.Normalize(Vector3D.Cross(Front, Up));
        Position -= right * speed;
        return this;
    }

    public Camera MoveRight(float speed)
    {
        var right = Vector3D.Normalize(Vector3D.Cross(Front, Up));
        Position += right * speed;
        return this;
    }

    public Camera SetPosition(Vector3D<float> pos)
    {
        Log.Verbose("Position set [{A}] -> [{B}]", Position, pos);
        Position = pos;
        return this;
    }
}