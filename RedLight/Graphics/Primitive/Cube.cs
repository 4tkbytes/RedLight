using RedLight.Utils;

namespace RedLight.Graphics.Primitive;

public class Cube
{
    public Transformable<RLModel> Model { get; set; }

    public Cube(RLGraphics graphics, TextureManager textureManager, ShaderManager shaderManager)
    : this(graphics, textureManager, shaderManager, "cube")
    {

    }

    public Cube(RLGraphics graphics, TextureManager textureManager, ShaderManager shaderManager, string name)
    {
        Model = new RLModel(
                graphics,
                RLFiles.GetEmbeddedResourcePath("RedLight.Resources.Models.Basic.cube.model"),
                textureManager)
            .AttachShader(shaderManager.Get("basic"))
            .AttachTexture(textureManager.Get("no-texture"))
            .MakeTransformable();
    }
}