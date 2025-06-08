using System.Numerics;
using ImGuiNET;
using RedLight.Core;
using RedLight.Graphics;
using RedLight.Input;
using RedLight.Scene;
using Serilog;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;

namespace RedLight.UI;

public class RLImGui
{
    private RLGraphics graphics;
    private RLWindow window;
    private InputManager inputManager;
    private TextureManager textureManager;
    private ShaderManager shaderManager;
    private SceneManager sceneManager;
    
    private string _filterText = string.Empty;
    private string _inputBuffer = string.Empty;
    private bool _autoScroll = true;
    
    public ConsoleLog Console { get; private set; } = new ConsoleLog();
    
    public ImGuiController Controller { get; private set; }
    public List<Transformable<RLModel>> ImGuiRenderingObjects { get; private set; }
    
    /// <summary>
    /// This function enables Dear ImGui and uses it in an idiomatic way. This is the ctor which creates and
    /// initialises ImGui and their functions. 
    /// </summary>
    /// <param name="graphics"></param>
    /// <param name="window"></param>
    /// <param name="inputManager"></param>
    public RLImGui(RLGraphics graphics, RLWindow window, InputManager inputManager, ShaderManager shaderManager, TextureManager textureManager, SceneManager sceneManager)
    {
        this.graphics = graphics;
        this.window = window;
        this.inputManager = inputManager;
        this.shaderManager = shaderManager;
        this.textureManager = textureManager;
        this.sceneManager = sceneManager;
        
        Controller = new ImGuiController(
            graphics.OpenGL,
            window.Window,
            inputManager.input
        );
        
        ImGuiRenderingObjects = new List<Transformable<RLModel>>();
    }

    /// <summary>
    /// A purely idiomatic way of rendering ImGui menus.
    ///
    /// Function contains:
    ///     - Support for editing object positions and rotations.
    ///     - Camera manipulation
    /// </summary>
    /// <param name="deltaTime"></param>
    /// <param name="camera"></param>
    public void Render(double deltaTime, Camera camera)
    {
        var controller = Controller;
        
        controller.Update((float)deltaTime);
        
        RenderConsole();

        var io = ImGui.GetIO();
        var windowSize = new System.Numerics.Vector2(350, io.DisplaySize.Y);
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(io.DisplaySize.X - windowSize.X, 0), ImGuiCond.Always);
        ImGui.SetNextWindowSize(windowSize, ImGuiCond.Always);

        ImGui.Begin("Scene Objects", ImGuiWindowFlags.AlwaysAutoResize);

        // Model controls section
        int idx = 0;
        foreach (var model in ImGuiRenderingObjects)
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
    
    /// <summary>
    /// Renders a console window with command input and log display
    /// </summary>
    public void RenderConsole()
    {
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(500, 300), ImGuiCond.FirstUseEver);
        if (!ImGui.Begin("Console"))
        {
            ImGui.End();
            return;
        }

        // Options menu
        if (ImGui.BeginPopup("Options"))
        {
            ImGui.Checkbox("Auto-scroll", ref _autoScroll);
            ImGui.EndPopup();
        }

        // Options, Filter, Clear buttons
        if (ImGui.Button("Options"))
            ImGui.OpenPopup("Options");
        ImGui.SameLine();
        bool clearButton = ImGui.Button("Clear");
        ImGui.SameLine();
        bool copyButton = ImGui.Button("Copy");
        ImGui.SameLine();
        
        // Replace filter.Draw with InputText for filtering
        ImGui.InputText("Filter", ref _filterText, 256);

        ImGui.Separator();

        // Content area - updated to use ImGuiChildFlags instead of bool
        ImGui.BeginChild("ScrollingRegion", new System.Numerics.Vector2(0, -ImGui.GetFrameHeightWithSpacing()),
            ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar);

        if (ImGui.BeginPopupContextWindow())
        {
            if (ImGui.Selectable("Clear")) clearButton = true;
            if (ImGui.Selectable("Copy")) copyButton = true;
            ImGui.EndPopup();
        }

