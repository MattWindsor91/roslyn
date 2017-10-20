namespace TinyLinq.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                SpecialisedRangeTests.Run();
                UnspecialisedArrayTests.Run();
                SpecialisedArrayTests.Run();
                LinqSyntaxTests.Run();
            }
            catch (SerialPBT.TestFailedException)
            {
                return;
            }
        }
    }
}
