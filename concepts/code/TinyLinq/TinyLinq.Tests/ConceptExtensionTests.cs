using System;
using System.Collections.Generic;
using System.Concepts;
using System.Text;
using System.Threading.Tasks;
using TinyLinq.SpecialisedInstances;
using static System.Concepts.Enumerable.Instances;

namespace TinyLinq
{
    public static class ConceptExtensionTests
    {
        public static void Run()
        {
            var l = new int[] { 1, 2, 3, 4, 5, 6 };

            var sel = l.Select(x => x * 5);

            var sel2 = from x in l select x;


        }
    }
}
