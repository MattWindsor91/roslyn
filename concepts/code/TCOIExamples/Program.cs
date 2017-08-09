// Examples based on Bruno C. d. S. Oliveira, Adriaan Moors and Martin
// Odersky's paper, 'Type Classes as Objects and Implicits'.
using System.Collections.Generic;


/// <summary>
/// Main example driver.
/// </summary>
namespace TCOIExamples
{
    using System;


    class Program
    {
        static void PrintList<A>(List<A> args)
        {
            Console.Out.Write("List(");
            if (0 < args.Count)
            {
                Console.Out.Write(args[0].ToString());
                for (int i = 1; i < args.Count; i++)
                {
                    Console.Out.Write(", ");
                    Console.Out.Write(args[i].ToString());
                }
            }
            Console.Out.WriteLine(")");
        }

        static void Main(string[] args)
        {
            Section1.Examples();
            Console.Out.WriteLine();

            Section3.Examples();
            Console.Out.WriteLine();

            Section4.Examples();
            Console.Out.WriteLine();
        }
    }
}
