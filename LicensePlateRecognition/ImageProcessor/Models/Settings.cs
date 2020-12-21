namespace ImageProcessor.Models
{
    public class Settings
    {
        public string ImagesPath { get; set; }

        public int KernelSize { get; set; } = 7;//33
        public int Sigma { get; set; } = 5;//8
        public double LowThreshold { get; set; } = 250;
        public double HighThreshold { get; set; } = 260;
        public int WeakPixel { get; set; } = 100;
    }
}
