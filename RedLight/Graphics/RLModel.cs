using System.Numerics;
using RedLight.Utils;
using Silk.NET.Assimp;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Serilog;

namespace RedLight.Graphics;

public class RLModel
{
    List<Mesh> meshes = new List<Mesh>();
    private Assimp _assimp;
    private RLGraphics graphics;
    private GL _gl;
    private List<RLTexture> _texturesLoaded = new();
    private TextureManager textureManager;
    public string Directory { get; protected set; } = string.Empty;
    public List<Mesh> Meshes { get; protected set; } = new List<Mesh>();
    public String Name { get; private set; }
    private bool shaderAttached = false;

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

        Directory = RLFiles.GetParentFolder(path);

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

    public RLModel AttachShader(RLShaderBundle shaderBundle)
    {
        foreach (var mesh in Meshes)
        {
            mesh.AttachShader(shaderBundle.vertexShader, shaderBundle.fragmentShader);
        }
        
        shaderAttached = true;
        return this;
    }

    public RLModel AttachTexture(RLTexture texture, bool silent)
    {
        if (texture == null)
        {
            if (!silent)
                Log.Warning("RLModel.AttachTexture: Provided texture is null. Using fallback 'no-texture'.");
            if (textureManager != null)
            {
                if (textureManager.TryGet("no-texture", true) == null)
                    textureManager.Add("no-texture", new RLTexture(graphics, RLFiles.GetEmbeddedResourcePath(RLConstants.RL_NO_TEXTURE_PATH), RLTextureType.Diffuse));

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

    private RLModel AttachTextureFirstTime(RLTexture texture)
    {
        return AttachTexture(texture, true);
    }

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
                vec.Y = mesh->MTextureCoords[0][i].Y;
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
        // now walk through each of the mesh's faces (a face is a mesh its triangle) and retrieve the corresponding vertex indices.
        for (uint i = 0; i < mesh->MNumFaces; i++)
        {
            Face face = mesh->MFaces[i];
            for (uint j = 0; j < face.MNumIndices; j++)
                indices.Add(face.MIndices[j]);
        }
        // process materials
        Material* material = scene->MMaterials[mesh->MMaterialIndex];

        // Load all relevant textures for this material
        textures.AddRange(LoadMaterialTextures(material, TextureType.Diffuse, RLTextureType.Diffuse));
        textures.AddRange(LoadMaterialTextures(material, TextureType.Metalness, RLTextureType.Metallic));
        textures.AddRange(LoadMaterialTextures(material, TextureType.DiffuseRoughness, RLTextureType.Roughness));
        textures.AddRange(LoadMaterialTextures(material, TextureType.Specular, RLTextureType.Specular));
        textures.AddRange(LoadMaterialTextures(material, TextureType.Normals, RLTextureType.Normal));
        textures.AddRange(LoadMaterialTextures(material, TextureType.Height, RLTextureType.Normal));
        textures.AddRange(LoadMaterialTextures(material, TextureType.Ambient, RLTextureType.Height));

        if (!textures.Any())
        {
            Log.Warning($"No texture found for mesh '{mesh->MName}' in model '{Name}'. Mesh will render without texture.");
        }

        if (textures.Any())
        {
            if (!graphics.ShutUp)
                Log.Debug($"Assigned {textures.Count} textures to mesh '{mesh->MName}' in model '{Name}'.");
        }

        var meshObj = new Mesh(graphics, vertices, BuildIndices(indices)).AttachTexture(textures);
        return meshObj;
    }

    private unsafe List<RLTexture> LoadMaterialTextures(Material* mat, TextureType type, RLTextureType rlType)
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
                textureFile.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
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
                var texturePath = Path.Combine(Directory, textureFile);
                if (!System.IO.File.Exists(texturePath))
                {
                    Log.Error("Texture file not found: {TexturePath}", texturePath);
                }
                else
                {
                    Log.Debug("Loading texture: {TexturePath}", texturePath);
                }
                var texture = new RLTexture(graphics, texturePath, rlType);
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