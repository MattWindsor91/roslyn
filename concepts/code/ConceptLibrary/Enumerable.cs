using System.Concepts;

namespace System.Concepts.Enumerable
{
    /// <summary>
    ///     Concept for types which may be enumerated.
    /// </summary>
    /// <typeparam name="C">
    ///     The type to be enumerated.
    /// </typeparam>
    /// <typeparam name="E">
    ///     The element returned by the enumerator.
    /// </typeparam>
    /// <typeparam name="S">
    ///     The state held by the enumerator.
    /// </typeparam>
    public concept CEnumerator<E, [AssociatedType] S>
    {
        void Reset(ref S enumerator);
        bool MoveNext(ref S enumerator);
        E Current(ref S enumerator);
        void Dispose(ref S enumerator);
    }

    /// <summary>
    ///     Concept for types which may be enumerated.
    /// </summary>
    /// <typeparam name="C">
    ///     The type to be enumerated.
    /// </typeparam>
    /// <typeparam name="E">
    ///     The element returned by the enumerator.
    /// </typeparam>
    /// <typeparam name="S">
    ///     The state held by the enumerator.
    /// </typeparam>
    public concept CEnumerable<C, [AssociatedType] E, [AssociatedType] S> : CEnumerator<E, S>
    {
        S GetEnumerator(C container);
    }
}
