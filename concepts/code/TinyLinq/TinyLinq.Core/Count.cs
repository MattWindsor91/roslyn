using System.Concepts.Countable;

namespace TinyLinq
{
    // Count is already implemented in the concept library.
    // This just adds new instances to CCountable and CStaticCountable
    // for LINQ stuff.

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
    /// Instance of <see cref="CStaticCount{T}"/> for <typeparamref name="TEnum"/>.
    /// </typeparam>
    public instance StaticCountable_Select<TEnum, TElem, TProj, implicit S> : CStaticCountable<Select<TEnum, TElem, TProj>>
        where S : CStaticCountable<TEnum>
    {
        int Count(this Select<TEnum, TElem, TProj> sel) => S.Count(sel.source);
    }
}
