using System.Drawing;
using System.Numerics;
using ImGuiNET;
using RedLight.Core;
using RedLight.Input;
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
    /// Extra debugging information for model loading
    /// </summary>
    public bool ShutUp { get; set; }

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
    /// Checks if a mouse is captured and changes the cursor mode.
    ///
    /// If the mouse is captured, it will change it to CursorMode.Disabled. If it
    /// is not disabled, it will change it to CursorMode.Normal.  
    /// </summary>
    /// <param name="mouse">IMouse</param>
    /// <param name="isCaptured">bool</param>
    public void IsCaptured(IMouse mouse, bool isCaptured)
    {
        if (!isCaptured)
            mouse.Cursor.CursorMode = CursorMode.Normal;

        if (isCaptured)
            mouse.Cursor.CursorMode = CursorMode.Disabled;
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
                var local = Tmodel.Model;
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
            var local = Tmesh.Model;
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
    public Transformable<RLModel> CreateModel(string resourceName, TextureManager textureManager, ShaderManager shaderManager, string name)
    {
        return new RLModel(this, RLFiles.GetResourcePath(resourceName), textureManager, name)
            .AttachShader(shaderManager.Get("basic"))
            .MakeTransformable();
    }

    /// <summary>
    /// Starts rendering the frame and enables IsRendering boolean
    /// </summary>
    public void Begin()
    {
        Log.Verbose("[RLGraphics] Begin frame");
        IsRendering = true;
    }

    /// <summary>
    /// Stops rendering the frame and disables the IsRendering boolean. 
    /// </summary>
    public void End()
    {
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
    /// Makes a model a player by setting the camera as third person. It is very broken and buddy so it is to be changed.
    ///
    /// Sets the distance a default of 5.0f
    /// </summary>
    /// <param name="camera">Camera</param>
    /// <param name="model">Transformable Model</param>
    public void MakePlayer(Camera camera, Transformable<RLModel> model)
    {
        MakePlayer(camera, model, 5.0f);
    }

    /// <summary>
    /// Makes a model a player by setting the camera as third person. It is very broken and buggy so it is to be changes
    ///
    /// You are able to change the players distance. 
    /// </summary>
    /// <param name="camera">Camera</param>
    /// <param name="model">Transformable Model</param>
    /// <param name="distance">float</param>
    public void MakePlayer(Camera camera, Transformable<RLModel> model, float distance)
    {
        // Camera position and forward direction
        var camPos = camera.Position;
        var camForward = camera.Front; // Assuming your Camera class has a .Front property (normalized direction)
        var targetPos = camPos + camForward * distance;
        // Reset and set the new position
        model.AbsoluteReset();
        model.Translate(targetPos);
    }

    public void AddModels(List<Transformable<RLModel>> models, RLImGui imGui, Transformable<RLModel> model)
    {
        models.Add(model);
        imGui.AddModels(model);
    }
}