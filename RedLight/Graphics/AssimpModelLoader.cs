using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using Silk.NET.Assimp;
using Silk.NET.OpenGL;
using RedLight.Graphics;
using RedLight.Utils;

namespace RedLight.Graphics
{
    public unsafe class AssimpModelLoader
    {
        private readonly GL _gl;
        private readonly Assimp _assimp;
        private readonly TextureManager _textureManager;
        
        public AssimpModelLoader(GL gl, TextureManager textureManager)
        {
            _gl = gl;
            _assimp = Assimp.GetApi();
            _textureManager = textureManager;
        }

        public Model LoadModel(string resourcePath, Shader shader, Texture2D defaultTexture = null)
        {
            // Load embedded resource
            byte[] modelData = LoadEmbeddedResource(resourcePath);
            if (modelData == null)
            {
                Console.WriteLine($"Failed to load model: {resourcePath}");
                return null;
            }

            // Get directory path for loading textures
            string directory = Path.GetDirectoryName(resourcePath.Replace('.', '/'));
            
            // Import scene using Assimp
            var scene = _assimp.ImportFileFromMemory(
                modelData, 
                (uint)(PostProcessSteps.Triangulate | 
                       PostProcessSteps.GenerateSmoothNormals | 
                       PostProcessSteps.FlipUVs | 
                       PostProcessSteps.CalculateTangentSpace),
                "obj");

            if (scene == null || scene.MFlags == Silk.NET.Assimp.Assimp.SceneFlagsIncomplete || scene.MRootNode == null)
            {
                Console.WriteLine($"Assimp error: {_assimp.GetErrorString()}");
                return null;
            }

            try
            {
                // Process the model
                string modelName = Path.GetFileNameWithoutExtension(resourcePath);
                return ProcessNode(scene.MRootNode, scene, modelName, shader, defaultTexture, directory);
            }
            finally
            {
                // Free the imported scene
                _assimp.ReleaseImport(scene);
            }
        }

        private Model ProcessNode(Node node, Scene scene, string modelName, Shader shader, Texture2D defaultTexture, string directory)
        {
            Model model = null;
            
            // Process all meshes in the node
            for (int i = 0; i < node.MNumMeshes; i++)
            {
                var mesh = scene.MMeshes[node.MMeshes[i]];
                var processedMesh = ProcessMesh(mesh, scene, directory);
                
                if (model == null)
                {
                    model = new Model(modelName, processedMesh);
                }
                
                // If we have a material, set it up
                if (mesh.MMaterialIndex >= 0)
                {
                    var material = model.Materials[0]; // Use default material
                    
                    // Set texture if available or use default
                    if (defaultTexture != null)
                    {
                        material.DiffuseTexture = defaultTexture;
                    }
                    
                    // Process material properties if needed
                    ProcessMaterial(scene.MMaterials[mesh.MMaterialIndex], material, directory);
                }
            }
            
            // Process children nodes recursively
            for (int i = 0; i < node.MNumChildren; i++)
            {
                // For simplicity, we're only supporting a single model with a single mesh
                // In a more complex system, you'd want to handle multiple meshes and child nodes
                if (model == null)
                {
                    model = ProcessNode(node.MChildren[i], scene, modelName, shader, defaultTexture, directory);
                }
            }
            
            return model;
        }

        private Mesh ProcessMesh(Silk.NET.Assimp.Mesh mesh, Scene scene, string directory)
        {
            List<float> vertices = new List<float>();
            List<uint> indices = new List<uint>();

            // Process vertices
            for (int i = 0; i < mesh.MNumVertices; i++)
            {
                // Position
                vertices.Add(mesh.MVertices[i].X);
                vertices.Add(mesh.MVertices[i].Y);
                vertices.Add(mesh.MVertices[i].Z);
                
                // Normals (if available)
                if (mesh.MNormals != null)
                {
                    vertices.Add(mesh.MNormals[i].X);
                    vertices.Add(mesh.MNormals[i].Y);
                    vertices.Add(mesh.MNormals[i].Z);
                }
                else
                {
                    vertices.Add(0.0f);
                    vertices.Add(1.0f);
                    vertices.Add(0.0f);
                }
                
                // Texture coordinates (if available)
                if (mesh.MTextureCoords[0] != null)
                {
                    vertices.Add(mesh.MTextureCoords[0][i].X);
                    vertices.Add(mesh.MTextureCoords[0][i].Y);
                }
                else
                {
                    vertices.Add(0.0f);
                    vertices.Add(0.0f);
                }
            }

            // Process indices
            for (int i = 0; i < mesh.MNumFaces; i++)
            {
                var face = mesh.MFaces[i];
                for (int j = 0; j < face.MNumIndices; j++)
                {
                    indices.Add(face.MIndices[j]);
                }
            }

            // Create and return the mesh
            return new Mesh(_gl, vertices.ToArray(), indices.ToArray());
        }

        private void ProcessMaterial(Material material, RedLight.Graphics.Material redLightMaterial, string directory)
        {
            // Process diffuse color
            if (_assimp.GetMaterialColor(material, Assimp.MatKeyDiffuse, 0, 0, out Silk.NET.Assimp.Color4D diffuse))
            {
                redLightMaterial.DiffuseColor = new Vector4(diffuse.R, diffuse.G, diffuse.B, diffuse.A);
            }
            
            // Process diffuse texture
            if (_assimp.GetMaterialTexture(material, TextureType.Diffuse, 0, out string texPath, out _, out _, out _, out _, out _, out _) == ReturnCode.Success)
            {
                string fullPath = Path.Combine(directory, texPath).Replace('\\', '/');
                
                // Try to load the texture from embedded resources
                try
                {
                    // Convert path format to resource format
                    string texResourcePath = fullPath.Replace('/', '.');
                    
                    // Check if texture is already loaded
                    if (!_textureManager.HasTexture(texResourcePath))
                    {
                        var textureData = LoadEmbeddedResource(texResourcePath);
                        if (textureData != null)
                        {
                            var texture = new Texture2D(_gl, texResourcePath);
                            _textureManager.AddTexture(texture, texResourcePath);
                        }
                    }
                    
                    // Set texture
                    redLightMaterial.DiffuseTexture = _textureManager.GetTexture(texResourcePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load texture: {fullPath}. Error: {ex.Message}");
                }
            }
        }

        private byte[] LoadEmbeddedResource(string resourcePath)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(resourcePath);
            
            if (stream == null)
            {
                Console.WriteLine($"Resource not found: {resourcePath}");
                return null;
            }
            
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
    }
}