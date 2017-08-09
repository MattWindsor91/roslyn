/// <summary>
/// Attributes for the concepts system.
/// </summary>
namespace System.Concepts
{
    /// <summary>
    /// Attribute marking interfaces as concepts.
    /// <para>
    /// Syntactic concepts are reduced to interfaces with this attribute in the
    /// emitted code.  Also, interfaces with this attribute are treated as
    /// concepts by the compiler.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
    public class ConceptAttribute : Attribute { }

    /// <summary>
    /// Attribute marking structs as concept instances.
    /// <para>
    /// Syntactic instances are reduced to structs with this attribute in the
    /// emitted code.  Also, structs with this attribute are treated as concept
    /// instances by the compiler.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public class ConceptInstanceAttribute : Attribute { }


    /// <summary>
    /// Attribute marking structs as concept default structs.
    /// <para>
    /// Syntactic defaults are reduced to structs with this attribute in the
    /// emitted code.  Also, structs with this attribute are treated as concept
    /// default structs by the compiler.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public class ConceptDefaultAttribute : Attribute { }

    /// <summary>
    /// Attribute marking type parameters as associated types.
    /// <para>
    /// Generated associated types are given this attribute in the emitted code.
    /// Also, type parameters with this attribute are treated as associated
    /// types by the compiler.
    /// </para>
    /// <para>
    /// Associated type parameters are handled specially by the type
    /// inferrer: if they are unfixed, the concept inferrer will try to
    /// back-propagate a definition for them using results from inferring
    /// concept witnesses.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.GenericParameter, AllowMultiple = false, Inherited = true)]
    public class AssociatedTypeAttribute : Attribute { }
}