using System;
using ImageProcessor;
using ImageProcessor.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ConsoleApplication
{
    public class DependencyInjectionContainer
    {
        public static IServiceProvider Build()
        {
            return new ServiceCollection()
                .AddSingleton<IImageProcessing, ImageProcessing>()
                .AddScoped<IBitmapConverter, BitmapConverter>()
                .AddScoped<IFileInputOutputHelper, FileInputOutputHelper>()
                .BuildServiceProvider();
        }
    }
}
