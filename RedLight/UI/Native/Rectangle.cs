using System.Drawing;
using System.Numerics;
using RedLight.Graphics;
using RedLight.Utils;
using Silk.NET.OpenGL;
using ShaderType = RedLight.Graphics.ShaderType;

namespace RedLight.UI.Native;

/// <summary>
/// This class creates a rectangle as part of UI. it can be of any size, can be a rounded
/// </summary>
public class Rectangle : UIElement
{
    private uint _vao, _vbo, _ebo;
    private bool _initialised = false;
    
    public float CornerRadius { get; set; } = 0.0f; // For rounded rectangles
    
    public Rectangle(Vector2 position, Vector2 size, Color color)
    {
        Position = position;
        Size = size;
        Color = color;
    }
    
    private void Initialise(RLGraphics graphics)
    {
        if (_initialised) return;
        
        var gl = graphics.OpenGL;
        
        // Create vertices for a quad (screen space coordinates)
        float[] vertices = {
            // Position (X, Y)    // UV coordinates
            0.0f, 0.0f,           0.0f, 0.0f,  // Bottom-left
            1.0f, 0.0f,           1.0f, 0.0f,  // Bottom-right
            1.0f, 1.0f,           1.0f, 1.0f,  // Top-right
            0.0f, 1.0f,           0.0f, 1.0f   // Top-left
        };
        
        uint[] indices = {
            0, 1, 2,
            2, 3, 0
        };
        
        _vao = gl.GenVertexArray();
        _vbo = gl.GenBuffer();
        _ebo = gl.GenBuffer();
        
        gl.BindVertexArray(_vao);
        
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        unsafe
        {
            fixed (float* ptr = vertices)
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)), 
                             ptr, BufferUsageARB.StaticDraw);
            }
        }
        
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        unsafe
        {
            fixed (uint* ptr = indices)
            {
                gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indices.Length * sizeof(uint)), 
                             ptr, BufferUsageARB.StaticDraw);
            }
        }
        
        // Position attribute
        gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
        gl.EnableVertexAttribArray(0);
        
        // UV attribute
        gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
        gl.EnableVertexAttribArray(1);
        
        gl.BindVertexArray(0);
        _initialised = true;
    }
    
    public override void Render(RLGraphics graphics, Camera camera)
    {
        if (!IsVisible) return;
    
        Initialise(graphics);
        UpdateTransform(); // This now uses ClampedPosition
    
        var gl = graphics.OpenGL;

        if (ShaderManager.Instance.TryGet("ui").VertexShader == null)
        {
            ShaderManager.Instance.TryAdd("ui",
                new RLShader(graphics, ShaderType.Vertex,
                    RLFiles.GetResourceAsString("RedLight.Resources.Shaders.ui.vert")),
                new RLShader(graphics, ShaderType.Fragment,
                    RLFiles.GetResourceAsString("RedLight.Resources.Shaders.ui.frag")));
        }
        
        var shader = ShaderManager.Instance.Get("ui");
    
        gl.UseProgram(shader.Program.ProgramHandle);
    
        // Convert screen coordinates to NDC (Normalized Device Coordinates)
        var orthoMatrix = CreateOrthographicMatrix(camera);
    
        unsafe
        {
            // Set uniforms
            var transform = Transform;
            float* transformPtr = (float*)&transform;
            int transformLoc = gl.GetUniformLocation(shader.Program.ProgramHandle, "u_transform");
            if (transformLoc != -1)
                gl.UniformMatrix4(transformLoc, 1, false, transformPtr);
        
            float* orthoPtr = (float*)&orthoMatrix;
            int projLoc = gl.GetUniformLocation(shader.Program.ProgramHandle, "u_projection");
            if (projLoc != -1)
                gl.UniformMatrix4(projLoc, 1, false, orthoPtr);
        
            var color = RLUtils.ColorToVector4(Color);
            int colorLoc = gl.GetUniformLocation(shader.Program.ProgramHandle, "u_color");
            if (colorLoc != -1) 
                gl.Uniform4(colorLoc, color.X, color.Y, color.Z, color.W);
        }
    
        gl.BindVertexArray(_vao);
        gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
        gl.BindVertexArray(0);
    }
    
    private Matrix4x4 CreateOrthographicMatrix(Camera camera)
    {
        // Use the actual viewport size for orthographic projection
        return Matrix4x4.CreateOrthographicOffCenter(0, ViewportSize.X, ViewportSize.Y, 0, -1, 1);
    }

}