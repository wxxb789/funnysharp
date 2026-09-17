# Public API inventory: bcl-collections-immutable

Assemblies: System.Collections.Immutable 10.0.0.0, System.Collections 10.0.0.0, System.Collections.Concurrent 10.0.0.0, System.Linq 10.0.0.0

Type count: 55

## System.Collections.Concurrent

### AlternateLookup`1<TKey, TValue, TAlternateKey> (struct [readonly struct])

- `public System.Collections.Concurrent.ConcurrentDictionary<TKey, TValue> Dictionary { get; }`
- `public TValue Item(TAlternateKey key) { get; set; }`
- `public System.Boolean ContainsKey(TAlternateKey key)`
- `public System.Boolean TryAdd(TAlternateKey key, TValue value)`
- `public System.Boolean TryGetValue(TAlternateKey key, out TValue& value)`
- `public System.Boolean TryGetValue(TAlternateKey key, out TKey& actualKey, out TValue& value)`
- `public System.Boolean TryRemove(TAlternateKey key, out TValue& value)`
- `public System.Boolean TryRemove(TAlternateKey key, out TKey& actualKey, out TValue& value)`

### BlockingCollection`1<T> (class) : System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.ICollection, System.IDisposable

- `public BlockingCollection`1()`
- `public BlockingCollection`1(System.Collections.Concurrent.IProducerConsumerCollection<T> collection)`
- `public BlockingCollection`1(System.Int32 boundedCapacity)`
- `public BlockingCollection`1(System.Collections.Concurrent.IProducerConsumerCollection<T> collection, System.Int32 boundedCapacity)`
- `public System.Int32 BoundedCapacity { get; }`
- `public System.Int32 Count { get; }`
- `public System.Boolean IsAddingCompleted { get; }`
- `public System.Boolean IsCompleted { get; }`
- `public System.Void Add(T item)`
- `public System.Void Add(T item, System.Threading.CancellationToken cancellationToken)`
- `public static System.Int32 AddToAny(System.Collections.Concurrent.BlockingCollection<T>[] collections, T item)`
- `public static System.Int32 AddToAny(System.Collections.Concurrent.BlockingCollection<T>[] collections, T item, System.Threading.CancellationToken cancellationToken)`
- `public System.Void CompleteAdding()`
- `public System.Void CopyTo(T[] array, System.Int32 index)`
- `public System.Void Dispose()`
- `public System.Collections.Generic.IEnumerable<T> GetConsumingEnumerable()`
- `public System.Collections.Generic.IEnumerable<T> GetConsumingEnumerable(System.Threading.CancellationToken cancellationToken)`
- `public T Take()`
- `public T Take(System.Threading.CancellationToken cancellationToken)`
- `public static System.Int32 TakeFromAny(System.Collections.Concurrent.BlockingCollection<T>[] collections, out T& item)`
- `public static System.Int32 TakeFromAny(System.Collections.Concurrent.BlockingCollection<T>[] collections, out T& item, System.Threading.CancellationToken cancellationToken)`
- `public T[] ToArray()`
- `public System.Boolean TryAdd(T item)`
- `public System.Boolean TryAdd(T item, System.Int32 millisecondsTimeout)`
- `public System.Boolean TryAdd(T item, System.TimeSpan timeout)`
- `public System.Boolean TryAdd(T item, System.Int32 millisecondsTimeout, System.Threading.CancellationToken cancellationToken)`
- `public static System.Int32 TryAddToAny(System.Collections.Concurrent.BlockingCollection<T>[] collections, T item)`
- `public static System.Int32 TryAddToAny(System.Collections.Concurrent.BlockingCollection<T>[] collections, T item, System.Int32 millisecondsTimeout)`
- `public static System.Int32 TryAddToAny(System.Collections.Concurrent.BlockingCollection<T>[] collections, T item, System.TimeSpan timeout)`
- `public static System.Int32 TryAddToAny(System.Collections.Concurrent.BlockingCollection<T>[] collections, T item, System.Int32 millisecondsTimeout, System.Threading.CancellationToken cancellationToken)`
- `public System.Boolean TryTake(out T& item)`
- `public System.Boolean TryTake(out T& item, System.Int32 millisecondsTimeout)`
- `public System.Boolean TryTake(out T& item, System.TimeSpan timeout)`
- `public System.Boolean TryTake(out T& item, System.Int32 millisecondsTimeout, System.Threading.CancellationToken cancellationToken)`
- `public static System.Int32 TryTakeFromAny(System.Collections.Concurrent.BlockingCollection<T>[] collections, out T& item)`
- `public static System.Int32 TryTakeFromAny(System.Collections.Concurrent.BlockingCollection<T>[] collections, out T& item, System.Int32 millisecondsTimeout)`
- `public static System.Int32 TryTakeFromAny(System.Collections.Concurrent.BlockingCollection<T>[] collections, out T& item, System.TimeSpan timeout)`
- `public static System.Int32 TryTakeFromAny(System.Collections.Concurrent.BlockingCollection<T>[] collections, out T& item, System.Int32 millisecondsTimeout, System.Threading.CancellationToken cancellationToken)`

### ConcurrentBag`1<T> (class) : System.Collections.Concurrent.IProducerConsumerCollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable, System.Collections.ICollection, System.Collections.Generic.IReadOnlyCollection<T>

- `public ConcurrentBag`1()`
- `public ConcurrentBag`1(System.Collections.Generic.IEnumerable<T> collection)`
- `public System.Int32 Count { get; }`
- `public System.Boolean IsEmpty { get; }`
- `public System.Void Add(T item)`
- `public System.Void Clear()`
- `public System.Void CopyTo(T[] array, System.Int32 index)`
- `public System.Collections.Generic.IEnumerator<T> GetEnumerator()`
- `public T[] ToArray()`
- `public System.Boolean TryPeek(out T& result)`
- `public System.Boolean TryTake(out T& result)`

### ConcurrentDictionary`2<TKey, TValue> (class) : System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.IEnumerable, System.Collections.Generic.IDictionary<TKey, TValue>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>, System.Collections.ICollection, System.Collections.IDictionary

