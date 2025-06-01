using System.Numerics;
using Silk.NET.OpenGL;

namespace RedLight.Graphics;

public class RLGraphics
{
    public GL OpenGL { get; set; }
    /*
     * other apis will be added later
     */
    
    public struct Colour
    {
        public float r;
        public float g;
        public float b;
        public float a;
    }
    
    public Mesh CreateMesh(float[] vertices, uint[] indices, RLShader vertexShader, RLShader fragmentShader)
    {
        return new Mesh(OpenGL, vertices, indices, vertexShader, fragmentShader);
    }

    public void Clear()
    {
        OpenGL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    public void ClearColour(Colour colour)
    {
        OpenGL.ClearColor(colour.r, colour.g, colour.b, colour.a);
    }

    public void UpdateTransform(Transformable<Mesh> Tmesh)
    {
        var mat = Tmesh.Transform;
        unsafe
        {
            float* ptr = (float*)&mat;
            OpenGL.UniformMatrix4(OpenGL.GetUniformLocation(Tmesh.Target.program, "transform"), 1, false, ptr);
        }
    }

    public void BindMesh(Mesh mesh)
    {
        OpenGL.BindVertexArray(mesh.vao);
        
        OpenGL.UseProgram(mesh.program);
    }

    public void BindTransformableMesh(Transformable<Mesh> mesh)
    {
        OpenGL.BindVertexArray(mesh.Target.vao);
        
        OpenGL.UseProgram(mesh.Target.program);
    }

    public void ActivateTexture()
    {
        OpenGL.ActiveTexture(TextureUnit.Texture0);
    }

    public void BindTexture(RLTexture rlTexture)
    {
        OpenGL.BindTexture(TextureTarget.Texture2D, rlTexture.Handle);
    }

    public void Draw(int lengthOfIndices)
    {
        unsafe
        {
            OpenGL.DrawElements(PrimitiveType.Triangles, (uint) lengthOfIndices, DrawElementsType.UnsignedInt, null);
        }
    }
}