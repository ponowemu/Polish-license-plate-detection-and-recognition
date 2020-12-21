using Emgu.CV;
using Emgu.CV.Structure;
using ImageProcessor.Models;
using ImageProcessor.Services.Filters;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageProcessor.Services
{
    public interface IBitmapConverter
    {
        ImageContext ApplyGrayScale(ImageContext imageContext);
        ImageContext ApplyGaussianBlur(ImageContext imageContext, Settings settings);
        ImageContext ApplySobelFilter(ImageContext imageContext, Settings settings);
        ImageContext ApplyFullCannyOperator(ImageContext imageContext, Settings settings);
    }

    public class BitmapConverter : IBitmapConverter
    {
        private readonly IGaussianBlur _gaussianBlur;
        private readonly ISobelFilter _sobelFilter;

        private static readonly ColorMatrix ColorMatrix = new ColorMatrix(
            new[]
            {
                new[] {.3f, .3f, .3f, 0, 0},
                new[] {.59f, .59f, .59f, 0, 0},
                new[] {.11f, .11f, .11f, 0, 0},
                new[] {0f, 0, 0, 1, 0},
                new[] {0f, 0, 0, 0, 1}
            });

        public BitmapConverter(IGaussianBlur gaussianBlur, ISobelFilter sobelFilter)
        {
            _gaussianBlur = gaussianBlur;
            _sobelFilter = sobelFilter;
        }

        public ImageContext ApplyGrayScale(ImageContext imageContext)
        {
            imageContext.ProcessedBitmap = MakeGrayScaleAlter(imageContext.ProcessedBitmap);
            return imageContext;
        }

        public ImageContext ApplyGaussianBlur(ImageContext imageContext, Settings settings)
        {
            imageContext.ProcessedBitmap = _gaussianBlur.Apply(imageContext.ProcessedBitmap, settings);
            return imageContext;
        }

        public ImageContext ApplySobelFilter(ImageContext imageContext, Settings settings)
        {
            imageContext.ProcessedBitmap = _sobelFilter.Apply(imageContext.ProcessedBitmap, settings);
            return imageContext;
        }

        public ImageContext ApplyFullCannyOperator(ImageContext imageContext, Settings settings)
        {
            var img = imageContext.ProcessedBitmap.ToImage<Gray, byte>();

            imageContext.GenericImage = img.SmoothGaussian(settings.KernelSize);
            imageContext.GenericImage = img.Canny(settings.LowThreshold, settings.HighThreshold);

            return imageContext;
        }

        private static Bitmap MakeGrayScale(Bitmap orgImage)
        {
            var newBitmap = new Bitmap(orgImage.Width, orgImage.Height);

            for (var width = 0; width < orgImage.Width; width++)
            for (var height = 0; height < orgImage.Height; height++)
            {
                var originalColor = orgImage.GetPixel(width, height);
                var gray = (int)(originalColor.R * .3 +
                                 originalColor.G * .59 +
                                 originalColor.B * .11);

                var newColor = Color.FromArgb(originalColor.A, gray, gray, gray);

                newBitmap.SetPixel(width, height, newColor);
            }

            return newBitmap;
        }

        private static Bitmap MakeGrayScaleAlter(Bitmap original)
        {
            var newBitmap = new Bitmap(original.Width, original.Height);

            using (var graphics = Graphics.FromImage(newBitmap))
            {
                var attributes = new ImageAttributes();

                attributes.SetColorMatrix(ColorMatrix);

                graphics.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
                    0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);
            }
            return newBitmap;
        }
    }
}
