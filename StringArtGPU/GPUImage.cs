using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
using static System.Net.Mime.MediaTypeNames;
using System.Windows.Forms;

namespace StringArtGPU
{
    internal class GPUImage
    {
        public int ssbo;

        public int width;
        public int height;

        public GPUImage(string path)
        {

            using (Bitmap bmp = new Bitmap(path))
            {
                width = bmp.Width; height = bmp.Height;
                float[] image = new float[width * height];

                for (int y = 0; y < bmp.Height; y++)
                {
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        Color pixelColor = bmp.GetPixel(x, y);
                        image[y * width + x] = 1.0f / 3.0f * (pixelColor.R / 255.0f + 0f + pixelColor.G / 255.0f + 0f + pixelColor.B / 255.0f + 0f);
                    }
                }

                ssbo = GL.GenBuffer();

                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ssbo);
                GL.BufferData(BufferTarget.ShaderStorageBuffer, image.Length * sizeof(float), image, BufferUsageHint.StaticDraw);
            }

        }

        public GPUImage(int width, int height, Color pixelColor)
        {
            ssbo = GL.GenBuffer();

            this.width = width;
            this.height = height;

            float[] image = new float[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    image[y * width + x] = 1.0f / 3.0f * (pixelColor.R / 255.0f + pixelColor.G / 255.0f + pixelColor.B / 255.0f);
                }
            }

            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ssbo);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, image.Length * sizeof(float), image, BufferUsageHint.StaticDraw);
        }

        float Clamp(float k, float min, float max)
        {
            return Math.Max(min, Math.Min(k, max));
        }

        public float[] GetData()
        {
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ssbo);
            IntPtr ptr = GL.MapBuffer(BufferTarget.ShaderStorageBuffer, BufferAccess.ReadOnly);
            float[] data = new float[width * height];
            Marshal.Copy(ptr, data, 0, width * height);
            GL.UnmapBuffer(BufferTarget.ShaderStorageBuffer);
            return data;
        }

        public Bitmap GetBitmap()
        {
            float[] inputs = GetData();

            byte[] pixelData = new byte[width * height * 3];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    pixelData[index * 3] = (byte)Clamp((inputs[index] - 0) * 255, 0, 255);
                    pixelData[index * 3 + 1] = (byte)Clamp((inputs[index] - 0) * 255, 0, 255);
                    pixelData[index * 3 + 2] = (byte)Clamp((inputs[index] - 0) * 255, 0, 255);
                }
            }

            Bitmap bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            Rectangle rect = new Rectangle(0, 0, width, height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, bmp.PixelFormat);

            Marshal.Copy(pixelData, 0, bmpData.Scan0, pixelData.Length);
            bmp.UnlockBits(bmpData);

            return bmp;
        }
    }
}
