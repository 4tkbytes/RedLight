using System.Numerics;

namespace RedLight.Graphics
{
    public class Transform
    {
        public Vector3 Position { get; set; } = Vector3.Zero;
        public Vector3 Rotation { get; set; } = Vector3.Zero;
        public Vector3 Scale { get; set; } = Vector3.One;

        public Matrix4x4 ViewMatrix
        {
            get
            {
                // Create transformation matrix
                Matrix4x4 translation = Matrix4x4.CreateTranslation(Position);
                Matrix4x4 rotationX = Matrix4x4.CreateRotationX(Rotation.X);
                Matrix4x4 rotationY = Matrix4x4.CreateRotationY(Rotation.Y);
                Matrix4x4 rotationZ = Matrix4x4.CreateRotationZ(Rotation.Z);
                Matrix4x4 scale = Matrix4x4.CreateScale(Scale);

                // Combine transformations (scale, then rotate, then translate)
                return scale * rotationX * rotationY * rotationZ * translation;
            }
        }
    }
}