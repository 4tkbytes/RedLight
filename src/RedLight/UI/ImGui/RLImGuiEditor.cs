using System.Diagnostics;
using System.Numerics;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using RedLight.Entities;
using RedLight.Graphics;
using RedLight.Utils;
using Serilog;
using Silk.NET.Input;
using Silk.NET.Windowing;
using Framebuffer = RedLight.Graphics.Framebuffer;

namespace RedLight.UI.ImGui;

public class RLImGuiEditor
{
    private ImGuiController _imGuiController;
    private RLEngine _engine;
    private bool _showDemoWindow = false;
    private Framebuffer _gameFramebuffer;
    private Vector2 _viewportSize = new Vector2(800, 600);
    private bool _viewportFocused = false;
    private bool _viewportHovered = false;
    private bool _editorMode = false;

    // Model inspector properties
    private List<Entity> _modelList = new();
    private Entity _selectedModel = null;
    private int _selectedModelIndex = -1;

    // ImGuizmo shenanigans
    private Camera _camera;
    private ImGuizmoOperation _currentGizmoOperation = ImGuizmoOperation.Translate;
    private ImGuizmoMode _currentGizmoMode = ImGuizmoMode.Local;



    public Framebuffer GameFramebuffer => _gameFramebuffer;
    public Vector2 ViewportSize => _viewportSize;
    public bool IsViewportFocused => _viewportFocused;
    public bool IsViewportHovered => _viewportHovered;
    public bool IsEditorMode => _editorMode;

    public RLImGuiEditor(RLGraphics graphics, IView view, IInputContext input, RLEngine engine)
    {
        var gl = graphics.OpenGL;
        _engine = engine;

        _imGuiController = new ImGuiController(
            gl,
            view,
            input,
            null,
            () => Hexa.NET.ImGui.ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable
            );
        
        _gameFramebuffer = new Framebuffer(graphics, (int)_viewportSize.X, (int)_viewportSize.Y);
        Log.Debug("RLImGuiEditor initialized");
    }

    public void Load()
    {
        Log.Debug("RLImGuiEditor loaded");
    }

    public void Update(float deltaTime)
    {
        _imGuiController.Update(deltaTime);
    }

    public void ToggleEditorMode()
    {
        _editorMode = !_editorMode;
        Log.Debug("Editor mode toggled: {EditorMode}", _editorMode);
    }

    public void SetEditorMode(bool enabled)
    {
        _editorMode = enabled;
        Log.Debug("Editor mode set to: {EditorMode}", _editorMode);
    }

    public void SetModelList(List<Entity> models, Camera activeCamera)
    {
        _modelList = models;
        _camera = activeCamera;

        // Reset selection if current selection is no longer valid
        if (_selectedModel != null && !_modelList.Contains(_selectedModel))
        {
            _selectedModel = null;
            _selectedModelIndex = -1;
        }
    }

    public Entity GetSelectedModel() => _selectedModel;

