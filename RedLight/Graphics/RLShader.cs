using Serilog;
using Silk.NET.OpenGL;

namespace RedLight.Graphics;

public class RLShader
{
    private RLGraphics graphics;
    private ShaderType shaderType;
    
    public uint Handle { get; private set; }
    
    public RLShader(RLGraphics graphics, ShaderType shaderType, string shaderSource)
    {
        if (string.IsNullOrWhiteSpace(shaderSource))
        {
            throw new ArgumentException("Shader source cannot be null or empty", nameof(shaderSource));
        }
        
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
        graphics.OpenGL.CompileShader(Handle);
        graphics.OpenGL.GetShader(Handle, ShaderParameterName.CompileStatus, out var status);
        if (status != (int)GLEnum.True)
        {
            var info = graphics.OpenGL.GetShaderInfoLog(Handle);
            Log.Error("Failed to compile {ShaderType} shader:\n{Info}", shaderType, info);
        }
    }
    
    public void Delete()
    {
        graphics.OpenGL.DeleteShader(Handle);
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