        // Display logs
        if (clearButton)
            Console.Clear();

        if (copyButton)
            ImGui.LogToClipboard();

        foreach (var item in Console.Logs)
        {
            // Simple filter implementation
            if (!string.IsNullOrEmpty(_filterText) && !item.Contains(_filterText, StringComparison.OrdinalIgnoreCase))
                continue;

            // Color different log levels
            if (item.Contains("[ERR]"))
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.4f, 0.4f, 1.0f));
            else if (item.Contains("[WRN]"))
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.8f, 0.6f, 1.0f));
            else if (item.Contains("[INF]"))
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.8f, 0.8f, 1.0f, 1.0f));
            else
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.8f, 0.8f, 0.8f, 1.0f));

            ImGui.TextUnformatted(item);
            ImGui.PopStyleColor();
        }

        if (_autoScroll && ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
            ImGui.SetScrollHereY(1.0f);

        ImGui.EndChild();
        ImGui.Separator();

        bool reclaimFocus = false;
        if (ImGui.InputText("Command", ref _inputBuffer, 1024,
                ImGuiInputTextFlags.EnterReturnsTrue))
        {
            if (!string.IsNullOrWhiteSpace(_inputBuffer))
            {
                ExecuteCommand(_inputBuffer);
                Console.AddCommand(_inputBuffer);
                _inputBuffer = string.Empty;
            }
            reclaimFocus = true;
        }

        // Manual keyboard navigation handling (replacing callback)
        if (ImGui.IsItemFocused())
        {
            if (ImGui.IsKeyPressed(ImGuiKey.UpArrow))
            {
                _inputBuffer = Console.GetPreviousCommand(_inputBuffer);
            }
            else if (ImGui.IsKeyPressed(ImGuiKey.DownArrow))
            {
                _inputBuffer = Console.GetNextCommand();
            }
        }

        // Auto-focus on window apparition
        ImGui.SetItemDefaultFocus();
        if (reclaimFocus)
            ImGui.SetKeyboardFocusHere(-1);

        ImGui.End();
    }

    private void ExecuteCommand(string command)
    {
        AddLog($"> {command}");

        // Parse and execute commands here
        if (command.Equals("clear", StringComparison.OrdinalIgnoreCase))
        {
            Console.Clear();
        }
        else if (command.StartsWith("help", StringComparison.OrdinalIgnoreCase))
        {
            AddLog("Commands:");
            AddLog("  clear - Clear console");
            AddLog("  help - Show help");
            AddLog("  model create <resource_path> [model_name] - Create a new model");
            AddLog("  model delete <model_name> - Delete a model");
            AddLog("  model texture override <model_name> <mesh_name> <textureID> - Override texture");
            AddLog("  model texture list all - List all textures in the manager");
            AddLog("  model texture list <model_name> - List all textures used by a model");
            AddLog("  model texture dump <model_name> - Dump all textures used by a model to its resource folder");
        }
        else if (command.StartsWith("model", StringComparison.OrdinalIgnoreCase))
        {
            var parts = command.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                AddLog("Available model commands: create, delete, texture");
                return;
            }

            var subCommand = parts[1].ToLowerInvariant();
            switch (subCommand)
            {
                case "create":
                    HandleModelCreate(parts);
                    break;
                case "delete":
                    HandleModelDelete(parts);
                    break;
                case "texture":
                    if (parts.Length >= 3)
                    {
                        var texSub = parts[2].ToLowerInvariant();
                        if (texSub == "override")
                            HandleModelTextureOverride(parts);
                        else if (texSub == "list")
                            HandleModelTextureList(parts);
                        else if (texSub == "dump")
                            HandleModelTextureDump(parts);
                        else
                            AddLog("Usage: model texture [override|list|dump] ...");
                    }
                    else
                    {
                        AddLog("Usage: model texture [override|list|dump] ...");
                    }
                    break;
                default:
                    AddLog($"Unknown model subcommand: {subCommand}");
                    AddLog("Available model commands: create, delete, texture");
                    break;
            }
        }
        else
        {
            AddLog($"Unknown command: {command}");
        }
    }

    private void AddLog(string message)
    {
        if (message.Contains("[WRN]"))
        {
            Log.Warning(message.Replace("[WRN]", "").Trim());
        }
        else if (message.Contains("[ERR]"))
        {
            Log.Error(message.Replace("[ERR]", "").Trim());
        }
        else if (message.Contains("> "))
        {
            Log.Information(message);
        }
        else
        {
            Log.Debug(message);
        }
    }

    private void HandleModelTextureList(string[] parts)
    {
        if (parts.Length == 4 && parts[3].Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            AddLog("All textures in TextureManager:");
            foreach (var kvp in textureManager.textures)
            {
                var tex = kvp.Value;
                AddLog($"  - {kvp.Key} (Type: {tex.Type}, Path: {tex.Path})");
            }
            return;
        }
        if (parts.Length == 4)
        {
            var modelName = parts[3];
            var model = ImGuiRenderingObjects.FirstOrDefault(m => m.Target.Name.Equals(modelName, StringComparison.OrdinalIgnoreCase));
            if (model == null)
            {
                AddLog($"[ERR] Model '{modelName}' not found");
                return;
            }
            var usedTextures = model.Target.Meshes
                .SelectMany(mesh => typeof(Mesh).GetField("textures", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(mesh) as List<RLTexture> ?? new List<RLTexture>())
                .Distinct()
                .ToList();

            AddLog($"Textures used by model '{modelName}':");
            foreach (var tex in usedTextures)
            {
                AddLog($"  - {tex.Name} (Type: {tex.Type}, Path: {tex.Path})");
            }
            return;
        }
        AddLog("Usage: model texture list all");
        AddLog("       model texture list <model_name>");
    }

    private void HandleModelTextureDump(string[] parts)
    {
        if (parts.Length != 4)
        {
            AddLog("Usage: model texture dump <model_name>");
            return;
        }
        var modelName = parts[3];
        var model = ImGuiRenderingObjects.FirstOrDefault(m => m.Target.Name.Equals(modelName, StringComparison.OrdinalIgnoreCase));
        if (model == null)
        {
            AddLog($"[ERR] Model '{modelName}' not found");
            return;
        }
        var modelDir = model.Target.Directory;
        if (string.IsNullOrEmpty(modelDir) || !Directory.Exists(modelDir))
        {
            AddLog($"[ERR] Model directory '{modelDir}' not found or invalid");
            return;
        }

        var usedTextures = model.Target.Meshes
            .SelectMany(mesh => typeof(Mesh).GetField("textures", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(mesh) as List<RLTexture> ?? new List<RLTexture>())
            .Distinct()
            .ToList();

        int dumped = 0;
        foreach (var tex in usedTextures)
        {
            try
            {
                // Only dump if the texture has a valid file path
                if (!string.IsNullOrEmpty(tex.Path) && File.Exists(tex.Path))
                {
                    var fileName = Path.GetFileName(tex.Path);
                    var destPath = Path.Combine(modelDir, fileName);
                    File.Copy(tex.Path, destPath, true);
                    AddLog($"Dumped texture '{tex.Name}' to '{destPath}'");
                    dumped++;
                }
                else
                {
                    AddLog($"[WRN] Texture '{tex.Name}' does not have a valid file path or is embedded.");
                }
            }
            catch (Exception ex)
            {
                AddLog($"[ERR] Failed to dump texture '{tex.Name}': {ex.Message}");
            }
        }
        AddLog($"Dumped {dumped} textures to '{modelDir}'");
    }

    private void HandleModelCreate(string[] parts)
    {
        if (parts.Length < 3)
        {
            AddLog("Usage: model create <resource_path> [model_name]");
            return;
        }

        var resourcePath = parts[2];
        var modelName = parts.Length > 3 ? parts[3] : Path.GetFileNameWithoutExtension(resourcePath);

        try
        {
            AddLog($"Creating model from: {resourcePath}");
            var model = graphics.CreateModel(resourcePath, textureManager, shaderManager, modelName);
            
            sceneManager.GetCurrentScene().ObjectModels.Add(model);
            ImGuiRenderingObjects.Add(model);
            AddLog($"Model '{modelName}' added to scene and UI controls");
        }
        catch (Exception ex)
        {
            AddLog($"[ERR] Failed to create model: {ex.Message}");
        }
    }
    
    private void HandleModelDelete(string[] parts)
    {
        var scene = sceneManager.GetCurrentScene();
        
        if (parts.Length < 3)
        {
            AddLog("Usage: model delete <model_name>");
            return;
        }

        var modelName = parts[2];
        bool modelFound = false;

        // Find and remove from ImGui rendering objects
        for (int i = ImGuiRenderingObjects.Count - 1; i >= 0; i--)
        {
            if (ImGuiRenderingObjects[i].Target.Name.Equals(modelName, StringComparison.OrdinalIgnoreCase))
            {
                ImGuiRenderingObjects.RemoveAt(i);
                modelFound = true;
            }
        }

        for (int i = scene.ObjectModels.Count - 1; i >= 0; i--)
        {
            if (scene.ObjectModels[i].Target.Name.Equals(modelName, StringComparison.OrdinalIgnoreCase))
            {
                scene.ObjectModels.RemoveAt(i);
                modelFound = true;
            }
        }

        if (modelFound)
            AddLog($"Model '{modelName}' deleted");
        else
            AddLog($"[WRN] Model '{modelName}' not found");
    }
    
    private void HandleModelTextureOverride(string[] parts)
    {
        if (parts.Length < 6)
        {
            AddLog("Usage: model texture override <model_name> <mesh_name> <textureID>");
            return;
        }

        var modelName = parts[3];
        var meshName = parts[4];
        var textureId = parts[5];

        // Find the model
        var model = ImGuiRenderingObjects.FirstOrDefault(m => 
            m.Target.Name.Equals(modelName, StringComparison.OrdinalIgnoreCase));

        if (model == null)
        {
            AddLog($"[ERR] Model '{modelName}' not found");
            return;
        }

        // Find the mesh
        var mesh = model.Target.Meshes.FirstOrDefault(m => 
            m.Name.Equals(meshName, StringComparison.OrdinalIgnoreCase));

        if (mesh == null)
        {
            AddLog($"[ERR] Mesh '{meshName}' not found in model '{modelName}'");
            AddLog("Available meshes:");
            foreach (var m in model.Target.Meshes)
            {
                AddLog($"  - {m.Name}");
            }
            return;
        }

        // Check if texture exists
        if (textureManager.TryGet(textureId) != null)
        {
            AddLog($"[ERR] Texture '{textureId}' not found");
            AddLog("Available textures:");
            foreach (var texName in textureManager.textures.Keys)
            {
                AddLog($"  - {texName}");
            }
            return;
        }

        try
        {
            // Override texture
            mesh.AttachTexture(textureManager.Get(textureId));
            AddLog($"Texture for mesh '{meshName}' in model '{modelName}' set to '{textureId}'");
        }
        catch (Exception ex)
        {
            AddLog($"[ERR] Failed to override texture: {ex.Message}");
        }
    }
    
        
    /// <summary>
    /// Adds 1 model to the ImGuiRenderingObjects. 
    /// </summary>
    /// <param name="model"></param>
    public void AddModels(Transformable<RLModel> model)
    {
        ImGuiRenderingObjects.Add(model);
    }

    /// <summary>
    /// Iterates through each model in a List of Transformable RLModels and adds it to
    /// ImGuiRenderingObjects. 
    /// </summary>
    /// <param name="models"></param>
    public void AddModels(List<Transformable<RLModel>> models)
    {
        foreach (var model in models)
            ImGuiRenderingObjects.Add(model);
    }

}