- `public ConcurrentDictionary`2()`
- `public ConcurrentDictionary`2(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> collection)`
- `public ConcurrentDictionary`2(System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `public ConcurrentDictionary`2(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> collection, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `public ConcurrentDictionary`2(System.Int32 concurrencyLevel, System.Int32 capacity)`
- `public ConcurrentDictionary`2(System.Int32 concurrencyLevel, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> collection, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `public ConcurrentDictionary`2(System.Int32 concurrencyLevel, System.Int32 capacity, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `public System.Collections.Generic.IEqualityComparer<TKey> Comparer { get; }`
- `public System.Int32 Count { get; }`
- `public System.Boolean IsEmpty { get; }`
- `public TValue Item(TKey key) { get; set; }`
- `public System.Collections.Generic.ICollection<TKey> Keys { get; }`
- `public System.Collections.Generic.ICollection<TValue> Values { get; }`
- `public TValue AddOrUpdate(TKey key, System.Func<TKey, TValue> addValueFactory, System.Func<TKey, TValue, TValue> updateValueFactory)`
- `public TValue AddOrUpdate(TKey key, TValue addValue, System.Func<TKey, TValue, TValue> updateValueFactory)`
- `public TValue AddOrUpdate<TArg>(TKey key, System.Func<TKey, TArg, TValue> addValueFactory, System.Func<TKey, TValue, TArg, TValue> updateValueFactory, TArg factoryArgument)`
- `public System.Void Clear()`
- `public System.Boolean ContainsKey(TKey key)`
- `public System.Collections.Concurrent.ConcurrentDictionary<TKey, TValue, TAlternateKey> GetAlternateLookup<TAlternateKey>()`
- `public System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<TKey, TValue>> GetEnumerator()`
- `public TValue GetOrAdd(TKey key, System.Func<TKey, TValue> valueFactory)`
- `public TValue GetOrAdd(TKey key, TValue value)`
- `public TValue GetOrAdd<TArg>(TKey key, System.Func<TKey, TArg, TValue> valueFactory, TArg factoryArgument)`
- `public System.Collections.Generic.KeyValuePair<TKey, TValue>[] ToArray()`
- `public System.Boolean TryAdd(TKey key, TValue value)`
- `public System.Boolean TryGetAlternateLookup<TAlternateKey>(out System.Collections.Concurrent.ConcurrentDictionary<TKey, TValue, TAlternateKey>& lookup)`
- `public System.Boolean TryGetValue(TKey key, out TValue& value)`
- `public System.Boolean TryRemove(System.Collections.Generic.KeyValuePair<TKey, TValue> item)`
- `public System.Boolean TryRemove(TKey key, out TValue& value)`
- `public System.Boolean TryUpdate(TKey key, TValue newValue, TValue comparisonValue)`

### ConcurrentQueue`1<T> (class) : System.Collections.Concurrent.IProducerConsumerCollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable, System.Collections.ICollection, System.Collections.Generic.IReadOnlyCollection<T>

- `public ConcurrentQueue`1()`
- `public ConcurrentQueue`1(System.Collections.Generic.IEnumerable<T> collection)`
- `public System.Int32 Count { get; }`
- `public System.Boolean IsEmpty { get; }`
- `public System.Void Clear()`
- `public System.Void CopyTo(T[] array, System.Int32 index)`
- `public System.Void Enqueue(T item)`
- `public System.Collections.Generic.IEnumerator<T> GetEnumerator()`
- `public T[] ToArray()`
- `public System.Boolean TryDequeue(out T& result)`
- `public System.Boolean TryPeek(out T& result)`

### ConcurrentStack`1<T> (class) : System.Collections.Concurrent.IProducerConsumerCollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable, System.Collections.ICollection, System.Collections.Generic.IReadOnlyCollection<T>

- `public ConcurrentStack`1()`
- `public ConcurrentStack`1(System.Collections.Generic.IEnumerable<T> collection)`
- `public System.Int32 Count { get; }`
- `public System.Boolean IsEmpty { get; }`
- `public System.Void Clear()`
- `public System.Void CopyTo(T[] array, System.Int32 index)`
- `public System.Collections.Generic.IEnumerator<T> GetEnumerator()`
- `public System.Void Push(T item)`
- `public System.Void PushRange(T[] items)`
- `public System.Void PushRange(T[] items, System.Int32 startIndex, System.Int32 count)`
- `public T[] ToArray()`
- `public System.Boolean TryPeek(out T& result)`
- `public System.Boolean TryPop(out T& result)`
- `public System.Int32 TryPopRange(T[] items)`
- `public System.Int32 TryPopRange(T[] items, System.Int32 startIndex, System.Int32 count)`

### EnumerablePartitionerOptions (enum) : System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable

- `None = 0`
- `NoBuffering = 1`

### IProducerConsumerCollection`1<T> (interface) : System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable, System.Collections.ICollection

- `public System.Void CopyTo(T[] array, System.Int32 index)`
- `public T[] ToArray()`
- `public System.Boolean TryAdd(T item)`
- `public System.Boolean TryTake(out T& item)`

### OrderablePartitioner`1<TSource> (class [abstract]) : System.Collections.Concurrent.Partitioner<TSource>

- `public System.Boolean KeysNormalized { get; }`
- `public System.Boolean KeysOrderedAcrossPartitions { get; }`
- `public System.Boolean KeysOrderedInEachPartition { get; }`
- `public System.Collections.Generic.IEnumerable<TSource> GetDynamicPartitions()`
- `public System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.Int64, TSource>> GetOrderableDynamicPartitions()`
- `public System.Collections.Generic.IList<System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<System.Int64, TSource>>> GetOrderablePartitions(System.Int32 partitionCount)`
- `public System.Collections.Generic.IList<System.Collections.Generic.IEnumerator<TSource>> GetPartitions(System.Int32 partitionCount)`

### Partitioner (class [static])

- `public static System.Collections.Concurrent.OrderablePartitioner<TSource> Create<TSource>(System.Collections.Generic.IEnumerable<TSource> source)`
- `public static System.Collections.Concurrent.OrderablePartitioner<System.Tuple<System.Int32, System.Int32>> Create(System.Int32 fromInclusive, System.Int32 toExclusive)`
- `public static System.Collections.Concurrent.OrderablePartitioner<System.Tuple<System.Int64, System.Int64>> Create(System.Int64 fromInclusive, System.Int64 toExclusive)`
- `public static System.Collections.Concurrent.OrderablePartitioner<TSource> Create<TSource>(System.Collections.Generic.IEnumerable<TSource> source, System.Collections.Concurrent.EnumerablePartitionerOptions partitionerOptions)`
- `public static System.Collections.Concurrent.OrderablePartitioner<TSource> Create<TSource>(System.Collections.Generic.IList<TSource> list, System.Boolean loadBalance)`
- `public static System.Collections.Concurrent.OrderablePartitioner<TSource> Create<TSource>(TSource[] array, System.Boolean loadBalance)`
- `public static System.Collections.Concurrent.OrderablePartitioner<System.Tuple<System.Int32, System.Int32>> Create(System.Int32 fromInclusive, System.Int32 toExclusive, System.Int32 rangeSize)`
- `public static System.Collections.Concurrent.OrderablePartitioner<System.Tuple<System.Int64, System.Int64>> Create(System.Int64 fromInclusive, System.Int64 toExclusive, System.Int64 rangeSize)`

### Partitioner`1<TSource> (class [abstract])

- `public System.Boolean SupportsDynamicPartitions { get; }`
- `public System.Collections.Generic.IEnumerable<TSource> GetDynamicPartitions()`
- `public System.Collections.Generic.IList<System.Collections.Generic.IEnumerator<TSource>> GetPartitions(System.Int32 partitionCount)`

## System.Collections.Frozen

### AlternateLookup`1<TKey, TValue, TAlternateKey> (struct [readonly struct])

- `public System.Collections.Frozen.FrozenDictionary<TKey, TValue> Dictionary { get; }`
- `public TValue Item(TAlternateKey key) { get; }`
- `public System.Boolean ContainsKey(TAlternateKey key)`
- `public System.Boolean TryGetValue(TAlternateKey key, out TValue& value)`

### AlternateLookup`1<T, TAlternate> (struct [readonly struct])

- `public System.Collections.Frozen.FrozenSet<T> Set { get; }`
- `public System.Boolean Contains(TAlternate item)`
- `public System.Boolean TryGetValue(TAlternate equalValue, out T& actualValue)`

### Enumerator<TKey, TValue> (struct) : System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.IEnumerator, System.IDisposable

- `public System.Collections.Generic.KeyValuePair<TKey, TValue> Current { get; }`
- `public System.Boolean MoveNext()`

### Enumerator<T> (struct) : System.Collections.Generic.IEnumerator<T>, System.Collections.IEnumerator, System.IDisposable

- `public T Current { get; }`
- `public System.Boolean MoveNext()`

### FrozenDictionary (class [static])

- `public static System.Collections.Frozen.FrozenDictionary<TKey, TValue> Create<TKey, TValue>(System.ReadOnlySpan<System.Collections.Generic.KeyValuePair<TKey, TValue>> source)`
- `public static System.Collections.Frozen.FrozenDictionary<TKey, TValue> Create<TKey, TValue>(System.Collections.Generic.IEqualityComparer<TKey> comparer, System.ReadOnlySpan<System.Collections.Generic.KeyValuePair<TKey, TValue>> source)`
- `[ext] public static System.Collections.Frozen.FrozenDictionary<TKey, TValue> ToFrozenDictionary<TKey, TValue>(this System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> source, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Frozen.FrozenDictionary<TKey, TSource> ToFrozenDictionary<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Frozen.FrozenDictionary<TKey, TElement> ToFrozenDictionary<TSource, TKey, TElement>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`

### FrozenDictionary`2<TKey, TValue> (class [abstract]) : System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.IEnumerable, System.Collections.Generic.IDictionary<TKey, TValue>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>, System.Collections.ICollection, System.Collections.IDictionary

- `public System.Collections.Generic.IEqualityComparer<TKey> Comparer { get; }`
- `public System.Int32 Count { get; }`
- `public static System.Collections.Frozen.FrozenDictionary<TKey, TValue> Empty { get; }`
- `public TValue& Item(TKey key) { get; }`
- `public System.Collections.Immutable.ImmutableArray<TKey> Keys { get; }`
- `public System.Collections.Immutable.ImmutableArray<TValue> Values { get; }`
- `public System.Boolean ContainsKey(TKey key)`
- `public System.Void CopyTo(System.Span<System.Collections.Generic.KeyValuePair<TKey, TValue>> destination)`
- `public System.Void CopyTo(System.Collections.Generic.KeyValuePair<TKey, TValue>[] destination, System.Int32 destinationIndex)`
- `public System.Collections.Frozen.FrozenDictionary<TKey, TValue, TAlternateKey> GetAlternateLookup<TAlternateKey>()`
- `public System.Collections.Frozen.FrozenDictionary<TKey, TValue> GetEnumerator()`
- `public TValue& GetValueRefOrNullRef(TKey key)`
- `public System.Boolean TryGetAlternateLookup<TAlternateKey>(out System.Collections.Frozen.FrozenDictionary<TKey, TValue, TAlternateKey>& lookup)`
- `public System.Boolean TryGetValue(TKey key, out TValue& value)`

### FrozenSet (class [static])

- `public static System.Collections.Frozen.FrozenSet<T> Create<T>(System.ReadOnlySpan<T> source)`
- `public static System.Collections.Frozen.FrozenSet<T> Create<T>(System.Collections.Generic.IEqualityComparer<T> equalityComparer, System.ReadOnlySpan<T> source)`
- `[ext] public static System.Collections.Frozen.FrozenSet<T> ToFrozenSet<T>(this System.Collections.Generic.IEnumerable<T> source, System.Collections.Generic.IEqualityComparer<T> comparer)`

### FrozenSet`1<T> (class [abstract]) : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.ISet<T>, System.Collections.ICollection, System.Collections.Generic.IReadOnlySet<T>

- `public System.Collections.Generic.IEqualityComparer<T> Comparer { get; }`
- `public System.Int32 Count { get; }`
- `public static System.Collections.Frozen.FrozenSet<T> Empty { get; }`
- `public System.Collections.Immutable.ImmutableArray<T> Items { get; }`
- `public System.Boolean Contains(T item)`
- `public System.Void CopyTo(System.Span<T> destination)`
- `public System.Void CopyTo(T[] destination, System.Int32 destinationIndex)`
- `public System.Collections.Frozen.FrozenSet<T, TAlternate> GetAlternateLookup<TAlternate>()`
- `public System.Collections.Frozen.FrozenSet<T> GetEnumerator()`
- `public System.Boolean IsProperSubsetOf(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean IsProperSupersetOf(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean IsSubsetOf(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean IsSupersetOf(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean Overlaps(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean SetEquals(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean TryGetAlternateLookup<TAlternate>(out System.Collections.Frozen.FrozenSet<T, TAlternate>& lookup)`
- `public System.Boolean TryGetValue(T equalValue, out T& actualValue)`

## System.Collections.Immutable

### Builder<T> (class [sealed]) : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable, System.Collections.Generic.IList<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.IReadOnlyList<T>

- `public System.Int32 Capacity { get; set; }`
- `public System.Int32 Count { get; set; }`
- `public T Item(System.Int32 index) { get; set; }`
- `public System.Void Add(T item)`
- `public System.Void AddRange(System.Collections.Generic.IEnumerable<T> items)`
- `public System.Void AddRange(System.Collections.Immutable.ImmutableArray<T> items)`
- `public System.Void AddRange(System.Collections.Immutable.ImmutableArray<T> items)`
- `public System.Void AddRange(params T[] items)`
- `public System.Void AddRange<TDerived>(System.Collections.Immutable.ImmutableArray<TDerived> items)`
- `where TDerived : T`
- `public System.Void AddRange<TDerived>(System.Collections.Immutable.ImmutableArray<TDerived> items)`
- `where TDerived : T`
- `public System.Void AddRange<TDerived>(TDerived[] items)`
- `where TDerived : T`
- `public System.Void AddRange(System.ReadOnlySpan<T> items)`
- `public System.Void AddRange<TDerived>(System.ReadOnlySpan<TDerived> items)`
- `where TDerived : T`
- `public System.Void AddRange(System.Collections.Immutable.ImmutableArray<T> items, System.Int32 length)`
- `public System.Void AddRange(T[] items, System.Int32 length)`
- `public System.Void Clear()`
- `public System.Boolean Contains(T item)`
- `public System.Void CopyTo(T[] destination)`
- `public System.Void CopyTo(System.Span<T> destination)`
- `public System.Void CopyTo(T[] array, System.Int32 index)`
- `public System.Void CopyTo(System.Int32 sourceIndex, T[] destination, System.Int32 destinationIndex, System.Int32 length)`
- `public System.Collections.Immutable.ImmutableArray<T> DrainToImmutable()`
- `public System.Collections.Generic.IEnumerator<T> GetEnumerator()`
- `public System.Int32 IndexOf(T item)`
- `public System.Int32 IndexOf(T item, System.Int32 startIndex)`
- `public System.Int32 IndexOf(T item, System.Int32 startIndex, System.Int32 count)`
- `public System.Int32 IndexOf(T item, System.Int32 startIndex, System.Collections.Generic.IEqualityComparer<T> equalityComparer)`
- `public System.Int32 IndexOf(T item, System.Int32 startIndex, System.Int32 count, System.Collections.Generic.IEqualityComparer<T> equalityComparer)`
- `public System.Void Insert(System.Int32 index, T item)`
- `public System.Void InsertRange(System.Int32 index, System.Collections.Generic.IEnumerable<T> items)`
- `public System.Void InsertRange(System.Int32 index, System.Collections.Immutable.ImmutableArray<T> items)`
- `public T& ItemRef(System.Int32 index)`
- `public System.Int32 LastIndexOf(T item)`
- `public System.Int32 LastIndexOf(T item, System.Int32 startIndex)`
- `public System.Int32 LastIndexOf(T item, System.Int32 startIndex, System.Int32 count)`
- `public System.Int32 LastIndexOf(T item, System.Int32 startIndex, System.Int32 count, System.Collections.Generic.IEqualityComparer<T> equalityComparer)`
- `public System.Collections.Immutable.ImmutableArray<T> MoveToImmutable()`
- `public System.Boolean Remove(T element)`
- `public System.Boolean Remove(T element, System.Collections.Generic.IEqualityComparer<T> equalityComparer)`
- `public System.Void RemoveAll(System.Predicate<T> match)`
- `public System.Void RemoveAt(System.Int32 index)`
- `public System.Void RemoveRange(System.Collections.Generic.IEnumerable<T> items)`
- `public System.Void RemoveRange(System.Int32 index, System.Int32 length)`
- `public System.Void RemoveRange(System.Collections.Generic.IEnumerable<T> items, System.Collections.Generic.IEqualityComparer<T> equalityComparer)`
- `public System.Void Replace(T oldValue, T newValue)`
- `public System.Void Replace(T oldValue, T newValue, System.Collections.Generic.IEqualityComparer<T> equalityComparer)`
- `public System.Void Reverse()`
- `public System.Void Sort()`
- `public System.Void Sort(System.Collections.Generic.IComparer<T> comparer)`
- `public System.Void Sort(System.Comparison<T> comparison)`
- `public System.Void Sort(System.Int32 index, System.Int32 count, System.Collections.Generic.IComparer<T> comparer)`
- `public T[] ToArray()`
- `public System.Collections.Immutable.ImmutableArray<T> ToImmutable()`

### Builder<TKey, TValue> (class [sealed]) : System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.IEnumerable, System.Collections.Generic.IDictionary<TKey, TValue>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>, System.Collections.ICollection, System.Collections.IDictionary

- `public System.Int32 Count { get; }`
- `public TValue Item(TKey key) { get; set; }`
- `public System.Collections.Generic.IEqualityComparer<TKey> KeyComparer { get; set; }`
- `public System.Collections.Generic.IEnumerable<TKey> Keys { get; }`
- `public System.Collections.Generic.IEqualityComparer<TValue> ValueComparer { get; set; }`
- `public System.Collections.Generic.IEnumerable<TValue> Values { get; }`
- `public System.Void Add(System.Collections.Generic.KeyValuePair<TKey, TValue> item)`
- `public System.Void Add(TKey key, TValue value)`
- `public System.Void AddRange(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> items)`
- `public System.Void Clear()`
- `public System.Boolean Contains(System.Collections.Generic.KeyValuePair<TKey, TValue> item)`
- `public System.Boolean ContainsKey(TKey key)`
- `public System.Boolean ContainsValue(TValue value)`
- `public System.Collections.Immutable.ImmutableDictionary<TKey, TValue> GetEnumerator()`
- `public TValue GetValueOrDefault(TKey key)`
- `public TValue GetValueOrDefault(TKey key, TValue defaultValue)`
- `public System.Boolean Remove(System.Collections.Generic.KeyValuePair<TKey, TValue> item)`
- `public System.Boolean Remove(TKey key)`
- `public System.Void RemoveRange(System.Collections.Generic.IEnumerable<TKey> keys)`
- `public System.Collections.Immutable.ImmutableDictionary<TKey, TValue> ToImmutable()`
- `public System.Boolean TryGetKey(TKey equalKey, out TKey& actualKey)`
- `public System.Boolean TryGetValue(TKey key, out TValue& value)`

### Builder<T> (class [sealed]) : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.ISet<T>

- `public System.Int32 Count { get; }`
- `public System.Collections.Generic.IEqualityComparer<T> KeyComparer { get; set; }`
- `public System.Boolean Add(T item)`
- `public System.Void Clear()`
- `public System.Boolean Contains(T item)`
- `public System.Void ExceptWith(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Collections.Immutable.ImmutableHashSet<T> GetEnumerator()`
- `public System.Void IntersectWith(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean IsProperSubsetOf(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean IsProperSupersetOf(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean IsSubsetOf(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean IsSupersetOf(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean Overlaps(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean Remove(T item)`
- `public System.Boolean SetEquals(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Void SymmetricExceptWith(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Collections.Immutable.ImmutableHashSet<T> ToImmutable()`
- `public System.Boolean TryGetValue(T equalValue, out T& actualValue)`
- `public System.Void UnionWith(System.Collections.Generic.IEnumerable<T> other)`

### Builder<T> (class [sealed]) : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable, System.Collections.Generic.IList<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.IReadOnlyList<T>, System.Collections.ICollection, System.Collections.IList

- `public System.Int32 Count { get; }`
- `public T Item(System.Int32 index) { get; set; }`
- `public System.Void Add(T item)`
- `public System.Void AddRange(System.Collections.Generic.IEnumerable<T> items)`
- `public System.Int32 BinarySearch(T item)`
- `public System.Int32 BinarySearch(T item, System.Collections.Generic.IComparer<T> comparer)`
- `public System.Int32 BinarySearch(System.Int32 index, System.Int32 count, T item, System.Collections.Generic.IComparer<T> comparer)`
- `public System.Void Clear()`
- `public System.Boolean Contains(T item)`
- `public System.Collections.Immutable.ImmutableList<TOutput> ConvertAll<TOutput>(System.Func<T, TOutput> converter)`
- `public System.Void CopyTo(T[] array)`
- `public System.Void CopyTo(T[] array, System.Int32 arrayIndex)`
- `public System.Void CopyTo(System.Int32 index, T[] array, System.Int32 arrayIndex, System.Int32 count)`
- `public System.Boolean Exists(System.Predicate<T> match)`
- `public T Find(System.Predicate<T> match)`
- `public System.Collections.Immutable.ImmutableList<T> FindAll(System.Predicate<T> match)`
- `public System.Int32 FindIndex(System.Predicate<T> match)`
- `public System.Int32 FindIndex(System.Int32 startIndex, System.Predicate<T> match)`
- `public System.Int32 FindIndex(System.Int32 startIndex, System.Int32 count, System.Predicate<T> match)`
- `public T FindLast(System.Predicate<T> match)`
- `public System.Int32 FindLastIndex(System.Predicate<T> match)`
- `public System.Int32 FindLastIndex(System.Int32 startIndex, System.Predicate<T> match)`
- `public System.Int32 FindLastIndex(System.Int32 startIndex, System.Int32 count, System.Predicate<T> match)`
- `public System.Void ForEach(System.Action<T> action)`
- `public System.Collections.Immutable.ImmutableList<T> GetEnumerator()`
- `public System.Collections.Immutable.ImmutableList<T> GetRange(System.Int32 index, System.Int32 count)`
- `public System.Int32 IndexOf(T item)`
- `public System.Int32 IndexOf(T item, System.Int32 index)`
- `public System.Int32 IndexOf(T item, System.Int32 index, System.Int32 count)`
- `public System.Int32 IndexOf(T item, System.Int32 index, System.Int32 count, System.Collections.Generic.IEqualityComparer<T> equalityComparer)`
- `public System.Void Insert(System.Int32 index, T item)`
- `public System.Void InsertRange(System.Int32 index, System.Collections.Generic.IEnumerable<T> items)`
- `public T& ItemRef(System.Int32 index)`
- `public System.Int32 LastIndexOf(T item)`
- `public System.Int32 LastIndexOf(T item, System.Int32 startIndex)`
- `public System.Int32 LastIndexOf(T item, System.Int32 startIndex, System.Int32 count)`
- `public System.Int32 LastIndexOf(T item, System.Int32 startIndex, System.Int32 count, System.Collections.Generic.IEqualityComparer<T> equalityComparer)`
- `public System.Boolean Remove(T item)`
- `public System.Boolean Remove(T item, System.Collections.Generic.IEqualityComparer<T> equalityComparer)`
- `public System.Int32 RemoveAll(System.Predicate<T> match)`
- `public System.Void RemoveAt(System.Int32 index)`
- `public System.Void RemoveRange(System.Collections.Generic.IEnumerable<T> items)`
- `public System.Void RemoveRange(System.Int32 index, System.Int32 count)`
- `public System.Void RemoveRange(System.Collections.Generic.IEnumerable<T> items, System.Collections.Generic.IEqualityComparer<T> equalityComparer)`
- `public System.Void Replace(T oldValue, T newValue)`
- `public System.Void Replace(T oldValue, T newValue, System.Collections.Generic.IEqualityComparer<T> equalityComparer)`
- `public System.Void Reverse()`
- `public System.Void Reverse(System.Int32 index, System.Int32 count)`
- `public System.Void Sort()`
- `public System.Void Sort(System.Collections.Generic.IComparer<T> comparer)`
- `public System.Void Sort(System.Comparison<T> comparison)`
- `public System.Void Sort(System.Int32 index, System.Int32 count, System.Collections.Generic.IComparer<T> comparer)`
- `public System.Collections.Immutable.ImmutableList<T> ToImmutable()`
- `public System.Boolean TrueForAll(System.Predicate<T> match)`

### Builder<TKey, TValue> (class [sealed]) : System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.IEnumerable, System.Collections.Generic.IDictionary<TKey, TValue>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>, System.Collections.ICollection, System.Collections.IDictionary

- `public System.Int32 Count { get; }`
- `public TValue Item(TKey key) { get; set; }`
- `public System.Collections.Generic.IComparer<TKey> KeyComparer { get; set; }`
- `public System.Collections.Generic.IEnumerable<TKey> Keys { get; }`
- `public System.Collections.Generic.IEqualityComparer<TValue> ValueComparer { get; set; }`
- `public System.Collections.Generic.IEnumerable<TValue> Values { get; }`
- `public System.Void Add(System.Collections.Generic.KeyValuePair<TKey, TValue> item)`
- `public System.Void Add(TKey key, TValue value)`
- `public System.Void AddRange(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> items)`
- `public System.Void Clear()`
- `public System.Boolean Contains(System.Collections.Generic.KeyValuePair<TKey, TValue> item)`
- `public System.Boolean ContainsKey(TKey key)`
- `public System.Boolean ContainsValue(TValue value)`
- `public System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> GetEnumerator()`
- `public TValue GetValueOrDefault(TKey key)`
- `public TValue GetValueOrDefault(TKey key, TValue defaultValue)`
- `public System.Boolean Remove(System.Collections.Generic.KeyValuePair<TKey, TValue> item)`
- `public System.Boolean Remove(TKey key)`
- `public System.Void RemoveRange(System.Collections.Generic.IEnumerable<TKey> keys)`
- `public System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> ToImmutable()`
- `public System.Boolean TryGetKey(TKey equalKey, out TKey& actualKey)`
- `public System.Boolean TryGetValue(TKey key, out TValue& value)`
- `public TValue& ValueRef(TKey key)`

### Builder<T> (class [sealed]) : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.ISet<T>, System.Collections.ICollection

- `public System.Int32 Count { get; }`
- `public T Item(System.Int32 index) { get; }`
- `public System.Collections.Generic.IComparer<T> KeyComparer { get; set; }`
- `public T Max { get; }`
- `public T Min { get; }`
- `public System.Boolean Add(T item)`
- `public System.Void Clear()`
- `public System.Boolean Contains(T item)`
- `public System.Void ExceptWith(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Collections.Immutable.ImmutableSortedSet<T> GetEnumerator()`
- `public System.Int32 IndexOf(T item)`
- `public System.Void IntersectWith(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean IsProperSubsetOf(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean IsProperSupersetOf(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean IsSubsetOf(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean IsSupersetOf(System.Collections.Generic.IEnumerable<T> other)`
- `public T& ItemRef(System.Int32 index)`
- `public System.Boolean Overlaps(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean Remove(T item)`
- `public System.Collections.Generic.IEnumerable<T> Reverse()`
- `public System.Boolean SetEquals(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Void SymmetricExceptWith(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Collections.Immutable.ImmutableSortedSet<T> ToImmutable()`
- `public System.Boolean TryGetValue(T equalValue, out T& actualValue)`
- `public System.Void UnionWith(System.Collections.Generic.IEnumerable<T> other)`

### Enumerator<T> (struct)

- `public T Current { get; }`
- `public System.Boolean MoveNext()`

### Enumerator<TKey, TValue> (struct) : System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.IEnumerator, System.IDisposable

- `public System.Collections.Generic.KeyValuePair<TKey, TValue> Current { get; }`
- `public System.Void Dispose()`
- `public System.Boolean MoveNext()`
- `public System.Void Reset()`

### Enumerator<T> (struct) : System.Collections.Generic.IEnumerator<T>, System.Collections.IEnumerator, System.IDisposable

- `public T Current { get; }`
- `public System.Void Dispose()`
- `public System.Boolean MoveNext()`
- `public System.Void Reset()`

### Enumerator<T> (struct) : System.Collections.Generic.IEnumerator<T>, System.Collections.IEnumerator, System.IDisposable

- `public T Current { get; }`
- `public System.Void Dispose()`
- `public System.Boolean MoveNext()`
- `public System.Void Reset()`

### Enumerator<T> (struct)

- `public T Current { get; }`
- `public System.Boolean MoveNext()`

### Enumerator<TKey, TValue> (struct) : System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.IEnumerator, System.IDisposable

- `public System.Collections.Generic.KeyValuePair<TKey, TValue> Current { get; }`
- `public System.Void Dispose()`
- `public System.Boolean MoveNext()`
- `public System.Void Reset()`

### Enumerator<T> (struct) : System.Collections.Generic.IEnumerator<T>, System.Collections.IEnumerator, System.IDisposable

- `public T Current { get; }`
- `public System.Void Dispose()`
- `public System.Boolean MoveNext()`
- `public System.Void Reset()`

### Enumerator<T> (struct)

- `public T Current { get; }`
- `public System.Boolean MoveNext()`

### IImmutableDictionary`2<TKey, TValue> (interface) : System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.IEnumerable, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>

- `public System.Collections.Immutable.IImmutableDictionary<TKey, TValue> Add(TKey key, TValue value)`
- `public System.Collections.Immutable.IImmutableDictionary<TKey, TValue> AddRange(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> pairs)`
- `public System.Collections.Immutable.IImmutableDictionary<TKey, TValue> Clear()`
- `public System.Boolean Contains(System.Collections.Generic.KeyValuePair<TKey, TValue> pair)`
- `public System.Collections.Immutable.IImmutableDictionary<TKey, TValue> Remove(TKey key)`
- `public System.Collections.Immutable.IImmutableDictionary<TKey, TValue> RemoveRange(System.Collections.Generic.IEnumerable<TKey> keys)`
- `public System.Collections.Immutable.IImmutableDictionary<TKey, TValue> SetItem(TKey key, TValue value)`
- `public System.Collections.Immutable.IImmutableDictionary<TKey, TValue> SetItems(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> items)`
- `public System.Boolean TryGetKey(TKey equalKey, out TKey& actualKey)`

### IImmutableList`1<T> (interface) : System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.IReadOnlyList<T>

- `public System.Collections.Immutable.IImmutableList<T> Add(T value)`
- `public System.Collections.Immutable.IImmutableList<T> AddRange(System.Collections.Generic.IEnumerable<T> items)`
- `public System.Collections.Immutable.IImmutableList<T> Clear()`
- `public System.Int32 IndexOf(T item, System.Int32 index, System.Int32 count, System.Collections.Generic.IEqualityComparer<T> equalityComparer)`
- `public System.Collections.Immutable.IImmutableList<T> Insert(System.Int32 index, T element)`
- `public System.Collections.Immutable.IImmutableList<T> InsertRange(System.Int32 index, System.Collections.Generic.IEnumerable<T> items)`
- `public System.Int32 LastIndexOf(T item, System.Int32 index, System.Int32 count, System.Collections.Generic.IEqualityComparer<T> equalityComparer)`
- `public System.Collections.Immutable.IImmutableList<T> Remove(T value, System.Collections.Generic.IEqualityComparer<T> equalityComparer)`
- `public System.Collections.Immutable.IImmutableList<T> RemoveAll(System.Predicate<T> match)`
- `public System.Collections.Immutable.IImmutableList<T> RemoveAt(System.Int32 index)`
- `public System.Collections.Immutable.IImmutableList<T> RemoveRange(System.Collections.Generic.IEnumerable<T> items, System.Collections.Generic.IEqualityComparer<T> equalityComparer)`
- `public System.Collections.Immutable.IImmutableList<T> RemoveRange(System.Int32 index, System.Int32 count)`
- `public System.Collections.Immutable.IImmutableList<T> Replace(T oldValue, T newValue, System.Collections.Generic.IEqualityComparer<T> equalityComparer)`
- `public System.Collections.Immutable.IImmutableList<T> SetItem(System.Int32 index, T value)`

### IImmutableQueue`1<T> (interface) : System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable

- `public System.Boolean IsEmpty { get; }`
- `public System.Collections.Immutable.IImmutableQueue<T> Clear()`
- `public System.Collections.Immutable.IImmutableQueue<T> Dequeue()`
- `public System.Collections.Immutable.IImmutableQueue<T> Enqueue(T value)`
- `public T Peek()`

### IImmutableSet`1<T> (interface) : System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable, System.Collections.Generic.IReadOnlyCollection<T>

- `public System.Collections.Immutable.IImmutableSet<T> Add(T value)`
- `public System.Collections.Immutable.IImmutableSet<T> Clear()`
- `public System.Boolean Contains(T value)`
- `public System.Collections.Immutable.IImmutableSet<T> Except(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Collections.Immutable.IImmutableSet<T> Intersect(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean IsProperSubsetOf(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean IsProperSupersetOf(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean IsSubsetOf(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean IsSupersetOf(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean Overlaps(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Collections.Immutable.IImmutableSet<T> Remove(T value)`
- `public System.Boolean SetEquals(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Collections.Immutable.IImmutableSet<T> SymmetricExcept(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean TryGetValue(T equalValue, out T& actualValue)`
- `public System.Collections.Immutable.IImmutableSet<T> Union(System.Collections.Generic.IEnumerable<T> other)`

### IImmutableStack`1<T> (interface) : System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable

- `public System.Boolean IsEmpty { get; }`
- `public System.Collections.Immutable.IImmutableStack<T> Clear()`
- `public T Peek()`
- `public System.Collections.Immutable.IImmutableStack<T> Pop()`
- `public System.Collections.Immutable.IImmutableStack<T> Push(T value)`

### ImmutableArray (class [static])

- `[ext] public static System.Int32 BinarySearch<T>(this System.Collections.Immutable.ImmutableArray<T> array, T value)`
- `[ext] public static System.Int32 BinarySearch<T>(this System.Collections.Immutable.ImmutableArray<T> array, T value, System.Collections.Generic.IComparer<T> comparer)`
- `[ext] public static System.Int32 BinarySearch<T>(this System.Collections.Immutable.ImmutableArray<T> array, System.Int32 index, System.Int32 length, T value)`
- `[ext] public static System.Int32 BinarySearch<T>(this System.Collections.Immutable.ImmutableArray<T> array, System.Int32 index, System.Int32 length, T value, System.Collections.Generic.IComparer<T> comparer)`
- `public static System.Collections.Immutable.ImmutableArray<T> Create<T>()`
- `public static System.Collections.Immutable.ImmutableArray<T> Create<T>(T item)`
- `public static System.Collections.Immutable.ImmutableArray<T> Create<T>(params T[] items)`
- `public static System.Collections.Immutable.ImmutableArray<T> Create<T>(System.ReadOnlySpan<T> items)`
- `public static System.Collections.Immutable.ImmutableArray<T> Create<T>(System.Span<T> items)`
- `public static System.Collections.Immutable.ImmutableArray<T> Create<T>(T item1, T item2)`
- `public static System.Collections.Immutable.ImmutableArray<T> Create<T>(System.Collections.Immutable.ImmutableArray<T> items, System.Int32 start, System.Int32 length)`
- `public static System.Collections.Immutable.ImmutableArray<T> Create<T>(T item1, T item2, T item3)`
- `public static System.Collections.Immutable.ImmutableArray<T> Create<T>(T[] items, System.Int32 start, System.Int32 length)`
- `public static System.Collections.Immutable.ImmutableArray<T> Create<T>(T item1, T item2, T item3, T item4)`
- `public static System.Collections.Immutable.ImmutableArray<T> CreateBuilder<T>()`
- `public static System.Collections.Immutable.ImmutableArray<T> CreateBuilder<T>(System.Int32 initialCapacity)`
- `public static System.Collections.Immutable.ImmutableArray<T> CreateRange<T>(System.Collections.Generic.IEnumerable<T> items)`
- `public static System.Collections.Immutable.ImmutableArray<TResult> CreateRange<TSource, TResult>(System.Collections.Immutable.ImmutableArray<TSource> items, System.Func<TSource, TResult> selector)`
- `public static System.Collections.Immutable.ImmutableArray<TResult> CreateRange<TSource, TArg, TResult>(System.Collections.Immutable.ImmutableArray<TSource> items, System.Func<TSource, TArg, TResult> selector, TArg arg)`
- `public static System.Collections.Immutable.ImmutableArray<TResult> CreateRange<TSource, TResult>(System.Collections.Immutable.ImmutableArray<TSource> items, System.Int32 start, System.Int32 length, System.Func<TSource, TResult> selector)`
- `public static System.Collections.Immutable.ImmutableArray<TResult> CreateRange<TSource, TArg, TResult>(System.Collections.Immutable.ImmutableArray<TSource> items, System.Int32 start, System.Int32 length, System.Func<TSource, TArg, TResult> selector, TArg arg)`
- `[ext] public static System.Collections.Immutable.ImmutableArray<TSource> ToImmutableArray<TSource>(this System.Collections.Generic.IEnumerable<TSource> items)`
- `[ext] public static System.Collections.Immutable.ImmutableArray<TSource> ToImmutableArray<TSource>(this System.Collections.Immutable.ImmutableArray<TSource> builder)`
- `[ext] public static System.Collections.Immutable.ImmutableArray<T> ToImmutableArray<T>(this System.ReadOnlySpan<T> items)`
- `[ext] public static System.Collections.Immutable.ImmutableArray<T> ToImmutableArray<T>(this System.Span<T> items)`

### ImmutableArray`1<T> (struct [readonly struct]) : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable, System.Collections.Generic.IList<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.IReadOnlyList<T>, System.Collections.ICollection, System.Collections.IList, System.Collections.Immutable.IImmutableList<T>, System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable, System.IEquatable<System.Collections.Immutable.ImmutableArray<T>>

- `public static System.Collections.Immutable.ImmutableArray<T> Empty`
- `public System.Boolean IsDefault { get; }`
- `public System.Boolean IsDefaultOrEmpty { get; }`
- `public System.Boolean IsEmpty { get; }`
- `public T Item(System.Int32 index) { get; }`
- `public System.Int32 Length { get; }`
- `public System.Collections.Immutable.ImmutableArray<T> Add(T item)`
- `public System.Collections.Immutable.ImmutableArray<T> AddRange(System.Collections.Generic.IEnumerable<T> items)`
- `public System.Collections.Immutable.ImmutableArray<T> AddRange(System.Collections.Immutable.ImmutableArray<T> items)`
- `public System.Collections.Immutable.ImmutableArray<T> AddRange<TDerived>(TDerived[] items)`
- `where TDerived : T`
- `public System.Collections.Immutable.ImmutableArray<T> AddRange<TDerived>(System.Collections.Immutable.ImmutableArray<TDerived> items)`
- `where TDerived : T`
- `public System.Collections.Immutable.ImmutableArray<T> AddRange(System.ReadOnlySpan<T> items)`
- `public System.Collections.Immutable.ImmutableArray<T> AddRange(params T[] items)`
- `public System.Collections.Immutable.ImmutableArray<T> AddRange(T[] items, System.Int32 length)`
- `public System.Collections.Immutable.ImmutableArray<T> AddRange(System.Collections.Immutable.ImmutableArray<T> items, System.Int32 length)`
- `public System.Collections.Immutable.ImmutableArray<TOther> As<TOther>()`
- `where TOther : class`
- `public System.ReadOnlyMemory<T> AsMemory()`
- `public System.ReadOnlySpan<T> AsSpan()`
- `public System.ReadOnlySpan<T> AsSpan(System.Range range)`
- `public System.ReadOnlySpan<T> AsSpan(System.Int32 start, System.Int32 length)`
- `public System.Collections.Immutable.ImmutableArray<TOther> CastArray<TOther>()`
- `where TOther : class`
- `public static System.Collections.Immutable.ImmutableArray<T> CastUp<TDerived>(System.Collections.Immutable.ImmutableArray<TDerived> items)`
- `where TDerived : class, T`
- `public System.Collections.Immutable.ImmutableArray<T> Clear()`
- `public System.Boolean Contains(T item)`
- `public System.Boolean Contains(T item, System.Collections.Generic.IEqualityComparer<T> equalityComparer)`
- `public System.Void CopyTo(T[] destination)`
- `public System.Void CopyTo(System.Span<T> destination)`
- `public System.Void CopyTo(T[] destination, System.Int32 destinationIndex)`
- `public System.Void CopyTo(System.Int32 sourceIndex, T[] destination, System.Int32 destinationIndex, System.Int32 length)`
- `public System.Boolean Equals(System.Collections.Immutable.ImmutableArray<T> other)`
- `public System.Boolean Equals(System.Object obj)`
- `public System.Collections.Immutable.ImmutableArray<T> GetEnumerator()`
- `public System.Int32 GetHashCode()`
- `public System.Int32 IndexOf(T item)`
- `public System.Int32 IndexOf(T item, System.Int32 startIndex)`
- `public System.Int32 IndexOf(T item, System.Int32 startIndex, System.Collections.Generic.IEqualityComparer<T> equalityComparer)`
- `public System.Int32 IndexOf(T item, System.Int32 startIndex, System.Int32 count)`
- `public System.Int32 IndexOf(T item, System.Int32 startIndex, System.Int32 count, System.Collections.Generic.IEqualityComparer<T> equalityComparer)`
- `public System.Collections.Immutable.ImmutableArray<T> Insert(System.Int32 index, T item)`
- `public System.Collections.Immutable.ImmutableArray<T> InsertRange(System.Int32 index, System.Collections.Generic.IEnumerable<T> items)`
- `public System.Collections.Immutable.ImmutableArray<T> InsertRange(System.Int32 index, System.Collections.Immutable.ImmutableArray<T> items)`
- `public System.Collections.Immutable.ImmutableArray<T> InsertRange(System.Int32 index, T[] items)`
- `public System.Collections.Immutable.ImmutableArray<T> InsertRange(System.Int32 index, System.ReadOnlySpan<T> items)`
- `public T& ItemRef(System.Int32 index)`
- `public System.Int32 LastIndexOf(T item)`
- `public System.Int32 LastIndexOf(T item, System.Int32 startIndex)`
- `public System.Int32 LastIndexOf(T item, System.Int32 startIndex, System.Int32 count)`
- `public System.Int32 LastIndexOf(T item, System.Int32 startIndex, System.Int32 count, System.Collections.Generic.IEqualityComparer<T> equalityComparer)`
- `public System.Collections.Generic.IEnumerable<TResult> OfType<TResult>()`
- `public System.Collections.Immutable.ImmutableArray<T> Remove(T item)`
- `public System.Collections.Immutable.ImmutableArray<T> Remove(T item, System.Collections.Generic.IEqualityComparer<T> equalityComparer)`
- `public System.Collections.Immutable.ImmutableArray<T> RemoveAll(System.Predicate<T> match)`
- `public System.Collections.Immutable.ImmutableArray<T> RemoveAt(System.Int32 index)`
- `public System.Collections.Immutable.ImmutableArray<T> RemoveRange(System.Collections.Generic.IEnumerable<T> items)`
- `public System.Collections.Immutable.ImmutableArray<T> RemoveRange(System.Collections.Immutable.ImmutableArray<T> items)`
- `public System.Collections.Immutable.ImmutableArray<T> RemoveRange(System.Collections.Generic.IEnumerable<T> items, System.Collections.Generic.IEqualityComparer<T> equalityComparer)`
- `public System.Collections.Immutable.ImmutableArray<T> RemoveRange(System.Collections.Immutable.ImmutableArray<T> items, System.Collections.Generic.IEqualityComparer<T> equalityComparer)`
- `public System.Collections.Immutable.ImmutableArray<T> RemoveRange(System.Int32 index, System.Int32 length)`
- `public System.Collections.Immutable.ImmutableArray<T> RemoveRange(System.ReadOnlySpan<T> items, System.Collections.Generic.IEqualityComparer<T> equalityComparer)`
- `public System.Collections.Immutable.ImmutableArray<T> RemoveRange(T[] items, System.Collections.Generic.IEqualityComparer<T> equalityComparer)`
- `public System.Collections.Immutable.ImmutableArray<T> Replace(T oldValue, T newValue)`
- `public System.Collections.Immutable.ImmutableArray<T> Replace(T oldValue, T newValue, System.Collections.Generic.IEqualityComparer<T> equalityComparer)`
- `public System.Collections.Immutable.ImmutableArray<T> SetItem(System.Int32 index, T item)`
- `public System.Collections.Immutable.ImmutableArray<T> Slice(System.Int32 start, System.Int32 length)`
- `public System.Collections.Immutable.ImmutableArray<T> Sort()`
- `public System.Collections.Immutable.ImmutableArray<T> Sort(System.Collections.Generic.IComparer<T> comparer)`
- `public System.Collections.Immutable.ImmutableArray<T> Sort(System.Comparison<T> comparison)`
- `public System.Collections.Immutable.ImmutableArray<T> Sort(System.Int32 index, System.Int32 count, System.Collections.Generic.IComparer<T> comparer)`
- `public System.Collections.Immutable.ImmutableArray<T> ToBuilder()`
- `public static System.Boolean op_Equality(System.Collections.Immutable.ImmutableArray<T> left, System.Collections.Immutable.ImmutableArray<T> right)`
- `public static System.Boolean op_Equality(System.Collections.Immutable.ImmutableArray<T>? left, System.Collections.Immutable.ImmutableArray<T>? right)`
- `public static System.Boolean op_Inequality(System.Collections.Immutable.ImmutableArray<T> left, System.Collections.Immutable.ImmutableArray<T> right)`
- `public static System.Boolean op_Inequality(System.Collections.Immutable.ImmutableArray<T>? left, System.Collections.Immutable.ImmutableArray<T>? right)`

### ImmutableDictionary (class [static])

- `[ext] public static System.Boolean Contains<TKey, TValue>(this System.Collections.Immutable.IImmutableDictionary<TKey, TValue> map, TKey key, TValue value)`
- `public static System.Collections.Immutable.ImmutableDictionary<TKey, TValue> Create<TKey, TValue>()`
- `public static System.Collections.Immutable.ImmutableDictionary<TKey, TValue> Create<TKey, TValue>(System.Collections.Generic.IEqualityComparer<TKey> keyComparer)`
- `public static System.Collections.Immutable.ImmutableDictionary<TKey, TValue> Create<TKey, TValue>(System.Collections.Generic.IEqualityComparer<TKey> keyComparer, System.Collections.Generic.IEqualityComparer<TValue> valueComparer)`
- `public static System.Collections.Immutable.ImmutableDictionary<TKey, TValue> CreateBuilder<TKey, TValue>()`
- `public static System.Collections.Immutable.ImmutableDictionary<TKey, TValue> CreateBuilder<TKey, TValue>(System.Collections.Generic.IEqualityComparer<TKey> keyComparer)`
- `public static System.Collections.Immutable.ImmutableDictionary<TKey, TValue> CreateBuilder<TKey, TValue>(System.Collections.Generic.IEqualityComparer<TKey> keyComparer, System.Collections.Generic.IEqualityComparer<TValue> valueComparer)`
- `public static System.Collections.Immutable.ImmutableDictionary<TKey, TValue> CreateRange<TKey, TValue>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> items)`
- `public static System.Collections.Immutable.ImmutableDictionary<TKey, TValue> CreateRange<TKey, TValue>(System.Collections.Generic.IEqualityComparer<TKey> keyComparer, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> items)`
- `public static System.Collections.Immutable.ImmutableDictionary<TKey, TValue> CreateRange<TKey, TValue>(System.Collections.Generic.IEqualityComparer<TKey> keyComparer, System.Collections.Generic.IEqualityComparer<TValue> valueComparer, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> items)`
- `public static System.Collections.Immutable.ImmutableDictionary<TKey, TValue> CreateRangeWithOverwrite<TKey, TValue>(System.ReadOnlySpan<System.Collections.Generic.KeyValuePair<TKey, TValue>> items)`
- `public static System.Collections.Immutable.ImmutableDictionary<TKey, TValue> CreateRangeWithOverwrite<TKey, TValue>(System.Collections.Generic.IEqualityComparer<TKey> keyComparer, System.ReadOnlySpan<System.Collections.Generic.KeyValuePair<TKey, TValue>> items)`
- `[ext] public static TValue GetValueOrDefault<TKey, TValue>(this System.Collections.Immutable.IImmutableDictionary<TKey, TValue> dictionary, TKey key)`
- `[ext] public static TValue GetValueOrDefault<TKey, TValue>(this System.Collections.Immutable.IImmutableDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)`
- `[ext] public static System.Collections.Immutable.ImmutableDictionary<TKey, TValue> ToImmutableDictionary<TKey, TValue>(this System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> source)`
- `[ext] public static System.Collections.Immutable.ImmutableDictionary<TKey, TValue> ToImmutableDictionary<TKey, TValue>(this System.Collections.Immutable.ImmutableDictionary<TKey, TValue> builder)`
- `[ext] public static System.Collections.Immutable.ImmutableDictionary<TKey, TValue> ToImmutableDictionary<TKey, TValue>(this System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> source, System.Collections.Generic.IEqualityComparer<TKey> keyComparer)`
- `[ext] public static System.Collections.Immutable.ImmutableDictionary<TKey, TSource> ToImmutableDictionary<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector)`
- `[ext] public static System.Collections.Immutable.ImmutableDictionary<TKey, TValue> ToImmutableDictionary<TKey, TValue>(this System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> source, System.Collections.Generic.IEqualityComparer<TKey> keyComparer, System.Collections.Generic.IEqualityComparer<TValue> valueComparer)`
- `[ext] public static System.Collections.Immutable.ImmutableDictionary<TKey, TSource> ToImmutableDictionary<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IEqualityComparer<TKey> keyComparer)`
- `[ext] public static System.Collections.Immutable.ImmutableDictionary<TKey, TValue> ToImmutableDictionary<TSource, TKey, TValue>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TValue> elementSelector)`
- `[ext] public static System.Collections.Immutable.ImmutableDictionary<TKey, TValue> ToImmutableDictionary<TSource, TKey, TValue>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TValue> elementSelector, System.Collections.Generic.IEqualityComparer<TKey> keyComparer)`
- `[ext] public static System.Collections.Immutable.ImmutableDictionary<TKey, TValue> ToImmutableDictionary<TSource, TKey, TValue>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TValue> elementSelector, System.Collections.Generic.IEqualityComparer<TKey> keyComparer, System.Collections.Generic.IEqualityComparer<TValue> valueComparer)`

### ImmutableDictionary`2<TKey, TValue> (class [sealed]) : System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.IEnumerable, System.Collections.Generic.IDictionary<TKey, TValue>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>, System.Collections.ICollection, System.Collections.IDictionary, System.Collections.Immutable.IImmutableDictionary<TKey, TValue>

- `public static System.Collections.Immutable.ImmutableDictionary<TKey, TValue> Empty`
- `public System.Int32 Count { get; }`
- `public System.Boolean IsEmpty { get; }`
- `public TValue Item(TKey key) { get; }`
- `public System.Collections.Generic.IEqualityComparer<TKey> KeyComparer { get; }`
- `public System.Collections.Generic.IEnumerable<TKey> Keys { get; }`
- `public System.Collections.Generic.IEqualityComparer<TValue> ValueComparer { get; }`
- `public System.Collections.Generic.IEnumerable<TValue> Values { get; }`
- `public System.Collections.Immutable.ImmutableDictionary<TKey, TValue> Add(TKey key, TValue value)`
- `public System.Collections.Immutable.ImmutableDictionary<TKey, TValue> AddRange(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> pairs)`
- `public System.Collections.Immutable.ImmutableDictionary<TKey, TValue> Clear()`
- `public System.Boolean Contains(System.Collections.Generic.KeyValuePair<TKey, TValue> pair)`
- `public System.Boolean ContainsKey(TKey key)`
- `public System.Boolean ContainsValue(TValue value)`
- `public System.Collections.Immutable.ImmutableDictionary<TKey, TValue> GetEnumerator()`
- `public System.Collections.Immutable.ImmutableDictionary<TKey, TValue> Remove(TKey key)`
- `public System.Collections.Immutable.ImmutableDictionary<TKey, TValue> RemoveRange(System.Collections.Generic.IEnumerable<TKey> keys)`
- `public System.Collections.Immutable.ImmutableDictionary<TKey, TValue> SetItem(TKey key, TValue value)`
- `public System.Collections.Immutable.ImmutableDictionary<TKey, TValue> SetItems(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> items)`
- `public System.Collections.Immutable.ImmutableDictionary<TKey, TValue> ToBuilder()`
- `public System.Boolean TryGetKey(TKey equalKey, out TKey& actualKey)`
- `public System.Boolean TryGetValue(TKey key, out TValue& value)`
- `public System.Collections.Immutable.ImmutableDictionary<TKey, TValue> WithComparers(System.Collections.Generic.IEqualityComparer<TKey> keyComparer)`
- `public System.Collections.Immutable.ImmutableDictionary<TKey, TValue> WithComparers(System.Collections.Generic.IEqualityComparer<TKey> keyComparer, System.Collections.Generic.IEqualityComparer<TValue> valueComparer)`

### ImmutableHashSet (class [static])

- `public static System.Collections.Immutable.ImmutableHashSet<T> Create<T>()`
- `public static System.Collections.Immutable.ImmutableHashSet<T> Create<T>(System.Collections.Generic.IEqualityComparer<T> equalityComparer)`
- `public static System.Collections.Immutable.ImmutableHashSet<T> Create<T>(T item)`
- `public static System.Collections.Immutable.ImmutableHashSet<T> Create<T>(params T[] items)`
- `public static System.Collections.Immutable.ImmutableHashSet<T> Create<T>(System.ReadOnlySpan<T> items)`
- `public static System.Collections.Immutable.ImmutableHashSet<T> Create<T>(System.Collections.Generic.IEqualityComparer<T> equalityComparer, T item)`
- `public static System.Collections.Immutable.ImmutableHashSet<T> Create<T>(System.Collections.Generic.IEqualityComparer<T> equalityComparer, params T[] items)`
- `public static System.Collections.Immutable.ImmutableHashSet<T> Create<T>(System.Collections.Generic.IEqualityComparer<T> equalityComparer, System.ReadOnlySpan<T> items)`
- `public static System.Collections.Immutable.ImmutableHashSet<T> CreateBuilder<T>()`
- `public static System.Collections.Immutable.ImmutableHashSet<T> CreateBuilder<T>(System.Collections.Generic.IEqualityComparer<T> equalityComparer)`
- `public static System.Collections.Immutable.ImmutableHashSet<T> CreateRange<T>(System.Collections.Generic.IEnumerable<T> items)`
- `public static System.Collections.Immutable.ImmutableHashSet<T> CreateRange<T>(System.Collections.Generic.IEqualityComparer<T> equalityComparer, System.Collections.Generic.IEnumerable<T> items)`
- `[ext] public static System.Collections.Immutable.ImmutableHashSet<TSource> ToImmutableHashSet<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `[ext] public static System.Collections.Immutable.ImmutableHashSet<TSource> ToImmutableHashSet<TSource>(this System.Collections.Immutable.ImmutableHashSet<TSource> builder)`
- `[ext] public static System.Collections.Immutable.ImmutableHashSet<TSource> ToImmutableHashSet<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Collections.Generic.IEqualityComparer<TSource> equalityComparer)`

### ImmutableHashSet`1<T> (class [sealed]) : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.ISet<T>, System.Collections.Generic.IReadOnlySet<T>, System.Collections.ICollection, System.Collections.Immutable.IImmutableSet<T>

- `public static System.Collections.Immutable.ImmutableHashSet<T> Empty`
- `public System.Int32 Count { get; }`
- `public System.Boolean IsEmpty { get; }`
- `public System.Collections.Generic.IEqualityComparer<T> KeyComparer { get; }`
- `public System.Collections.Immutable.ImmutableHashSet<T> Add(T item)`
- `public System.Collections.Immutable.ImmutableHashSet<T> Clear()`
- `public System.Boolean Contains(T item)`
- `public System.Collections.Immutable.ImmutableHashSet<T> Except(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Collections.Immutable.ImmutableHashSet<T> GetEnumerator()`
- `public System.Collections.Immutable.ImmutableHashSet<T> Intersect(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean IsProperSubsetOf(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean IsProperSupersetOf(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean IsSubsetOf(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean IsSupersetOf(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean Overlaps(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Collections.Immutable.ImmutableHashSet<T> Remove(T item)`
- `public System.Boolean SetEquals(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Collections.Immutable.ImmutableHashSet<T> SymmetricExcept(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Collections.Immutable.ImmutableHashSet<T> ToBuilder()`
- `public System.Boolean TryGetValue(T equalValue, out T& actualValue)`
- `public System.Collections.Immutable.ImmutableHashSet<T> Union(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Collections.Immutable.ImmutableHashSet<T> WithComparer(System.Collections.Generic.IEqualityComparer<T> equalityComparer)`

### ImmutableInterlocked (class [static])

- `public static TValue AddOrUpdate<TKey, TValue>(ref System.Collections.Immutable.ImmutableDictionary<TKey, TValue>& location, TKey key, System.Func<TKey, TValue> addValueFactory, System.Func<TKey, TValue, TValue> updateValueFactory)`
- `public static TValue AddOrUpdate<TKey, TValue>(ref System.Collections.Immutable.ImmutableDictionary<TKey, TValue>& location, TKey key, TValue addValue, System.Func<TKey, TValue, TValue> updateValueFactory)`
- `public static System.Void Enqueue<T>(ref System.Collections.Immutable.ImmutableQueue<T>& location, T value)`
- `public static TValue GetOrAdd<TKey, TValue>(ref System.Collections.Immutable.ImmutableDictionary<TKey, TValue>& location, TKey key, System.Func<TKey, TValue> valueFactory)`
- `public static TValue GetOrAdd<TKey, TValue>(ref System.Collections.Immutable.ImmutableDictionary<TKey, TValue>& location, TKey key, TValue value)`
- `public static TValue GetOrAdd<TKey, TValue, TArg>(ref System.Collections.Immutable.ImmutableDictionary<TKey, TValue>& location, TKey key, System.Func<TKey, TArg, TValue> valueFactory, TArg factoryArgument)`
- `public static System.Collections.Immutable.ImmutableArray<T> InterlockedCompareExchange<T>(ref System.Collections.Immutable.ImmutableArray<T>& location, System.Collections.Immutable.ImmutableArray<T> value, System.Collections.Immutable.ImmutableArray<T> comparand)`
- `public static System.Collections.Immutable.ImmutableArray<T> InterlockedExchange<T>(ref System.Collections.Immutable.ImmutableArray<T>& location, System.Collections.Immutable.ImmutableArray<T> value)`
- `public static System.Boolean InterlockedInitialize<T>(ref System.Collections.Immutable.ImmutableArray<T>& location, System.Collections.Immutable.ImmutableArray<T> value)`
- `public static System.Void Push<T>(ref System.Collections.Immutable.ImmutableStack<T>& location, T value)`
- `public static System.Boolean TryAdd<TKey, TValue>(ref System.Collections.Immutable.ImmutableDictionary<TKey, TValue>& location, TKey key, TValue value)`
- `public static System.Boolean TryDequeue<T>(ref System.Collections.Immutable.ImmutableQueue<T>& location, out T& value)`
- `public static System.Boolean TryPop<T>(ref System.Collections.Immutable.ImmutableStack<T>& location, out T& value)`
- `public static System.Boolean TryRemove<TKey, TValue>(ref System.Collections.Immutable.ImmutableDictionary<TKey, TValue>& location, TKey key, out TValue& value)`
- `public static System.Boolean TryUpdate<TKey, TValue>(ref System.Collections.Immutable.ImmutableDictionary<TKey, TValue>& location, TKey key, TValue newValue, TValue comparisonValue)`
- `public static System.Boolean Update<T>(ref T& location, System.Func<T, T> transformer)`
- `where T : class`
- `public static System.Boolean Update<T>(ref System.Collections.Immutable.ImmutableArray<T>& location, System.Func<System.Collections.Immutable.ImmutableArray<T>, System.Collections.Immutable.ImmutableArray<T>> transformer)`
- `public static System.Boolean Update<T, TArg>(ref T& location, System.Func<T, TArg, T> transformer, TArg transformerArgument)`
- `where T : class`
- `public static System.Boolean Update<T, TArg>(ref System.Collections.Immutable.ImmutableArray<T>& location, System.Func<System.Collections.Immutable.ImmutableArray<T>, TArg, System.Collections.Immutable.ImmutableArray<T>> transformer, TArg transformerArgument)`

### ImmutableList (class [static])

- `public static System.Collections.Immutable.ImmutableList<T> Create<T>()`
- `public static System.Collections.Immutable.ImmutableList<T> Create<T>(T item)`
- `public static System.Collections.Immutable.ImmutableList<T> Create<T>(params T[] items)`
- `public static System.Collections.Immutable.ImmutableList<T> Create<T>(System.ReadOnlySpan<T> items)`
- `public static System.Collections.Immutable.ImmutableList<T> CreateBuilder<T>()`
- `public static System.Collections.Immutable.ImmutableList<T> CreateRange<T>(System.Collections.Generic.IEnumerable<T> items)`
- `[ext] public static System.Int32 IndexOf<T>(this System.Collections.Immutable.IImmutableList<T> list, T item)`
- `[ext] public static System.Int32 IndexOf<T>(this System.Collections.Immutable.IImmutableList<T> list, T item, System.Collections.Generic.IEqualityComparer<T> equalityComparer)`
- `[ext] public static System.Int32 IndexOf<T>(this System.Collections.Immutable.IImmutableList<T> list, T item, System.Int32 startIndex)`
- `[ext] public static System.Int32 IndexOf<T>(this System.Collections.Immutable.IImmutableList<T> list, T item, System.Int32 startIndex, System.Int32 count)`
- `[ext] public static System.Int32 LastIndexOf<T>(this System.Collections.Immutable.IImmutableList<T> list, T item)`
- `[ext] public static System.Int32 LastIndexOf<T>(this System.Collections.Immutable.IImmutableList<T> list, T item, System.Collections.Generic.IEqualityComparer<T> equalityComparer)`
- `[ext] public static System.Int32 LastIndexOf<T>(this System.Collections.Immutable.IImmutableList<T> list, T item, System.Int32 startIndex)`
- `[ext] public static System.Int32 LastIndexOf<T>(this System.Collections.Immutable.IImmutableList<T> list, T item, System.Int32 startIndex, System.Int32 count)`
- `[ext] public static System.Collections.Immutable.IImmutableList<T> Remove<T>(this System.Collections.Immutable.IImmutableList<T> list, T value)`
- `[ext] public static System.Collections.Immutable.IImmutableList<T> RemoveRange<T>(this System.Collections.Immutable.IImmutableList<T> list, System.Collections.Generic.IEnumerable<T> items)`
- `[ext] public static System.Collections.Immutable.IImmutableList<T> Replace<T>(this System.Collections.Immutable.IImmutableList<T> list, T oldValue, T newValue)`
- `[ext] public static System.Collections.Immutable.ImmutableList<TSource> ToImmutableList<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `[ext] public static System.Collections.Immutable.ImmutableList<TSource> ToImmutableList<TSource>(this System.Collections.Immutable.ImmutableList<TSource> builder)`

### ImmutableList`1<T> (class [sealed]) : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable, System.Collections.Generic.IList<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.IReadOnlyList<T>, System.Collections.ICollection, System.Collections.IList, System.Collections.Immutable.IImmutableList<T>

- `public static System.Collections.Immutable.ImmutableList<T> Empty`
- `public System.Int32 Count { get; }`
- `public System.Boolean IsEmpty { get; }`
- `public T Item(System.Int32 index) { get; }`
- `public System.Collections.Immutable.ImmutableList<T> Add(T value)`
- `public System.Collections.Immutable.ImmutableList<T> AddRange(System.Collections.Generic.IEnumerable<T> items)`
- `public System.Int32 BinarySearch(T item)`
- `public System.Int32 BinarySearch(T item, System.Collections.Generic.IComparer<T> comparer)`
- `public System.Int32 BinarySearch(System.Int32 index, System.Int32 count, T item, System.Collections.Generic.IComparer<T> comparer)`
- `public System.Collections.Immutable.ImmutableList<T> Clear()`
- `public System.Boolean Contains(T value)`
- `public System.Collections.Immutable.ImmutableList<TOutput> ConvertAll<TOutput>(System.Func<T, TOutput> converter)`
- `public System.Void CopyTo(T[] array)`
- `public System.Void CopyTo(T[] array, System.Int32 arrayIndex)`
- `public System.Void CopyTo(System.Int32 index, T[] array, System.Int32 arrayIndex, System.Int32 count)`
- `public System.Boolean Exists(System.Predicate<T> match)`
- `public T Find(System.Predicate<T> match)`
- `public System.Collections.Immutable.ImmutableList<T> FindAll(System.Predicate<T> match)`
- `public System.Int32 FindIndex(System.Predicate<T> match)`
- `public System.Int32 FindIndex(System.Int32 startIndex, System.Predicate<T> match)`
- `public System.Int32 FindIndex(System.Int32 startIndex, System.Int32 count, System.Predicate<T> match)`
- `public T FindLast(System.Predicate<T> match)`
- `public System.Int32 FindLastIndex(System.Predicate<T> match)`
- `public System.Int32 FindLastIndex(System.Int32 startIndex, System.Predicate<T> match)`
- `public System.Int32 FindLastIndex(System.Int32 startIndex, System.Int32 count, System.Predicate<T> match)`
- `public System.Void ForEach(System.Action<T> action)`
- `public System.Collections.Immutable.ImmutableList<T> GetEnumerator()`
- `public System.Collections.Immutable.ImmutableList<T> GetRange(System.Int32 index, System.Int32 count)`
- `public System.Int32 IndexOf(T value)`
- `public System.Int32 IndexOf(T item, System.Int32 index, System.Int32 count, System.Collections.Generic.IEqualityComparer<T> equalityComparer)`
- `public System.Collections.Immutable.ImmutableList<T> Insert(System.Int32 index, T item)`
- `public System.Collections.Immutable.ImmutableList<T> InsertRange(System.Int32 index, System.Collections.Generic.IEnumerable<T> items)`
- `public T& ItemRef(System.Int32 index)`
- `public System.Int32 LastIndexOf(T item, System.Int32 index, System.Int32 count, System.Collections.Generic.IEqualityComparer<T> equalityComparer)`
- `public System.Collections.Immutable.ImmutableList<T> Remove(T value)`
- `public System.Collections.Immutable.ImmutableList<T> Remove(T value, System.Collections.Generic.IEqualityComparer<T> equalityComparer)`
- `public System.Collections.Immutable.ImmutableList<T> RemoveAll(System.Predicate<T> match)`
- `public System.Collections.Immutable.ImmutableList<T> RemoveAt(System.Int32 index)`
- `public System.Collections.Immutable.ImmutableList<T> RemoveRange(System.Collections.Generic.IEnumerable<T> items)`
- `public System.Collections.Immutable.ImmutableList<T> RemoveRange(System.Collections.Generic.IEnumerable<T> items, System.Collections.Generic.IEqualityComparer<T> equalityComparer)`
- `public System.Collections.Immutable.ImmutableList<T> RemoveRange(System.Int32 index, System.Int32 count)`
- `public System.Collections.Immutable.ImmutableList<T> Replace(T oldValue, T newValue)`
- `public System.Collections.Immutable.ImmutableList<T> Replace(T oldValue, T newValue, System.Collections.Generic.IEqualityComparer<T> equalityComparer)`
- `public System.Collections.Immutable.ImmutableList<T> Reverse()`
- `public System.Collections.Immutable.ImmutableList<T> Reverse(System.Int32 index, System.Int32 count)`
- `public System.Collections.Immutable.ImmutableList<T> SetItem(System.Int32 index, T value)`
- `public System.Collections.Immutable.ImmutableList<T> Sort()`
- `public System.Collections.Immutable.ImmutableList<T> Sort(System.Collections.Generic.IComparer<T> comparer)`
- `public System.Collections.Immutable.ImmutableList<T> Sort(System.Comparison<T> comparison)`
- `public System.Collections.Immutable.ImmutableList<T> Sort(System.Int32 index, System.Int32 count, System.Collections.Generic.IComparer<T> comparer)`
- `public System.Collections.Immutable.ImmutableList<T> ToBuilder()`
- `public System.Boolean TrueForAll(System.Predicate<T> match)`

### ImmutableQueue (class [static])

- `public static System.Collections.Immutable.ImmutableQueue<T> Create<T>()`
- `public static System.Collections.Immutable.ImmutableQueue<T> Create<T>(T item)`
- `public static System.Collections.Immutable.ImmutableQueue<T> Create<T>(params T[] items)`
- `public static System.Collections.Immutable.ImmutableQueue<T> Create<T>(System.ReadOnlySpan<T> items)`
- `public static System.Collections.Immutable.ImmutableQueue<T> CreateRange<T>(System.Collections.Generic.IEnumerable<T> items)`
- `[ext] public static System.Collections.Immutable.IImmutableQueue<T> Dequeue<T>(this System.Collections.Immutable.IImmutableQueue<T> queue, out T& value)`

### ImmutableQueue`1<T> (class [sealed]) : System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable, System.Collections.Immutable.IImmutableQueue<T>

- `public static System.Collections.Immutable.ImmutableQueue<T> Empty { get; }`
- `public System.Boolean IsEmpty { get; }`
- `public System.Collections.Immutable.ImmutableQueue<T> Clear()`
- `public System.Collections.Immutable.ImmutableQueue<T> Dequeue()`
- `public System.Collections.Immutable.ImmutableQueue<T> Dequeue(out T& value)`
- `public System.Collections.Immutable.ImmutableQueue<T> Enqueue(T value)`
- `public System.Collections.Immutable.ImmutableQueue<T> GetEnumerator()`
- `public T Peek()`
- `public T& PeekRef()`

### ImmutableSortedDictionary (class [static])

- `public static System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> Create<TKey, TValue>()`
- `public static System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> Create<TKey, TValue>(System.Collections.Generic.IComparer<TKey> keyComparer)`
- `public static System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> Create<TKey, TValue>(System.Collections.Generic.IComparer<TKey> keyComparer, System.Collections.Generic.IEqualityComparer<TValue> valueComparer)`
- `public static System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> CreateBuilder<TKey, TValue>()`
- `public static System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> CreateBuilder<TKey, TValue>(System.Collections.Generic.IComparer<TKey> keyComparer)`
- `public static System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> CreateBuilder<TKey, TValue>(System.Collections.Generic.IComparer<TKey> keyComparer, System.Collections.Generic.IEqualityComparer<TValue> valueComparer)`
- `public static System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> CreateRange<TKey, TValue>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> items)`
- `public static System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> CreateRange<TKey, TValue>(System.Collections.Generic.IComparer<TKey> keyComparer, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> items)`
- `public static System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> CreateRange<TKey, TValue>(System.Collections.Generic.IComparer<TKey> keyComparer, System.Collections.Generic.IEqualityComparer<TValue> valueComparer, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> items)`
- `[ext] public static System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> ToImmutableSortedDictionary<TKey, TValue>(this System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> source)`
- `[ext] public static System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> ToImmutableSortedDictionary<TKey, TValue>(this System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> builder)`
- `[ext] public static System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> ToImmutableSortedDictionary<TKey, TValue>(this System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> source, System.Collections.Generic.IComparer<TKey> keyComparer)`
- `[ext] public static System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> ToImmutableSortedDictionary<TKey, TValue>(this System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> source, System.Collections.Generic.IComparer<TKey> keyComparer, System.Collections.Generic.IEqualityComparer<TValue> valueComparer)`
- `[ext] public static System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> ToImmutableSortedDictionary<TSource, TKey, TValue>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TValue> elementSelector)`
- `[ext] public static System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> ToImmutableSortedDictionary<TSource, TKey, TValue>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TValue> elementSelector, System.Collections.Generic.IComparer<TKey> keyComparer)`
- `[ext] public static System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> ToImmutableSortedDictionary<TSource, TKey, TValue>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TValue> elementSelector, System.Collections.Generic.IComparer<TKey> keyComparer, System.Collections.Generic.IEqualityComparer<TValue> valueComparer)`

### ImmutableSortedDictionary`2<TKey, TValue> (class [sealed]) : System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.IEnumerable, System.Collections.Generic.IDictionary<TKey, TValue>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>, System.Collections.ICollection, System.Collections.IDictionary, System.Collections.Immutable.IImmutableDictionary<TKey, TValue>

- `public static System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> Empty`
- `public System.Int32 Count { get; }`
- `public System.Boolean IsEmpty { get; }`
- `public TValue Item(TKey key) { get; }`
- `public System.Collections.Generic.IComparer<TKey> KeyComparer { get; }`
- `public System.Collections.Generic.IEnumerable<TKey> Keys { get; }`
- `public System.Collections.Generic.IEqualityComparer<TValue> ValueComparer { get; }`
- `public System.Collections.Generic.IEnumerable<TValue> Values { get; }`
- `public System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> Add(TKey key, TValue value)`
- `public System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> AddRange(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> items)`
- `public System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> Clear()`
- `public System.Boolean Contains(System.Collections.Generic.KeyValuePair<TKey, TValue> pair)`
- `public System.Boolean ContainsKey(TKey key)`
- `public System.Boolean ContainsValue(TValue value)`
- `public System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> GetEnumerator()`
- `public System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> Remove(TKey value)`
- `public System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> RemoveRange(System.Collections.Generic.IEnumerable<TKey> keys)`
- `public System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> SetItem(TKey key, TValue value)`
- `public System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> SetItems(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> items)`
- `public System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> ToBuilder()`
- `public System.Boolean TryGetKey(TKey equalKey, out TKey& actualKey)`
- `public System.Boolean TryGetValue(TKey key, out TValue& value)`
- `public TValue& ValueRef(TKey key)`
- `public System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> WithComparers(System.Collections.Generic.IComparer<TKey> keyComparer)`
- `public System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> WithComparers(System.Collections.Generic.IComparer<TKey> keyComparer, System.Collections.Generic.IEqualityComparer<TValue> valueComparer)`

### ImmutableSortedSet (class [static])

- `public static System.Collections.Immutable.ImmutableSortedSet<T> Create<T>()`
- `public static System.Collections.Immutable.ImmutableSortedSet<T> Create<T>(System.Collections.Generic.IComparer<T> comparer)`
- `public static System.Collections.Immutable.ImmutableSortedSet<T> Create<T>(T item)`
- `public static System.Collections.Immutable.ImmutableSortedSet<T> Create<T>(params T[] items)`
- `public static System.Collections.Immutable.ImmutableSortedSet<T> Create<T>(System.ReadOnlySpan<T> items)`
- `public static System.Collections.Immutable.ImmutableSortedSet<T> Create<T>(System.Collections.Generic.IComparer<T> comparer, T item)`
- `public static System.Collections.Immutable.ImmutableSortedSet<T> Create<T>(System.Collections.Generic.IComparer<T> comparer, params T[] items)`
- `public static System.Collections.Immutable.ImmutableSortedSet<T> Create<T>(System.Collections.Generic.IComparer<T> comparer, System.ReadOnlySpan<T> items)`
- `public static System.Collections.Immutable.ImmutableSortedSet<T> CreateBuilder<T>()`
- `public static System.Collections.Immutable.ImmutableSortedSet<T> CreateBuilder<T>(System.Collections.Generic.IComparer<T> comparer)`
- `public static System.Collections.Immutable.ImmutableSortedSet<T> CreateRange<T>(System.Collections.Generic.IEnumerable<T> items)`
- `public static System.Collections.Immutable.ImmutableSortedSet<T> CreateRange<T>(System.Collections.Generic.IComparer<T> comparer, System.Collections.Generic.IEnumerable<T> items)`
- `[ext] public static System.Collections.Immutable.ImmutableSortedSet<TSource> ToImmutableSortedSet<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `[ext] public static System.Collections.Immutable.ImmutableSortedSet<TSource> ToImmutableSortedSet<TSource>(this System.Collections.Immutable.ImmutableSortedSet<TSource> builder)`
- `[ext] public static System.Collections.Immutable.ImmutableSortedSet<TSource> ToImmutableSortedSet<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Collections.Generic.IComparer<TSource> comparer)`

### ImmutableSortedSet`1<T> (class [sealed]) : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable, System.Collections.Generic.IList<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.IReadOnlyList<T>, System.Collections.Generic.ISet<T>, System.Collections.Generic.IReadOnlySet<T>, System.Collections.ICollection, System.Collections.IList, System.Collections.Immutable.IImmutableSet<T>

