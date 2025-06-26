using System.Drawing;
using System.Numerics;
using RedLight.Utils;
using Silk.NET.Assimp;
using Silk.NET.OpenGL;
using Serilog;

namespace RedLight.Graphics;

/// <summary>
/// Represents a 3D model loaded from a file using Assimp.
/// Handles loading, processing, and rendering of 3D models with materials and textures.
/// </summary>
public class RLModel
{
    private Assimp _assimp;
    private RLGraphics graphics;
    private GL _gl;
    private List<RLTexture> _texturesLoaded = new();
    private TextureManager textureManager;

    public RLShaderBundle AttachedShader { get; private set; }

    /// <summary>
    /// Gets the directory path where the model file is located.
    /// </summary>
    public string Directory { get; protected set; } = string.Empty;

    /// <summary>
    /// Gets the resource path used to load the model.
    /// </summary>
    public string ResourcePath { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the collection of meshes that make up this model.
    /// </summary>
    public List<Mesh> Meshes { get; protected set; } = new();

    /// <summary>
    /// Gets the name identifier of this model.
    /// </summary>
    public String Name { get; private set; }
    private bool shaderAttached;
    private Color _modelTint = Color.White;

    /// <summary>
    /// Initialises a new instance of the RLModel class with a specified name.
    /// </summary>
    /// <param name="graphics">The graphics context to use for rendering.</param>
    /// <param name="path">The resource path to the model file.</param>
    /// <param name="textureManager">The texture manager to use for loading textures.</param>
    /// <param name="name">The name to assign to this model.</param>
    public RLModel(RLGraphics graphics, string path, TextureManager textureManager, string name)
    {
        var assimp = Assimp.GetApi();
        Name = name;
        _assimp = assimp;
        this.graphics = graphics;
        _gl = graphics.OpenGL;
        this.textureManager = textureManager;

        ResourcePath = path;
        path = RLFiles.GetResourcePath(path);

        LoadModel(path);
        if (Meshes.Count == 0)
        {
            Log.Error("No meshes loaded from model! Check the model file and loader.");
            throw new Exception("No meshes loaded from model!");
        }
        Log.Debug($"Loaded {Meshes.Count} mesh(es) from model.");
    }

    public RLModel(RLGraphics graphics, string path, string name) : this(graphics, path, TextureManager.Instance, name)
    {
    }

    /// <summary>
    /// Initializes a new instance of the RLModel class.
    /// Uses an empty string as the model name.
    /// </summary>
    /// <param name="graphics">The graphics context to use for rendering.</param>
    /// <param name="path">The resource path to the model file.</param>
    /// <param name="textureManager">The texture manager to use for loading textures.</param>
    public RLModel(RLGraphics graphics, string path, TextureManager textureManager)
    : this(graphics, path, textureManager, "")
    { }

    /// <summary>
    /// Makes the model transformable, allowing manipulation of position, rotation, and scale.
    /// </summary>
    /// <returns>A transformable wrapper around this model.</returns>
    public Transformable<RLModel> MakeTransformable()
    {
        return new ConcreteTransformable<RLModel>(this);
    }

    private unsafe void LoadModel(string path)
    {
        var scene = _assimp.ImportFile(path, (uint)PostProcessSteps.Triangulate);

        if (scene == null || scene->MFlags == Silk.NET.Assimp.Assimp.SceneFlagsIncomplete || scene->MRootNode == null)
        {
            var error = _assimp.GetErrorStringS();
            throw new Exception(error);
        }

        Directory = Path.GetDirectoryName(RLFiles.GetResourcePath(path)) ?? string.Empty;

        ProcessNode(scene->MRootNode, scene);
    }

    /// <summary>
    /// Renders the model by drawing all of its meshes.
    /// Requires a shader to be attached before calling.
    /// </summary>
    public void Draw()
    {
        if (!shaderAttached)
            Log.Error("No shader found for mesh [{A}]. Did you forget to attach it?", Name);

        if (_modelTint != Color.White)
        {
            Vector4 normalizedColor = new Vector4(
                _modelTint.R / 255.0f, _modelTint.G / 255.0f,
                _modelTint.B / 255.0f, _modelTint.A / 255.0f
            );

            foreach (var mesh in Meshes)
            {
                if (mesh.program != 0)
                {
                    if (!_gl.IsProgram(mesh.program))
                    {
                        Console.WriteLine($"[GL ERROR] Invalid Shader Program ID: {mesh.program}");
                    }
                    _gl.UseProgram(mesh.program);
                    int colorLocation = _gl.GetUniformLocation(mesh.program, "uModelColor");
                    if (colorLocation != -1)
                    {
                        _gl.Uniform4(colorLocation, normalizedColor);
                    }
                }
            }
        }

        foreach (var mesh in Meshes)
        {
            if (mesh.program != 0)
                _gl.UseProgram(mesh.program);
            else if (shaderAttached)
            {
                Log.Warning($"Mesh '{mesh.Name}' in model '{Name}' has no shader program, but model's shaderAttached is true. Skipping draw for this mesh.");
                continue;
            }

            mesh.Draw();
        }
    }

    private unsafe void ProcessNode(Node* node, Silk.NET.Assimp.Scene* scene)
    {
        if (!graphics.ShutUp)
            Log.Debug($"ProcessNode: node has {node->MNumMeshes} meshes, {node->MNumChildren} children");
        for (var i = 0; i < node->MNumMeshes; i++)
        {
            var mesh = scene->MMeshes[node->MMeshes[i]];
            if (!graphics.ShutUp)
                Log.Debug($"  Mesh {i}: {mesh->MNumVertices} vertices, {mesh->MNumFaces} faces");
            Meshes.Add(ProcessMesh(mesh, scene));
        }

        for (var i = 0; i < node->MNumChildren; i++)
        {
            ProcessNode(node->MChildren[i], scene);
        }
    }

    /// <summary>
    /// Applies a texture override to a specific mesh in the model.
    /// Useful when a texture is not rendering properly from the original model.
    /// </summary>
    /// <param name="meshName">The name or index of the mesh to apply the texture to.</param>
    /// <param name="texture">The texture to apply.</param>
    /// <returns>This model instance for method chaining.</returns>
    public RLModel ApplyTextureOverride(string meshName, RLTexture texture)
    {
        if (textureManager == null)
        {
            Log.Error("Cannot apply texture: TextureManager is null");
            return this;
        }

        // Try to find mesh by index first
        if (int.TryParse(meshName, out int meshIndex) && meshIndex >= 0 && meshIndex < Meshes.Count)
        {
            if (texture != null)
            {
                Meshes[meshIndex].AttachTexture(texture);
                Log.Debug("Applied texture for mesh index [{Index}] from texture manager: {TextureName}",
                    meshIndex, texture.Name);
            }
            else
            {
                Log.Warning("Cannot apply texture: Texture is null");
            }
            return this;
        }

        // Then try to find mesh by name
        for (int i = 0; i < Meshes.Count; i++)
        {
            if (Meshes[i].Name == meshName)
            {
                if (texture != null)
                {
                    Meshes[i].AttachTexture(texture);
                    Log.Debug("Applied texture for mesh [{MeshName}] from texture manager: {TextureName}",
                        meshName, texture.Name);
                }
                else
                {
                    Log.Warning("Cannot apply texture: Texture is null");
                }
                return this;
            }
        }

        Log.Warning("Cannot apply texture: Mesh [{MeshName}] not found", meshName);
        return this;
    }

    /// <summary>
    /// Attaches a shader to all meshes in the model.
    /// Required before rendering the model.
    /// </summary>
    /// <param name="shaderBundle">The shader bundle to attach.</param>
    /// <returns>This model instance for method chaining.</returns>
    public RLModel AttachShader(RLShaderBundle shaderBundle)
    {
        foreach (var mesh in Meshes)
        {
            mesh.AttachShader(shaderBundle.VertexShader, shaderBundle.FragmentShader);
        }

        AttachedShader = shaderBundle;
        shaderAttached = true;
        return this;
    }

    /// <summary>
    /// Attaches a texture to all meshes in the model.
    /// </summary>
    /// <param name="texture">The texture to attach.</param>
    /// <param name="silent">Whether to suppress logging.</param>
    /// <returns>This model instance for method chaining.</returns>
    public RLModel AttachTexture(RLTexture texture, bool silent)
    {
        if (texture == null)
        {
            if (!silent)
                Log.Warning("RLModel.AttachTexture: Provided texture is null. Using fallback 'no-texture'.");
            if (textureManager != null)
            {
                if (textureManager.TryGet("no-texture", true) == null)
                    textureManager.Add("no-texture", new RLTexture(graphics, RLFiles.GetResourcePath(RLConstants.RL_NO_TEXTURE_PATH), RLTextureType.Diffuse));

                texture = textureManager.Get("no-texture");
            }

            if (texture == null)
            {
                Log.Error("RLModel.AttachTexture: 'no-texture' fallback texture is missing! Model will render without texture.");
                return this;
            }
        }
        foreach (var mesh in Meshes)
        {
            mesh.AttachTexture(texture);
        }
        return this;
    }

    /// <summary>
    /// Attaches a texture to all meshes in the model with logging enabled.
    /// </summary>
    /// <param name="texture">The texture to attach.</param>
    /// <returns>This model instance for method chaining.</returns>
    public RLModel AttachTexture(RLTexture texture)
    {
        return AttachTexture(texture, false);
    }

    public RLModel SetColour(Color colour)
    {
        _modelTint = colour;

        if (!shaderAttached)
        {
            Log.Warning($"Cannot apply color to model '{Name}': No shader is attached. Call AttachShader() first and then SetColour() again.");
            // Store the color, it will be applied if AttachShader is called later and it re-calls SetColour.
            return this;
        }

        // Convert System.Drawing.Color to a Vector4 (normalized RGBA)
        Vector4 normalizedColor = new Vector4(
            colour.R / 255.0f,
            colour.G / 255.0f,
            colour.B / 255.0f,
            colour.A / 255.0f
        );

        foreach (var mesh in Meshes)
        {
            if (mesh.program == 0)
            {
                Log.Warning($"Mesh '{mesh.Name}' in model '{Name}' does not have a shader program. Cannot set color for this mesh.");
                continue;
            }

            _gl.UseProgram(mesh.program); // Activate the mesh's shader program

            // The shader should have a uniform like "vec4 uModelColor;"
            int colorLocation = _gl.GetUniformLocation(mesh.program, "uModelColor");

            if (colorLocation != -1)
            {
                _gl.Uniform4(colorLocation, normalizedColor.X, normalizedColor.Y, normalizedColor.Z, normalizedColor.W);
                if (!graphics.ShutUp)
                    Log.Verbose($"Set color for mesh '{mesh.Name}' in model '{Name}' to R:{normalizedColor.X} G:{normalizedColor.Y} B:{normalizedColor.Z} A:{normalizedColor.W}");
            }
            else
            {
                // Log a warning if the uniform is not found.
                // This might happen frequently if shaders don't support uModelColor, so consider logging level or frequency.
                Log.Warning($"Uniform 'uModelColor' not found in shader for mesh '{mesh.Name}' (program ID {mesh.program}) in model '{Name}'. Color will not be applied to this mesh.");
            }
        }
        // It's generally good practice to unbind the program if you're done with it,
        // but Draw() will bind programs as needed.
        // _gl.UseProgram(0); 
        return this;
    }

    private unsafe Mesh ProcessMesh(Silk.NET.Assimp.Mesh* mesh, Silk.NET.Assimp.Scene* scene)
    {
        if (!graphics.ShutUp)
            Log.Debug($"ProcessMesh: {mesh->MNumVertices} vertices, {mesh->MNumFaces} faces");
        List<Vertex> vertices = new List<Vertex>();
        List<uint> indices = new List<uint>();
        List<RLTexture> textures = new List<RLTexture>();

        for (uint i = 0; i < mesh->MNumVertices; i++)
        {
            Vertex vertex = new Vertex();
            vertex.BoneIDs = new int[RLConstants.MAX_BONE_INFLUENCE];
            vertex.Weights = new float[RLConstants.MAX_BONE_INFLUENCE];
            Vector3 vector = new();
            vector.X = mesh->MVertices[i].X;
            vector.Y = mesh->MVertices[i].Y;
            vector.Z = mesh->MVertices[i].Z;
            vertex.Position = vector;
            // normals
            if (mesh->MNormals != null)
            {
                vector.X = mesh->MNormals[i].X;
                vector.Y = mesh->MNormals[i].Y;
                vector.Z = mesh->MNormals[i].Z;
                vertex.Normal = vector;
            }
            // texture coordinates
            if (mesh->MTextureCoords[0] != null)
            {
                Vector2 vec = new();
                vec.X = mesh->MTextureCoords[0][i].X;
                // lol glb texture fix was just flipping the v coord
                vec.Y = 1.0f - mesh->MTextureCoords[0][i].Y;
                vertex.TexCoords = vec;
                // tangent
                if (mesh->MTangents != null)
                {
                    vector.X = mesh->MTangents[i].X;
                    vector.Y = mesh->MTangents[i].Y;
                    vector.Z = mesh->MTangents[i].Z;
                    vertex.Tangent = vector;
                }
                // bitangent
                if (mesh->MBitangents != null)
                {
                    vector.X = mesh->MBitangents[i].X;
                    vector.Y = mesh->MBitangents[i].Y;
                    vector.Z = mesh->MBitangents[i].Z;
                    vertex.BitTangent = vector;
                }
            }
            else
                vertex.TexCoords = new Vector2(0.0f, 0.0f);

            vertices.Add(vertex);
        }
        for (uint i = 0; i < mesh->MNumFaces; i++)
        {
            Face face = mesh->MFaces[i];
            for (uint j = 0; j < face.MNumIndices; j++)
                indices.Add(face.MIndices[j]);
        }
        // process materials
        Material* material = scene->MMaterials[mesh->MMaterialIndex];

        // load the textures
        textures.AddRange(LoadMaterialTextures(material, TextureType.Diffuse, RLTextureType.Diffuse, scene));
        textures.AddRange(LoadMaterialTextures(material, TextureType.Metalness, RLTextureType.Metallic, scene));
        textures.AddRange(LoadMaterialTextures(material, TextureType.DiffuseRoughness, RLTextureType.Roughness, scene));
        textures.AddRange(LoadMaterialTextures(material, TextureType.Specular, RLTextureType.Specular, scene));
        textures.AddRange(LoadMaterialTextures(material, TextureType.Normals, RLTextureType.Normal, scene));
        textures.AddRange(LoadMaterialTextures(material, TextureType.Height, RLTextureType.Normal, scene));
        textures.AddRange(LoadMaterialTextures(material, TextureType.Ambient, RLTextureType.Height, scene));

        if (textures.Count == 0)
        {
            Log.Debug($"[WARNING] No texture found for mesh '{mesh->MName}' in model '{Name}'. Mesh will render without texture.");
        }

        if (textures.Count <= 1)
        {
            if (!graphics.ShutUp)
                Log.Debug($"Assigned {textures.Count} textures to mesh '{mesh->MName}' in model '{Name}'.");
        }

        var meshObj = new Mesh(graphics, vertices, BuildIndices(indices)).AttachTexture(textures);
        unsafe
        {
            meshObj.Name = mesh->MName.ToString();
        }
        return meshObj;
    }

    private unsafe List<RLTexture> LoadMaterialTextures(Material* mat, TextureType type, RLTextureType rlType, Silk.NET.Assimp.Scene* scene)
    {
        var textureCount = _assimp.GetMaterialTextureCount(mat, type);
        Log.Verbose("Loading {Count} textures of type {Type} for material.", textureCount, type);
        List<RLTexture> textures = new List<RLTexture>();

        for (uint i = 0; i < textureCount; i++)
        {
            AssimpString path;
            _assimp.GetMaterialTexture(mat, type, i, &path, null, null, null, null, null, null);
            var textureFile = path.ToString();
            Log.Verbose("Processing texture index {Index}: {TextureFile}", i, textureFile);

            // Skip if already loaded
            var loaded = _texturesLoaded.FirstOrDefault(t => t.Path == textureFile);
            if (loaded != null)
            {
                Log.Verbose("Texture already loaded: {TextureFile}", textureFile);
                textures.Add(loaded);
                continue;
            }

            // Skip if empty or just '?'
            if (string.IsNullOrWhiteSpace(textureFile) || textureFile == "?")
            {
                Log.Warning("Skipping empty or placeholder texture: {TextureFile}", textureFile);
                continue;
            }

            // If path contains invalid characters (except '*'), attempt fallback with filename
            if (textureFile.IndexOfAny(Path.GetInvalidFileNameChars().Where(c => c != '*').ToArray()) >= 0)
            {
                Log.Warning("Texture path contains invalid characters, attempting fallback search: {TextureFile}", textureFile);
                // Fallback search logic will run below
            }

            RLTexture texture = null;

            // Embedded texture
            if (textureFile.Contains("*"))
            {
                Log.Verbose("Detected embedded texture: {TextureFile}", textureFile);
                if (int.TryParse(textureFile.Substring(1), out int texIndex) && scene != null && texIndex >= 0 && texIndex < scene->MNumTextures)
                {
                    string embeddedTexName = $"embedded_{Name}_{texIndex}";
                    if (textureManager.Exists(embeddedTexName))
                    {
                        Log.Verbose("Embedded texture already exists in manager: {Name}", embeddedTexName);
                        texture = textureManager.Get(embeddedTexName);
                    }
                    else
                    {
                        try
                        {
                            var embTexture = scene->MTextures[texIndex];
                            texture = new RLTexture(graphics, embTexture, rlType);
                            textureManager.Add(embeddedTexName, texture);
                            Log.Verbose("Created and registered new embedded texture: {Name}", embeddedTexName);
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Failed to create embedded texture: {Error}", ex.Message);
                            texture = textureManager.Get("no-texture");
                        }
                    }
                }
                else
                {
                    Log.Warning("Invalid embedded texture format or index: {Path}", textureFile);
                    texture = textureManager.Get("no-texture");
                }
            }
            else
            {
                // Regular file-based texture
                string filename = Path.GetFileName(textureFile);
                string fullPath = Path.Combine(Directory, filename);
                Log.Verbose("Looking for texture file at: {FullPath}", fullPath);

                if (System.IO.File.Exists(fullPath))
                {
                    try
                    {
                        Log.Verbose("Texture file found at expected location: {FullPath}", fullPath);
                        texture = new RLTexture(graphics, fullPath, rlType);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Failed to load texture: {Path} - {Error}", fullPath, ex.Message);
                        texture = textureManager.Get("no-texture");
                    }
                }
                else
                {
                    Log.Verbose("Texture file not found at expected location. Searching recursively for: {Filename}", filename);
                    // Fallback: recursively search for the file in the models directory
                    string foundPath = System.IO.Directory
                        .EnumerateFiles(Directory, filename, SearchOption.AllDirectories)
                        .FirstOrDefault();

                    if (!string.IsNullOrEmpty(foundPath))
                    {
                        try
                        {
                            Log.Verbose("Found fallback texture at: {FoundPath}", foundPath);
                            texture = new RLTexture(graphics, foundPath, rlType);
                            Log.Information("Found and loaded fallback texture: {Path}", foundPath);
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Failed to load found fallback texture: {Path} - {Error}", foundPath, ex.Message);
                            texture = textureManager.Get("no-texture");
                        }
                    }
                    else
                    {
                        Log.Warning("Texture file not found: {Path}", fullPath);
                        texture = textureManager.Get("no-texture");
                    }
                }
            }

            if (texture != null)
            {
                Log.Verbose("Registering texture: {TextureFile}", textureFile);
                texture.Path = textureFile;
                texture.Type = rlType;
                textures.Add(texture);
                _texturesLoaded.Add(texture);
            }
        }
        return textures;
    }

    private float[] BuildVertices(List<Vertex> vertexCollection)
    {
        var vertices = new List<float>();

        foreach (var vertex in vertexCollection)
        {
            vertices.Add(vertex.Position.X);
            vertices.Add(vertex.Position.Y);
            vertices.Add(vertex.Position.Z);
            vertices.Add(vertex.TexCoords.X);
            vertices.Add(vertex.TexCoords.Y);
        }

        return vertices.ToArray();
    }

    private uint[] BuildIndices(List<uint> indices)
    {
        return indices.ToArray();
    }
}