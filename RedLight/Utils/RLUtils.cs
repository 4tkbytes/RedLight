using System.Drawing;
using Silk.NET.Maths;
using System.Numerics;

namespace RedLight.Utils;

public static class RLUtils
{
    public static Vector3 SilkVector3DToNumericsVector3 (Vector3D<float> vector3D)
    {
        return new Vector3(vector3D.X, vector3D.Y, vector3D.Z);
    }

    public static Vector2 SilkVector2DToNumericsVector2(Vector2D<int> vector2D)
    {
        return new Vector2(vector2D.X, vector2D.Y);
    }

    public static Vector3D<float> NumericsVector3ToSilkVector3D (Vector3 vector3)
    {
        return new Vector3D<float>(vector3.X, vector3.Y, vector3.Z);
    }

    public static Color Vector3ToColor(Vector3 vector3D)
    {
        return Color.FromArgb((int)(vector3D.X * 255), (int)(vector3D.Y * 255), (int)(vector3D.Z * 255));
    }

    public static Vector4 ColorToVector4(Color color)
    {
        return new Vector4(color.R, color.G, color.B, color.A);
    }
    
    public static Vector3 ColorToVector3(Color color)
    {
        return new Vector3((float) color.R / 255, (float) color.G / 255, (float) color.B / 255);
    }
}
