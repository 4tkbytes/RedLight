using Silk.NET.OpenGL;

namespace RedLight.Graphics;

public class RLShader
{
    private RLGraphics graphics;
    private ShaderType shaderType;
    
    public uint Shader { get; private set; }
    
    public RLShader(RLGraphics graphics, ShaderType shaderType, string shaderSource)
    {
        if (string.IsNullOrWhiteSpace(shaderSource))
        {
            throw new ArgumentException("Shader source cannot be null or empty", nameof(shaderSource));
        }
        
        this.shaderType = shaderType;
        this.graphics = graphics;
        
        Shader = graphics.OpenGL.CreateShader(shaderType switch
        {
            ShaderType.Vertex => Silk.NET.OpenGL.ShaderType.VertexShader,
            ShaderType.Fragment => Silk.NET.OpenGL.ShaderType.FragmentShader,
            _ => throw new ArgumentOutOfRangeException(nameof(shaderType), shaderType, null)
        });
        graphics.OpenGL.ShaderSource(Shader, shaderSource);
    }

    public void Compile()
    {
        graphics.OpenGL.CompileShader(Shader);
        
        graphics.OpenGL.GetShader(Shader, ShaderParameterName.CompileStatus, out int vStatus);
        if (vStatus != (int) GLEnum.True)
            throw new Exception($"Shader of type {shaderType} failed to compile: " + graphics.OpenGL.GetShaderInfoLog(Shader));
    }
    
    public void Delete()
    {
        graphics.OpenGL.DeleteShader(Shader);
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