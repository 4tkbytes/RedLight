using System.Collections.Generic;
using Silk.NET.OpenGL;
using RedLight.Graphics;

namespace RedLight.Graphics
{
    public class ModelManager
    {
        private readonly GL _gl;
        private readonly List<Model> _models = new();
        private readonly AssimpModelLoader _assimpLoader;
        
        public ModelManager(GL gl, TextureManager textureManager)
        {
            _gl = gl;
            _assimpLoader = new AssimpModelLoader(gl, textureManager);
        }
        
    public Model? LoadModel(string resourcePath, Shader shader, Texture2D? defaultTexture = null)
    {
        var model = _assimpLoader.LoadModel(resourcePath, shader, defaultTexture);
        
        if (model != null)
        {
            _models.Add(model);
        }
        
        return model;
    }
          public Model? GetModel(int index)
        {
            if (index >= 0 && index < _models.Count)
                return _models[index];
            
            return null;
        }
        
        public void RenderAll(Shader shader, Camera camera)
        {
            foreach (var model in _models)
            {
                model.Render(_gl, shader, camera);
            }
        }
    }
}