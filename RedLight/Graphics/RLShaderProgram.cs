using Serilog;
using Silk.NET.OpenGL;
using System.Numerics;

namespace RedLight.Graphics;

public class RLShaderProgram
{
    public uint ProgramHandle { get; private set; }
    private RLGraphics graphics;
    public string Name { get; set; } = "unnamed";
    public RLShader VertexShader { get; private set; }
    public RLShader FragmentShader { get; private set; }

    /// <summary>
    /// Creates a shader program for the RLShaders to link to. 
    /// </summary>
    /// <param name="graphics"></param>
    /// <param name="vertexShader"></param>
    /// <param name="fragmentShader"></param>
    /// <exception cref="Exception"></exception>
    public RLShaderProgram(RLGraphics graphics, RLShader vertexShader, RLShader fragmentShader)
    {
        this.graphics = graphics;
        this.VertexShader = vertexShader;
        this.FragmentShader = fragmentShader;
        var gl = graphics.OpenGL;

        ProgramHandle = gl.CreateProgram();
        gl.AttachShader(ProgramHandle, vertexShader.Handle);
        gl.AttachShader(ProgramHandle, fragmentShader.Handle);
        gl.LinkProgram(ProgramHandle);

        gl.GetProgram(ProgramHandle, ProgramPropertyARB.LinkStatus, out var status);
        if (status != (int)GLEnum.True)
        {
            var info = gl.GetProgramInfoLog(ProgramHandle);
            Log.Error("Failed to link shader program:\n{Info}", info);
            throw new Exception($"Shader program linking failed: {info}");
        }

        gl.DetachShader(ProgramHandle, vertexShader.Handle);
        gl.DetachShader(ProgramHandle, fragmentShader.Handle);
    }

    /// <summary>
    /// Enables/Uses the program
    /// </summary>
    public void Use()
    {
        graphics.OpenGL.UseProgram(ProgramHandle);
    }

    /// <summary>
    /// Deletes the program
    /// </summary>
    public void Delete()
    {
        graphics.OpenGL.DeleteProgram(ProgramHandle);
    }

    /// <summary>
    /// Sets a uniform value in the shader program with detailed logging
    /// </summary>
    /// <typeparam name="T">Type of the uniform value</typeparam>
    /// <param name="name">Name of the uniform</param>
    /// <param name="value">Value to set</param>
    public void SetUniform<T>(string name, T value)
    {
        var gl = graphics.OpenGL;
        int location = gl.GetUniformLocation(ProgramHandle, name);
        
        if (location == -1)
        {
            Log.Verbose("Uniform '{UniformName}' not found in shader program '{ShaderName}'", name, Name);
            return;
        }

        try
        {
            switch (value)
            {
                case int i:
                    gl.Uniform1(location, i);
                    break;
                case float f:
                    gl.Uniform1(location, f);
                    break;
                case Vector2 v2:
                    gl.Uniform2(location, v2.X, v2.Y);
                    break;
                case Vector3 v3:
                    gl.Uniform3(location, v3.X, v3.Y, v3.Z);
                    break;
                case Vector4 v4:
                    gl.Uniform4(location, v4.X, v4.Y, v4.Z, v4.W);
                    break;
                case Matrix4x4 m4:
                    unsafe
                    {
                        float* ptr = &m4.M11;
                        gl.UniformMatrix4(location, 1, false, ptr);
                    }
                    break;
                case bool b:
                    gl.Uniform1(location, b ? 1 : 0);
                    break;
                default:
                    Log.Warning("Uniform type {Type} is not supported for uniform '{UniformName}'", typeof(T), name);
                    throw new NotSupportedException($"Uniform type {typeof(T)} is not supported.");
            }
            
            Log.Verbose("Set uniform '{UniformName}' = {Value} in shader '{ShaderName}'", name, value, Name);
        }
        catch (Exception ex)
        {
            Log.Error("Failed to set uniform '{UniformName}' in shader '{ShaderName}': {Error}", name, Name, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Convenience methods for common uniform types with better logging
    /// </summary>
    public void SetUniform(string name, float value)
    {
        SetUniform<float>(name, value);
    }

    public void SetUniform(string name, Vector3 value)
    {
        SetUniform<Vector3>(name, value);
    }

    public void SetUniform(string name, Matrix4x4 value)
    {
        SetUniform<Matrix4x4>(name, value);
    }

    public void SetUniform(string name, int value)
    {
        SetUniform<int>(name, value);
    }

    public void SetUniform(string name, bool value)
    {
        SetUniform<bool>(name, value);
    }

    /// <summary>
    /// Check if a uniform exists in the shader program
    /// </summary>
    /// <param name="name">Name of the uniform</param>
    /// <returns>True if uniform exists, false otherwise</returns>
    public bool HasUniform(string name)
    {
        var gl = graphics.OpenGL;
        int location = gl.GetUniformLocation(ProgramHandle, name);
        return location != -1;
    }

    /// <summary>
    /// Get all active uniforms in the shader program (for debugging)
    /// </summary>
    /// <returns>List of uniform names</returns>
    public List<string> GetActiveUniforms()
    {
        var gl = graphics.OpenGL;
        var uniforms = new List<string>();
        
        gl.GetProgram(ProgramHandle, ProgramPropertyARB.ActiveUniforms, out int uniformCount);
        
        for (uint i = 0; i < uniformCount; i++)
        {
            string name = gl.GetActiveUniform(ProgramHandle, i, out _, out _);
            uniforms.Add(name);
        }
        
        return uniforms;
    }
}