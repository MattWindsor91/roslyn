// TODO: These benchmarks extend Peter Sestoft's sorting benchmarks with ones based on dictionary passing (see IOrd<T>),
// but on closer inspection, the existing tests are not making the best use of generics. REVISE!

// Sorting with Generic C#, and comparisons with dynamically typed sorting
// Revised to use three-way comparisons (IComparable and IGComparable)
// Sorting integers or strings
// This program requires .NET version 2.0.
// Peter Sestoft (sestoft@dina.kvl.dk) * 2001-11-01, 2001-11-22, 2003-08-11

using System;

namespace Perf {
  // Generic sorting routines

  public class Polysort {
    // Cannot use this in 
    //   void qsort<T>(IGComparable<T>[] arr, int a, int b)
    // because ref arguments that are array elements of reference
    // type must have the exact element type of the formal parameter

    private static void swap<U>(ref U s, ref U t) {
      U tmp = s; s = t; t = tmp;
    }

    private static void swap<U>(U[] arr, int s, int t) {
      U tmp = arr[s]; arr[s] = arr[t]; arr[t] = tmp;
    }

    private static void swap(object[] arr, int s, int t) {
      object tmp = arr[s]; arr[s] = arr[t]; arr[t] = tmp;
    }

    // Polymorphic OO-style quicksort: general, not typesafe

    private static void qsort<T>(IGComparable<T>[] arr, int a, int b) {
      // sort arr[a..b]
      if (a < b) {
        int i = a, j = b;
        IGComparable<T> x = arr[(i + j) / 2];
        do {
          while (arr[i].CompareTo(x) < 0) i++;
          while (x.CompareTo(arr[j]) < 0) j--;
          if (i <= j) {
            swap<IGComparable<T>>(arr, i, j);
            i++; j--;
          }
        } while (i <= j);
        qsort<T>(arr, a, j);
        qsort<T>(arr, i, b);
      }
    }

    public static void Quicksort<T>(IGComparable<T>[] arr) {
      qsort<T>(arr, 0, arr.Length - 1);
    }

    public static void CheckSorted<T>(IGComparable<T>[] arr) {
      for (int i = 1; i < arr.Length; i++)
        if (arr[i].CompareTo(arr[i - 1]) < 0)
          throw new Exception("Polysort.CheckSorted");
    }

    // Polymorphic functional-style quicksort: general, typesafe

    private static void qsort<T>(T[] arr, IGComparer<T> cmp, int a, int b) {
      // sort arr[a..b]
      if (a < b) {
        int i = a, j = b;
        T x = arr[(i + j) / 2];
        do {
          while (cmp.Compare(arr[i], x) < 0) i++;
          while (cmp.Compare(x, arr[j]) < 0) j--;
          if (i <= j) {
            swap<T>(ref arr[i], ref arr[j]);
            // swap<T>(arr, i, j);           
            i++; j--;
          }
        } while (i <= j);
        qsort<T>(arr, cmp, a, j);
        qsort<T>(arr, cmp, i, b);
      }
    }

    public static void Quicksort<T>(T[] arr, IGComparer<T> cmp) {
      qsort<T>(arr, cmp, 0, arr.Length - 1);
    }

    public static void CheckSorted<T>(T[] arr, IGComparer<T> cmp) {
      for (int i = 1; i < arr.Length; i++)
        if (cmp.Compare(arr[i], arr[i - 1]) < 0)
          throw new Exception("Polysort.CheckSorted");
    }

    // Polymorphic functional-style quicksort using delegates: general, typesafe

    public delegate int DGComparer<T>(T v1, T v2);

    private static void qsort<T>(T[] arr, DGComparer<T> cmp, int a, int b) {
      // sort arr[a..b]
      if (a < b) {
        int i = a, j = b;
        T x = arr[(i + j) / 2];
        do {
          while (cmp(arr[i], x) < 0) i++;
          while (cmp(x, arr[j]) < 0) j--;
          if (i <= j) {
            swap<T>(ref arr[i], ref arr[j]);
            // swap<T>(arr, i, j);           
            i++; j--;
          }
        } while (i <= j);
        qsort<T>(arr, cmp, a, j);
        qsort<T>(arr, cmp, i, b);
      }
    }

    public static void Quicksort<T>(T[] arr, DGComparer<T> cmp) {
      qsort<T>(arr, cmp, 0, arr.Length - 1);
    }
  }

  public class Polyselfsort {
    private static void swap<T>(T[] arr, int s, int t) {
      T tmp = arr[s]; arr[s] = arr[t]; arr[t] = tmp;
    }

    // Polymorphic OO-style quicksort: general, typesafe
    // Note the type parameter bound in the generic method

    public static void qsort<T>(T[] arr, int a, int b)
      where T : IGSelfComparable<T> {
      // sort arr[a..b]
      if (a < b) {
        int i = a, j = b;
        T x = arr[(i + j) / 2];
        do {
          while (arr[i].CompareTo(x) < 0) i++;
          while (x.CompareTo(arr[j]) < 0) j--;
          if (i <= j) {
            swap<T>(arr, i, j);
            i++; j--;
          }
        } while (i <= j);
        qsort<T>(arr, a, j);
        qsort<T>(arr, i, b);
      }
    }

    public static void Quicksort<T>(T[] arr) where T : IGSelfComparable<T> {
      qsort<T>(arr, 0, arr.Length - 1);
    }
  }

  public interface IOrd<T> {
    int Compare(T a, T b);
  }

  public static class Overloads {
    public static int Compare<IOrdT, T>(T v1, T v2) where IOrdT : IOrd<T> {
      return default(IOrdT).Compare(v1, v2);
    }
  }
  public struct IOrdInt : IOrd<int> {
    public int Compare(int v1, int v2) {
      return v1 < v2 ? -1 : v1 > v2 ? +1 : 0;
     // return v1.CompareTo(v2);
    }
  }

  public struct IOrdString : IOrd<string> {
    public int Compare(string v1, string v2) {
      return string.Compare(v1, v2);
      //return v1.CompareTo(v2);
    }
  }

