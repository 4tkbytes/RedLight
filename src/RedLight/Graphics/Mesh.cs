using System.Numerics;
using System.Collections.Generic;
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
    // Add a static set to track problematic meshes
    private static readonly HashSet<string> ProblematicMeshes = new HashSet<string>();
    
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
        this.Name = ""; // Initialize name to avoid compiler error
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
            Log.Error("Failed to link shader program for mesh {MeshName}:\n{Info}", Name, info);
            // Don't use a failed program
            gl.DeleteProgram(program);
            program = 0;
            return this;
        }

        gl.DetachShader(program, vertexShader.Handle);
        gl.DetachShader(program, fragmentShader.Handle);

        unsafe
        {
            UseProgram(program);
            
            // Check and set texture uniforms if they exist
            int texLoc = gl.GetUniformLocation(program, "uTexture");
            if (texLoc != -1)
            {
                gl.Uniform1(texLoc, 0);
            }

            // Check and set diffuse/specular material uniforms if they exist
            int diffuseUniformLoc = gl.GetUniformLocation(program, "material.diffuse");
            if (diffuseUniformLoc != -1)
            {
                gl.Uniform1(diffuseUniformLoc, 0);
            }
            
            int specularUniformLoc = gl.GetUniformLocation(program, "material.specular");
            if (specularUniformLoc != -1)
            {
                gl.Uniform1(specularUniformLoc, 1);
            }

            // Set model matrix uniform if it exists
            int modelLoc = gl.GetUniformLocation(program, "model");
            if (modelLoc != -1)
            {
                var local = Transform;
                float* ptr = (float*)&local;
                gl.UniformMatrix4(modelLoc, 1, false, ptr);
            }
            
            // Verify active attributes against the mesh's VAO setup
            gl.GetProgram(program, ProgramPropertyARB.ActiveAttributes, out int attribCount);
            bool positionFound = false, texCoordFound = false, normalFound = false;
            
            for (uint i = 0; i < attribCount; i++)
            {
                string attribName = gl.GetActiveAttrib(program, i, out _, out _);
                if (attribName == "aPos" || attribName == "position" || attribName == "Position" || attribName == "inPosition")
                    positionFound = true;
                else if (attribName == "aTexCoord" || attribName == "texCoord" || attribName == "TexCoord" || attribName == "inTexCoord")
                    texCoordFound = true;
                else if (attribName == "aNormal" || attribName == "normal" || attribName == "Normal" || attribName == "inNormal")
                    normalFound = true;
            }
            
            if (!positionFound)
                Log.Warning("Shader for mesh {MeshName} does not have a position attribute.", Name);
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
    
    public void Draw(bool debug = false)
    {
        var gl = graphics.OpenGL;

        // Skip problematic meshes to prevent spam
        if (ProblematicMeshes.Contains(Name))
        {
            return;
        }

        // Add debug logging for meshes with no textures
        bool forceDebug = textures.Count == 0;
        if (forceDebug)
        {
            Log.Information($"[MESH DEBUG] Drawing mesh '{Name}' with NO TEXTURES (count: {textures.Count})");
        }

        // Validate shader program exists and is valid
        if (program == 0)
        {
            Log.Error($"[MESH DRAW] Attempted to draw mesh '{Name}' with invalid shader program (0)");
            return;
        }

        // Check if the program is actually a valid OpenGL program
        if (!gl.IsProgram(program))
        {
            Log.Error($"[MESH DRAW] Shader program {program} is not a valid OpenGL program for mesh '{Name}'");
            return;
        }

        // Check program link status
        gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out int linkStatus);
        if (linkStatus == 0)
        {
            Log.Error($"[MESH DRAW] Shader program {program} is not properly linked for mesh '{Name}'");
            string infoLog = gl.GetProgramInfoLog(program);
            Log.Error($"[MESH DRAW] Link error: {infoLog}");
            return;
        }

        // Bind shader program
        gl.UseProgram(program);
        
        // Verify the program was actually bound
        int currentProgram = gl.GetInteger(GetPName.CurrentProgram);
        if (currentProgram != program)
        {
            Log.Error($"[MESH DRAW] Failed to bind program! Expected: {program}, Got: {currentProgram}");
            return;
        }

        if (debug || forceDebug) Log.Debug($"[MESH DRAW] Successfully bound program {program}");
        
        // Debug: Log available uniforms in debug mode
        if (debug || forceDebug)
        {
            gl.GetProgram(program, ProgramPropertyARB.ActiveUniforms, out int uniformCount);
            Log.Debug($"[MESH DRAW] Shader has {uniformCount} active uniforms");
            for (int u = 0; u < uniformCount; u++)
            {
                string uniformName = gl.GetActiveUniform(program, (uint)u, out int size, out UniformType type);
                Log.Debug($"[MESH DRAW] Uniform {u}: {uniformName} (type: {type}, size: {size})");
            }
        }
        
        // Bind textures with proper uniform handling
        bool hasDiffuseTexture = false, hasSpecularTexture = false;
        
        // First, check what uniforms are available in the shader
        int diffuseUniformLoc = gl.GetUniformLocation(program, "material.diffuse");
        int specularUniformLoc = gl.GetUniformLocation(program, "material.specular");
        int basicTextureUniformLoc = gl.GetUniformLocation(program, "uTexture");
        
        if (debug || forceDebug)
        {
            Log.Debug($"[MESH DRAW] Uniform locations - material.diffuse: {diffuseUniformLoc}, material.specular: {specularUniformLoc}, uTexture: {basicTextureUniformLoc}");
        }
        
        // Bind textures to appropriate texture units
        for (int i = 0; i < textures.Count; i++)
        {
            // Validate texture handle before binding
            if (textures[i].Handle == 0 || !gl.IsTexture(textures[i].Handle))
            {
                Log.Warning($"[MESH DRAW] Invalid texture handle {textures[i].Handle} for texture {i} in mesh {Name}. Skipping.");
                continue;
            }
            
            gl.ActiveTexture(TextureUnit.Texture0 + i);
            gl.BindTexture(TextureTarget.Texture2D, textures[i].Handle);

            switch (textures[i].Type)
            {
                case RLTextureType.Diffuse:
                    if (diffuseUniformLoc != -1)
                    {
                        gl.Uniform1(diffuseUniformLoc, i);
                        hasDiffuseTexture = true;
                        if (debug || forceDebug) Log.Debug($"[MESH DRAW] Bound diffuse texture to unit {i}");
                    }
                    else if (basicTextureUniformLoc != -1)
                    {
                        // Fallback for basic shader
                        gl.Uniform1(basicTextureUniformLoc, i);
                        if (debug) Log.Debug($"[MESH DRAW] Bound diffuse texture to basic uTexture unit {i}");
                    }
                    break;
                    
                case RLTextureType.Specular:
                    if (specularUniformLoc != -1)
                    {
                        gl.Uniform1(specularUniformLoc, i);
                        hasSpecularTexture = true;
                        if (debug) Log.Debug($"[MESH DRAW] Bound specular texture to unit {i}");
                    }
                    break;
                    
                default:
                    // For other texture types, try the old naming convention
                    string name = textures[i].Type.ToString().ToLower();
                    string uniformName = $"texture_{name}1";
                    int location = gl.GetUniformLocation(program, uniformName);
                    if (location != -1)
                    {
                        gl.Uniform1(location, i);
                        if (debug) Log.Debug($"[MESH DRAW] Bound {uniformName} texture to unit {i}");
                    }
                    break;
            }
        }
        
        // Set default values for material uniforms if they exist but no textures were bound
        // This prevents InvalidOperation errors from uninitialized samplers
        if (!hasDiffuseTexture && diffuseUniformLoc != -1)
        {
            // Check if we have any textures at all
            if (textures.Count > 0)
            {
                gl.Uniform1(diffuseUniformLoc, 0);
                if (debug) Log.Debug($"[MESH DRAW] Set default material.diffuse to texture unit 0");
            }
            else
            {
                // No textures available - we need to create/bind a default white texture
                // For now, try to get the no-texture fallback from texture manager
                try
                {
                    var textureManager = TextureManager.Instance;
                    var defaultTexture = textureManager?.TryGet("no-texture", false);
                    
                    if (defaultTexture != null)
                    {
                        gl.ActiveTexture(TextureUnit.Texture0);
                        gl.BindTexture(TextureTarget.Texture2D, defaultTexture.Handle);
                        gl.Uniform1(diffuseUniformLoc, 0);
                        if (debug) Log.Debug($"[MESH DRAW] Bound fallback texture to material.diffuse");
                    }
                    else
                    {
                        // Create a simple 1x1 white texture as last resort
                        uint whiteTexture = CreateDefaultWhiteTexture(gl);
                        gl.ActiveTexture(TextureUnit.Texture0);
                        gl.BindTexture(TextureTarget.Texture2D, whiteTexture);
                        gl.Uniform1(diffuseUniformLoc, 0);
                        if (debug) Log.Debug($"[MESH DRAW] Created and bound default white texture to material.diffuse");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[MESH DRAW] Failed to bind default texture for mesh {Name}: {ex.Message}");
                }
            }
        }
        
        if (!hasSpecularTexture && specularUniformLoc != -1)
        {
            // For specular, we can point to the same texture as diffuse if needed
            int textureUnit = textures.Count > 1 ? 1 : 0;
            if (textures.Count > 0)
            {
                gl.Uniform1(specularUniformLoc, textureUnit);
                if (debug) Log.Debug($"[MESH DRAW] Set default material.specular to texture unit {textureUnit}");
            }
            else
            {
                // Point to the same default texture as diffuse (texture unit 0)
                gl.Uniform1(specularUniformLoc, 0);
                if (debug) Log.Debug($"[MESH DRAW] Set default material.specular to texture unit 0 (same as diffuse)");
            }
        }

        unsafe
        {
            if (debug) Log.Debug($"[MESH DRAW] About to bind VAO {vao} for mesh: {Name}");
            
            // Check if VAO is valid
            if (!gl.IsVertexArray(vao))
            {
                Log.Error($"[MESH DRAW] VAO {vao} is not valid for mesh: {Name}");
                return;
            }
            
            gl.BindVertexArray(vao);
            
            // Verify VAO binding
            int currentVao = gl.GetInteger(GetPName.VertexArrayBinding);
            if (currentVao != vao)
            {
                Log.Error($"[MESH DRAW] Failed to bind VAO! Expected: {vao}, Got: {currentVao}");
                return;
            }
            
            // Check buffer bindings
            int elementBuffer = gl.GetInteger(GetPName.ElementArrayBufferBinding);
            if (debug) Log.Debug($"[MESH DRAW] Element buffer bound: {elementBuffer}, IndicesCount: {IndicesCount}");
            
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
            
            // Validate vertex attributes before drawing
            bool hasValidAttributes = false;
            for (int i = 0; i < 8; i++) // Check more attributes
            {
                gl.GetVertexAttrib((uint)i, VertexAttribPropertyARB.VertexAttribArrayEnabled, out int enabled);
                if (enabled == 1)
                {
                    hasValidAttributes = true;
                    gl.GetVertexAttrib((uint)i, VertexAttribPropertyARB.VertexAttribArraySize, out int size);
                    gl.GetVertexAttrib((uint)i, VertexAttribPropertyARB.VertexAttribArrayType, out int type);
                    if (debug) Log.Debug($"[MESH DRAW] Vertex attrib {i}: enabled={enabled}, size={size}, type={type}");
                }
            }
            
            if (!hasValidAttributes)
            {
                Log.Error($"[MESH DRAW] No valid vertex attributes enabled for mesh: {Name}");
                gl.BindVertexArray(0);
                return;
            }
            
            // Check for any OpenGL errors before drawing
            var errorBefore = gl.GetError();
            if (errorBefore != GLEnum.NoError)
            {
                Log.Error($"[MESH DRAW] OpenGL error before DrawElements: {errorBefore}");
                gl.BindVertexArray(0);
                return;
            }
            
            if (debug) Log.Debug($"[MESH DRAW] About to call DrawElements with {IndicesCount} indices");
            
            // Try to catch the specific error that's happening
            try
            {
                gl.DrawElements(PrimitiveType.Triangles, (uint)IndicesCount, DrawElementsType.UnsignedInt, null);
                
                // Check for errors immediately after drawing
                var errorAfter = gl.GetError();
                if (errorAfter != GLEnum.NoError)
                {
                    Log.Error($"[MESH DRAW] OpenGL error after DrawElements: {errorAfter} for mesh: {Name}");
                    
                    // Additional debugging for this specific mesh
                    Log.Error($"[MESH DEBUG] Problematic mesh details:");
                    Log.Error($"[MESH DEBUG] - VAO: {vao}, VBO: {vbo}, EBO: {ebo}");
                    Log.Error($"[MESH DEBUG] - Program: {program}");
                    Log.Error($"[MESH DEBUG] - Vertices: {Vertices?.Count}, Indices: {indices?.Length}");
                    Log.Error($"[MESH DEBUG] - Texture count: {textures.Count}");
                    
                    // Skip this mesh to prevent spam
                    Log.Warning($"[MESH DEBUG] Skipping problematic mesh '{Name}' to prevent error spam");
                    ProblematicMeshes.Add(Name); // Add to problematic mesh list
                    gl.BindVertexArray(0);
                    return;
                }
                else
                {
                    if (debug) Log.Debug($"[MESH DRAW] DrawElements completed successfully");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[MESH DRAW] Exception during DrawElements for mesh '{Name}': {ex.Message}");
                gl.BindVertexArray(0);
                return;
            }
            
            gl.BindVertexArray(0);
        }

        gl.ActiveTexture(TextureUnit.Texture0);
        if (debug) Log.Debug($"[MESH DRAW] Draw completed for mesh: {Name}");
    }
    
    // Add this method to wrap all glUseProgram calls
    public void UseProgram(uint program, bool debug = false)
    {
        if (debug) Log.Debug($"[GL STATE] UseProgram called with: {program} (from: {System.Environment.StackTrace.Split('\n')[1].Trim()})");
        graphics.OpenGL.UseProgram(program);
    
        // Verify it was set
        int current = graphics.OpenGL.GetInteger(GetPName.CurrentProgram);
        if (current != program)
        {
            Log.Error($"[GL STATE] UseProgram failed! Requested: {program}, Got: {current}");
        }
    }

    // Helper method to create a 1x1 white texture as fallback
    private static Dictionary<GL, uint> _defaultWhiteTextures = new Dictionary<GL, uint>();
    
    private uint CreateDefaultWhiteTexture(GL gl)
    {
        // Check if we already have a default texture for this GL context
        if (_defaultWhiteTextures.TryGetValue(gl, out uint existingTexture))
        {
            return existingTexture;
        }
        
        // Create a 1x1 white texture
        uint texture = gl.GenTexture();
        gl.BindTexture(TextureTarget.Texture2D, texture);
        
        // Create white pixel data (RGBA)
        byte[] whitePixel = { 255, 255, 255, 255 };
        unsafe
        {
            fixed (byte* ptr = whitePixel)
            {
                gl.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    InternalFormat.Rgba,
                    1, 1, 0,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    ptr
                );
            }
        }
        
        // Set texture parameters
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        
        gl.BindTexture(TextureTarget.Texture2D, 0);
        
        // Cache it for future use
        _defaultWhiteTextures[gl] = texture;
        
        return texture;
    }

    public int IndicesCount => indices != null ? indices.Length : 0;
}