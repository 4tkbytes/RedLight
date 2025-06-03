using System.Numerics;
using RedLight.Utils;
using Silk.NET.Assimp;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace RedLight.Graphics;

public class RLModel
{
    List<Mesh> meshes = new List<Mesh>();
    private Assimp _assimp;
    private RLGraphics graphics;
    private GL _gl;
    private List<RLTexture> _texturesLoaded = new();
    public string Directory { get; protected set; } = string.Empty;
    public List<Mesh> Meshes { get; protected set; } = new List<Mesh>();

    public RLModel(RLGraphics graphics, string path)
    {
        var assimp = Assimp.GetApi();
        _assimp = assimp;
        this.graphics = graphics;
        _gl = graphics.OpenGL;
        LoadModel(path);
    }

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

        Directory = path;

        ProcessNode(scene->MRootNode, scene);
    }
    
    private unsafe void ProcessNode(Node* node, Silk.NET.Assimp.Scene* scene)
    {
        for (var i = 0; i < node->MNumMeshes; i++)
        {
            var mesh = scene->MMeshes[node->MMeshes[i]];
            Meshes.Add(ProcessMesh(mesh, scene));

        }

        for (var i = 0; i < node->MNumChildren; i++)
        {
            ProcessNode(node->MChildren[i], scene);
        }
    }

    private unsafe Mesh ProcessMesh(Silk.NET.Assimp.Mesh* mesh, Silk.NET.Assimp.Scene* scene)
    {
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
                if (mesh->MTextureCoords[0] != null) // does the mesh contain texture coordinates?
                {
                    Vector2D<float> vec = new();
                    // a vertex can contain up to 8 different texture coordinates. We thus make the assumption that we won't 
                    // use models where a vertex can have multiple texture coordinates so we always take the first set (0).
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
            // now wak through each of the mesh's faces (a face is a mesh its triangle) and retrieve the corresponding vertex indices.
            for (uint i = 0; i < mesh->MNumFaces; i++)
            {
                Face face = mesh->MFaces[i];
                // retrieve all indices of the face and store them in the indices vector
                for (uint j = 0; j < face.MNumIndices; j++)
                    indices.Add(face.MIndices[j]);
            }
            // process materials
            Material* material = scene->MMaterials[mesh->MMaterialIndex];
            // we assume a convention for sampler names in the shaders. Each diffuse texture should be named
            // as 'texture_diffuseN' where N is a sequential number ranging from 1 to MAX_SAMPLER_NUMBER. 
            // Same applies to other texture as the following list summarizes:
            // diffuse: texture_diffuseN
            // specular: texture_specularN
            // normal: texture_normalN

            // 1. diffuse maps
            var diffuseMaps = LoadMaterialTextures(material, TextureType.Diffuse, "texture_diffuse");
            if (diffuseMaps.Any())
                textures.AddRange(diffuseMaps);
            // 2. specular maps
            var specularMaps = LoadMaterialTextures(material, TextureType.Specular, "texture_specular");
            if (specularMaps.Any())
                textures.AddRange(specularMaps);
            // 3. normal maps
            var normalMaps = LoadMaterialTextures(material, TextureType.Height, "texture_normal");
            if (normalMaps.Any())
                textures.AddRange(normalMaps);
            // 4. height maps
            var heightMaps = LoadMaterialTextures(material, TextureType.Ambient, "texture_height");
            if (heightMaps.Any())
                textures.AddRange(heightMaps);

            // return a mesh object created from the extracted mesh data
            var result = new Mesh(graphics, vertices, BuildIndices(indices)).AttachTexture(textures);
            return result;
    }
    
    private unsafe List<RLTexture> LoadMaterialTextures(Material* mat, TextureType type, string typeName)
    {
        var textureCount = _assimp.GetMaterialTextureCount(mat, type);
        List<RLTexture> textures = new List<RLTexture>();
        for (uint i = 0; i < textureCount; i++)
        {
            AssimpString path;
            _assimp.GetMaterialTexture(mat, type, i, &path, null, null, null, null, null, null);
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
                // Map Assimp TextureType to RLTextureType
                RLTextureType rlType = RLTextureType.Diffuse; // default
                switch (type)
                {
                    case TextureType.Diffuse:
                        rlType = RLTextureType.Diffuse;
                        break;
                    case TextureType.Specular:
                        rlType = RLTextureType.Specular;
                        break;
                    case TextureType.Normals:
                    case TextureType.Height:
                        rlType = RLTextureType.Normal;
                        break;
                    case TextureType.Ambient:
                        rlType = RLTextureType.Height;
                        break;
                }
                var texture = new RLTexture(graphics, Directory, rlType);
                texture.Path = path;
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