  public class PolyDictSort {
    private static void swap<T>(T[] arr, int s, int t) {
      T tmp = arr[s]; arr[s] = arr[t]; arr[t] = tmp;
    }

    // Polymorphic OO-style quicksort: general, typesafe
    // Note the type parameter bound in the generic method

    public static void qsort<IOrdT, T>(T[] arr, int a, int b)
      where IOrdT : IOrd<T> {
       IOrdT iordt = default(IOrdT);
      // sort arr[a..b]
      if (a < b) {
        int i = a, j = b;
        T x = arr[(i + j) / 2];
        do {
          while (iordt.Compare(arr[i], x) < 0) i++;
          while (iordt.Compare(x, arr[j]) < 0) j--;
          if (i <= j) {
            swap<T>(arr, i, j);
            i++; j--;
          }
        } while (i <= j);
        qsort<IOrdT, T>(arr, a, j);
        qsort<IOrdT, T>(arr, i, b);
      }
    }

    public static void Quicksort<IOrdT, T>(T[] arr) where IOrdT : struct, IOrd<T> {
      qsort<IOrdT, T>(arr, 0, arr.Length - 1);
    }
  }

  public class PolyDictSort<IOrdT, T> where IOrdT : IOrd<T> {
    private static void swap/*<T>*/(T[] arr, int s, int t) {
      T tmp = arr[s]; arr[s] = arr[t]; arr[t] = tmp;
    }

    // Polymorphic OO-style quicksort: general, typesafe
    // Note the type parameter bound in the generic method

    

    public static void qsort(T[] arr, int a, int b) {
      IOrdT DICT = default(IOrdT);
      // sort arr[a..b]
      if (a < b) {
        int i = a, j = b;
        T x = arr[(i + j) / 2];
        do {
          //         while (Overloads.Compare<IOrdT, T>(arr[i], x) < 0) i++;
          //         while (Overloads.Compare<IOrdT, T>(x, arr[j]) < 0) j--;
          while (DICT.Compare(arr[i], x) < 0) i++;
          while (DICT.Compare(x, arr[j]) < 0) j--;
          if (i <= j) {
            swap/*<T>*/(arr, i, j);
            i++; j--;
          }
        } while (i <= j);
        qsort(arr, a, j);
        qsort(arr, i, b);
      }
    }

    public static void Quicksort(T[] arr) {
      qsort(arr, 0, arr.Length - 1);
    }
  }

  public class PolyICTSort {
    private static void swap<T>(T[] arr, int s, int t) {
      T tmp = arr[s]; arr[s] = arr[t]; arr[t] = tmp;
    }
     
    // Polymorphic OO-style quicksort: general, typesafe
    // Note the type parameter bound in the generic method

    public static void qsort<T>(T[] arr, int a, int b) where T : IComparable<T> {
      // sort arr[a..b]
      if (a < b) {
        int i = a, j = b;
        T x = arr[(i + j) / 2];
        do {
          //         while (Overloads.Compare<IOrdT, T>(arr[i], x) < 0) i++;
          //         while (Overloads.Compare<IOrdT, T>(x, arr[j]) < 0) j--;
          while (arr[i].CompareTo(x) < 0) i++;
          while (x.CompareTo(arr[j]) < 0) j--;
          if (i <= j) {
            swap<T>(arr, i, j);
            i++; j--;
          }
        } while (i <= j);
        qsort(arr, a, j);
        qsort(arr, i, b);
      }
    }
    public static void Quicksort<T>(T[] arr) where T : IComparable<T> {
      qsort(arr, 0, arr.Length - 1);
    }
  }
  public class PolyICTSort<T> where T : IComparable<T> {
    private static void swap/*<T>*/(T[] arr, int s, int t) {
      T tmp = arr[s]; arr[s] = arr[t]; arr[t] = tmp;
    }

    // Polymorphic OO-style quicksort: general, typesafe
    // Note the type parameter bound in the generic method

    public static void qsort(T[] arr, int a, int b) {
      // sort arr[a..b]
      if (a < b) {
        int i = a, j = b;
        T x = arr[(i + j) / 2];
        do {
          //         while (Overloads.Compare<IOrdT, T>(arr[i], x) < 0) i++;
          //         while (Overloads.Compare<IOrdT, T>(x, arr[j]) < 0) j--;
          while (arr[i].CompareTo(x) < 0) i++;
          while (x.CompareTo(arr[j]) < 0) j--;
          if (i <= j) {
            swap/*<T>*/(arr, i, j);
            i++; j--;
          }
        } while (i <= j);
        qsort(arr, a, j);
        qsort(arr, i, b);
      }
    }

    public static void Quicksort(T[] arr) {
      qsort(arr, 0, arr.Length - 1);
    }
  }
  public class Objsort {
    private static void swap(object[] arr, int s, int t) {
      object tmp = arr[s]; arr[s] = arr[t]; arr[t] = tmp;
    }

    // OO-style IComparable quicksort: general, not typesafe

    private static void qsort(IComparable[] arr, int a, int b) {
      // sort arr[a..b]
      if (a < b) {
        int i = a, j = b;
        IComparable x = arr[(i + j) / 2];
        do {
          while (arr[i].CompareTo(x) < 0) i++;
          while (x.CompareTo(arr[j]) < 0) j--;
          if (i <= j) {
            swap(arr, i, j);
            i++; j--;
          }
        } while (i <= j);
        qsort(arr, a, j);
        qsort(arr, i, b);
      }
    }

    public static void Quicksort(IComparable[] arr) {
      qsort(arr, 0, arr.Length - 1);
    }

    public static void CheckSorted(IComparable[] arr) {
      for (int i = 1; i < arr.Length; i++)
        if (arr[i].CompareTo(arr[i - 1]) < 0)
          throw new Exception("Objsort.CheckSorted");
    }
  }

  public class Intsort {
    private static void swap(int[] arr, int s, int t) {
      int tmp = arr[s]; arr[s] = arr[t]; arr[t] = tmp;
    }

