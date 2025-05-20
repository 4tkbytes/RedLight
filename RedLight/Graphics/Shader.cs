using Silk.NET.OpenGL;

namespace RedLight.Graphics;

public class Shader
{
    private readonly GL _gl;
    public uint Handle { get; }

    public Shader(GL gl, string vertexSource, string fragmentSource)
    {
        _gl = gl;
        uint vertex = CompileShader(ShaderType.VertexShader, vertexSource);
        uint fragment = CompileShader(ShaderType.FragmentShader, fragmentSource);

        Handle = _gl.CreateProgram();
        _gl.AttachShader(Handle, vertex);
        _gl.AttachShader(Handle, fragment);
        _gl.LinkProgram(Handle);

        _gl.GetProgram(Handle, GLEnum.LinkStatus, out var status);
        if (status == 0)
        {
            throw new Exception($"Shader linking failed: {_gl.GetProgramInfoLog(Handle)}");
        }

        _gl.DetachShader(Handle, vertex);
        _gl.DetachShader(Handle, fragment);
        _gl.DeleteShader(vertex);
        _gl.DeleteShader(fragment);
    }

    private uint CompileShader(ShaderType type, string source)
    {
        uint shader = _gl.CreateShader(type);
        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);

        string infoLog = _gl.GetShaderInfoLog(shader);
        if (!string.IsNullOrWhiteSpace(infoLog))
        {
            throw new Exception($"Error compiling {type}: {infoLog}");
        }
        return shader;
    }

    public void Use()
    {
        _gl.UseProgram(Handle);
    }

    public void Dispose()
    {
        _gl.DeleteProgram(Handle);
    }
}