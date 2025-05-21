using System.Collections.Generic;
using Silk.NET.OpenGL;

namespace RedLight.Graphics
{
    public class ModelManager
    {
        private Dictionary<int, Model> _models = new Dictionary<int, Model>();
        private GL _gl;
        
        public ModelManager(GL gl)
        {
            _gl = gl;
        }
        
        public Model LoadObjModel(string filePath, Shader shader, Texture2D texture = null)
        {
            var mesh = RLObjLoader.LoadObj(_gl, filePath, shader, texture);
            var modelName = Path.GetFileNameWithoutExtension(filePath);
            var model = new Model(modelName, mesh);
    
            // If texture is provided or inferred from material, use it
            if (texture != null)
            {
                model.Materials[0].DiffuseTexture = texture;
            }
    
            _models.Add(model.Id, model);
            return model;
        }
        
        public Model GetModel(int id)
        {
            if (_models.TryGetValue(id, out var model))
                return model;
            
            return null;
        }
        
        public void RenderAll(Shader shader, Camera camera)
        {
            foreach (var model in _models.Values)
            {
                model.Render(_gl, shader, camera);
            }
        }
    }
}