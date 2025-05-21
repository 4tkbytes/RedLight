using RedLight.Graphics;
using Silk.NET.Assimp;
using Silk.NET.OpenGL;
using System.Numerics;
using System.Reflection;
using System.IO;
using Mesh = RedLight.Graphics.Mesh;
using Shader = RedLight.Graphics.Shader;

public unsafe class AssimpModelLoader
{
    private readonly GL _gl;
    private readonly Silk.NET.Assimp.Assimp _assimp;
    private readonly TextureManager _textureManager;

    public AssimpModelLoader(GL gl, TextureManager textureManager)
    {
        _gl = gl;
        _assimp = Silk.NET.Assimp.Assimp.GetApi();
        _textureManager = textureManager;
    }

    public Model? LoadModel(string resourcePath, Shader shader, Texture2D? defaultTexture = null)
    {
#if DEBUG
        Console.WriteLine($"[DEBUG] Attempting to load model: {resourcePath}");
#endif
        byte[]? modelData = null;

        // Try to load from file system if the path exists
        if (System.IO.File.Exists(resourcePath))
        {
            modelData = System.IO.File.ReadAllBytes(resourcePath);
        }
        else
        {
            // Fallback to embedded resource
            modelData = LoadEmbeddedResource(resourcePath);
        }

        if (modelData == null)
        {
#if DEBUG
            Console.WriteLine($"[DEBUG] Failed to load model data for resource: {resourcePath}");
#endif
            return null;
        }
#if DEBUG
        Console.WriteLine($"[DEBUG] Model data loaded. Size: {modelData.Length} bytes");
#endif

        string directory = Path.GetDirectoryName(resourcePath.Replace('.', '/')) ?? string.Empty;
#if DEBUG
        Console.WriteLine($"[DEBUG] Model directory resolved to: {directory}");
#endif

        Silk.NET.Assimp.Scene* scene = null;
        try
        {
            fixed (byte* dataPtr = modelData)
            {
                scene = _assimp.ImportFileFromMemory(
                    dataPtr,
                    (uint)modelData.Length,
                    (uint)(PostProcessSteps.Triangulate |
                           PostProcessSteps.GenerateSmoothNormals |
                           PostProcessSteps.FlipUVs |
                           PostProcessSteps.CalculateTangentSpace),
                    "obj");
            }

            if (scene == null)
            {
#if DEBUG
                Console.WriteLine($"[DEBUG] Assimp returned null scene. Error: {_assimp.GetErrorStringS()}");
#endif
                return null;
            }
            if ((scene->MFlags & (uint)SceneFlags.Incomplete) != 0)
            {
#if DEBUG
                Console.WriteLine($"[DEBUG] Assimp scene is incomplete. Flags: {scene->MFlags}. Error: {_assimp.GetErrorStringS()}");
#endif
                return null;
            }
            if (scene->MRootNode == null)
            {
#if DEBUG
                Console.WriteLine($"[DEBUG] Assimp scene root node is null. Error: {_assimp.GetErrorStringS()}");
#endif
                return null;
            }
#if DEBUG
            Console.WriteLine($"[DEBUG] Assimp scene loaded. Root node has {scene->MRootNode->MNumMeshes} meshes and {scene->MRootNode->MNumChildren} children.");
#endif

            string modelName = Path.GetFileNameWithoutExtension(resourcePath);
            var model = ProcessNode(scene->MRootNode, scene, modelName, shader, defaultTexture, directory);

#if DEBUG
            if (model == null)
            {
                Console.WriteLine($"[DEBUG] ProcessNode returned null for model: {modelName}");
            }
            else
            {
                Console.WriteLine($"[DEBUG] Model '{modelName}' loaded successfully.");
            }
#endif

            return model;
        }
        finally
        {
            if (scene != null)
                _assimp.ReleaseImport(scene);
        }
    }

