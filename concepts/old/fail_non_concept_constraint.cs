// This should fail, because instance EqArray is trying to constrain to an interface
// that isn't a concept.
// Remember to reference ConceptAttributes.dll!

interface Eq<A>
{
    bool Equals(A a, A b);
}

// This should fail...
instance EqArray<A> : Eq<A[]> where EqA: Eq<A>
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

// As should this...
class Foo<A> where EqA: Eq<A> {}

// And this.
interface Bar {}

class Foo where B : Bar {}


class Program {
   static void Main()
   {}
}

