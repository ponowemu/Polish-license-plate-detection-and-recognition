﻿using ImageProcessor.Models;
using Microsoft.Extensions.Logging;
using Emgu.CV;
using Emgu.Util;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV.CvEnum;
using System.Collections.Generic;
using System.IO;

namespace ImageProcessor.Services
{
    public interface IRectangleDetector
    {
        /// <summary>
        /// Detects the rectangles from given image context
        /// </summary>
        /// <remarks>
        /// Processes the image after initial preparation, such as 
        /// greyscaling, tresholding, etc. 
        /// </remarks>
        /// <param name="imageContext">Context of the processing image</param>
        /// <param name="useOpenCV">Use OpenCV library algorithms</param>
        ImageContext Detect(ImageContext imageContext, bool useOpenCV = false);
    }
    /// <inheritdoc cref="IRectangleDetector"/>
    public class RectangleDetector : IRectangleDetector
    {
        private readonly IFileInputOutputHelper _fileIOHelper;

        static int[] directionInX = new int[] { -1, 0, 1, 0 };
        static int[] directionInY = new int[] { 0, 1, 0, -1 };

        public RectangleDetector(
            IFileInputOutputHelper fileIOHelper
            )
        {
            _fileIOHelper = fileIOHelper;
        }
        public ImageContext Detect(ImageContext imageContext, bool useOpenCV = false)
        {
            if (imageContext.ProcessedBitmap.Width > 0
                && imageContext.ProcessedBitmap.Height > 0)
            {
                if (useOpenCV)
                    ProcessOpenCv(imageContext.ProcessedBitmap);
                else
                    ProcessDfs(imageContext.ProcessedBitmap);
            }
            return imageContext;
        }
        #region Procesowanie własne DFS
        private static Bitmap ProcessDfs(System.Drawing.Bitmap image)
        {
            var binaryMatrix = ConvertBitmapTo2d(image);
            if (binaryMatrix.Length > 0)
            {
                // generalnie musimy coś tutaj rozsądnego
                // zwracać z tej metody
                DetectAreas(binaryMatrix);
            }

            return image;
        }
        /// <summary>
        /// Converts an image bitmap to 2d-array (matrix)
        /// </summary>
        /// <remarks>
        /// The matrix is beign filled with 0/1 values,
        /// where 0 can be read as black pixel, and 1 as a white one.
        /// </remarks>
        /// <param name="image">Bitmap image</param>
        /// <returns>Matrix of an image</returns>
        private static int[,] ConvertBitmapTo2d(System.Drawing.Bitmap image)
        {
            // teraz tak, ważne żeby ta bitmapa była w określonym formacie BGR
            // musimy rozważyć ew. jeszce alphe, tylko wtedy bedą nam przypadać
            // 4 bity na kazdy piksel 
            var test = image.PixelFormat;
            if (image.PixelFormat != PixelFormat.Format24bppRgb
                && image.PixelFormat != PixelFormat.Format32bppRgb
                && image.PixelFormat != PixelFormat.Format32bppArgb)
                return default;
            // można ew. rozważyć użycie innych formatów, ale nie wiem
            // czy jest sens bawić się w kanał alfa póki co 
            int[,] boolMatrix = new int[image.Height, image.Width];
            // póki co pomysł jest taki żeby macierz T/F uzupełniać
            // wartościami białych pikseli, przez co potem będziemy mogli
            // w miarę łatwiej wyznaczyć potencjalne regiony
            Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);
            BitmapData bmpData =
                image.LockBits(rect, ImageLockMode.ReadWrite,
                image.PixelFormat);

            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;
                int bytes = Math.Abs(bmpData.Stride) * image.Height;

                for (int y = 0; y < image.Height; y++)
                {
                    var row = ptr + (y * bmpData.Stride);
                    for (int x = 0; x < image.Width; x++)
                    {
                        // Zgodnie z początkowym założeniem akceptujemy
                        // kanał RGB, więc przypada nam 3 bity na każdy 
                        // piksel |B|G|R|
                        var pixel = row + x * 3;
                        // proste sprawdzenie czy dany piksel jest biały 
                        // dla czarnego oczywiście sprawdzalibyśmy 0 
                        // w tym przypadku interesują nas jednak tylko białe
                        // a kolory pozostałych są nieistotne
                        bool isWhite = (pixel[0] == 255 &&
                                        pixel[1] == 255 &&
                                        pixel[2] == 255);
                        boolMatrix[y, x] = isWhite == true ? 1 : 0;
                    }
                }
            }

            //SaveMatToFile(boolMatrix, @"D:\\mat_out.txt");
            return boolMatrix;

