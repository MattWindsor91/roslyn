namespace TCOIExamples
{
    /// <summary>
    /// Definition of Ord in d. S. Oliveira et al.
    /// </summary>
    public concept Ord<T>
    {
        bool Compare(T a, T b);
    }

    // Scala:
    //   trait Ord[T] {
    //     def compare (a : T, b : T) : Boolean
    //   }
}