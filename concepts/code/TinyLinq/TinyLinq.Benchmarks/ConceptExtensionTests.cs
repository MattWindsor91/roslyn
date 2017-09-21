using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TinyLinq.SpecialisedInstances;

namespace TinyLinq.Benchmarks
{
    class ConceptExtensionTests
    {
        public void Test()
        {
            var l = new int[] { 1, 2, 3, 4, 5, 6 };

            var sel = l.Select((int x) => x * 5);
        }
    }
}
