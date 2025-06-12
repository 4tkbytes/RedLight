using Silk.NET.Maths;
using System.Numerics;

namespace RedLight.Utils;

public static class RLUtils
{
    public static Vector3 SilkVector3DToNumericsVector3 (Vector3D<float> vector3D)
    {
        return new Vector3(vector3D.X, vector3D.Y, vector3D.Z);
    }

    public static Vector3D<float> NumericsVector3ToSilkVector3D (Vector3 vector3)
    {
        return new Vector3D<float>(vector3.X, vector3.Y, vector3.Z);
    }
}