- `public static System.Collections.Immutable.ImmutableSortedSet<T> Empty`
- `public System.Int32 Count { get; }`
- `public System.Boolean IsEmpty { get; }`
- `public T Item(System.Int32 index) { get; }`
- `public System.Collections.Generic.IComparer<T> KeyComparer { get; }`
- `public T Max { get; }`
- `public T Min { get; }`
- `public System.Collections.Immutable.ImmutableSortedSet<T> Add(T value)`
- `public System.Collections.Immutable.ImmutableSortedSet<T> Clear()`
- `public System.Boolean Contains(T value)`
- `public System.Collections.Immutable.ImmutableSortedSet<T> Except(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Collections.Immutable.ImmutableSortedSet<T> GetEnumerator()`
- `public System.Int32 IndexOf(T item)`
- `public System.Collections.Immutable.ImmutableSortedSet<T> Intersect(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean IsProperSubsetOf(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean IsProperSupersetOf(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean IsSubsetOf(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean IsSupersetOf(System.Collections.Generic.IEnumerable<T> other)`
- `public T& ItemRef(System.Int32 index)`
- `public System.Boolean Overlaps(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Collections.Immutable.ImmutableSortedSet<T> Remove(T value)`
- `public System.Collections.Generic.IEnumerable<T> Reverse()`
- `public System.Boolean SetEquals(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Collections.Immutable.ImmutableSortedSet<T> SymmetricExcept(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Collections.Immutable.ImmutableSortedSet<T> ToBuilder()`
- `public System.Boolean TryGetValue(T equalValue, out T& actualValue)`
- `public System.Collections.Immutable.ImmutableSortedSet<T> Union(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Collections.Immutable.ImmutableSortedSet<T> WithComparer(System.Collections.Generic.IComparer<T> comparer)`

