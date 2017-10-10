using System;
using System.Collections.Generic;
using static System.Console;

namespace ShapesComparison
{
    /// <summary>
    /// Side-by-side comparison between the current Concept-C# prototype
    /// and the
    /// <a href="https://github.com/dotnet/csharplang/issues/164">
    /// Shapes and Extensions proposal</a>.
    /// The text of the proposal is contained inline with a few changes.
    /// </summary>
    class Program
    {
        /*
        # Shapes and Extensions
        
        This is essentially a merger of two other proposals:
        
        1. Extension everything, which allows types to be extended with most
           kinds of members in the manner of extension methods, and

        2. Type Classes, which provide abstraction over sets of operations
            that can be added to a type separate from the type itself.

        These two features have good synergy, and would benefit from being
        designed together. This proposal is a concrete shot at doing so,
        knowing full well that there are a myriad of different decisions that
        could be made. This is not to be particularly opinionated about those
        choices - it's just easier to understand and discuss a general
        proposal when it has a concrete shape.
        
        # Extensions

        The idea behind "extension everything" in most proposals is to use a
        different approach to declaration syntax from today's "static methods
        with a this modifier", instead providing a type-like declaration with
        a name, an indication of the type to be extended, and a set of member
        declarations for that type. This syntactic approach generalizes more
        easily to other member kinds, including properties, static members
        and even operators.
        
        Here is an example adding and using a static property:
        */

        /********************************  // public extension IntZero of int
         * No Concept-C# equivalent yet *  // {
         ********************************/ //    public static int Zero => 0;
                                           // }
                                           //
                                           // WriteLine(5 + int.Zero); // in the scope of the extension, int has a Zero property

        /* 
        The name of the extension declaration (like that of the static class
        containing an extension method today) is useful primarily for
        disambiguation purposes. We'll get back to that later.
        
        What should extension declarations compile into? The straightforward
        answer is a static class with static members (taking an extra
        parameter for the receiver if necessary). However, as we'll see
        below, this proposal suggests a different approach.
        
        # Shapes
        
        Interfaces abstract over the shape of objects and values that are
        instances of types. The idea behind type classes is essentially to
        abstract over the shapes of the types themselves instead.
        Furthermore, where a type needs to opt in through its declaration to
        implement an interface, somebody else can make it implement a type
        class in separate code.
        
        In C#, let's call type classes "shapes":
        */

        public concept CGroup<T>      // public shape SGroup<T>
        {                             // {
            T operator +(T t1, T t2); //     static T operator +(T t1, T t2);
            T Zero { get; }           //     static T Zero { get; }
        }                             // }
        
        /*
        This declaration says that a type can be an SGroup<T> if it
        implements a + operator over T, and a Zero static property.

        As an example, the int type is halfway to implementing SGroup<int>,
        since it has a + operator over int, and above we showed how to use an
        extension to add a static int-valued property Zero. Let's make it so
        that an extension declaration can also declare that the extended type
        implements a given shape:
        */
        
        public instance IntGroup : CGroup<int>       // public extension IntGroup of int : SGroup<int>
        {                                            // {
            // TODO: auto-forward this.              //
            int operator +(int t1, int t2) => t1+t2; //
            int Zero => 0;                           //     public static int Zero => 0;
        }                                            // }
        
        /* 
        This declaration extends int not only with the Zero property, but
        with SGroup<int>-ness. In the scope of this extension, int is known
        to be an SGroup<int>.

        In general, a "shape" declaration is very much like an interface
        declaration, except that it:

        - Can define almost any kind of member (including static members)
        - Can be implemented by an extension
        - Can be used like a type only in certain places

        That last restriction is important: a shape is not a type. Instead,
        the primary purpose of a shape is to be used as a generic constraint,
        limiting type arguments to have the right shape, while allowing the
        body of the generic declaration to make use of that shape:
        */

