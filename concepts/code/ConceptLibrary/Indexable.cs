namespace System.Concepts.Indexable
{
    // Rust-style indexable concept.

    /// <summary>
    /// Concept for types that can be indexed.
    /// </summary>
    /// <typeparam name="TColl">The type to index.</typeparam>
    /// <typeparam name="TIdx">The type of the index.</typeparam>
    /// <typeparam name="TElem">The type of elements returned.</typeparam>
    public concept CIndexable<TColl, TIdx, [AssociatedType] TElem>
    {
        // TODO(MattWindsor91): indexer operators
       
        /// <summary>
        /// Returns the item at the given index in this collection.
        /// </summary>
        /// <param name="c">The collection to index.</param>
        /// <param name="i">The index being accessed.</param>
        /// <returns>
        /// The element at the given index.
        /// </returns>
        TElem At(this TColl c, TIdx i);
    }

    /// <summary>
    /// Indexable instance for arrays.
    /// </summary>
    /// <typeparam name="TElem">The type of elements in the array.</typeparam>
    public instance Indexable_Array<TElem> : CIndexable<TElem[], int, TElem>
    {
        TElem At(this TElem[] c, int i) => c[i];
    }
}
