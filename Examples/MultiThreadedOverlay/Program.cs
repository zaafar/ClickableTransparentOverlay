using System.Threading.Tasks;

namespace MultiThreadedOverlay;

class Program
{
    static async Task Main()
    {
        using var overlay = new SampleOverlay();
        await overlay.Run();
    }
}
