using System;

namespace TCOIExamples
{
    public static class Section4
    {
        //
        // Figure 2
        //

        public struct Apple
        {
            public int x;
        }

        // Scala:
        //   class Apple (x : Int) {}

        public instance OrdApple : Ord<Apple>
        {
            bool Compare(Apple a, Apple b) => a.x <= b.x;
        }

        // Scala (without implicits):
        //   object ordApple extends Ord[Apple] {
        //     def compare (a1: Apple, a2: Apple) = a1.x <= a2.x
        //   }

        public static T Pick<T, implicit OrdA>(T a1, T a2) where OrdA : Ord<T> => OrdA.Compare(a1, a2) ? a2 : a1;

        // Scala (without implicits):
        //   def pick[T] (a1: T, a2: T) (ordA : Ord[T]) =
        //     if (ordA.Compare (a1, a2) a2 else a1

        public instance OrdApple2 : Ord<Apple>
        {
            bool Compare(Apple a, Apple b) => a.x > b.x;
        }

        // Scala (without implicits):
        //   object ordApple2 extends Ord[Apple] {
        //     def compare (a1: Apple, a2: Apple) = a1.x > a2.x
        //   }

        public static void Examples()
        {
            // Figure 2: Apples to Apples with the CONCEPT pattern.

            var a1 = new Apple { x = 3 };
            var a2 = new Apple { x = 5 };
            var a3 = Pick<Apple, OrdApple>(a1, a2);
            Console.Out.WriteLine($"> Pick(apple {a1.x}, apple {a2.x})(OrdApple) = apple {a3.x}");
            var a4 = Pick<Apple, OrdApple2>(a1, a2);
            Console.Out.WriteLine($"> Pick(apple {a1.x}, apple {a2.x})(OrdApple2) = apple {a4.x}");
        }
    }
}