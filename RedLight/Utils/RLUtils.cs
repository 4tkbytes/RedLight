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
    
    /// <summary>
    /// Extracts the uniform location number from an OpenGL error message
    /// </summary>
    /// <param name="errorMessage">The OpenGL error message</param>
    /// <returns>The uniform location as an integer, or -1 if not found</returns>
    public static int ExtractUniformLocation(string errorMessage)
    {
        // Regex to find "uniform of location "X"" pattern
        var regex = new System.Text.RegularExpressions.Regex("uniform of location \"(\\d+)\"");
        var match = regex.Match(errorMessage);
    
        if (match.Success && match.Groups.Count > 1)
        {
            if (int.TryParse(match.Groups[1].Value, out int location))
            {
                return location;
            }
        }
    
        return -1;
    }
    
    /// <summary>
    /// Extracts both program handle and uniform location from an OpenGL error message
    /// </summary>
    /// <param name="errorMessage">The OpenGL error message</param>
    /// <returns>Tuple containing (programHandle, uniformLocation), or (-1, -1) if not found</returns>
    public static (int ProgramHandle, int UniformLocation) ExtractGLErrorInfo(string errorMessage)
    {
        int programHandle = -1;
        int uniformLocation = -1;
    
        // Extract uniform location
        var locationRegex = new System.Text.RegularExpressions.Regex("uniform of location \"(\\d+)\"");
        var locationMatch = locationRegex.Match(errorMessage);
        if (locationMatch.Success && locationMatch.Groups.Count > 1)
        {
            int.TryParse(locationMatch.Groups[1].Value, out uniformLocation);
        }
    
        // Extract program handle
        var programRegex = new System.Text.RegularExpressions.Regex("in program (\\d+),");
        var programMatch = programRegex.Match(errorMessage);
        if (programMatch.Success && programMatch.Groups.Count > 1)
        {
            int.TryParse(programMatch.Groups[1].Value, out programHandle);
        }
    
        return (programHandle, uniformLocation);
    }
}
