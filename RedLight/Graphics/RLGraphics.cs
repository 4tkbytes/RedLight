using System.Drawing;
using System.Numerics;
using ImGuiNET;
using RedLight.Core;
using RedLight.Graphics.Primitive;
using RedLight.Input;
using RedLight.Physics;
using RedLight.UI;
using RedLight.Utils;
using Serilog;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace RedLight.Graphics;

public class RLGraphics
{
    /// <summary>
    /// OpenGL rendering API. 
    /// </summary>
    public GL OpenGL { get; set; }

    public bool IsRendering { get; private set; }

    /// <summary>
    /// Extra debugging information for model loading. By default, it is set to true
    /// to improve performance and decrease the load/info from the logger. 
    /// </summary>
    public bool ShutUp { get; set; } = true;

    /// <summary>
    /// Struct containing colour. There's probably better alternatives with better support but this works for me. 
    /// </summary>
    public struct Colour
    {
        /// <summary>
        /// red
        /// </summary>
        public float r;
        
        /// <summary>
        /// green
        /// </summary>
        public float g;
        
        /// <summary>
        /// blue
        /// </summary>
        public float b;
        
        /// <summary>
        /// alpha
        /// </summary>
        public float a;
    }

    /// <summary>
    /// Enabled OpenGL's Depth Text, culls the back faces and internal faces from textures. 
    /// </summary>
    public void Enable()
    {
        OpenGL.Enable(EnableCap.DepthTest);
        OpenGL.Enable(EnableCap.CullFace);
        OpenGL.CullFace(GLEnum.Back);
        OpenGL.FrontFace(GLEnum.Ccw);
    }

    /// <summary>
    /// Enabled OpenGL Debug Error Callback. This is to be only used for debugging purposes, as it
    /// can tank performance by a decent amount. 
    /// </summary>
    public void EnableDebugErrorCallback()
    {
#if DEBUG
        Log.Information("This build is Debug, therefore OpenGL Debug Error callback will be enabled.");
        Log.Information("Expect performance decreases, however it will be way more easier to play around with!");
        Log.Information("Enjoy and have fun ヾ(≧▽≦*)o");

        OpenGL.Enable(GLEnum.DebugOutput);
        OpenGL.Enable(GLEnum.DebugOutputSynchronous);
        unsafe
        {
            OpenGL.DebugMessageCallback((source, type, id, severity, length, message, userParam) =>
            {
                string msg = Silk.NET.Core.Native.SilkMarshal.PtrToString((nint)message);
                Console.WriteLine($"[GL DEBUG] {msg}");
            }, null);
        }
#else
        Log.Information("OpenGL Debug Error Callback can only work under a Debug build, therefore it will not work.");
        Log.Information("Instead, expect to see standard OpenGL errors (if there are any)!");
        Log.Information("Enjoy and have fun (づ￣ 3￣)づ");
#endif
    }

