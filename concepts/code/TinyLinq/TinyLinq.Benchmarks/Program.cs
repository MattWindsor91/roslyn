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

            var a = new int[] { 1, 2, 3 };
            var b = new int[] { 4, 5, 6 };

            var c = a.CSelectMany(((int xs) => b), ((int x, int y) => (x, y)));
            var d = c.CToArray();

            foreach (var (x, y) in d)
            {
                System.Console.WriteLine($"({x}, {y})");
            }
        }
    }
}
