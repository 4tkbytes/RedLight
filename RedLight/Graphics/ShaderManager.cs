using Serilog;
using Silk.NET.OpenGL;

namespace RedLight.Graphics;

public struct RLShaderBundle
{
    public RLShader vertexShader;
    public RLShader fragmentShader;
    public RLShaderProgram program;
}

public class ShaderManager
{
    private Dictionary<string, RLShaderBundle> shaders = new();

    public void TryAdd(string id, RLShader vertexShader, RLShader fragmentShader)
    {
        if (shaders.ContainsKey(id))
        {
            Log.Warning("Shader {A} exists, not re-adding shader again", id);
            return;
        }

        var program = new RLShaderProgram(vertexShader.graphics, vertexShader, fragmentShader);
        shaders.Add(id, new RLShaderBundle
        {
            vertexShader = vertexShader,
            fragmentShader = fragmentShader,
            program = program
        });
    }

    public RLShaderBundle Get(string id)
    {
        if (!shaders.ContainsKey(id))
            throw new Exception($"ID [{id}] does not exist");
        return shaders[id];
    }
}