    // Plain monomorphic quicksort: not general, but typesafe

    private static void qsort(int[] arr, int a, int b) {
      // sort arr[a..b]
      if (a < b) {
        int i = a, j = b;
        int x = arr[(i + j) / 2];
        do {
          while (arr[i] < x) i++;
          while (x < arr[j]) j--;
          if (i <= j) {
            swap(arr, i, j);
            i++; j--;
          }
        } while (i <= j);
        qsort(arr, a, j);
        qsort(arr, i, b);
      }
    }

    public static void Quicksort(int[] arr) {
      qsort(arr, 0, arr.Length - 1);
    }

    public static void CheckSorted(int[] arr) {
      for (int i = 1; i < arr.Length; i++)
        if (arr[i] < arr[i - 1])
          throw new Exception("Intsort.CheckSorted");
    }
  }

  public class Stringsort {
    private static void swap(string[] arr, int s, int t) {
      string tmp = arr[s]; arr[s] = arr[t]; arr[t] = tmp;
    }

    // Plain monomorphic quicksort: not general, but typesafe

    private static void qsort(string[] arr, int a, int b) {
      // sort arr[a..b]
      if (a < b) {
        int i = a, j = b;
        string x = arr[(i + j) / 2];
        do {
          while (string.Compare(arr[i], x) < 0) i++;
          while (string.Compare(x, arr[j]) < 0) j--;
          if (i <= j) {
            swap(arr, i, j);
            i++; j--;
          }
        } while (i <= j);
        qsort(arr, a, j);
        qsort(arr, i, b);
      }
    }

    public static void Quicksort(string[] arr) {
      qsort(arr, 0, arr.Length - 1);
    }

    public static void CheckSorted(string[] arr) {
      for (int i = 1; i < arr.Length; i++)
        if (string.Compare(arr[i], arr[i - 1]) < 0)
          throw new Exception("Stringsort.CheckSorted");
    }
  }

  public class FunIntsort {
    private static void swap(int[] arr, int s, int t) {
      int tmp = arr[s]; arr[s] = arr[t]; arr[t] = tmp;
    }

    // Monomorphic quicksort with comparer: not general, but typesafe

    private static void qsort(int[] arr, IIntComparer cmp, int a, int b) {
      // sort arr[a..b]
      if (a < b) {
        int i = a, j = b;
        int x = arr[(i + j) / 2];
        do {
          while (cmp.Compare(arr[i], x) < 0) i++;
          while (cmp.Compare(x, arr[j]) < 0) j--;
          if (i <= j) {
            swap(arr, i, j);
            i++; j--;
          }
        } while (i <= j);
        qsort(arr, cmp, a, j);
        qsort(arr, cmp, i, b);
      }
    }

    public static void Quicksort(int[] arr, IIntComparer cmp) {
      qsort(arr, cmp, 0, arr.Length - 1);
    }
  }

  public class FunStringsort {
    private static void swap(string[] arr, int s, int t) {
      string tmp = arr[s]; arr[s] = arr[t]; arr[t] = tmp;
    }

    // Monomorphic quicksort with comparer: not general, but typesafe

    private static void qsort(string[] arr, IStringComparer cmp, int a, int b) {
      // sort arr[a..b]
      if (a < b) {
        int i = a, j = b;
        string x = arr[(i + j) / 2];
        do {
          while (cmp.Compare(arr[i], x) < 0) i++;
          while (cmp.Compare(x, arr[j]) < 0) j--;
          if (i <= j) {
            swap(arr, i, j);
            i++; j--;
          }
        } while (i <= j);
        qsort(arr, cmp, a, j);
        qsort(arr, cmp, i, b);
      }
    }

    public static void Quicksort(string[] arr, IStringComparer cmp) {
      qsort(arr, cmp, 0, arr.Length - 1);
    }
  }

  // Two generic versions of IComparable

  public interface IGComparable<T> {
    int CompareTo(IGComparable<T> that);
  }

  public interface IGSelfComparable<T> {
    // Actually we could assert a bound on the parameter: 
    //   public interface IGSelfComparable< T : IGSelfComparable<T> >
    // but there seems to be no need for that.

    // Note that the argument type is T itself, not a superclass:
    int CompareTo(T that);
  }

  // An int wrapper that implements all Comparable interfaces

  public class OrderedInt : IComparable,
                            IGComparable<OrderedInt>,
                            IGSelfComparable<OrderedInt> {
    int i;

    public OrderedInt(int i) {
      this.i = i;
    }

    public int Value {
      get { return i; }
    }

    // Implements IComparable.CompareTo(object)
    public int CompareTo(object that) {
      int thati = ((OrderedInt)that).i;
      return i < thati ? -1 : i > thati ? +1 : 0;
    }

    // Implements IGComparable<OrderedInt>.CompareTo(IGComparable<OrderedInt>)
    public int CompareTo(IGComparable<OrderedInt> that) {
      int thati = ((OrderedInt)that).i;
      return i < thati ? -1 : i > thati ? +1 : 0;
    }

    // Implements IGSelfComparable<OrderedInt>.CompareTo(T)
    // because with T = OrderedInt we have T : IGSelfComparable<T>
    public int CompareTo(OrderedInt that) {
      // Simple subtraction i-that.i won't do because of possible overflow.
      return i < that.i ? -1 : i > that.i ? +1 : 0;
      // This following is eight times slower, although the compiler 
      // and runtime knows that i and that.i are ints:
      // return i.CompareTo(that.i);
    }
  }

  // A string wrapper that implements all Comparable interfaces

