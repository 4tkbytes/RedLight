using Serilog;
using Serilog.Core;
using Silk.NET.Maths;

namespace RedLight.Graphics;

public class Camera
{
    public Matrix4X4<float> View { get; set; } = Matrix4X4<float>.Identity;
    public Matrix4X4<float> Projection { get; private set; } = Matrix4X4<float>.Identity;
    public Vector3D<float> Position { get; private set; }
    public Vector3D<float> Front { get; private set; }
    public Vector3D<float> Up { get; private set; }
    public float Yaw { get; set; }
    public float Pitch { get; set; }


    public float Speed { get; private set; } = 0.05f;
    public float Sensitivity { get; private set; } = 0.1f;


    private Vector3D<float> cameraTarget;

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

    public Camera UpdateCamera()
    {
        cameraTarget = Position + Vector3D.Normalize(Front);
        LookAt(Position, cameraTarget, Up);
        
        Log.Verbose("Updated camera target");
        return this;
    }

    public Camera LookAt(Vector3D<float> position, Vector3D<float> cameraTarget, Vector3D<float> cameraUpVector)
    {
        View = Matrix4X4.CreateLookAt(
            position,
            cameraTarget,
            cameraUpVector
        );
        
        Log.Verbose("Looking at new view");
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