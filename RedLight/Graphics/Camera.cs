using System.Numerics;
using RedLight.Utils;

namespace RedLight.Graphics
{
    public class Camera
    {
        // Camera position and orientation
        public Vector3 Position { get; set; } = new Vector3(0, 0, 3);
        public Vector3 Front { get; private set; } = -Vector3.UnitZ; // Forward direction
        public Vector3 Up { get; private set; } = Vector3.UnitY;     // Up direction
        public Vector3 Right { get; private set; } = Vector3.UnitX;  // Right direction
        
        // Camera properties
        private float _fov = 45.0f;
        private float _aspectRatio = 16.0f / 9.0f;
        private float _nearPlane = 0.1f;
        private float _farPlane = 100.0f;
        
        // Euler angles
        private float _yaw = -90.0f;    // Yaw is initialized to -90 degrees since a yaw of 0 
                                        // would result in a direction vector pointing to the right
        private float _pitch = 0.0f;    // Pitch is initialized to 0 degrees
        
        public float Yaw
        {
            get => _yaw;
            set
            {
                _yaw = value;
                UpdateCameraVectors();
            }
        }
        
        public float Pitch
        {
            get => _pitch;
            set
            {
                // Constrain pitch to avoid flipping
                _pitch = Math.Clamp(value, -89.0f, 89.0f);
                UpdateCameraVectors();
            }
        }

        public float FieldOfView 
        { 
            get => _fov; 
            set => _fov = value; 
        }

        public void SetAspectRatio(float width, float height)
        {
            _aspectRatio = width / height;
        }
        
        // Update front, right and up vectors based on the current yaw and pitch
        private void UpdateCameraVectors()
        {
            // Calculate the new front vector
            Vector3 newFront;
            newFront.X = MathF.Cos(RLMath.DegreesToRadians(_yaw)) * MathF.Cos(RLMath.DegreesToRadians(_pitch));
            newFront.Y = MathF.Sin(RLMath.DegreesToRadians(_pitch));
            newFront.Z = MathF.Sin(RLMath.DegreesToRadians(_yaw)) * MathF.Cos(RLMath.DegreesToRadians(_pitch));
            
            Front = Vector3.Normalize(newFront);
            // Recalculate the right and up vectors
            Right = Vector3.Normalize(Vector3.Cross(Front, Vector3.UnitY));
            Up = Vector3.Normalize(Vector3.Cross(Right, Front));
        }
        
        // Move the camera based on direction
        public void Move(Vector3 direction, float speed)
        {
            Position += direction * speed;
        }
        
        // Create view matrix
        public Matrix4x4 GetViewMatrix()
        {
            return Matrix4x4.CreateLookAt(Position, Position + Front, Up);
        }
        
        // Create projection matrix
        public Matrix4x4 GetProjectionMatrix()
        {
            return Matrix4x4.CreatePerspectiveFieldOfView(
                RLMath.DegreesToRadians(_fov),
                _aspectRatio,
                _nearPlane,
                _farPlane
            );
        }
    }
}