  public class OrderedString : IComparable,
                               IGComparable<OrderedString>,
                               IGSelfComparable<OrderedString> {
    string s;

    public OrderedString(string s) {
      this.s = s;
    }

    public string Value {
      get { return s; }
    }

    // Implements IComparable.CompareTo(object)
    public int CompareTo(object that) {
      return string.Compare(this.s, ((OrderedString)that).s);
    }

    // Implements IGComparable<OrderedString>.CompareTo(IGComparable<OrderedString>)
    public int CompareTo(IGComparable<OrderedString> that) {
      return string.Compare(this.s, ((OrderedString)that).s);
    }

    // Implements IGSelfComparable<OrderedString>.CompareTo(T)
    // because with T = OrderedString we have T : IGSelfComparable<T>
    public int CompareTo(OrderedString that) {
      return string.Compare(this.s, that.s);
    }
  }

  // A generic version of IComparer

  public interface IGComparer<T> {
    int Compare(T v1, T v2);
  }

  public interface IIntComparer {
    int Compare(int v1, int v2);
  }

  public class IntComparer : IGComparer<int>, IIntComparer {
    public int Compare(int v1, int v2) {
      return v1 < v2 ? -1 : v1 > v2 ? +1 : 0;
    }
  }

  public interface IStringComparer {
    int Compare(string v1, string v2);
  }

  public class StringComparer : IGComparer<string>, IStringComparer {
    public int Compare(string v1, string v2) {
      return string.Compare(v1, v2);
    }
  }

  // Try it on integers

  public class Gsort {
    static readonly Random rnd = new Random();

    public static void Run(string[] args) {
      if (args.Length < 1)
        Console.Out.WriteLine("Usage: Gsort <arraysize> [string]\n");
      else {
        int N = int.Parse(args[0]);
        const string fmt = "{0,9:0.00},";
        //if (args.Length < 2 || args[1] != "string") 
        {
          Console.Out.WriteLine("\n Sorting {0} ints", N);
          headers(fmt);
          for (int i = 0; i < 3; i++) {
            int[] arr = mkRandomInts(N);
            Console.Out.Write(fmt, ObjComparable(arr));
            Console.Out.Write(fmt, ObjOrderedInt(arr));
            Console.Out.Write(fmt, MonoIntPrimitive(arr));
            Console.Out.Write(fmt, MonoIntComparer(arr));
            Console.Out.Write(fmt, PolyIGComparable(arr));
            Console.Out.Write(fmt, PolyIGSelfComparable(arr));
            Console.Out.Write(fmt, PolyIGComparer(arr));
            Console.Out.Write(fmt, PolyDGComparer(arr));
            Console.Out.Write(fmt, PolyICIntSort(arr));
            Console.Out.Write(fmt, PolyStaticICIntSort(arr));
            Console.Out.Write(fmt, PolyIOrdIntSort(arr));
            Console.Out.Write(fmt, PolyStaticIOrdIntSort(arr));
            Console.Out.WriteLine();
          }
        }
        //else
        {
          Console.Out.WriteLine("\n Sorting {0} strings", N);
          headers(fmt);
          for (int i = 0; i < 3; i++) {
            string[] arr = mkRandomStrings(N);
            Console.Out.Write(fmt, ObjComparable(arr));
            Console.Out.Write(fmt, ObjOrderedString(arr));
            Console.Out.Write(fmt, MonoStringPrimitive(arr));
            Console.Out.Write(fmt, MonoStringComparer(arr));
            Console.Out.Write(fmt, PolyIGComparable(arr));
            Console.Out.Write(fmt, PolyIGSelfComparable(arr));
            Console.Out.Write(fmt, PolyIGComparer(arr));
            Console.Out.Write(fmt, PolyDGComparer(arr));
            Console.Out.Write(fmt, PolyICStringSort(arr));
            Console.Out.Write(fmt, PolyStaticICStringSort(arr));
            Console.Out.Write(fmt, PolyIOrdStringSort(arr));
            Console.Out.Write(fmt, PolyStaticIOrdStringSort(arr));
            Console.Out.WriteLine();
          }
        }

        {
          Console.Out.WriteLine("\n Sorting {0} tuples", N);
          //Console.Out.Write(fmt, "TplSrt    ");
          //Console.Out.Write(fmt, "StaTplSrt ");
          //Console.Out.Write(fmt, "CmpSrt    ");
          //Console.Out.Write(fmt, "StaCmpSrt ");
          Console.Out.Write(fmt, "ICT    ");
          Console.Out.Write(fmt, "StatICT ");
          Console.Out.Write(fmt, "IOrd    ");
          Console.Out.Write(fmt, "StatIOrd");
          Console.WriteLine();
          for (int i = 0; i < 3; i++) {
            Tuple<int, int>[] tuples = new Tuple<int, int>[N];
            ComparableTuple<int, int>[] cmptuples = new ComparableTuple<int, int>[N];
            mkRandomTuples(N,out tuples,out cmptuples);
            Console.Out.Write(fmt, PolyICCmpTupleSort(cmptuples));
            Console.Out.Write(fmt, PolyStaticICCmpTupleSort(cmptuples));
            Console.Out.Write(fmt, PolyIOrdTupleSort(tuples));
            Console.Out.Write(fmt, PolyStaticIOrdTupleSort(tuples));
            Console.Out.WriteLine();
          }
        }
      }
    }

    static void headers(string fmt) {
      Console.Out.Write(fmt, "general");
      Console.Out.Write(fmt, "general");
      Console.Out.Write(fmt, "not genl");
      Console.Out.Write(fmt, "not genl");
      Console.Out.Write(fmt, "general");
      Console.Out.Write(fmt, "general");
      Console.Out.Write(fmt, "general");
      Console.Out.Write(fmt, "general");
      Console.Out.Write(fmt, "general");
      Console.Out.Write(fmt, "general");
      Console.Out.Write(fmt, "general");
      Console.Out.Write(fmt, "general");
      Console.Out.WriteLine();
      Console.Out.Write(fmt, "not safe");
      Console.Out.Write(fmt, "not safe");
      Console.Out.Write(fmt, "typesafe");
      Console.Out.Write(fmt, "typesafe");
      Console.Out.Write(fmt, "not safe");
      Console.Out.Write(fmt, "typesafe");
      Console.Out.Write(fmt, "typesafe");
      Console.Out.Write(fmt, "typesafe");
      Console.Out.Write(fmt, "typesafe");
      Console.Out.Write(fmt, "typesafe");
      Console.Out.Write(fmt, "typesafe");
      Console.Out.Write(fmt, "typesafe");
      Console.Out.WriteLine();
      Console.Out.Write(fmt, "Comparab");
      Console.Out.Write(fmt, "OrderedI");
      Console.Out.Write(fmt, "Primitiv");
      Console.Out.Write(fmt, "Comparer");
      Console.Out.Write(fmt, "GCompara");
      Console.Out.Write(fmt, "GSelfCom");
      Console.Out.Write(fmt, "IGCompar");
      Console.Out.Write(fmt, "DGCompar");
      Console.Out.Write(fmt, "ICT    ");
      Console.Out.Write(fmt, "StatICT ");
      Console.Out.Write(fmt, "IOrd    ");
      Console.Out.Write(fmt, "StatIOrd");
      Console.Out.WriteLine();
    }

