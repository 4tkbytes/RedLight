using RedLight.Core;
using Serilog;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

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

    // public Mesh CreateMesh(float[] vertices, uint[] indices, RLShader vertexShader, RLShader fragmentShader)
    // {
    //     return new Mesh(this, vertices, indices, vertexShader, fragmentShader);
    // }

    public void IsCaptured(IMouse mouse, bool isCaptured)
    {
        if (!isCaptured)
            mouse.Cursor.CursorMode = CursorMode.Normal;

        if (isCaptured)
            mouse.Cursor.CursorMode = CursorMode.Disabled;
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
    
    public void UpdateProjection(Camera camera, Transformable<RLModel> Tmodel)
    {
        unsafe
        {
            var local = camera.Projection;
            float* ptr = (float*)&local;
            foreach (var mesh in Tmodel.Target.Meshes)
            {
                int loc = OpenGL.GetUniformLocation(mesh.program, "projection");
                OpenGL.UniformMatrix4(loc, 1, false, ptr);
            }
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
    
    public void UpdateView(Camera camera, Transformable<RLModel> Tmodel)
    {
        unsafe
        {
            var local = camera.View;
            float* ptr = (float*)&local;
            foreach (var mesh in Tmodel.Target.Meshes)
            {
                int loc = OpenGL.GetUniformLocation(mesh.program, "view");
                OpenGL.UniformMatrix4(loc, 1, false, ptr);
            }
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
    
    public void UpdateModel(Transformable<RLModel> Tmodel)
    {
        unsafe
        {
            foreach (var mesh in Tmodel.Target.Meshes)
            {
                var local = Tmodel.Model;
                float* ptr = (float*)&local;
                int loc = OpenGL.GetUniformLocation(mesh.program, "model");
                OpenGL.UniformMatrix4(loc, 1, false, ptr);
            }
        }
    }
    
    public void CheckGLErrors()
    {
        var err = OpenGL.GetError();
        if (err != 0)
            Log.Error("GL Error: {Error}", err);
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
        Log.Verbose("[RLGraphics] Binding VAO: {VAO}", mesh.vao);
        OpenGL.BindVertexArray(mesh.vao);
        Log.Verbose("[RLGraphics] Using program: {Program}", mesh.program);
        OpenGL.UseProgram(mesh.program);
    }

    public void Use(Transformable<Mesh> mesh)
    {
        if (mesh.Target.program == 0)
        {
            Log.Error("[RLGraphics] Attempted to use invalid shader program (0)!");
            return;
        }
        Log.Verbose("[RLGraphics] Using program: {Program}", mesh.Target.program);
        OpenGL.UseProgram(mesh.Target.program);
    }

    public void Use(Transformable<RLModel> model)
    {
        var prog = model.Target.Meshes.First().program;
        if (prog == 0)
        {
            Log.Error("[RLGraphics] Attempted to use invalid shader program (0)!");
            return;
        }
        Log.Verbose("[RLGraphics] Using program: {Program}", prog);
        OpenGL.UseProgram(prog);
    }

    public bool IsRendering { get; private set; }

    public void Begin()
    {
        Log.Verbose("[RLGraphics] Begin frame");
        IsRendering = true;
    }

    public void End()
    {
        Log.Verbose("[RLGraphics] End frame");
        IsRendering = false;
    }

    public void Bind(Transformable<Mesh> mesh)
    {
        Log.Verbose("[RLGraphics] Binding VAO: {VAO}", mesh.Target.vao);
        OpenGL.BindVertexArray(mesh.Target.vao);
    }

    public void ActivateTexture()
    {
        Log.Verbose("[RLGraphics] Activating TextureUnit0");
        OpenGL.ActiveTexture(TextureUnit.Texture0);
    }

    public void BindTexture(RLTexture rlTexture)
    {
        Log.Verbose("[RLGraphics] Binding Texture: {Handle}", rlTexture.Handle);
        OpenGL.BindTexture(TextureTarget.Texture2D, rlTexture.Handle);
    }

    public void Draw(int lengthOfIndices)
    {
        Log.Verbose("[RLGraphics] Drawing {Count} indices", lengthOfIndices);
        unsafe
        {
            OpenGL.DrawElements(GLEnum.Triangles, (uint)lengthOfIndices, GLEnum.UnsignedInt, null);
        }
    }
    
    public void Draw(Transformable<RLModel> model)
    {
        // unsafe
        // {
        //     foreach (var mesh in model.Target.Meshes)
        //     {
        //         if (mesh.IndicesCount > 0)
        //         {
        //             Log.Verbose("[RLGraphics] Drawing mesh VAO: {VAO} with {Count} indices", mesh.vao, mesh.IndicesCount);
        //             OpenGL.BindVertexArray(mesh.vao);
        //             OpenGL.DrawElements(GLEnum.Triangles, (uint)mesh.IndicesCount, GLEnum.UnsignedInt, null);
        //         }
        //     }
        //     OpenGL.BindVertexArray(0);
        // }
        model.Target.Draw();
    }
}