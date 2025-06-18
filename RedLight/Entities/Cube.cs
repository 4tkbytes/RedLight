using RedLight.Graphics;
using RedLight.Utils;

namespace RedLight.Entities;

/// <summary>
/// A simple cube entity with direct access to transformation methods.
/// Example of the simplified API: cube.Translate(), cube.Model, etc.
/// </summary>
public class Cube : SimpleShape
{
    public Cube(RLGraphics graphics, bool applyGravity = true)
        : this(graphics, "cube", applyGravity)
    {
    }

    public Cube(RLGraphics graphics, string name, bool applyGravity = true) 
        : base(
            new RLModel(graphics, RLFiles.GetResourcePath("RedLight.Resources.Models.Basic.cube.model"), TextureManager.Instance, name)
                .AttachShader(ShaderManager.Instance.Get("lit"))
                .AttachTexture(TextureManager.Instance.Get("no-texture")),
            applyGravity)
    {
        HitboxConfig = HitboxConfig.ForCube(
            size: 1.0f,
            groundOffset: 0.5f
        );
        
        ApplyHitboxConfig();
    }
}