    // The standard OO thing to do, given that int : IComparable
    static double ObjComparable(int[] arr) {
      int n = arr.Length;
      // Objsort.Quicksort(arr) would be illegal since int[] cannot be
      // converted to IComparable[], even though int : IComparable.
      IComparable[] oarr = new IComparable[n];
      for (int i = 0; i < n; i++)
        oarr[i] = arr[i];  // using that int : IComparable
      Timer t = new Timer();
      Objsort.Quicksort(oarr);
      //    print(oarr);
      return t.Check();
    }

    // Here we're using our own int wrapper, instead of IComparable (faster)
    static double ObjOrderedInt(int[] arr) {
      int n = arr.Length;
      OrderedInt[] oarr = new OrderedInt[n];
      for (int i = 0; i < n; i++)
        oarr[i] = new OrderedInt(arr[i]);
      Timer t = new Timer();
      Objsort.Quicksort(oarr);
      //    print(oarr);
      return t.Check();
    }

    static double MonoIntPrimitive(int[] arr) {
      int n = arr.Length;
      int[] narr = new int[n];
      for (int i = 0; i < n; i++)
        narr[i] = arr[i];
      Timer t = new Timer();
      Intsort.Quicksort(narr);
      //    print(narr);
      return t.Check();
    }

    static double MonoIntComparer(int[] arr) {
      int n = arr.Length;
      int[] narr = new int[n];
      for (int i = 0; i < n; i++)
        narr[i] = arr[i];
      Timer t = new Timer();
      FunIntsort.Quicksort(narr, new IntComparer());
      //    print(narr);
      return t.Check();
    }

    static double PolyIGComparable(int[] arr) {
      int n = arr.Length;
      OrderedInt[] oarr = new OrderedInt[n];
      for (int i = 0; i < n; i++)
        oarr[i] = new OrderedInt(arr[i]);
      Timer t = new Timer();
      Polysort.Quicksort<OrderedInt>(oarr);
      //    print(oarr);
      return t.Check();
    }

    static double PolyIGSelfComparable(int[] arr) {
      int n = arr.Length;
      OrderedInt[] oarr = new OrderedInt[n];
      for (int i = 0; i < n; i++)
        oarr[i] = new OrderedInt(arr[i]);
      Timer t = new Timer();
      Polyselfsort.Quicksort<OrderedInt>(oarr);
      //    print(oarr);
      return t.Check();
    }

    static double PolyIGComparer(int[] arr) {
      int n = arr.Length;
      int[] narr = new int[n];
      for (int i = 0; i < n; i++)
        narr[i] = arr[i];
      Timer t = new Timer();
      Polysort.Quicksort<int>(narr, new IntComparer());
      //    print(narr);
      return t.Check();
    }

    static int intCompare(int v1, int v2) {
      return v1 < v2 ? -1 : v1 > v2 ? +1 : 0;
    }

    static double PolyDGComparer(int[] arr) {
      int n = arr.Length;
      int[] narr = new int[n];
      for (int i = 0; i < n; i++)
        narr[i] = arr[i];
      Timer t = new Timer();
      Polysort.Quicksort<int>(narr, new Polysort.DGComparer<int>(intCompare));
      //    print(narr);
      return t.Check();
    }

    static double PolyIOrdIntSort(int[] arr) {
      int n = arr.Length;
      int[] narr = new int[n];
      for (int i = 0; i < n; i++)
        narr[i] = arr[i];
      Timer t = new Timer();
      PolyDictSort.Quicksort<IOrdInt, int>(narr);
      //    print(narr);
      return t.Check();
    }

    static double PolyStaticIOrdIntSort(int[] arr) {
      int n = arr.Length;
      int[] narr = new int[n];
      for (int i = 0; i < n; i++)
        narr[i] = arr[i];
      Timer t = new Timer();
      PolyDictSort<IOrdInt, int>.Quicksort(narr);
      //    print(narr);
      return t.Check();
    }

    static double PolyICIntSort(int[] arr) {
      int n = arr.Length;
      int[] narr = new int[n];
      for (int i = 0; i < n; i++)
        narr[i] = arr[i];
      Timer t = new Timer();
      PolyICTSort.Quicksort<int>(narr);
      //    print(narr);
      return t.Check();
    }

    static double PolyStaticICIntSort(int[] arr) {
      int n = arr.Length;
      int[] narr = new int[n];
      for (int i = 0; i < n; i++)
        narr[i] = arr[i];
      Timer t = new Timer();
      PolyICTSort<int>.Quicksort(narr);
      //    print(narr);
      return t.Check();
    }



    static double PolyIOrdTupleSort(Tuple<int,int>[] arr) {
      int n = arr.Length;
      Tuple<int, int>[] narr = new Tuple<int, int>[n];
      for (int i = 0; i < n; i++)
        narr[i] = arr[i];
      Timer t = new Timer();
      PolyDictSort.Quicksort<IOrdTuple<IOrdInt,IOrdInt,int,int>, Tuple<int,int>>(narr);
      //    print(narr);
      return t.Check();
    }

