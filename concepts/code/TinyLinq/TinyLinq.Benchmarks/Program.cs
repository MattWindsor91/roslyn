using System.Concepts.Enumerable;
using static System.Concepts.Enumerable.Instances;
using System.Concepts.Showable;

namespace TinyLinq
{
    class Program
    {
        static void Main(string[] args)
        {
            DesugaredTests.Run();
            LinqSyntaxTests.Run();
        }
    }
}
