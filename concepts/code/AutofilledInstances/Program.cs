using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        }
    }
}