    static double PolyStaticIOrdTupleSort(Tuple<int, int>[] arr) {
      int n = arr.Length;
      Tuple<int, int>[] narr = new Tuple<int, int>[n];
      for (int i = 0; i < n; i++)
        narr[i] = arr[i];
      Timer t = new Timer();
      PolyDictSort<IOrdTuple<IOrdInt, IOrdInt, int, int>, Tuple<int, int>>.Quicksort(narr);
      //    print(narr);
      return t.Check();
    }

    static double PolyICCmpTupleSort(ComparableTuple<int,int>[] arr) {
      int n = arr.Length;
      ComparableTuple<int, int>[] narr = new ComparableTuple<int, int>[n];
      for (int i = 0; i < n; i++)
        narr[i] = arr[i];
      Timer t = new Timer();
      PolyICTSort.Quicksort<ComparableTuple<int,int>>(narr);
      //    print(narr);
      return t.Check();
    }

    static double PolyStaticICCmpTupleSort(ComparableTuple<int, int>[] arr) {
      int n = arr.Length;
      ComparableTuple<int, int>[] narr = new ComparableTuple<int, int>[n];
      for (int i = 0; i < n; i++)
        narr[i] = arr[i];
      Timer t = new Timer();
      PolyICTSort<ComparableTuple<int, int>>.Quicksort(narr);
      //    print(narr);
      return t.Check();
    }
    // Eight ways to sort strings

    // The standard OO thing to do, given that string : IComparable
    static double ObjComparable(string[] arr) {
      int n = arr.Length;
      // Objsort.Quicksort(arr) would be illegal since string[] cannot be
      // converted to IComparable[], even though string : IComparable.
      IComparable[] oarr = new IComparable[n];
      for (int i = 0; i < n; i++)
        oarr[i] = arr[i];  // using that string : IComparable
      Timer t = new Timer();
      Objsort.Quicksort(oarr);
      //    print(oarr);
      return t.Check();
    }

    // Here we're using our own string wrapper, instead of IComparable (faster)
    static double ObjOrderedString(string[] arr) {
      int n = arr.Length;
      OrderedString[] oarr = new OrderedString[n];
      for (int i = 0; i < n; i++)
        oarr[i] = new OrderedString(arr[i]);
      Timer t = new Timer();
      Objsort.Quicksort(oarr);
      //    print(oarr);
      return t.Check();
    }

    static double MonoStringPrimitive(string[] arr) {
      int n = arr.Length;
      string[] narr = new string[n];
      for (int i = 0; i < n; i++)
        narr[i] = arr[i];
      Timer t = new Timer();
      Stringsort.Quicksort(narr);
      //    print(narr);
      return t.Check();
    }

    static double MonoStringComparer(string[] arr) {
      int n = arr.Length;
      string[] narr = new string[n];
      for (int i = 0; i < n; i++)
        narr[i] = arr[i];
      Timer t = new Timer();
      FunStringsort.Quicksort(narr, new StringComparer());
      //    print(narr);
      return t.Check();
    }

    static double PolyIGComparable(string[] arr) {
      int n = arr.Length;
      OrderedString[] oarr = new OrderedString[n];
      for (int i = 0; i < n; i++)
        oarr[i] = new OrderedString(arr[i]);
      Timer t = new Timer();
      Polysort.Quicksort<OrderedString>(oarr);
      //    print(oarr);
      return t.Check();
    }

    static double PolyIGSelfComparable(string[] arr) {
      int n = arr.Length;
      OrderedString[] oarr = new OrderedString[n];
      for (int i = 0; i < n; i++)
        oarr[i] = new OrderedString(arr[i]);
      Timer t = new Timer();
      Polyselfsort.Quicksort<OrderedString>(oarr);
      //    print(oarr);
      return t.Check();
    }

    static double PolyIGComparer(string[] arr) {
      int n = arr.Length;
      string[] narr = new string[n];
      for (int i = 0; i < n; i++)
        narr[i] = arr[i];
      Timer t = new Timer();
      Polysort.Quicksort<string>(narr, new StringComparer());
      //    print(narr);
      return t.Check();
    }

    static double PolyDGComparer(string[] arr) {
      int n = arr.Length;
      string[] narr = new string[n];
      for (int i = 0; i < n; i++)
        narr[i] = arr[i];
      Timer t = new Timer();
      // Note that string.Compare plugs right into the delegate:
      Polysort.Quicksort<string>(narr, new Polysort.DGComparer<string>(string.Compare));
      //    print(narr);
      return t.Check();
    }

    static double PolyIOrdStringSort(string[] arr) {
      int n = arr.Length;
      string[] narr = new string[n];
      for (int i = 0; i < n; i++)
        narr[i] = arr[i];
      Timer t = new Timer();
      PolyDictSort.Quicksort<IOrdString, string>(narr);
      //    print(narr);
      return t.Check();
    }

    static double PolyStaticIOrdStringSort(string[] arr) {
      int n = arr.Length;
      string[] narr = new string[n];
      for (int i = 0; i < n; i++)
        narr[i] = arr[i];
      Timer t = new Timer();
      PolyDictSort<IOrdString, string>.Quicksort(narr);
      //    print(narr);
      return t.Check();
    }

    static double PolyICStringSort(string[] arr) {
      int n = arr.Length;
      string[] narr = new string[n];
      for (int i = 0; i < n; i++)
        narr[i] = arr[i];
      Timer t = new Timer();
      PolyICTSort.Quicksort<string>(narr);
      //    print(narr);
      return t.Check();
    }

    static double PolyStaticICStringSort(string[] arr) {
      int n = arr.Length;
      string[] narr = new string[n];
      for (int i = 0; i < n; i++)
        narr[i] = arr[i];
      Timer t = new Timer();
      PolyICTSort<string>.Quicksort(narr);
      //    print(narr);
      return t.Check();
    }

    // Create arrays of random ints