        public static T AddAll<T, implicit W>(T[] ts) where W : CGroup<T> // public static T AddAll<T>(T[] ts) where T : SGroup<T> // shape used as constraint
        {                                                                 // {
            var result = W.Zero;                                          //     var result = T.Zero; // Making use of the shape's Zero property
            foreach (var t in ts) { result += t; }                        //     foreach (var t in ts) { result += t; } // Making use of the shape's + operator
            return result;                                                //     return result;
        }                                                                 // }
        
        /*
        So as an important special case, shapes address the long-desired goal
        of abstracting numeric and computational code over the specific data
        types being manipulated, while allowing clean use of operators.

        Let's call the AddAll method with some ints:
        */

        public static void InlineTest1()
        {
            int[] numbers = { 5, 1, 9, 2, 3, 10, 8, 4, 7, 6 }; // int[] numbers = { 5, 1, 9, 2, 3, 10, 8, 4, 7, 6 };
            WriteLine(AddAll(numbers));                        // WriteLine(AddAll(numbers)); // infers T = int
        }

        /* 
        Clearly we need to check the constraint at the call site. If this is
        called within the scope of the IntGroup extension declaration above,
        the compiler does indeed know that T = int satisfies the SGroup<T>
        constraint. However, there is more going on: how does the AddAll
        method know how int is an SGroup<int> - at the call site? There needs
        to be more information passed in than just the (inferred) int type
        argument and the numbers array.

        # Implementation

        There is an implementation trick at play here, which is stolen
        straight out of the type classes proposal referenced above. The trick
        starts as follows:

        - Shapes are translated into interfaces, with each member (even
          static ones) turning into an instance member on the interface
        - Extensions are translated into structs, with each member (even
          static ones) turning into an instance member on the struct 
        - If the extension implements one or more shapes, then the underlying
          struct implements the underlying interfaces of those shapes
          
        In our example, the shape
        and extension declarations translate into this:
        
        public interface SGroup<T>
        {
            T op_Addition(T t1, T t2); // Can't use "operator +" here
            T Zero { get; }
        }
        
        public struct IntGroup : SGroup<int>
        {
            public int op_Addition(int i1, int i2) => i1 + i2;
            public int Zero => 0;
        }

        (Instance declarations of operators aren't allowed in C#, so those +
        "methods" are encoded as instance methods, just as today's operator
        declarations are actually encoded as static methods in IL).
        
        Note that the struct encoding of IntGroup has a declaration for the +
        operator even though the original extension declaration doesn't. It
        captures what it thinks + means on ints, and thus fulfills the
        SGroup<int> interface.
        
        The generic method taking shape-constrained type
        parameters is translated as follows:
        
        - For each type parameter that is constrained by one or more shapes,
          the generic method actually gets an extra type parameter
          constrained by struct and by the underlying interfaces of those
          shapes
        - The method creates and keeps an instance of each of those extra
          type parameters (it can because of the struct constraint)
        - Whenever an operation from the shape is used in the body, the
          translation instead calls it on the corresponding instance
          
        Let's see that on the AddAll method from above:
        
        public static T AddAll<T, Impl>(T[] ts) where Impl : struct, SGroup<T>
        {
            var impl = new Impl();
            var result = impl.Zero;
            foreach (var t in ts) { result = impl.op_Addition(result, t); }
            return result;
        }
        
        See how the extra Impl type parameter carries the knowledge of how to
        do + and Zero into the method. The benefit of doing this with a
        struct type parameter, rather than, say, extra delegate parameters,
        is that the runtime does a really good job of optimizing it: it will
        specialize the generic method code for each different struct it gets
        called with, so that the method body can inline and optimize the
        specific + and Zero implementations.  Measurements on the linked type
        classes proposal show incredibly good performance, with near-zero
        cost to the abstraction.
        
        In general I show the translations instantiating the Impl structs
        once when possible, but it is essentially free to create an instance
        of an empty struct, so we could also consider instantiating it every
        single time we need to call a member on it.  That's less readable
        though.
        
        Finally, there's a bit of extra work for the call site: It needs to
        infer and pass that extra type argument:

        int[] numbers = { 5, 1, 9, 2, 3, 10, 8, 4, 7, 6 };
        WriteLine(AddAll<int, IntGroup>(numbers));
        
        It infers T = int the
        normal way, and then looks to find exactly one declaration in scope
        that implements SGroup<int> on int. Finding the IntGroup extension,
        it passes its underlying struct type. In case of ambiguities, the
        original code needs to disambiguate, just as when more than one
        extension applies elsewhere. We'll get to that later.
        
        # Implementing shapes directly
        
        Once shapes are in the world, new types will want to
        implement them directly, instead of via an extension declaration:
        */