    /// <summary>
    /// Clears the screen
    /// </summary>
    public void Clear()
    {
        OpenGL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    /// <summary>
    /// Clears the screen with a colour
    /// </summary>
    /// <param name="colour">Graphics.Colour</param>
    public void ClearColour(Colour colour)
    {
        OpenGL.ClearColor(colour.r, colour.g, colour.b, colour.a);
    }

    /// <summary>
    /// Updates the projection model of a RLModel
    /// </summary>
    /// <param name="camera">Camera</param>
    /// <param name="Tmodel"></param>
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

    /// <summary>
    /// Updates the view of a RLModel
    /// </summary>
    /// <param name="camera">Camera</param>
    /// <param name="Tmodel"></param>
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

    /// <summary>
    /// Updates the model's positioning and other stuff
    /// </summary>
    /// <param name="Tmodel"></param>
    public void UpdateModel(Transformable<RLModel> Tmodel)
    {
        unsafe
        {
            foreach (var mesh in Tmodel.Target.Meshes)
            {
                var local = Tmodel.ModelMatrix;
                float* ptr = (float*)&local;
                int loc = OpenGL.GetUniformLocation(mesh.program, "model");
                OpenGL.UniformMatrix4(loc, 1, false, ptr);
            }
        }
    }

    /// <summary>
    /// Updates the projection of a mesh
    /// </summary>
    /// <param name="camera"></param>
    /// <param name="Tmesh"></param>
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
    
    /// <summary>
    /// Updates the view of a mesh
    /// </summary>
    /// <param name="camera"></param>
    /// <param name="Tmesh"></param>
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

    /// <summary>
    /// Updates the model of a mesh
    /// </summary>
    /// <param name="Tmesh"></param>
    public void UpdateModel(Transformable<Mesh> Tmesh)
    {
        unsafe
        {
            var local = Tmesh.ModelMatrix;
            float* ptr = (float*)&local;
            int loc = OpenGL.GetUniformLocation(Tmesh.Target.program, "model");
            OpenGL.UniformMatrix4(loc, 1, false, ptr);
        }
    }

    /// <summary>
    /// Updates the model, view and projection all in one of a Transformable Model. 
    /// </summary>
    /// <param name="camera"></param>
    /// <param name="Tmodel"></param>
    public void Update(Camera camera, Transformable<RLModel> Tmodel)
    {
        UpdateModel(Tmodel);
        UpdateView(camera, Tmodel);
        UpdateProjection(camera, Tmodel);
    }
    
    /// <summary>
    /// Updates the model, view and projection all in one of a Transformable Mesh. 
    /// </summary>
    /// <param name="camera"></param>
    /// <param name="Tmesh"></param>
    public void Update(Camera camera, Transformable<Mesh> Tmesh)
    {
        UpdateModel(Tmesh);
        UpdateView(camera, Tmesh);
        UpdateProjection(camera, Tmesh);
    }

    /// <summary>
    /// Checks if there are any OpenGL errors. Best to use straight after an OpenGL function as it will check
    /// the latest error and log it. 
    /// </summary>
    public void CheckGLErrors()
    {
        var err = OpenGL.GetError();
        if (err != 0)
            Log.Error("GL Error: {Error}", err);
    }

    /// <summary>
    /// Logs a vector (specifically a Vector3D float) in verbose mode. 
    /// </summary>
    /// <param name="type">string</param>
    /// <param name="vector">Vector3D</param>
    public void LogVector(string type, Vector3D<float> vector)
    {
        Log.Verbose("{A}: {X}, {Y}, {Z}", type, vector.X, vector.Y, vector.Z);
    }

    /// <summary>
    /// Logs a matrix (specifically a Matrix4X4 float) in verbose mode. 
    /// </summary>
    /// <param name="type">string</param>
    /// <param name="matrix">Matrix4X4</param>
    public void LogMatrix4(string type, Matrix4X4<float> matrix)
    {
        Log.Verbose("{A}: \n {B} {C} {D} {E}\n {F} {G} {H} {I} \n {J} {K} {L} {M} \n {N} {O} {P} {Q}\n",
            type,
            matrix.M11, matrix.M12, matrix.M13, matrix.M14,
            matrix.M21, matrix.M22, matrix.M23, matrix.M24,
            matrix.M31, matrix.M32, matrix.M33, matrix.M34,
            matrix.M41, matrix.M42, matrix.M43, matrix.M44);
    }

    /// <summary>
    /// Enables the mesh's shader program. 
    /// </summary>
    /// <param name="mesh"></param>
    public void Use(Transformable<Mesh> mesh)
    {
        if (mesh.Target.program == 0)
        {
            Log.Error("[RLGraphics] Attempted to use invalid shader program (0)!");
            return;
        }
        if (!ShutUp)
            Log.Verbose("[RLGraphics] Using program: {Program}", mesh.Target.program);
        OpenGL.UseProgram(mesh.Target.program);
    }

    /// <summary>
    /// Enables the model's shader program. 
    /// </summary>
    /// <param name="model"></param>
    public void Use(Transformable<RLModel> model)
    {
        var prog = model.Target.Meshes.First().program;
        if (prog == 0)
        {
            Log.Error("[RLGraphics] Attempted to use invalid shader program (0)!");
            return;
        }
        if (!ShutUp)
            Log.Verbose("[RLGraphics] Using program: {Program}", prog);
        OpenGL.UseProgram(prog);
    }

    /// <summary>
    /// Creates a new model in an easier way. It creates a new RLModel from the resouceName, then attaches the "basic"
    /// shader and makes it Transformable. 
    /// </summary>
    /// <param name="resourceName">string</param>
    /// <param name="textureManager">TextureManager</param>
    /// <param name="shaderManager">ShaderManager</param>
    /// <param name="name">string</param>
    /// <returns>Transformable RLModel</returns>
    public Transformable<RLModel> CreateModel(string resourceName, string name)
    {
        return new RLModel(this, resourceName, TextureManager.Instance, name)
            .AttachShader(ShaderManager.Instance.Get("basic"))
            .MakeTransformable();
    }

    /// <summary>
    /// Converts a model into a player. This overload creates a new camera on your behalf. 
    /// </summary>
    /// <param name="screenSize"><see cref="Vector2D"/></param>
    /// <param name="model"><see cref="Transformable{RLModel}"/></param>
    /// <returns></returns>
    public Player MakePlayer(Vector2D<int> screenSize, Transformable<RLModel> model)
    {
        var camera = new Camera(screenSize);
        return new Player(camera, model);
    }

    /// <summary>
    /// Converts a model into a player. This specific overload includes a custom camera that can be parsed through. 
    /// </summary>
    /// <param name="camera"><see cref="Camera"/></param>
    /// <param name="model"><see cref="Transformable{RLModel}"/></param>
    /// <returns><see cref="Player"/></returns>
    public Player MakePlayer(Camera camera, Transformable<RLModel> model)
    {
        return new Player(camera, model);
    }

    /// <summary>
    /// Starts rendering the frame and enables IsRendering boolean
    /// </summary>
    public void Begin()
    {
        if (!ShutUp)
            Log.Verbose("[RLGraphics] Begin frame");
        IsRendering = true;
    }

    /// <summary>
    /// Stops rendering the frame and disables the IsRendering boolean. 
    /// </summary>
    public void End()
    {
        if (!ShutUp)
            Log.Verbose("[RLGraphics] End frame");
        IsRendering = false;
    }

    /// <summary>
    /// Draws the model using OpenGL. 
    /// </summary>
    /// <param name="model">Transformable RLModel</param>
    public void Draw(Transformable<RLModel> model)
    {
        model.Target.Draw();
        CheckGLErrors();
    }

    /// <summary>
    /// This function adds models to both list, specifically the ObjectModels list and the ImGui list
    /// in the case that it is required. It halves the amount of commands used and makes it simpler. 
    /// </summary>
    /// <param name="models"><see cref="List{Entity{Transformable{RLModel}}}"/></param>
    /// <param name="imGui"><see cref="RLImGui"/></param>
    /// <param name="model"><see cref="Transformable{RLModel}"/></param>
    public void AddModels(List<Entity<Transformable<RLModel>>> models, RLImGui imGui, Entity<Transformable<RLModel>> model)
    {
        models.Add(model);
        imGui.AddModels(model);
    }
}