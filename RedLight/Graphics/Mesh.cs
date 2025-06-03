using System.Numerics;
using RedLight.Utils;
using Serilog;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System.Runtime.InteropServices;

namespace RedLight.Graphics;

[StructLayout(LayoutKind.Sequential)]
public struct Vertex
{
    public Vector3D<float> Position;
    public Vector3D<float> Normal;
    public Vector2D<float> TexCoords;
    public Vector3D<float> Tangent;
    public Vector3D<float> BitTangent;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public int[] BoneIDs;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] Weights;
}

public class Mesh
{
    public uint vao;
    private uint vbo;
    private uint ebo;
    public uint program;
    private List<Vertex> vertices;
    private List<RLTexture> textures;
    private RLGraphics graphics;

    public Matrix4x4 Transform { get; set; } = Matrix4x4.Identity;

    internal Mesh(RLGraphics graphics, List<Vertex> vertices, uint[] indices, RLShader vertexShader, RLShader fragmentShader)
    {
        this.graphics = graphics;
        var gl = graphics.OpenGL;

        vao = gl.GenVertexArray();
        vbo = gl.GenBuffer();
        ebo = gl.GenBuffer();

        // bind vao
        gl.BindVertexArray(vao);

        // bind vert
        unsafe
        {
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);

            var vertArray = vertices.ToArray();
            fixed (Vertex* vertPtr = vertArray)
            {
                gl.BufferData(
                    BufferTargetARB.ArrayBuffer,
                    (nuint)(vertArray.Length * sizeof(Vertex)),
                    (nint)vertPtr,
                    BufferUsageARB.StaticDraw
                );
            }
        }

        // bind index
        unsafe
        {
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);

