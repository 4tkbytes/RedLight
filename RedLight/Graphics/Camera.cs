using Silk.NET.Maths;

namespace RedLight.Graphics;

public class Camera
{
    public Matrix4X4<float> View { get; set; } = Matrix4X4<float>.Identity;
    public Matrix4X4<float> Projection { get; private set; } = Matrix4X4<float>.Identity;
    public Vector3D<float> Position { get; private set; }
    
    public Camera(float fov, float aspect, float near, float far)
    {
        Vector3D<float> cameraPos = new(0, 0, 3);
        Position = cameraPos;
        Vector3D<float> cameraTarget = new(0, 0, 0);
        Vector3D<float> cameraDirection = Vector3D.Normalize(cameraTarget - cameraPos);
        
        Projection = Matrix4X4.Add(Projection, Matrix4X4.CreatePerspectiveFieldOfView(fov, aspect, near, far));

        Vector3D<float> up = new(0, 1, 0);
        Vector3D<float> cameraRight = Vector3D.Normalize(Vector3D.Cross(up, cameraDirection));
        
        Vector3D<float> cameraUp = Vector3D.Cross(cameraDirection, cameraRight);
        
        View = Matrix4X4.CreateLookAt(
            cameraPos,
            cameraTarget,
            cameraUp
        );
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
}