using Silk.NET.OpenGL;

namespace RedLight.Graphics;

public class Mesh
{
    public uint vao;
    private uint vbo;
    private uint ebo;
    public uint program;
    
    internal Mesh(GL gl, float[] vertices, uint[] indices, RLShader vertexShader, RLShader fragmentShader)
    {
        vao = gl.GenVertexArray();
        gl.BindVertexArray(vao);
        
        vbo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);

        unsafe
        {
            fixed (float* buf = vertices)
                gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint) (vertices.Length * sizeof(float)), buf, BufferUsageARB.StaticDraw);
        }

        ebo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
        
        unsafe
        {
            fixed (uint* buf = indices)
                gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint) (indices.Length * sizeof(uint)), buf, BufferUsageARB.StaticDraw);
        }
        
        vertexShader.Compile();
        fragmentShader.Compile();
        
        program = gl.CreateProgram();
        gl.AttachShader(program, vertexShader.Handle);
        gl.AttachShader(program, fragmentShader.Handle);
        
        gl.LinkProgram(program);
        
        gl.GetProgram(program, GLEnum.LinkStatus, out var status);
        if (status == 0)
        {
            Console.WriteLine($"Error linking shader {gl.GetProgramInfoLog(program)}");
        }
        
        gl.DetachShader(program, vertexShader.Handle);
        gl.DetachShader(program, fragmentShader.Handle);
        vertexShader.Delete();
        fragmentShader.Delete();

        unsafe
        {
            uint vertCoordLoc = 0;
            gl.VertexAttribPointer(vertCoordLoc, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), null);
            gl.EnableVertexAttribArray(vertCoordLoc);
        }

        unsafe
        {
            uint texCoordLoc = 1;
            gl.EnableVertexAttribArray(texCoordLoc);
            gl.VertexAttribPointer(texCoordLoc, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)(3*sizeof(float)));
        }

        int location = gl.GetUniformLocation(program, "uTexture");
        gl.Uniform1(location, 0);
    }
}