        /********************************  // public struct Z10 : SGroup<Z10>
         * No Concept-C# equivalent yet *  // {
         ********************************/ //     public readonly int I;
                                           //     public Z10(int i) => I = i % 10;
                                           //     public static Z10 operator +(Z10 z1, Z10 z2) => new Z10(z1.I + z2.I);
                                           //     public static Z10 Zero => new Z10(0);
                                           // }

        /*
        This is easily supported by simply a) checking that the type does
        indeed conform to the shape, and b) generating an extension next to
        the type declaration, or rather its underlying struct, witnessing the
        implementation:
        
        public struct Z10
        {
            public readonly int I;
            public Z10(int i) => I = i % 10;
            public static Z10 operator +(Z10 z1, Z10 z2) => new Z10(z1.I + z2.I);
            public static Z10 Zero => new Z10(0);
        }

        public struct __Z10_SComparable : SGroup<Z10>
        {
            public Z10 op_Addition(Z10 t1, Z10 t2) => t1 + t2;
            public Z10 Zero => Z10.Zero;
        }

        Whenever the Z10 type is in scope, so is the fact that it is an SGroup<Z10>.

        # Instance members

        So far we only explored shapes and extensions for static members, but
        they should apply equally to instance members.
        */
        
        public concept CComparable<This, T>                   // public shape SComparable<T>
        {                                                     // {
            int CompareTo(this This me, T t);                 //     int CompareTo(T t);
        }                                                     // }
                                                              //
        public instance IntComparable : CComparable<int, int> // public extension IntComparable of int : SComparable<int>
        {                                                     // {
            int CompareTo(this int me, int t) => me - t;      //     public int CompareTo(int t) => this - t;
        }                                                     // }
        
        /*
        In order to create the underlying interface for SComparable<T> we
        need to take a page out of the current extension methods feature and
        add an extra parameter to convey the receiver of the CompareTo call.
        What should be the type of that receiver? Well that depends on what
        type the shape is ultimately implemented on. In other words, we need
        to give the interface an extra type parameter representing the "this
        type", and let implementers fill that in:
        
        public interface SComparable<This, T>
        {
            int CompareTo(This @this, T t);
        }

        public struct IntComparable : SComparable<int, int>
        {
            public int CompareTo(int @this, int t) => @this - t;
        }

        Essentially, any shape that defines instance members needs to also
        have an extra This type parameter.
        
        From there on, the translation of generic methods over these shapes
        is unsurprising. This method:
        */

        public static T Max<T, implicit W>(T[] ts) where W : CComparable<T, T> // public static T Max<T>(T[] ts) where T : SComparable<T>
        {                                                                      // {
            var result = ts[0];                                                //     var result = ts[0];
            foreach (var t in ts) { if (result.CompareTo(t) < 0) result = t; } //     foreach (var t in ts) { if (result.CompareTo(t) < 0) result = t; }
            return result;                                                     //     return result;
        }                                                                      // }

        /*
        Translates to this:

        public static T Max<T, Impl>(T[] ts) where Impl : struct, SComparable<T, T>
        {
            var impl = new Impl();

            var result = ts[0];
            foreach (var t in ts) { if (impl.CompareTo(result, t) < 0) result = t; }
            return result;
        }

        The instance method call result.CompareTo(t) "on" the result gets translated into an instance method call impl.CompareTo(result, t) on the impl struct, taking the "receiver" as a first parameter.

        # Extending interfaces with shapes

        Note that the shape SComparable<T> is almost identical to the
        existing interface IComparable<T>. Obviously there's a completely
        trivial implementation of SComparable<T> on any T that implements
        IComparable<T>, and so we can write that implementation once and for
        all by extending the interface itself:
        */
        
