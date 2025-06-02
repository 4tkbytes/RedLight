using RedLight.Utils;
using Silk.NET.OpenGL;
using StbImageSharp;

namespace RedLight.Graphics;

public class RLTexture
{
    private RLGraphics graphics;
    private ImageResult imageResult;

    public uint Handle { get; set; }

    public RLTexture(RLGraphics graphics, byte[] image)
    {
        var gl = graphics.OpenGL;

        Handle = gl.GenTexture();
        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(TextureTarget.Texture2D, Handle);

        imageResult = ImageResult.FromMemory(image, ColorComponents.RedGreenBlueAlpha);
        unsafe
        {
            fixed (byte* ptr = imageResult.Data)
                gl.TexImage2D(
                    GLEnum.Texture2D,
                    0,
                    InternalFormat.Rgba,
                    (uint)imageResult.Width, (uint)imageResult.Height,
                    0,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    ptr);
        }

        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)TextureWrapMode.Repeat);
        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)TextureWrapMode.Repeat);
        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)TextureMinFilter.NearestMipmapNearest); // <- change here!
        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)TextureMagFilter.Nearest);
        // create mipmap
        gl.GenerateMipmap(TextureTarget.Texture2D);

        // unbind the texture
        gl.BindTexture(TextureTarget.Texture2D, 0);
    }
}