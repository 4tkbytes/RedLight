using Silk.NET.Maths;

namespace RedLight.Graphics;

public class Camera
{
    public Matrix4X4<float> View { get; set; } = Matrix4X4<float>.Identity;
    public Matrix4X4<float> Projection { get; private set; } = Matrix4X4<float>.Identity;
    public Vector3D<float> Position { get; private set; }
    public Vector3D<float> Front { get; private set; }
    public Vector3D<float> Up { get; private set; }

    public float Speed { get; private set; } = 0.05f;

    private Vector3D<float> cameraTarget;

    public Camera(Vector3D<float> cameraPosition, Vector3D<float> cameraFront, Vector3D<float> cameraUp,
        float fov, float aspect, float near, float far)
    {
        Position = cameraPosition;
        Front = cameraFront; // <-- Fix: assign cameraFront to Front
        Projection = Matrix4X4.Add(Projection, Matrix4X4.CreatePerspectiveFieldOfView(fov, aspect, near, far));
        
        cameraTarget = Position + cameraFront;
        Up = cameraUp;
        
        View = Matrix4X4.CreateLookAt(
            Position,
            cameraTarget,
            Up
        );
    }

    public Camera UpdateCamera()
    {
        cameraTarget = Position + Vector3D.Normalize(Front); // Always look in the direction of Front
        LookAt(Position, cameraTarget, Up);
        
        return this;
    }

    public Camera LookAt(Vector3D<float> position, Vector3D<float> cameraTarget, Vector3D<float> cameraUpVector)
    {
        View = Matrix4X4.CreateLookAt(
            position,
            cameraTarget,
            cameraUpVector
        );
        
        return this;
    }

    public Camera SetSpeed(float speed)
    {
        Speed = speed;
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

    public Camera SetFront(Vector3D<float> front)
    {
        Front = Vector3D.Normalize(front);
        return this;
    }

    public Camera MoveForward(float speed)
    {
        Position += speed * Vector3D.Normalize(Front);
        return this;
    }
    
    public Camera MoveBack(float speed)
    {
        Position -= speed * Vector3D.Normalize(Front);
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
        Position = pos;
        return this;
    }
}