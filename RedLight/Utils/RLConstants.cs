using RedLight.Graphics;

namespace RedLight.Utils;

public static class RLConstants
{
    public static readonly byte[] RL_NO_TEXTURE =
        RLFiles.GetEmbeddedResourceBytes("RedLight.Resources.Textures.no-texture.png");
    public static readonly string RL_NO_TEXTURE_PATH = "RedLight.Resources.Textures.no-texture.png";

    public static readonly string RL_BASIC_SHADER_VERT =
        RLFiles.GetEmbeddedResourceString("RedLight.Resources.Shaders.basic.vert");

    public static readonly string RL_BASIC_SHADER_FRAG =
        RLFiles.GetEmbeddedResourceString("RedLight.Resources.Shaders.basic.frag");
        
    public static readonly int MAX_BONE_INFLUENCE = 4;

    public static readonly RLGraphics.Colour RL_COLOUR_CORNFLOWER_BLUE =
        new RLGraphics.Colour { r = 100f / 256, g = 146f / 256, b = 237f / 256, a = 1f };
}