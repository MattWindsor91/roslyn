using System.Concepts.Enumerable;

namespace TinyLinq
{
    /// <summary>
    /// Concept for enumerators whose length is known without enumerating.
    /// </summary>
    /// <typeparam name="TEnum">
    /// The type of the enumerator.
    /// </typeparam>
    public concept CBounded<TEnum>
    {
        /// <summary>
        /// Gets the length of the enumerator.
        /// </summary>
        /// <param name="t">
        /// The enumerator to query.
        /// </param>
        /// <returns>
        /// The length of the enumerator (without enumerating it).
        /// </returns>
        int Bound(ref TEnum t);
    }

    /// <summary>
    /// Instance for O(1) length lookup of array cursors.
    /// </summary>
    /// <typeparam name="TElem">
    /// Type of elements in the array.
    /// </typeparam>
    public instance CBounded_ArrayCursor<TElem> : CBounded<Instances.ArrayCursor<TElem>>
    {
        int Bound(ref Instances.ArrayCursor<TElem> t) => t.hi;
    }

    /// <summary>
    /// Instance for O(1) length lookup of selections, when the selected-over
    /// collection is itself bounded.
    /// </summary>
    /// <typeparam name="TEnum">
    /// Type of the source of the selection.
    /// </typeparam>
    /// <typeparam name="TElem">
    /// Type of the elements of <typeparamref name="TEnum"/>.
    /// </typeparam>
    /// <typeparam name="TProj">
    /// Type of the projected elements of the selection.
    /// </typeparam>
    /// <typeparam name="B">
    /// Instance of <see cref="CBounded{T}"/> for <typeparamref name="TEnum"/>.
    /// </typeparam>
    public instance CBounded_Select<TEnum, TElem, TProj, implicit B> : CBounded<Select<TEnum, TElem, TProj>>
        where B : CBounded<TEnum>
    {
        int Bound(ref Select<TEnum, TElem, TProj> sel) => B.Bound(ref sel.source);
    }
}