    private Model? ProcessNode(Node* node, Silk.NET.Assimp.Scene* scene, string modelName, Shader shader, Texture2D? defaultTexture, string directory)
    {
#if DEBUG
        Console.WriteLine($"[DEBUG] Processing node. Meshes: {node->MNumMeshes}, Children: {node->MNumChildren}");
#endif
        Model? model = null;

        for (int i = 0; i < node->MNumMeshes; i++)
        {
            var mesh = scene->MMeshes[node->MMeshes[i]];
#if DEBUG
            Console.WriteLine($"[DEBUG] Processing mesh {i} with {mesh->MNumVertices} vertices and {mesh->MNumFaces} faces.");
#endif
            var processedMesh = ProcessMesh(mesh, scene, shader, defaultTexture, directory);

            if (model == null)
            {
                model = new Model(modelName, processedMesh);
#if DEBUG
                Console.WriteLine($"[DEBUG] Created Model instance for '{modelName}'.");
#endif
            }

            if (mesh->MMaterialIndex >= 0)
            {
                var material = model.Materials[0];

                if (defaultTexture != null)
                    material.DiffuseTexture = defaultTexture;

                ProcessMaterial(scene->MMaterials[mesh->MMaterialIndex], material, directory);
            }
        }

        for (int i = 0; i < node->MNumChildren; i++)
        {
            if (model == null)
            {
#if DEBUG
                Console.WriteLine($"[DEBUG] Recursively processing child node {i}.");
#endif
                model = ProcessNode(node->MChildren[i], scene, modelName, shader, defaultTexture, directory);
            }
        }

        return model;
    }

    private Mesh ProcessMesh(Silk.NET.Assimp.Mesh* mesh, Silk.NET.Assimp.Scene* scene, Shader shader, Texture2D? defaultTexture, string directory)
    {
#if DEBUG
        Console.WriteLine($"[DEBUG] Building mesh: Vertices={mesh->MNumVertices}, Faces={mesh->MNumFaces}");
#endif
        List<float> vertices = new List<float>();
        List<uint> indices = new List<uint>();
        List<float> texCoords = new List<float>();

        // Process vertex data with position, normal, and texcoords interleaved
        for (int i = 0; i < mesh->MNumVertices; i++)
        {
            // Vertex positions
            vertices.Add(mesh->MVertices[i].X);
            vertices.Add(mesh->MVertices[i].Y);
            vertices.Add(mesh->MVertices[i].Z);

            // Normals
            if (mesh->MNormals != null)
            {
                vertices.Add(mesh->MNormals[i].X);
                vertices.Add(mesh->MNormals[i].Y);
                vertices.Add(mesh->MNormals[i].Z);
            }
            else
            {
                vertices.Add(0.0f);
                vertices.Add(1.0f);
                vertices.Add(0.0f);
            }

            // Texture coordinates
            if (mesh->MTextureCoords[0] != null)
            {
                vertices.Add(mesh->MTextureCoords[0][i].X);
                vertices.Add(mesh->MTextureCoords[0][i].Y);
                
                // Store separately for texture operations
                texCoords.Add(mesh->MTextureCoords[0][i].X);
                texCoords.Add(mesh->MTextureCoords[0][i].Y);
            }
            else
            {
                vertices.Add(0.0f);
                vertices.Add(0.0f);
                texCoords.Add(0.0f);
                texCoords.Add(0.0f);
            }
        }

        // Process indices
        for (int i = 0; i < mesh->MNumFaces; i++)
        {
            var face = mesh->MFaces[i];
            for (int j = 0; j < face.MNumIndices; j++)
            {
                indices.Add(face.MIndices[j]);
            }
        }
#if DEBUG
        Console.WriteLine($"[DEBUG] Mesh built: {vertices.Count / 8} vertices, {indices.Count} indices, {texCoords.Count / 2} texCoords.");
#endif

        // Use the provided default texture or create a placeholder
        Texture2D texture = defaultTexture ?? new Texture2D(_gl, string.Empty);
        return new RedLight.Graphics.Mesh(_gl, vertices.ToArray(), indices.ToArray(), texCoords.ToArray(), shader, texture);
    }

