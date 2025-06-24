// todo: finish the fuckass class once i complete the tutorial

using System.Numerics;
using System.Runtime.InteropServices;
using FreeTypeSharp;
using RedLight.Graphics;
using RedLight.Utils;
using Serilog;
using Silk.NET.OpenGL;
using TextureWrapMode = Silk.NET.OpenGL.TextureWrapMode;

namespace RedLight.UI;

public unsafe struct FreeTypeText
{
    public FT_LibraryRec_* Library;
    public FT_FaceRec_* Face;
}

public struct FontConfig
{
    public uint PixelWidth = 0;
    public uint PixelHeight = 48;

    public FontConfig(uint pixelWidth, uint pixelHeight)
    {
        PixelWidth = pixelWidth;
        PixelHeight = pixelHeight;
    }
}

public class Text
{
    public FreeTypeText FT_Text;

    private Dictionary<char, Characters> characters;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="resourceName">The name of the resource, something like "RedLight.Resources.Fonts.Arial.tft"</param>
    /// <exception cref="Exception"></exception>
    public Text(RLGraphics graphics, string resourceName, FontConfig fontConfig)
    {
        var shaderManager = ShaderManager.Instance;

        ShaderManager.Instance.TryAdd("text",
            new RLShaderBundle(graphics, RLFiles.GetResourceAsString("RedLight.Resources.Shaders.text.vert"),
                RLFiles.GetResourceAsString("RedLight.Resources.Shaders.text.vert")));

        unsafe
        {
            var lib = FT_Text.Library;
            var face = FT_Text.Face;

            try
            {
                ThrowIfError(
                    FT.FT_Init_FreeType(&lib),
                    "Initialising FreetypeSharp"
                );

                var directory = RLFiles.GetResourcePath(resourceName);
                ThrowIfError(
                    FT.FT_New_Face(lib, (byte*)Marshal.StringToHGlobalAnsi(directory), 0, &face),
                    "Loading Font"
                );

                ThrowIfError(
                    FT.FT_Set_Pixel_Sizes(face, fontConfig.PixelWidth, fontConfig.PixelHeight),
                    "Setting Pixel Sizes"
                );

                Log.Debug("Face has been successfully loaded");
                Log.Debug("Location of font: {A}", directory);

                var gl = graphics.OpenGL;

                gl.PixelStore(GLEnum.UnpackAlignment, 1);

                for (char c = 'A'; c <= 'Z'; c++)
                {
                    if (ThrowIfError(
                            FT.FT_Load_Char(face, c, FT_LOAD.FT_LOAD_RENDER),
                            "Loading Glyph",
                            false
                        )) continue;

                    uint texture;
                    texture = gl.GenTexture();
                    gl.BindTexture(GLEnum.Texture2D, texture);
                    gl.TexImage2D(
                        GLEnum.Texture2D,
                        0,
                        InternalFormat.Red,
                        face->glyph->bitmap.width,
                        face->glyph->bitmap.rows,
                        0,
                        PixelFormat.Red,
                        PixelType.UnsignedByte,
                        face->glyph->bitmap.buffer);

                    // gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                    // gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                    gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
                    gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)TextureWrapMode.ClampToBorder);
                    gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)TextureMinFilter.Linear);
                    gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)TextureMagFilter.Nearest);

                    Characters character = new Characters(
                        texture,
                        new Vector2(face->glyph->bitmap.width, face->glyph->bitmap.rows),
                        new Vector2(face->glyph->bitmap_left, face->glyph->bitmap_top),
                        face->glyph->advance.x
                    );

                    characters.Add(c, character);
                }

            }
            finally
            {
                FT.FT_Done_Face(face);
                FT.FT_Done_FreeType(lib);
            }
        }
    }

    private bool ThrowIfError(FT_Error error, string action, bool throwException = true)
    {
        if (error != FT_Error.FT_Err_Ok)
        {
            Log.Error("Freetype Error: {ERROR}", error);
            if (throwException)
                throw new Exception($"Error occurred while {action} Freetype");
            return true;
        }

        return false;
    }
}

public struct Characters
{
    public uint TextureID;
    public Vector2 Size;
    public Vector2 Bearing;
    public IntPtr Advance;

    public Characters(uint textureid, Vector2 size, Vector2 bearing, IntPtr advance)
    {
        TextureID = textureid;
        Size = size;
        Bearing = bearing;
        Advance = advance;
    }
}



public class TextManager
{
    // singleton stuff
    private static readonly Lazy<TextManager> _instance = new(() => new TextManager());
    public static TextManager Instance => _instance.Value;
    private TextManager() { }

    public Dictionary<string, Text> characters = new();


}