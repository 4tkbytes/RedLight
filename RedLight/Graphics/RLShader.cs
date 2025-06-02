using Serilog;
using Silk.NET.OpenGL;

namespace RedLight.Graphics;

public class RLShader
{
    public RLGraphics graphics;
    private ShaderType shaderType;
    public bool IsCompiled { get; private set; } = false;

    public uint Handle { get; private set; }

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

    public void Delete()
    {
        graphics.OpenGL.DeleteShader(Handle);
        IsCompiled = false;
    }
}

// Example usage: Creating and linking a shader program
public class RLShaderProgram
{
    public uint ProgramHandle { get; private set; }
    private RLGraphics graphics;

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

        // Optional: detach shaders after linking
        gl.DetachShader(ProgramHandle, vertexShader.Handle);
        gl.DetachShader(ProgramHandle, fragmentShader.Handle);
    }

    public void Use()
    {
        graphics.OpenGL.UseProgram(ProgramHandle);
    }

    public void Delete()
    {
        graphics.OpenGL.DeleteProgram(ProgramHandle);
    }
}

public enum ShaderType
{
    Vertex,
    Fragment,
    Geometry,               // anything beyond (and incl) this are considered mental illnesses
    TessellationControl,
    TessellationEvaluation,
    Compute
}