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
    private uint[] indices;

    public Matrix4x4 Transform { get; set; } = Matrix4x4.Identity;
    // ensure the name is passed (i dont know how)
    public string Name { get; set; }

    public Mesh(RLGraphics graphics, List<Vertex> vertices, uint[] indices)
    {
        this.graphics = graphics;
        this.indices = indices;
        this.vertices = vertices;
        var gl = graphics.OpenGL;
        vao = gl.GenVertexArray();
        vbo = gl.GenBuffer();
        ebo = gl.GenBuffer();
        gl.BindVertexArray(vao);

        // Flatten vertices to float array: position (3) + texcoords (2)
        float[] flatVerts = new float[vertices.Count * 5];
        for (int i = 0; i < vertices.Count; i++)
        {
            flatVerts[i * 5 + 0] = vertices[i].Position.X;
            flatVerts[i * 5 + 1] = vertices[i].Position.Y;
            flatVerts[i * 5 + 2] = vertices[i].Position.Z;
            flatVerts[i * 5 + 3] = vertices[i].TexCoords.X;
            flatVerts[i * 5 + 4] = vertices[i].TexCoords.Y;
        }

        unsafe
        {
            fixed (float* vertPtr = flatVerts)
            {
                gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
                gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(flatVerts.Length * sizeof(float)), vertPtr, BufferUsageARB.StaticDraw);
            }
            fixed (uint* buf = indices)
            {
                gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
                gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indices.Length * sizeof(uint)), buf, BufferUsageARB.StaticDraw);
            }
            // Attribute 0: position (vec3)
            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)0);
            // Attribute 1: texcoords (vec2)
            gl.EnableVertexAttribArray(1);
            gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)(3 * sizeof(float)));
        }
        gl.BindVertexArray(0);
        textures = new List<RLTexture>();
    }

    /// <summary>
    /// Attaches a shader to a mesh
    /// </summary>
    /// <param name="vertexShader">RLShader</param>
    /// <param name="fragmentShader">RLShader</param>
    /// <returns></returns>
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

    /// <summary>
    /// Attaches a texture to a mesh
    /// </summary>
    /// <param name="texture">RLTexture</param>
    /// <returns></returns>
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
        return new ConcreteTransformable<Mesh>(this);
    }

    public void Draw()
    {
        var gl = graphics.OpenGL;

        uint diffuseNr = 1, specularNr = 1, normalNr = 1, heightNr = 1;
        for (int i = 0; i < textures.Count; i++)
        {
            gl.ActiveTexture(TextureUnit.Texture0);

            string number = "1";
            string name = textures[i].Type.ToString().ToLower();
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

            string uniformName = $"texture_{name}{number}";
            int location = gl.GetUniformLocation(program, uniformName);
            if (location != -1)
                gl.Uniform1(location, i);

            gl.BindTexture(TextureTarget.Texture2D, textures[i].Handle);
        }

        unsafe
        {
            gl.BindVertexArray(vao);
            gl.DrawElements(PrimitiveType.Triangles, (uint)IndicesCount, DrawElementsType.UnsignedInt, null);
            gl.BindVertexArray(0);
        }


        gl.ActiveTexture(TextureUnit.Texture0);
    }

    public int IndicesCount => indices != null ? indices.Length : 0;
}