using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedLight.Graphics
{
    public class TextureManager
    {
        private Dictionary<string, Texture2D> _textures = new();
        private GL _gl;

        public TextureManager(GL gl) => _gl = gl;

        public Texture2D GetTexture(string path)
        {
            if (!_textures.ContainsKey(path))
                _textures[path] = new Texture2D(_gl, path);
            return _textures[path];
        }

    public Texture2D? GetTextureByName(string name)
    {
        foreach (var kvp in _textures)
        {
            if (kvp.Key.EndsWith(name, StringComparison.OrdinalIgnoreCase))
            {
                return kvp.Value;
            }
        }
        return null;
    }

        public void AddTexture(Texture2D texture, string name)
        {
            if (_textures.ContainsKey(name))
            {
                throw new Exception($"Texture [{name}] exists in texture manager");
            }
            if (texture == null)
                throw new Exception("Texture is null, does not exist");
            _textures.Add(name, texture);
        }

        public void DisposeAll()
        {
            foreach (var tex in _textures.Values)
                tex.Dispose();
            _textures.Clear();
        }
    }
}