            // poniżej generalnie do zaorania 
            // na potrzeby testów tylko sobie odkładamy
            void SaveMatToFile(int[,] boolMatrix, string filePath)
            {
                using (var sw = new StreamWriter(filePath))
                {
                    for (int i = 0; i < boolMatrix.GetLength(0); i++)
                    {
                        for (int j = 0; j < boolMatrix.GetLength(1); j++)
                        {
                            sw.Write(boolMatrix[i, j] + " ");
                        }
                        sw.Write("\n");
                    }
                    sw.Flush();
                    sw.Close();
                }
            }
        }
        private static void DetectAreas(int[,] pixelsMatrix)
        {
            // Wydaje mi sie, że któryś z tych dwóch algor.
            // będzie odpowiedni przy drobnej modyfikacji
            // -- Connected-component labeling
            // -- Flood-fill
            int rows = pixelsMatrix.GetLength(0);
            int cols = pixelsMatrix.GetLength(1);
            bool[,] visited = new bool[rows, cols];
            ///////
            ///

            // inicjalnie ustawiamy wszystkie miejsce
            // jako nieodwiedzone
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    visited[i, j] = false;

            bool cycle = false;

            for (int i = 0; i < rows; i++)
            {
                if (cycle == true)
                    break;

                for (int j = 0; j < cols; j++)
                {
                    if (visited[i, j] == false)
                        cycle = IsCycle(i, j, pixelsMatrix,
                                        visited, -1, -1, rows, cols);

                    if (cycle == true)
                        break;
                }
            }
            if (cycle)
                Console.WriteLine("The cycle was found");

        }
        private static bool IsValid(
            int x,
            int y,
            int rows,
            int cols)
        {
            if (x < rows && x >= 0 &&
                y < cols && y >= 0)
                return true;
            else
                return false;
        }
        private static bool IsCycle(
            int x,
            int y,
            int[,] arr,
            bool[,] visited,
            int parentX,
            int parentY,
            int rows,
            int cols)
        {

            visited[x, y] = true;

            for (int k = 0; k < 4; k++)
            {
                int newX = x + directionInX[k];
                int newY = y + directionInY[k];

                if (IsValid(newX, newY, rows, cols) == true &&
                    arr[newX, newY] == arr[x, y] &&
                    !(parentX == newX && parentY == newY))
                {

                    if (visited[newX, newY] == true)
                        return true;
                    else
                    {
                        bool check =
                            IsCycle(
                                newX,
                                newY,
                                arr,
                                visited,
                                x,
                                y,
                                rows,
                                cols);

                        if (check)
                        {
                            Console.WriteLine($"Point: {x},{y}");
                            return true;
                        }
                            
                    }
                }
            }

            return false;
        }
        #endregion
        #region Procesowanie OpenCV
        private static Bitmap ProcessOpenCv(System.Drawing.Bitmap image)
        {
            var matImage = ConvertBitmapToMat(image);
            Mat proceMat = new Mat();
            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
            {
                // Na podstawie obrazka, który już mamy odpowiednie przeprocesowany
                // możemy wykryć wszystkie kontury jakie się w nim znajdują
                // to nic innego jak 0 bity oddzielony 1 bitami 
                // realizujemy podobne podejście jak w metodzie własnej
                CvInvoke.CvtColor(matImage, proceMat, ColorConversion.Bgr2Gray);
                CvInvoke.FindContours(proceMat, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);

                if (contours.Size > 0)
                {
                    double maxArea = 0;
                    int chosen = 0;
                    for (int i = 0; i < contours.Size; i++)
                    {
                        // Każdy kontur następnie zaznaczamy na docelowym 
                        // obrazku 
                        VectorOfPoint contour = contours[i];
                        double area = CvInvoke.ContourArea(contour);
                        if (area > maxArea)
                        {
                            maxArea = area;
                            chosen = i;
                            MarkDetectedObject(matImage, contours[chosen], maxArea);
                        }
                    }
                }
                Image<Bgr, Byte> imageBgr = matImage.ToImage<Bgr, Byte>();
                return imageBgr.ToBitmap();
            }
        }
        private static void MarkDetectedObject(Mat frame, VectorOfPoint contour, double area)
        {
            // Getting minimal rectangle which contains the contour
            Rectangle box = CvInvoke.BoundingRectangle(contour);

            // Drawing contour and box around it
            CvInvoke.Polylines(frame, contour, true, new MCvScalar(0, 255, 0), 2, LineType.Filled);
            //CvInvoke.Rectangle(frame, box, new MCvScalar(0,255,0), 2, LineType.Filled);

            // Write information next to marked object
            Point center = new Point(box.X + box.Width / 2, box.Y + box.Height / 2);
            //WriteMultilineText(frame, info, new Point(box.Right + 5, center.Y));
        }
        /// <summary>
        /// Konwertuje obraz bitmapy do odpowiedniej macierzy
        /// </summary>
        /// <param name="image">Bitmapa z obrazem</param>
        /// <returns>Macierz zgodna z modelem OpenCV</returns>
        private static Mat ConvertBitmapToMat(Bitmap image)
        {
            int stride = 0;
            Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);
            BitmapData bmpData = image.LockBits(rect, ImageLockMode.ReadWrite, image.PixelFormat);

            PixelFormat pf = image.PixelFormat;
            if (pf == PixelFormat.Format32bppArgb)
                stride = image.Width * 4;
            else
                stride = image.Width * 3;

            Image<Bgra, byte> cvImage = new Image<Bgra, byte>(image.Width, image.Height, stride, (IntPtr)bmpData.Scan0);
            image.UnlockBits(bmpData);

            return cvImage.Mat;
        }
        #endregion
    }
}
