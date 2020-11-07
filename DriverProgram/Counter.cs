namespace DriverProgram
{
    public class Counter
    {
        public long Count { get; private set; }

        public void Increment()
        {
            Count++;
        }
    }
}