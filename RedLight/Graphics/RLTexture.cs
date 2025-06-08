using RedLight.Utils;
using Silk.NET.OpenGL;
using StbImageSharp;
using Serilog;
using Silk.NET.Assimp;
using File = System.IO.File;
using TextureWrapMode = Silk.NET.OpenGL.TextureWrapMode;

namespace RedLight.Graphics;

public class RLTexture
{
    private RLGraphics graphics;
    private ImageResult imageResult;

    public uint Handle { get; set; }
    public string Path { get; set; }
    public RLTextureType Type { get; set; }
    public string Name { get; set; }

    /// <summary>
    /// This creates a new texture from the image provided. Its default type is the RLTextureType.Diffuse however
    /// you are able to change it to your liking. 
    /// </summary>
    /// <param name="graphics"></param>
    /// <param name="directory"></param>
    /// <param name="type"></param>
    public RLTexture(RLGraphics graphics, string directory, RLTextureType type = RLTextureType.Diffuse)
    {
        this.graphics = graphics;
        var gl = graphics.OpenGL;
        Type = type;
        Path = directory;

        Handle = gl.GenTexture();
        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(TextureTarget.Texture2D, Handle);

        try
        {
            using var fileStream = File.OpenRead(directory);
            imageResult = ImageResult.FromStream(fileStream, ColorComponents.RedGreenBlueAlpha);
        }
        catch (Exception ex)
        {
            Log.Error("Failed to load texture {Path}: {Error}", directory, ex.Message);
            // Load fallback texture (e.g., no-texture.png)
            try
            {
                using var fallbackStream = File.OpenRead(RLFiles.GetResourcePath("RedLight.Resources.Textures.no-texture.png"));
                imageResult = ImageResult.FromStream(fallbackStream, ColorComponents.RedGreenBlueAlpha);
            }
            catch (Exception fallbackEx)
            {
                Log.Error("Failed to load fallback texture: {Error}", fallbackEx.Message);
                throw;
            }
        }

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
    
    /// <summary>
    /// Creates a texture from Assimp's embedded texture data
    /// </summary>
    /// <param name="graphics">Graphics context</param>
    /// <param name="texelData">Pointer to Assimp's Texel data</param>
    /// <param name="width">Texture width</param>
    /// <param name="height">Texture height</param>
    /// <param name="type">Texture type</param>
    public unsafe RLTexture(RLGraphics graphics, Texel* texelData, int width, int height, RLTextureType type = RLTextureType.Diffuse)
    {
        this.graphics = graphics;
        var gl = graphics.OpenGL;
        Type = type;
        Path = $"*embedded*";

        Handle = gl.GenTexture();
        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(TextureTarget.Texture2D, Handle);

        try
        {
            // Use the Texel data directly - Texel is RGBA format in Assimp
            gl.TexImage2D(
                TextureTarget.Texture2D,
                0,
                InternalFormat.Rgba,
                (uint)width,
                (uint)height,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                texelData
            );
        
            Log.Debug("Created embedded texture: {Width}x{Height}", width, height);
        }
        catch (Exception ex)
        {
            Log.Error("Failed to create texture from embedded data: {Error}", ex.Message);
            throw;
        }

        gl.TexParameter(TextureTarget.Texture2D, GLEnum.TextureWrapS, (int)TextureWrapMode.Repeat);
        gl.TexParameter(TextureTarget.Texture2D, GLEnum.TextureWrapT, (int)TextureWrapMode.Repeat);
        gl.TexParameter(TextureTarget.Texture2D, GLEnum.TextureMinFilter, (int)TextureMinFilter.NearestMipmapNearest);
        gl.TexParameter(TextureTarget.Texture2D, GLEnum.TextureMagFilter, (int)TextureMagFilter.Nearest);
        gl.GenerateMipmap(TextureTarget.Texture2D);
        gl.BindTexture(TextureTarget.Texture2D, 0);
    }

    /// <summary>
    /// Creates a texture from Assimp's embedded texture data
    /// </summary>
    /// <param name="graphics">Graphics context</param>
    /// <param name="embTexture">Pointer to Assimp's embedded texture</param>
    /// <param name="type">Texture type</param>
    public unsafe RLTexture(RLGraphics graphics, Silk.NET.Assimp.Texture* embTexture, RLTextureType type = RLTextureType.Diffuse)
    {
        this.graphics = graphics;
        var gl = graphics.OpenGL;
        Type = type;
        Path = $"*embedded*";

        Handle = gl.GenTexture();
        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(TextureTarget.Texture2D, Handle);

        try
        {
            string formatHint = GetStringFromBytePointer(embTexture->AchFormatHint);
            Log.Debug("Processing embedded texture: Format={Format}, DataSize={DataSize}",
                formatHint, embTexture->MWidth);

            if (!string.IsNullOrEmpty(formatHint))
            {
                // This is a compressed texture (PNG, JPG, etc.)
                int dataSize = (int)embTexture->MWidth;
                byte[] data = new byte[dataSize];
                fixed (byte* destPtr = data)
                {
                    System.Buffer.MemoryCopy(embTexture->PcData, destPtr, dataSize, dataSize);
                }

                ImageResult imageResult;
                using (var stream = new MemoryStream(data))
                {
                    imageResult = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
                }

                fixed (byte* ptr = imageResult.Data)
                {
                    gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba,
                        (uint)imageResult.Width, (uint)imageResult.Height,
                        0, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
                }

                // **Fix: Log the decoded image size, not the compressed data size**
                Log.Debug("Decoded compressed embedded texture: {Width}x{Height}",
                    imageResult.Width, imageResult.Height);
            }
            else
            {
                // This is raw pixel data
                int width = (int)embTexture->MWidth;
                int height = embTexture->MHeight > 0 ? (int)embTexture->MHeight : 1;

                gl.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    InternalFormat.Rgba,
                    (uint)width,
                    (uint)height,
                    0,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    embTexture->PcData
                );

                Log.Debug("Created raw embedded texture: {Width}x{Height}", width, height);
            }
        }
        catch (Exception ex)
        {
            Log.Error("Failed to create texture from embedded data: {Error}", ex.Message);
            throw;
        }

        gl.TexParameter(TextureTarget.Texture2D, GLEnum.TextureWrapS, (int)TextureWrapMode.Repeat);
        gl.TexParameter(TextureTarget.Texture2D, GLEnum.TextureWrapT, (int)TextureWrapMode.Repeat);
        gl.TexParameter(TextureTarget.Texture2D, GLEnum.TextureMinFilter, (int)TextureMinFilter.NearestMipmapNearest);
        gl.TexParameter(TextureTarget.Texture2D, GLEnum.TextureMagFilter, (int)TextureMagFilter.Nearest);
        gl.GenerateMipmap(TextureTarget.Texture2D);
        gl.BindTexture(TextureTarget.Texture2D, 0);
    }

    private unsafe string GetStringFromBytePointer(byte* bytePointer)
    {
        if (bytePointer == null)
            return string.Empty;
            
        // Find the length by searching for null terminator
        int length = 0;
        while (bytePointer[length] != 0)
            length++;
            
        // If empty, return empty string
        if (length == 0)
            return string.Empty;
            
        // Convert to string
        return System.Text.Encoding.UTF8.GetString(bytePointer, length);
    }

}

/// <summary>
/// This is just the different Texture Types you can use. 
/// </summary>
public enum RLTextureType
{
    Diffuse,
    Specular,
    Normal,
    Height,
    Metallic,
    Roughness
}