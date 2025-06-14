using System.Drawing;
using System.Numerics;
using RedLight.Entities;
using RedLight.Lighting;
using RedLight.UI;
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
        OpenGL.ClearColor((float) colour.R / 256, (float) colour.G / 256, (float) colour.B / 256, (float) colour.A / 256);
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
    public void Update(Camera camera, Entity entity, bool applyLighting = true)
    {
        UpdateModel(entity);
        UpdateView(camera, entity);
        UpdateProjection(camera, entity);
    
        // Apply lighting to each mesh if the entity is using a lit shader
        if (applyLighting)
        {
            foreach (var mesh in entity.Model.Meshes)
            {
                // Check if this mesh is using a lit shader program
                // You might need to store shader information differently
                // For now, let's apply lighting to all meshes
                ApplyLightingToMesh(mesh.program, camera.Position);
            }
        }
    }
    
    public void UpdateAlt(Camera camera, Entity entity)
    {
        if (entity.Model?.Shader.Program == null)
        {
            Log.Error("Cannot update entity {EntityName} - no shader program", entity.Name ?? "Unknown");
            return;
        }

        var shaderProgram = entity.Model.Shader.Program;

        // Set transformation matrices
        shaderProgram.SetUniform("model", entity.ModelMatrix);
        shaderProgram.SetUniform("view", camera.View);
        shaderProgram.SetUniform("projection", camera.Projection);

        // CRITICAL: Apply lighting if this is the lit shader
        if (entity.Model.Shader.Name == "lit")
        {
            RedLight.Lighting.LightManager.Instance.ApplyLighting(shaderProgram, camera.Position);
            Log.Verbose("Applied lighting to entity {EntityName}", entity.Name ?? "Unknown");
        }
        else
        {
            Log.Verbose("Entity {EntityName} uses {ShaderName} shader - no lighting applied", 
                entity.Name ?? "Unknown", entity.Model.Shader.Name);
        }
    }
    
    /// <summary>
    /// Applies lighting uniforms to a specific mesh program
    /// </summary>
    /// <param name="program">OpenGL program handle</param>
    /// <param name="viewPosition">Camera position</param>
    private void ApplyLightingToMesh(uint program, Vector3 viewPosition, bool silent = true)
    {
        // Get the current program to restore it later
        OpenGL.GetInteger(GetPName.CurrentProgram, out int currentProgram);
        
        // Use the mesh program
        OpenGL.UseProgram(program);
        
        var directionalLight = LightManager.Instance.GetDirectionalLight();
        var pointLights = LightManager.Instance.GetPointLights();
        var firstPointLight = pointLights.FirstOrDefault();

        // Set much brighter ambient lighting and ALWAYS log (not just verbose)
        var ambientColor = new Vector3(0.3f, 0.3f, 0.4f);
        var ambientStrength = 0.5f;
        SetUniform(program, "ambientColor", ambientColor);
        SetUniform(program, "ambientStrength", ambientStrength);
        SetUniform(program, "viewPos", viewPosition);
        
        // ALWAYS log these for debugging (remove Log.Verbose, use Log.Debug)
        if (!silent) Log.Debug("=== APPLYING LIGHTING TO PROGRAM {Program} ===", program);
        if (!silent) Log.Debug("  Ambient: Color={AmbientColor}, Strength={AmbientStrength}", ambientColor, ambientStrength);
        if (!silent) Log.Debug("  View Position: {ViewPos}", viewPosition);

        // Set directional light (sun)
        if (directionalLight != null)
        {
            SetUniform(program, "directionalLight_direction", directionalLight.Direction);
            SetUniform(program, "directionalLight_color", directionalLight.Colour);
            SetUniform(program, "directionalLight_intensity", directionalLight.Intensity);
            if (!silent) Log.Debug("  Directional Light: Direction={Direction}, Color={Color}, Intensity={Intensity}", 
                directionalLight.Direction, directionalLight.Colour, directionalLight.Intensity);
        }
        else
        {
            SetUniform(program, "directionalLight_intensity", 0.0f);
            if (!silent) Log.Debug("  No directional light found - setting intensity to 0");
        }

        // Set point light (lamp)
        if (firstPointLight != null)
        {
            SetUniform(program, "pointLight_position", firstPointLight.Position);
            SetUniform(program, "pointLight_color", firstPointLight.Colour);
            SetUniform(program, "pointLight_intensity", firstPointLight.Intensity);
            SetUniform(program, "pointLight_constant", firstPointLight.Constant);
            SetUniform(program, "pointLight_linear", firstPointLight.Linear);
            SetUniform(program, "pointLight_quadratic", firstPointLight.Quadratic);
            if (!silent) Log.Debug("  Point Light: Position={Position}, Color={Color}, Intensity={Intensity}", 
                firstPointLight.Position, firstPointLight.Colour, firstPointLight.Intensity);
            if (!silent) Log.Debug("  Point Light Attenuation: Constant={Constant}, Linear={Linear}, Quadratic={Quadratic}", 
                firstPointLight.Constant, firstPointLight.Linear, firstPointLight.Quadratic);
        }
        else
        {
            SetUniform(program, "pointLight_intensity", 0.0f);
            if (!silent) Log.Debug("  No point light found - setting intensity to 0");
        }
        
        if (!silent) Log.Debug("=== FINISHED APPLYING LIGHTING ===");
        
        // Restore the previous program
        OpenGL.UseProgram((uint)currentProgram);
    }
    
    /// <summary>
    /// Helper method to set Vector3 uniforms directly on a program
    /// </summary>
    private void SetUniform(uint program, string name, Vector3 value)
    {
        int location = OpenGL.GetUniformLocation(program, name);
        if (location != -1)
        {
            OpenGL.Uniform3(location, value.X, value.Y, value.Z);
            Log.Verbose("    Set uniform '{UniformName}' = {Value}", name, value);
        }
        else
        {
            Log.Warning("Uniform '{UniformName}' not found in program {Program}", name, program);
        }
    }
    
    /// <summary>
    /// Helper method to set uniforms directly on a program
    /// </summary>
    private void SetUniform(uint program, string name, float value)
    {
        int location = OpenGL.GetUniformLocation(program, name);
        if (location != -1)
        {
            OpenGL.Uniform1(location, value);
            Log.Verbose("    Set uniform '{UniformName}' = {Value}", name, value);
        }
        else
        {
            Log.Warning("Uniform '{UniformName}' not found in program {Program}", name, program);
        }
    }

    /// <summary>
    /// Checks if there are any OpenGL errors. Best to use straight after an OpenGL function as it will check
    /// the latest error and log it. 
    /// </summary>
    public void CheckGlErrors()
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
    /// Debug method to check what uniforms are available in a shader program
    /// </summary>
    public void CheckUniformsInProgram(uint program)
    {
        var gl = OpenGL;
        gl.GetProgram(program, ProgramPropertyARB.ActiveUniforms, out int uniformCount);
    
        Log.Debug("    Active uniforms ({Count}):", uniformCount);
        for (uint i = 0; i < uniformCount; i++)
        {
            string name = gl.GetActiveUniform(program, i, out _, out _);
            Log.Debug("      - {UniformName}", name);
        }
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
    /// <param name="resourceName">string</param>
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
    /// <param name="screenSize"><see cref="Vector2"/></param>
    /// <param name="model"><see cref="Transformable{RLModel}"/></param>
    /// <returns>Player</returns>
    public Player MakePlayer(Vector2 screenSize, Transformable<RLModel> model)
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
        CheckGlErrors();
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