### ImmutableStack (class [static])

- `public static System.Collections.Immutable.ImmutableStack<T> Create<T>()`
- `public static System.Collections.Immutable.ImmutableStack<T> Create<T>(T item)`
- `public static System.Collections.Immutable.ImmutableStack<T> Create<T>(params T[] items)`
- `public static System.Collections.Immutable.ImmutableStack<T> Create<T>(System.ReadOnlySpan<T> items)`
- `public static System.Collections.Immutable.ImmutableStack<T> CreateRange<T>(System.Collections.Generic.IEnumerable<T> items)`
- `[ext] public static System.Collections.Immutable.IImmutableStack<T> Pop<T>(this System.Collections.Immutable.IImmutableStack<T> stack, out T& value)`

### ImmutableStack`1<T> (class [sealed]) : System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable, System.Collections.Immutable.IImmutableStack<T>

- `public static System.Collections.Immutable.ImmutableStack<T> Empty { get; }`
- `public System.Boolean IsEmpty { get; }`
- `public System.Collections.Immutable.ImmutableStack<T> Clear()`
- `public System.Collections.Immutable.ImmutableStack<T> GetEnumerator()`
- `public T Peek()`
- `public T& PeekRef()`
- `public System.Collections.Immutable.ImmutableStack<T> Pop()`
- `public System.Collections.Immutable.ImmutableStack<T> Pop(out T& value)`
- `public System.Collections.Immutable.ImmutableStack<T> Push(T value)`

