using System.Numerics;
using RedLight.Graphics;

namespace RedLight.UI.Native;

public class UIManager
{
    private List<UIElement> _elements = new();
    private Vector2 _viewportSize = new Vector2(1920, 1080);
    
    /// <summary>
    /// Current viewport size for UI clamping calculations
    /// </summary>
    public Vector2 ViewportSize 
    { 
        get => _viewportSize; 
        set 
        {
            _viewportSize = value;
            UpdateAllElementViewports();
        }
    }
    
    public void AddElement(UIElement element)
    {
        _elements.Add(element);
        element.UpdateViewportSize(_viewportSize);
    }
    
    public void RemoveElement(UIElement element)
    {
        _elements.Remove(element);
    }
    
    public void Clear()
    {
        _elements.Clear();
    }
    
    /// <summary>
    /// Updates viewport size for all elements (call when window is resized)
    /// </summary>
    public void UpdateViewportSize(Vector2 newSize)
    {
        ViewportSize = newSize;
    }
    
    private void UpdateAllElementViewports()
    {
        foreach (var element in _elements)
        {
            element.UpdateViewportSize(_viewportSize);
        }
    }
    
    public void RenderAll(RLGraphics graphics, Camera camera)
    {
        // Disable depth testing for UI rendering
        var gl = graphics.OpenGL;
        gl.Disable(Silk.NET.OpenGL.GLEnum.DepthTest);
        gl.Enable(Silk.NET.OpenGL.GLEnum.Blend);
        gl.BlendFunc(Silk.NET.OpenGL.BlendingFactor.SrcAlpha, Silk.NET.OpenGL.BlendingFactor.OneMinusSrcAlpha);
        
        foreach (var element in _elements)
        {
            element.Render(graphics, camera);
        }
        
        // Re-enable depth testing for 3D rendering
        gl.Enable(Silk.NET.OpenGL.GLEnum.DepthTest);
        gl.Disable(Silk.NET.OpenGL.GLEnum.Blend);
    }
}