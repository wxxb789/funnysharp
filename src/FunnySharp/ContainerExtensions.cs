namespace FunnySharp;

/// <summary>
/// Provides option bridges for common container Try-pattern operations.
/// </summary>
/// <remarks>
/// The members use the <c>OrNone</c> cardinality suffix: <c>None</c> translates the empty, not-found,
/// and absent-key failure modes of the underlying containers instead of throwing or returning
/// sentinel values. Keyed lookups on read-only dictionaries keep
/// <see cref="OptionExtensions.GetOption{TKey,TValue}(IReadOnlyDictionary{TKey,TValue}, TKey)"/>.
/// </remarks>
public static class ContainerExtensions
{
    /// <summary>
    /// Dequeues the element at the front of the queue and converts it to an option.
    /// </summary>
    /// <typeparam name="T">The element type of the queue.</typeparam>
    /// <param name="source">The queue to dequeue from.</param>
    /// <returns>
    /// An option containing the removed front element, or <c>None</c> when the queue is empty.
    /// </returns>
    /// <remarks>
    /// This bridge translates the empty-queue failure of <see cref="Queue{T}.Dequeue"/>, which throws
    /// <see cref="InvalidOperationException"/>, to <c>None</c>. <c>None</c> can only mean that the
    /// queue was empty; the queue is mutated only when the returned option is present.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static Option<T> DequeueOrNone<T>(this Queue<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.TryDequeue(out var element) ? Option<T>.Some(element!) : Option<T>.None;
    }

    /// <summary>
    /// Peeks at the element at the front of the queue without removing it and converts it to an
    /// option.
    /// </summary>
    /// <typeparam name="T">The element type of the queue.</typeparam>
    /// <param name="source">The queue to peek at.</param>
    /// <returns>
    /// An option containing the front element, or <c>None</c> when the queue is empty.
    /// </returns>
    /// <remarks>
    /// This bridge translates the empty-queue failure of <see cref="Queue{T}.Peek"/>, which throws
    /// <see cref="InvalidOperationException"/>, to <c>None</c>. <c>None</c> can only mean that the
    /// queue was empty; the queue is never mutated.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static Option<T> PeekOrNone<T>(this Queue<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.TryPeek(out var element) ? Option<T>.Some(element!) : Option<T>.None;
    }

    /// <summary>
    /// Pops the element at the top of the stack and converts it to an option.
    /// </summary>
    /// <typeparam name="T">The element type of the stack.</typeparam>
    /// <param name="source">The stack to pop from.</param>
    /// <returns>
    /// An option containing the removed top element, or <c>None</c> when the stack is empty.
    /// </returns>
    /// <remarks>
    /// This bridge translates the empty-stack failure of <see cref="Stack{T}.Pop"/>, which throws
    /// <see cref="InvalidOperationException"/>, to <c>None</c>. <c>None</c> can only mean that the
    /// stack was empty; the stack is mutated only when the returned option is present.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static Option<T> PopOrNone<T>(this Stack<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.TryPop(out var element) ? Option<T>.Some(element!) : Option<T>.None;
    }

    /// <summary>
    /// Peeks at the element at the top of the stack without removing it and converts it to an
    /// option.
    /// </summary>
    /// <typeparam name="T">The element type of the stack.</typeparam>
    /// <param name="source">The stack to peek at.</param>
    /// <returns>
    /// An option containing the top element, or <c>None</c> when the stack is empty.
    /// </returns>
    /// <remarks>
    /// This bridge translates the empty-stack failure of <see cref="Stack{T}.Peek"/>, which throws
    /// <see cref="InvalidOperationException"/>, to <c>None</c>. <c>None</c> can only mean that the
    /// stack was empty; the stack is never mutated.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static Option<T> PeekOrNone<T>(this Stack<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.TryPeek(out var element) ? Option<T>.Some(element!) : Option<T>.None;
    }

    /// <summary>
    /// Dequeues the minimal element of the priority queue and converts it to an option.
    /// </summary>
    /// <typeparam name="TElement">The element type of the priority queue.</typeparam>
    /// <typeparam name="TPriority">The priority type of the priority queue.</typeparam>
    /// <param name="source">The priority queue to dequeue from.</param>
    /// <returns>
    /// An option containing the removed minimal element, or <c>None</c> when the priority queue is
    /// empty.
    /// </returns>
    /// <remarks>
    /// This bridge translates the empty-queue failure of
    /// <see cref="PriorityQueue{TElement,TPriority}.Dequeue"/>, which throws
    /// <see cref="InvalidOperationException"/>, to <c>None</c>. <c>None</c> can only mean that the
    /// priority queue was empty; the priority queue is mutated only when the returned option is
    /// present.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static Option<TElement> DequeueOrNone<TElement, TPriority>(this PriorityQueue<TElement, TPriority> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.TryDequeue(out var element, out _) ? Option<TElement>.Some(element!) : Option<TElement>.None;
    }

    /// <summary>
    /// Peeks at the minimal element of the priority queue without removing it and converts it to an
    /// option.
    /// </summary>
    /// <typeparam name="TElement">The element type of the priority queue.</typeparam>
    /// <typeparam name="TPriority">The priority type of the priority queue.</typeparam>
    /// <param name="source">The priority queue to peek at.</param>
    /// <returns>
    /// An option containing the minimal element, or <c>None</c> when the priority queue is empty.
    /// </returns>
    /// <remarks>
    /// This bridge translates the empty-queue failure of
    /// <see cref="PriorityQueue{TElement,TPriority}.Peek"/>, which throws
    /// <see cref="InvalidOperationException"/>, to <c>None</c>. <c>None</c> can only mean that the
    /// priority queue was empty; the priority queue is never mutated.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static Option<TElement> PeekOrNone<TElement, TPriority>(this PriorityQueue<TElement, TPriority> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.TryPeek(out var element, out _) ? Option<TElement>.Some(element!) : Option<TElement>.None;
    }

    /// <summary>
    /// Searches for an item in the list and converts the found index to an option.
    /// </summary>
    /// <typeparam name="T">The item type of the list.</typeparam>
    /// <param name="source">The list to search.</param>
    /// <param name="item">The item to locate, compared with the list's own comparison semantics.</param>
    /// <returns>
    /// An option containing the zero-based index of the first occurrence of <paramref name="item"/>,
    /// or <c>None</c> when the item is not found.
    /// </returns>
    /// <remarks>
    /// This bridge translates the <c>-1</c> sentinel of <see cref="IList{T}.IndexOf"/> to
    /// <c>None</c>, which can only mean that the item was not found. The lookup uses the list's own
    /// comparison semantics, so it applies to <see cref="List{T}"/> and to
    /// <c>ImmutableList&lt;T&gt;</c>, which implements <see cref="IList{T}"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static Option<int> IndexOfOrNone<T>(this IList<T> source, T item)
    {
        ArgumentNullException.ThrowIfNull(source);

        var index = source.IndexOf(item);
        return index >= 0 ? Option<int>.Some(index) : Option<int>.None;
    }

    /// <summary>
    /// Removes a key from the dictionary and converts the removed value to an option.
    /// </summary>
    /// <typeparam name="TKey">The key type of the dictionary.</typeparam>
    /// <typeparam name="TValue">The value type of the dictionary.</typeparam>
    /// <param name="source">The dictionary to remove from.</param>
    /// <param name="key">The key to remove.</param>
    /// <returns>
    /// An option containing the removed value, or <c>None</c> when the key is absent.
    /// </returns>
    /// <remarks>
    /// This bridge uses <c>Dictionary.Remove(TKey, out TValue)</c>, so removal and value retrieval
    /// are a single operation. <c>None</c> can only mean that the key was absent; the dictionary is
    /// mutated only when the returned option is present.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static Option<TValue> RemoveOrNone<TKey, TValue>(this Dictionary<TKey, TValue> source, TKey key)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Remove(key, out var value) ? Option<TValue>.Some(value!) : Option<TValue>.None;
    }
}