        // TODO: make dropping the body equivalent to an empty body        //
        public instance Comparable<T> : CComparable<IComparable<T>, T> {}  // public extension Comparable<T> of IComparable<T> : SComparable<T> ;

        /*
        We don't even need to provide a body; the compiler can just figure it
        out. We just have to say it to make it true (and to declare the
        underlying struct to "witness" the SComparableness to generic
        methods).
        
        Under the hood, the compiler translates to:

        public struct Comparable<T> : SComparable<T, T> where T: IComparable<T>
        {
            public int CompareTo(T @this, T t) => @this.CompareTo(t);
        }

        # Shapes in generic types

        So far we've seen generic methods with type parameters constrained by
        shapes. We can do the same for generic classes, where a given type
        argument gets to come in with its own way of doing certain things.

        As an example let's build a SortedList<T> where T needs to be
        SComparable<T>. This will then work both for T's that inherently
        implement IComparable<T> (and hence SComparable<T> if the previous
        section is applied), but for other T's the instantiator of
        SortedList<T> can apply an extension and imbue T with a suitable
        comparison to apply inside of the list (please forgive algorithmic
        errors! =D):
        */

        public class SortedList<T, implicit W> where W : CComparable<T, T> // public class SortedList<T> where T : SComparable<T>
        {                                                                  // {
            List<T> ts = new List<T>();                                    //     List<T> ts = new List<T>();
                                                                           // 
            public void Add(T t)                                           //     public void Add(T t)
            {                                                              //     {
                int l = 0, r = ts.Count;                                   //         int l = 0, r = ts.Count;
                while (l < r)                                              //         while (l < r)
                {                                                          //         {
                    int m = (l + r) / 2;                                   //             int m = (l + r) / 2;
                    if (t.CompareTo(ts[m]) < 0) { r = m; }                 //             if (t.CompareTo(ts[m]) < 0) { r = m; }
                    else { l = m + 1; }                                    //             else { l = m + 1; }
                }                                                          //         }
                ts.Insert(l, t);                                           //         ts.Insert(l, t); // <- omitted in original example
            }                                                              //     }
        }                                                                  // }

        /*
        We can implement this much like we do with generic methods, adding an
        extra type parameter to pass in the implementation struct. We can
        even store an instance of that struct in a static field if we want.

        public class SortedList<T, Impl> where Impl : struct, SComparable<T, T>
        {
            static Impl impl = new Impl();

            List<T> ts = new List<T>();
            
            public void Add(T t)
            {
                int l = 0, r = ts.Count;
                while (l < r)
                {
                    int m = (l + r) / 2;
                    if (impl.CompareTo(t, ts[m]) < 0) { r = m; }
                    else { l = m + 1; }
                }
            }
        }

        One problem here is that the Impl type argument becomes part of the
        type identity of the constructed SortedList type. So if SortedList<T>
        is constructed with the same explicit type argument in two different
        places that implement SComparable<T> with different extensions, those
        are different constructed SortedList<T> types! The shape
        implementation becomes part of the type identity, and if it differs,
        those types are not interchangeable.
        
        Also, generic types can be overloaded on arity, so introducing secret
        extra type parameters can potentially throw a wrench into families of
        generic types all differing only on arity.

        The type classes proposal linked above actually makes the "implicit"
        type parameters explicit. This comes with its own problems, but does
        have the advantage that the number of type parameters shown in source
        code corresponds to the number in IL.

        # Extensions on shapes

        Using an approach similar to the shape-parametized types above, we
        can let extensions extend shapes, not just types. Let's say we want
        to write an extension that offers the trivial implementation of all
        the comparison operators on everything that implements
        SComparable<T>:
        */

