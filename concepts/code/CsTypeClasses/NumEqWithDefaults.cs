

// encoding Haskell subclassing...
// overloading numeric operations that also implement equality (thus passing fewer dictionaries to MemSq.
namespace NumEqWithDefaults {
	
  /* Haskell allows default implementations in classes that may be overriden in instances
   * 
   * We can emulate this by putting the body of the default method (e.g. Subtract) in an separate
   * helper class carrying its implementation.
   * 
   * Instance declarations either re-implement or delegate to the default.
   * 
   * Q: What about overrides in derived classes (not just instances?)
   */

  using System;

  using Eq;
   
  interface Num<A> : Eq<A> {
    A Add(A a, A b);
    A Mult(A a, A b);
    A Neg(A a);
    A Subtract(A a, A b);
   /* default:
    { return Add(A,Neg(b));
    }
	*/
    

  }

    // type containing default implementations of methods in Num<A>
    // could also be a (static) class.
   struct NumDefaults<NumA,A> where NumA:struct, Num<A> {
        public static A Subtract(A a, A b) {
            NumA numA = default(NumA);
            return numA.Add(a, numA.Neg(b));
        }
    }


    struct NumInt : Num<int> {
    public bool Equals(int a, int b) { return default(EqInt).Equals(a, b); }
    public int Add(int a, int b) { return a + b; }
    public int Mult(int a, int b) { return a * b; }
    public int Neg(int a) { return -a; }

    public int Subtract(int a, int b) {
           return NumDefaults<NumInt, int>.Subtract(a, b); // still a direct call.
        }
    }


	// similar, but overrides default Substract.
    struct NumFloat : Num<float> {
        public bool Equals(float a, float b) { return default(EqFloat).Equals(a, b); }
        public float Add(float a, float b) { return a + b; }
        public float Mult(float a, float b) { return a * b; }
        public float Neg(float a) { return -a; }

        public float Subtract(float a, float b) {
            return a - b;
        }
    }


}
