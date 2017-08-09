// Test to see whether the prelude version of Eq works.
// Remember to reference ConceptAttributes.dll and Prelude.dll!

using System.Concepts.Prelude;

class Program {
    static int count = 1;

    // This fairly convoluted driver is trying to test both class witness
    // capture and method witness capture.

    static void ShouldEq<A>(A[] l, A[] r) where EqA : Eq<A>
    {
        new Tester<A, EqA>(count, l, r, true).Test();
        count++;
    }

    static void ShouldNotEq<A>(A[] l, A[] r) where EqA : Eq<A>
    {
        new Tester<A, EqA>(count, l, r, false).Test();
        count++;
    }

    class Tester<A> where EqA: Eq<A>
    {
        int _num;
        A[] _l;
        A[] _r;
        bool _expected;

        public Tester(int num, A[] l, A[] r, bool expected)
        {
            _num = num;
            _l = l;
            _r = r;
            _expected = expected;
        }

        public void Test()
        {
            System.Console.Out.Write($"{_num}: ");
            System.Console.Out.WriteLine((EqArray<A, EqA>.Equals(_l, _r) == _expected) ? "pass" : "fail");
        }
    }

    static void Main()
    {
        ShouldEq(new int[] { }, new int[] { });
        ShouldEq(new int[] { 1, 2, 3 }, new int[] { 1, 2, 3 });
        ShouldNotEq(new int[] { 1, 2, 3 }, new int[] { 1, 2 });
        ShouldNotEq(new int[] { 1, 2, 3 }, new int[] { 4, 5, 6 });
    }
}

