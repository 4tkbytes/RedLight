using System.Drawing;
using System.Numerics;
using ImGuiNET;
using RedLight.Core;
using RedLight.Input;
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
    public GL OpenGL { get; set; }

    // other graphics apis will be added later
    public bool IsRendering { get; private set; }
    public bool ShutUp { get; set; }

    private bool locked = false;

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

    public void Enable()
    {
        OpenGL.Enable(EnableCap.DepthTest);
        OpenGL.Enable(EnableCap.CullFace);
        OpenGL.CullFace(GLEnum.Back);
        OpenGL.FrontFace(GLEnum.Ccw);
    }

    public void Clear()
    {
        OpenGL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    public void ClearColour(Colour colour)
    {
        OpenGL.ClearColor(colour.r, colour.g, colour.b, colour.a);
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

    public void Update(Camera camera, Transformable<RLModel> Tmodel)
    {
        UpdateModel(Tmodel);
        UpdateView(camera, Tmodel);
        UpdateProjection(camera, Tmodel);
    }

    public void CheckGLErrors()
    {
        var err = OpenGL.GetError();
        if (err != 0)
            Log.Error("GL Error: {Error}", err);
    }

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

    public Transformable<RLModel> CreateModel(string resourceName, TextureManager textureManager, ShaderManager shaderManager, string name)
    {
        return new RLModel(this, RLFiles.CopyDirToTempAndGetEmbeddedResource(resourceName), textureManager, name)
            .AttachShader(shaderManager.Get("basic"))
            .MakeTransformable();
    }

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

    public void Draw(Transformable<RLModel> model)
    {
        model.Target.Draw();
        CheckGLErrors();
    }

    public void MakePlayer(Camera camera, Transformable<RLModel> model)
    {
        MakePlayer(camera, model, 5.0f);
    }

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

    public ImGuiController ImGuiLoad(RLWindow window, InputManager inputManager)
    {
        ImGuiController controller = new ImGuiController(
            OpenGL,
            window.Window,
            inputManager.input
        );

        return controller;
    }

    public void ImGuiRender(ImGuiController controller, double deltaTime, List<Transformable<RLModel>> objectModels)
    {
        controller.Update((float)deltaTime);

        var io = ImGui.GetIO();
        var windowSize = new System.Numerics.Vector2(350, io.DisplaySize.Y);
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(io.DisplaySize.X - windowSize.X, 0), ImGuiCond.Always);
        ImGui.SetNextWindowSize(windowSize, ImGuiCond.Always);

        ImGui.Begin("Scene Objects", ImGuiWindowFlags.AlwaysAutoResize);

        int idx = 0;
        foreach (var model in objectModels)
        {
            string header = $"{model.Target.Name}";
            if (ImGui.CollapsingHeader(header, ImGuiTreeNodeFlags.DefaultOpen))
            {
                // Extract current values from the matrix
                Matrix4X4.Decompose(model.Model, out var sc, out var rot, out var pos);
                var position = new Vector3(pos.X, pos.Y, pos.Z);
                var rotation = new Vector4(rot.W, rot.X, rot.Y, rot.Z);
                var scale = new Vector3(sc.X, sc.Y, sc.Z);

                bool changed = false;

                // Position sliders
                if (ImGui.SliderFloat3($"Position##{idx}", ref position, -10f, 10f))
                {
                    changed = true;
                }

                // Scale sliders
                bool scaleChanged = false;
                if (locked)
                {
                    // Only show one slider, and apply to all axes
                    float uniformScale = scale.X;
                    if (ImGui.SliderFloat($"Scale (Locked)##{idx}", ref uniformScale, 0.01f, 2f))
                    {
                        scale = new Vector3(uniformScale, uniformScale, uniformScale);
                        scaleChanged = true;
                    }
                }
                else
                {
                    if (ImGui.SliderFloat3($"Scale##{idx}", ref scale, 0.01f, 2f))
                    {
                        scaleChanged = true;
                    }
                }

                if (ImGui.Button(locked ? "Unlock Scale" : "Lock Scale"))
                {
                    locked = !locked;
                    Log.Debug("ImGui Scale Lock has been toggled [{A}]", locked);
                    if (locked)
                    {
                        scale = new Vector3(scale.X, scale.X, scale.X);
                        model.AbsoluteReset();
                        model.Scale(new Vector3D<float>(scale.X, scale.Y, scale.Z));
                    }
                }

                if (scaleChanged)
                {
                    changed = true;
                }

                // Rotation slider
                if (ImGui.SliderFloat4($"Yaw##{idx}", ref rotation, -180, 180))
                {
                    changed = true;
                }

                // Only update when something changed
                if (changed)
                {
                    model.AbsoluteReset(); // Reset to identity
                    model.Scale(new Vector3D<float>(scale.X, scale.Y, scale.Z));
                    // Convert quaternion to angle/axis for Rotate
                    var quat = new System.Numerics.Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W);
                    quat = System.Numerics.Quaternion.Normalize(quat);
                    float angle = 2.0f * (float)Math.Acos(quat.W);
                    float sinHalfAngle = (float)Math.Sqrt(1.0f - quat.W * quat.W);
                    Vector3D<float> axis;
                    if (sinHalfAngle < 0.001f)
                    {
                        // If the angle is small, use default axis
                        axis = Vector3D<float>.UnitY;
                    }
                    else
                    {
                        axis = new Vector3D<float>(
                            quat.X / sinHalfAngle,
                            quat.Y / sinHalfAngle,
                            quat.Z / sinHalfAngle
                        );
                    }
                    model.Rotate(angle, axis);
                    model.Translate(new Vector3D<float>(position.X, position.Y, position.Z));
                }
            }
            ImGui.Separator();
            idx++;
        }

        ImGui.End();
        controller.Render();
    }

}