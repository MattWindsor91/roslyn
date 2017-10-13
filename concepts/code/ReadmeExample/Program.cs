/// <summary>
/// The example from <c>readme.md</c>, arranged so it can be built as a
/// program.
/// </summary>
namespace ReadmeExample
{
    class Program
    {
        public concept CMonoid<T>
        {
            T Append(this T x, T y);  // extension methods
            T Zero { get; }           // properties on types!
        }

        public instance Monoid_Ints : CMonoid<int>
        {
            int Append(this int x, int y) => x + y;
            int Zero => 0;
        }

        public static T Sum<T, implicit M>(T[] ts) where M : CMonoid<T>
        {
            T acc = M.Zero;
            foreach (var t in ts) acc = acc.Append(t);
            return acc;
        }

        static void Main(string[] args)
        {
            System.Console.WriteLine(Sum(new int[] { 1, 2, 3, 4, 5 }));  // 15
        }
    }
}
