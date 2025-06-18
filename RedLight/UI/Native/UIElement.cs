using System.Drawing;
using System.Numerics;
using RedLight.Graphics;

namespace RedLight.UI.Native;

public abstract class UIElement
{
    private Vector2 _position;
    private Vector2 _size;
    private UIClamping _clamping = UIClamping.Free;
    private Vector2 _offset = Vector2.Zero;
    
    public Vector2 Position 
    { 
        get => _position; 
        set 
        {
            _position = value;
            UpdateClampedPosition();
        }
    }
    
    public Vector2 Size 
    { 
        get => _size; 
        set 
        {
            _size = value;
            UpdateClampedPosition();
        }
    }
    
    /// <summary>
    /// How this UI element should be clamped to the viewport
    /// </summary>
    public UIClamping Clamping 
    { 
        get => _clamping; 
        set 
        {
            _clamping = value;
            UpdateClampedPosition();
        }
    }
    
    /// <summary>
    /// Offset from the clamped position (useful for fine-tuning positioning)
    /// </summary>
    public Vector2 Offset 
    { 
        get => _offset; 
        set 
        {
            _offset = value;
            UpdateClampedPosition();
        }
    }
    
    /// <summary>
    /// The actual position after clamping calculations
    /// </summary>
    public Vector2 ClampedPosition { get; private set; }
    
    public Color Color { get; set; } = Color.White; // RGBA
    public bool IsVisible { get; set; } = true;
    public Matrix4x4 Transform { get; protected set; } = Matrix4x4.Identity;
    
    // Viewport dimensions (should be updated by UIManager)
    internal Vector2 ViewportSize { get; set; } = new Vector2(1920, 1080);
    
    public abstract void Render(RLGraphics graphics, Camera camera);
    
    protected void UpdateTransform()
    {
        Transform = Matrix4x4.CreateScale(Size.X, Size.Y, 1.0f) * 
                   Matrix4x4.CreateTranslation(ClampedPosition.X, ClampedPosition.Y, 0.0f);
    }
    
    private void UpdateClampedPosition()
    {
        ClampedPosition = CalculateClampedPosition();
    }
    
    private Vector2 CalculateClampedPosition()
    {
        Vector2 clampedPos = _position;
        
        switch (_clamping)
        {
            case UIClamping.Free:
                // No clamping, use position as-is
                break;
                
            case UIClamping.Center:
                clampedPos = new Vector2(
                    (ViewportSize.X - Size.X) * 0.5f,
                    (ViewportSize.Y - Size.Y) * 0.5f
                );
                break;
                
            case UIClamping.Left:
                clampedPos = new Vector2(
                    0,
                    _position.Y
                );
                break;
                
            case UIClamping.Right:
                clampedPos = new Vector2(
                    ViewportSize.X - Size.X,
                    _position.Y
                );
                break;
                
            case UIClamping.Top:
                clampedPos = new Vector2(
                    _position.X,
                    0
                );
                break;
                
            case UIClamping.Bottom:
                clampedPos = new Vector2(
                    _position.X,
                    ViewportSize.Y - Size.Y
                );
                break;
        }
        
        return clampedPos + _offset;
    }
    
    /// <summary>
    /// Updates the viewport size for clamping calculations
    /// </summary>
    internal void UpdateViewportSize(Vector2 viewportSize)
    {
        ViewportSize = viewportSize;
        UpdateClampedPosition();
    }
}