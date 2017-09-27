using System;
using System.Collections.Generic;
using System.Concepts;
using System.Text;
using System.Threading.Tasks;
using TinyLinq.SpecialisedInstances;

namespace TinyLinq
{
    static class EExtensions
    {

        public static D Select<T, [AssociatedType]U, [AssociatedType] D, implicit M>(this T[] This, Func<T, U> f) where M : CSelect<T, U, T[], D>

        {

            return M.Select(This, f);

        }
    }

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
