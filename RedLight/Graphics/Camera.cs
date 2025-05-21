using System.Numerics;
using RedLight.Utils;

namespace RedLight.Graphics
{
    public class Camera
    {
        public Vector3 Position { get; set; } = new Vector3(0, 0, 3);
        public Vector3 Target { get; set; } = Vector3.Zero;
        public Vector3 Up { get; set; } = Vector3.UnitY;
        
        private float _fov = 45.0f;
        private float _aspectRatio = 16.0f / 9.0f;
        private float _nearPlane = 0.1f;
        private float _farPlane = 100.0f;

        public float FieldOfView 
        { 
            get => _fov; 
            set => _fov = value; 
        }

        public void SetAspectRatio(float width, float height)
        {
            _aspectRatio = width / height;
        }
        
        public Matrix4x4 GetViewMatrix()
        {
            return Matrix4x4.CreateLookAt(Position, Target, Up);
        }
        
        public Matrix4x4 GetProjectionMatrix()
        {
            return Matrix4x4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(_fov),
                _aspectRatio,
                _nearPlane,
                _farPlane
            );
        }
    }
}