namespace FunnySharp;

/// <summary>
/// Binds object fields to one known parent type so accessor lambdas infer naturally.
/// </summary>
/// <typeparam name="TParent">The object type that owns the fields.</typeparam>
/// <remarks>
/// Create a set with <see cref="Contract.For{TParent}"/>. Each method delegates to the matching
/// static <see cref="Contract"/> factory while fixing the parent type, so an accessor such as
/// <c>order =&gt; order.Id</c> needs no explicit parameter type. The set is stateless and holds
/// no field values.
/// </remarks>
public sealed class ContractFieldSet<TParent>
{
    internal ContractFieldSet()
    {
    }

    /// <summary>Binds a required object field.</summary>
    /// <typeparam name="T">The member value type.</typeparam>
    /// <param name="name">The non-empty case-sensitive property name.</param>
    /// <param name="contract">The non-null member contract.</param>
    /// <param name="getter">The accessor used to validate, shrink, and describe an existing object.</param>
    /// <returns>The field binding.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    public ContractField<TParent, T> Field<T>(
        string name,
        Contract<T> contract,
        Func<TParent, T> getter) =>
        Contract.Field(name, contract, getter);

    /// <summary>Binds an optional field where a missing property means absence.</summary>
    /// <typeparam name="T">The present-value type.</typeparam>
    /// <param name="name">The non-empty case-sensitive property name.</param>
    /// <param name="contract">The non-null contract for a present value.</param>
    /// <param name="getter">The accessor used to validate, shrink, and describe an existing object.</param>
    /// <returns>The field binding.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    public ContractField<TParent, Option<T>> OptionalField<T>(
        string name,
        Contract<T> contract,
        Func<TParent, Option<T>> getter) =>
        Contract.OptionalField(name, contract, getter);

    /// <summary>Binds an optional field where missing and explicit null both mean absence.</summary>
    /// <typeparam name="T">The present-value type.</typeparam>
    /// <param name="name">The non-empty case-sensitive property name.</param>
    /// <param name="contract">The non-null contract for a present non-null value.</param>
    /// <param name="getter">The accessor used to validate, shrink, and describe an existing object.</param>
    /// <returns>The field binding.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    public ContractField<TParent, Option<T>> NullableField<T>(
        string name,
        Contract<T> contract,
        Func<TParent, Option<T>> getter) =>
        Contract.NullableField(name, contract, getter);
}
