using System.Collections.Generic;
using System.Concepts;
using System.Linq;

namespace ConceptExtensionMethods
{
    public concept CMonoid<T>
    {
        [ConceptExtension]
        T Plus(T me, T you);

        T Zero { get; }
    }

    public instance Monoid_List<T> : CMonoid<List<T>>
    {
        [ConceptExtension]
        List<T> Plus(List<T> me, List<T> you) => new List<T>(me.Concat(you));

        List<T> Zero => new List<T>();
    }
        

    class Program
    {
        static void Main(string[] args)
        {
            // Static invocation
            System.Console.Write("Static:");
            foreach (var i in CMonoid<List<int>>.Plus(new List<int> { 1, 2, 3 }, new List<int> { 4, 5, 6 }))
            {
                System.Console.Write(" ");
                System.Console.Write(i);
            }
            System.Console.WriteLine();

            // Extension invocation
            System.Console.Write("CEM:   ");
            foreach (var i in (new List<int> { 1, 2, 3 }).Plus(new List<int> { 4, 5, 6 }))
            {
                System.Console.Write(" ");
                System.Console.Write(i);
            }
            System.Console.WriteLine();
        }
    }
}
