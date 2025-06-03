using RedLight.Utils;

namespace RedLight.Graphics.Primitive;

public class Sphere
{
    public Transformable<RLModel> Model { get; set; }
    
    public Sphere(RLGraphics graphics, TextureManager textureManager, ShaderManager shaderManager)
    {
        Model = new RLModel(
                graphics,
                RLFiles.GetEmbeddedResourcePath("RedLight.Resources.Models.Basic.sphere.model"),
                textureManager)
            .AttachShader(shaderManager.Get("basic"))
            .MakeTransformable();
    }
}