    static int[] mkRandomInts(int n) {
      int[] arr = new int[n];
      for (int i = 0; i < n; i++)
        arr[i] = rnd.Next(100000000);
      return arr;
    }

    // Create arrays of random strings

    static string[] mkRandomStrings(int n) {
      string[] arr = new string[n];
      for (int i = 0; i < n; i++)
        arr[i] = mkRandomString(5 + rnd.Next(15));
      return arr;
    }

    static string mkRandomString(int n) {
      System.Text.StringBuilder sb = new System.Text.StringBuilder();
      for (int i = 0; i < n; i++)
        sb.Append((char)(65 + rnd.Next(26) + 32 * rnd.Next(2)));
      return sb.ToString();
    }
    
    // MkRandomTuples
    static void mkRandomTuples(int n, out Tuple<int,int>[] tuples, out ComparableTuple<int,int> [] cmptuples ) {
      int[] As = mkRandomInts(n);
      int[] Bs = mkRandomInts(n);
      tuples = new Tuple<int, int>[n];
      cmptuples = new ComparableTuple<int, int>[n];
      for (int i = 0; i < n; i++) {
        tuples[i] = new Tuple<int, int>(As[i], Bs[i]);
        cmptuples[i] = new ComparableTuple<int, int>(As[i], Bs[i]);
      }
    }
      
    static void print(int[] arr) {
      for (int i = 0; i < arr.Length; i++)
        Console.Write("{0} ", arr[i]);
      Console.WriteLine();
    }

    static void print(IComparable[] arr) {
      for (int i = 0; i < arr.Length; i++)
        Console.Write("{0} ", (int)arr[i]);
      Console.WriteLine();
    }

    static void print(OrderedInt[] arr) {
      for (int i = 0; i < arr.Length; i++)
        Console.Write("{0} ", arr[i].Value);
      Console.WriteLine();
    }
  }

  public class Timer {
    private DateTime start;

    public Timer() {
      start = DateTime.Now;
    }

    public double Check() {
      TimeSpan dur = DateTime.Now - start;
      return dur.TotalSeconds;
    }
  }

  public struct IOrdTuple<IOrdA, IOrdB, A, B> : IOrd<Tuple<A, B>>
    where IOrdA : struct, IOrd<A>
    where IOrdB : struct, IOrd<B> {
    
    public int Compare(Tuple<A,B> t, Tuple<A, B> u) {
      //int ca = Overloads.Compare<IOrdA, A>(t.a, u.a);
      //int cb = Overloads.Compare<IOrdB, B>(t.b, u.b);
      int ca = default(IOrdA).Compare(t.a, u.a);
      int cb = default(IOrdB).Compare(t.b, u.b);
      return (ca == 0) ? cb : ca;
    }
  }

  public class Tuple<A, B> {
    public A a;
    public B b;
    public Tuple(A a, B b) {
      this.a = a;
      this.b = b;
    }
  }

  public class ComparableTuple<A, B>  : IComparable<ComparableTuple<A,B>> where A: IComparable<A> where B: IComparable<B>
  {
    public A a;
    public B b;
    public ComparableTuple(A a, B b) {
      this.a = a;
      this.b = b;
    }
    public int CompareTo(ComparableTuple<A,B> that)  {
      int ca = this.a.CompareTo(that.a);
      int cb = this.b.CompareTo(that.b);
      return (ca == 0) ? cb : ca;
    }
  }

