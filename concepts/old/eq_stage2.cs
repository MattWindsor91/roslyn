// Fully expressed concept-based Eq with explicit dictionary usage.
// Remember to reference ConceptAttributes.dll!

concept Eq<A>
{
    bool Equals(A a, A b);
}

instance EqInt : Eq<int>
{
    bool Equals(int a, int b) => a == b;
}

instance EqArray<A> : Eq<A[]> where EqA: Eq<A>
{
    bool Equals(A[] a, A[] b)
    {
        if (a == null) return b == null;
        if (b == null) return false;
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
        {
            if (!(default(EqA).Equals(a[i], b[i]))) return false;
        }
        return true;
    }
}

//
// Driver.
//

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
        System.Console.Out.WriteLine((default(EqArray<A, EqA>).Equals(_l, _r) == _expected) ? "pass" : "fail");
    }
}

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

    static void Main()
    {
        ShouldEq<int, EqInt>(new int[] { }, new int[] { });
        ShouldEq<int, EqInt>(new int[] { 1, 2, 3 }, new int[] { 1, 2, 3 });
        ShouldNotEq<int, EqInt>(new int[] { 1, 2, 3 }, new int[] { 1, 2 });
        ShouldNotEq<int, EqInt>(new int[] { 1, 2, 3 }, new int[] { 4, 5, 6 });
    }
}

