using RedLight.Utils;

namespace RedLight.Graphics.Primitive;

public class Cube : SimpleShape
{
    public Cube(RLGraphics graphics, TextureManager textureManager, ShaderManager shaderManager, bool applyGravity = true)
    : this(graphics, textureManager, shaderManager, "cube", applyGravity)
    {

    }

    public Cube(RLGraphics graphics, TextureManager textureManager, ShaderManager shaderManager, string name, bool applyGravity = true) : base(
            new RLModel(
                graphics,
                RLFiles.GetResourcePath("RedLight.Resources.Models.Basic.cube.model"),
                textureManager)
                .AttachShader(shaderManager.Get("basic"))
                .AttachTexture(textureManager.Get("no-texture"))
                .MakeTransformable()
            )
    { }
}