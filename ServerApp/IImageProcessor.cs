using System.Drawing;

namespace ServerApp
{
    public interface IImageProcessor
    {
        Bitmap ProcessImage(Bitmap image);
        byte ClampToByte(int value);
    }
}
