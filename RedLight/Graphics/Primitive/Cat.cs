using RedLight.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedLight.Graphics.Primitive
{
    public class Cat
    {
        public Transformable<RLModel> Model { get; set; }

        public Cat(RLGraphics graphics, TextureManager textureManager, ShaderManager shaderManager)
    : this(graphics, textureManager, shaderManager, "cat")
        {

        }

        public Cat(RLGraphics graphics, TextureManager textureManager, ShaderManager shaderManager, string name)
        {
            Model = new RLModel(
                    graphics,
                    RLFiles.GetEmbeddedResourcePath("RedLight.Resources.Models.Basic.cat.fbx"),
                    textureManager)
                .AttachShader(shaderManager.Get("basic"))
                .MakeTransformable();
        }
    }
}