        /* This isn't directly possible in Concept-C#.             //
           Instead, you'd need to make a separate concept: */      //
        public concept CComparison<T>                              //
        {                                                          //
            bool operator ==(T t1, T t2);                          //
            bool operator !=(T t1, T t2);                          //
            bool operator > (T t1, T t2);                          //
            bool operator >=(T t1, T t2);                          //
            bool operator < (T t1, T t2);                          //
            bool operator <=(T t1, T t2);                          //
        }                                                          //
                                                                   //
        public instance Comparison<T, implicit W> : CComparison<T> // public extension Comparison<T> of SComparable<T>
            where W : CComparable<T, T>                            //
        {                                                          // {
            bool operator ==(T t1, T t2) => t1.CompareTo(t2) == 0; //     public bool operator ==(T t1, T t2) => t1.CompareTo(t2) == 0;
            bool operator !=(T t1, T t2) => t1.CompareTo(t2) != 0; //     public bool operator !=(T t1, T t2) => t1.CompareTo(t2) != 0;
            bool operator > (T t1, T t2) => t1.CompareTo(t2) >  0; //     public bool operator > (T t1, T t2) => t1.CompareTo(t2) >  0;
            bool operator >=(T t1, T t2) => t1.CompareTo(t2) >= 0; //     public bool operator >=(T t1, T t2) => t1.CompareTo(t2) >= 0;
            bool operator < (T t1, T t2) => t1.CompareTo(t2) <  0; //     public bool operator < (T t1, T t2) => t1.CompareTo(t2) <  0;
            bool operator <=(T t1, T t2) => t1.CompareTo(t2) <= 0; //     public bool operator <=(T t1, T t2) => t1.CompareTo(t2) <= 0;
        }                                                          // }

        /*
        Just like the generic methods and types explored above, the
        underlying struct for this extension needs to have an extra type
        parameter for the implementation of the SComparable<T, T> interface:

        public struct Comparison<T, Impl> where Impl : struct, SComparable<T, T>
        {
            static Impl impl = new Impl();

            public bool op_Equality(T t1, T t2) => impl.CompareTo(t1, t2) == 0;
            public bool op_Inequality(T t1, T t2) => impl.CompareTo(t1, t2) != 0;
            public bool op_GreaterThan(T t1, T t2) => impl.CompareTo(t1, t2) > 0;
            public bool op_GreaterThanOrEqual(T t1, T t2) => impl.CompareTo(t1, t2) >= 0;
            public bool op_LessThan(T t1, T t2) => impl.CompareTo(t1, t2) < 0;
            public bool op_LessThanOrEqual(T t1, T t2) => impl.CompareTo(t1, t2) <= 0;
        }

        If that extension is in scope at the declaration of the Max method above, the comparison operators can now be used directly:
        */

        /* Again, not directly possible in Concept-C#.                       //
           We have to call into the other concept: */                        //
        public static T Max2<T, implicit W>(T[] ts) where W : CComparison<T> // public static T Max<T>(T[] ts) where T : SComparable<T>
        {                                                                    // {
            var result = ts[0];                                              //     var result = ts[0];
            foreach (var t in ts) { if (result < t) result = t; }            //     foreach (var t in ts) { if (result < t) result = t; }
            return result;                                                   //     return result;
        }                                                                    // }

        /*
        This gets straightforwardly implemented by passing the method's Impl
        type parameter (implementing SComparable) to the Comparison struct
        above, instantiating that, and calling its operator implementations:

        public static T Max<T, Impl>(T[] ts) where Impl : struct, SComparable<T, T>
        {
            var impl = new Comparison<T, Impl>();

            var result = ts[0];
            foreach (var t in ts) { if (impl.op_LessThan(result, t)) result = t; }
            return result;
        }

        # Explicit implementation and disambiguation

        This section is a potentially useful tangent, that one can choose to
        go down only a certain part of the way.
        
        We can consider explicit implementation, akin to what interfaces
        have, where the shape's members don't show up on the extended types
        themselves, but only when accessed through the shape directly. For
        instance, integers can also be viewed as a group under
        multiplication, but since that would mean implementing + as * and
        Zero as 1, we would not have those versions show up directly on the
        int type:
        */

