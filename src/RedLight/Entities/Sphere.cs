using RedLight.Graphics;
using RedLight.Utils;

namespace RedLight.Entities;

public class Sphere : SimpleShape
{
    public Sphere(RLGraphics graphics, TextureManager textureManager, ShaderManager shaderManager, bool applyGravity = true)
        : this(graphics, textureManager, shaderManager, "sphere", applyGravity)
    {
    }

    public Sphere(RLGraphics graphics, TextureManager textureManager, ShaderManager shaderManager, string name, bool applyGravity = true)
        : base(
            new RLModel(graphics, RLFiles.GetResourcePath("RedLight.Resources.Models.Basic.sphere.model"), textureManager, name)
                .AttachShader(shaderManager.Get("lit"))
                .AttachTexture(textureManager.Get("no-texture")),
            null,
            applyGravity)
    {
    }
}