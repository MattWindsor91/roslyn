// This should fail, because Foo: concept is invalid syntax on non-overrides.
// Remember to reference ConceptAttributes.dll!

concept Eq<A>
{
    bool Equals(A a, A b);
}

instance EqArray<A> : Eq<A[]> where EqA: concept // bad!
{
    bool Equals(A[] a, A[] b)
    {
        if (a == null) return b == null;
        if (b == null) return false;
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
        {
            if (!EqA.Equals(a[i], b[i])) return false;
        }
        return true;
    }
}

class Program {
   public void Foo<A>(A a) where A: concept // bad!
   {}

   static void Main()
   {}
}

