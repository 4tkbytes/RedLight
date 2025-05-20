using Silk.NET.OpenGL;
using System.Collections.Generic;

namespace RedLight.Graphics;

public class ShaderManager
{
    private readonly GL _gl;
    private readonly Dictionary<string, Shader> _shaders = new();

    public ShaderManager(GL gl)
    {
        _gl = gl;
    }

    public void Add(string name, string vertexSource, string fragmentSource)
    {
        if (_shaders.ContainsKey(name))
            throw new Exception($"Shader '{name}' already exists.");

        var shader = new Shader(_gl, vertexSource, fragmentSource);
        _shaders[name] = shader;
    }

    public Shader Get(string name)
    {
        if (!_shaders.TryGetValue(name, out var shader))
            throw new Exception($"Shader '{name}' not found.");
        return shader;
    }

    public void Remove(string name)
    {
        if (_shaders.TryGetValue(name, out var shader))
        {
            shader.Dispose();
            _shaders.Remove(name);
        }
    }

    public void DisposeAll()
    {
        foreach (var shader in _shaders.Values)
            shader.Dispose();
        _shaders.Clear();
    }
}