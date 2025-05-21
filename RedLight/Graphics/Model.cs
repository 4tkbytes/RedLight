using System.Collections.Generic;
using System.Numerics;
using Silk.NET.OpenGL;
using RedLight.Entities;

namespace RedLight.Graphics
{
    public class Model
    {
        public int Id { get; }
        public string Name { get; set; }
        public Mesh Mesh { get; }
        public List<Entity> LinkedEntities { get; } = new List<Entity>();
        public List<Material> Materials { get; } = new List<Material>();
        
        private static int _nextId = 0;
        
        public Model(string name, Mesh mesh)
        {
            Id = _nextId++;
            Name = name;
            Mesh = mesh;
            
            // Add a default material
            Materials.Add(new Material($"{name}_Default"));
        }
        
        public Entity CreateEntity(int materialId = 0)
        {
            var entity = new Entity(Id, materialId);
            LinkedEntities.Add(entity);
            return entity;
        }
        
        public Material GetMaterial(int materialId)
        {
            if (materialId >= 0 && materialId < Materials.Count)
                return Materials[materialId];
            
            return Materials[0]; // Return default material
        }
          public void Render(GL gl, Shader shader, Camera camera)
        {
            shader.Use();
            
            // Set camera matrices
            Matrix4x4 view = camera.GetViewMatrix();
            Matrix4x4 projection = camera.GetProjectionMatrix();
            
            shader.Uniforms.SetMatrix4("uView", view);
            shader.Uniforms.SetMatrix4("uProjection", projection);
            
            foreach (var entity in LinkedEntities)
            {
                // Get material for this entity
                var material = GetMaterial(entity.MaterialId);
                
                // Set material properties
                shader.Uniforms.SetVector4("diffuseColor", material.DiffuseColor);
                
                // Apply entity transform
                shader.Uniforms.SetMatrix4("uModel", entity.Transform.ViewMatrix);
                
                // Bind texture if available
                if (material.DiffuseTexture != null)
                {
                    material.DiffuseTexture.Bind(TextureUnit.Texture0);
                    shader.Uniforms.SetInt("texture0", 0);
                }
                
                // Render the mesh
                Mesh.Render();
            }
        }
    }
}