    private void ProcessMaterial(Silk.NET.Assimp.Material* material, RedLight.Graphics.Material redLightMaterial, string directory)
    {
#if DEBUG
        Console.WriteLine($"[DEBUG] Processing material in directory: {directory}");
#endif
        // Get diffuse color
        var colorKey = Silk.NET.Assimp.Assimp.MatkeyColorDiffuse;
        byte[] colorKeyBytes = System.Text.Encoding.ASCII.GetBytes(colorKey + "\0");
        fixed (byte* colorKeyPtr = colorKeyBytes)
        {
            Vector4 colorStruct;
            var result = _assimp.GetMaterialColor(material, colorKeyPtr, 0, 0, &colorStruct);
            if (result == Silk.NET.Assimp.Return.Success)
            {
                // The order depends on how Assimp stores the color
                redLightMaterial.DiffuseColor = new Vector4(colorStruct.X, colorStruct.Y, colorStruct.Z, colorStruct.W);
#if DEBUG
                Console.WriteLine($"[DEBUG] Material color loaded: {redLightMaterial.DiffuseColor}");
#endif
            }
            else
            {
                redLightMaterial.DiffuseColor = new Vector4(1f, 1f, 1f, 1f);
#if DEBUG
                Console.WriteLine($"[DEBUG] Material color defaulted to white.");
#endif
            }
        }

        // Get diffuse texture
        AssimpString path = default;
        var texResult = _assimp.GetMaterialTexture(material, TextureType.Diffuse, 0, &path, null, null, null, null, null, null);
        if (texResult == Silk.NET.Assimp.Return.Success)
        {
            string texPath = path.AsString;
            
            // Create proper path to the texture file
            string fullPath = Path.Combine(directory, texPath).Replace('\\', '/');
            
#if DEBUG
            Console.WriteLine($"[DEBUG] Attempting to load material texture: {fullPath}");
#endif

            try
            {
                // First try to get if texture already exists
                var existingTexture = _textureManager.GetTextureByName(Path.GetFileName(fullPath));
                
                if (existingTexture != null)
                {
#if DEBUG
                    Console.WriteLine($"[DEBUG] Using existing texture: {Path.GetFileName(fullPath)}");
#endif
                    redLightMaterial.DiffuseTexture = existingTexture;
                }
                else if (System.IO.File.Exists(fullPath))
                {
#if DEBUG
                    Console.WriteLine($"[DEBUG] Loading texture from file: {fullPath}");
#endif
                    var texture = new Texture2D(_gl, fullPath);
                    var textureName = Path.GetFileName(fullPath);
                    _textureManager.AddTexture(texture, textureName);
                    redLightMaterial.DiffuseTexture = texture;
                }
                else
                {
#if DEBUG
                    Console.WriteLine($"[DEBUG] Texture file not found: {fullPath}");
#endif
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine($"[DEBUG] Failed to load texture: {texPath}. Error: {ex.Message}");
#endif
            }
        }
        else
        {
#if DEBUG
            Console.WriteLine($"[DEBUG] No diffuse texture found in material.");
#endif
        }
    }

    private byte[]? LoadEmbeddedResource(string resourcePath)
    {
        var assembly = Assembly.GetExecutingAssembly();
#if DEBUG
        Console.WriteLine($"[DEBUG] Attempting to load embedded resource: {resourcePath}");
#endif
        using Stream? stream = assembly.GetManifestResourceStream(resourcePath);

        if (stream == null)
        {
#if DEBUG
            Console.WriteLine($"[DEBUG] Resource not found: {resourcePath}");
#endif
            return null;
        }

        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
#if DEBUG
        Console.WriteLine($"[DEBUG] Resource loaded: {resourcePath}, size: {memoryStream.Length} bytes");
#endif
        return memoryStream.ToArray();
    }
}