using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ServerApp
{
    public class LinearImageProcessor : IImageProcessor
    {
        public Bitmap ProcessImage(Bitmap image)
        {
            return ApplyHighPassFilter(image);
        }

        private Bitmap ApplyHighPassFilter(Bitmap image)
        {
            int[,] filterMatrix = {
                { -1, -1, -1 },
                { -1,  8, -1 },
                { -1, -1, -1 }
            };

            Bitmap filteredImage = new Bitmap(image.Width, image.Height);
            BitmapData srcData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData destData = filteredImage.LockBits(new Rectangle(0, 0, filteredImage.Width, filteredImage.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            int bytesPerPixel = 3;
            int stride = srcData.Stride;
            IntPtr srcScan0 = srcData.Scan0;
            IntPtr destScan0 = destData.Scan0;

            byte[] srcBuffer = new byte[Math.Abs(stride) * image.Height];
            byte[] destBuffer = new byte[Math.Abs(stride) * filteredImage.Height];

            Marshal.Copy(srcScan0, srcBuffer, 0, srcBuffer.Length);
            Marshal.Copy(destScan0, destBuffer, 0, destBuffer.Length);

            int filterOffset = 1;
            for (int y = filterOffset; y < image.Height - filterOffset; y++)
            {
                for (int x = filterOffset; x < image.Width - filterOffset; x++)
                {
                    int blue = 0, green = 0, red = 0;
                    int byteOffset = y * stride + x * bytesPerPixel;

                    for (int filterY = -filterOffset; filterY <= filterOffset; filterY++)
                    {
                        for (int filterX = -filterOffset; filterX <= filterOffset; filterX++)
                        {
                            int calcOffset = byteOffset + (filterX * bytesPerPixel) + (filterY * stride);

                            blue += srcBuffer[calcOffset] * filterMatrix[filterY + filterOffset, filterX + filterOffset];
                            green += srcBuffer[calcOffset + 1] * filterMatrix[filterY + filterOffset, filterX + filterOffset];
                            red += srcBuffer[calcOffset + 2] * filterMatrix[filterY + filterOffset, filterX + filterOffset];
                        }
                    }

                    destBuffer[byteOffset] = ClampToByte(blue);
                    destBuffer[byteOffset + 1] = ClampToByte(green);
                    destBuffer[byteOffset + 2] = ClampToByte(red);
                }
            }

            Marshal.Copy(destBuffer, 0, destScan0, destBuffer.Length);
            image.UnlockBits(srcData);
            filteredImage.UnlockBits(destData);

            return filteredImage;
        }

        public byte ClampToByte(int value)
        {
            return (byte)(value < 0 ? 0 : (value > 255 ? 255 : value));
        }
    }

}
