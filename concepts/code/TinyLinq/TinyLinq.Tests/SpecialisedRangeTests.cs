using System;
using System.Concepts;
using System.Concepts.Countable;
using System.Concepts.Enumerable;
using static System.Concepts.Enumerable.Instances;
using System.Concepts.Indexable;
using System.Concepts.Prelude;
using System.Concepts.Showable;
using SerialPBT;
using TinyLinq.SpecialisedInstances;

namespace TinyLinq.Tests
{
    public class SpecialisedRangeTests
    {
        public static Imp<bool, Func<bool>> PythagoreanTinyLinq(int max) =>
            PBTHelpers.Implies(
                0 < max,
                (Func<bool>)(() => {
                    var lcount = LinqOracles.PythagoreanTripleCount(max);
                    var tcount =
                        (from a in System.Linq.Enumerable.Range(1, max + 1)
                         from b in System.Linq.Enumerable.Range(a, max + 1 - a)
                         from c in System.Linq.Enumerable.Range(b, max + 1 - b)
                         where a * a + b * b == c * c
                         select true).Count();
                    return lcount == tcount;
                }));

        public static void Run()
        {
            PBTHelpers.Check(PythagoreanTinyLinq, 10);
            PBTHelpers.Check((Func<Range<int>, bool>)GenericTests.Prop_SelectIdentity, 7);
        }
    }
}
