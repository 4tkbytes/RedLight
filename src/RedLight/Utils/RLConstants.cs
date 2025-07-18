using System.Numerics;
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

    public static readonly Vector3 RL_SUN_DIRECTION = new(-0.5f, -1.0f, -0.3f);
}