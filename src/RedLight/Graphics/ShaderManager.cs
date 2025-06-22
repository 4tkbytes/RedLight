using Serilog;

namespace RedLight.Graphics;

public struct RLShaderBundle
{
    public RLGraphics Graphics;
    public RLShader VertexShader;
    public RLShader FragmentShader;
    public RLShaderProgram Program;
    public string Name;

    public RLShaderBundle(RLGraphics graphics, string vertexSource, string fragmentSource)
    {
        Graphics = graphics;
        VertexShader = new RLShader(Graphics, ShaderType.Vertex, vertexSource);
        FragmentShader = new RLShader(Graphics, ShaderType.Fragment, fragmentSource);
    }
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
    
    public void TryAdd(string id, RLShaderBundle shaderBundle)
    {
        if (shaders.ContainsKey(id))
        {
            Log.Warning("Shader {A} exists, not re-adding shader again", id);
            return;
        }
        
        shaderBundle.Program = new RLShaderProgram(shaderBundle.Graphics, shaderBundle.VertexShader, shaderBundle.FragmentShader);
        shaderBundle.Name = id;
        
        shaders.Add(id, shaderBundle);
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
    public void SetUniform<T>(string shaderId, string uniform, T value)
    {
        Get(shaderId).Program.SetUniform(uniform, value);
    }
    
    /// <summary>
    /// Checks if a uniform with the given name exists in the shader program
    /// </summary>
    /// <param name="uniform">The name of the uniform to check</param>
    /// <returns>True if the uniform exists, false otherwise</returns>
    public bool HasUniform(string shaderId, string uniform)
    {
        var shader = Get(shaderId);
        var gl = shader.VertexShader.graphics.OpenGL;
        try
        {
            int location = gl.GetUniformLocation(shader.Program.ProgramHandle, uniform);
            return location != -1;
        }
        catch (Exception ex)
        {
            Log.Warning("Error checking uniform {UniformName}: {Error}", uniform, ex.Message);
            return false;
        }
    }

    public void SetUniformIfExists<T>(string shaderId, string uniform, T value)
    {
        if (HasUniform(shaderId, uniform))
            SetUniform(shaderId, uniform, value);
    }

    public string GetProgramHandleString(string shaderId)
    {
        var shader = Get(shaderId);
        return shader.Program.ProgramHandle.ToString();
    }
    
    /// <summary>
    /// Finds a shader ID by its program handle
    /// </summary>
    /// <param name="programHandle">The OpenGL program handle to search for</param>
    /// <returns>The shader ID if found, or null if not found</returns>
    public string FindShaderIdByProgramHandle(uint programHandle)
    {
        foreach (var kvp in shaders)
        {
            if (kvp.Value.Program.ProgramHandle == programHandle)
            {
                return kvp.Key;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Enhanced method to find shader details by program handle with added debugging information
    /// </summary>
    /// <param name="programHandle">The OpenGL program handle</param>
    /// <returns>Detailed information about the shader</returns>
    public string GetShaderDetailsForDebugging(uint programHandle)
    {
        try
        {
            // Try to find the shader ID first
            string shaderId = FindShaderIdByProgramHandle(programHandle);
        
            if (string.IsNullOrEmpty(shaderId))
            {
                // If not found in our shaders dictionary, return a message with program handle
                return $"[Unknown Shader Program: {programHandle}]";
            }
        
            // Get the shader from the dictionary
            var shader = Get(shaderId);
            if (shader.Name == null)
                return $"[Shader ID {shaderId} exists but shader object is null]";
            
            // Return more detailed information about the shader
            return $"{shaderId} (Program: {programHandle})";
        }
        catch (Exception ex)
        {
            // Catch and provide information about any exceptions
            return $"[Error getting shader: {ex.Message}]";
        }
    }
}