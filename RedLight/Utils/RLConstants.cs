using RedLight.Graphics;

namespace RedLight.Utils;

public static class RLConstants
{
    public static readonly string RL_NO_TEXTURE_PATH = "RedLight.Resources.Textures.no-texture.png";

    public static readonly string RL_BASIC_SHADER_VERT =
        RLFiles.GetResourceAsString("RedLight.Resources.Shaders.basic.vert");

    public static readonly string RL_BASIC_SHADER_FRAG =
        RLFiles.GetResourceAsString("RedLight.Resources.Shaders.basic.frag");

    public static readonly int MAX_BONE_INFLUENCE = 4;
}