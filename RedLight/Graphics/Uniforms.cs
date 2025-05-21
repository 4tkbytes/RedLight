using Silk.NET.OpenGL;
using System.Collections.Generic;
using System.Numerics;

namespace RedLight.Graphics;

public class Uniforms
{
    private readonly GL _gl;
    private readonly uint _shaderHandle;
    private readonly Dictionary<string, int> _uniformLocations = new();

    public Uniforms(GL gl, uint shaderHandle)
    {
        _gl = gl;
        _shaderHandle = shaderHandle;
    }

    private int GetUniformLocation(string name)
    {
        if (!_uniformLocations.TryGetValue(name, out int location))
        {
            location = _gl.GetUniformLocation(_shaderHandle, name);
            _uniformLocations[name] = location;
        }
        return location;
    }

    public void SetInt(string name, int value)
    {
        _gl.Uniform1(GetUniformLocation(name), value);
    }

    public void SetFloat(string name, float value)
    {
        _gl.Uniform1(GetUniformLocation(name), value);
    }

    public void SetVector2(string name, Vector2 value)
    {
        _gl.Uniform2(GetUniformLocation(name), value.X, value.Y);
    }

    public void SetVector3(string name, Vector3 value)
    {
        _gl.Uniform3(GetUniformLocation(name), value.X, value.Y, value.Z);
    }

    public void SetVector4(string name, Vector4 value)
    {
        _gl.Uniform4(GetUniformLocation(name), value.X, value.Y, value.Z, value.W);
    }

    public void SetMatrix4(string name, Matrix4x4 value)
    {
        unsafe
        {
            _gl.UniformMatrix4(GetUniformLocation(name), 1, false, (float*)&value);
        }
    }
}