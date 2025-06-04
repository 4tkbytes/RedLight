using Serilog;

namespace RedLight.Graphics;

public class TextureManager
{
    public Dictionary<string, RLTexture> textures = new();

    public void Add(string id, RLTexture rlTexture)
    {
        if (textures.ContainsKey(id))
        {
            throw new Exception($"Texture [{id}] already exists");
        }
        textures.Add(id, rlTexture);
    }

    public void TryAdd(string id, RLTexture rlTexture)
    {
        if (textures.ContainsKey(id))
        {
            Log.Warning("Texture {A} exists, not re-adding texture again", id);
            return;
        }
        textures.Add(id, rlTexture);
    }

    public RLTexture Get(string id)
    {
        if (!textures.ContainsKey(id))
        {
            throw new Exception($"ID [{id}] does not exist");
        }

        return textures[id];
    }

    public RLTexture TryGet(string id)
    {
        if (!textures.ContainsKey(id))
        {
            Log.Warning($"ID [{id}] does not exist, returning null");
            return null;
        }

        return textures[id];
    }

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

    public void Remove(string id)
    {
        if (!textures.ContainsKey(id))
        {
            throw new Exception($"ID [{id}] does not exist");
        }

        textures.Remove(id);
    }

    public void TryRemove(string id)
    {
        if (!textures.ContainsKey(id))
        {
            return;
        }

        textures.Remove(id);
    }
}