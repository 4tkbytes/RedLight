using RedLight.Utils;

namespace RedLight.Graphics.Primitive;

public class Cube : SimpleShape
{
    public Cube(RLGraphics graphics, TextureManager textureManager, ShaderManager shaderManager)
    : this(graphics, textureManager, shaderManager, "cube")
    {

    }

    public Cube(RLGraphics graphics, TextureManager textureManager, ShaderManager shaderManager, string name) : base(
            new RLModel(
                graphics,
                RLFiles.GetResourcePath("RedLight.Resources.Models.Basic.cube.model"),
                textureManager)
                .AttachShader(shaderManager.Get("basic"))
                .AttachTexture(textureManager.Get("no-texture"))
            )
    { }

    public void dummy()
    {
        
    }
}