using Serilog;

namespace RedLight.Graphics;

public class TextureManager
{
    private static readonly Lazy<TextureManager> _instance = new(() => new TextureManager());
    public static TextureManager Instance => _instance.Value;

    public Dictionary<string, RLTexture> textures = new();

    private TextureManager() { }

    /// <summary>
    /// This function attempts to add a texture to its dictionary. If the texture exists, it will throw
    /// an exception. If it does exist, it will add the texture to its ID to the dictionary. 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="rlTexture"></param>
    /// <exception cref="Exception"></exception>
    public void Add(string id, RLTexture rlTexture)
    {
        if (textures.ContainsKey(id))
        {
            throw new Exception($"Texture [{id}] already exists");
        }

        rlTexture.Name = id;
        textures.Add(id, rlTexture);
    }

    /// <summary>
    /// This function attempts to add a texture to its dictionary. If the texture exists, it will Log a warning and
    /// not add. If it does exist, it will add the texture to its ID to the dictionary. 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="rlTexture"></param>
    public void TryAdd(string id, RLTexture rlTexture)
    {
        if (textures.ContainsKey(id))
        {
            Log.Warning("Texture {A} exists, not re-adding texture again", id);
            return;
        }
        textures.Add(id, rlTexture);
    }

    /// <summary>
    /// This function attempts to fetch the Texture from its id. If the texture does not exist, it will throw
    /// an exception. If it does exist, it will return a RLTexture.  
    /// </summary>
    /// <param name="id"></param>
    /// <returns>RLTexture</returns>
    /// <exception cref="Exception"></exception>
    public RLTexture Get(string id)
    {
        if (!textures.ContainsKey(id))
        {
            throw new Exception($"ID [{id}] does not exist");
        }

        return textures[id];
    }

    /// <summary>
    /// This function attempts to fetch the Texture from its id. If the texture does not exist, it will Log a warning
    /// and return null. If it does exist, it will return a RLTexture. 
    /// </summary>
    /// <param name="id"></param>
    /// <returns>RLTexture</returns>
    public RLTexture TryGet(string id)
    {
        if (!textures.ContainsKey(id))
        {
            Log.Warning($"ID [{id}] does not exist, returning null");
            return null;
        }

        return textures[id];
    }

    /// <summary>
    /// This function attempts to fetch the Texture from its id. If the texture does not exist, it will Log a warning
    /// and return null. If it does exist, it will return a RLTexture.
    ///
    /// This function contains an additional parameter, where if it is silent it will not log and silently return null. 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="silent"></param>
    /// <returns>RLTexture</returns>
    public RLTexture TryGet(string id, bool silent)
    {
        if (!textures.ContainsKey(id))
        {
            if (!silent)
                Log.Warning($"ID [{id}] does not exist, returning null");
            return null;
        }

        return textures[id];
    }

    public bool Exists(string id)
    {
        return textures.ContainsKey(id);
    }

    /// <summary>
    /// This function removes the RLTexture from the dictionary by searching by its ID. If there is no texture with the ID,
    /// it will throw an Exception. Otherwise it will remove the texture. 
    /// </summary>
    /// <param name="id"></param>
    /// <exception cref="Exception"></exception>
    public void Remove(string id)
    {
        if (!textures.ContainsKey(id))
        {
            throw new Exception($"ID [{id}] does not exist");
        }

        textures.Remove(id);
    }

    /// <summary>
    /// This function removes the RLTexture from the dictionary by searching by its ID. If there is no texture with the ID,
    /// it will just return nothing, without deleting it (because there is no need to). Otherwise it will remove the texture. 
    /// </summary>
    /// <param name="id"></param>
    public void TryRemove(string id)
    {
        if (!textures.ContainsKey(id))
        {
            return;
        }

        textures.Remove(id);
    }
}