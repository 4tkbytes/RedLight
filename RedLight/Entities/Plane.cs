using RedLight.Utils;
using RedLight.Graphics;
using System.Numerics;
using System.Collections.Generic;

namespace RedLight.Graphics.Primitive;

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
    public Plane(RLGraphics graphics, float width = 10f, float height = 10f, int tilesX = 10, int tilesZ = 10) 
        : base(
            new RLModel(graphics, RLFiles.GetResourcePath("RedLight.Resources.Models.Basic.plane.model"), TextureManager.Instance, "plane")
                .AttachShader(ShaderManager.Instance.Get("basic")),
            false) // planes don't apply gravity by default
    {
        // Set bounding box to match actual plane dimensions
        // Plane extends from -width/2 to +width/2 in X, and -height/2 to +height/2 in Z
        // Y is kept minimal since it's a flat plane (small thickness for collision detection)
        DefaultBoundingBoxMin = new Vector3(-width / 2f, -0.1f, -height / 2f);
        DefaultBoundingBoxMax = new Vector3(width / 2f, 0.1f, height / 2f);

        // Update the actual bounding box based on current position
        var currentPosition = Position;
        BoundingBoxMin = currentPosition + DefaultBoundingBoxMin;
        BoundingBoxMax = currentPosition + DefaultBoundingBoxMax;

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
        var shader = ShaderManager.Instance.Get("basic");
        planeMesh.AttachShader(shader.vertexShader, shader.fragmentShader);

        Model.Meshes.Clear();
        Model.Meshes.Add(planeMesh);
        Model.AttachShader(ShaderManager.Instance.Get("basic"));
        Model.AttachTexture(TextureManager.Instance.Get("no-texture"));
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
