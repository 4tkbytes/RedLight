using System.Drawing;
using System.Numerics;
using RedLight.Graphics;
using RedLight.Utils;
using Silk.NET.OpenGL;

namespace RedLight.UI;

public class Rectangle
{
    private uint _vao;
    private uint _vbo;
    private RLGraphics graphics;
    public RLShaderBundle Shader { get; set; }

    public Rectangle(RLGraphics graphics, RLShaderBundle? shader = null)
    {
        this.graphics = graphics;
        
        if (shader.HasValue)
        {
            Shader = shader.Value;
        }
        else
        {
            Shader = ShaderManager.Instance.Get("rectangle");
        }
    }
    
    public void Draw(RLTexture texture, Vector2 position, Vector2 size, float rotation, Color color)
    {
        Shader.Use();
        Matrix4x4 model = Matrix4x4.Identity * 1.0f;
        model = Translate(model, new Vector3(position, 0.0f));
        
        model = Translate(model, new Vector3(0.5f * size.X, 0.5f * size.Y, 0.0f));
        model = Rotate(model, float.DegreesToRadians(rotation), RLUtils.VectorRotationAxis.Z);
        model = Translate(model, new Vector3(-0.5f * size.X, -0.5f * size.Y, 0.0f));

        model = Scale(model, new Vector3(size, 1.0f));
        
        Shader.SetUniform("model", model);
        Shader.SetUniform("spriteColor", color);

        graphics.OpenGL.ActiveTexture(GLEnum.Texture0);
    }

    private Matrix4x4 Translate(Matrix4x4 matrix1, Vector3 translation)
    {
        return Matrix4x4.Multiply(matrix1, Matrix4x4.CreateTranslation(translation));
    }

    private Matrix4x4 Rotate(Matrix4x4 matrix1, float radians, RLUtils.VectorRotationAxis axis)
    {
        switch (axis)
        {
            case RLUtils.VectorRotationAxis.X:
                return Matrix4x4.Multiply(matrix1, Matrix4x4.CreateRotationX(radians));
            case RLUtils.VectorRotationAxis.Y:
                return Matrix4x4.Multiply(matrix1, Matrix4x4.CreateRotationY(radians));
            case RLUtils.VectorRotationAxis.Z:
                return Matrix4x4.Multiply(matrix1, Matrix4x4.CreateRotationZ(radians));
            default:
                throw new Exception("How tf you get here???");
        }
    }

    private Matrix4x4 Scale(Matrix4x4 matrix1, Vector3 scale)
    {
        return Matrix4x4.Multiply(matrix1, Matrix4x4.CreateScale(scale));
    }

    private unsafe void InitRenderData()
    {
        float[] vertices = { 
            // pos      // tex
            0.0f, 1.0f, 0.0f, 1.0f,
            1.0f, 0.0f, 1.0f, 0.0f,
            0.0f, 0.0f, 0.0f, 0.0f, 
    
            0.0f, 1.0f, 0.0f, 1.0f,
            1.0f, 1.0f, 1.0f, 1.0f,
            1.0f, 0.0f, 1.0f, 0.0f
        };

        var gl = graphics.OpenGL;
        
        _vao = gl.GenVertexArray();
        _vbo = gl.GenBuffer();
        
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        fixed (float* ptr = vertices)
            gl.BufferData(BufferTargetARB.ArrayBuffer, (uint) vertices.Length * sizeof(float), ptr, BufferUsageARB.StaticDraw);
        
        gl.BindVertexArray(_vao);
        gl.EnableVertexAttribArray(0);
        gl.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, 4 * sizeof(float), (void*) 0);
        
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        gl.BindVertexArray(0);
    }
}