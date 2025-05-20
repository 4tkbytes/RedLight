using Silk.NET.OpenGL;
using RedLight.Graphics;

public class Mesh
{
    private GL Gl;
    private uint Vao, Vbo, Ebo;
    private float[] Vertices;
    private uint[] Indices;
    private RedLight.Graphics.Shader Shader;

    public Mesh(GL gl, float[] vertices, uint[] indices, RedLight.Graphics.Shader shader)
    {
        Gl = gl;
        Vertices = vertices;
        Indices = indices;
        Shader = shader;

        CreateBuffers();
        SetupVertexAttributes();
    }

    private void CreateBuffers()
    {
        Vao = Gl.GenVertexArray();
        Gl.BindVertexArray(Vao);

        Vbo = Gl.GenBuffer();
        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, Vbo);
        unsafe
        {
            fixed (float* v = &Vertices[0])
            {
                Gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(Vertices.Length * sizeof(float)), v, BufferUsageARB.StaticDraw);
            }
        }

        Ebo = Gl.GenBuffer();
        Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, Ebo);
        unsafe
        {
            fixed (uint* i = &Indices[0])
            {
                Gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(Indices.Length * sizeof(uint)), i, BufferUsageARB.StaticDraw);
            }
        }
    }

    private void SetupVertexAttributes()
    {
        Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        Gl.EnableVertexAttribArray(0);
    }

    public void Render()
    {
        unsafe
        {
            Shader.Use();
            Gl.BindVertexArray(Vao);
            Gl.DrawElements(PrimitiveType.Triangles, (uint)Indices.Length, DrawElementsType.UnsignedInt, null);
        }
    }
}