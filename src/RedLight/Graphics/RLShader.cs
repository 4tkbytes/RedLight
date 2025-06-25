using System.Drawing;
using RedLight.Utils;
using Serilog;
using Silk.NET.OpenGL;

namespace RedLight.Graphics;

public class RLShader
{
    public RLGraphics graphics;
    private ShaderType shaderType;
    public bool IsCompiled { get; private set; } = false;

    public uint Handle { get; private set; }

    /// <summary>
    /// This function creates a new shader for OpenGL. 
    /// </summary>
    /// <param name="graphics"></param>
    /// <param name="shaderType"></param>
    /// <param name="shaderSource"></param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public RLShader(RLGraphics graphics, ShaderType shaderType, string shaderSource)
    {
        if (string.IsNullOrWhiteSpace(shaderSource))
            throw new ArgumentException("Shader source cannot be null or empty", nameof(shaderSource));

        this.shaderType = shaderType;
        this.graphics = graphics;

        Handle = graphics.OpenGL.CreateShader(shaderType switch
        {
            ShaderType.Vertex => Silk.NET.OpenGL.ShaderType.VertexShader,
            ShaderType.Fragment => Silk.NET.OpenGL.ShaderType.FragmentShader,
            _ => throw new ArgumentOutOfRangeException(nameof(shaderType), shaderType, null)
        });
        graphics.OpenGL.ShaderSource(Handle, shaderSource);

        Compile();
    }

    /// <summary>
    /// As the name suggests, it just compiles the shader. 
    /// </summary>
    /// <exception cref="Exception"></exception>
    public void Compile()
    {
        if (!IsCompiled)
            graphics.OpenGL.CompileShader(Handle);

        graphics.OpenGL.GetShader(Handle, ShaderParameterName.CompileStatus, out var status);
        if (status != (int)GLEnum.True)
        {
            var info = graphics.OpenGL.GetShaderInfoLog(Handle);
            Log.Error("Failed to compile {ShaderType} shader:\n{Info}", shaderType, info);
            throw new Exception($"Shader compilation failed: {info}");
        }
        IsCompiled = true;
    }

    /// <summary>
    /// Deletes the shader. Thats it. 
    /// </summary>
    public void Delete()
    {
        graphics.OpenGL.DeleteShader(Handle);
        IsCompiled = false;
    }
}

public class RLShaderProgram
{
    public uint ProgramHandle { get; private set; }
    private RLGraphics graphics;

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

    public void SetUniform<T>(string name, T value)
    {
        var gl = graphics.OpenGL;
        int location = gl.GetUniformLocation(ProgramHandle, name);
        if (location == -1)
        {
            Log.Warning("Uniform {UniformName} not found in shader program.", name);
            return;
        }

        switch (value)
        {
            case int i:
                gl.Uniform1(location, i);
                break;
            case float f:
                gl.Uniform1(location, f);
                break;
            case System.Numerics.Vector2 v2:
                gl.Uniform2(location, v2.X, v2.Y);
                break;
            case System.Numerics.Vector3 v3:
                gl.Uniform3(location, v3.X, v3.Y, v3.Z);
                break;
            case System.Numerics.Vector4 v4:
                gl.Uniform4(location, v4.X, v4.Y, v4.Z, v4.W);
                break;
            case System.Numerics.Matrix4x4 m4:
                unsafe
                {
                    float* ptr = &m4.M11;
                    gl.UniformMatrix4(location, 1, false, ptr);
                }
                break;
            case bool b:
                gl.Uniform1(location, b ? 1 : 0);
                break;
            case Color color:
                SetUniform(name, RLUtils.ColorToVector3(color));
                break;
            default:
                throw new NotSupportedException($"Uniform type {typeof(T)} is not supported.");
        }
    }
}

/// <summary>
/// This contains the different shader types. So far, there is only support for Vertex and Fragment Shaders. Anything
/// else is considered a mental illness. 
/// </summary>
public enum ShaderType
{
    Vertex,
    Fragment,
    Geometry,               // anything beyond (and incl) this are considered mental illnesses
    TessellationControl,
    TessellationEvaluation,
    Compute
}