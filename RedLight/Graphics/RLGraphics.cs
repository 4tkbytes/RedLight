using Serilog;
using Silk.NET.Maths;
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
        Log.Debug("Mesh created successfully");
    }

    public void EnableDepth()
    {
        OpenGL.Enable(EnableCap.DepthTest);
    }

    public void Clear()
    {
        OpenGL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    public void ClearColour(Colour colour)
    {
        OpenGL.ClearColor(colour.r, colour.g, colour.b, colour.a);
    }

    public void UpdateProjection(Camera camera, Transformable<Mesh> Tmesh)
    {
        unsafe
        {
            var local = camera.Projection;
            float* ptr = (float*)&local;
            int loc = OpenGL.GetUniformLocation(Tmesh.Target.program, "projection");
            OpenGL.UniformMatrix4(loc, 1, false, ptr);
        }
    }

    public void UpdateView(Camera camera, Transformable<Mesh> Tmesh)
    {
        unsafe
        {
            var local = camera.View;
            float* ptr = (float*)&local;
            int loc = OpenGL.GetUniformLocation(Tmesh.Target.program, "view");
            OpenGL.UniformMatrix4(loc, 1, false, ptr);
        }
    }

    public void UpdateModel(Transformable<Mesh> Tmesh)
    {
        unsafe
        {
            var local = Tmesh.Model;
            float* ptr = (float*)&local;
            int loc = OpenGL.GetUniformLocation(Tmesh.Target.program, "model");
            OpenGL.UniformMatrix4(loc, 1, false, ptr);
        } 
    }

    // public Vector3D<float> MeshToVector(Transformable<Mesh> Tmesh)
    // {
    //     var view = Tmesh.View;
    //     Matrix4X4.Invert(view, out var inverseView);
    //     Matrix4X4.Decompose(inverseView, out _, out _, out var cameraPos);
    //     return cameraPos;
    // }

    public void LogVector(string type, Vector3D<float> vector)
    {
        Log.Verbose("{A}: {X}, {Y}, {Z}", type, vector.X, vector.Y, vector.Z);
    }

    public void LogMatrix4(string type, Matrix4X4<float> matrix)
    {
        Log.Verbose("{A}: \n {B} {C} {D} {E}\n {F} {G} {H} {I} \n {J} {K} {L} {M} \n {N} {O} {P} {Q}\n",
            type,
            matrix.M11, matrix.M12, matrix.M13, matrix.M14,
            matrix.M21, matrix.M22, matrix.M23, matrix.M24,
            matrix.M31, matrix.M32, matrix.M33, matrix.M34,
            matrix.M41, matrix.M42, matrix.M43, matrix.M44);
    }

    public void BindMesh(Mesh mesh)
    {
        OpenGL.BindVertexArray(mesh.vao);
        
        OpenGL.UseProgram(mesh.program);
    }

    public void Use(Transformable<Mesh> mesh)
    {
        OpenGL.UseProgram(mesh.Target.program);
    }

    public void Bind(Transformable<Mesh> mesh)
    {
        OpenGL.BindVertexArray(mesh.Target.vao);
    }

    public void ActivateTexture()
    {
        OpenGL.ActiveTexture(TextureUnit.Texture0);
    }

    public void BindTexture(RLTexture rlTexture)
    {
        OpenGL.BindTexture(TextureTarget.Texture2D, rlTexture.Handle);
    }

    public void Draw(/*int lengthOfIndices*/)
    {
        unsafe
        {
            OpenGL.DrawArrays(GLEnum.Triangles, 0, 36);
        }
    }
}