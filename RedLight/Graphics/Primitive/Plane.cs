using RedLight.Graphics;
using RedLight.Utils;
using Silk.NET.Maths;
using System.Collections.Generic;

namespace RedLight.Graphics.Primitive
{
    public class Plane
    {
        private RLGraphics graphics;
        private TextureManager textureManager;
        private ShaderManager shaderManager;
        public Transformable<RLModel> Model { get; private set; }

        public Plane(RLGraphics graphics, TextureManager textureManager, ShaderManager shaderManager,
            float width = 10f, float height = 10f, int tilesX = 10, int tilesZ = 10)
        {
            this.graphics = graphics;
            this.textureManager = textureManager;
            this.shaderManager = shaderManager;

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

            RLModel model = new RLModel(graphics, RLFiles.GetResourcePath("RedLight.Resources.Models.Basic.plane.model"), textureManager, "plane");
            model.Meshes.Clear();
            model.Meshes.Add(planeMesh);
            model.AttachShader(shaderManager.Get("basic"));
            model.AttachTexture(textureManager.Get("no-texture"));

            Model = model.MakeTransformable();
        }

        public Plane Default()
        {
            Model.Translate(new Vector3D<float>(0, -0.5f, 0));
            return this;
        }
    }
}