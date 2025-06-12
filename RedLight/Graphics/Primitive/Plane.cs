using RedLight.Graphics;
using RedLight.Utils;
using Silk.NET.Maths;
using System.Collections.Generic;
using System.Numerics;

namespace RedLight.Graphics.Primitive
{
    /// <summary>
    /// A flat plane. Thats it. 
    /// </summary>
    public class Plane : SimpleShape
    {
        private RLGraphics graphics;
        private TextureManager textureManager;
        private ShaderManager shaderManager;

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
        public Plane(RLGraphics graphics, TextureManager textureManager, ShaderManager shaderManager,
            float width = 10f, float height = 10f, int tilesX = 10, int tilesZ = 10) : base(
            // Pass the transformable and color to SimpleShape
            new RLModel(
                graphics,
                RLFiles.GetResourcePath("RedLight.Resources.Models.Basic.plane.model"),
                textureManager,
                "plane"
            )
            .AttachShader(shaderManager.Get("basic")).MakeTransformable()
            )
        {
            this.graphics = graphics;
            this.textureManager = textureManager;
            this.shaderManager = shaderManager;
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
                    vertex.Position = new Vector3D<float>(
                        (x - 0.5f) * width,
                        0.0f,
                        (z - 0.5f) * height
                    );

                    // Normal (pointing up)
                    vertex.Normal = new Vector3D<float>(0.0f, 1.0f, 0.0f);

                    // Texture coordinates (scaled by tiling factor)
                    vertex.TexCoords = new Vector2D<float>(
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
            var shader = shaderManager.Get("basic");
            planeMesh.AttachShader(shader.vertexShader, shader.fragmentShader);

            Target.Target.Meshes.Clear();
            Target.Target.Meshes.Add(planeMesh);
            Target.Target.AttachShader(shaderManager.Get("basic"));
            Target.Target.AttachTexture(textureManager.Get("no-texture"));
            
            SetHitboxDefault(
                new Vector3D<float>(-10f, -0.1f, -10f), 
                new Vector3D<float>(10f, 0.1f, 10f)
            );
        }

        /// <summary>
        /// This function just translate the Model to a default location. This location is (0, -0.5f, 0). 
        /// </summary>
        /// <returns><see cref="Plane"/></returns>
        public Plane Default()
        {
            Target.Translate(new Vector3D<float>(0, -1, 0));
            return this;
        }

        /// <summary>
        /// Updates the bounding box's position (for <see cref="Plane"/> class)
        /// </summary>
        public void Update(float deltaTime)
        {
            UpdatePhysics(deltaTime);
        }
    }
}