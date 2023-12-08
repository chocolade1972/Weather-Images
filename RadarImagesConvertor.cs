using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Weather_Images
{
    public class RadarImagesConvertor
    {
        public RadarImagesConvertor(string imageToChange, string folderToSave)
        {
            ConvertRadarImages(imageToChange, folderToSave);
        }

        public static Bitmap Create32bpp(System.Drawing.Image image, Size size)
        {
            Bitmap bmp = new Bitmap(size.Width, size.Height,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics gr = Graphics.FromImage(bmp))
            {
                gr.Clear(Color.White);
                gr.DrawImage(image, new Rectangle(0, 0, bmp.Width, bmp.Height));
            }
            return bmp;
        }

        public void ConvertRadarImages(string imageToChange, string folderToSave)
        {
            var image1 = Create32bpp(
        System.Drawing.Image.FromStream(new MemoryStream(File.ReadAllBytes(imageToChange)), true, false), new Size(512, 512));
            var image2 = new Bitmap(Properties.Resources.clean_radar_image);
            image1.SetResolution(96, 96);
            image2.SetResolution(96, 96);
            int tolerance = 64;
            var img1 = new LockBitmap(image1);
            var img2 = new LockBitmap(image2);
            img1.LockBits();
            img2.LockBits();
            for (int x = 0; x < image1.Width; x++)
            {
                for (int y = 0; y < image1.Height; y++)
                {
                    Color pixelColor = img1.GetPixel(x, y);
                    // just average R, G, and B values to get gray. Then invert by 255.
                    int invertedGrayValue = 255 - (int)((pixelColor.R + pixelColor.G + pixelColor.B) / 3);
                    if (invertedGrayValue > tolerance) { invertedGrayValue = 255; }
                    // this keeps the original pixel color but sets the alpha value
                    img1.SetPixel(x, y, Color.FromArgb(invertedGrayValue, pixelColor));
                }
            }
            img1.UnlockBits();
            img2.UnlockBits();
            using (Graphics g = Graphics.FromImage(image2))
            {
                g.CompositingMode = CompositingMode.SourceOver;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.DrawImage(image1, new Point(0, 0));
            }

            string fileToSave = Path.GetFileNameWithoutExtension(imageToChange) + "_Processed.png";
            string fullPathToSave = Path.Combine(folderToSave, fileToSave);

            // Save the processed image
            image2.Save(fullPathToSave, ImageFormat.Png);

            // Delete the original file
            File.Delete(imageToChange);
        }
    }
}