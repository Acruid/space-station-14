using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using OpenTK.Graphics.OpenGL;
using PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;

namespace Mike.Graphics
{
    public class Texture
    {
        // no point binding the texture twice
        private static Texture LastTexture;

        private Texture() { }

        public int ID { get; private set; }

        public static Texture Create(FileInfo file)
        {
            var tex = new Texture();
            tex.ID = LoadTexture2d(file.FullName);
            return tex;
        }

        public static Texture Create(Bitmap image)
        {
            var tex = new Texture();
            tex.ID = LoadTexture2d(image);
            return tex;
        }

        /// <summary>
        ///     Loads a texture from a file into OpenGL.
        ///     Make sure the dimensions are a power of two (2^n)!
        /// </summary>
        /// <param name="filename">Filename of the texture.</param>
        /// <returns>Pointer to the texture in gpu memory.</returns>
        private static int LoadTexture2d(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                throw new ArgumentException(filename);

            var id = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, id);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Nearest);

            var bmp = new Bitmap(filename);
            var bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmpData.Width, bmpData.Height, 0,
                PixelFormat.Bgra, PixelType.UnsignedByte, bmpData.Scan0);

            bmp.UnlockBits(bmpData);

            return id;
        }

        private static int LoadTexture2d(Bitmap texture)
        {
            var id = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, id);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Nearest);

            var bmpData = texture.LockBits(new Rectangle(0, 0, texture.Width, texture.Height), ImageLockMode.ReadOnly, texture.PixelFormat);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmpData.Width, bmpData.Height, 0,
                PixelFormat.Bgra, PixelType.UnsignedByte, bmpData.Scan0);
            texture.UnlockBits(bmpData);

            return id;
        }

        public void BindTexture2d(TextureUnit textureUnit)
        {
            if(LastTexture != this)
            {
                LastTexture = this;
                GL.ActiveTexture(textureUnit);
                GL.BindTexture(TextureTarget.Texture2D, ID);
            }
        }
    }
}
