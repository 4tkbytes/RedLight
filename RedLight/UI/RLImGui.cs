using ImGuiNET;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RedLight.Core;
using RedLight.Graphics;
using RedLight.Input;
using RedLight.Scene;
using Serilog;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using System.Numerics;
using System.Reflection;
using RedLight.Utils;
using Serilog.Core;

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
    private int _logLevel = 0;
    private static readonly string[] _logLevelNames = { "Information", "Debug", "Verbose" };
    private static readonly LoggingLevelSwitch _levelSwitch = new(Serilog.Events.LogEventLevel.Information);

    // ignore
    private bool maxwellSpin;
    private Transformable<RLModel>? maxwellModel;

    private Dictionary<string, bool> scaleLockStates = new();

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
    /// A very easy way of rendering ImGui menus.
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

        // model control section
        int idx = 0;
        foreach (var model in ImGuiRenderingObjects)
        {
            string header = $"{model.Target.Name}";
            if (ImGui.CollapsingHeader(header, ImGuiTreeNodeFlags.DefaultOpen))
            {
                bool locked = scaleLockStates.TryGetValue(model.Target.Name, out var l) ? l : false;

                Matrix4X4.Decompose(model.Model, out var sc, out var rot, out var pos);
                var position = new Vector3(pos.X, pos.Y, pos.Z);
                var scale = new Vector3(sc.X, sc.Y, sc.Z);

                bool changed = false;

                if (ImGui.SliderFloat3($"Position##{idx}", ref position, -10f, 10f))
                {
                    changed = true;
                }
                ImGui.SameLine();
                if (ImGui.Button($"Reset Pos##{idx}"))
                {
                    Log.Debug("{A} position has been reset", model.Target.Name);
                    position = new Vector3(0, 0, 0);
                    changed = true;
                }

                // scale lock
                if (ImGui.Button(locked ? "Unlock Scale" : "Lock Scale"))
                {
                    locked = !locked;
                    scaleLockStates[model.Target.Name] = locked;
                    Log.Debug("Lock state has been changed for model \"{A}\": [{B}]", model.Target.Name, locked);
                    if (locked)
                    {
                        scale = new Vector3(scale.X, scale.X, scale.X);
                        changed = true;
                    }
                }

                // Scale sliders
                bool scaleChanged = false;
                if (locked)
                {
                    float uniformScale = scale.X;
                    if (ImGui.SliderFloat($"Scale (Locked)##{idx}", ref uniformScale, 0.01f, 2f))
                    {
                        scale = new Vector3(uniformScale, uniformScale, uniformScale);
                        scaleChanged = true;
                    }
                    ImGui.SameLine();
                    if (ImGui.Button($"Reset Scale##{idx}"))
                    {
                        Log.Debug("{A} scale has been reset", model.Target.Name);
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
                        Log.Debug("{A} scale has been reset", model.Target.Name);
                        scale = new Vector3(1, 1, 1);
                        scaleChanged = true;
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
                    Log.Debug("\"{A}\" rotation has been reset", model.Target.Name);
                    model.eulerAngles = new Vector3(0, 0, 0);
                    changed = true;
                }

                if (changed)
                {
                    model.Reset();
                    model.SetScale(new Vector3D<float>(scale.X, scale.Y, scale.Z));

                    var rotationX = Quaternion.CreateFromAxisAngle(Vector3.UnitX, model.eulerAngles.X * MathF.PI / 180f);
                    var rotationY = Quaternion.CreateFromAxisAngle(Vector3.UnitY, model.eulerAngles.Y * MathF.PI / 180f);
                    var rotationZ = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, model.eulerAngles.Z * MathF.PI / 180f);

                    var finalRotation = rotationX * rotationY * rotationZ;
                    var rotMatrix = Matrix4X4.CreateFromQuaternion(new Quaternion<float>(
                        finalRotation.X, finalRotation.Y, finalRotation.Z, finalRotation.W));

                    var scaleMatrix = Matrix4X4.CreateScale(scale.X, scale.Y, scale.Z);
                    var translationMatrix = Matrix4X4.CreateTranslation(position.X, position.Y, position.Z);
                    var modelMatrix = scaleMatrix * rotMatrix * translationMatrix;

                    model.SetModel(modelMatrix);
                }
                if (maxwellSpin && maxwellModel != null)
                {
                    maxwellModel.SetRotation((float)(deltaTime * MathF.PI), Silk.NET.Maths.Vector3D<float>.UnitZ);

                    double time = DateTimeOffset.Now.ToUnixTimeMilliseconds() / 1000.0;
                    float offset = (float)Math.Sin(time * 6.5) * 0.5f; // 2.0 = frequency, 1.0 = amplitude
                    maxwellModel.SetPosition(new Silk.NET.Maths.Vector3D<float>(0, 0, offset));
                }
            }
            ImGui.Separator();
            idx++;
        }

        ImGui.Separator();
        if (ImGui.CollapsingHeader("Camera Controls", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var cameraPos = new Vector3(camera.Position.X, camera.Position.Y, camera.Position.Z);
            if (ImGui.SliderFloat3("Camera Position", ref cameraPos, -20f, 20f))
            {
                camera.SetPosition(new Vector3D<float>(cameraPos.X, cameraPos.Y, cameraPos.Z));
            }

            // you dont work for shit
            float cameraSpeed = camera.Speed;
            if (ImGui.SliderFloat("Camera Speed", ref cameraSpeed, 0.1f, 10.0f))
            {
                camera.SetSpeed(cameraSpeed);
            }

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
                Vector3D<float> direction = new Vector3D<float>();
                direction.X = float.Cos(float.DegreesToRadians(yaw)) * float.Cos(float.DegreesToRadians(pitch));
                direction.Y = float.Sin(float.DegreesToRadians(pitch));
                direction.Z = float.Sin(float.DegreesToRadians(yaw)) * float.Cos(float.DegreesToRadians(pitch));
                camera.SetFront(direction);
            }

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

        if (ImGui.BeginPopup("Options"))
        {
            ImGui.Checkbox("Auto-scroll", ref _autoScroll);
            ImGui.EndPopup();
        }

        if (ImGui.Button("Options"))
            ImGui.OpenPopup("Options");
        ImGui.SameLine();
        ImGui.Text("Log Level:");
        ImGui.SameLine();
        if (ImGui.Combo("##LogLevel", ref _logLevel, _logLevelNames, _logLevelNames.Length))
        {
            switch (_logLevel)
            {
                case 1: _levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Debug; break;
                case 2: _levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Verbose; break;
                default: _levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Information; break;
            }
            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(RLImGui._levelSwitch)
            .WriteTo.Console()
            .WriteTo.Debug()
            .WriteTo.File("logs/log.txt", rollingInterval: Serilog.RollingInterval.Day, rollOnFileSizeLimit: false)
            .WriteTo.ImGuiConsole(Console)
            .CreateLogger();
            AddLog($"[INF] Log level set to {_logLevelNames[_logLevel]}");
        }
        ImGui.SameLine();
        bool clearButton = ImGui.Button("Clear");
        ImGui.SameLine();
        bool copyButton = ImGui.Button("Copy");
        ImGui.SameLine();
        
        ImGui.InputText("Filter", ref _filterText, 256);

        ImGui.Separator();

        ImGui.BeginChild("ScrollingRegion", new System.Numerics.Vector2(0, -ImGui.GetFrameHeightWithSpacing()),
            ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar);

        if (ImGui.BeginPopupContextWindow())
        {
            if (ImGui.Selectable("Clear")) clearButton = true;
            if (ImGui.Selectable("Copy")) copyButton = true;
            ImGui.EndPopup();
        }

        if (clearButton)
            Console.Clear();

        if (copyButton)
            ImGui.LogToClipboard();

        foreach (var item in Console.Logs)
        {
            if (!string.IsNullOrEmpty(_filterText) && !item.Contains(_filterText, StringComparison.OrdinalIgnoreCase))
                continue;

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

        ImGui.SetItemDefaultFocus();
        if (reclaimFocus)
            ImGui.SetKeyboardFocusHere(-1);

        ImGui.End();
    }

    private void ExecuteCommand(string command)
    {
        AddLog($"> {command}");
        string[] commands = command.Split(new[] { "&&" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var cmd in commands)
        {
            var trimmedCmd = cmd.Trim();
            if (string.IsNullOrWhiteSpace(trimmedCmd))
                continue;
            if (commands.Contains("&&"))
                AddLog($"!> {cmd}");
            
            if (trimmedCmd.Equals("clear", StringComparison.OrdinalIgnoreCase))
            {
                Console.Clear();
            }
            else if (trimmedCmd.StartsWith("help", StringComparison.OrdinalIgnoreCase))
            {
                AddLog("Commands:");
                AddLog("  clear - Clear console");
                AddLog("  help - Show help");
                AddLog("  model - Model configuration");
                AddLog("  scene - Scene editing");
                AddLog("  graphics - Editing graphics backend configs");
            }
            else if (trimmedCmd.StartsWith("model", StringComparison.OrdinalIgnoreCase))
            {
                var parts = trimmedCmd.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                {
                    AddLog("Available model commands: ");
                    AddLog("  - add");
                    AddLog("  - delete");
                    AddLog("  - texture");
                    return;
                }

                var subCommand = parts[1].ToLowerInvariant();
                switch (subCommand)
                {
                    case "add":
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
                                AddLog("Usage: model texture [override|list|dump|add|delete] ...");
                        }
                        else
                        {
                            AddLog("Usage: model texture [override|list|dump|add|delete] ...");
                        }
                        break;
                    default:
                        AddLog($"Unknown model subcommand: {subCommand}");
                        AddLog("Available model commands: ");
                        AddLog("  - add");
                        AddLog("  - delete");
                        AddLog("  - texture");
                        break;
                }
            } else if (trimmedCmd.StartsWith("scene", StringComparison.OrdinalIgnoreCase))
            {
                var parts = trimmedCmd.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                {
                    AddLog("Available scene commands: ");
                    AddLog("  - create");
                    AddLog("  - switch");
                    AddLog("  - delete");
                    AddLog("  - export");
                    AddLog("  - import");
                    AddLog("  - compile");
                    AddLog("  - save");
                    AddLog("  - saveas");
                    AddLog("  - list");
                    return;
                }
                
                var subCommand = parts[1].ToLowerInvariant();
                switch (subCommand)
                {
                    case "create":
                        HandleSceneCreate(parts);
                        break;
                    case "delete":
                        HandleSceneDelete(parts);
                        break;
                    case "switch":
                        HandleSceneSwitch(parts);
                        break;
                    case "export":
                        HandleSceneExport(parts);
                        break;
                    case "import":
                        HandleSceneImport(parts);
                        break;
                    case "compile":
                        HandleSceneCompile(parts);
                        break;
                    case "save":
                        HandleSceneSave(parts);
                        break;
                    case "saveas":
                        HandleSceneSaveAs(parts);
                        break;
                    case "list":
                        HandleSceneList();
                        break;
                    default:
                        AddLog($"Unknown scene subcommand: {subCommand}");
                        AddLog("Available scene commands: ");
                        AddLog("  - create");
                        AddLog("  - switch");
                        AddLog("  - delete");
                        AddLog("  - export");
                        AddLog("  - import");
                        AddLog("  - compile");
                        AddLog("  - save");
                        AddLog("  - saveas");
                        AddLog("  - list");
                        break;
                }
            } else if (trimmedCmd == "maxwell")
            {
                maxwellSpin = !maxwellSpin;
                maxwellModel = null;
                var objectModels = sceneManager.GetCurrentScene().ObjectModels;
                foreach (var model in objectModels)
                {
                    if (model.Target.Name == "maxwell")
                    {
                        maxwellModel = model;
                        break;
                    }
                }
                if (maxwellModel == null)
                {
                    AddLog("[ERR] Maxwell_the_cat model not found :(");
                    maxwellSpin = false;
                    return;
                }

                // create lock
                maxwellModel.SetDefault();
                if (maxwellSpin)
                {
                    maxwellModel.Reset();
                    AddLog("o i i a i o i i a i");

                }
                else
                {
                    AddLog("maxwell stopped");
                }
            } else if (trimmedCmd.StartsWith("texture", StringComparison.OrdinalIgnoreCase))
            {
                var parts = trimmedCmd.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                {
                    AddLog("Available texture commands: ");
                    AddLog("  - add");
                    AddLog("  - delete");
                    return;
                }

                var subCommand = parts[1].ToLowerInvariant();
                switch (subCommand)
                {
                    case "add":
                        HandleTextureAdd(parts);
                        break;
                    case "delete":
                        HandleTextureDelete(parts);
                        break;
                    default:
                        AddLog("Available texture commands: ");
                        AddLog("  - add");
                        AddLog("  - delete");
                        break;
                }
            }
            else if (trimmedCmd.StartsWith("graphics", StringComparison.OrdinalIgnoreCase))
            {
                var parts = trimmedCmd.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                HandleGraphicsCommand(parts);
            }
            else if (trimmedCmd.Equals("shutup", StringComparison.OrdinalIgnoreCase))
            {
                graphics.ShutUp = true;
                AddLog("[INF] Graphics verbose output disabled");
            }
            else if (trimmedCmd.Equals("speak", StringComparison.OrdinalIgnoreCase))
            {
                graphics.ShutUp = false;
                AddLog("[INF] Graphics verbose output enabled");
            }
            else
            {
                AddLog($"Unknown command: {trimmedCmd}");
            }
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
    
    private void HandleSceneList()
    {
        // List scenes already added
        AddLog("Scenes currently loaded in SceneManager:");
        foreach (var scene in sceneManager.Scenes.Keys)
        {
            AddLog($"  - {scene}");
        }

        // List all .cs files in Exported directory
        string exportDir = Path.Combine(AppContext.BaseDirectory, "Resources", "Exported");
        if (!Directory.Exists(exportDir))
        {
            AddLog("[WRN] Exported directory does not exist.");
            return;
        }

        var allSceneFiles = Directory.GetFiles(exportDir, "*.cs")
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .ToList();

        // Exclude already loaded scenes
        var notLoaded = allSceneFiles.Except(sceneManager.Scenes.Keys, StringComparer.OrdinalIgnoreCase).ToList();

        AddLog("Scene files available to add:");
        foreach (var scene in notLoaded)
        {
            AddLog($"  - {scene}");
        }
    }
    
    private void HandleGraphicsCommand(string[] parts)
    {
        if (parts.Length < 3)
        {
            AddLog("Usage: graphics enable|disable DepthTest|CullFace");
            AddLog("       graphics cull back|front");
            AddLog("       graphics frontface ccw|cw");
            return;
        }

        var action = parts[1].ToLower();
        var target = parts[2].ToLower();

        switch (action)
        {
            case "enable":
                if (target == "depthtest")
                    graphics.OpenGL.Enable(Silk.NET.OpenGL.EnableCap.DepthTest);
                else if (target == "cullface")
                    graphics.OpenGL.Enable(Silk.NET.OpenGL.EnableCap.CullFace);
                else
                    AddLog($"Unknown enable target: {target}");
                break;
            case "disable":
                if (target == "depthtest")
                    graphics.OpenGL.Disable(Silk.NET.OpenGL.EnableCap.DepthTest);
                else if (target == "cullface")
                    graphics.OpenGL.Disable(Silk.NET.OpenGL.EnableCap.CullFace);
                else
                    AddLog($"Unknown disable target: {target}");
                break;
            case "cull":
                if (target == "back")
                    graphics.OpenGL.CullFace(Silk.NET.OpenGL.GLEnum.Back);
                else if (target == "front")
                    graphics.OpenGL.CullFace(Silk.NET.OpenGL.GLEnum.Front);
                else
                    AddLog($"Unknown cull target: {target}");
                break;
            case "frontface":
                if (target == "ccw")
                    graphics.OpenGL.FrontFace(Silk.NET.OpenGL.GLEnum.Ccw);
                else if (target == "cw")
                    graphics.OpenGL.FrontFace(Silk.NET.OpenGL.GLEnum.CW);
                else
                    AddLog($"Unknown frontface target: {target}");
                break;
            default:
                AddLog($"Unknown graphics action: {action}");
                break;
        }
    }
    
    private void HandleTextureAdd(string[] parts)
    {
        if (parts.Length < 4)
        {
            AddLog("Usage: texture add <resource_path> <texture_name>");
            return;
        }

        var resourcePath = parts[2];
        var textureName = parts[3];

        try
        {
            var texture = new RLTexture(graphics, RLFiles.GetResourcePath(resourcePath));
            texture.Name = textureName;
            textureManager.TryAdd(textureName, texture);
            AddLog($"Texture '{textureName}' added from '{resourcePath}'");
        }
        catch (Exception ex)
        {
            AddLog($"[ERR] Failed to add texture: {ex.Message}");
        }
    }

    private void HandleTextureDelete(string[] parts)
    {
        if (parts.Length < 3)
        {
            AddLog("Usage: texture delete <texture_name>");
            return;
        }

        var textureName = parts[2];

        textureManager.TryRemove(textureName);
        AddLog($"Texture '{textureName}' deleted");
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
            AddLog("Usage: model add <resource_path> [model_name]");
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
        if (textureManager.TryGet(textureId) == null)
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

    private void HandleSceneExport(string[] parts)
    {
        try
        {
            int sceneIndex = 0;
            string className = $"ExportedScene{sceneIndex}";
            if (parts.Length > 2)
            {
                // If the argument is an integer, treat as index; otherwise, as class name
                if (int.TryParse(parts[2], out int idx))
                {
                    sceneIndex = idx;
                    className = $"ExportedScene{sceneIndex}";
                }
                else
                {
                    className = parts[2];
                }
            }

            string templatePath = "Resources/Templates/SceneTemplate.cs";
            string exportDir = Path.Combine(AppContext.BaseDirectory, "Resources", "Exported");

            // get all the models
            var objectModels = sceneManager.GetCurrentScene().ObjectModels;

            Utils.RLFiles.ExportScene(templatePath, exportDir, className, objectModels);

            AddLog($"Scene exported as {className}.cs in Resources/Exported/");
        }
        catch (Exception ex)
        {
            AddLog($"[ERR] Scene export failed: {ex.Message}");
        }
    }

    private void HandleSceneImport(string[] parts)
    {
        if (parts.Length < 3)
        {
            AddLog("Usage: scene import <SceneClassName>");
            return;
        }
        string className = parts[2];
        try
        {
            // Assume the scene class is already compiled and available in the current AppDomain
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == className);

            if (type == null)
            {
                AddLog($"[ERR] Scene class '{className}' not found in loaded assemblies.");
                return;
            }

            var scene = (RLScene)Activator.CreateInstance(type)!;
            sceneManager.Add(className, scene);
            AddLog($"Scene '{className}' imported and added to SceneManager.");
        }
        catch (Exception ex)
        {
            AddLog($"[ERR] Failed to import scene: {ex.Message}");
        }
    }

    private void HandleSceneSwitch(string[] parts)
    {
        if (parts.Length < 3)
        {
            AddLog("Usage: scene switch <sceneId>");
            return;
        }
        string sceneId = parts[2];
        try
        {
            foreach (var scene in sceneManager.Scenes)
            {
                Log.Debug("{A}, {B}", scene.Key, scene.Value);
            }
            sceneManager.SwitchScene(sceneId);
            AddLog($"Switched to scene '{sceneId}'.");
        }
        catch (Exception ex)
        {
            AddLog($"[ERR] Failed to switch scene: {ex.Message}");
            AddLog(ex.StackTrace);
        }
    }

    private void HandleSceneCreate(string[] parts)
    {
        if (parts.Length < 3)
        {
            AddLog("Usage: scene create <NewSceneName>");
            return;
        }
        string newSceneName = parts[2];
        try
        {
            // Copy template file to new scene file
            string templatePath = "Resources/Templates/SceneTemplate.cs";
            string exportDir = Path.Combine(AppContext.BaseDirectory, "Resources", "Exported");
            string newScenePath = Path.Combine(exportDir, $"{newSceneName}.cs");
            File.Copy(templatePath, newScenePath, overwrite: true);

            // Replace class name inside the file
            string content = File.ReadAllText(newScenePath);
            content = content.Replace("SceneTemplate", newSceneName);
            File.WriteAllText(newScenePath, content);

            AddLog($"Scene file '{newSceneName}.cs' created in Resources/Exported. Compile and import to use.");

            HandleSceneCompile(new string[] { "scene", "compile", newSceneName });
            HandleSceneImport(new string[] { "scene", "import", newSceneName });
            HandleSceneSwitch(new string[] { "scene", "switch", newSceneName });
        }
        catch (Exception ex)
        {
            AddLog($"[ERR] Failed to create scene: {ex.Message}");
        }
    }

    private void HandleSceneDelete(string[] parts)
    {
        if (parts.Length < 3)
        {
            AddLog("Usage: scene delete <sceneId>");
            return;
        }
        string sceneId = parts[2];
        try
        {
            sceneManager.Remove(sceneId);
            AddLog($"Scene '{sceneId}' removed from SceneManager.");
            // Optionally, delete the .cs file
            string exportDir = Path.Combine(AppContext.BaseDirectory, "Resources", "Exported");
            string sceneFile = Path.Combine(exportDir, $"{sceneId}.cs");
            if (File.Exists(sceneFile))
            {
                File.Delete(sceneFile);
                AddLog($"Scene file '{sceneId}.cs' deleted from Resources/Exported.");
            }
        }
        catch (Exception ex)
        {
            AddLog($"[ERR] Failed to delete scene: {ex.Message}");
        }
    }

    private void HandleSceneCompile(string[] parts)
    {
        if (parts.Length < 3)
        {
            AddLog("Usage: scene compile <SceneClassName>");
            return;
        }
        string className = parts[2];
        string exportDir = Path.Combine(AppContext.BaseDirectory, "Resources", "Exported");
        string sceneFile = Path.Combine(exportDir, $"{className}.cs");

        if (!File.Exists(sceneFile))
        {
            AddLog($"[ERR] Scene file '{sceneFile}' not found.");
            return;
        }

        try
        {
            string code = File.ReadAllText(sceneFile);

            // Reference all currently loaded assemblies
            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => MetadataReference.CreateFromFile(a.Location))
                .Cast<MetadataReference>()
                .ToList();

            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create(
                $"{className}_Dynamic",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            using var ms = new MemoryStream();
            var result = compilation.Emit(ms);

            if (!result.Success)
            {
                foreach (var diagnostic in result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
                    AddLog($"[ERR] {diagnostic}");
                return;
            }

            ms.Seek(0, SeekOrigin.Begin);
            var assembly = Assembly.Load(ms.ToArray());

            AddLog($"Scene '{className}' compiled and loaded into memory.");

            HandleSceneImport(new string[] { "scene", "import", className });
        }
        catch (Exception ex)
        {
            AddLog($"[ERR] Scene compilation failed: {ex.Message}");
        }
    }
    
    private void HandleModelTextureAdd(string[] parts)
    {
        if (parts.Length < 6)
        {
            AddLog("Usage: model texture add <model_name> <mesh_name> <texture_path>");
            return;
        }

        var modelName = parts[3];
        var meshName = parts[4];
        var texturePath = parts[5];

        var model = ImGuiRenderingObjects.FirstOrDefault(m =>
            m.Target.Name.Equals(modelName, StringComparison.OrdinalIgnoreCase));
        if (model == null)
        {
            AddLog($"[ERR] Model '{modelName}' not found");
            return;
        }

        var mesh = model.Target.Meshes.FirstOrDefault(m =>
            m.Name.Equals(meshName, StringComparison.OrdinalIgnoreCase));
        if (mesh == null)
        {
            AddLog($"[ERR] Mesh '{meshName}' not found in model '{modelName}'");
            return;
        }

        try
        {
            var texture = textureManager.TryGet(texturePath);
            mesh.AttachTexture(texture);
            AddLog($"Texture '{texture.Name}' added to mesh '{meshName}' in model '{modelName}'");
        }
        catch (Exception ex)
        {
            AddLog($"[ERR] Failed to add texture: {ex.Message}");
        }
    }

    private void HandleModelTextureDelete(string[] parts)
    {
        if (parts.Length < 6)
        {
            AddLog("Usage: model texture delete <model_name> <mesh_name> <texture_name>");
            return;
        }

        var modelName = parts[3];
        var meshName = parts[4];
        var textureName = parts[5];

        var model = ImGuiRenderingObjects.FirstOrDefault(m =>
            m.Target.Name.Equals(modelName, StringComparison.OrdinalIgnoreCase));
        if (model == null)
        {
            AddLog($"[ERR] Model '{modelName}' not found");
            return;
        }

        var mesh = model.Target.Meshes.FirstOrDefault(m =>
            m.Name.Equals(meshName, StringComparison.OrdinalIgnoreCase));
        if (mesh == null)
        {
            AddLog($"[ERR] Mesh '{meshName}' not found in model '{modelName}'");
            return;
        }

        try
        {
            var texturesField = typeof(Mesh).GetField("textures", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var textures = texturesField?.GetValue(mesh) as List<RLTexture>;
            if (textures == null)
            {
                AddLog($"[ERR] Could not access textures for mesh '{meshName}'");
                return;
            }
            int removed = textures.RemoveAll(t => t.Name.Equals(textureName, StringComparison.OrdinalIgnoreCase));
            if (removed > 0)
                AddLog($"Texture '{textureName}' removed from mesh '{meshName}' in model '{modelName}'");
            else
                AddLog($"[WRN] Texture '{textureName}' not found on mesh '{meshName}'");
        }
        catch (Exception ex)
        {
            AddLog($"[ERR] Failed to delete texture: {ex.Message}");
        }
    }
    
    private void HandleSceneSave(string[] parts)
    {
        try
        {
            var currentScene = sceneManager.GetCurrentScene();
            string className = currentScene.GetType().Name;
            string templatePath = "Resources/Templates/SceneTemplate.cs";
            string exportDir = Path.Combine(AppContext.BaseDirectory, "Resources", "Exported");
            var objectModels = currentScene.ObjectModels;

            Utils.RLFiles.ExportScene(templatePath, exportDir, className, objectModels);

            AddLog($"Scene saved as {className}.cs in Resources/Exported/");
        }
        catch (Exception ex)
        {
            AddLog($"[ERR] Scene save failed: {ex.Message}");
        }
    }

    private void HandleSceneSaveAs(string[] parts)
    {
        if (parts.Length < 3)
        {
            AddLog("Usage: scene saveas <ExportedSceneName>");
            return;
        }
        try
        {
            string className = parts[2];
            string templatePath = "Resources/Templates/SceneTemplate.cs";
            string exportDir = Path.Combine(AppContext.BaseDirectory, "Resources", "Exported");
            var objectModels = sceneManager.GetCurrentScene().ObjectModels;

            Utils.RLFiles.ExportScene(templatePath, exportDir, className, objectModels);

            AddLog($"Scene saved as {className}.cs in Resources/Exported/");
        }
        catch (Exception ex)
        {
            AddLog($"[ERR] Scene saveas failed: {ex.Message}");
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