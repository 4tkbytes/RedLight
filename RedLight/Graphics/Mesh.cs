using System.Numerics;
using Serilog;
using Silk.NET.OpenGL;

namespace RedLight.Graphics;

public class Mesh
{
    public uint vao;
    private uint vbo;
    private uint ebo;
    public uint program;
    
    public Matrix4x4 Transform { get; set; } = Matrix4x4.Identity;
    
    internal Mesh(GL gl, float[] vertices, uint[] indices, RLShader vertexShader, RLShader fragmentShader)
    {
        vao = gl.GenVertexArray();
        gl.BindVertexArray(vao);
        
        vbo = gl.GenBuffer();
        ebo = gl.GenBuffer();

        // bind vert
        unsafe
        {
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);

            fixed (float* buf = vertices)
                gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint) (vertices.Length * sizeof(float)), buf, BufferUsageARB.StaticDraw);
        }
        
        // bind index
        unsafe
        {
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);

            fixed (uint* buf = indices)
                gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint) (indices.Length * sizeof(uint)), buf, BufferUsageARB.StaticDraw);
        }
        
        // position attrib
        unsafe
        {
            uint vertCoordLoc = 0;
            gl.VertexAttribPointer(vertCoordLoc, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), null);
            gl.EnableVertexAttribArray(vertCoordLoc);
        }

        // tex coord attrib
        unsafe
        {
            uint texCoordLoc = 1;
            gl.EnableVertexAttribArray(texCoordLoc);
            gl.VertexAttribPointer(texCoordLoc, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)(3*sizeof(float)));
        }
        
        vertexShader.Compile();
        fragmentShader.Compile();
        
        program = gl.CreateProgram();
        gl.AttachShader(program, vertexShader.Handle);
        gl.AttachShader(program, fragmentShader.Handle);
        
        gl.LinkProgram(program);
        
        gl.GetProgram(program, GLEnum.LinkStatus, out var linkStatus);
        if (linkStatus != (int)GLEnum.True)
        {
            var info = gl.GetProgramInfoLog(program);
            Log.Error("Failed to link shader program:\n{Info}", info);
        }
        
        gl.DetachShader(program, vertexShader.Handle);
        gl.DetachShader(program, fragmentShader.Handle);
        vertexShader.Delete();
        fragmentShader.Delete();
        
        unsafe
        {
            gl.UseProgram(program);

            int texLoc = gl.GetUniformLocation(program, "uTexture");
            gl.Uniform1(texLoc, 0);

            int modelLoc = gl.GetUniformLocation(program, "model");
            var local = Transform;
            float* ptr = (float*)&local;
            gl.UniformMatrix4(modelLoc, 1, false, ptr);
        }
    }

    public Transformable<Mesh> MakeTransformable() => new Transformable<Mesh>(this);
    
}