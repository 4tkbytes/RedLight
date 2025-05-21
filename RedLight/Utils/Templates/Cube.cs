using Silk.NET.OpenGL;
using System.Numerics;

namespace RedLight.Graphics
{
    public class Cube : Mesh
    {
        public Cube(GL gl, Shader shader, Texture2D texture) 
            : base(gl, GenerateVertices(), GenerateIndices(), GenerateTexCoords(), shader, texture)
        {
        }

        private static float[] GenerateVertices()
        {
            // Define the 8 vertices of a cube (centered at origin, size 1.0)
            return new float[] {
                // Front face
                0.5f,  0.5f,  0.5f, // top-right front
                0.5f, -0.5f,  0.5f, // bottom-right front
                -0.5f, -0.5f,  0.5f, // bottom-left front
                -0.5f,  0.5f,  0.5f, // top-left front
                
                // Back face
                0.5f,  0.5f, -0.5f, // top-right back
                0.5f, -0.5f, -0.5f, // bottom-right back
                -0.5f, -0.5f, -0.5f, // bottom-left back
                -0.5f,  0.5f, -0.5f, // top-left back
            };
        }

        private static uint[] GenerateIndices()
        {
            // Define the 12 triangles (6 faces)
            return new uint[] {
                // Front face
                0, 1, 3, 1, 2, 3,
                
                // Back face
                4, 7, 5, 7, 6, 5,
                
                // Top face
                0, 3, 4, 3, 7, 4,
                
                // Bottom face
                1, 5, 2, 5, 6, 2,
                
                // Right face
                0, 4, 1, 4, 5, 1,
                
                // Left face
                3, 2, 7, 2, 6, 7
            };
        }

        private static float[] GenerateTexCoords()
        {
            // Texture coordinates for each vertex
            return new float[] {
                // Front face
                1.0f, 1.0f,
                1.0f, 0.0f,
                0.0f, 0.0f,
                0.0f, 1.0f,
                
                // Back face
                0.0f, 1.0f,
                0.0f, 0.0f,
                1.0f, 0.0f,
                1.0f, 1.0f,
            };
        }
    }
}