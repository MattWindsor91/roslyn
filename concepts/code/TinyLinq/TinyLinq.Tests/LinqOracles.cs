using System.Linq;

namespace TinyLinq.Tests
{
    public static class LinqOracles
    {
        public static int PythagoreanTripleCount(int max)
        {
            return (from a in Enumerable.Range(1, max + 1)
                    from b in Enumerable.Range(a, max + 1 - a)
                    from c in Enumerable.Range(b, max + 1 - b)
                    where a * a + b * b == c * c
                    select true).Count();
        }
    }
}
