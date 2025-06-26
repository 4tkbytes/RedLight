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
    public Vector3 Position;
    public Vector3 Normal;
    public Vector2 TexCoords;
    public Vector3 Tangent;
    public Vector3 BitTangent;
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
    public List<Vertex> Vertices { get; private set; }
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
        this.Vertices = vertices;
        var gl = graphics.OpenGL;
        vao = gl.GenVertexArray();
        vbo = gl.GenBuffer();
        ebo = gl.GenBuffer();
        gl.BindVertexArray(vao);

        // Flatten vertices to float array: position (3) + texcoords (2)
        float[] flatVerts = new float[vertices.Count * 8];
        for (int i = 0; i < vertices.Count; i++)
        {
            flatVerts[i * 8 + 0] = vertices[i].Position.X;
            flatVerts[i * 8 + 1] = vertices[i].Position.Y;
            flatVerts[i * 8 + 2] = vertices[i].Position.Z;
            flatVerts[i * 8 + 3] = vertices[i].TexCoords.X;
            flatVerts[i * 8 + 4] = vertices[i].TexCoords.Y;
            flatVerts[i * 8 + 5] = vertices[i].Normal.X;
            flatVerts[i * 8 + 6] = vertices[i].Normal.Y;
            flatVerts[i * 8 + 7] = vertices[i].Normal.Z;
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
            gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), (void*)0);
            // Attribute 1: texcoords (vec2)
            gl.EnableVertexAttribArray(1);
            gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), (void*)(3 * sizeof(float)));
            // Attribute 2: normals (vec3)
            gl.EnableVertexAttribArray(2);
            gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), (void*)(5 * sizeof(float)));
        }
        gl.BindVertexArray(0);
        textures = new List<RLTexture>();
        
        Log.Debug($"[MESH SETUP] Creating VAO for mesh: {Name}");
        Log.Debug($"[MESH SETUP] Vertex count: {vertices?.Count}, Index count: {indices?.Length}");
        Log.Debug($"[MESH SETUP] VAO: {vao}, VBO: {vbo}, EBO: {ebo}");
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
            UseProgram(program);
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

        if (program == 0)
        {
            Log.Error($"[MESH DRAW] Program is 0 for mesh: {Name}");
            return;
        }

        gl.UseProgram(program);
        Log.Debug($"[MESH DRAW] Successfully bound program {program}");
        
        if (!gl.IsProgram(program))
        {
            Log.Error($"Mesh '{Name}' has invalid shader program {program}");
            program = 0;
            return;
        }
        
        // Check program link status
        gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out int linkStatus);
        if (linkStatus == 0)
        {
            Log.Error($"[MESH DRAW] Program {program} is not linked for mesh: {Name}");
            return;
        }

        UseProgram(program);
    
        // Verify it was actually set
        int currentProgram = gl.GetInteger(GetPName.CurrentProgram);
        if (currentProgram != program)
        {
            Log.Error($"[MESH DRAW] Failed to bind program! Expected: {program}, Got: {currentProgram}");
            return;
        }

        Log.Debug($"[MESH DRAW] Successfully bound program {program}");
        
        uint diffuseNr = 1, specularNr = 1, normalNr = 1, heightNr = 1;
        for (int i = 0; i < textures.Count; i++)
        {
            gl.ActiveTexture(TextureUnit.Texture0 + i); // Use different texture unit for each

            string number = "1";
            string name = textures[i].Type.ToString().ToLower();
            switch (textures[i].Type)
            {
                case RLTextureType.Diffuse:  number = (diffuseNr++).ToString(); break;
                case RLTextureType.Specular: number = (specularNr++).ToString(); break;
                case RLTextureType.Normal:   number = (normalNr++).ToString(); break;
                case RLTextureType.Height:   number = (heightNr++).ToString(); break;
            }

            string uniformName = $"texture_{name}{number}";
            int location = gl.GetUniformLocation(program, uniformName);
            if (location != -1)
                gl.Uniform1(location, i);

            gl.BindTexture(TextureTarget.Texture2D, textures[i].Handle);
        }

        unsafe
        {
            // Debug VAO state before binding
            Log.Debug($"[MESH DRAW] About to bind VAO {vao} for mesh: {Name}");
            
            // Check if VAO is valid
            if (!gl.IsVertexArray(vao))
            {
                Log.Error($"[MESH DRAW] VAO {vao} is not valid for mesh: {Name}");
                return;
            }
            
            gl.BindVertexArray(vao);
            
            // Verify VAO was bound
            int currentVao = gl.GetInteger(GetPName.VertexArrayBinding);
            if (currentVao != vao)
            {
                Log.Error($"[MESH DRAW] Failed to bind VAO! Expected: {vao}, Got: {currentVao}");
                return;
            }
            
            // Check buffer bindings
            int elementBuffer = gl.GetInteger(GetPName.ElementArrayBufferBinding);
            Log.Debug($"[MESH DRAW] Element buffer bound: {elementBuffer}, IndicesCount: {IndicesCount}");
            
            if (elementBuffer == 0)
            {
                Log.Error($"[MESH DRAW] No element array buffer bound for mesh: {Name}");
                gl.BindVertexArray(0);
                return;
            }
            
            if (IndicesCount <= 0)
            {
                Log.Error($"[MESH DRAW] Invalid indices count: {IndicesCount} for mesh: {Name}");
                gl.BindVertexArray(0);
                return;
            }
            
            // Check vertex attributes
            for (int i = 0; i < 3; i++) // Check first few vertex attributes
            {
                gl.GetVertexAttrib((uint)i, VertexAttribPropertyARB.VertexAttribArrayEnabled, out int enabled);
                if (enabled == 1)
                {
                    gl.GetVertexAttrib((uint)i, VertexAttribPropertyARB.VertexAttribArraySize, out int size);
                    gl.GetVertexAttrib((uint)i, VertexAttribPropertyARB.VertexAttribArrayType, out int type);
                    Log.Debug($"[MESH DRAW] Vertex attrib {i}: enabled, size={size}, type={type}");
                }
            }
            
            Log.Debug($"[MESH DRAW] About to call DrawElements with {IndicesCount} indices");
            
            gl.DrawElements(PrimitiveType.Triangles, (uint)IndicesCount, DrawElementsType.UnsignedInt, null);
            
            Log.Debug($"[MESH DRAW] DrawElements completed");
            gl.BindVertexArray(0);
        }

        Log.Debug($"[MESH DRAW] Draw completed for mesh: {Name}");
    }
    
    // Add this method to wrap all glUseProgram calls
    public void UseProgram(uint program)
    {
        Log.Debug($"[GL STATE] UseProgram called with: {program} (from: {System.Environment.StackTrace.Split('\n')[1].Trim()})");
        graphics.OpenGL.UseProgram(program);
    
        // Verify it was set
        int current = graphics.OpenGL.GetInteger(GetPName.CurrentProgram);
        if (current != program)
        {
            Log.Error($"[GL STATE] UseProgram failed! Requested: {program}, Got: {current}");
        }
    }

    public int IndicesCount => indices != null ? indices.Length : 0;
}