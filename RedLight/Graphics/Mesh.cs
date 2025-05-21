using System.Numerics;
using Silk.NET.OpenGL;
using RedLight.Graphics;

namespace RedLight.Graphics
{
    public class Mesh
    {
        private GL Gl;
        private uint Vao, Vbo, Ebo, TcboHandle;
        private float[] Vertices;
        private uint[] Indices;
        private float[] TexCoords;
        private Shader Shader;
        private Texture2D Texture;
        
        public Transform Transform { get; set; } = new Transform();

        public Mesh(GL gl, float[] vertices, uint[] indices, float[] texCoords, Shader shader, Texture2D texture)
        {
            Gl = gl;
            Vertices = vertices;
            Indices = indices;
            TexCoords = texCoords;
            Shader = shader;
            Texture = texture;

            CreateBuffers();
            SetupVertexAttributes();
        }

        private void CreateBuffers()
        {
            Vao = Gl.GenVertexArray();
            Gl.BindVertexArray(Vao);

            // Vertex positions buffer
            Vbo = Gl.GenBuffer();
            Gl.BindBuffer(BufferTargetARB.ArrayBuffer, Vbo);
            unsafe
            {
                fixed (float* v = &Vertices[0])
                {
                    Gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(Vertices.Length * sizeof(float)), v, BufferUsageARB.StaticDraw);
                }
            }

            // Texture coordinates buffer
            TcboHandle = Gl.GenBuffer();
            Gl.BindBuffer(BufferTargetARB.ArrayBuffer, TcboHandle);
            unsafe
            {
                fixed (float* t = &TexCoords[0])
                {
                    Gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(TexCoords.Length * sizeof(float)), t, BufferUsageARB.StaticDraw);
                }
            }

            // Element buffer
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
            // Set up vertex positions (attribute 0)
            Gl.BindBuffer(BufferTargetARB.ArrayBuffer, Vbo);
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            Gl.EnableVertexAttribArray(0);

            // Set up texture coordinates (attribute 1)
            Gl.BindBuffer(BufferTargetARB.ArrayBuffer, TcboHandle);
            Gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
            Gl.EnableVertexAttribArray(1);
        }

        public void Render()
        {
            Shader.Use();
    
            // Use Uniforms property instead of unsafe code
            Shader.Uniforms.SetMatrix4("uModel", Transform.ViewMatrix);

            // binding da texture
            if (Texture != null)
            {
                Texture.Bind(TextureUnit.Texture0);
                Shader.Uniforms.SetInt("texture0", 0);
            }

            Gl.BindVertexArray(Vao);
            unsafe
            {
                Gl.DrawElements(PrimitiveType.Triangles, (uint)Indices.Length, DrawElementsType.UnsignedInt, null);
            }
        }

        public void Dispose()
        {
            Gl.DeleteBuffer(Vbo);
            Gl.DeleteBuffer(Ebo);
            Gl.DeleteBuffer(TcboHandle);
            Gl.DeleteVertexArray(Vao);
        }
    }
}