  /*

  Fifth run 2001-10-31: Runtime in seconds for sorting 1 million
  random integers in the range 0-99999999 on a 1 GHz PIII and
  retail CLRG 1.00.3417 (Generic):

  C:\cs>gsort 1000000
    general  general not genl not genl  general  general  general  general
   not safe not safe typesafe typesafe not safe typesafe typesafe typesafe
   Comparab OrderedI IntPrimi IntCompa GCompara GSelfCom IGCompar DGCompar
       4.93     3.06     0.47     1.07     3.09     2.33     1.04     1.84
       4.94     3.02     0.46     1.05     3.08     2.32     1.05     1.83
       4.96     3.03     0.47     1.03     3.07     2.34     1.03     1.83
       4.96     3.03     0.46     1.05     3.07     2.33     1.04     1.83
       4.94     3.04     0.46     1.05     3.08     2.33     1.05     1.83
       4.95     3.03     0.47     1.04     3.08     2.33     1.05     1.83
       4.96     3.03     0.46     1.03     3.07     2.34     1.04     1.83
       4.95     3.03     0.46     1.05     3.07     2.33     1.04     1.84
       4.95     3.02     0.46     1.04     3.08     2.33     1.04     1.84
       4.95     3.03     0.47     1.03     3.09     2.33     1.05     1.83
       4.96     3.04     0.46     1.04     3.07     2.33     1.04     1.84
       4.95     3.04     0.46     1.05     3.08     2.32     1.05     1.82
       4.94     3.03     0.46     1.04     3.07     2.32     1.04     1.83
       4.96     3.02     0.47     1.03     3.07     2.34     1.04     1.84
       4.96     3.03     0.46     1.04     3.07     2.33     1.04     1.84
       4.95     3.04     0.46     1.05     3.08     2.32     1.05     1.84
       4.93     3.03     0.46     1.04     3.07     2.33     1.05     1.84
       4.96     3.03     0.46     1.04     3.06     2.34     1.04     1.83
       4.96     3.03     0.46     1.05     3.06     2.32     1.03     1.83
       4.94     3.03     0.46     1.05     3.10     2.33     1.05     1.83


  Sixth run, with random strings, 2001-11-01:

   Sorting 200000 strings
    general  general not genl not genl  general  general  general  general
   not safe not safe typesafe typesafe not safe typesafe typesafe typesafe
   Comparab OrderedI Primitiv Comparer GCompara GSelfCom IGCompar DGCompar
       3.25     2.64     2.14     2.22     2.61     2.58     2.22     2.89
       3.12     2.50     2.04     2.13     2.50     2.45     2.12     2.74
       3.15     2.54     2.08     2.17     2.55     2.51     2.17     2.81
       3.24     2.65     2.15     2.25     2.64     2.61     2.24     2.91
       3.16     2.56     2.08     2.17     2.56     2.53     2.17     2.81
       3.19     2.58     2.10     2.19     2.58     2.55     2.19     2.84
       3.22     2.60     2.11     2.20     2.60     2.54     2.20     2.86
       3.09     2.47     2.03     2.11     2.47     2.45     2.10     2.73
       3.31     2.74     2.19     2.29     2.70     2.68     2.28     2.96
       3.17     2.55     2.09     2.17     2.56     2.51     2.17     2.81
       3.14     2.52     2.05     2.14     2.52     2.50     2.15     2.78
       3.18     2.60     2.11     2.19     2.58     2.56     2.19     2.83
       3.07     2.47     2.01     2.10     2.46     2.42     2.11     2.74
       3.29     2.67     2.18     2.27     2.66     2.63     2.26     2.94
       3.18     2.58     2.10     2.22     2.56     2.54     2.19     2.84
       3.17     2.57     2.09     2.19     2.57     2.53     2.18     2.83
       3.09     2.49     2.03     2.12     2.49     2.45     2.11     2.75
       3.15     2.53     2.07     2.16     2.54     2.50     2.18     2.83
       3.17     2.56     2.08     2.18     2.56     2.52     2.16     2.82
       3.33     2.70     2.21     2.39     2.71     2.68     2.32     2.98

  Don Syme: 

  · Also I think it’s worth adding an issue regarding
    IGComparable – the results are slower for this than they
    could be, because we box one of the integers (the first).
    Also the terminology “typesafe” could be clarified to
    “prone to typecheck failures at runtime”, which will
    mean more to the runtime team.

  · If it were possible to repeat the sorting examples on (a)
    strings (b) some value type, e.g. DateTime and (c) some kind
    of boxed record where you sort on one of the fields then that
    would be great.


  Seventh run, with random ints, fixed bbt runtime (RAID 109)
  2001-11-06, all sharing enabled, lazy dictionary lookup:

   Sorting 1000000 ints
    general  general not genl not genl  general  general  general  general
   not safe not safe typesafe typesafe not safe typesafe typesafe typesafe
   Comparab OrderedI Primitiv Comparer GCompara GSelfCom IGCompar DGCompar
       4.93     2.98     0.46     1.03     3.27     2.41     1.11     1.88
       5.02     3.11     0.46     1.06     3.15     2.44     1.16     1.91
       4.96     3.03     0.45     1.04     3.11     2.42     1.14     1.95
       4.98     3.02     0.46     1.03     3.08     2.40     1.13     1.86
       5.04     3.13     0.47     1.06     3.18     2.48     1.15     1.94
       4.91     3.02     0.45     1.04     3.14     2.41     1.12     1.87
       4.92     3.03     0.46     1.04     3.10     2.41     1.14     1.88
       5.12     3.22     0.49     1.09     3.24     2.55     1.18     2.00
       4.89     3.01     0.46     1.04     3.05     2.39     1.12     1.86
       4.92     3.01     0.45     1.03     3.05     2.39     1.12     1.84
       5.02     3.12     0.46     1.06     3.19     2.47     1.14     1.90
       5.02     3.11     0.48     1.05     3.17     2.46     1.14     1.91
       4.97     3.09     0.45     1.05     3.11     2.44     1.13     1.90
       4.99     3.09     0.47     1.06     3.14     2.45     1.15     1.89
       5.14     3.26     0.51     1.10     3.33     2.58     1.17     2.00
       5.06     3.16     0.48     1.08     3.21     2.49     1.16     1.95
       4.93     3.02     0.46     1.05     3.09     2.41     1.13     1.88
       4.91     3.00     0.45     1.03     3.06     2.39     1.12     1.85
       4.97     3.08     0.47     1.06     3.19     2.46     1.14     1.94
       5.06     3.18     0.49     1.10     3.23     2.54     1.19     2.00

  Eighth run, with random ints, fixed bbt runtime (RAID 109)
  2001-11-06, all sharing disabled, no lazy dictionary lookup:

   Sorting 1000000 ints
    general  general not genl not genl  general  general  general  general
   not safe not safe typesafe typesafe not safe typesafe typesafe typesafe
   Comparab OrderedI Primitiv Comparer GCompara GSelfCom IGCompar DGCompar
       5.01     3.10     0.47     1.05     3.13     2.37     1.07     1.85
       4.90     3.00     0.45     1.05     2.99     2.29     1.05     1.81
       4.96     3.06     0.46     1.05     3.03     2.32     1.06     1.83
       4.96     3.08     0.45     1.05     3.05     2.34     1.05     1.82
       4.93     3.03     0.45     1.04     3.01     2.30     1.05     1.81
       4.94     3.04     0.47     1.05     3.01     2.31     1.03     1.82
       4.99     3.10     0.47     1.06     3.07     2.35     1.07     1.86
       4.84     2.94     0.45     1.03     2.90     2.25     1.02     1.80
       4.96     3.07     0.47     1.06     3.03     2.31     1.04     1.84
       4.96     3.09     0.46     1.05     3.04     2.33     1.04     1.85
       4.97     3.10     0.47     1.06     3.06     2.34     1.04     1.83
       5.05     3.14     0.47     1.06     3.11     2.38     1.07     1.88
       5.06     3.14     0.50     1.07     3.13     2.39     1.08     1.91
       5.19     3.29     0.49     1.12     3.23     2.47     1.10     1.96
       4.96     3.08     0.45     1.05     3.05     2.32     1.06     1.83
       4.95     3.06     0.46     1.06     3.04     2.32     1.05     1.84
       4.95     3.05     0.45     1.05     3.03     2.30     1.05     1.82
       4.94     3.03     0.45     1.04     3.01     2.30     1.05     1.80
       4.97     3.10     0.45     1.05     3.06     2.34     1.05     1.84
       4.96     3.08     0.45     1.06     3.06     2.35     1.05     1.86

  */


}