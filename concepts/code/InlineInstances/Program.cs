using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InlineInstances
{
    public concept CStringify<T>
    {
        string AsString(this T t);
    }

    // The ': CStringify<IntPair>' here tells Concept-C# that IntPair has
    // the right set of members to generate a completely autofilled
    // instance.  Concept-C#, behind the scenes, generates an unnamed
    // instance to bounce into those members.
    public class IntPair : CStringify<IntPair>
    {
        private int _x;
        private int _y;

        public IntPair(int x, int y)
        {
            _x = x;
            _y = y;
        }

        public string AsString() => $"{_x}, {_y}";
    }

    class Program
    {
        static void Main(string[] args)
        {
            // Concept witness inference can pick up the inline instance,
            // as seen here.
            Console.WriteLine(CStringify<IntPair>.AsString(new IntPair(27, 53)));
        }
    }
}