        /********************************  // public extension IntMulGroup of int : SGroup<int>
         * No Concept-C# equivalent yet *  // {
         ********************************/ //     static int operator Sgroup<int>.+(int i1, int i2) => i1 * i2;
                                           //     static int SGroup<int>.Zero => 1;
                                           // }

        /*
        Thus, if both IntGroup and IntMulGroup were in scope, int.Zero would
        still yield 0, not 1.

        When passing an SGroup constrained type argument, however, we'd still
        want to be able to disambiguate whether we meant "int with addition"
        or "int with multiplication".

        # Specifying which shape or extension to use
        */

        /********************************************************
         * None of this section has a Concept-C# equivalent yet * 
         ********************************************************/

        /*
        When there is more than one declaration in scope providing a given
        member or shape implementation, the compiler cannot automatically
        infer which one to use. We may be able to give sensible resolution
        rules that deal with a lot of cases, but there's going to be
        situations where you want to specify which extension declaration you
        meant to use.
        
        AddAll(numbers); // use IntGroup or IntMulGroup?
        AddAll<int>(numbers); // Doesn't help, it's Impl that can't be inferred, not T

        An approach to this could be to simply allow the name of the
        extension declaration itself as a type name, with the rough meaning
        of "same type as the extended type, but give priority to this
        extension." It's sort of similar to base meaning "this type, but
        start member lookup in the base type":
        
        AddAll<IntMulGroup>(numbers); // becomes AddAll<int, IntMulGroup>(numbers)

        This would also work as an approach to get at explicitly implemented members:

        IntMulGroup.Zero; // 1;

        When accessing instance members on a receiver, to get at an
        explicitly implemented member, or to choose an extension to "view it
        as", cast the instance to the shape or extension name:

        ((SComparable<Point>)p1).CompareTo(p2); // Access an explicitly implemented but unambiguous member
        ((PointComparable))p1).CompareTo(p2);   // Access an ambiguous member by naming the declaring extension

        Or maybe it looks better with and as expression:

        (p1 as SComparable<Point>).CompareTo(p2);
        (p1 as PointComparable).CompareTo(p2);

        # Using extensions as types
        */

        /********************************************************
        /* None of this section has a Concept-C# equivalent yet */
        /********************************************************/

        /*
        The number of places where you can use shapes as types is very
        limited: we've only seen them as constraints and in disambiguating uses. That
        is because they do not correspond to a single underlying type.

        Extensions however, really do correspond to a single underlying type:
        the one that they extend. We could therefore imagine allowing them to
        be used as types of fields, parameters, etc. They would then denote,
        at runtime, the underlying type, but the compiler would know to "view
        it as" the extension.
        
        Let's again imagine that PointComparable explicitly implements
        SComparable<Point> on the type Point. But now I want to write code
        that compares Points all the time, and I don't want to have to cast
        every single time. Instead, can I just declare that I want to view
        these particular ints as PointComparable's?:

        PointComparable[] ps = GetPoints();
        ...
        ps[i].CompareTo(ps[j]);

        This translates into:

        var impl = new PointComparable();

        Point[] ps = GetPoints();
        ...
        impl.CompareTo(ps[i], ps[j]);

        For public interfaces we would have a way to signal the "overlay"
        extension type in metadata, e.g. through an attribute.

        # Extensions as wrapper types
        */

        /********************************************************
        /* None of this section has a Concept-C# equivalent yet */
        /********************************************************/

