using Silk.NET.OpenGL;

namespace RedLight.Graphics;

public class Framebuffer : IDisposable
{
    private readonly GL _gl;
    public uint FramebufferObject { get; private set; }
    public uint ColorTexture { get; private set; }
    public uint DepthTexture { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }

    public Framebuffer(RLGraphics graphics, int width, int height)
    {
        _gl = graphics.OpenGL;
        Width = width;
        Height = height;

        CreateFramebuffer();
    }

    private unsafe void CreateFramebuffer()
    {
        // Generate framebuffer
        FramebufferObject = _gl.GenFramebuffer();
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferObject);

        // Create color texture
        ColorTexture = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, ColorTexture);
        _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgb, (uint)Width, (uint)Height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, null);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
        _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, ColorTexture, 0);

        // Create depth texture
        DepthTexture = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, DepthTexture);
        _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.DepthComponent, (uint)Width, (uint)Height, 0, PixelFormat.DepthComponent, PixelType.Float, null);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
        _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, DepthTexture, 0);

        // Check if framebuffer is complete
        if (_gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete)
        {
            throw new Exception("Framebuffer not complete!");
        }

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void Resize(int newWidth, int newHeight)
    {
        if (Width == newWidth && Height == newHeight) return;

        Width = newWidth;
        Height = newHeight;

        // Delete old textures
        _gl.DeleteTexture(ColorTexture);
        _gl.DeleteTexture(DepthTexture);

        // Recreate with new size
        CreateFramebuffer();
    }

    public void Bind()
    {
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferObject);
        _gl.Viewport(0, 0, (uint)Width, (uint)Height);
    }

    public void Unbind()
    {
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void Dispose()
    {
        _gl.DeleteFramebuffer(FramebufferObject);
        _gl.DeleteTexture(ColorTexture);
        _gl.DeleteTexture(DepthTexture);
    }
}