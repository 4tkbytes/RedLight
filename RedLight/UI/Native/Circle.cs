using System.Drawing;
using System.Numerics;
using RedLight.Graphics;
using RedLight.Utils;
using Silk.NET.OpenGL;

namespace RedLight.UI.Native;

public class Circle : UIElement
{
    private uint _vao, _vbo, _ebo;
    private bool _initialised = false;
    private int _segments;
    
    public float Radius { get; set; }
    
    public Circle(Vector2 center, float radius, Color color, int segments = 32)
    {
        Position = center;
        Radius = radius;
        Size = new Vector2(radius * 2, radius * 2);
        Color = color;
        _segments = segments;
    }
    
    private void Initialise(RLGraphics graphics)
    {
        if (_initialised) return;
        
        var gl = graphics.OpenGL;
        
        // Generate vertices for circle
        var vertices = new List<float>();
        var indices = new List<uint>();
        
        // Center vertex
        vertices.AddRange(new float[] { 0.0f, 0.0f, 0.5f, 0.5f }); // Position + UV
        
        // Circle vertices
        for (int i = 0; i <= _segments; i++)
        {
            float angle = 2.0f * MathF.PI * i / _segments;
            float x = MathF.Cos(angle);
            float y = MathF.Sin(angle);
            
            vertices.AddRange(new float[] { x, y, (x + 1) * 0.5f, (y + 1) * 0.5f });
            
            if (i < _segments)
            {
                indices.AddRange(new uint[] { 0, (uint)(i + 1), (uint)(i + 2) });
            }
        }
        
        _vao = gl.GenVertexArray();
        _vbo = gl.GenBuffer();
        _ebo = gl.GenBuffer();
        
        gl.BindVertexArray(_vao);
        
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        unsafe
        {
            fixed (float* ptr = vertices.ToArray())
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Count * sizeof(float)), 
                             ptr, BufferUsageARB.StaticDraw);
            }
        }
        
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        unsafe
        {
            fixed (uint* ptr = indices.ToArray())
            {
                gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indices.Count * sizeof(uint)), 
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
        UpdateTransform();
        
        var gl = graphics.OpenGL;
        var shader = ShaderManager.Instance.Get("ui");
        
        gl.UseProgram(shader.Program.ProgramHandle);
        
        var orthoMatrix = CreateOrthographicMatrix(camera);
        
        unsafe
        {
            var transform = Matrix4x4.CreateScale(Radius) * Matrix4x4.CreateTranslation(Position.X, Position.Y, 0.0f);
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
        gl.DrawElements(PrimitiveType.Triangles, (uint)(_segments * 3), DrawElementsType.UnsignedInt, 0);
        gl.BindVertexArray(0);
    }
    
    private Matrix4x4 CreateOrthographicMatrix(Camera camera)
    {
        return Matrix4x4.CreateOrthographicOffCenter(0, 1920, 1080, 0, -1, 1);
    }
}