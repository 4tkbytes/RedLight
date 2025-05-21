using Silk.NET.OpenGL;
using StbImageSharp;
using System;
using System.IO;

namespace RedLight.Graphics
{
    public class Texture2D : IDisposable
    {
        public uint Handle { get; private set; }
        public bool IsTransparent { get; private set; }
        public string Path { get; private set; }

        private GL _gl;

        public Texture2D(GL gl, string path)
        {
            _gl = gl;
            Path = path;
            Handle = _gl.GenTexture();
            Bind();

            if (string.IsNullOrEmpty(path))
            {
                // Create a default checkerboard texture
                CreateDefaultTexture();
                return;
            }

            // Load image using StbImageSharp
            Console.WriteLine($"[DEBUG] Loading texture from path: {path}");
            ImageResult image;
            try
            {
                using (var stream = System.IO.File.OpenRead(path))
                {
                    image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
                }

                LoadImageData(image);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Failed to load texture: {path}, Exception: {ex.Message}");
                // Create a default texture in case of failure
                CreateDefaultTexture();
            }
        }

        private void LoadImageData(ImageResult image)
        {
            unsafe
            {
                fixed (byte* data = image.Data)
                {
                    _gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba,
                        (uint)image.Width, (uint)image.Height, 0,
                        PixelFormat.Rgba, PixelType.UnsignedByte, data);
                }
            }

            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.LinearMipmapLinear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);

            _gl.GenerateMipmap(TextureTarget.Texture2D);
        }

        private void CreateDefaultTexture()
        {
            Console.WriteLine("[DEBUG] Creating default texture (checkerboard)");
            // Create a simple checkerboard texture
            byte[] checkerboard = new byte[4 * 64 * 64]; // 64x64 RGBA texture
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    byte color = (byte)(((x / 8) + (y / 8)) % 2 == 0 ? 255 : 100);
                    int index = 4 * (y * 64 + x);
                    checkerboard[index] = color;     // R
                    checkerboard[index + 1] = color; // G
                    checkerboard[index + 2] = color; // B
                    checkerboard[index + 3] = 255;   // A (fully opaque)
                }
            }

            unsafe
            {
                fixed (byte* data = checkerboard)
                {
                    _gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba,
                        64, 64, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
                }
            }

            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.LinearMipmapLinear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);

            _gl.GenerateMipmap(TextureTarget.Texture2D);
        }

        public void Bind(TextureUnit unit = TextureUnit.Texture0)
        {
            _gl.ActiveTexture(unit);
            _gl.BindTexture(TextureTarget.Texture2D, Handle);
        }

        public void MakeTransparent(bool transparent = false)
        {
            IsTransparent = transparent;
        }

        public void Dispose()
        {
            _gl.DeleteTexture(Handle);
        }

        public void DebugTexture(string texResourcePath, TextureManager _textureManager)
        {
            Console.WriteLine($"[DEBUG] Material texture path: {texResourcePath}");
            var existingTexture = _textureManager.GetTexture(texResourcePath);
            if (existingTexture == null)
                Console.WriteLine($"[DEBUG] Texture not found in manager: {texResourcePath}");
            else
                Console.WriteLine($"[DEBUG] Texture found in manager: {texResourcePath}");

            // Console.WriteLine($"[DEBUG] Material diffuse texture handle: {material.DiffuseTexture?.Handle}");
        }
    }
}