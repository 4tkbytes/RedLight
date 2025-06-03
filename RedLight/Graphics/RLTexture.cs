using RedLight.Utils;
using Silk.NET.OpenGL;
using StbImageSharp;

namespace RedLight.Graphics;

public class RLTexture
{
    private RLGraphics graphics;
    private ImageResult imageResult;

    public uint Handle { get; set; }
    public string Path { get; set; }
    public RLTextureType Type { get; set; }
    
    public RLTexture(RLGraphics graphics, string directory, RLTextureType type)
    {
        this.graphics = graphics;
        var gl = graphics.OpenGL;
        Type = type;
        Path = directory;

        Handle = gl.GenTexture();
        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(TextureTarget.Texture2D, Handle);

        using var fileStream = File.OpenRead(directory);
        imageResult = ImageResult.FromStream(fileStream, ColorComponents.RedGreenBlueAlpha);

        unsafe
        {
            fixed (byte* ptr = imageResult.Data)
                gl.TexImage2D(
                    GLEnum.Texture2D,
                    0,
                    InternalFormat.Rgba,
                    (uint)imageResult.Width,
                    (uint)imageResult.Height,
                    0,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    ptr
                );
        }

        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)TextureWrapMode.Repeat);
        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)TextureWrapMode.Repeat);
        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)TextureMinFilter.NearestMipmapNearest);
        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)TextureMagFilter.Nearest);
        gl.GenerateMipmap(TextureTarget.Texture2D);
        gl.BindTexture(TextureTarget.Texture2D, 0);
    }

}

public enum RLTextureType
{
    Diffuse,
    Specular,
    Normal,
    Height
}