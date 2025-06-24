using System.Numerics;
using RedLight.Graphics;
using RedLight.Utils;

namespace RedLight.Entities;

/// <summary>
/// A simple plane entity with direct access to transformation methods.
/// Example of the simplified API: plane.Translate(), plane.Model, etc.
/// </summary>
public class Plane : SimpleShape
{
    /// <summary>
    /// Creates a flat plane that you can use to present models or any other purposes.
    /// </summary>
    /// <param name="graphics">The <see cref="RLGraphics"/> instance used for rendering.</param>
    /// <param name="width">Width of the plane.</param>
    /// <param name="height">Height of the plane.</param>
    /// <param name="tilesX">Number of texture tiles in X direction.</param>
    /// <param name="tilesZ">Number of texture tiles in Z direction.</param>
    public Plane(RLGraphics graphics, float width = 10f, float height = 10f, int tilesX = 0, int tilesZ = 0)
        : base(
            new RLModel(graphics, RLFiles.GetResourcePath("RedLight.Resources.Models.Basic.plane.model"), TextureManager.Instance, "plane")
                .AttachShader(ShaderManager.Instance.Get("lit")),
            null,
            false)
    {
        if (tilesX == 0 || tilesZ == 0)
        {
            tilesX = (int)width;
            tilesZ = (int)height;
        }

        // Configure hitbox for plane using HitboxConfig
        HitboxConfig = HitboxConfig.ForPlane(width, height, 0.1f);

        // Apply the hitbox configuration
        ApplyHitboxConfig();

        // Create custom plane geometry
        CreatePlaneGeometry(graphics, width, height, tilesX, tilesZ);
    }

    private void CreatePlaneGeometry(RLGraphics graphics, float width, float height, int tilesX, int tilesZ)
    {
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
        var shader = ShaderManager.Instance.Get("lit");
        planeMesh.AttachShader(shader.VertexShader, shader.FragmentShader);

        Model.Meshes.Clear();
        Model.Meshes.Add(planeMesh);
        Model.AttachShader(shader);
        Model.AttachTexture(TextureManager.Instance.Get("prototype"));
    }

    /// <summary>
    /// This function just translates the plane to a default location. This location is (0, -1, 0). 
    /// </summary>
    /// <returns><see cref="Plane"/></returns>
    public Plane Default()
    {
        Translate(new Vector3(0, -1, 0));
        return this;
    }
}