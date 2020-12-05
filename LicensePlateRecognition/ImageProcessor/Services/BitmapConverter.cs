using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using ImageProcessor.Models;

namespace ImageProcessor.Services
{
    public interface IBitmapConverter
    {
        ImageContext ApplyGrayScale(ImageContext imageContext);
        ImageContext ApplyGaussianBlur(ImageContext imageContext, Settings settings);
    }

    public class BitmapConverter : IBitmapConverter
    {
        private static readonly ColorMatrix ColorMatrix = new ColorMatrix(
            new[]
            {
                new[] {.3f, .3f, .3f, 0, 0},
                new[] {.59f, .59f, .59f, 0, 0},
                new[] {.11f, .11f, .11f, 0, 0},
                new[] {0f, 0, 0, 1, 0},
                new[] {0f, 0, 0, 0, 1}
            });

        public ImageContext ApplyGrayScale(ImageContext imageContext)
        {
            imageContext.ProcessedBitmap = MakeGrayScaleAlter(imageContext.ProcessedBitmap);
            return imageContext;
        }

        public ImageContext ApplyGaussianBlur(ImageContext imageContext, Settings settings)
        {
            var gaussian = GaussianMatrix(settings.KernelSize, settings.Sigma);
            imageContext.ProcessedBitmap = Convolve(imageContext.ProcessedBitmap, gaussian);
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

        private static double[,] GaussianMatrix(int size, double sigma)
        {
            var kernel = new double[size, size];
            var kernelSum = 0d;
            var offset = (size - 1) / 2;
            var constant = 1d / (2 * Math.PI * sigma * sigma);

            for (var y = -offset; y <= offset; y++)
            for (var x = -offset; x <= offset; x++)
            {
                var distance = ((y * y) + (x * x)) / (2 * sigma * sigma);
                kernel[y + offset, x + offset] = constant * Math.Exp(-distance);
                kernelSum += kernel[y + offset, x + offset];
            }

            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                kernel[y, x] = kernel[y, x] * 1d / kernelSum;
            }

            return kernel;
        }

        public static Bitmap Convolve(Bitmap srcImage, double[,] kernel)
        {
            int width = srcImage.Width;
            int height = srcImage.Height;
            BitmapData srcData = srcImage.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var bytes = srcData.Stride * srcData.Height;
            var buffer = new byte[bytes];
            var result = new byte[bytes];
            Marshal.Copy(srcData.Scan0, buffer, 0, bytes);
            srcImage.UnlockBits(srcData);
            var colorChannels = 3;
            var rgb = new double[colorChannels];
            int foff = (kernel.GetLength(0) - 1) / 2;
            for (var y = foff; y < height - foff; y++)
            {
                for (int x = foff; x < width - foff; x++)
                {
                    for (var c = 0; c < colorChannels; c++)
                    {
                        rgb[c] = 0.0;
                    }
                    var kcenter = y * srcData.Stride + x * 4;
                    for (var fy = -foff; fy <= foff; fy++)
                    {
                        for (var fx = -foff; fx <= foff; fx++)
                        {
                            var kpixel = kcenter + fy * srcData.Stride + fx * 4;
                            for (var c = 0; c < colorChannels; c++)
                            {
                                rgb[c] += (double)(buffer[kpixel + c]) * kernel[fy + foff, fx + foff];
                            }
                        }
                    }
                    for (int c = 0; c < colorChannels; c++)
                    {
                        if (rgb[c] > 255)
                        {
                            rgb[c] = 255;
                        }
                        else if (rgb[c] < 0)
                        {
                            rgb[c] = 0;
                        }
                    }
                    for (var c = 0; c < colorChannels; c++)
                    {
                        result[kcenter + c] = (byte)rgb[c];
                    }
                    result[kcenter + 3] = 255;
                }
            }
            var resultImage = new Bitmap(width, height);
            var resultData = resultImage.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(result, 0, resultData.Scan0, bytes);
            resultImage.UnlockBits(resultData);
            return resultImage;
        }
    }
}
