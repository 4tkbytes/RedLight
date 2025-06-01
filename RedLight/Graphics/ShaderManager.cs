using Serilog;
using Silk.NET.OpenGL;

namespace RedLight.Graphics;

public class ShaderManager
{
    public struct RLShaderPair
    {
        public RLShader vertexShader;
        public RLShader fragmentShader;
    }
    private Dictionary<string, RLShaderPair> shaders = new();

    public void Add(string id, RLShader vertexShader, RLShader fragmentShader)
    {
        if (shaders.ContainsKey(id))
        {
            throw new Exception($"ID [{id}] is already registered");
        }
        
        shaders.Add(id, new RLShaderPair() { vertexShader = vertexShader, fragmentShader = fragmentShader });
    }
    
    public void TryAdd(string id, RLShader vertexShader, RLShader fragmentShader)
    {
        if (shaders.ContainsKey(id))
        {
            Log.Debug("Shader {A} exists, not re-adding shader again", id);
            return;
        }
        
        shaders.Add(id, new RLShaderPair() { vertexShader = vertexShader, fragmentShader = fragmentShader });
    }

    public RLShaderPair Get(string id)
    {
        if (!shaders.ContainsKey(id))
        {
            throw new Exception($"ID [{id}] does not exist");
        }
        
        return shaders[id];
    }

    public void Remove(string id)
    {
        if (!shaders.ContainsKey(id))
        {
            throw new Exception($"ID [{id}] does not exist");
        }
        
        shaders.Remove(id);
    }
}