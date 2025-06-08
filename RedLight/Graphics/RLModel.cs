using System.Numerics;
using RedLight.Utils;
using Silk.NET.Assimp;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Serilog;

namespace RedLight.Graphics;

public class RLModel
{
    private Assimp _assimp;
    private RLGraphics graphics;
    private GL _gl;
    private List<RLTexture> _texturesLoaded = new();
    private TextureManager textureManager;
    public string Directory { get; protected set; } = string.Empty;
    public List<Mesh> Meshes { get; protected set; } = new();
    public String Name { get; private set; }
    private bool shaderAttached;

    public RLModel(RLGraphics graphics, string path, TextureManager textureManager, string name)
    {
        var assimp = Assimp.GetApi();
        Name = name;
        _assimp = assimp;
        this.graphics = graphics;
        _gl = graphics.OpenGL;
        this.textureManager = textureManager;

        LoadModel(path);
        if (Meshes.Count == 0)
        {
            Log.Error("No meshes loaded from model! Check the model file and loader.");
            throw new Exception("No meshes loaded from model!");
        }
        Log.Debug($"Loaded {Meshes.Count} mesh(es) from model.");
    }

    public RLModel(RLGraphics graphics, string path, TextureManager textureManager)
    : this(graphics, path, textureManager, "")
    { }

    /// <summary>
    /// Makes it transformable. Once it is transformable, you can edit the position, rotation
    /// and scale. 
    /// </summary>
    /// <returns></returns>
    public Transformable<RLModel> MakeTransformable()
    {
        return new Transformable<RLModel>(this);
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

    public void Draw()
    {
        if (!shaderAttached)
            Log.Error("No shader found for mesh [{A}]. Did you forget to attach it?", Name);
        foreach (var mesh in Meshes)
        {
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
    /// This function is used in the case that a texture is not being rendered properly from a model.
    ///
    /// The mesh name will be logged. You can take that and apply the texture override to a specific mesh. 
    /// </summary>
    /// <param name="meshName">string</param>
    /// <param name="texture">RLTexture</param>
    /// <returns>RLModel</returns>
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
    /// Attach a texture to an RLModel. It iterates through each mesh and attaches the shader to each one. 
    /// </summary>
    /// <param name="shaderBundle">RLShaderBundle</param>
    /// <returns>RLModel</returns>
    public RLModel AttachShader(RLShaderBundle shaderBundle)
    {
        foreach (var mesh in Meshes)
        {
            mesh.AttachShader(shaderBundle.vertexShader, shaderBundle.fragmentShader);
        }

        shaderAttached = true;
        return this;
    }

    /// <summary>
    /// Attaches a texture to a model. Can change it so it is silent (logging). 
    /// </summary>
    /// <param name="texture">RLTexture</param>
    /// <param name="silent">bool</param>
    /// <returns>RLModel</returns>
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
    /// Attaches a texture to the RLModel model. By default, it is not silent, so there will be logging. 
    /// </summary>
    /// <param name="texture">RLTexture</param>
    /// <returns>RLModel</returns>
    public RLModel AttachTexture(RLTexture texture)
    {
        return AttachTexture(texture, false);
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
            Vector3D<float> vector = new();
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
                Vector2D<float> vec = new();
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
                vertex.TexCoords = new Vector2D<float>(0.0f, 0.0f);

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
            Log.Warning($"No texture found for mesh '{mesh->MName}' in model '{Name}'. Mesh will render without texture.");
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
        List<RLTexture> textures = new List<RLTexture>();
        for (uint i = 0; i < textureCount; i++)
        {
            AssimpString path;
            _assimp.GetMaterialTexture(mat, type, i, &path, null, null, null, null, null, null);
            var textureFile = path.ToString();

            // Skip invalid or empty texture names
            if (string.IsNullOrWhiteSpace(textureFile) ||
                textureFile == "?" ||
                textureFile.IndexOfAny(Path.GetInvalidFileNameChars().Where(c => c != '*').ToArray()) >= 0)
            {
                Log.Warning("Skipping invalid texture file name: {TextureFile}", textureFile);
                continue;
            }

            bool skip = false;
            for (int j = 0; j < _texturesLoaded.Count; j++)
            {
                if (_texturesLoaded[j].Path == path)
                {
                    textures.Add(_texturesLoaded[j]);
                    skip = true;
                    break;
                }
            }

            if (!skip)
            {
                RLTexture texture = null;

                // Handle embedded textures (with "*" in the path)
                if (textureFile.Contains("*"))
                {
                    // Parse the texture index from the path (e.g., "*1" -> 1)
                    if (int.TryParse(textureFile.Substring(1), out int texIndex))
                    {
                        Log.Debug("Loading embedded texture with index {Index}", texIndex);

                        // Look for the texture in the texture manager first with a standard naming convention
                        string embeddedTexName = $"embedded_{Name}_{texIndex}";

                        if (textureManager.Exists(embeddedTexName))
                        {
                            // Use existing texture if already loaded
                            texture = textureManager.Get(embeddedTexName);
                            Log.Debug("Using existing embedded texture: {Name}", embeddedTexName);
                        }
                        else
                        {
                            // Extract the embedded texture from the scene
                            try
                            {
                                // Verify the scene and texture index are valid
                                if (scene != null && texIndex >= 0 && texIndex < scene->MNumTextures)
                                {
                                    var embTexture = scene->MTextures[texIndex];
                                    if (embTexture != null)
                                    {
                                        // Create a texture ID for the texture manager
                                        string textureId = embeddedTexName;
                                        
                                        // Get dimensions
                                        int width = (int)embTexture->MWidth;
                                        int height = embTexture->MHeight > 0 ? (int)embTexture->MHeight : 1; // Handle 1D textures
                                        
                                        // Create texture directly from the embedded Texel data
                                        texture = new RLTexture(graphics, embTexture, rlType);
                                        
                                        // Add to texture manager for future reference
                                        textureManager.Add(textureId, texture);
                                        Log.Information("Created embedded texture: {TextureId} ({Width}x{Height})", textureId, width, height);
                                    }
                                    else
                                    {
                                        Log.Warning("Embedded texture at index {Index} is invalid", texIndex);
                                        texture = textureManager.Get("no-texture");
                                    }
                                }
                                else
                                {
                                    Log.Warning("Embedded texture index {Index} is out of range (max: {Max})",
                                        texIndex, scene != null ? scene->MNumTextures - 1 : -1);
                                    texture = textureManager.Get("no-texture");
                                }
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
                        Log.Warning("Invalid embedded texture format: {Path}", textureFile);
                        texture = textureManager.Get("no-texture");
                    }
                }
                else
                {
                    // Handle regular file-based textures
                    string filename = Path.GetFileName(textureFile);
                    string fullPath = Path.Combine(Directory, filename);

                    try
                    {
                        if (System.IO.File.Exists(fullPath))
                        {
                            texture = new RLTexture(graphics, fullPath, rlType);
                        }
                        else
                        {
                            Log.Warning("Texture file not found: {Path}", fullPath);
                            texture = textureManager.Get("no-texture");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Failed to load texture: {Path} - {Error}", fullPath, ex.Message);
                        texture = textureManager.Get("no-texture");
                    }
                }

                if (texture != null)
                {
                    texture.Path = textureFile;
                    texture.Type = rlType;
                    textures.Add(texture);
                    _texturesLoaded.Add(texture);
                }
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

    // public void Dispose()
    // {
    //     foreach (var mesh in Meshes)
    //     {
    //         mesh.Dispose();
    //     }
    //
    //     _texturesLoaded = null;
    // }
}