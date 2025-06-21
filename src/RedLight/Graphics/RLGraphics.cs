using System.Drawing;
using System.Numerics;
using RedLight.Entities;
using RedLight.UI;
using RedLight.Utils;
using Serilog;
using Silk.NET.OpenGL;

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
        string silly = "\u30fe\u0028\u2267\u25bd\u2266\u002a\u0029\u006f";
        Log.Information("This build is Debug, therefore OpenGL Debug Error callback will be enabled.");
        Log.Information("Expect performance decreases, however it will be way more easier to play around with!");
        Log.Information("Enjoy and have fun {kaomoji}", silly);

        OpenGL.Enable(GLEnum.DebugOutput);
        OpenGL.Enable(GLEnum.DebugOutputSynchronous);
        unsafe
        {
            OpenGL.DebugMessageCallback((source, type, id, severity, length, message, userParam) =>
            {
                string msg = Silk.NET.Core.Native.SilkMarshal.PtrToString((nint)message);
                Console.WriteLine($"[GL DEBUG] {msg}");
                
                var (programHandle, uniformLocation) = RLUtils.ExtractGLErrorInfo(msg);
                var shaderId = ShaderManager.Instance.GetShaderDetailsForDebugging((uint)programHandle);
                Console.WriteLine($"Error in shader: {shaderId}, program: {programHandle}, uniform location: {uniformLocation}");
            }, null);
        }
