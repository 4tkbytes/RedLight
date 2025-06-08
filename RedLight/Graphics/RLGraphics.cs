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
// ReSharper disable ReplaceWithSingleAssignment.False

namespace RedLight.Graphics;

public class RLGraphics
{
    public GL OpenGL { get; set; }

    // other graphics apis will be added later
    public bool IsRendering { get; private set; }
    public bool ShutUp { get; set; }

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

    public void Update(Camera camera, Transformable<RLModel> Tmodel)
    {
        UpdateModel(Tmodel);
        UpdateView(camera, Tmodel);
        UpdateProjection(camera, Tmodel);
    }

    public void Update(Camera camera, Transformable<Mesh> Tmesh)
    {
        UpdateModel(Tmesh);
        UpdateView(camera, Tmesh);
        UpdateProjection(camera, Tmesh);
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
        var thing = resourceName.Split(".");
        return new RLModel(this, RLFiles.GetResourcePath(resourceName), textureManager, name)
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

    public void ImGuiRender(ImGuiController controller, double deltaTime, List<Transformable<RLModel>> objectModels, Camera camera)
    {
        controller.Update((float)deltaTime);

        var io = ImGui.GetIO();
        var windowSize = new System.Numerics.Vector2(350, io.DisplaySize.Y);
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(io.DisplaySize.X - windowSize.X, 0), ImGuiCond.Always);
        ImGui.SetNextWindowSize(windowSize, ImGuiCond.Always);

        ImGui.Begin("Scene Objects", ImGuiWindowFlags.AlwaysAutoResize);

        // Model controls section
        int idx = 0;
        foreach (var model in objectModels)
        {
            string header = $"{model.Target.Name}";
            if (ImGui.CollapsingHeader(header, ImGuiTreeNodeFlags.DefaultOpen))
            {
                bool locked = false;

                // Extract current values from the matrix
                Matrix4X4.Decompose(model.Model, out var sc, out var rot, out var pos);
                var position = new Vector3(pos.X, pos.Y, pos.Z);
                var scale = new Vector3(sc.X, sc.Y, sc.Z);

                bool changed = false;

                // Position sliders
                if (ImGui.SliderFloat3($"Position##{idx}", ref position, -10f, 10f))
                {
                    changed = true;
                }
                ImGui.SameLine();
                if (ImGui.Button($"Reset Pos##{idx}"))
                {
                    position = new Vector3(0, 0, 0);
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
                    ImGui.SameLine();
                    if (ImGui.Button($"Reset Scale##{idx}"))
                    {
                        scale = new Vector3(1, 1, 1);
                        scaleChanged = true;
                    }
                }
                else
                {
                    if (ImGui.SliderFloat3($"Scale##{idx}", ref scale, 0.01f, 2f))
                    {
                        scaleChanged = true;
                    }
                    ImGui.SameLine();
                    if (ImGui.Button($"Reset Scale##{idx}"))
                    {
                        scale = new Vector3(1, 1, 1);
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
                        changed = true;
                    }
                }

                if (scaleChanged)
                {
                    changed = true;
                }

                if (ImGui.SliderFloat3($"Rotation (Pitch/Yaw/Roll)##{idx}", ref model.eulerAngles, -180f, 180f))
                {
                    changed = true;
                }

                ImGui.SameLine();
                if (ImGui.Button($"Reset Rot##{idx}"))
                {
                    model.eulerAngles = new Vector3(0, 0, 0);
                    changed = true;
                }

                if (changed)
                {
                    model.AbsoluteReset();
                    model.Scale(new Vector3D<float>(scale.X, scale.Y, scale.Z));

                    var rotationX = Quaternion.CreateFromAxisAngle(Vector3.UnitX, model.eulerAngles.X * MathF.PI / 180f);
                    var rotationY = Quaternion.CreateFromAxisAngle(Vector3.UnitY, model.eulerAngles.Y * MathF.PI / 180f);
                    var rotationZ = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, model.eulerAngles.Z * MathF.PI / 180f);

                    var finalRotation = rotationX * rotationY * rotationZ;

                    var rotMatrix = Matrix4X4.CreateFromQuaternion(new Quaternion<float>(
                        finalRotation.X, finalRotation.Y, finalRotation.Z, finalRotation.W));

                    model.SetModel(Matrix4X4.Multiply(rotMatrix, model.Model));
                    model.Translate(new Vector3D<float>(position.X, position.Y, position.Z));
                }
            }
            ImGui.Separator();
            idx++;
        }

        ImGui.Separator();
        if (ImGui.CollapsingHeader("Camera Controls", ImGuiTreeNodeFlags.DefaultOpen))
        {
            // Camera position control
            var cameraPos = new Vector3(camera.Position.X, camera.Position.Y, camera.Position.Z);
            if (ImGui.SliderFloat3("Camera Position", ref cameraPos, -20f, 20f))
            {
                camera.SetPosition(new Vector3D<float>(cameraPos.X, cameraPos.Y, cameraPos.Z));
            }

            // Camera speed control
            float cameraSpeed = camera.Speed;
            if (ImGui.SliderFloat("Camera Speed", ref cameraSpeed, 0.1f, 10.0f))
            {
                camera.SetSpeed(cameraSpeed);
            }

            // Camera orientation controls
            float yaw = camera.Yaw;
            float pitch = camera.Pitch;
            bool orientationChanged = false;

            if (ImGui.SliderFloat("Yaw", ref yaw, -180f, 180f))
            {
                camera.Yaw = yaw;
                orientationChanged = true;
            }

            if (ImGui.SliderFloat("Pitch", ref pitch, -89f, 89f))
            {
                camera.Pitch = pitch;
                orientationChanged = true;
            }

            if (orientationChanged)
            {
                // Update camera direction based on yaw and pitch
                Vector3D<float> direction = new Vector3D<float>();
                direction.X = float.Cos(float.DegreesToRadians(yaw)) * float.Cos(float.DegreesToRadians(pitch));
                direction.Y = float.Sin(float.DegreesToRadians(pitch));
                direction.Z = float.Sin(float.DegreesToRadians(yaw)) * float.Cos(float.DegreesToRadians(pitch));
                camera.SetFront(direction);
            }

            // Quick movement buttons
            if (ImGui.Button("Move Forward"))
            {
                camera.MoveForward(1.0f);
            }
            ImGui.SameLine();
            if (ImGui.Button("Move Back"))
            {
                camera.MoveBack(1.0f);
            }

            if (ImGui.Button("Move Left"))
            {
                camera.MoveLeft(1.0f);
            }
            ImGui.SameLine();
            if (ImGui.Button("Move Right"))
            {
                camera.MoveRight(1.0f);
            }

            // Reset camera button
            if (ImGui.Button("Reset Camera"))
            {
                // Reset to default values
                camera.SetPosition(new Vector3D<float>(0, 0, 3));
                camera.SetFront(new Vector3D<float>(0, 0, -1));
                camera.Yaw = 0;
                camera.Pitch = 0;
            }
        }

        ImGui.End();
        controller.Render();
    }

}