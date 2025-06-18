namespace RedLight.UI.Native;

/// <summary>
/// This enum tells the UI Element where to be clamped
/// </summary>
public enum UIClamping
{
    /// <summary>
    /// Free moving, able to move anywhere without being clamped
    /// </summary>
    Free,
    
    /// <summary>
    /// Pinned to the center of the viewport
    /// </summary>
    Center,
    
    /// <summary>
    /// Pinned to the left of the viewport
    /// </summary>
    Left,
    
    /// <summary>
    /// Pinned to the right of the viewport
    /// </summary>
    Right,
    
    /// <summary>
    /// Pinned to the top of the viewport
    /// </summary>
    Top,
    
    /// <summary>
    /// Pinned to the bottom of the viewport
    /// </summary>
    Bottom
}