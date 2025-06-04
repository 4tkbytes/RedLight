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

    // internal Mesh(RLGraphics graphics, List<Vertex> vertices, uint[] indices, RLShader vertexShader, RLShader fragmentShader)
    // {
    //     this.graphics = graphics;
    //     Log.Verbose("[Mesh] Creating mesh with {VertexCount} vertices and {IndexCount} indices", vertices.Count, indices.Length);
    //     var gl = graphics.OpenGL;
    //
    //     vao = gl.GenVertexArray();
    //     vbo = gl.GenBuffer();
    //     ebo = gl.GenBuffer();
    //     Log.Verbose("[Mesh] Generated VAO: {VAO}, VBO: {VBO}, EBO: {EBO}", vao, vbo, ebo);
    //
    //     // bind vao
    //     gl.BindVertexArray(vao);
    //     Log.Verbose("[Mesh] Bound VAO: {VAO}", vao);
    //
    //     // bind vert
    //     unsafe
    //     {
    //         gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
    //         Log.Verbose("[Mesh] Bound VBO: {VBO}", vbo);
    //
    //         var vertArray = vertices.ToArray();
    //         fixed (Vertex* vertPtr = vertArray)
    //         {
    //             gl.BufferData(
    //                 BufferTargetARB.ArrayBuffer,
    //                 (nuint)(vertArray.Length * sizeof(Vertex)),
    //                 (nint)vertPtr,
    //                 BufferUsageARB.StaticDraw
    //             );
    //             Log.Verbose("[Mesh] Uploaded vertex data: {Size} bytes", vertArray.Length * sizeof(Vertex));
    //         }
    //     }
    //
    //     // bind index
    //     unsafe
    //     {
    //         gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
    //         Log.Verbose("[Mesh] Bound EBO: {EBO}", ebo);
    //
    //         fixed (uint* buf = indices)
    //             gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indices.Length * sizeof(uint)), buf, BufferUsageARB.StaticDraw);
    //         Log.Verbose("[Mesh] Uploaded index data: {Size} bytes", indices.Length * sizeof(uint));
    //     }
    //
    //     unsafe
    //     {
    //         gl.EnableVertexAttribArray(0);
    //         gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), (void*)0);
    //         Log.Verbose("[Mesh] Set vertex attrib pointer for Position");
    //
    //         gl.EnableVertexAttribArray(1);
    //         gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), (void*)Marshal.OffsetOf<Vertex>("Normal"));
    //         Log.Verbose("[Mesh] Set vertex attrib pointer for Normal");
    //
    //         gl.EnableVertexAttribArray(2);
    //         gl.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), (void*)Marshal.OffsetOf<Vertex>("TexCoords"));
    //         Log.Verbose("[Mesh] Set vertex attrib pointer for TexCoords");
    //
    //         gl.EnableVertexAttribArray(2);
    //         gl.VertexAttribPointer(3, 3, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), (void*)Marshal.OffsetOf<Vertex>("Tangent"));
    //         Log.Verbose("[Mesh] Set vertex attrib pointer for Tangent");
    //
    //         gl.EnableVertexAttribArray(4);
    //         gl.VertexAttribPointer(4, 3, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), (void*)Marshal.OffsetOf<Vertex>("BitTangent"));
    //         Log.Verbose("[Mesh] Set vertex attrib pointer for BitTangent");
    //
    //         gl.EnableVertexAttribArray(5);
    //         gl.VertexAttribPointer(5, 4, VertexAttribPointerType.Int, false, (uint)sizeof(Vertex), (void*)Marshal.OffsetOf<Vertex>("BoneIDs"));
    //         Log.Verbose("[Mesh] Set vertex attrib pointer for BoneIDs");
    //
    //         gl.EnableVertexAttribArray(6);
    //         gl.VertexAttribPointer(6, 4, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), (void*)Marshal.OffsetOf<Vertex>("Weights"));
    //         Log.Verbose("[Mesh] Set vertex attrib pointer for Weights");
    //
    //         gl.BindVertexArray(0);
    //         Log.Verbose("[Mesh] Unbound VAO after attribute setup");
    //     }
    //
    //     program = gl.CreateProgram();
    //     Log.Verbose("[Mesh] Created shader program: {Program}", program);
    //     gl.AttachShader(program, vertexShader.Handle);
    //     gl.AttachShader(program, fragmentShader.Handle);
    //     Log.Verbose("[Mesh] Attached shaders: VS={VS}, FS={FS}", vertexShader.Handle, fragmentShader.Handle);
    //
    //     gl.LinkProgram(program);
    //     Log.Verbose("[Mesh] Linked shader program: {Program}", program);
    //
    //     gl.GetProgram(program, GLEnum.LinkStatus, out var linkStatus);
    //     if (linkStatus != (int)GLEnum.True)
    //     {
    //         var info = gl.GetProgramInfoLog(program);
    //         Log.Error("Failed to link shader program:\n{Info}", info);
    //     }
    //     else
    //     {
    //         Log.Verbose("[Mesh] Shader program linked successfully");
    //     }
    //
    //     gl.DetachShader(program, vertexShader.Handle);
    //     gl.DetachShader(program, fragmentShader.Handle);
    //     Log.Verbose("[Mesh] Detached shaders after linking");
    //
    //     if (graphics.IsRendering)
    //     {
    //         vertexShader.Delete();
    //         fragmentShader.Delete();
    //         Log.Verbose("[Mesh] Deleted shaders after linking");
    //     }
    //
    //     unsafe
    //     {
    //         gl.UseProgram(program);
    //         Log.Verbose("[Mesh] Using shader program: {Program}", program);
    //
    //         int texLoc = gl.GetUniformLocation(program, "uTexture");
    //         if (texLoc != -1)
    //         {
    //             gl.Uniform1(texLoc, 0);
    //             Log.Verbose("[Mesh] Set uTexture uniform at location {Loc}", texLoc);
    //         }
    //         else
    //         {
    //             Log.Verbose("[Mesh] uTexture uniform not found");
    //         }
    //
    //         int modelLoc = gl.GetUniformLocation(program, "model");
    //         if (modelLoc != -1)
    //         {
    //             var local = Transform;
    //             float* ptr = (float*)&local;
    //             gl.UniformMatrix4(modelLoc, 1, false, ptr);
    //             Log.Verbose("[Mesh] Set model matrix uniform at location {Loc}", modelLoc);
    //         }
    //         else
    //         {
    //             Log.Verbose("[Mesh] model uniform not found");
    //         }
    //     }
    // }
}