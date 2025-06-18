using System.Numerics;

namespace RedLight.UI.Native;

/// <summary>
/// Helper class for creating common UI layouts and positioning
/// </summary>
public static class UILayout
{
    /// <summary>
    /// Creates a UI element positioned at the center of the screen
    /// </summary>
    public static T CreateCentered<T>(T element) where T : UIElement
    {
        element.Clamping = UIClamping.Center;
        return element;
    }
    
    /// <summary>
    /// Creates a UI element positioned at the top-left corner
    /// </summary>
    public static T CreateTopLeft<T>(T element, Vector2 padding = default) where T : UIElement
    {
        element.Clamping = UIClamping.Free;
        element.Position = padding;
        return element;
    }
    
    /// <summary>
    /// Creates a UI element positioned at the top-right corner
    /// </summary>
    public static T CreateTopRight<T>(T element, Vector2 padding = default) where T : UIElement
    {
        element.Clamping = UIClamping.Right;
        element.Position = new Vector2(0, padding.Y);
        element.Offset = new Vector2(-padding.X, 0);
        return element;
    }
    
    /// <summary>
    /// Creates a UI element positioned at the bottom-left corner
    /// </summary>
    public static T CreateBottomLeft<T>(T element, Vector2 padding = default) where T : UIElement
    {
        element.Clamping = UIClamping.Bottom;
        element.Position = new Vector2(padding.X, 0);
        element.Offset = new Vector2(0, -padding.Y);
        return element;
    }
    
    /// <summary>
    /// Creates a UI element positioned at the bottom-right corner
    /// </summary>
    public static T CreateBottomRight<T>(T element, Vector2 padding = default) where T : UIElement
    {
        element.Clamping = UIClamping.Free;
        element.Position = Vector2.Zero;
        element.Offset = new Vector2(-element.Size.X - padding.X, -element.Size.Y - padding.Y);
        return element;
    }
    
    /// <summary>
    /// Creates a horizontally centered element at a specific Y position
    /// </summary>
    public static T CreateHorizontallyCentered<T>(T element, float y, float offsetX = 0) where T : UIElement
    {
        element.Clamping = UIClamping.Free;
        element.Position = new Vector2(0, y);
        element.Offset = new Vector2(offsetX, 0);
        // Position will be calculated to center horizontally
        return element;
    }
    
    /// <summary>
    /// Creates a vertically centered element at a specific X position
    /// </summary>
    public static T CreateVerticallyCentered<T>(T element, float x, float offsetY = 0) where T : UIElement
    {
        element.Clamping = UIClamping.Free;
        element.Position = new Vector2(x, 0);
        element.Offset = new Vector2(0, offsetY);
        // Position will be calculated to center vertically
        return element;
    }
}