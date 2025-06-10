using System.Numerics;
using RedLight.Physics;
using RedLight.Utils;

namespace RedLight.Graphics.Primitive;

public class Sphere : SimpleShape
{
    public Sphere(RLGraphics graphics, TextureManager textureManager, ShaderManager shaderManager)
        : this(graphics, textureManager, shaderManager, "sphere") { }

    public Sphere(RLGraphics graphics, TextureManager textureManager, ShaderManager shaderManager, string name)
        : base(
            new RLModel(
                graphics,
                RLFiles.GetResourcePath("RedLight.Resources.Models.Basic.sphere.model"),
                textureManager
            ).AttachShader(shaderManager.Get("basic"))
            )
    { }
}