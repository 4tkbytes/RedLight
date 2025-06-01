using System.Numerics;

namespace RedLight.Utils;

public static class RLMath
{
    public static T DegreesToRadians<T>(T degrees)
        where T : struct, IConvertible
    {
        double value = degrees.ToDouble(null) * (Math.PI / 180);
        return (T)Convert.ChangeType(value, typeof(T));
    }
    
    public static T RadiansToDegrees<T>(T radians)
        where T : struct, IConvertible
    {
        double value = radians.ToDouble(null) * (180 / Math.PI);
        return (T)Convert.ChangeType(value, typeof(T));
    }
    
    public static Matrix4x4 Rotate(Matrix4x4 matrix, float radians, Vector3 axis)
    {
        axis = Vector3.Normalize(axis);
        float cos = (float)Math.Cos(radians);
        float sin = (float)Math.Sin(radians);
        float oneMinus = 1f - cos;

        float x = axis.X, y = axis.Y, z = axis.Z;
        float xx = x * x, yy = y * y, zz = z * z;
        float xy = x * y, xz = x * z, yz = y * z;

        var rot = new Matrix4x4(
            cos + xx * oneMinus,  xy * oneMinus - z * sin, xz * oneMinus + y * sin, 0f,
            xy * oneMinus + z * sin, cos + yy * oneMinus,  yz * oneMinus - x * sin, 0f,
            xz * oneMinus - y * sin, yz * oneMinus + x * sin, cos + zz * oneMinus,  0f,
            0f,                       0f,                     0f,                    1f
        );

        return Matrix4x4.Multiply(matrix, rot);
    }
    
    public static Matrix4x4 Scale(Matrix4x4 matrix, Vector3 scale)
    {
        var scaleMat = new Matrix4x4(
            scale.X, 0f,     0f,     0f,
            0f,     scale.Y, 0f,     0f,
            0f,     0f,     scale.Z, 0f,
            0f,     0f,     0f,     1f
        );
        return Matrix4x4.Multiply(matrix, scaleMat);
    }
    
    public static Matrix4x4 Translate(Matrix4x4 matrix, Vector3 translation)
    {
        var transMat = Matrix4x4.CreateTranslation(translation);
        return Matrix4x4.Multiply(matrix, transMat);
    }
    
    public static Matrix4x4 Mat4(float scalar)
    {
        return Matrix4x4.Identity * scalar;
    }
    
    public static Matrix4x4 Ortho(
        float left,   float right,
        float bottom, float top,
        float near,   float far)
    {
        float invRL = 1f / (right - left);
        float invTB = 1f / (top - bottom);
        float invFN = 1f / (far  - near);

        return new Matrix4x4(
            2f * invRL,    0f,            0f,           -(right + left)   * invRL,
            0f,            2f * invTB,    0f,           -(top   + bottom) * invTB,
            0f,            0f,           -2f * invFN,   -(far   + near)   * invFN,
            0f,            0f,            0f,            1f
        );
    }
    
    public static Matrix4x4 Perspective(Matrix4x4 projection, float fov, float aspect, float near, float far)
    {
        float f = 1f / (float)Math.Tan(fov * 0.5f);
        return new Matrix4x4(
            f / aspect,  0f,                           0f,                              0f,
            0f,          f,                            0f,                              0f,
            0f,          0f,    (far + near) / (near - far), (2f * far * near) / (near - far),
            0f,          0f,                          -1f,                              0f
        ) * projection;
    }
}