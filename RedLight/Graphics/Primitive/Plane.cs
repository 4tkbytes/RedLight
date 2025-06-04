using RedLight.Utils;

namespace RedLight.Graphics.Primitive;

public class Plane
{
    public Transformable<RLModel> Model { get; set; }

    public Plane(RLGraphics graphics, TextureManager textureManager, ShaderManager shaderManager)
    : this(graphics, textureManager, shaderManager, "cube")
    {

    }

    public Plane(RLGraphics graphics, TextureManager textureManager, ShaderManager shaderManager, string name)
    {
        Model = new RLModel(
                graphics,
                RLFiles.GetEmbeddedResourcePath("RedLight.Resources.Models.Basic.plane.model"),
                textureManager)
            .AttachShader(shaderManager.Get("basic"))
            .MakeTransformable();
    }
}