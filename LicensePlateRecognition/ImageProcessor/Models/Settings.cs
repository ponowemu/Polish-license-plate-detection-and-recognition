namespace ImageProcessor.Models
{
    public class Settings
    {
        public string ImagesPath { get; set; }

        public int KernelSize { get; set; } = 50;
        public float Sigma { get; set; } = 4;
        public float LowThreshold { get; set; } = 0.24f;
        public float HighThreshold { get; set; } = 0.43f;
        public int WeakPixel { get; set; } = 100;
    }
}