        /*
        One potentially useful further step to this, is to allow extensions
        to explicitly implement their own members, not just ones from shapes.
        What it would mean is, they don't actually expose the member on the
        underlying extended type, but only when the extension itself is used
        as the type.  This can be used to create compile time "wrapper
        types", that compile down to using the underlying type at runtime,
        but give it an extra face at compile time:

        public extension JPoint of JObject
        {
                public int JPoint.X => (int)this["X"];
                public int JPoint.Y => (int)this["Y"];
        }

        JObject o = GetObject[];
        WriteLine(o.X); // Error: X is not exposed on JObject, because it is explicitly implemented
        JPoint p = o;
        WriteLine(p.X); // Now the JObject is seen as a JPoint, so X is there

        This is an example of giving a typed overlay to something less typed.
        That appears to be a common scenario, and is the whole basis for e.g.
        TypeScript's type system. Whether or not this is the right mechanism
        for it is probably debatable, but it is certainly a mechanism.

        # Discussion

        This is a very high level proposal - it is more than a proof on
        concept that a design exists, and many details would need to be
        locked down (and changed) if we want to pursue this, e.g.:

        - How is an extension brought into scope? Does it need to be usinged,
          or is it in effect just through its presence?
        - How exactly are instance extension members encoded, so that they
          can have the extra @this parameter?
        - Which rules should be used to pick which extension members are more
          specific, so that there aren't ambiguities all the time?
        - Etc...

        Some issues with the proposal as it currently stands:

        - Two new "type declaration" forms to the language make it heavy on
          "concept".
        - Shapes and extensions are only "halfway" types, which may be a
          confusing notion to wrap your head around.
        - Hidden type parameters introduce a split between source and IL
          level generics, that may be ugly to pave over

        Other directions one might explore to achieve some of the same goals:

        - Find a way to extend interfaces to play the role of shapes here:
          declare static members, apply after the fact, etc. This would
          likely require runtime changes, but maybe that's better all-up.
        - Something more dynamic: structural typing, duck typing, whatever
          the term. This has the potential to fail at runtime, if something
          "turns out" not to fit the shape you assumed at compile time, and
          also doesn't clearly address some of the more generic scenarios.

        Looking forward to further discussion of the pros and cons!
        Mads
        */

        // Some testing code follows

        // This code is derived from
        // https://msdn.microsoft.com/en-us/library/4d7sx9hd(v=vs.110).aspx

        public class Temperature : IComparable<Temperature>
        {
            // Implement the generic CompareTo method with the Temperature 
            // class as the Type parameter. 
            //
            public int CompareTo(Temperature other)
            {
                // If other is not a valid object reference, this instance is greater.
                if (other == null) return 1;

                // The temperature comparison depends on the comparison of 
                // the underlying Double values. 
                return m_value.CompareTo(other.m_value);
            }

            // The underlying temperature value.
            protected double m_value = 0.0;

            public double Celsius
            {
                get
                {
                    return m_value - 273.15;
                }
            }

            public double Kelvin
            {
                get
                {
                    return m_value;
                }
                set
                {
                    if (value < 0.0)
                    {
                        throw new ArgumentException("Temperature cannot be less than absolute zero.");
                    }
                    else
                    {
                        m_value = value;
                    }
                }
            }

            public Temperature(double kelvins)
            {
                Kelvin = kelvins;
            }
        }

        static bool Gt<A, B, implicit W>(A x, B y) where W : CComparable<A, B>
            => W.CompareTo(x, y) > 0;

        static void Main(string[] args)
        {
            InlineTest1();
            int[] numbers = { 5, 1, 9, 2, 3, 10, 8, 4, 7, 6 };
            WriteLine(Max(numbers));
            WriteLine(Max2(numbers));

            var s = new SortedList<int>();
            s.Add(27);
            s.Add(1989);
            s.Add(53);
            s.Add(413);
            s.Add(1);
            s.Add(6);
            s.Add(95);

            // Concept-C# has a few issues with variance and subtyping at the
            // moment.
            //
            // We can't do
            //   new Temperature(2017.15) > new Temperature(0)
            // because > requires both sides to be the same type, and the
            // only instance for comparison between temperatures compares
            // IComparable<Temperature> to Temperature.
            //
            // The fact that Temperature is, itself, an
            // IComparable<Temperature> doesn't seem to gel properly yet.
            IComparable<Temperature> ict = new Temperature(2017.15);
            WriteLine(Gt(ict, new Temperature(0)));
        }
    }
}
