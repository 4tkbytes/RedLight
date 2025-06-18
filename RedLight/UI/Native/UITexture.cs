using System.Drawing;
using System.Numerics;
using RedLight.Graphics;
using RedLight.Utils;
using Silk.NET.OpenGL;
using ShaderType = RedLight.Graphics.ShaderType;

namespace RedLight.UI.Native;

/// <summary>
/// This class creates a texture to add onto the camera as a UI element
/// </summary>
public class UITexture : UIElement
{
    private uint _vao, _vbo, _ebo;
    private bool _initialised = false;
    
    public RLTexture Texture { get; set; }
    public Vector2 UV0 { get; set; } = Vector2.Zero;
    public Vector2 UV1 { get; set; } = Vector2.One;
    
    public UITexture(Vector2 position, Vector2 size, RLTexture texture)
    {
        Position = position;
        Size = size;
        Texture = texture;
        Color = Color.White;
        
    }
    
    private void Initialise(RLGraphics graphics)
    {
        if (_initialised) return;
        
        var gl = graphics.OpenGL;
        
        float[] vertices = {
            // Position        // UV
            0.0f, 0.0f,       UV0.X, UV0.Y,  // Bottom-left
            1.0f, 0.0f,       UV1.X, UV0.Y,  // Bottom-right
            1.0f, 1.0f,       UV1.X, UV1.Y,  // Top-right
            0.0f, 1.0f,       UV0.X, UV1.Y   // Top-left
        };
        
        uint[] indices = { 0, 1, 2, 2, 3, 0 };
        
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
        
        gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
        gl.EnableVertexAttribArray(0);
        
        gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
        gl.EnableVertexAttribArray(1);
        
        gl.BindVertexArray(0);
        _initialised = true;
    }
    
    public override void Render(RLGraphics graphics, Camera camera)
    {
        if (!IsVisible || Texture == null) return;
        
        Initialise(graphics);
        UpdateTransform();
        
        var gl = graphics.OpenGL;
        
        if (ShaderManager.Instance.TryGet("ui_textured").VertexShader == null)
        {
            ShaderManager.Instance.TryAdd("ui",
                new RLShader(graphics, ShaderType.Vertex,
                    RLFiles.GetResourceAsString("RedLight.Resources.Shaders.ui.vert")),
                new RLShader(graphics, ShaderType.Fragment,
                    RLFiles.GetResourceAsString("RedLight.Resources.Shaders.ui_textured.frag")));
        }
        
        var shader = ShaderManager.Instance.Get("ui_textured");
        
        gl.UseProgram(shader.Program.ProgramHandle);
        
        // Bind texture
        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(TextureTarget.Texture2D, Texture.Handle);
        
        var orthoMatrix = CreateOrthographicMatrix(camera);
        
        unsafe
        {
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
            
            int textureLoc = gl.GetUniformLocation(shader.Program.ProgramHandle, "u_texture");
            if (textureLoc != -1)
                gl.Uniform1(textureLoc, 0);
        }
        
        gl.BindVertexArray(_vao);
        gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
        gl.BindVertexArray(0);
    }
    
    private Matrix4x4 CreateOrthographicMatrix(Camera camera)
    {
        return Matrix4x4.CreateOrthographicOffCenter(0, 1920, 1080, 0, -1, 1);
    }
}