using FluentAssertions;
using Xunit;

namespace ImageProcessorTests
{
    public class FileInputOutputHelperTests
    {
        [Fact]
        public void ReadSingeFile()
        {
            1.Should().BeGreaterOrEqualTo(1);
        }
    }
}