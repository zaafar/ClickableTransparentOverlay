namespace SingleThreadedOverlayWithCoroutines
{
    using System.Threading.Tasks;

    class Program
    {
        static async Task Main()
        {
            using var overlay = new SampleOverlay();
            await overlay.Run();
        }
    }
}
