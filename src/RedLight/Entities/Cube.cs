using System.Numerics;
using RedLight.Graphics;
using RedLight.Scene;
using RedLight.Utils;

namespace RedLight.Entities;

/// <summary>
/// A simple cube entity with direct access to transformation methods.
/// Example of the simplified API: cube.Translate(), cube.Model, etc.
/// </summary>
public class Cube : SimpleShape
{
    public Cube(bool applyGravity = true)
        : this("cube", applyGravity)
    {
    }

    public Cube(string name, bool applyGravity = true) 
        : base(
            new RLModel(SceneManager.Instance.GetCurrentScene().Graphics, RLFiles.GetResourcePath("RedLight.Resources.Models.Basic.cube.model"), TextureManager.Instance, name)
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

    public static Cube CreateLightCube(string name, string shaderId)
    {
        var cube = new Cube(name, false);
        cube.SetScale(new Vector3(0.5f));
        cube.Model.AttachShader(ShaderManager.Instance.Get(shaderId));
        cube.ModelType = ModelType.Light;
        return cube;
    }
}
