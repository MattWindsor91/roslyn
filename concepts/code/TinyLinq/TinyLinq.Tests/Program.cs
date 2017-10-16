namespace TinyLinq.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            SpecialisedRangeTests.Run();
            UnspecialisedArrayTests.Run();
            SpecialisedArrayTests.Run();
            LinqSyntaxTests.Run();
        }
    }
}
