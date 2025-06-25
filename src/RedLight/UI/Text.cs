using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using FreeTypeSharp;
using RedLight.Graphics;
using RedLight.Scene;
using RedLight.Utils;
using Serilog;
using Silk.NET.OpenGL;
using ShaderType = RedLight.Graphics.ShaderType;
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

public class Font
{
    public FreeTypeText FT_Text;

    private Dictionary<char, Characters> characters = new Dictionary<char, Characters>();

    /// <summary>
    ///
    /// </summary>
    /// <param name="resourceName">The name of the resource, something like "RedLight.Resources.Fonts.Arial.tft"</param>
    /// <exception cref="Exception"></exception>
    public Font(string resourceName, FontConfig fontConfig)
    {
        var shaderManager = ShaderManager.Instance;
        var graphics = SceneManager.Instance.GetCurrentScene().Graphics;

        ShaderManager.Instance.TryAdd("text",
            new RLShaderBundle(graphics, RLFiles.GetResourceAsString("RedLight.Resources.Shaders.text.vert"),
                RLFiles.GetResourceAsString("RedLight.Resources.Shaders.text.frag")));

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

                for (int i = 0; i <= 127; i++)
                {
                    char c = (char)i;
                    
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

                // Store the library and face for future use
                FT_Text.Library = lib;
                FT_Text.Face = face;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error initializing text");
                FT.FT_Done_Face(face);
                FT.FT_Done_FreeType(lib);
                throw;
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
    
    public Dictionary<char, Characters> GetCharacters()
    {
        return characters;
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
    private TextManager() 
    {
        _vao = 0;
        _vbo = 0;
    }

    public Dictionary<string, Font> characters = new();
    
    private uint _vao;
    private uint _vbo;

    private void InitializeBuffers(GL gl)
    {
        if (_vao == 0)
        {
            _vao = gl.GenVertexArray();
            _vbo = gl.GenBuffer();
            
            gl.BindVertexArray(_vao);
            gl.BindBuffer(GLEnum.ArrayBuffer, _vbo);
            
            // Each character quad uses 6 vertices, each vertex has 4 floats (position x,y and texture coords s,t)
            gl.BufferData(GLEnum.ArrayBuffer, (uint)(sizeof(float) * 6 * 4), IntPtr.Zero, BufferUsageARB.DynamicDraw);
            
            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            
            gl.BindBuffer(GLEnum.ArrayBuffer, 0);
            gl.BindVertexArray(0);
        }
    }
    
    public void RenderText(RLGraphics graphics, string text, Vector2 position, float scale, Color color)
    {
        RLShaderBundle shader = ShaderManager.Instance.Get("text");
    
        if (ShaderManager.Instance.TryGet("text").VertexShader == null)
            ShaderManager.Instance.TryAdd("text",
                new RLShader(graphics, ShaderType.Vertex, RLFiles.GetResourceAsString("RedLight.Resources.Shaders.text.vert")),
                new RLShader(graphics, ShaderType.Fragment, RLFiles.GetResourceAsString("RedLight.Resources.Shaders.text.frag")));

        var gl = graphics.OpenGL;
        InitializeBuffers(gl);

        shader.Use();
        shader.SetUniform("textColor", RLUtils.ColorToVector3(color));
    
        // ADD THIS: Set the projection matrix for 2D text rendering
        var projection = Matrix4x4.CreateOrthographicOffCenter(0.0f, SceneManager.Instance.GetCurrentScene().Engine.Window.Size.X, 0f, SceneManager.Instance.GetCurrentScene().Engine.Window.Size.Y, -1.0f, 1.0f);
        shader.SetUniform("projection", projection);
        
        gl.Enable(EnableCap.Blend);
        gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
    
        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindVertexArray(_vao);

        string fontName = "default";
        
        // Check if the requested font exists
        if (!characters.ContainsKey(fontName))
        {
            Log.Warning("Font '{FontName}' not found, using default", fontName);
            if (!characters.ContainsKey("default"))
            {
                Log.Error("Default font not found");
                return;
            }
            fontName = "default";
        }
        
        var font = characters[fontName];
        var fontChars = font.GetCharacters();

        float x = position.X;
        float y = position.Y;

        // Iterate through all characters
        foreach (var c in text)
        {
            if (!fontChars.TryGetValue(c, out var ch))
            {
                // Skip characters not in our character set
                continue;
            }

            float xpos = x + ch.Bearing.X * scale;
            float ypos = y - (ch.Size.Y - ch.Bearing.Y) * scale;

            float w = ch.Size.X * scale;
            float h = ch.Size.Y * scale;

            // Update VBO for each character
            float[] vertices = new float[6 * 4]
            {
                xpos,     ypos + h, 0.0f, 0.0f,
                xpos,     ypos,     0.0f, 1.0f,
                xpos + w, ypos,     1.0f, 1.0f,

                xpos,     ypos + h, 0.0f, 0.0f,
                xpos + w, ypos,     1.0f, 1.0f,
                xpos + w, ypos + h, 1.0f, 0.0f
            };

            // Render glyph texture over quad
            gl.BindTexture(GLEnum.Texture2D, ch.TextureID);
            
            // Update content of VBO memory
            gl.BindBuffer(GLEnum.ArrayBuffer, _vbo);
            unsafe
            {
                fixed (float* vertexPtr = vertices)
                {
                    gl.BufferSubData(GLEnum.ArrayBuffer, 0, (uint)(vertices.Length * sizeof(float)), vertexPtr);
                }
            }
            gl.BindBuffer(GLEnum.ArrayBuffer, 0);
            
            // Render quad
            gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
            
            // Advance cursors for next glyph
            x += (ch.Advance.ToInt64() >> 6) * scale; // Bitshift by 6 to get value in pixels (2^6 = 64)
        }

        gl.BindVertexArray(0);
        gl.BindTexture(GLEnum.Texture2D, 0);
        
        gl.Disable(EnableCap.Blend);
    }

    public void AddFont(string name, Font font)
    {
        characters[name] = font;
    }
}