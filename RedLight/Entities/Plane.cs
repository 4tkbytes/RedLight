using RedLight.Utils;
using System.Numerics;

namespace RedLight.Graphics.Primitive
{
    /// <summary>
    /// A flat plane. Thats it. 
    /// </summary>
    public class Plane : SimpleShape
    {
        /// <summary>
        /// Creates a flat plane that you can use to present models or any other purposes.
        /// </summary>
        /// <param name="graphics">The <see cref="RLGraphics"/> instance used for rendering.</param>
        /// <param name="textureManager">The <see cref="TextureManager"/> for handling textures.</param>
        /// <param name="shaderManager">The <see cref="ShaderManager"/> for shader programs.</param>
        /// <param name="width">Width of the plane.</param>
        /// <param name="height">Height of the plane.</param>
        /// <param name="tilesX">Number of texture tiles in X direction.</param>
        /// <param name="tilesZ">Number of texture tiles in Z direction.</param>
        public Plane(RLGraphics graphics,
            float width = 10f, float height = 10f, int tilesX = 10, int tilesZ = 10) : base(
            // Pass the transformable and color to SimpleShape
            new RLModel(
                graphics,
                RLFiles.GetResourcePath("RedLight.Resources.Models.Basic.plane.model"),
                TextureManager.Instance,
                "plane"
            )
            .AttachShader(ShaderManager.Instance.Get("basic")).MakeTransformable()
            )
        {
            ApplyGravity = false;

            List<Vertex> vertices = new List<Vertex>();
            List<uint> indices = new List<uint>();

            // Create vertices
            for (int z = 0; z <= 1; z++)
            {
                for (int x = 0; x <= 1; x++)
                {
                    Vertex vertex = new Vertex();

                    // Position (-0.5 to 0.5 scaled by width/height)
                    vertex.Position = new Vector3(
                        (x - 0.5f) * width,
                        0.0f,
                        (z - 0.5f) * height
                    );

                    // Normal (pointing up)
                    vertex.Normal = new Vector3(0.0f, 1.0f, 0.0f);

                    // Texture coordinates (scaled by tiling factor)
                    vertex.TexCoords = new Vector2(
                        x * tilesX,
                        z * tilesZ
                    );

                    vertices.Add(vertex);
                }
            }

            // Create indices for a quad (2 triangles)
            indices.Add(0);
            indices.Add(2);
            indices.Add(1);
            indices.Add(1);
            indices.Add(2);
            indices.Add(3);

            Mesh planeMesh = new Mesh(graphics, vertices, indices.ToArray());
            var shader = ShaderManager.Instance.Get("basic");
            planeMesh.AttachShader(shader.vertexShader, shader.fragmentShader);

            Target.Target.Meshes.Clear();
            Target.Target.Meshes.Add(planeMesh);
            Target.Target.AttachShader(ShaderManager.Instance.Get("basic"));
            Target.Target.AttachTexture(TextureManager.Instance.Get("no-texture"));
        }

        /// <summary>
        /// This function just translate the Model to a default location. This location is (0, -0.5f, 0). 
        /// </summary>
        /// <returns><see cref="Plane"/></returns>
        public Plane Default()
        {
            Target.Translate(new Vector3(0, -1, 0));
            return this;
        }
    }
}