#else
        Log.Information("OpenGL Debug Error Callback can only work under a Debug build, therefore it will not work.");
        Log.Information("Instead, expect to see standard OpenGL errors (if there are any)!");
        Log.Information("Enjoy and have fun \u0028\u3065\uffe3\u0020\u0033\uffe3\u0029\u3065");
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
    public void ClearColour(Color colour)
    {
        OpenGL.ClearColor((float) colour.R/256, (float) colour.G/256, (float) colour.B/256, (float) colour.A/256);
    }

    /// <summary>
    /// Updates the projection model of an Entity
    /// </summary>
    /// <param name="camera">Camera</param>
    /// <param name="entity">Entity</param>
    public void UpdateProjection(Camera camera, Entity entity)
    {
        unsafe
        {
            var local = camera.Projection;
            float* ptr = (float*)&local;
            foreach (var mesh in entity.Model.Meshes)
            {
                int loc = OpenGL.GetUniformLocation(mesh.program, "projection");
                OpenGL.UniformMatrix4(loc, 1, false, ptr);
            }
        }
    }

    /// <summary>
    /// Updates the view of an Entity
    /// </summary>
    /// <param name="camera">Camera</param>
    /// <param name="entity">Entity</param>
    public void UpdateView(Camera camera, Entity entity)
    {
        unsafe
        {
            var local = camera.View;
            float* ptr = (float*)&local;
            foreach (var mesh in entity.Model.Meshes)
            {
                int loc = OpenGL.GetUniformLocation(mesh.program, "view");
                OpenGL.UniformMatrix4(loc, 1, false, ptr);
            }
        }
    }

    /// <summary>
    /// Updates the entity's positioning and other stuff
    /// </summary>
    /// <param name="entity">Entity</param>
    public void UpdateModel(Entity entity)
    {
        unsafe
        {
            foreach (var mesh in entity.Model.Meshes)
            {
                var local = entity.ModelMatrix;
                float* ptr = (float*)&local;
                int loc = OpenGL.GetUniformLocation(mesh.program, "model");
                OpenGL.UniformMatrix4(loc, 1, false, ptr);
            }
        }
    }

    /// <summary>
    /// Updates the model, view and projection all in one of an Entity. 
    /// </summary>
    /// <param name="camera">Camera</param>
    /// <param name="entity">Entity</param>
    public void Update(Camera camera, Entity entity)
    {
        UpdateModel(entity);
        UpdateView(camera, entity);
        UpdateProjection(camera, entity);
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
    public void LogVector(string type, Vector3 vector)
    {
        Log.Verbose("{A}: {X}, {Y}, {Z}", type, vector.X, vector.Y, vector.Z);
    }

    /// <summary>
    /// Logs a matrix (specifically a Matrix4X4 float) in verbose mode. 
    /// </summary>
    /// <param name="type">string</param>
    /// <param name="matrix">Matrix4X4</param>
    public void LogMatrix4(string type, Matrix4x4 matrix)
    {
        Log.Verbose("{A}: \n {B} {C} {D} {E}\n {F} {G} {H} {I} \n {J} {K} {L} {M} \n {N} {O} {P} {Q}\n",
            type,
            matrix.M11, matrix.M12, matrix.M13, matrix.M14,
            matrix.M21, matrix.M22, matrix.M23, matrix.M24,
            matrix.M31, matrix.M32, matrix.M33, matrix.M34,
            matrix.M41, matrix.M42, matrix.M43, matrix.M44);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="graphics"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">fsdajklfh</exception>
    /// <exception cref="ApplicationException">jfkldsaj;ljkl</exception>
    public RLGraphics Stupid(RLGraphics graphics)
    {
        if (graphics == null)
        {
            throw new ArgumentNullException(nameof(graphics));
        } else if (graphics.IsRendering == false)
        {
            throw new ApplicationException();
        }
        return this;
    }

    /// <summary>
    /// Enables the entity's shader program. 
    /// </summary>
    /// <param name="entity">Entity</param>
    public void Use(Entity entity)
    {
        var prog = entity.Model.Meshes.First().program;
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
    /// Creates a new model in an easier way. It creates a new RLModel from the resourceName, then attaches the "basic"
    /// shader and makes it Transformable. 
    /// </summary>
    /// <param name="resourceName">The resource name, not the directory such as
    /// "RedLight.Resources.Models.Maxwell.maxwell_the_cat.glb"</param>
    /// <param name="name">string</param>
    /// <returns>Transformable RLModel</returns>
    public Transformable<RLModel> CreateModel(string resourceName, string name)
    {
        return new RLModel(this, resourceName, TextureManager.Instance, name)
            .AttachShader(ShaderManager.Instance.Get("lit"))
            .MakeTransformable();
    }

    /// <summary>
    /// Converts a model into a player. This overload creates a new camera on your behalf, however a hitbox config
    /// is required. 
    /// </summary>
    /// <param name="screenSize"></param>
    /// <param name="model"></param>
    /// <param name="hitboxConfig"></param>
    /// <returns></returns>
    public Player MakePlayer(Vector2 screenSize, Transformable<RLModel> model, HitboxConfig hitboxConfig)
    {
        var camera = new Camera(screenSize);
        return new Player(camera, model, hitboxConfig);
    }
    
    /// <summary>
    /// Converts a model into a player.
    /// This specific overload includes a custom camera that can be parsed through and
    /// a required hitbox for the physics system. 
    /// </summary>
    /// <param name="camera"></param>
    /// <param name="model"></param>
    /// <param name="hitbox"></param>
    /// <returns></returns>
    public Player MakePlayer(Camera camera, Transformable<RLModel> model, HitboxConfig hitbox)
    {
        return new Player(camera, model, hitbox);
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
    /// Draws the entity using OpenGL. 
    /// </summary>
    /// <param name="entity">Entity</param>
    public void Draw(Entity entity)
    {
        entity.Model.Draw();
        CheckGLErrors();
    }

    /// <summary>
    /// This function adds entities to both list, specifically the ObjectModels list and the ImGui list
    /// in the case that it is required. It halves the amount of commands used and makes it simpler. 
    /// </summary>
    /// <param name="entities"><see cref="List{Entity}"/></param>
    /// <param name="imGui"><see cref="RLImGui"/></param>
    /// <param name="entity"><see cref="Entity"/></param>
    public void AddModels(List<Entity> entities, Entity entity)
    {
        entities.Add(entity);
        // imgui is broken so sad
    }
}