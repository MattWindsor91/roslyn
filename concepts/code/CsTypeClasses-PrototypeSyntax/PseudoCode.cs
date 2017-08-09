using System;
using System.Collections.Generic;
using System.Text;
 
#if false
namespace CsTypeClasses {
  /* class Eq a where 
     (==)                  :: a -> a -> Bool */
  concept Eq<A> {
    bool Equals(A a, A b);
  }
  /* instance Eq Integer where 
     x == y                =  x `integerEq` y */
  instance Eq<int> {
    bool Equals(int a, int b) { return a == b; }
  }
  /*  instance (Eq a) => Eq ([a]) where 
      nil        == nil                 = true
      (a:as) == (b:bs)                  =  (a == b) && (as == bs)
  */
  instance <A>Eq<A[]> where Eq<A> {
    bool Equals(A[] a, A[] b) {
      if (a == null) return b == null;
      if (b == null) return false;
      if (a.Length != b.Length) return false;
      for (int i = 0; i < a.Length; i++)
        if (Equals<A>(a[i], b[i])) return false;
      return true;
    }
 }
}
#endif