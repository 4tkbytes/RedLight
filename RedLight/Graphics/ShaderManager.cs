using Serilog;

namespace RedLight.Graphics;

public struct RLShaderBundle
{
    public RLShader vertexShader;
    public RLShader fragmentShader;
    public RLShaderProgram program;
}

public class ShaderManager
{
    private static readonly Lazy<ShaderManager> _instance = new(() => new ShaderManager());
    public static ShaderManager Instance => _instance.Value;

    private Dictionary<string, RLShaderBundle> shaders = new();

    private ShaderManager() { }

    /// <summary>
    /// This function attempts to add a shader into the ShaderManagers dictionary. If the shader exists (as per the id),
    /// it will only provide a warning and return unlike a normal Add function which throws an exception. 
    /// </summary>
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

    /// <summary>
    /// This function attempts to fetch the shader by its ID from the dictionary. If the ID does not exist, it will
    /// log a warning and return null. 
    /// </summary>
    public RLShaderBundle TryGet(string id)
    {
        if (!shaders.ContainsKey(id))
        {
            Log.Warning($"ID [{id}] does not exist, returning null");
            return new RLShaderBundle { vertexShader = null, fragmentShader = null, program = null };
        }

        return shaders[id];
    }

    /// <summary>
    /// This function attempts to fetch the shader by its ID from the dictionary. If the ID does not exist, it will
    /// throw an Exception stating that the ID/Shader does not exist. If it does exist, it will return a RLShaderBundle. 
    /// </summary>
    public RLShaderBundle Get(string id)
    {
        if (!shaders.ContainsKey(id))
            throw new Exception($"ID [{id}] does not exist");
        return shaders[id];
    }
}