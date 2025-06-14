using Serilog;
using Silk.NET.OpenGL;

namespace RedLight.Graphics;

public class RLShader
{
    public RLGraphics graphics;
    private ShaderType shaderType;
    public bool IsCompiled { get; private set; } = false;
    public string Name { get; set; } = "unnamed";

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

/// <summary>
/// This contains the different shader types. So far, there is only support for Vertex and Fragment Shaders. Anything
/// else is considered a mental illness. 
/// </summary>
public enum ShaderType
{
    Vertex,
    Fragment
}