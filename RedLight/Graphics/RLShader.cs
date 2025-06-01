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
    }

    public void Compile()
    {
        graphics.OpenGL.CompileShader(Handle);
        
        graphics.OpenGL.GetShader(Handle, ShaderParameterName.CompileStatus, out int vStatus);
        if (vStatus != (int) GLEnum.True)
            throw new Exception($"Shader of type {shaderType} failed to compile: " + graphics.OpenGL.GetShaderInfoLog(Handle));
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
    Geometry,               // anything beyond (and incl) this are mental illnesses
    TessellationControl,
    TessellationEvaluation,
    Compute
}