using Serilog;

namespace RedLight.Graphics;

public struct RLShaderBundle
{
    public RLShader VertexShader;
    public RLShader FragmentShader;
    public RLShaderProgram Program;
    public string Name;
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
            VertexShader = vertexShader,
            FragmentShader = fragmentShader,
            Program = program,
            Name = id
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
            return new RLShaderBundle { VertexShader = null, FragmentShader = null, Program = null };
        }

        return shaders[id];
    }

    /// <summary>
    /// Clones/Duplicates a shader. 
    /// </summary>
    /// <param name="oldId"></param>
    /// <param name="newId"></param>
    /// <exception cref="Exception"></exception>
    public void Clone(string oldId, string newId)
    {
        if (!shaders.ContainsKey(oldId))
            throw new Exception($"ID [{oldId}] does not exist");
        
        var vertexShader = shaders[oldId].VertexShader;
        var fragmentShader = shaders[oldId].FragmentShader;
        
        // recompile new program
        var program = new RLShaderProgram(vertexShader.graphics, vertexShader, fragmentShader);
        
        shaders.Add(newId, new RLShaderBundle
        {
            VertexShader = vertexShader,
            FragmentShader = fragmentShader,
            Program = program
        });
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

    /// <summary>
    /// A better way of setting a shaders uniform. Instead of going directly into the ShaderBundles program,
    /// it will search it up for you. 
    /// </summary>
    /// <param name="shaderId"></param>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    public void SetUniform<T>(string shaderId, string name, T value)
    {
        Get(shaderId).Program.SetUniform(name, value);
    }
    
    /// <summary>
    /// Checks if a uniform with the given name exists in the shader program
    /// </summary>
    /// <param name="name">The name of the uniform to check</param>
    /// <returns>True if the uniform exists, false otherwise</returns>
    public bool HasUniform(string shaderId, string name)
    {
        var shader = Get(shaderId);
        var gl = shader.VertexShader.graphics.OpenGL;
        try
        {
            int location = gl.GetUniformLocation(shader.Program.ProgramHandle, name);
            return location != -1;
        }
        catch (Exception ex)
        {
            Log.Warning("Error checking uniform {UniformName}: {Error}", name, ex.Message);
            return false;
        }
    }
}