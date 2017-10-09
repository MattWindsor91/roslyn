using System;

namespace AutofilledInstances
{
    public concept CMonoid<T>
    {
        T Plus(this T me, T you);

        T Zero { get; }
    }

    struct Pair
    {
        public int x;
        public int y;

        public Pair Plus(Pair you)
        {
            return new Pair { x = x + you.x, y = y + you.y };
        }
    }

    instance Monoid_Pair : CMonoid<Pair>
    {
        Pair Zero => new Pair { x = 0, y = 0 };
    }

    class Program
    {
        static void Main(string[] args)
        {
            var p1 = new Pair { x = 30, y = 31 };
            var p2 = new Pair { x = 95, y = 98 };

            Console.WriteLine($"{p1.Plus(p2).x}, {p1.Plus(p2).y}");
        }
    }
}
