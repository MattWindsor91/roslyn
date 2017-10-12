using System;

namespace StandaloneInstances
{
    class Program
    {
        // # Standalone instances
        //
        // If an instance does not implement any concepts, Concept-C# lets its
        // concept extension methods and operator overloads be accessed as if
        // they were attached to a concept in scope.
        //
        // This makes instances become a more powerful form of extension class.

        public instance DoubleInt
        {
            int Double(this int x) => x * x;
        }

        // Like normal extension methods, .Double() is now available to ints:

        public static void TestDoubleInt()
        {
            Console.WriteLine(5.Double());  // 10
        }

        // Standalone instances can also define operator overloads.
        // Defining one on 'int' to break the normal int behaviour looks
        // possible...

        public instance BreakInt
        {
            int operator +(int x, int y) => 5;
        }

        // ...but since concept operator overloads are prioritised below
        // other types of operator overload, this won't actually happen:

        public static void TestBreakInt()
        {
            Console.WriteLine(2 + 2);  // 4
        }

        static void Main(string[] args)
        {
            TestDoubleInt();
            TestBreakInt();
        }
    }
}
