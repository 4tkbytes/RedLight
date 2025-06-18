using System.Numerics;
using Hexa.NET.ImGui;
using RedLight.UI.ImGui;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Serilog;
using RedLight.Graphics;
using Framebuffer = RedLight.Graphics.Framebuffer;

namespace RedLight.UI;

public class RLImGuiEditor
{
    private ImGuiController _imGuiController;
    private RLEngine _engine;
    private bool _showDemoWindow = false;
    private Framebuffer _gameFramebuffer;
    private Vector2 _viewportSize = new Vector2(800, 600);
    private bool _viewportFocused = false;
    private bool _viewportHovered = false;

    public Framebuffer GameFramebuffer => _gameFramebuffer;
    public Vector2 ViewportSize => _viewportSize;
    public bool IsViewportFocused => _viewportFocused;
    public bool IsViewportHovered => _viewportHovered;

    public RLImGuiEditor(RLGraphics graphics, IView view, IInputContext input, RLEngine engine)
    {
        var gl = graphics.OpenGL;
        _engine = engine;
        _imGuiController = new ImGuiController(gl, view, input);
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

    public void Render()
    {
        _imGuiController.MakeCurrent();

        // Main menu bar
        if (global::Hexa.NET.ImGui.ImGui.BeginMainMenuBar())
        {
            if (global::Hexa.NET.ImGui.ImGui.BeginMenu("File"))
            {
                if (global::Hexa.NET.ImGui.ImGui.MenuItem("New"))
                {
                    // TODO: Implement new file functionality
                    Log.Debug("File -> New clicked");
                }
                
                if (global::Hexa.NET.ImGui.ImGui.MenuItem("Open"))
                {
                    // TODO: Implement open file functionality
                    Log.Debug("File -> Open clicked");
                }
                
                if (global::Hexa.NET.ImGui.ImGui.MenuItem("Save"))
                {
                    // TODO: Implement save functionality
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
                    // TODO: Implement undo functionality
                    Log.Debug("Edit -> Undo clicked");
                }
                
                if (global::Hexa.NET.ImGui.ImGui.MenuItem("Redo"))
                {
                    // TODO: Implement redo functionality
                    Log.Debug("Edit -> Redo clicked");
                }
                
                global::Hexa.NET.ImGui.ImGui.Separator();
                
                if (global::Hexa.NET.ImGui.ImGui.MenuItem("Cut"))
                {
                    // TODO: Implement cut functionality
                    Log.Debug("Edit -> Cut clicked");
                }
                
                if (global::Hexa.NET.ImGui.ImGui.MenuItem("Copy"))
                {
                    // TODO: Implement copy functionality
                    Log.Debug("Edit -> Copy clicked");
                }
                
                if (global::Hexa.NET.ImGui.ImGui.MenuItem("Paste"))
                {
                    // TODO: Implement paste functionality
                    Log.Debug("Edit -> Paste clicked");
                }
                
                global::Hexa.NET.ImGui.ImGui.EndMenu();
            }

            if (global::Hexa.NET.ImGui.ImGui.BeginMenu("View"))
            {
                // global::Hexa.NET.ImGui.ImGui.MenuItem("Show Demo Window","", ref _showDemoWindow);
                
                if (global::Hexa.NET.ImGui.ImGui.MenuItem("Reset Layout"))
                {
                    // TODO: Implement layout reset functionality
                    Log.Debug("View -> Reset Layout clicked");
                }
                
                global::Hexa.NET.ImGui.ImGui.EndMenu();
            }

            if (global::Hexa.NET.ImGui.ImGui.BeginMenu("Help"))
            {
                if (global::Hexa.NET.ImGui.ImGui.MenuItem("About"))
                {
                    // TODO: Implement about dialog
                    Log.Debug("Help -> About clicked");
                }
                
                if (global::Hexa.NET.ImGui.ImGui.MenuItem("Documentation"))
                {
                    // TODO: Implement documentation link
                    Log.Debug("Help -> Documentation clicked");
                }
                
                global::Hexa.NET.ImGui.ImGui.EndMenu();
            }

            global::Hexa.NET.ImGui.ImGui.EndMainMenuBar();
        }

        // Viewport window
        RenderViewport();

        _imGuiController.Render();
    }

    private void RenderViewport()
    {
        global::Hexa.NET.ImGui.ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        
        if (global::Hexa.NET.ImGui.ImGui.Begin("Viewport"))
        {
            _viewportFocused = global::Hexa.NET.ImGui.ImGui.IsWindowFocused();
            _viewportHovered = global::Hexa.NET.ImGui.ImGui.IsWindowHovered();

            var availableRegion = global::Hexa.NET.ImGui.ImGui.GetContentRegionAvail();
            
            if (availableRegion.X > 0 && availableRegion.Y > 0)
            {
                // Calculate the largest size that fits the aspect ratio
                var targetAspectRatio = _engine.Window.Size.X / _engine.Window.Size.Y;
                var availableAspectRatio = availableRegion.X / availableRegion.Y;
                
                Vector2 imageSize;
                if (availableAspectRatio > targetAspectRatio)
                {
                    // Window is wider than 16:9, fit to height
                    imageSize = new Vector2(availableRegion.Y * targetAspectRatio, availableRegion.Y);
                }
                else
                {
                    // Window is taller than 16:9, fit to width
                    imageSize = new Vector2(availableRegion.X, availableRegion.X / targetAspectRatio);
                }
                
                // Center the image in the available space
                var cursorPos = global::Hexa.NET.ImGui.ImGui.GetCursorPos();
                var centeredPos = new Vector2(
                    cursorPos.X + (availableRegion.X - imageSize.X) * 0.5f,
                    cursorPos.Y + (availableRegion.Y - imageSize.Y) * 0.5f
                );
                
                global::Hexa.NET.ImGui.ImGui.SetCursorPos(centeredPos);
                
                // Update viewport size if it changed
                if (Math.Abs(imageSize.X - _viewportSize.X) > 1.0f || 
                    Math.Abs(imageSize.Y - _viewportSize.Y) > 1.0f)
                {
                    _viewportSize = imageSize;
                    _gameFramebuffer.Resize((int)_viewportSize.X, (int)_viewportSize.Y);
                    Log.Debug("Viewport resized to: {Width}x{Height} (16:9 aspect ratio)", _viewportSize.X, _viewportSize.Y);
                }

                // Display the framebuffer texture - flip Y coordinates to fix upside down rendering
                global::Hexa.NET.ImGui.ImGui.Image(
                    new ImTextureID(_gameFramebuffer.ColorTexture),
                    imageSize,
                    new Vector2(0, 1), // Start from bottom-left instead of top-left
                    new Vector2(1, 0)  // End at top-right instead of bottom-right
                );
            }
        }
        
        global::Hexa.NET.ImGui.ImGui.End();
        global::Hexa.NET.ImGui.ImGui.PopStyleVar();
    }

    public void Dispose()
    {
        _gameFramebuffer?.Dispose();
        _imGuiController?.Dispose();
    }
}