    public void Render()
    {
        // Only render ImGui interface in editor mode
        if (!_editorMode)
            return;

        _imGuiController.MakeCurrent();

        // Main menu bar
        if (global::Hexa.NET.ImGui.ImGui.BeginMainMenuBar())
        {
            if (global::Hexa.NET.ImGui.ImGui.BeginMenu("File"))
            {
                if (global::Hexa.NET.ImGui.ImGui.MenuItem("New"))
                {
                    Log.Debug("File -> New clicked");
                }

                if (global::Hexa.NET.ImGui.ImGui.MenuItem("Open"))
                {
                    Log.Debug("File -> Open clicked");
                }

                if (global::Hexa.NET.ImGui.ImGui.MenuItem("Save"))
                {
                    Log.Debug("File -> Save clicked");
                }

                global::Hexa.NET.ImGui.ImGui.Separator();

                if (global::Hexa.NET.ImGui.ImGui.MenuItem("Quit"))
                {
                    Log.Debug("File -> Quit clicked");
                    _engine.Window.Quit();
                }

                global::Hexa.NET.ImGui.ImGui.EndMenu();
            }

            if (global::Hexa.NET.ImGui.ImGui.BeginMenu("Edit"))
            {
                if (global::Hexa.NET.ImGui.ImGui.MenuItem("Undo"))
                {
                    Log.Debug("Edit -> Undo clicked");
                }

                if (global::Hexa.NET.ImGui.ImGui.MenuItem("Redo"))
                {
                    Log.Debug("Edit -> Redo clicked");
                }

                global::Hexa.NET.ImGui.ImGui.Separator();

                if (global::Hexa.NET.ImGui.ImGui.MenuItem("Cut"))
                {
                    Log.Debug("Edit -> Cut clicked");
                }

                if (global::Hexa.NET.ImGui.ImGui.MenuItem("Copy"))
                {
                    Log.Debug("Edit -> Copy clicked");
                }

                if (global::Hexa.NET.ImGui.ImGui.MenuItem("Paste"))
                {
                    Log.Debug("Edit -> Paste clicked");
                }

                global::Hexa.NET.ImGui.ImGui.EndMenu();
            }

            if (global::Hexa.NET.ImGui.ImGui.BeginMenu("View"))
            {
                global::Hexa.NET.ImGui.ImGui.MenuItem("Show Demo Window", "", ref _showDemoWindow);

                if (global::Hexa.NET.ImGui.ImGui.MenuItem("Reset Layout"))
                {
                    Log.Debug("View -> Reset Layout clicked");
                }

                global::Hexa.NET.ImGui.ImGui.Separator();

                if (global::Hexa.NET.ImGui.ImGui.MenuItem("Exit Editor", "F12"))
                {
                    SetEditorMode(false);
                    Log.Debug("View -> Exit Editor clicked");
                }

                global::Hexa.NET.ImGui.ImGui.EndMenu();
            }

            if (global::Hexa.NET.ImGui.ImGui.BeginMenu("Help"))
            {
                if (global::Hexa.NET.ImGui.ImGui.MenuItem("About"))
                {
                    Log.Debug("Help -> About clicked");
                }

                if (global::Hexa.NET.ImGui.ImGui.MenuItem("Documentation"))
                {
                    Log.Debug("Help -> Documentation clicked");
                    string url = "https://4tkbytes.github.io/RedLight/";
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }

                global::Hexa.NET.ImGui.ImGui.EndMenu();
            }

            global::Hexa.NET.ImGui.ImGui.EndMainMenuBar();
        }

        // Create dockspace for organized layout
        var dockspaceId = global::Hexa.NET.ImGui.ImGui.GetID("MainDockSpace");
        var mainViewport = global::Hexa.NET.ImGui.ImGui.GetMainViewport();

        // Set dockspace position to be below the menu bar
        var menuBarHeight = global::Hexa.NET.ImGui.ImGui.GetFrameHeight();
        var dockspacePos = new Vector2(mainViewport.Pos.X, mainViewport.Pos.Y + menuBarHeight);
        var dockspaceSize = new Vector2(mainViewport.Size.X, mainViewport.Size.Y - menuBarHeight);

        global::Hexa.NET.ImGui.ImGui.SetNextWindowPos(dockspacePos);
        global::Hexa.NET.ImGui.ImGui.SetNextWindowSize(dockspaceSize);
        global::Hexa.NET.ImGui.ImGui.SetNextWindowViewport(mainViewport.ID);

        var dockspaceFlags = ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoDocking |
                            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse |
                            ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove |
                            ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;

        global::Hexa.NET.ImGui.ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);
        global::Hexa.NET.ImGui.ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);
        global::Hexa.NET.ImGui.ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

        global::Hexa.NET.ImGui.ImGui.Begin("DockSpace", dockspaceFlags);
        global::Hexa.NET.ImGui.ImGui.PopStyleVar(3);

        global::Hexa.NET.ImGui.ImGui.DockSpace(dockspaceId, Vector2.Zero, ImGuiDockNodeFlags.None);
        global::Hexa.NET.ImGui.ImGui.End();

        // Render individual windows
        RenderViewport();
        RenderModelList();
        RenderModelInspector();
        
        // Show demo window if requested
        if (_showDemoWindow)
        {
            global::Hexa.NET.ImGui.ImGui.ShowDemoWindow(ref _showDemoWindow);
        }

        _imGuiController.Render();
    }

    private void RenderViewport()
    {
        global::Hexa.NET.ImGui.ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

        if (global::Hexa.NET.ImGui.ImGui.Begin("Viewport"))
        {
            _viewportFocused = global::Hexa.NET.ImGui.ImGui.IsWindowFocused();
            _viewportHovered = global::Hexa.NET.ImGui.ImGui.IsWindowHovered();

            var contentRegion = global::Hexa.NET.ImGui.ImGui.GetContentRegionAvail();

            // Update viewport size if it changed
            if (contentRegion.X > 0 && contentRegion.Y > 0 &&
                (Math.Abs(contentRegion.X - _viewportSize.X) > 1.0f ||
                 Math.Abs(contentRegion.Y - _viewportSize.Y) > 1.0f))
            {
                _viewportSize = contentRegion;
                _gameFramebuffer.Resize((int)_viewportSize.X, (int)_viewportSize.Y);
                Log.Debug("Viewport resized to: {Width}x{Height}", _viewportSize.X, _viewportSize.Y);
            }

            // Display the framebuffer texture
            global::Hexa.NET.ImGui.ImGui.Image(
                new ImTextureID(_gameFramebuffer.ColorTexture),
                _viewportSize,
                new Vector2(0, 1),
                new Vector2(1, 0)
            );

            // IMPORTANT: Get the content area position (this is where your image is drawn)
            var windowPos = global::Hexa.NET.ImGui.ImGui.GetWindowPos();

            // Only render gizmo if both model and camera are available
            if (_selectedModel != null && _camera != null)
            {
                // Set up ImGuizmo for this frame
                ImGuizmo.SetOrthographic(false);
                ImGuizmo.BeginFrame();

                // Set the rect to match the exact viewport position and size
                ImGuizmo.SetRect(
                    windowPos.X,
                    windowPos.Y,
                    _viewportSize.X,
                    _viewportSize.Y
                );

                // Set up the matrices
                Matrix4x4 view = _camera.View;
                Matrix4x4 projection = _camera.Projection;
                Matrix4x4 model = _selectedModel.ModelMatrix;

                // Enable this to see debug output
                ImGuizmo.Enable(true);

                // This is the critical call to render and interact with the gizmo
                bool matrixChanged = ImGuizmo.Manipulate(
                    ref view,
                    ref projection,
                    _currentGizmoOperation,
                    _currentGizmoMode,
                    ref model
                );

                // Update model if the matrix changed
                if (matrixChanged)
                {
                    if (Matrix4x4.Decompose(model, out Vector3 scale, out Quaternion rotation, out Vector3 position))
                    {
                        _selectedModel.SetPosition(position);
                        Vector3 eulerAngles = RLUtils.QuaternionToEuler(rotation);
                        _selectedModel.SetRotation(eulerAngles);
                        _selectedModel.SetScale(scale);
                    }
                }
            }
        }

        global::Hexa.NET.ImGui.ImGui.End();
        global::Hexa.NET.ImGui.ImGui.PopStyleVar();
    }

    private void RenderModelList()
    {
        if (global::Hexa.NET.ImGui.ImGui.Begin("Scene Objects"))
        {
            global::Hexa.NET.ImGui.ImGui.Text($"Total Objects: {_modelList.Count}");
            global::Hexa.NET.ImGui.ImGui.Separator();

            for (int i = 0; i < _modelList.Count; i++)
            {
                var model = _modelList[i];
                var modelName = !string.IsNullOrEmpty(model.Name) ? model.Name : $"Object_{i}";
                var modelType = model.GetType().Name;

                // Create selectable item
                var isSelected = _selectedModelIndex == i;
                var displayText = $"{modelName} ({modelType})";

                if (global::Hexa.NET.ImGui.ImGui.Selectable(displayText, isSelected))
                {
                    _selectedModelIndex = i;
                    _selectedModel = model;
                    Log.Debug("Selected model: {Name} ({Type})", modelName, modelType);
                }

                // Right-click context menu
                if (global::Hexa.NET.ImGui.ImGui.BeginPopupContextItem($"context_{i}"))
                {
                    global::Hexa.NET.ImGui.ImGui.Text($"Actions for {modelName}");
                    global::Hexa.NET.ImGui.ImGui.Separator();

                    if (global::Hexa.NET.ImGui.ImGui.MenuItem("Focus on Object"))
                    {
                        Log.Debug("Focus on object: {Name}", modelName);
                        // TODO: Implement camera focus on object
                    }

                    if (global::Hexa.NET.ImGui.ImGui.MenuItem("Toggle Hitbox"))
                    {
                        model.ToggleHitbox();
                        Log.Debug("Toggled hitbox for: {Name}", modelName);
                    }

                    if (global::Hexa.NET.ImGui.ImGui.MenuItem("Duplicate"))
                    {
                        Log.Debug("Duplicate object: {Name}", modelName);
                        // TODO: Implement object duplication
                    }

                    global::Hexa.NET.ImGui.ImGui.Separator();

                    if (global::Hexa.NET.ImGui.ImGui.MenuItem("Delete", "Del"))
                    {
                        Log.Debug("Delete object: {Name}", modelName);
                        // TODO: Implement object deletion
                    }

                    global::Hexa.NET.ImGui.ImGui.EndPopup();
                }
            }
        }

        global::Hexa.NET.ImGui.ImGui.End();
    }

    private void RenderModelInspector()
    {
        if (global::Hexa.NET.ImGui.ImGui.Begin("Inspector"))
        {
            if (_selectedModel != null)
            {
                var modelName = !string.IsNullOrEmpty(_selectedModel.Name) ? _selectedModel.Name : "Unnamed Object";
                global::Hexa.NET.ImGui.ImGui.Text($"Selected: {modelName}");
                global::Hexa.NET.ImGui.ImGui.Text($"Type: {_selectedModel.GetType().Name}");
                global::Hexa.NET.ImGui.ImGui.Separator();

                // Transform section
                if (global::Hexa.NET.ImGui.ImGui.CollapsingHeader("Transform", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    // Position
                    var position = _selectedModel.Position;
                    var posArray = new float[] { position.X, position.Y, position.Z };
                    if (global::Hexa.NET.ImGui.ImGui.DragFloat3("Position", ref posArray[0], 0.1f))
                    {
                        _selectedModel.SetPosition(new Vector3(posArray[0], posArray[1], posArray[2]));
                        Log.Debug("Updated position for {Name}: {Position}", modelName, _selectedModel.Position);
                    }

                    // Rotation (in degrees for easier editing)
                    var rotation = _selectedModel.Rotation;
                    var rotDegrees = new float[] {
                        float.RadiansToDegrees(rotation.X),
                        float.RadiansToDegrees(rotation.Y),
                        float.RadiansToDegrees(rotation.Z)
                    };
                    if (global::Hexa.NET.ImGui.ImGui.DragFloat3("Rotation", ref rotDegrees[0], 1.0f))
                    {
                        _selectedModel.SetRotation(new Vector3(
                            float.DegreesToRadians(rotDegrees[0]),
                            float.DegreesToRadians(rotDegrees[1]),
                            float.DegreesToRadians(rotDegrees[2])
                        ));
                        Log.Debug("Updated rotation for {Name}: {Rotation}", modelName, _selectedModel.Rotation);
                    }

                    // Scale
                    var scale = _selectedModel.Scale;
                    var scaleArray = new float[] { scale.X, scale.Y, scale.Z };
                    if (global::Hexa.NET.ImGui.ImGui.DragFloat3("Scale", ref scaleArray[0], 0.01f, 0.01f, 10.0f))
                    {
                        _selectedModel.SetScale(new Vector3(scaleArray[0], scaleArray[1], scaleArray[2]));
                        Log.Debug("Updated scale for {Name}: {Scale}", modelName, _selectedModel.Scale);
                    }
                }

                // ImGuizmo
                global::Hexa.NET.ImGui.ImGui.Text("Gizmo Controls");

                bool isTranslate = _currentGizmoOperation == ImGuizmoOperation.Translate;
                bool isRotate = _currentGizmoOperation == ImGuizmoOperation.Rotate;
                bool isScale = _currentGizmoOperation == ImGuizmoOperation.Scale;

                if (global::Hexa.NET.ImGui.ImGui.RadioButton("Translate", isTranslate))
                    _currentGizmoOperation = ImGuizmoOperation.Translate;

                global::Hexa.NET.ImGui.ImGui.SameLine();
                if (global::Hexa.NET.ImGui.ImGui.RadioButton("Rotate", isRotate))
                    _currentGizmoOperation = ImGuizmoOperation.Rotate;

                global::Hexa.NET.ImGui.ImGui.SameLine();
                if (global::Hexa.NET.ImGui.ImGui.RadioButton("Scale", isScale))
                    _currentGizmoOperation = ImGuizmoOperation.Scale;


                // Physics section
                if (global::Hexa.NET.ImGui.ImGui.CollapsingHeader("Physics"))
                {
                    global::Hexa.NET.ImGui.ImGui.Text($"Has Physics: {_selectedModel.PhysicsSystem != null}");

                    if (_selectedModel.PhysicsSystem != null)
                    {
                        // Mass
                        if (_selectedModel is Entity entity)
                        {
                            global::Hexa.NET.ImGui.ImGui.Text($"Mass: {entity.Mass:F2}");

                            // Friction coefficient
                            var friction = entity.FrictionCoefficient;
                            if (global::Hexa.NET.ImGui.ImGui.DragFloat("Friction", ref friction, 0.1f, 0.0f, 10.0f))
                            {
                                entity.FrictionCoefficient = friction;
                                Log.Debug("Updated friction for {Name}: {Friction}", modelName, friction);
                            }
                        }
                    }

                    // Hitbox toggle
                    var showHitbox = _selectedModel.IsHitboxShown;
                    if (global::Hexa.NET.ImGui.ImGui.Checkbox("Show Hitbox", ref showHitbox))
                    {
                        if (showHitbox != _selectedModel.IsHitboxShown)
                        {
                            _selectedModel.ToggleHitbox();
                            Log.Debug("Toggled hitbox for {Name}: {Show}", modelName, showHitbox);
                        }
                    }
                }

                // Rendering section
                if (global::Hexa.NET.ImGui.ImGui.CollapsingHeader("Rendering"))
                {
                    // global::Hexa.NET.ImGui.ImGui.Text($"Visible: {_selectedModel.IsVisible}");

                    // Shader information
                    if (_selectedModel.Model?.AttachedShader != null)
                    {
                        global::Hexa.NET.ImGui.ImGui.Text($"Shader: {_selectedModel.Model.AttachedShader.Name}");
                    }
                    else
                    {
                        global::Hexa.NET.ImGui.ImGui.Text("Shader: None");
                    }
                }
            }
            else
            {
                global::Hexa.NET.ImGui.ImGui.Text("No object selected");
                global::Hexa.NET.ImGui.ImGui.Text("Select an object from the Scene Objects panel");
            }
        }

        global::Hexa.NET.ImGui.ImGui.End();
    }

    public void Dispose()
    {
        _gameFramebuffer?.Dispose();
        _imGuiController?.Dispose();
    }
}