            fixed (uint* buf = indices)
                gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indices.Length * sizeof(uint)), buf, BufferUsageARB.StaticDraw);
        }

        unsafe
        {
            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), (void*)0);

            gl.EnableVertexAttribArray(1);
            gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), (void*)Marshal.OffsetOf<Vertex>("Normal"));

            gl.EnableVertexAttribArray(2);
            gl.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), (void*)Marshal.OffsetOf<Vertex>("TexCoords"));

            gl.EnableVertexAttribArray(2);
            gl.VertexAttribPointer(3, 3, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), (void*)Marshal.OffsetOf<Vertex>("Tangent"));

            gl.EnableVertexAttribArray(4);
            gl.VertexAttribPointer(4, 3, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), (void*)Marshal.OffsetOf<Vertex>("BitTangent"));

            gl.EnableVertexAttribArray(5);
            gl.VertexAttribPointer(5, 4, VertexAttribPointerType.Int, false, (uint)sizeof(Vertex), (void*)Marshal.OffsetOf<Vertex>("BoneIDs"));

            gl.EnableVertexAttribArray(6);
            gl.VertexAttribPointer(6, 4, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), (void*)Marshal.OffsetOf<Vertex>("Weights"));

            gl.BindVertexArray(0);

        }

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

        if (graphics.IsRendering)
        {
            vertexShader.Delete();
            fragmentShader.Delete();
        }

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
    
    public Mesh(RLGraphics graphics, List<Vertex> vertices, uint[] indices)
    {
        this.graphics = graphics;
        var gl = graphics.OpenGL;
        vao = gl.GenVertexArray();
        vbo = gl.GenBuffer();
        ebo = gl.GenBuffer();

        gl.BindVertexArray(vao);

        unsafe
        {
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
            var vertArray = vertices.ToArray();
            fixed (Vertex* vertPtr = vertArray)
            {
                gl.BufferData(
                    BufferTargetARB.ArrayBuffer,
                    (nuint)(vertArray.Length * sizeof(Vertex)),
                    (nint)vertPtr,
                    BufferUsageARB.StaticDraw
                );
            }
        }

        unsafe
        {
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
            fixed (uint* buf = indices)
                gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indices.Length * sizeof(uint)), buf, BufferUsageARB.StaticDraw);
        }

        unsafe
        {
            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), (void*)0);

            gl.EnableVertexAttribArray(1);
            gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), (void*)Marshal.OffsetOf<Vertex>("Normal"));

            gl.EnableVertexAttribArray(2);
            gl.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), (void*)Marshal.OffsetOf<Vertex>("TexCoords"));

            gl.EnableVertexAttribArray(3);
            gl.VertexAttribPointer(3, 3, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), (void*)Marshal.OffsetOf<Vertex>("Tangent"));

            gl.EnableVertexAttribArray(4);
            gl.VertexAttribPointer(4, 3, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), (void*)Marshal.OffsetOf<Vertex>("BitTangent"));

            gl.EnableVertexAttribArray(5);
            gl.VertexAttribPointer(5, 4, VertexAttribPointerType.Int, false, (uint)sizeof(Vertex), (void*)Marshal.OffsetOf<Vertex>("BoneIDs"));

            gl.EnableVertexAttribArray(6);
            gl.VertexAttribPointer(6, 4, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), (void*)Marshal.OffsetOf<Vertex>("Weights"));
        }

        gl.BindVertexArray(0);
        textures = new List<RLTexture>();
    }

    public Mesh AttachShader(RLShader vertexShader, RLShader fragmentShader)
    {
        var gl = graphics.OpenGL;
        program = gl.CreateProgram();
        gl.AttachShader(program, vertexShader.Handle);
        gl.AttachShader(program, fragmentShader.Handle);
        gl.LinkProgram(program);

        gl.GetProgram(program, GLEnum.LinkStatus, out var linkStatus);
        if (linkStatus != (int)GLEnum.True)
        {
            var info = gl.GetProgramInfoLog(program);
            Log.Error("Failed to link shader program:\\n{Info}", info);
        }

        gl.DetachShader(program, vertexShader.Handle);
        gl.DetachShader(program, fragmentShader.Handle);

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

        return this;
    }

    public Mesh AttachTexture(RLTexture texture)
    {
        textures.Add(texture);
        return this;
    }

    public Mesh AttachTexture(List<RLTexture> textureList)
    {
        textures = textureList;
        return this;
    }

    public Transformable<Mesh> MakeTransformable()
    {
        Log.Verbose("Made mesh transformable");
        return new Transformable<Mesh>(this);
    }

    public void Draw(RLGraphics graphics)
    {
        var gl = graphics.OpenGL;

        uint diffuseNr = 1, specularNr = 1, normalNr = 1, heightNr = 1;
        for (int i = 0; i < textures.Count; i++)
        {
            gl.ActiveTexture(TextureUnit.Texture0 + i);

            string number = "1";
            string name = textures[i].Type.ToString().ToLower(); // e.g. "diffuse"
            switch (textures[i].Type)
            {
                case RLTextureType.Diffuse:
                    number = (diffuseNr++).ToString();
                    break;
                case RLTextureType.Specular:
                    number = (specularNr++).ToString();
                    break;
                case RLTextureType.Normal:
                    number = (normalNr++).ToString();
                    break;
                case RLTextureType.Height:
                    number = (heightNr++).ToString();
                    break;
            }

            // Compose uniform name like "texture_diffuse1"
            string uniformName = $"texture_{name}{number}";
            int location = gl.GetUniformLocation(program, uniformName);
            if (location != -1)
                gl.Uniform1(location, i);

            gl.BindTexture(TextureTarget.Texture2D, textures[i].Handle);
        }

        unsafe
        {
            gl.BindVertexArray(vao);
            gl.DrawElements(PrimitiveType.Triangles, (uint)vertices.Count, DrawElementsType.UnsignedInt, null);
            gl.BindVertexArray(0);
        }
        

        gl.ActiveTexture(TextureUnit.Texture0);
    }

}