using System.Threading.Tasks;

namespace DriverProgram
{
    class Program
    {
        static async Task Main()
        {
            using var overlay = new SampleOverlay();
            await overlay.Run();
        }
    }
}
