using Silk.NET.OpenGL;
using StbImageSharp;
using System;
using System.IO;

namespace RedLight.Graphics
{
    public class Texture2D : IDisposable
    {
        public uint Handle { get; private set; }
        private GL _gl;

        public Texture2D(GL gl, string path)
        {
            _gl = gl;
            Handle = _gl.GenTexture();
            Bind();

            // Load image using StbImageSharp
            ImageResult image;
            using (var stream = File.OpenRead(path))
            {
                image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
            }

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
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);

            _gl.GenerateMipmap(TextureTarget.Texture2D);
        }

        public void Bind(TextureUnit unit = TextureUnit.Texture0)
        {
            _gl.ActiveTexture(unit);
            _gl.BindTexture(TextureTarget.Texture2D, Handle);
        }

        public void Dispose()
        {
            _gl.DeleteTexture(Handle);
        }
    }
}