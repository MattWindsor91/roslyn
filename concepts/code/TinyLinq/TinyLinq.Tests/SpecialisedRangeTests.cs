using System;
using System.Concepts;
using System.Concepts.Countable;
using System.Concepts.Enumerable;
using static System.Concepts.Enumerable.Instances;
using System.Concepts.Indexable;
using System.Concepts.Prelude;
using System.Concepts.Showable;
using SerialPBT;

namespace TinyLinq.Tests
{
    public class SpecialisedRangeTests
    {
        public static int PythagoreanTinyLinq()
        {
            var max = 10;
            return (from a in System.Linq.Enumerable.Range(1, max + 1)
                    from b in System.Linq.Enumerable.Range(a, max + 1 - a)
                    from c in System.Linq.Enumerable.Range(b, max + 1 - b)
                    where a * a + b * b == c * c
                    select true).Count();
        }

        public static void Run()
        {
            Console.WriteLine(PythagoreanTinyLinq());
            PBTHelpers.Check((Func<Range<int>, bool>)GenericTests.Prop_SelectIdentity, 7);
        }
    }
}
