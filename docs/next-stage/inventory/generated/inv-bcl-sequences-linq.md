# Public API inventory: bcl-sequences-linq

Assemblies: System.Linq 10.0.0.0, System.Linq.AsyncEnumerable 10.0.0.0, System.Runtime 10.0.0.0, System.Collections 10.0.0.0, System.Memory 10.0.0.0, System.Buffers 10.0.0.0, System.Threading.Tasks.Extensions 10.0.0.0

Type count: 73

## System

### Array (class [abstract]) : System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList, System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable, System.ICloneable

- `public System.Boolean IsFixedSize { get; }`
- `public System.Boolean IsReadOnly { get; }`
- `public System.Boolean IsSynchronized { get; }`
- `public System.Int32 Length { get; }`
- `public System.Int64 LongLength { get; }`
- `public static System.Int32 MaxLength { get; }`
- `public System.Int32 Rank { get; }`
- `public System.Object SyncRoot { get; }`
- `public static System.Collections.ObjectModel.ReadOnlyCollection<T> AsReadOnly<T>(T[] array)`
- `public static System.Int32 BinarySearch(System.Array array, System.Object value)`
- `public static System.Int32 BinarySearch<T>(T[] array, T value)`
- `public static System.Int32 BinarySearch(System.Array array, System.Object value, System.Collections.IComparer comparer)`
- `public static System.Int32 BinarySearch<T>(T[] array, T value, System.Collections.Generic.IComparer<T> comparer)`
- `public static System.Int32 BinarySearch(System.Array array, System.Int32 index, System.Int32 length, System.Object value)`
- `public static System.Int32 BinarySearch<T>(T[] array, System.Int32 index, System.Int32 length, T value)`
- `public static System.Int32 BinarySearch(System.Array array, System.Int32 index, System.Int32 length, System.Object value, System.Collections.IComparer comparer)`
- `public static System.Int32 BinarySearch<T>(T[] array, System.Int32 index, System.Int32 length, T value, System.Collections.Generic.IComparer<T> comparer)`
- `public static System.Void Clear(System.Array array)`
- `public static System.Void Clear(System.Array array, System.Int32 index, System.Int32 length)`
- `public System.Object Clone()`
- `public static System.Void ConstrainedCopy(System.Array sourceArray, System.Int32 sourceIndex, System.Array destinationArray, System.Int32 destinationIndex, System.Int32 length)`
- `public static TOutput[] ConvertAll<TInput, TOutput>(TInput[] array, System.Converter<TInput, TOutput> converter)`
- `public static System.Void Copy(System.Array sourceArray, System.Array destinationArray, System.Int32 length)`
- `public static System.Void Copy(System.Array sourceArray, System.Array destinationArray, System.Int64 length)`
- `public static System.Void Copy(System.Array sourceArray, System.Int32 sourceIndex, System.Array destinationArray, System.Int32 destinationIndex, System.Int32 length)`
- `public static System.Void Copy(System.Array sourceArray, System.Int64 sourceIndex, System.Array destinationArray, System.Int64 destinationIndex, System.Int64 length)`
- `public System.Void CopyTo(System.Array array, System.Int32 index)`
- `public System.Void CopyTo(System.Array array, System.Int64 index)`
- `public static System.Array CreateInstance(System.Type elementType, System.Int32 length)`
- `public static System.Array CreateInstance(System.Type elementType, params System.Int32[] lengths)`
- `public static System.Array CreateInstance(System.Type elementType, params System.Int64[] lengths)`
- `public static System.Array CreateInstance(System.Type elementType, System.Int32 length1, System.Int32 length2)`
- `public static System.Array CreateInstance(System.Type elementType, System.Int32[] lengths, System.Int32[] lowerBounds)`
- `public static System.Array CreateInstance(System.Type elementType, System.Int32 length1, System.Int32 length2, System.Int32 length3)`
- `public static System.Array CreateInstanceFromArrayType(System.Type arrayType, System.Int32 length)`
- `public static System.Array CreateInstanceFromArrayType(System.Type arrayType, params System.Int32[] lengths)`
- `public static System.Array CreateInstanceFromArrayType(System.Type arrayType, System.Int32[] lengths, System.Int32[] lowerBounds)`
- `public static T[] Empty<T>()`
- `public static System.Boolean Exists<T>(T[] array, System.Predicate<T> match)`
- `public static System.Void Fill<T>(T[] array, T value)`
- `public static System.Void Fill<T>(T[] array, T value, System.Int32 startIndex, System.Int32 count)`
- `public static T Find<T>(T[] array, System.Predicate<T> match)`
- `public static T[] FindAll<T>(T[] array, System.Predicate<T> match)`
- `public static System.Int32 FindIndex<T>(T[] array, System.Predicate<T> match)`
- `public static System.Int32 FindIndex<T>(T[] array, System.Int32 startIndex, System.Predicate<T> match)`
- `public static System.Int32 FindIndex<T>(T[] array, System.Int32 startIndex, System.Int32 count, System.Predicate<T> match)`
- `public static T FindLast<T>(T[] array, System.Predicate<T> match)`
- `public static System.Int32 FindLastIndex<T>(T[] array, System.Predicate<T> match)`
- `public static System.Int32 FindLastIndex<T>(T[] array, System.Int32 startIndex, System.Predicate<T> match)`
- `public static System.Int32 FindLastIndex<T>(T[] array, System.Int32 startIndex, System.Int32 count, System.Predicate<T> match)`
- `public static System.Void ForEach<T>(T[] array, System.Action<T> action)`
- `public System.Collections.IEnumerator GetEnumerator()`
- `public System.Int32 GetLength(System.Int32 dimension)`
- `public System.Int64 GetLongLength(System.Int32 dimension)`
- `public System.Int32 GetLowerBound(System.Int32 dimension)`
- `public System.Int32 GetUpperBound(System.Int32 dimension)`
- `public System.Object GetValue(System.Int32 index)`
- `public System.Object GetValue(params System.Int32[] indices)`
- `public System.Object GetValue(System.Int64 index)`
- `public System.Object GetValue(params System.Int64[] indices)`
- `public System.Object GetValue(System.Int32 index1, System.Int32 index2)`
- `public System.Object GetValue(System.Int64 index1, System.Int64 index2)`
- `public System.Object GetValue(System.Int32 index1, System.Int32 index2, System.Int32 index3)`
- `public System.Object GetValue(System.Int64 index1, System.Int64 index2, System.Int64 index3)`
- `public static System.Int32 IndexOf(System.Array array, System.Object value)`
- `public static System.Int32 IndexOf<T>(T[] array, T value)`
- `public static System.Int32 IndexOf(System.Array array, System.Object value, System.Int32 startIndex)`
- `public static System.Int32 IndexOf<T>(T[] array, T value, System.Int32 startIndex)`
- `public static System.Int32 IndexOf(System.Array array, System.Object value, System.Int32 startIndex, System.Int32 count)`
- `public static System.Int32 IndexOf<T>(T[] array, T value, System.Int32 startIndex, System.Int32 count)`
- `public System.Void Initialize()`
- `public static System.Int32 LastIndexOf(System.Array array, System.Object value)`
- `public static System.Int32 LastIndexOf<T>(T[] array, T value)`
- `public static System.Int32 LastIndexOf(System.Array array, System.Object value, System.Int32 startIndex)`
- `public static System.Int32 LastIndexOf<T>(T[] array, T value, System.Int32 startIndex)`
- `public static System.Int32 LastIndexOf(System.Array array, System.Object value, System.Int32 startIndex, System.Int32 count)`
- `public static System.Int32 LastIndexOf<T>(T[] array, T value, System.Int32 startIndex, System.Int32 count)`
- `public static System.Void Resize<T>(ref T[]& array, System.Int32 newSize)`
- `public static System.Void Reverse(System.Array array)`
- `public static System.Void Reverse<T>(T[] array)`
- `public static System.Void Reverse(System.Array array, System.Int32 index, System.Int32 length)`
- `public static System.Void Reverse<T>(T[] array, System.Int32 index, System.Int32 length)`
- `public System.Void SetValue(System.Object value, System.Int32 index)`
- `public System.Void SetValue(System.Object value, params System.Int32[] indices)`
- `public System.Void SetValue(System.Object value, System.Int64 index)`
- `public System.Void SetValue(System.Object value, params System.Int64[] indices)`
- `public System.Void SetValue(System.Object value, System.Int32 index1, System.Int32 index2)`
- `public System.Void SetValue(System.Object value, System.Int64 index1, System.Int64 index2)`
- `public System.Void SetValue(System.Object value, System.Int32 index1, System.Int32 index2, System.Int32 index3)`
- `public System.Void SetValue(System.Object value, System.Int64 index1, System.Int64 index2, System.Int64 index3)`
- `public static System.Void Sort(System.Array array)`
- `public static System.Void Sort<T>(T[] array)`
- `public static System.Void Sort(System.Array keys, System.Array items)`
- `public static System.Void Sort(System.Array array, System.Collections.IComparer comparer)`
- `public static System.Void Sort<T>(T[] array, System.Collections.Generic.IComparer<T> comparer)`
- `public static System.Void Sort<T>(T[] array, System.Comparison<T> comparison)`
- `public static System.Void Sort<TKey, TValue>(TKey[] keys, TValue[] items)`
- `public static System.Void Sort(System.Array keys, System.Array items, System.Collections.IComparer comparer)`
- `public static System.Void Sort(System.Array array, System.Int32 index, System.Int32 length)`
- `public static System.Void Sort<T>(T[] array, System.Int32 index, System.Int32 length)`
- `public static System.Void Sort<TKey, TValue>(TKey[] keys, TValue[] items, System.Collections.Generic.IComparer<TKey> comparer)`
- `public static System.Void Sort(System.Array keys, System.Array items, System.Int32 index, System.Int32 length)`
- `public static System.Void Sort(System.Array array, System.Int32 index, System.Int32 length, System.Collections.IComparer comparer)`
- `public static System.Void Sort<T>(T[] array, System.Int32 index, System.Int32 length, System.Collections.Generic.IComparer<T> comparer)`
- `public static System.Void Sort<TKey, TValue>(TKey[] keys, TValue[] items, System.Int32 index, System.Int32 length)`
- `public static System.Void Sort(System.Array keys, System.Array items, System.Int32 index, System.Int32 length, System.Collections.IComparer comparer)`
- `public static System.Void Sort<TKey, TValue>(TKey[] keys, TValue[] items, System.Int32 index, System.Int32 length, System.Collections.Generic.IComparer<TKey> comparer)`
- `public static System.Boolean TrueForAll<T>(T[] array, System.Predicate<T> match)`

### ArraySegment`1<T> (struct [readonly struct]) : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable, System.Collections.Generic.IList<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.IReadOnlyList<T>

- `public ArraySegment`1(T[] array)`
- `public ArraySegment`1(T[] array, System.Int32 offset, System.Int32 count)`
- `public T[] Array { get; }`
- `public System.Int32 Count { get; }`
- `public static System.ArraySegment<T> Empty { get; }`
- `public T Item(System.Int32 index) { get; set; }`
- `public System.Int32 Offset { get; }`
- `public System.Void CopyTo(System.ArraySegment<T> destination)`
- `public System.Void CopyTo(T[] destination)`
- `public System.Void CopyTo(T[] destination, System.Int32 destinationIndex)`
- `public System.Boolean Equals(System.ArraySegment<T> obj)`
- `public System.Boolean Equals(System.Object obj)`
- `public System.ArraySegment<T> GetEnumerator()`
- `public System.Int32 GetHashCode()`
- `public System.ArraySegment<T> Slice(System.Int32 index)`
- `public System.ArraySegment<T> Slice(System.Int32 index, System.Int32 count)`
- `public T[] ToArray()`
- `public static System.Boolean op_Equality(System.ArraySegment<T> a, System.ArraySegment<T> b)`
- `public static System.ArraySegment<T> op_Implicit(T[] array)`
- `public static System.Boolean op_Inequality(System.ArraySegment<T> a, System.ArraySegment<T> b)`

### Enumerator<T> (struct) : System.Collections.Generic.IEnumerator<T>, System.Collections.IEnumerator, System.IDisposable

- `public T Current { get; }`
- `public System.Void Dispose()`
- `public System.Boolean MoveNext()`

### Enumerator<T> (struct [ref struct]) : System.Collections.Generic.IEnumerator<T>, System.Collections.IEnumerator, System.IDisposable

- `public T& Current { get; }`
- `public System.Boolean MoveNext()`

### Enumerator<T> (struct [ref struct]) : System.Collections.Generic.IEnumerator<T>, System.Collections.IEnumerator, System.IDisposable

- `public T& Current { get; }`
- `public System.Boolean MoveNext()`

### MemoryExtensions (class [static])

- `[ext] public static System.ReadOnlyMemory<System.Char> AsMemory(this System.String text)`
- `[ext] public static System.Memory<T> AsMemory<T>(this System.ArraySegment<T> segment)`
- `[ext] public static System.Memory<T> AsMemory<T>(this T[] array)`
- `[ext] public static System.ReadOnlyMemory<System.Char> AsMemory(this System.String text, System.Index startIndex)`
- `[ext] public static System.ReadOnlyMemory<System.Char> AsMemory(this System.String text, System.Int32 start)`
- `[ext] public static System.ReadOnlyMemory<System.Char> AsMemory(this System.String text, System.Range range)`
- `[ext] public static System.Memory<T> AsMemory<T>(this System.ArraySegment<T> segment, System.Int32 start)`
- `[ext] public static System.Memory<T> AsMemory<T>(this T[] array, System.Index startIndex)`
- `[ext] public static System.Memory<T> AsMemory<T>(this T[] array, System.Int32 start)`
- `[ext] public static System.Memory<T> AsMemory<T>(this T[] array, System.Range range)`
- `[ext] public static System.ReadOnlyMemory<System.Char> AsMemory(this System.String text, System.Int32 start, System.Int32 length)`
- `[ext] public static System.Memory<T> AsMemory<T>(this System.ArraySegment<T> segment, System.Int32 start, System.Int32 length)`
- `[ext] public static System.Memory<T> AsMemory<T>(this T[] array, System.Int32 start, System.Int32 length)`
- `[ext] public static System.ReadOnlySpan<System.Char> AsSpan(this System.String text)`
- `[ext] public static System.Span<T> AsSpan<T>(this System.ArraySegment<T> segment)`
- `[ext] public static System.Span<T> AsSpan<T>(this T[] array)`
- `[ext] public static System.ReadOnlySpan<System.Char> AsSpan(this System.String text, System.Int32 start)`
- `[ext] public static System.ReadOnlySpan<System.Char> AsSpan(this System.String text, System.Index startIndex)`
- `[ext] public static System.ReadOnlySpan<System.Char> AsSpan(this System.String text, System.Range range)`
- `[ext] public static System.Span<T> AsSpan<T>(this System.ArraySegment<T> segment, System.Index startIndex)`
- `[ext] public static System.Span<T> AsSpan<T>(this System.ArraySegment<T> segment, System.Int32 start)`
- `[ext] public static System.Span<T> AsSpan<T>(this System.ArraySegment<T> segment, System.Range range)`
- `[ext] public static System.Span<T> AsSpan<T>(this T[] array, System.Index startIndex)`
- `[ext] public static System.Span<T> AsSpan<T>(this T[] array, System.Int32 start)`
- `[ext] public static System.Span<T> AsSpan<T>(this T[] array, System.Range range)`
- `[ext] public static System.ReadOnlySpan<System.Char> AsSpan(this System.String text, System.Int32 start, System.Int32 length)`
- `[ext] public static System.Span<T> AsSpan<T>(this System.ArraySegment<T> segment, System.Int32 start, System.Int32 length)`
- `[ext] public static System.Span<T> AsSpan<T>(this T[] array, System.Int32 start, System.Int32 length)`
- `[ext] public static System.Int32 BinarySearch<T>(this System.ReadOnlySpan<T> span, System.IComparable<T> comparable)`
- `[ext] public static System.Int32 BinarySearch<T>(this System.Span<T> span, System.IComparable<T> comparable)`
- `[ext] public static System.Int32 BinarySearch<T, TComparable>(this System.ReadOnlySpan<T> span, TComparable comparable)`
- `where TComparable : System.IComparable<T>`
- `[ext] public static System.Int32 BinarySearch<T, TComparable>(this System.Span<T> span, TComparable comparable)`
- `where TComparable : System.IComparable<T>`
- `[ext] public static System.Int32 BinarySearch<T, TComparer>(this System.ReadOnlySpan<T> span, T value, TComparer comparer)`
- `where TComparer : System.Collections.Generic.IComparer<T>`
- `[ext] public static System.Int32 BinarySearch<T, TComparer>(this System.Span<T> span, T value, TComparer comparer)`
- `where TComparer : System.Collections.Generic.IComparer<T>`
- `[ext] public static System.Int32 CommonPrefixLength<T>(this System.Span<T> span, System.ReadOnlySpan<T> other)`
- `[ext] public static System.Int32 CommonPrefixLength<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> other)`
- `[ext] public static System.Int32 CommonPrefixLength<T>(this System.Span<T> span, System.ReadOnlySpan<T> other, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `[ext] public static System.Int32 CommonPrefixLength<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> other, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `[ext] public static System.Int32 CompareTo(this System.ReadOnlySpan<System.Char> span, System.ReadOnlySpan<System.Char> other, System.StringComparison comparisonType)`
- `[ext] public static System.Boolean Contains<T>(this System.ReadOnlySpan<T> span, T value)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Boolean Contains<T>(this System.Span<T> span, T value)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Boolean Contains(this System.ReadOnlySpan<System.Char> span, System.ReadOnlySpan<System.Char> value, System.StringComparison comparisonType)`
- `[ext] public static System.Boolean Contains<T>(this System.ReadOnlySpan<T> span, T value, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `[ext] public static System.Boolean ContainsAny(this System.ReadOnlySpan<System.Char> span, System.Buffers.SearchValues<System.String> values)`
- `[ext] public static System.Boolean ContainsAny(this System.Span<System.Char> span, System.Buffers.SearchValues<System.String> values)`
- `[ext] public static System.Boolean ContainsAny<T>(this System.ReadOnlySpan<T> span, System.Buffers.SearchValues<T> values)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Boolean ContainsAny<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> values)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Boolean ContainsAny<T>(this System.Span<T> span, System.Buffers.SearchValues<T> values)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Boolean ContainsAny<T>(this System.Span<T> span, System.ReadOnlySpan<T> values)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Boolean ContainsAny<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> values, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `[ext] public static System.Boolean ContainsAny<T>(this System.ReadOnlySpan<T> span, T value0, T value1)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Boolean ContainsAny<T>(this System.Span<T> span, T value0, T value1)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Boolean ContainsAny<T>(this System.ReadOnlySpan<T> span, T value0, T value1, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `[ext] public static System.Boolean ContainsAny<T>(this System.ReadOnlySpan<T> span, T value0, T value1, T value2)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Boolean ContainsAny<T>(this System.Span<T> span, T value0, T value1, T value2)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Boolean ContainsAny<T>(this System.ReadOnlySpan<T> span, T value0, T value1, T value2, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `[ext] public static System.Boolean ContainsAnyExcept<T>(this System.ReadOnlySpan<T> span, System.Buffers.SearchValues<T> values)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Boolean ContainsAnyExcept<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> values)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Boolean ContainsAnyExcept<T>(this System.ReadOnlySpan<T> span, T value)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Boolean ContainsAnyExcept<T>(this System.Span<T> span, System.Buffers.SearchValues<T> values)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Boolean ContainsAnyExcept<T>(this System.Span<T> span, System.ReadOnlySpan<T> values)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Boolean ContainsAnyExcept<T>(this System.Span<T> span, T value)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Boolean ContainsAnyExcept<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> values, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `[ext] public static System.Boolean ContainsAnyExcept<T>(this System.ReadOnlySpan<T> span, T value, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `[ext] public static System.Boolean ContainsAnyExcept<T>(this System.ReadOnlySpan<T> span, T value0, T value1)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Boolean ContainsAnyExcept<T>(this System.Span<T> span, T value0, T value1)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Boolean ContainsAnyExcept<T>(this System.ReadOnlySpan<T> span, T value0, T value1, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `[ext] public static System.Boolean ContainsAnyExcept<T>(this System.ReadOnlySpan<T> span, T value0, T value1, T value2)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Boolean ContainsAnyExcept<T>(this System.Span<T> span, T value0, T value1, T value2)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Boolean ContainsAnyExcept<T>(this System.ReadOnlySpan<T> span, T value0, T value1, T value2, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `[ext] public static System.Boolean ContainsAnyExceptInRange<T>(this System.ReadOnlySpan<T> span, T lowInclusive, T highInclusive)`
- `where T : System.IComparable<T>`
- `[ext] public static System.Boolean ContainsAnyExceptInRange<T>(this System.Span<T> span, T lowInclusive, T highInclusive)`
- `where T : System.IComparable<T>`
- `[ext] public static System.Boolean ContainsAnyInRange<T>(this System.ReadOnlySpan<T> span, T lowInclusive, T highInclusive)`
- `where T : System.IComparable<T>`
- `[ext] public static System.Boolean ContainsAnyInRange<T>(this System.Span<T> span, T lowInclusive, T highInclusive)`
- `where T : System.IComparable<T>`
- `[ext] public static System.Void CopyTo<T>(this T[] source, System.Memory<T> destination)`
- `[ext] public static System.Void CopyTo<T>(this T[] source, System.Span<T> destination)`
- `[ext] public static System.Int32 Count<T>(this System.Span<T> span, T value)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 Count<T>(this System.ReadOnlySpan<T> span, T value)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 Count<T>(this System.Span<T> span, System.ReadOnlySpan<T> value)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 Count<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> value)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 Count<T>(this System.ReadOnlySpan<T> span, T value, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `[ext] public static System.Int32 Count<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> value, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `[ext] public static System.Int32 CountAny<T>(this System.ReadOnlySpan<T> span, System.Buffers.SearchValues<T> values)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 CountAny<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> values)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 CountAny<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> values, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `[ext] public static System.Boolean EndsWith<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> value)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Boolean EndsWith<T>(this System.Span<T> span, System.ReadOnlySpan<T> value)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Boolean EndsWith<T>(this System.ReadOnlySpan<T> span, T value)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Boolean EndsWith(this System.ReadOnlySpan<System.Char> span, System.ReadOnlySpan<System.Char> value, System.StringComparison comparisonType)`
- `[ext] public static System.Boolean EndsWith<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> value, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `[ext] public static System.Boolean EndsWith<T>(this System.ReadOnlySpan<T> span, T value, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `[ext] public static System.Text.SpanLineEnumerator EnumerateLines(this System.ReadOnlySpan<System.Char> span)`
- `[ext] public static System.Text.SpanLineEnumerator EnumerateLines(this System.Span<System.Char> span)`
- `[ext] public static System.Text.SpanRuneEnumerator EnumerateRunes(this System.ReadOnlySpan<System.Char> span)`
- `[ext] public static System.Text.SpanRuneEnumerator EnumerateRunes(this System.Span<System.Char> span)`
- `[ext] public static System.Boolean Equals(this System.ReadOnlySpan<System.Char> span, System.ReadOnlySpan<System.Char> other, System.StringComparison comparisonType)`
- `[ext] public static System.Int32 IndexOf<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> value)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 IndexOf<T>(this System.ReadOnlySpan<T> span, T value)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 IndexOf<T>(this System.Span<T> span, System.ReadOnlySpan<T> value)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 IndexOf<T>(this System.Span<T> span, T value)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 IndexOf(this System.ReadOnlySpan<System.Char> span, System.ReadOnlySpan<System.Char> value, System.StringComparison comparisonType)`
- `[ext] public static System.Int32 IndexOf<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> value, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `[ext] public static System.Int32 IndexOf<T>(this System.ReadOnlySpan<T> span, T value, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `[ext] public static System.Int32 IndexOfAny(this System.ReadOnlySpan<System.Char> span, System.Buffers.SearchValues<System.String> values)`
- `[ext] public static System.Int32 IndexOfAny(this System.Span<System.Char> span, System.Buffers.SearchValues<System.String> values)`
- `[ext] public static System.Int32 IndexOfAny<T>(this System.ReadOnlySpan<T> span, System.Buffers.SearchValues<T> values)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 IndexOfAny<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> values)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 IndexOfAny<T>(this System.Span<T> span, System.Buffers.SearchValues<T> values)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 IndexOfAny<T>(this System.Span<T> span, System.ReadOnlySpan<T> values)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 IndexOfAny<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> values, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `[ext] public static System.Int32 IndexOfAny<T>(this System.ReadOnlySpan<T> span, T value0, T value1)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 IndexOfAny<T>(this System.Span<T> span, T value0, T value1)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 IndexOfAny<T>(this System.ReadOnlySpan<T> span, T value0, T value1, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `[ext] public static System.Int32 IndexOfAny<T>(this System.ReadOnlySpan<T> span, T value0, T value1, T value2)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 IndexOfAny<T>(this System.Span<T> span, T value0, T value1, T value2)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 IndexOfAny<T>(this System.ReadOnlySpan<T> span, T value0, T value1, T value2, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `[ext] public static System.Int32 IndexOfAnyExcept<T>(this System.Span<T> span, T value)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 IndexOfAnyExcept<T>(this System.Span<T> span, System.Buffers.SearchValues<T> values)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 IndexOfAnyExcept<T>(this System.Span<T> span, System.ReadOnlySpan<T> values)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 IndexOfAnyExcept<T>(this System.ReadOnlySpan<T> span, T value)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 IndexOfAnyExcept<T>(this System.ReadOnlySpan<T> span, System.Buffers.SearchValues<T> values)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 IndexOfAnyExcept<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> values)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 IndexOfAnyExcept<T>(this System.Span<T> span, T value0, T value1)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 IndexOfAnyExcept<T>(this System.ReadOnlySpan<T> span, T value, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `[ext] public static System.Int32 IndexOfAnyExcept<T>(this System.ReadOnlySpan<T> span, T value0, T value1)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 IndexOfAnyExcept<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> values, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `[ext] public static System.Int32 IndexOfAnyExcept<T>(this System.Span<T> span, T value0, T value1, T value2)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 IndexOfAnyExcept<T>(this System.ReadOnlySpan<T> span, T value0, T value1, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `[ext] public static System.Int32 IndexOfAnyExcept<T>(this System.ReadOnlySpan<T> span, T value0, T value1, T value2)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 IndexOfAnyExcept<T>(this System.ReadOnlySpan<T> span, T value0, T value1, T value2, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `[ext] public static System.Int32 IndexOfAnyExceptInRange<T>(this System.ReadOnlySpan<T> span, T lowInclusive, T highInclusive)`
- `where T : System.IComparable<T>`
- `[ext] public static System.Int32 IndexOfAnyExceptInRange<T>(this System.Span<T> span, T lowInclusive, T highInclusive)`
- `where T : System.IComparable<T>`
- `[ext] public static System.Int32 IndexOfAnyInRange<T>(this System.ReadOnlySpan<T> span, T lowInclusive, T highInclusive)`
- `where T : System.IComparable<T>`
- `[ext] public static System.Int32 IndexOfAnyInRange<T>(this System.Span<T> span, T lowInclusive, T highInclusive)`
- `where T : System.IComparable<T>`
- `[ext] public static System.Boolean IsWhiteSpace(this System.ReadOnlySpan<System.Char> span)`
- `[ext] public static System.Int32 LastIndexOf<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> value)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 LastIndexOf<T>(this System.ReadOnlySpan<T> span, T value)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 LastIndexOf<T>(this System.Span<T> span, System.ReadOnlySpan<T> value)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 LastIndexOf<T>(this System.Span<T> span, T value)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 LastIndexOf(this System.ReadOnlySpan<System.Char> span, System.ReadOnlySpan<System.Char> value, System.StringComparison comparisonType)`
- `[ext] public static System.Int32 LastIndexOf<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> value, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `[ext] public static System.Int32 LastIndexOf<T>(this System.ReadOnlySpan<T> span, T value, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `[ext] public static System.Int32 LastIndexOfAny<T>(this System.ReadOnlySpan<T> span, System.Buffers.SearchValues<T> values)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 LastIndexOfAny<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> values)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 LastIndexOfAny<T>(this System.Span<T> span, System.Buffers.SearchValues<T> values)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 LastIndexOfAny<T>(this System.Span<T> span, System.ReadOnlySpan<T> values)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 LastIndexOfAny<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> values, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `[ext] public static System.Int32 LastIndexOfAny<T>(this System.ReadOnlySpan<T> span, T value0, T value1)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 LastIndexOfAny<T>(this System.Span<T> span, T value0, T value1)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 LastIndexOfAny<T>(this System.ReadOnlySpan<T> span, T value0, T value1, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `[ext] public static System.Int32 LastIndexOfAny<T>(this System.ReadOnlySpan<T> span, T value0, T value1, T value2)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 LastIndexOfAny<T>(this System.Span<T> span, T value0, T value1, T value2)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 LastIndexOfAny<T>(this System.ReadOnlySpan<T> span, T value0, T value1, T value2, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `[ext] public static System.Int32 LastIndexOfAnyExcept<T>(this System.Span<T> span, T value)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 LastIndexOfAnyExcept<T>(this System.Span<T> span, System.Buffers.SearchValues<T> values)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 LastIndexOfAnyExcept<T>(this System.Span<T> span, System.ReadOnlySpan<T> values)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 LastIndexOfAnyExcept<T>(this System.ReadOnlySpan<T> span, T value)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 LastIndexOfAnyExcept<T>(this System.ReadOnlySpan<T> span, System.Buffers.SearchValues<T> values)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 LastIndexOfAnyExcept<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> values)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 LastIndexOfAnyExcept<T>(this System.Span<T> span, T value0, T value1)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 LastIndexOfAnyExcept<T>(this System.ReadOnlySpan<T> span, T value, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `[ext] public static System.Int32 LastIndexOfAnyExcept<T>(this System.ReadOnlySpan<T> span, T value0, T value1)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 LastIndexOfAnyExcept<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> values, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `[ext] public static System.Int32 LastIndexOfAnyExcept<T>(this System.Span<T> span, T value0, T value1, T value2)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 LastIndexOfAnyExcept<T>(this System.ReadOnlySpan<T> span, T value0, T value1, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `[ext] public static System.Int32 LastIndexOfAnyExcept<T>(this System.ReadOnlySpan<T> span, T value0, T value1, T value2)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 LastIndexOfAnyExcept<T>(this System.ReadOnlySpan<T> span, T value0, T value1, T value2, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `[ext] public static System.Int32 LastIndexOfAnyExceptInRange<T>(this System.ReadOnlySpan<T> span, T lowInclusive, T highInclusive)`
- `where T : System.IComparable<T>`
- `[ext] public static System.Int32 LastIndexOfAnyExceptInRange<T>(this System.Span<T> span, T lowInclusive, T highInclusive)`
- `where T : System.IComparable<T>`
- `[ext] public static System.Int32 LastIndexOfAnyInRange<T>(this System.ReadOnlySpan<T> span, T lowInclusive, T highInclusive)`
- `where T : System.IComparable<T>`
- `[ext] public static System.Int32 LastIndexOfAnyInRange<T>(this System.Span<T> span, T lowInclusive, T highInclusive)`
- `where T : System.IComparable<T>`
- `[ext] public static System.Boolean Overlaps<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> other)`
- `[ext] public static System.Boolean Overlaps<T>(this System.Span<T> span, System.ReadOnlySpan<T> other)`
- `[ext] public static System.Boolean Overlaps<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> other, out System.Int32& elementOffset)`
- `[ext] public static System.Boolean Overlaps<T>(this System.Span<T> span, System.ReadOnlySpan<T> other, out System.Int32& elementOffset)`
- `[ext] public static System.Void Replace<T>(this System.Span<T> span, T oldValue, T newValue)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Void Replace<T>(this System.Span<T> span, T oldValue, T newValue, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `[ext] public static System.Void Replace<T>(this System.ReadOnlySpan<T> source, System.Span<T> destination, T oldValue, T newValue)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Void Replace<T>(this System.ReadOnlySpan<T> source, System.Span<T> destination, T oldValue, T newValue, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `[ext] public static System.Void ReplaceAny<T>(this System.Span<T> span, System.Buffers.SearchValues<T> values, T newValue)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Void ReplaceAny<T>(this System.ReadOnlySpan<T> source, System.Span<T> destination, System.Buffers.SearchValues<T> values, T newValue)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Void ReplaceAnyExcept<T>(this System.Span<T> span, System.Buffers.SearchValues<T> values, T newValue)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Void ReplaceAnyExcept<T>(this System.ReadOnlySpan<T> source, System.Span<T> destination, System.Buffers.SearchValues<T> values, T newValue)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Void Reverse<T>(this System.Span<T> span)`
- `[ext] public static System.Int32 SequenceCompareTo<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> other)`
- `where T : System.IComparable<T>`
- `[ext] public static System.Int32 SequenceCompareTo<T>(this System.Span<T> span, System.ReadOnlySpan<T> other)`
- `where T : System.IComparable<T>`
- `[ext] public static System.Int32 SequenceCompareTo<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> other, System.Collections.Generic.IComparer<T> comparer)`
- `[ext] public static System.Boolean SequenceEqual<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> other)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Boolean SequenceEqual<T>(this System.Span<T> span, System.ReadOnlySpan<T> other)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Boolean SequenceEqual<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> other, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `[ext] public static System.Boolean SequenceEqual<T>(this System.Span<T> span, System.ReadOnlySpan<T> other, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `[ext] public static System.Void Sort<T>(this System.Span<T> span)`
- `[ext] public static System.Void Sort<T>(this System.Span<T> span, System.Comparison<T> comparison)`
- `[ext] public static System.Void Sort<TKey, TValue>(this System.Span<TKey> keys, System.Span<TValue> items)`
- `[ext] public static System.Void Sort<T, TComparer>(this System.Span<T> span, TComparer comparer)`
- `where TComparer : System.Collections.Generic.IComparer<T>`
- `[ext] public static System.Void Sort<TKey, TValue>(this System.Span<TKey> keys, System.Span<TValue> items, System.Comparison<TKey> comparison)`
- `[ext] public static System.Void Sort<TKey, TValue, TComparer>(this System.Span<TKey> keys, System.Span<TValue> items, TComparer comparer)`
- `where TComparer : System.Collections.Generic.IComparer<TKey>`
- `[ext] public static System.MemoryExtensions+SpanSplitEnumerator<T> Split<T>(this System.ReadOnlySpan<T> source, T separator)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.MemoryExtensions+SpanSplitEnumerator<T> Split<T>(this System.ReadOnlySpan<T> source, System.ReadOnlySpan<T> separator)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 Split(this System.ReadOnlySpan<System.Char> source, System.Span<System.Range> destination, System.Char separator, System.StringSplitOptions options)`
- `[ext] public static System.Int32 Split(this System.ReadOnlySpan<System.Char> source, System.Span<System.Range> destination, System.ReadOnlySpan<System.Char> separator, System.StringSplitOptions options)`
- `[ext] public static System.MemoryExtensions+SpanSplitEnumerator<T> SplitAny<T>(this System.ReadOnlySpan<T> source, System.ReadOnlySpan<T> separators)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.MemoryExtensions+SpanSplitEnumerator<T> SplitAny<T>(this System.ReadOnlySpan<T> source, System.Buffers.SearchValues<T> separators)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Int32 SplitAny(this System.ReadOnlySpan<System.Char> source, System.Span<System.Range> destination, System.ReadOnlySpan<System.Char> separators, System.StringSplitOptions options)`
- `[ext] public static System.Int32 SplitAny(this System.ReadOnlySpan<System.Char> source, System.Span<System.Range> destination, System.ReadOnlySpan<System.String> separators, System.StringSplitOptions options)`
- `[ext] public static System.Boolean StartsWith<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> value)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Boolean StartsWith<T>(this System.Span<T> span, System.ReadOnlySpan<T> value)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Boolean StartsWith<T>(this System.ReadOnlySpan<T> span, T value)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Boolean StartsWith(this System.ReadOnlySpan<System.Char> span, System.ReadOnlySpan<System.Char> value, System.StringComparison comparisonType)`
- `[ext] public static System.Boolean StartsWith<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> value, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `[ext] public static System.Boolean StartsWith<T>(this System.ReadOnlySpan<T> span, T value, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `[ext] public static System.Int32 ToLower(this System.ReadOnlySpan<System.Char> source, System.Span<System.Char> destination, System.Globalization.CultureInfo culture)`
- `[ext] public static System.Int32 ToLowerInvariant(this System.ReadOnlySpan<System.Char> source, System.Span<System.Char> destination)`
- `[ext] public static System.Int32 ToUpper(this System.ReadOnlySpan<System.Char> source, System.Span<System.Char> destination, System.Globalization.CultureInfo culture)`
- `[ext] public static System.Int32 ToUpperInvariant(this System.ReadOnlySpan<System.Char> source, System.Span<System.Char> destination)`
- `[ext] public static System.Memory<System.Char> Trim(this System.Memory<System.Char> memory)`
- `[ext] public static System.ReadOnlyMemory<System.Char> Trim(this System.ReadOnlyMemory<System.Char> memory)`
- `[ext] public static System.ReadOnlySpan<System.Char> Trim(this System.ReadOnlySpan<System.Char> span)`
- `[ext] public static System.Span<System.Char> Trim(this System.Span<System.Char> span)`
- `[ext] public static System.ReadOnlySpan<System.Char> Trim(this System.ReadOnlySpan<System.Char> span, System.Char trimChar)`
- `[ext] public static System.ReadOnlySpan<System.Char> Trim(this System.ReadOnlySpan<System.Char> span, System.ReadOnlySpan<System.Char> trimChars)`
- `[ext] public static System.Memory<T> Trim<T>(this System.Memory<T> memory, System.ReadOnlySpan<T> trimElements)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Memory<T> Trim<T>(this System.Memory<T> memory, T trimElement)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.ReadOnlyMemory<T> Trim<T>(this System.ReadOnlyMemory<T> memory, System.ReadOnlySpan<T> trimElements)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.ReadOnlyMemory<T> Trim<T>(this System.ReadOnlyMemory<T> memory, T trimElement)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.ReadOnlySpan<T> Trim<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> trimElements)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.ReadOnlySpan<T> Trim<T>(this System.ReadOnlySpan<T> span, T trimElement)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Span<T> Trim<T>(this System.Span<T> span, System.ReadOnlySpan<T> trimElements)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Span<T> Trim<T>(this System.Span<T> span, T trimElement)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Memory<System.Char> TrimEnd(this System.Memory<System.Char> memory)`
- `[ext] public static System.ReadOnlyMemory<System.Char> TrimEnd(this System.ReadOnlyMemory<System.Char> memory)`
- `[ext] public static System.ReadOnlySpan<System.Char> TrimEnd(this System.ReadOnlySpan<System.Char> span)`
- `[ext] public static System.Span<System.Char> TrimEnd(this System.Span<System.Char> span)`
- `[ext] public static System.ReadOnlySpan<System.Char> TrimEnd(this System.ReadOnlySpan<System.Char> span, System.Char trimChar)`
- `[ext] public static System.ReadOnlySpan<System.Char> TrimEnd(this System.ReadOnlySpan<System.Char> span, System.ReadOnlySpan<System.Char> trimChars)`
- `[ext] public static System.Memory<T> TrimEnd<T>(this System.Memory<T> memory, System.ReadOnlySpan<T> trimElements)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Memory<T> TrimEnd<T>(this System.Memory<T> memory, T trimElement)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.ReadOnlyMemory<T> TrimEnd<T>(this System.ReadOnlyMemory<T> memory, System.ReadOnlySpan<T> trimElements)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.ReadOnlyMemory<T> TrimEnd<T>(this System.ReadOnlyMemory<T> memory, T trimElement)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.ReadOnlySpan<T> TrimEnd<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> trimElements)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.ReadOnlySpan<T> TrimEnd<T>(this System.ReadOnlySpan<T> span, T trimElement)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Span<T> TrimEnd<T>(this System.Span<T> span, System.ReadOnlySpan<T> trimElements)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Span<T> TrimEnd<T>(this System.Span<T> span, T trimElement)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Memory<System.Char> TrimStart(this System.Memory<System.Char> memory)`
- `[ext] public static System.ReadOnlyMemory<System.Char> TrimStart(this System.ReadOnlyMemory<System.Char> memory)`
- `[ext] public static System.ReadOnlySpan<System.Char> TrimStart(this System.ReadOnlySpan<System.Char> span)`
- `[ext] public static System.Span<System.Char> TrimStart(this System.Span<System.Char> span)`
- `[ext] public static System.ReadOnlySpan<System.Char> TrimStart(this System.ReadOnlySpan<System.Char> span, System.Char trimChar)`
- `[ext] public static System.ReadOnlySpan<System.Char> TrimStart(this System.ReadOnlySpan<System.Char> span, System.ReadOnlySpan<System.Char> trimChars)`
- `[ext] public static System.Memory<T> TrimStart<T>(this System.Memory<T> memory, System.ReadOnlySpan<T> trimElements)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Memory<T> TrimStart<T>(this System.Memory<T> memory, T trimElement)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.ReadOnlyMemory<T> TrimStart<T>(this System.ReadOnlyMemory<T> memory, System.ReadOnlySpan<T> trimElements)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.ReadOnlyMemory<T> TrimStart<T>(this System.ReadOnlyMemory<T> memory, T trimElement)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.ReadOnlySpan<T> TrimStart<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> trimElements)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.ReadOnlySpan<T> TrimStart<T>(this System.ReadOnlySpan<T> span, T trimElement)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Span<T> TrimStart<T>(this System.Span<T> span, System.ReadOnlySpan<T> trimElements)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Span<T> TrimStart<T>(this System.Span<T> span, T trimElement)`
- `where T : System.IEquatable<T>`
- `[ext] public static System.Boolean TryWrite(this System.Span<System.Char> destination, ref System.MemoryExtensions+TryWriteInterpolatedStringHandler& handler, out System.Int32& charsWritten)`
- `[ext] public static System.Boolean TryWrite(this System.Span<System.Char> destination, System.IFormatProvider provider, ref System.MemoryExtensions+TryWriteInterpolatedStringHandler& handler, out System.Int32& charsWritten)`
- `[ext] public static System.Boolean TryWrite<TArg0>(this System.Span<System.Char> destination, System.IFormatProvider provider, System.Text.CompositeFormat format, out System.Int32& charsWritten, TArg0 arg0)`
- `[ext] public static System.Boolean TryWrite(this System.Span<System.Char> destination, System.IFormatProvider provider, System.Text.CompositeFormat format, out System.Int32& charsWritten, params System.Object[] args)`
- `[ext] public static System.Boolean TryWrite(this System.Span<System.Char> destination, System.IFormatProvider provider, System.Text.CompositeFormat format, out System.Int32& charsWritten, System.ReadOnlySpan<System.Object> args)`
- `[ext] public static System.Boolean TryWrite<TArg0, TArg1>(this System.Span<System.Char> destination, System.IFormatProvider provider, System.Text.CompositeFormat format, out System.Int32& charsWritten, TArg0 arg0, TArg1 arg1)`
- `[ext] public static System.Boolean TryWrite<TArg0, TArg1, TArg2>(this System.Span<System.Char> destination, System.IFormatProvider provider, System.Text.CompositeFormat format, out System.Int32& charsWritten, TArg0 arg0, TArg1 arg1, TArg2 arg2)`

### ReadOnlyMemory`1<T> (struct [readonly struct]) : System.IEquatable<System.ReadOnlyMemory<T>>

- `public ReadOnlyMemory`1(T[] array)`
- `public ReadOnlyMemory`1(T[] array, System.Int32 start, System.Int32 length)`
- `public static System.ReadOnlyMemory<T> Empty { get; }`
- `public System.Boolean IsEmpty { get; }`
- `public System.Int32 Length { get; }`
- `public System.ReadOnlySpan<T> Span { get; }`
- `public System.Void CopyTo(System.Memory<T> destination)`
- `public System.Boolean Equals(System.Object obj)`
- `public System.Boolean Equals(System.ReadOnlyMemory<T> other)`
- `public System.Int32 GetHashCode()`
- `public System.Buffers.MemoryHandle Pin()`
- `public System.ReadOnlyMemory<T> Slice(System.Int32 start)`
- `public System.ReadOnlyMemory<T> Slice(System.Int32 start, System.Int32 length)`
- `public T[] ToArray()`
- `public System.String ToString()`
- `public System.Boolean TryCopyTo(System.Memory<T> destination)`
- `public static System.ReadOnlyMemory<T> op_Implicit(System.ArraySegment<T> segment)`
- `public static System.ReadOnlyMemory<T> op_Implicit(T[] array)`

### ReadOnlySpan`1<T> (struct [readonly struct, ref struct])

- `public ReadOnlySpan`1(in T& reference)`
- `public ReadOnlySpan`1(T[] array)`
- `public ReadOnlySpan`1(System.Void* pointer, System.Int32 length)`
- `public ReadOnlySpan`1(T[] array, System.Int32 start, System.Int32 length)`
- `public static System.ReadOnlySpan<T> Empty { get; }`
- `public System.Boolean IsEmpty { get; }`
- `public T& Item(System.Int32 index) { get; }`
- `public System.Int32 Length { get; }`
- `public static System.ReadOnlySpan<T> CastUp<TDerived>(System.ReadOnlySpan<TDerived> items)`
- `where TDerived : class, T`
- `public System.Void CopyTo(System.Span<T> destination)`
- `public System.Boolean Equals(System.Object obj)`
- `public System.ReadOnlySpan<T> GetEnumerator()`
- `public System.Int32 GetHashCode()`
- `public T& GetPinnableReference()`
- `public System.ReadOnlySpan<T> Slice(System.Int32 start)`
- `public System.ReadOnlySpan<T> Slice(System.Int32 start, System.Int32 length)`
- `public T[] ToArray()`
- `public System.String ToString()`
- `public System.Boolean TryCopyTo(System.Span<T> destination)`
- `public static System.Boolean op_Equality(System.ReadOnlySpan<T> left, System.ReadOnlySpan<T> right)`
- `public static System.ReadOnlySpan<T> op_Implicit(System.ArraySegment<T> segment)`
- `public static System.ReadOnlySpan<T> op_Implicit(T[] array)`
- `public static System.Boolean op_Inequality(System.ReadOnlySpan<T> left, System.ReadOnlySpan<T> right)`

### SpanSplitEnumerator`1<T> (struct [ref struct]) : System.Collections.Generic.IEnumerator<System.Range>, System.Collections.IEnumerator, System.IDisposable

- `where T : System.IEquatable<T>`

- `public System.Range Current { get; }`
- `public System.ReadOnlySpan<T> Source { get; }`
- `public System.MemoryExtensions+SpanSplitEnumerator<T> GetEnumerator()`
- `public System.Boolean MoveNext()`

### Span`1<T> (struct [readonly struct, ref struct])

- `public Span`1(ref T& reference)`
- `public Span`1(T[] array)`
- `public Span`1(System.Void* pointer, System.Int32 length)`
- `public Span`1(T[] array, System.Int32 start, System.Int32 length)`
- `public static System.Span<T> Empty { get; }`
- `public System.Boolean IsEmpty { get; }`
- `public T& Item(System.Int32 index) { get; }`
- `public System.Int32 Length { get; }`
- `public System.Void Clear()`
- `public System.Void CopyTo(System.Span<T> destination)`
- `public System.Boolean Equals(System.Object obj)`
- `public System.Void Fill(T value)`
- `public System.Span<T> GetEnumerator()`
- `public System.Int32 GetHashCode()`
- `public T& GetPinnableReference()`
- `public System.Span<T> Slice(System.Int32 start)`
- `public System.Span<T> Slice(System.Int32 start, System.Int32 length)`
- `public T[] ToArray()`
- `public System.String ToString()`
- `public System.Boolean TryCopyTo(System.Span<T> destination)`
- `public static System.Boolean op_Equality(System.Span<T> left, System.Span<T> right)`
- `public static System.Span<T> op_Implicit(System.ArraySegment<T> segment)`
- `public static System.ReadOnlySpan<T> op_Implicit(System.Span<T> span)`
- `public static System.Span<T> op_Implicit(T[] array)`
- `public static System.Boolean op_Inequality(System.Span<T> left, System.Span<T> right)`

### TryWriteInterpolatedStringHandler (struct [ref struct])

- `public TryWriteInterpolatedStringHandler(System.Int32 literalLength, System.Int32 formattedCount, System.Span<System.Char> destination, out System.Boolean& shouldAppend)`
- `public TryWriteInterpolatedStringHandler(System.Int32 literalLength, System.Int32 formattedCount, System.Span<System.Char> destination, System.IFormatProvider provider, out System.Boolean& shouldAppend)`
- `public System.Boolean AppendFormatted(System.ReadOnlySpan<System.Char> value)`
- `public System.Boolean AppendFormatted<T>(T value)`
- `public System.Boolean AppendFormatted(System.String value)`
- `public System.Boolean AppendFormatted<T>(T value, System.String format)`
- `public System.Boolean AppendFormatted<T>(T value, System.Int32 alignment)`
- `public System.Boolean AppendFormatted(System.ReadOnlySpan<System.Char> value, System.Int32 alignment, System.String format)`
- `public System.Boolean AppendFormatted<T>(T value, System.Int32 alignment, System.String format)`
- `public System.Boolean AppendFormatted(System.Object value, System.Int32 alignment, System.String format)`
- `public System.Boolean AppendFormatted(System.String value, System.Int32 alignment, System.String format)`
- `public System.Boolean AppendLiteral(System.String value)`

## System.Buffers

### ArrayBufferWriter`1<T> (class [sealed]) : System.Buffers.IBufferWriter<T>

- `public ArrayBufferWriter`1()`
- `public ArrayBufferWriter`1(System.Int32 initialCapacity)`
- `public System.Int32 Capacity { get; }`
- `public System.Int32 FreeCapacity { get; }`
- `public System.Int32 WrittenCount { get; }`
- `public System.ReadOnlyMemory<T> WrittenMemory { get; }`
- `public System.ReadOnlySpan<T> WrittenSpan { get; }`
- `public System.Void Advance(System.Int32 count)`
- `public System.Void Clear()`
- `public System.Memory<T> GetMemory(System.Int32 sizeHint)`
- `public System.Span<T> GetSpan(System.Int32 sizeHint)`
- `public System.Void ResetWrittenCount()`

### ArrayPool`1<T> (class [abstract])

- `public static System.Buffers.ArrayPool<T> Shared { get; }`
- `public static System.Buffers.ArrayPool<T> Create()`
- `public static System.Buffers.ArrayPool<T> Create(System.Int32 maxArrayLength, System.Int32 maxArraysPerBucket)`
- `public T[] Rent(System.Int32 minimumLength)`
- `public System.Void Return(T[] array, System.Boolean clearArray)`

### BuffersExtensions (class [static])

- `[ext] public static System.Void CopyTo<T>(this in System.Buffers.ReadOnlySequence<T>& source, System.Span<T> destination)`
- `[ext] public static System.SequencePosition? PositionOf<T>(this in System.Buffers.ReadOnlySequence<T>& source, T value)`
- `where T : System.IEquatable<T>`
- `[ext] public static T[] ToArray<T>(this in System.Buffers.ReadOnlySequence<T>& sequence)`
- `[ext] public static System.Void Write<T>(this System.Buffers.IBufferWriter<T> writer, System.ReadOnlySpan<T> value)`

### Enumerator<T> (struct)

- `public Enumerator(in System.Buffers.ReadOnlySequence<T>& sequence)`
- `public System.ReadOnlyMemory<T> Current { get; }`
- `public System.Boolean MoveNext()`

### IBufferWriter`1<T> (interface)

- `public System.Void Advance(System.Int32 count)`
- `public System.Memory<T> GetMemory(System.Int32 sizeHint)`
- `public System.Span<T> GetSpan(System.Int32 sizeHint)`

### IMemoryOwner`1<T> (interface) : System.IDisposable

- `public System.Memory<T> Memory { get; }`

### IPinnable (interface)

- `public System.Buffers.MemoryHandle Pin(System.Int32 elementIndex)`
- `public System.Void Unpin()`

### MemoryHandle (struct) : System.IDisposable

- `public MemoryHandle(System.Void* pointer, System.Runtime.InteropServices.GCHandle handle, System.Buffers.IPinnable pinnable)`
- `public System.Void* Pointer { get; }`
- `public System.Void Dispose()`

### MemoryManager`1<T> (class [abstract]) : System.Buffers.IMemoryOwner<T>, System.IDisposable, System.Buffers.IPinnable

- `public System.Memory<T> Memory { get; }`
- `public System.Span<T> GetSpan()`
- `public System.Buffers.MemoryHandle Pin(System.Int32 elementIndex)`
- `public System.Void Unpin()`

### MemoryPool`1<T> (class [abstract]) : System.IDisposable

- `public System.Int32 MaxBufferSize { get; }`
- `public static System.Buffers.MemoryPool<T> Shared { get; }`
- `public System.Void Dispose()`
- `public System.Buffers.IMemoryOwner<T> Rent(System.Int32 minBufferSize)`

### OperationStatus (enum) : System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable

- `Done = 0`
- `DestinationTooSmall = 1`
- `NeedMoreData = 2`
- `InvalidData = 3`

### ReadOnlySequenceSegment`1<T> (class [abstract])

- `public System.ReadOnlyMemory<T> Memory { get; }`
- `public System.Buffers.ReadOnlySequenceSegment<T> Next { get; }`
- `public System.Int64 RunningIndex { get; }`

### ReadOnlySequence`1<T> (struct [readonly struct])

- `public ReadOnlySequence`1(System.ReadOnlyMemory<T> memory)`
- `public ReadOnlySequence`1(T[] array)`
- `public ReadOnlySequence`1(T[] array, System.Int32 start, System.Int32 length)`
- `public ReadOnlySequence`1(System.Buffers.ReadOnlySequenceSegment<T> startSegment, System.Int32 startIndex, System.Buffers.ReadOnlySequenceSegment<T> endSegment, System.Int32 endIndex)`
- `public static System.Buffers.ReadOnlySequence<T> Empty`
- `public System.SequencePosition End { get; }`
- `public System.ReadOnlyMemory<T> First { get; }`
- `public System.ReadOnlySpan<T> FirstSpan { get; }`
- `public System.Boolean IsEmpty { get; }`
- `public System.Boolean IsSingleSegment { get; }`
- `public System.Int64 Length { get; }`
- `public System.SequencePosition Start { get; }`
- `public System.Buffers.ReadOnlySequence<T> GetEnumerator()`
- `public System.Int64 GetOffset(System.SequencePosition position)`
- `public System.SequencePosition GetPosition(System.Int64 offset)`
- `public System.SequencePosition GetPosition(System.Int64 offset, System.SequencePosition origin)`
- `public System.Buffers.ReadOnlySequence<T> Slice(System.Int64 start)`
- `public System.Buffers.ReadOnlySequence<T> Slice(System.SequencePosition start)`
- `public System.Buffers.ReadOnlySequence<T> Slice(System.Int32 start, System.Int32 length)`
- `public System.Buffers.ReadOnlySequence<T> Slice(System.Int32 start, System.SequencePosition end)`
- `public System.Buffers.ReadOnlySequence<T> Slice(System.Int64 start, System.Int64 length)`
- `public System.Buffers.ReadOnlySequence<T> Slice(System.Int64 start, System.SequencePosition end)`
- `public System.Buffers.ReadOnlySequence<T> Slice(System.SequencePosition start, System.Int32 length)`
- `public System.Buffers.ReadOnlySequence<T> Slice(System.SequencePosition start, System.Int64 length)`
- `public System.Buffers.ReadOnlySequence<T> Slice(System.SequencePosition start, System.SequencePosition end)`
- `public System.String ToString()`
- `public System.Boolean TryGet(ref System.SequencePosition& position, out System.ReadOnlyMemory<T>& memory, System.Boolean advance)`

### ReadOnlySpanAction`2<T, TArg> (delegate [sealed]) : System.ICloneable, System.Runtime.Serialization.ISerializable

- `public ReadOnlySpanAction`2(System.Object object, System.IntPtr method)`
- `delegate System.Void Invoke(System.ReadOnlySpan<T> span, TArg arg)`
- `public System.IAsyncResult BeginInvoke(System.ReadOnlySpan<T> span, TArg arg, System.AsyncCallback callback, System.Object object)`
- `public System.Void EndInvoke(System.IAsyncResult result)`

### SearchValues (class [static])

- `public static System.Buffers.SearchValues<System.Byte> Create(System.ReadOnlySpan<System.Byte> values)`
- `public static System.Buffers.SearchValues<System.Char> Create(System.ReadOnlySpan<System.Char> values)`
- `public static System.Buffers.SearchValues<System.String> Create(System.ReadOnlySpan<System.String> values, System.StringComparison comparisonType)`

### SearchValues`1<T> (class)

- `where T : System.IEquatable<T>`

- `public System.Boolean Contains(T value)`

### SequenceReaderExtensions (class [static])

- `[ext] public static System.Boolean TryReadBigEndian(this ref System.Buffers.SequenceReader<System.Byte>& reader, out System.Int16& value)`
- `[ext] public static System.Boolean TryReadBigEndian(this ref System.Buffers.SequenceReader<System.Byte>& reader, out System.Int32& value)`
- `[ext] public static System.Boolean TryReadBigEndian(this ref System.Buffers.SequenceReader<System.Byte>& reader, out System.Int64& value)`
- `[ext] public static System.Boolean TryReadLittleEndian(this ref System.Buffers.SequenceReader<System.Byte>& reader, out System.Int16& value)`
- `[ext] public static System.Boolean TryReadLittleEndian(this ref System.Buffers.SequenceReader<System.Byte>& reader, out System.Int32& value)`
- `[ext] public static System.Boolean TryReadLittleEndian(this ref System.Buffers.SequenceReader<System.Byte>& reader, out System.Int64& value)`

### SequenceReader`1<T> (struct [ref struct])

- `where T : struct, System.IEquatable<T>`

- `public SequenceReader`1(System.Buffers.ReadOnlySequence<T> sequence)`
- `public System.Int64 Consumed { get; }`
- `public System.ReadOnlySpan<T> CurrentSpan { get; }`
- `public System.Int32 CurrentSpanIndex { get; }`
- `public System.Boolean End { get; }`
- `public System.Int64 Length { get; }`
- `public System.SequencePosition Position { get; }`
- `public System.Int64 Remaining { get; }`
- `public System.Buffers.ReadOnlySequence<T> Sequence { get; }`
- `public System.Buffers.ReadOnlySequence<T> UnreadSequence { get; }`
- `public System.ReadOnlySpan<T> UnreadSpan { get; }`
- `public System.Void Advance(System.Int64 count)`
- `public System.Int64 AdvancePast(T value)`
- `public System.Int64 AdvancePastAny(System.ReadOnlySpan<T> values)`
- `public System.Int64 AdvancePastAny(T value0, T value1)`
- `public System.Int64 AdvancePastAny(T value0, T value1, T value2)`
- `public System.Int64 AdvancePastAny(T value0, T value1, T value2, T value3)`
- `public System.Void AdvanceToEnd()`
- `public System.Boolean IsNext(System.ReadOnlySpan<T> next, System.Boolean advancePast)`
- `public System.Boolean IsNext(T next, System.Boolean advancePast)`
- `public System.Void Rewind(System.Int64 count)`
- `public System.Boolean TryAdvanceTo(T delimiter, System.Boolean advancePastDelimiter)`
- `public System.Boolean TryAdvanceToAny(System.ReadOnlySpan<T> delimiters, System.Boolean advancePastDelimiter)`
- `public System.Boolean TryCopyTo(System.Span<T> destination)`
- `public System.Boolean TryPeek(out T& value)`
- `public System.Boolean TryPeek(System.Int64 offset, out T& value)`
- `public System.Boolean TryRead(out T& value)`
- `public System.Boolean TryReadExact(System.Int32 count, out System.Buffers.ReadOnlySequence<T>& sequence)`
- `public System.Boolean TryReadTo(out System.Buffers.ReadOnlySequence<T>& sequence, System.ReadOnlySpan<T> delimiter, System.Boolean advancePastDelimiter)`
- `public System.Boolean TryReadTo(out System.Buffers.ReadOnlySequence<T>& sequence, T delimiter, System.Boolean advancePastDelimiter)`
- `public System.Boolean TryReadTo(out System.ReadOnlySpan<T>& span, System.ReadOnlySpan<T> delimiter, System.Boolean advancePastDelimiter)`
- `public System.Boolean TryReadTo(out System.ReadOnlySpan<T>& span, T delimiter, System.Boolean advancePastDelimiter)`
- `public System.Boolean TryReadTo(out System.Buffers.ReadOnlySequence<T>& sequence, T delimiter, T delimiterEscape, System.Boolean advancePastDelimiter)`
- `public System.Boolean TryReadTo(out System.ReadOnlySpan<T>& span, T delimiter, T delimiterEscape, System.Boolean advancePastDelimiter)`
- `public System.Boolean TryReadToAny(out System.Buffers.ReadOnlySequence<T>& sequence, System.ReadOnlySpan<T> delimiters, System.Boolean advancePastDelimiter)`
- `public System.Boolean TryReadToAny(out System.ReadOnlySpan<T>& span, System.ReadOnlySpan<T> delimiters, System.Boolean advancePastDelimiter)`

### SpanAction`2<T, TArg> (delegate [sealed]) : System.ICloneable, System.Runtime.Serialization.ISerializable

- `public SpanAction`2(System.Object object, System.IntPtr method)`
- `delegate System.Void Invoke(System.Span<T> span, TArg arg)`
- `public System.IAsyncResult BeginInvoke(System.Span<T> span, TArg arg, System.AsyncCallback callback, System.Object object)`
- `public System.Void EndInvoke(System.IAsyncResult result)`

### StandardFormat (struct [readonly struct]) : System.IEquatable<System.Buffers.StandardFormat>

- `public StandardFormat(System.Char symbol, System.Byte precision)`
- `public const System.Byte MaxPrecision`
- `public const System.Byte NoPrecision`
- `public System.Boolean HasPrecision { get; }`
- `public System.Boolean IsDefault { get; }`
- `public System.Byte Precision { get; }`
- `public System.Char Symbol { get; }`
- `public System.Boolean Equals(System.Buffers.StandardFormat other)`
- `public System.Boolean Equals(System.Object obj)`
- `public System.Int32 GetHashCode()`
- `public static System.Buffers.StandardFormat Parse(System.ReadOnlySpan<System.Char> format)`
- `public static System.Buffers.StandardFormat Parse(System.String format)`
- `public System.String ToString()`
- `public static System.Boolean TryParse(System.ReadOnlySpan<System.Char> format, out System.Buffers.StandardFormat& result)`
- `public static System.Boolean op_Equality(System.Buffers.StandardFormat left, System.Buffers.StandardFormat right)`
- `public static System.Buffers.StandardFormat op_Implicit(System.Char symbol)`
- `public static System.Boolean op_Inequality(System.Buffers.StandardFormat left, System.Buffers.StandardFormat right)`

## System.Buffers.Binary

### BinaryPrimitives (class [static])

- `public static System.Double ReadDoubleBigEndian(System.ReadOnlySpan<System.Byte> source)`
- `public static System.Double ReadDoubleLittleEndian(System.ReadOnlySpan<System.Byte> source)`
- `public static System.Half ReadHalfBigEndian(System.ReadOnlySpan<System.Byte> source)`
- `public static System.Half ReadHalfLittleEndian(System.ReadOnlySpan<System.Byte> source)`
- `public static System.Int128 ReadInt128BigEndian(System.ReadOnlySpan<System.Byte> source)`
- `public static System.Int128 ReadInt128LittleEndian(System.ReadOnlySpan<System.Byte> source)`
- `public static System.Int16 ReadInt16BigEndian(System.ReadOnlySpan<System.Byte> source)`
- `public static System.Int16 ReadInt16LittleEndian(System.ReadOnlySpan<System.Byte> source)`
- `public static System.Int32 ReadInt32BigEndian(System.ReadOnlySpan<System.Byte> source)`
- `public static System.Int32 ReadInt32LittleEndian(System.ReadOnlySpan<System.Byte> source)`
- `public static System.Int64 ReadInt64BigEndian(System.ReadOnlySpan<System.Byte> source)`
- `public static System.Int64 ReadInt64LittleEndian(System.ReadOnlySpan<System.Byte> source)`
- `public static System.IntPtr ReadIntPtrBigEndian(System.ReadOnlySpan<System.Byte> source)`
- `public static System.IntPtr ReadIntPtrLittleEndian(System.ReadOnlySpan<System.Byte> source)`
- `public static System.Single ReadSingleBigEndian(System.ReadOnlySpan<System.Byte> source)`
- `public static System.Single ReadSingleLittleEndian(System.ReadOnlySpan<System.Byte> source)`
- `public static System.UInt128 ReadUInt128BigEndian(System.ReadOnlySpan<System.Byte> source)`
- `public static System.UInt128 ReadUInt128LittleEndian(System.ReadOnlySpan<System.Byte> source)`
- `public static System.UInt16 ReadUInt16BigEndian(System.ReadOnlySpan<System.Byte> source)`
- `public static System.UInt16 ReadUInt16LittleEndian(System.ReadOnlySpan<System.Byte> source)`
- `public static System.UInt32 ReadUInt32BigEndian(System.ReadOnlySpan<System.Byte> source)`
- `public static System.UInt32 ReadUInt32LittleEndian(System.ReadOnlySpan<System.Byte> source)`
- `public static System.UInt64 ReadUInt64BigEndian(System.ReadOnlySpan<System.Byte> source)`
- `public static System.UInt64 ReadUInt64LittleEndian(System.ReadOnlySpan<System.Byte> source)`
- `public static System.UIntPtr ReadUIntPtrBigEndian(System.ReadOnlySpan<System.Byte> source)`
- `public static System.UIntPtr ReadUIntPtrLittleEndian(System.ReadOnlySpan<System.Byte> source)`
- `public static System.Byte ReverseEndianness(System.Byte value)`
- `public static System.Int16 ReverseEndianness(System.Int16 value)`
- `public static System.Int32 ReverseEndianness(System.Int32 value)`
- `public static System.Int64 ReverseEndianness(System.Int64 value)`
- `public static System.SByte ReverseEndianness(System.SByte value)`
- `public static System.UInt16 ReverseEndianness(System.UInt16 value)`
- `public static System.UInt32 ReverseEndianness(System.UInt32 value)`
- `public static System.UInt64 ReverseEndianness(System.UInt64 value)`
- `public static System.IntPtr ReverseEndianness(System.IntPtr value)`
- `public static System.UIntPtr ReverseEndianness(System.UIntPtr value)`
- `public static System.Int128 ReverseEndianness(System.Int128 value)`
- `public static System.UInt128 ReverseEndianness(System.UInt128 value)`
- `public static System.Void ReverseEndianness(System.ReadOnlySpan<System.Int32> source, System.Span<System.Int32> destination)`
- `public static System.Void ReverseEndianness(System.ReadOnlySpan<System.Int128> source, System.Span<System.Int128> destination)`
- `public static System.Void ReverseEndianness(System.ReadOnlySpan<System.Int64> source, System.Span<System.Int64> destination)`
- `public static System.Void ReverseEndianness(System.ReadOnlySpan<System.IntPtr> source, System.Span<System.IntPtr> destination)`
- `public static System.Void ReverseEndianness(System.ReadOnlySpan<System.Int16> source, System.Span<System.Int16> destination)`
- `public static System.Void ReverseEndianness(System.ReadOnlySpan<System.UIntPtr> source, System.Span<System.UIntPtr> destination)`
- `public static System.Void ReverseEndianness(System.ReadOnlySpan<System.UInt32> source, System.Span<System.UInt32> destination)`
- `public static System.Void ReverseEndianness(System.ReadOnlySpan<System.UInt128> source, System.Span<System.UInt128> destination)`
- `public static System.Void ReverseEndianness(System.ReadOnlySpan<System.UInt64> source, System.Span<System.UInt64> destination)`
- `public static System.Void ReverseEndianness(System.ReadOnlySpan<System.UInt16> source, System.Span<System.UInt16> destination)`
- `public static System.Boolean TryReadDoubleBigEndian(System.ReadOnlySpan<System.Byte> source, out System.Double& value)`
- `public static System.Boolean TryReadDoubleLittleEndian(System.ReadOnlySpan<System.Byte> source, out System.Double& value)`
- `public static System.Boolean TryReadHalfBigEndian(System.ReadOnlySpan<System.Byte> source, out System.Half& value)`
- `public static System.Boolean TryReadHalfLittleEndian(System.ReadOnlySpan<System.Byte> source, out System.Half& value)`
- `public static System.Boolean TryReadInt128BigEndian(System.ReadOnlySpan<System.Byte> source, out System.Int128& value)`
- `public static System.Boolean TryReadInt128LittleEndian(System.ReadOnlySpan<System.Byte> source, out System.Int128& value)`
- `public static System.Boolean TryReadInt16BigEndian(System.ReadOnlySpan<System.Byte> source, out System.Int16& value)`
- `public static System.Boolean TryReadInt16LittleEndian(System.ReadOnlySpan<System.Byte> source, out System.Int16& value)`
- `public static System.Boolean TryReadInt32BigEndian(System.ReadOnlySpan<System.Byte> source, out System.Int32& value)`
- `public static System.Boolean TryReadInt32LittleEndian(System.ReadOnlySpan<System.Byte> source, out System.Int32& value)`
- `public static System.Boolean TryReadInt64BigEndian(System.ReadOnlySpan<System.Byte> source, out System.Int64& value)`
- `public static System.Boolean TryReadInt64LittleEndian(System.ReadOnlySpan<System.Byte> source, out System.Int64& value)`
- `public static System.Boolean TryReadIntPtrBigEndian(System.ReadOnlySpan<System.Byte> source, out System.IntPtr& value)`
- `public static System.Boolean TryReadIntPtrLittleEndian(System.ReadOnlySpan<System.Byte> source, out System.IntPtr& value)`
- `public static System.Boolean TryReadSingleBigEndian(System.ReadOnlySpan<System.Byte> source, out System.Single& value)`
- `public static System.Boolean TryReadSingleLittleEndian(System.ReadOnlySpan<System.Byte> source, out System.Single& value)`
- `public static System.Boolean TryReadUInt128BigEndian(System.ReadOnlySpan<System.Byte> source, out System.UInt128& value)`
- `public static System.Boolean TryReadUInt128LittleEndian(System.ReadOnlySpan<System.Byte> source, out System.UInt128& value)`
- `public static System.Boolean TryReadUInt16BigEndian(System.ReadOnlySpan<System.Byte> source, out System.UInt16& value)`
- `public static System.Boolean TryReadUInt16LittleEndian(System.ReadOnlySpan<System.Byte> source, out System.UInt16& value)`
- `public static System.Boolean TryReadUInt32BigEndian(System.ReadOnlySpan<System.Byte> source, out System.UInt32& value)`
- `public static System.Boolean TryReadUInt32LittleEndian(System.ReadOnlySpan<System.Byte> source, out System.UInt32& value)`
- `public static System.Boolean TryReadUInt64BigEndian(System.ReadOnlySpan<System.Byte> source, out System.UInt64& value)`
- `public static System.Boolean TryReadUInt64LittleEndian(System.ReadOnlySpan<System.Byte> source, out System.UInt64& value)`
- `public static System.Boolean TryReadUIntPtrBigEndian(System.ReadOnlySpan<System.Byte> source, out System.UIntPtr& value)`
- `public static System.Boolean TryReadUIntPtrLittleEndian(System.ReadOnlySpan<System.Byte> source, out System.UIntPtr& value)`
- `public static System.Boolean TryWriteDoubleBigEndian(System.Span<System.Byte> destination, System.Double value)`
- `public static System.Boolean TryWriteDoubleLittleEndian(System.Span<System.Byte> destination, System.Double value)`
- `public static System.Boolean TryWriteHalfBigEndian(System.Span<System.Byte> destination, System.Half value)`
- `public static System.Boolean TryWriteHalfLittleEndian(System.Span<System.Byte> destination, System.Half value)`
- `public static System.Boolean TryWriteInt128BigEndian(System.Span<System.Byte> destination, System.Int128 value)`
- `public static System.Boolean TryWriteInt128LittleEndian(System.Span<System.Byte> destination, System.Int128 value)`
- `public static System.Boolean TryWriteInt16BigEndian(System.Span<System.Byte> destination, System.Int16 value)`
- `public static System.Boolean TryWriteInt16LittleEndian(System.Span<System.Byte> destination, System.Int16 value)`
- `public static System.Boolean TryWriteInt32BigEndian(System.Span<System.Byte> destination, System.Int32 value)`
- `public static System.Boolean TryWriteInt32LittleEndian(System.Span<System.Byte> destination, System.Int32 value)`
- `public static System.Boolean TryWriteInt64BigEndian(System.Span<System.Byte> destination, System.Int64 value)`
- `public static System.Boolean TryWriteInt64LittleEndian(System.Span<System.Byte> destination, System.Int64 value)`
- `public static System.Boolean TryWriteIntPtrBigEndian(System.Span<System.Byte> destination, System.IntPtr value)`
- `public static System.Boolean TryWriteIntPtrLittleEndian(System.Span<System.Byte> destination, System.IntPtr value)`
- `public static System.Boolean TryWriteSingleBigEndian(System.Span<System.Byte> destination, System.Single value)`
- `public static System.Boolean TryWriteSingleLittleEndian(System.Span<System.Byte> destination, System.Single value)`
- `public static System.Boolean TryWriteUInt128BigEndian(System.Span<System.Byte> destination, System.UInt128 value)`
- `public static System.Boolean TryWriteUInt128LittleEndian(System.Span<System.Byte> destination, System.UInt128 value)`
- `public static System.Boolean TryWriteUInt16BigEndian(System.Span<System.Byte> destination, System.UInt16 value)`
- `public static System.Boolean TryWriteUInt16LittleEndian(System.Span<System.Byte> destination, System.UInt16 value)`
- `public static System.Boolean TryWriteUInt32BigEndian(System.Span<System.Byte> destination, System.UInt32 value)`
- `public static System.Boolean TryWriteUInt32LittleEndian(System.Span<System.Byte> destination, System.UInt32 value)`
- `public static System.Boolean TryWriteUInt64BigEndian(System.Span<System.Byte> destination, System.UInt64 value)`
- `public static System.Boolean TryWriteUInt64LittleEndian(System.Span<System.Byte> destination, System.UInt64 value)`
- `public static System.Boolean TryWriteUIntPtrBigEndian(System.Span<System.Byte> destination, System.UIntPtr value)`
- `public static System.Boolean TryWriteUIntPtrLittleEndian(System.Span<System.Byte> destination, System.UIntPtr value)`
- `public static System.Void WriteDoubleBigEndian(System.Span<System.Byte> destination, System.Double value)`
- `public static System.Void WriteDoubleLittleEndian(System.Span<System.Byte> destination, System.Double value)`
- `public static System.Void WriteHalfBigEndian(System.Span<System.Byte> destination, System.Half value)`
- `public static System.Void WriteHalfLittleEndian(System.Span<System.Byte> destination, System.Half value)`
- `public static System.Void WriteInt128BigEndian(System.Span<System.Byte> destination, System.Int128 value)`
- `public static System.Void WriteInt128LittleEndian(System.Span<System.Byte> destination, System.Int128 value)`
- `public static System.Void WriteInt16BigEndian(System.Span<System.Byte> destination, System.Int16 value)`
- `public static System.Void WriteInt16LittleEndian(System.Span<System.Byte> destination, System.Int16 value)`
- `public static System.Void WriteInt32BigEndian(System.Span<System.Byte> destination, System.Int32 value)`
- `public static System.Void WriteInt32LittleEndian(System.Span<System.Byte> destination, System.Int32 value)`
- `public static System.Void WriteInt64BigEndian(System.Span<System.Byte> destination, System.Int64 value)`
- `public static System.Void WriteInt64LittleEndian(System.Span<System.Byte> destination, System.Int64 value)`
- `public static System.Void WriteIntPtrBigEndian(System.Span<System.Byte> destination, System.IntPtr value)`
- `public static System.Void WriteIntPtrLittleEndian(System.Span<System.Byte> destination, System.IntPtr value)`
- `public static System.Void WriteSingleBigEndian(System.Span<System.Byte> destination, System.Single value)`
- `public static System.Void WriteSingleLittleEndian(System.Span<System.Byte> destination, System.Single value)`
- `public static System.Void WriteUInt128BigEndian(System.Span<System.Byte> destination, System.UInt128 value)`
- `public static System.Void WriteUInt128LittleEndian(System.Span<System.Byte> destination, System.UInt128 value)`
- `public static System.Void WriteUInt16BigEndian(System.Span<System.Byte> destination, System.UInt16 value)`
- `public static System.Void WriteUInt16LittleEndian(System.Span<System.Byte> destination, System.UInt16 value)`
- `public static System.Void WriteUInt32BigEndian(System.Span<System.Byte> destination, System.UInt32 value)`
- `public static System.Void WriteUInt32LittleEndian(System.Span<System.Byte> destination, System.UInt32 value)`
- `public static System.Void WriteUInt64BigEndian(System.Span<System.Byte> destination, System.UInt64 value)`
- `public static System.Void WriteUInt64LittleEndian(System.Span<System.Byte> destination, System.UInt64 value)`
- `public static System.Void WriteUIntPtrBigEndian(System.Span<System.Byte> destination, System.UIntPtr value)`
- `public static System.Void WriteUIntPtrLittleEndian(System.Span<System.Byte> destination, System.UIntPtr value)`

## System.Buffers.Text

### Base64 (class [static])

- `public static System.Buffers.OperationStatus DecodeFromUtf8(System.ReadOnlySpan<System.Byte> utf8, System.Span<System.Byte> bytes, out System.Int32& bytesConsumed, out System.Int32& bytesWritten, System.Boolean isFinalBlock)`
- `public static System.Buffers.OperationStatus DecodeFromUtf8InPlace(System.Span<System.Byte> buffer, out System.Int32& bytesWritten)`
- `public static System.Buffers.OperationStatus EncodeToUtf8(System.ReadOnlySpan<System.Byte> bytes, System.Span<System.Byte> utf8, out System.Int32& bytesConsumed, out System.Int32& bytesWritten, System.Boolean isFinalBlock)`
- `public static System.Buffers.OperationStatus EncodeToUtf8InPlace(System.Span<System.Byte> buffer, System.Int32 dataLength, out System.Int32& bytesWritten)`
- `public static System.Int32 GetMaxDecodedFromUtf8Length(System.Int32 length)`
- `public static System.Int32 GetMaxEncodedToUtf8Length(System.Int32 length)`
- `public static System.Boolean IsValid(System.ReadOnlySpan<System.Byte> base64TextUtf8)`
- `public static System.Boolean IsValid(System.ReadOnlySpan<System.Char> base64Text)`
- `public static System.Boolean IsValid(System.ReadOnlySpan<System.Byte> base64TextUtf8, out System.Int32& decodedLength)`
- `public static System.Boolean IsValid(System.ReadOnlySpan<System.Char> base64Text, out System.Int32& decodedLength)`

### Base64Url (class [static])

- `public static System.Byte[] DecodeFromChars(System.ReadOnlySpan<System.Char> source)`
- `public static System.Int32 DecodeFromChars(System.ReadOnlySpan<System.Char> source, System.Span<System.Byte> destination)`
- `public static System.Buffers.OperationStatus DecodeFromChars(System.ReadOnlySpan<System.Char> source, System.Span<System.Byte> destination, out System.Int32& charsConsumed, out System.Int32& bytesWritten, System.Boolean isFinalBlock)`
- `public static System.Byte[] DecodeFromUtf8(System.ReadOnlySpan<System.Byte> source)`
- `public static System.Int32 DecodeFromUtf8(System.ReadOnlySpan<System.Byte> source, System.Span<System.Byte> destination)`
- `public static System.Buffers.OperationStatus DecodeFromUtf8(System.ReadOnlySpan<System.Byte> source, System.Span<System.Byte> destination, out System.Int32& bytesConsumed, out System.Int32& bytesWritten, System.Boolean isFinalBlock)`
- `public static System.Int32 DecodeFromUtf8InPlace(System.Span<System.Byte> buffer)`
- `public static System.Char[] EncodeToChars(System.ReadOnlySpan<System.Byte> source)`
- `public static System.Int32 EncodeToChars(System.ReadOnlySpan<System.Byte> source, System.Span<System.Char> destination)`
- `public static System.Buffers.OperationStatus EncodeToChars(System.ReadOnlySpan<System.Byte> source, System.Span<System.Char> destination, out System.Int32& bytesConsumed, out System.Int32& charsWritten, System.Boolean isFinalBlock)`
- `public static System.String EncodeToString(System.ReadOnlySpan<System.Byte> source)`
- `public static System.Byte[] EncodeToUtf8(System.ReadOnlySpan<System.Byte> source)`
- `public static System.Int32 EncodeToUtf8(System.ReadOnlySpan<System.Byte> source, System.Span<System.Byte> destination)`
- `public static System.Buffers.OperationStatus EncodeToUtf8(System.ReadOnlySpan<System.Byte> source, System.Span<System.Byte> destination, out System.Int32& bytesConsumed, out System.Int32& bytesWritten, System.Boolean isFinalBlock)`
- `public static System.Int32 GetEncodedLength(System.Int32 bytesLength)`
- `public static System.Int32 GetMaxDecodedLength(System.Int32 base64Length)`
- `public static System.Boolean IsValid(System.ReadOnlySpan<System.Byte> utf8Base64UrlText)`
- `public static System.Boolean IsValid(System.ReadOnlySpan<System.Char> base64UrlText)`
- `public static System.Boolean IsValid(System.ReadOnlySpan<System.Byte> utf8Base64UrlText, out System.Int32& decodedLength)`
- `public static System.Boolean IsValid(System.ReadOnlySpan<System.Char> base64UrlText, out System.Int32& decodedLength)`
- `public static System.Boolean TryDecodeFromChars(System.ReadOnlySpan<System.Char> source, System.Span<System.Byte> destination, out System.Int32& bytesWritten)`
- `public static System.Boolean TryDecodeFromUtf8(System.ReadOnlySpan<System.Byte> source, System.Span<System.Byte> destination, out System.Int32& bytesWritten)`
- `public static System.Boolean TryEncodeToChars(System.ReadOnlySpan<System.Byte> source, System.Span<System.Char> destination, out System.Int32& charsWritten)`
- `public static System.Boolean TryEncodeToUtf8(System.ReadOnlySpan<System.Byte> source, System.Span<System.Byte> destination, out System.Int32& bytesWritten)`
- `public static System.Boolean TryEncodeToUtf8InPlace(System.Span<System.Byte> buffer, System.Int32 dataLength, out System.Int32& bytesWritten)`

### Utf8Formatter (class [static])

- `public static System.Boolean TryFormat(System.Boolean value, System.Span<System.Byte> destination, out System.Int32& bytesWritten, System.Buffers.StandardFormat format)`
- `public static System.Boolean TryFormat(System.Byte value, System.Span<System.Byte> destination, out System.Int32& bytesWritten, System.Buffers.StandardFormat format)`
- `public static System.Boolean TryFormat(System.DateTime value, System.Span<System.Byte> destination, out System.Int32& bytesWritten, System.Buffers.StandardFormat format)`
- `public static System.Boolean TryFormat(System.DateTimeOffset value, System.Span<System.Byte> destination, out System.Int32& bytesWritten, System.Buffers.StandardFormat format)`
- `public static System.Boolean TryFormat(System.Decimal value, System.Span<System.Byte> destination, out System.Int32& bytesWritten, System.Buffers.StandardFormat format)`
- `public static System.Boolean TryFormat(System.Double value, System.Span<System.Byte> destination, out System.Int32& bytesWritten, System.Buffers.StandardFormat format)`
- `public static System.Boolean TryFormat(System.Guid value, System.Span<System.Byte> destination, out System.Int32& bytesWritten, System.Buffers.StandardFormat format)`
- `public static System.Boolean TryFormat(System.Int16 value, System.Span<System.Byte> destination, out System.Int32& bytesWritten, System.Buffers.StandardFormat format)`
- `public static System.Boolean TryFormat(System.Int32 value, System.Span<System.Byte> destination, out System.Int32& bytesWritten, System.Buffers.StandardFormat format)`
- `public static System.Boolean TryFormat(System.Int64 value, System.Span<System.Byte> destination, out System.Int32& bytesWritten, System.Buffers.StandardFormat format)`
- `public static System.Boolean TryFormat(System.SByte value, System.Span<System.Byte> destination, out System.Int32& bytesWritten, System.Buffers.StandardFormat format)`
- `public static System.Boolean TryFormat(System.Single value, System.Span<System.Byte> destination, out System.Int32& bytesWritten, System.Buffers.StandardFormat format)`
- `public static System.Boolean TryFormat(System.TimeSpan value, System.Span<System.Byte> destination, out System.Int32& bytesWritten, System.Buffers.StandardFormat format)`
- `public static System.Boolean TryFormat(System.UInt16 value, System.Span<System.Byte> destination, out System.Int32& bytesWritten, System.Buffers.StandardFormat format)`
- `public static System.Boolean TryFormat(System.UInt32 value, System.Span<System.Byte> destination, out System.Int32& bytesWritten, System.Buffers.StandardFormat format)`
- `public static System.Boolean TryFormat(System.UInt64 value, System.Span<System.Byte> destination, out System.Int32& bytesWritten, System.Buffers.StandardFormat format)`

### Utf8Parser (class [static])

- `public static System.Boolean TryParse(System.ReadOnlySpan<System.Byte> source, out System.Boolean& value, out System.Int32& bytesConsumed, System.Char standardFormat)`
- `public static System.Boolean TryParse(System.ReadOnlySpan<System.Byte> source, out System.Byte& value, out System.Int32& bytesConsumed, System.Char standardFormat)`
- `public static System.Boolean TryParse(System.ReadOnlySpan<System.Byte> source, out System.DateTime& value, out System.Int32& bytesConsumed, System.Char standardFormat)`
- `public static System.Boolean TryParse(System.ReadOnlySpan<System.Byte> source, out System.DateTimeOffset& value, out System.Int32& bytesConsumed, System.Char standardFormat)`
- `public static System.Boolean TryParse(System.ReadOnlySpan<System.Byte> source, out System.Decimal& value, out System.Int32& bytesConsumed, System.Char standardFormat)`
- `public static System.Boolean TryParse(System.ReadOnlySpan<System.Byte> source, out System.Double& value, out System.Int32& bytesConsumed, System.Char standardFormat)`
- `public static System.Boolean TryParse(System.ReadOnlySpan<System.Byte> source, out System.Guid& value, out System.Int32& bytesConsumed, System.Char standardFormat)`
- `public static System.Boolean TryParse(System.ReadOnlySpan<System.Byte> source, out System.Int16& value, out System.Int32& bytesConsumed, System.Char standardFormat)`
- `public static System.Boolean TryParse(System.ReadOnlySpan<System.Byte> source, out System.Int32& value, out System.Int32& bytesConsumed, System.Char standardFormat)`
- `public static System.Boolean TryParse(System.ReadOnlySpan<System.Byte> source, out System.Int64& value, out System.Int32& bytesConsumed, System.Char standardFormat)`
- `public static System.Boolean TryParse(System.ReadOnlySpan<System.Byte> source, out System.SByte& value, out System.Int32& bytesConsumed, System.Char standardFormat)`
- `public static System.Boolean TryParse(System.ReadOnlySpan<System.Byte> source, out System.Single& value, out System.Int32& bytesConsumed, System.Char standardFormat)`
- `public static System.Boolean TryParse(System.ReadOnlySpan<System.Byte> source, out System.TimeSpan& value, out System.Int32& bytesConsumed, System.Char standardFormat)`
- `public static System.Boolean TryParse(System.ReadOnlySpan<System.Byte> source, out System.UInt16& value, out System.Int32& bytesConsumed, System.Char standardFormat)`
- `public static System.Boolean TryParse(System.ReadOnlySpan<System.Byte> source, out System.UInt32& value, out System.Int32& bytesConsumed, System.Char standardFormat)`
- `public static System.Boolean TryParse(System.ReadOnlySpan<System.Byte> source, out System.UInt64& value, out System.Int32& bytesConsumed, System.Char standardFormat)`

## System.Collections.Generic

### AlternateLookup`1<TKey, TValue, TAlternateKey> (struct [readonly struct])

- `public System.Collections.Generic.Dictionary<TKey, TValue> Dictionary { get; }`
- `public TValue Item(TAlternateKey key) { get; set; }`
- `public System.Boolean ContainsKey(TAlternateKey key)`
- `public System.Boolean Remove(TAlternateKey key)`
- `public System.Boolean Remove(TAlternateKey key, out TKey& actualKey, out TValue& value)`
- `public System.Boolean TryAdd(TAlternateKey key, TValue value)`
- `public System.Boolean TryGetValue(TAlternateKey key, out TValue& value)`
- `public System.Boolean TryGetValue(TAlternateKey key, out TKey& actualKey, out TValue& value)`

### AlternateLookup`1<T, TAlternate> (struct [readonly struct])

- `public System.Collections.Generic.HashSet<T> Set { get; }`
- `public System.Boolean Add(TAlternate item)`
- `public System.Boolean Contains(TAlternate item)`
- `public System.Boolean Remove(TAlternate item)`
- `public System.Boolean TryGetValue(TAlternate equalValue, out T& actualValue)`

### Dictionary`2<TKey, TValue> (class) : System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.IEnumerable, System.Collections.Generic.IDictionary<TKey, TValue>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>, System.Collections.ICollection, System.Collections.IDictionary, System.Runtime.Serialization.IDeserializationCallback, System.Runtime.Serialization.ISerializable

- `public Dictionary`2()`
- `public Dictionary`2(System.Collections.Generic.IDictionary<TKey, TValue> dictionary)`
- `public Dictionary`2(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> collection)`
- `public Dictionary`2(System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `public Dictionary`2(System.Int32 capacity)`
- `public Dictionary`2(System.Collections.Generic.IDictionary<TKey, TValue> dictionary, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `public Dictionary`2(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> collection, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `public Dictionary`2(System.Int32 capacity, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `public System.Int32 Capacity { get; }`
- `public System.Collections.Generic.IEqualityComparer<TKey> Comparer { get; }`
- `public System.Int32 Count { get; }`
- `public TValue Item(TKey key) { get; set; }`
- `public System.Collections.Generic.Dictionary<TKey, TValue> Keys { get; }`
- `public System.Collections.Generic.Dictionary<TKey, TValue> Values { get; }`
- `public System.Void Add(TKey key, TValue value)`
- `public System.Void Clear()`
- `public System.Boolean ContainsKey(TKey key)`
- `public System.Boolean ContainsValue(TValue value)`
- `public System.Int32 EnsureCapacity(System.Int32 capacity)`
- `public System.Collections.Generic.Dictionary<TKey, TValue, TAlternateKey> GetAlternateLookup<TAlternateKey>()`
- `public System.Collections.Generic.Dictionary<TKey, TValue> GetEnumerator()`
- `public System.Void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)`
- `public System.Void OnDeserialization(System.Object sender)`
- `public System.Boolean Remove(TKey key)`
- `public System.Boolean Remove(TKey key, out TValue& value)`
- `public System.Void TrimExcess()`
- `public System.Void TrimExcess(System.Int32 capacity)`
- `public System.Boolean TryAdd(TKey key, TValue value)`
- `public System.Boolean TryGetAlternateLookup<TAlternateKey>(out System.Collections.Generic.Dictionary<TKey, TValue, TAlternateKey>& lookup)`
- `public System.Boolean TryGetValue(TKey key, out TValue& value)`

### Enumerator<TKey, TValue> (struct) : System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.IEnumerator, System.IDisposable, System.Collections.IDictionaryEnumerator

- `public System.Collections.Generic.KeyValuePair<TKey, TValue> Current { get; }`
- `public System.Void Dispose()`
- `public System.Boolean MoveNext()`

### Enumerator<T> (struct) : System.Collections.Generic.IEnumerator<T>, System.Collections.IEnumerator, System.IDisposable, System.Runtime.Serialization.IDeserializationCallback, System.Runtime.Serialization.ISerializable

- `public T Current { get; }`
- `public System.Void Dispose()`
- `public System.Boolean MoveNext()`

### Enumerator<TKey, TValue> (struct) : System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.IEnumerator, System.IDisposable, System.Collections.IDictionaryEnumerator

- `public System.Collections.Generic.KeyValuePair<TKey, TValue> Current { get; }`
- `public System.Void Dispose()`
- `public System.Boolean MoveNext()`

### Enumerator<T> (struct) : System.Collections.Generic.IEnumerator<T>, System.Collections.IEnumerator, System.IDisposable

- `public T Current { get; }`
- `public System.Void Dispose()`
- `public System.Boolean MoveNext()`

### Enumerator<T> (struct) : System.Collections.Generic.IEnumerator<T>, System.Collections.IEnumerator, System.IDisposable

- `public T Current { get; }`
- `public System.Void Dispose()`
- `public System.Boolean MoveNext()`

### Enumerator<TKey, TValue> (struct) : System.Collections.Generic.IEnumerator<TKey>, System.Collections.IEnumerator, System.IDisposable

- `public TKey Current { get; }`
- `public System.Void Dispose()`
- `public System.Boolean MoveNext()`

### Enumerator<TKey, TValue> (struct) : System.Collections.Generic.IEnumerator<TValue>, System.Collections.IEnumerator, System.IDisposable

- `public TValue Current { get; }`
- `public System.Void Dispose()`
- `public System.Boolean MoveNext()`

### Enumerator<TKey, TValue> (struct) : System.Collections.Generic.IEnumerator<TKey>, System.Collections.IEnumerator, System.IDisposable

- `public TKey Current { get; }`
- `public System.Void Dispose()`
- `public System.Boolean MoveNext()`

### Enumerator<TKey, TValue> (struct) : System.Collections.Generic.IEnumerator<TValue>, System.Collections.IEnumerator, System.IDisposable

- `public TValue Current { get; }`
- `public System.Void Dispose()`
- `public System.Boolean MoveNext()`

### HashSet`1<T> (class) : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.ISet<T>, System.Collections.Generic.IReadOnlySet<T>, System.Runtime.Serialization.IDeserializationCallback, System.Runtime.Serialization.ISerializable

- `public HashSet`1()`
- `public HashSet`1(System.Collections.Generic.IEnumerable<T> collection)`
- `public HashSet`1(System.Collections.Generic.IEqualityComparer<T> comparer)`
- `public HashSet`1(System.Int32 capacity)`
- `public HashSet`1(System.Collections.Generic.IEnumerable<T> collection, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `public HashSet`1(System.Int32 capacity, System.Collections.Generic.IEqualityComparer<T> comparer)`
- `public System.Int32 Capacity { get; }`
- `public System.Collections.Generic.IEqualityComparer<T> Comparer { get; }`
- `public System.Int32 Count { get; }`
- `public System.Boolean Add(T item)`
- `public System.Void Clear()`
- `public System.Boolean Contains(T item)`
- `public System.Void CopyTo(T[] array)`
- `public System.Void CopyTo(T[] array, System.Int32 arrayIndex)`
- `public System.Void CopyTo(T[] array, System.Int32 arrayIndex, System.Int32 count)`
- `public static System.Collections.Generic.IEqualityComparer<System.Collections.Generic.HashSet<T>> CreateSetComparer()`
- `public System.Int32 EnsureCapacity(System.Int32 capacity)`
- `public System.Void ExceptWith(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Collections.Generic.HashSet<T, TAlternate> GetAlternateLookup<TAlternate>()`
- `public System.Collections.Generic.HashSet<T> GetEnumerator()`
- `public System.Void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)`
- `public System.Void IntersectWith(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean IsProperSubsetOf(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean IsProperSupersetOf(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean IsSubsetOf(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean IsSupersetOf(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Void OnDeserialization(System.Object sender)`
- `public System.Boolean Overlaps(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean Remove(T item)`
- `public System.Int32 RemoveWhere(System.Predicate<T> match)`
- `public System.Boolean SetEquals(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Void SymmetricExceptWith(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Void TrimExcess()`
- `public System.Void TrimExcess(System.Int32 capacity)`
- `public System.Boolean TryGetAlternateLookup<TAlternate>(out System.Collections.Generic.HashSet<T, TAlternate>& lookup)`
- `public System.Boolean TryGetValue(T equalValue, out T& actualValue)`
- `public System.Void UnionWith(System.Collections.Generic.IEnumerable<T> other)`

### IAsyncEnumerable`1<T> (interface)

- `public System.Collections.Generic.IAsyncEnumerator<T> GetAsyncEnumerator(System.Threading.CancellationToken cancellationToken)`

### IAsyncEnumerator`1<T> (interface) : System.IAsyncDisposable

- `public T Current { get; }`
- `public System.Threading.Tasks.ValueTask<System.Boolean> MoveNextAsync()`

### IDictionary`2<TKey, TValue> (interface) : System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.IEnumerable

- `public TValue Item(TKey key) { get; set; }`
- `public System.Collections.Generic.ICollection<TKey> Keys { get; }`
- `public System.Collections.Generic.ICollection<TValue> Values { get; }`
- `public System.Void Add(TKey key, TValue value)`
- `public System.Boolean ContainsKey(TKey key)`
- `public System.Boolean Remove(TKey key)`
- `public System.Boolean TryGetValue(TKey key, out TValue& value)`

### IEnumerable`1<T> (interface) : System.Collections.IEnumerable

- `public System.Collections.Generic.IEnumerator<T> GetEnumerator()`

### IEnumerator`1<T> (interface) : System.Collections.IEnumerator, System.IDisposable

- `public T Current { get; }`

### IEqualityComparer`1<T> (interface)

- `public System.Boolean Equals(T x, T y)`
- `public System.Int32 GetHashCode(T obj)`

### IList`1<T> (interface) : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable

- `public T Item(System.Int32 index) { get; set; }`
- `public System.Int32 IndexOf(T item)`
- `public System.Void Insert(System.Int32 index, T item)`
- `public System.Void RemoveAt(System.Int32 index)`

### IReadOnlyCollection`1<T> (interface) : System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable

- `public System.Int32 Count { get; }`

### IReadOnlyDictionary`2<TKey, TValue> (interface) : System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.IEnumerable, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>

- `public TValue Item(TKey key) { get; }`
- `public System.Collections.Generic.IEnumerable<TKey> Keys { get; }`
- `public System.Collections.Generic.IEnumerable<TValue> Values { get; }`
- `public System.Boolean ContainsKey(TKey key)`
- `public System.Boolean TryGetValue(TKey key, out TValue& value)`

### IReadOnlyList`1<T> (interface) : System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable, System.Collections.Generic.IReadOnlyCollection<T>

- `public T Item(System.Int32 index) { get; }`

### KeyCollection<TKey, TValue> (class [sealed]) : System.Collections.Generic.ICollection<TKey>, System.Collections.Generic.IEnumerable<TKey>, System.Collections.IEnumerable, System.Collections.Generic.IReadOnlyCollection<TKey>, System.Collections.ICollection

- `public KeyCollection(System.Collections.Generic.SortedDictionary<TKey, TValue> dictionary)`
- `public System.Int32 Count { get; }`
- `public System.Boolean Contains(TKey item)`
- `public System.Void CopyTo(TKey[] array, System.Int32 index)`
- `public System.Collections.Generic.SortedDictionary<TKey, TValue> GetEnumerator()`

### KeyCollection<TKey, TValue> (class [sealed]) : System.Collections.Generic.ICollection<TKey>, System.Collections.Generic.IEnumerable<TKey>, System.Collections.IEnumerable, System.Collections.Generic.IReadOnlyCollection<TKey>, System.Collections.ICollection

- `public KeyCollection(System.Collections.Generic.Dictionary<TKey, TValue> dictionary)`
- `public System.Int32 Count { get; }`
- `public System.Boolean Contains(TKey item)`
- `public System.Void CopyTo(TKey[] array, System.Int32 index)`
- `public System.Collections.Generic.Dictionary<TKey, TValue> GetEnumerator()`

### KeyValuePair (class [static])

- `public static System.Collections.Generic.KeyValuePair<TKey, TValue> Create<TKey, TValue>(TKey key, TValue value)`

### KeyValuePair`2<TKey, TValue> (struct [readonly struct])

- `public KeyValuePair`2(TKey key, TValue value)`
- `public TKey Key { get; }`
- `public TValue Value { get; }`
- `public System.Void Deconstruct(out TKey& key, out TValue& value)`
- `public System.String ToString()`

### List`1<T> (class) : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable, System.Collections.Generic.IList<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.IReadOnlyList<T>, System.Collections.ICollection, System.Collections.IList

- `public List`1()`
- `public List`1(System.Collections.Generic.IEnumerable<T> collection)`
- `public List`1(System.Int32 capacity)`
- `public System.Int32 Capacity { get; set; }`
- `public System.Int32 Count { get; }`
- `public T Item(System.Int32 index) { get; set; }`
- `public System.Void Add(T item)`
- `public System.Void AddRange(System.Collections.Generic.IEnumerable<T> collection)`
- `public System.Collections.ObjectModel.ReadOnlyCollection<T> AsReadOnly()`
- `public System.Int32 BinarySearch(T item)`
- `public System.Int32 BinarySearch(T item, System.Collections.Generic.IComparer<T> comparer)`
- `public System.Int32 BinarySearch(System.Int32 index, System.Int32 count, T item, System.Collections.Generic.IComparer<T> comparer)`
- `public System.Void Clear()`
- `public System.Boolean Contains(T item)`
- `public System.Collections.Generic.List<TOutput> ConvertAll<TOutput>(System.Converter<T, TOutput> converter)`
- `public System.Void CopyTo(T[] array)`
- `public System.Void CopyTo(T[] array, System.Int32 arrayIndex)`
- `public System.Void CopyTo(System.Int32 index, T[] array, System.Int32 arrayIndex, System.Int32 count)`
- `public System.Int32 EnsureCapacity(System.Int32 capacity)`
- `public System.Boolean Exists(System.Predicate<T> match)`
- `public T Find(System.Predicate<T> match)`
- `public System.Collections.Generic.List<T> FindAll(System.Predicate<T> match)`
- `public System.Int32 FindIndex(System.Predicate<T> match)`
- `public System.Int32 FindIndex(System.Int32 startIndex, System.Predicate<T> match)`
- `public System.Int32 FindIndex(System.Int32 startIndex, System.Int32 count, System.Predicate<T> match)`
- `public T FindLast(System.Predicate<T> match)`
- `public System.Int32 FindLastIndex(System.Predicate<T> match)`
- `public System.Int32 FindLastIndex(System.Int32 startIndex, System.Predicate<T> match)`
- `public System.Int32 FindLastIndex(System.Int32 startIndex, System.Int32 count, System.Predicate<T> match)`
- `public System.Void ForEach(System.Action<T> action)`
- `public System.Collections.Generic.List<T> GetEnumerator()`
- `public System.Collections.Generic.List<T> GetRange(System.Int32 index, System.Int32 count)`
- `public System.Int32 IndexOf(T item)`
- `public System.Int32 IndexOf(T item, System.Int32 index)`
- `public System.Int32 IndexOf(T item, System.Int32 index, System.Int32 count)`
- `public System.Void Insert(System.Int32 index, T item)`
- `public System.Void InsertRange(System.Int32 index, System.Collections.Generic.IEnumerable<T> collection)`
- `public System.Int32 LastIndexOf(T item)`
- `public System.Int32 LastIndexOf(T item, System.Int32 index)`
- `public System.Int32 LastIndexOf(T item, System.Int32 index, System.Int32 count)`
- `public System.Boolean Remove(T item)`
- `public System.Int32 RemoveAll(System.Predicate<T> match)`
- `public System.Void RemoveAt(System.Int32 index)`
- `public System.Void RemoveRange(System.Int32 index, System.Int32 count)`
- `public System.Void Reverse()`
- `public System.Void Reverse(System.Int32 index, System.Int32 count)`
- `public System.Collections.Generic.List<T> Slice(System.Int32 start, System.Int32 length)`
- `public System.Void Sort()`
- `public System.Void Sort(System.Collections.Generic.IComparer<T> comparer)`
- `public System.Void Sort(System.Comparison<T> comparison)`
- `public System.Void Sort(System.Int32 index, System.Int32 count, System.Collections.Generic.IComparer<T> comparer)`
- `public T[] ToArray()`
- `public System.Void TrimExcess()`
- `public System.Boolean TrueForAll(System.Predicate<T> match)`

### SortedDictionary`2<TKey, TValue> (class) : System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.IEnumerable, System.Collections.Generic.IDictionary<TKey, TValue>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>, System.Collections.ICollection, System.Collections.IDictionary

- `public SortedDictionary`2()`
- `public SortedDictionary`2(System.Collections.Generic.IComparer<TKey> comparer)`
- `public SortedDictionary`2(System.Collections.Generic.IDictionary<TKey, TValue> dictionary)`
- `public SortedDictionary`2(System.Collections.Generic.IDictionary<TKey, TValue> dictionary, System.Collections.Generic.IComparer<TKey> comparer)`
- `public System.Collections.Generic.IComparer<TKey> Comparer { get; }`
- `public System.Int32 Count { get; }`
- `public TValue Item(TKey key) { get; set; }`
- `public System.Collections.Generic.SortedDictionary<TKey, TValue> Keys { get; }`
- `public System.Collections.Generic.SortedDictionary<TKey, TValue> Values { get; }`
- `public System.Void Add(TKey key, TValue value)`
- `public System.Void Clear()`
- `public System.Boolean ContainsKey(TKey key)`
- `public System.Boolean ContainsValue(TValue value)`
- `public System.Void CopyTo(System.Collections.Generic.KeyValuePair<TKey, TValue>[] array, System.Int32 index)`
- `public System.Collections.Generic.SortedDictionary<TKey, TValue> GetEnumerator()`
- `public System.Boolean Remove(TKey key)`
- `public System.Boolean TryGetValue(TKey key, out TValue& value)`

### SortedSet`1<T> (class) : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.ISet<T>, System.Collections.Generic.IReadOnlySet<T>, System.Collections.ICollection, System.Runtime.Serialization.IDeserializationCallback, System.Runtime.Serialization.ISerializable

- `public SortedSet`1()`
- `public SortedSet`1(System.Collections.Generic.IComparer<T> comparer)`
- `public SortedSet`1(System.Collections.Generic.IEnumerable<T> collection)`
- `public SortedSet`1(System.Collections.Generic.IEnumerable<T> collection, System.Collections.Generic.IComparer<T> comparer)`
- `public System.Collections.Generic.IComparer<T> Comparer { get; }`
- `public System.Int32 Count { get; }`
- `public T Max { get; }`
- `public T Min { get; }`
- `public System.Boolean Add(T item)`
- `public System.Void Clear()`
- `public System.Boolean Contains(T item)`
- `public System.Void CopyTo(T[] array)`
- `public System.Void CopyTo(T[] array, System.Int32 index)`
- `public System.Void CopyTo(T[] array, System.Int32 index, System.Int32 count)`
- `public static System.Collections.Generic.IEqualityComparer<System.Collections.Generic.SortedSet<T>> CreateSetComparer()`
- `public static System.Collections.Generic.IEqualityComparer<System.Collections.Generic.SortedSet<T>> CreateSetComparer(System.Collections.Generic.IEqualityComparer<T> memberEqualityComparer)`
- `public System.Void ExceptWith(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Collections.Generic.SortedSet<T> GetEnumerator()`
- `public System.Collections.Generic.SortedSet<T> GetViewBetween(T lowerValue, T upperValue)`
- `public System.Void IntersectWith(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean IsProperSubsetOf(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean IsProperSupersetOf(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean IsSubsetOf(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean IsSupersetOf(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean Overlaps(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean Remove(T item)`
- `public System.Int32 RemoveWhere(System.Predicate<T> match)`
- `public System.Collections.Generic.IEnumerable<T> Reverse()`
- `public System.Boolean SetEquals(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Void SymmetricExceptWith(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean TryGetValue(T equalValue, out T& actualValue)`
- `public System.Void UnionWith(System.Collections.Generic.IEnumerable<T> other)`

### ValueCollection<TKey, TValue> (class [sealed]) : System.Collections.Generic.ICollection<TValue>, System.Collections.Generic.IEnumerable<TValue>, System.Collections.IEnumerable, System.Collections.Generic.IReadOnlyCollection<TValue>, System.Collections.ICollection

- `public ValueCollection(System.Collections.Generic.SortedDictionary<TKey, TValue> dictionary)`
- `public System.Int32 Count { get; }`
- `public System.Void CopyTo(TValue[] array, System.Int32 index)`
- `public System.Collections.Generic.SortedDictionary<TKey, TValue> GetEnumerator()`

### ValueCollection<TKey, TValue> (class [sealed]) : System.Collections.Generic.ICollection<TValue>, System.Collections.Generic.IEnumerable<TValue>, System.Collections.IEnumerable, System.Collections.Generic.IReadOnlyCollection<TValue>, System.Collections.ICollection

- `public ValueCollection(System.Collections.Generic.Dictionary<TKey, TValue> dictionary)`
- `public System.Int32 Count { get; }`
- `public System.Void CopyTo(TValue[] array, System.Int32 index)`
- `public System.Collections.Generic.Dictionary<TKey, TValue> GetEnumerator()`

## System.Linq

### AsyncEnumerable (class [static])

- `[ext] public static System.Threading.Tasks.ValueTask<TSource> AggregateAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TSource>> func, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TSource> AggregateAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, TSource, TSource> func, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TAccumulate> AggregateAsync<TSource, TAccumulate>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, TAccumulate seed, System.Func<TAccumulate, TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TAccumulate>> func, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TAccumulate> AggregateAsync<TSource, TAccumulate>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, TAccumulate seed, System.Func<TAccumulate, TSource, TAccumulate> func, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TResult> AggregateAsync<TSource, TAccumulate, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, TAccumulate seed, System.Func<TAccumulate, TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TAccumulate>> func, System.Func<TAccumulate, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TResult>> resultSelector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TResult> AggregateAsync<TSource, TAccumulate, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, TAccumulate seed, System.Func<TAccumulate, TSource, TAccumulate> func, System.Func<TAccumulate, TResult> resultSelector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<System.Collections.Generic.KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TKey>> keySelector, System.Func<TKey, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TAccumulate>> seedSelector, System.Func<TAccumulate, TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TAccumulate>> func, System.Collections.Generic.IEqualityComparer<TKey> keyComparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<System.Collections.Generic.KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TKey>> keySelector, TAccumulate seed, System.Func<TAccumulate, TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TAccumulate>> func, System.Collections.Generic.IEqualityComparer<TKey> keyComparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<System.Collections.Generic.KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TKey, TAccumulate> seedSelector, System.Func<TAccumulate, TSource, TAccumulate> func, System.Collections.Generic.IEqualityComparer<TKey> keyComparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<System.Collections.Generic.KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, TAccumulate seed, System.Func<TAccumulate, TSource, TAccumulate> func, System.Collections.Generic.IEqualityComparer<TKey> keyComparer)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Boolean> AllAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Boolean> AllAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.Boolean>> predicate, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Boolean> AnyAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Boolean> AnyAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Boolean> AnyAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.Boolean>> predicate, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> Append<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, TSource element)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Decimal> AverageAsync(this System.Collections.Generic.IAsyncEnumerable<System.Decimal> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Double> AverageAsync(this System.Collections.Generic.IAsyncEnumerable<System.Double> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Double> AverageAsync(this System.Collections.Generic.IAsyncEnumerable<System.Int32> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Double> AverageAsync(this System.Collections.Generic.IAsyncEnumerable<System.Int64> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Decimal?> AverageAsync(this System.Collections.Generic.IAsyncEnumerable<System.Decimal?> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Double?> AverageAsync(this System.Collections.Generic.IAsyncEnumerable<System.Double?> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Double?> AverageAsync(this System.Collections.Generic.IAsyncEnumerable<System.Int32?> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Double?> AverageAsync(this System.Collections.Generic.IAsyncEnumerable<System.Int64?> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Single?> AverageAsync(this System.Collections.Generic.IAsyncEnumerable<System.Single?> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Single> AverageAsync(this System.Collections.Generic.IAsyncEnumerable<System.Single> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> Cast<TResult>(this System.Collections.Generic.IAsyncEnumerable<System.Object> source)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource[]> Chunk<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Int32 size)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> Concat<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> first, System.Collections.Generic.IAsyncEnumerable<TSource> second)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Boolean> ContainsAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, TSource value, System.Collections.Generic.IEqualityComparer<TSource> comparer, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Int32> CountAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Int32> CountAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Int32> CountAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.Boolean>> predicate, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<System.Collections.Generic.KeyValuePair<TKey, System.Int32>> CountBy<TSource, TKey>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TKey>> keySelector, System.Collections.Generic.IEqualityComparer<TKey> keyComparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<System.Collections.Generic.KeyValuePair<TKey, System.Int32>> CountBy<TSource, TKey>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IEqualityComparer<TKey> keyComparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> DefaultIfEmpty<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> DefaultIfEmpty<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, TSource defaultValue)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> Distinct<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Collections.Generic.IEqualityComparer<TSource> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> DistinctBy<TSource, TKey>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TKey>> keySelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> DistinctBy<TSource, TKey>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Threading.Tasks.ValueTask<TSource> ElementAtAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Index index, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TSource> ElementAtAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Int32 index, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TSource> ElementAtOrDefaultAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Index index, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TSource> ElementAtOrDefaultAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Int32 index, System.Threading.CancellationToken cancellationToken)`
- `public static System.Collections.Generic.IAsyncEnumerable<TResult> Empty<TResult>()`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> Except<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> first, System.Collections.Generic.IAsyncEnumerable<TSource> second, System.Collections.Generic.IEqualityComparer<TSource> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> ExceptBy<TSource, TKey>(this System.Collections.Generic.IAsyncEnumerable<TSource> first, System.Collections.Generic.IAsyncEnumerable<TKey> second, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TKey>> keySelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> ExceptBy<TSource, TKey>(this System.Collections.Generic.IAsyncEnumerable<TSource> first, System.Collections.Generic.IAsyncEnumerable<TKey> second, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Threading.Tasks.ValueTask<TSource> FirstAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TSource> FirstAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TSource> FirstAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.Boolean>> predicate, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TSource> FirstOrDefaultAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TSource> FirstOrDefaultAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TSource> FirstOrDefaultAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.Boolean>> predicate, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TSource> FirstOrDefaultAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, TSource defaultValue, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TSource> FirstOrDefaultAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate, TSource defaultValue, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TSource> FirstOrDefaultAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.Boolean>> predicate, TSource defaultValue, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<System.Linq.IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TKey>> keySelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<System.Linq.IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<System.Linq.IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TKey>> keySelector, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TElement>> elementSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> GroupBy<TSource, TKey, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TKey>> keySelector, System.Func<TKey, System.Collections.Generic.IEnumerable<TSource>, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TResult>> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<System.Linq.IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> GroupBy<TSource, TKey, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TKey, System.Collections.Generic.IEnumerable<TSource>, TResult> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> GroupBy<TSource, TKey, TElement, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TKey>> keySelector, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TElement>> elementSelector, System.Func<TKey, System.Collections.Generic.IEnumerable<TElement>, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TResult>> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> GroupBy<TSource, TKey, TElement, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector, System.Func<TKey, System.Collections.Generic.IEnumerable<TElement>, TResult> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this System.Collections.Generic.IAsyncEnumerable<TOuter> outer, System.Collections.Generic.IAsyncEnumerable<TInner> inner, System.Func<TOuter, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TKey>> outerKeySelector, System.Func<TInner, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TKey>> innerKeySelector, System.Func<TOuter, System.Collections.Generic.IEnumerable<TInner>, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TResult>> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this System.Collections.Generic.IAsyncEnumerable<TOuter> outer, System.Collections.Generic.IAsyncEnumerable<TInner> inner, System.Func<TOuter, TKey> outerKeySelector, System.Func<TInner, TKey> innerKeySelector, System.Func<TOuter, System.Collections.Generic.IEnumerable<TInner>, TResult> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<System.ValueTuple<System.Int32, TSource>> Index<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source)`
- `public static System.Collections.Generic.IAsyncEnumerable<T> InfiniteSequence<T>(T start, T step)`
- `where T : System.Numerics.IAdditionOperators<T, T, T>`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> Intersect<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> first, System.Collections.Generic.IAsyncEnumerable<TSource> second, System.Collections.Generic.IEqualityComparer<TSource> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> IntersectBy<TSource, TKey>(this System.Collections.Generic.IAsyncEnumerable<TSource> first, System.Collections.Generic.IAsyncEnumerable<TKey> second, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TKey>> keySelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> IntersectBy<TSource, TKey>(this System.Collections.Generic.IAsyncEnumerable<TSource> first, System.Collections.Generic.IAsyncEnumerable<TKey> second, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> Join<TOuter, TInner, TKey, TResult>(this System.Collections.Generic.IAsyncEnumerable<TOuter> outer, System.Collections.Generic.IAsyncEnumerable<TInner> inner, System.Func<TOuter, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TKey>> outerKeySelector, System.Func<TInner, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TKey>> innerKeySelector, System.Func<TOuter, TInner, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TResult>> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> Join<TOuter, TInner, TKey, TResult>(this System.Collections.Generic.IAsyncEnumerable<TOuter> outer, System.Collections.Generic.IAsyncEnumerable<TInner> inner, System.Func<TOuter, TKey> outerKeySelector, System.Func<TInner, TKey> innerKeySelector, System.Func<TOuter, TInner, TResult> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Threading.Tasks.ValueTask<TSource> LastAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TSource> LastAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TSource> LastAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.Boolean>> predicate, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TSource> LastOrDefaultAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TSource> LastOrDefaultAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TSource> LastOrDefaultAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.Boolean>> predicate, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TSource> LastOrDefaultAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, TSource defaultValue, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TSource> LastOrDefaultAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate, TSource defaultValue, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TSource> LastOrDefaultAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.Boolean>> predicate, TSource defaultValue, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> LeftJoin<TOuter, TInner, TKey, TResult>(this System.Collections.Generic.IAsyncEnumerable<TOuter> outer, System.Collections.Generic.IAsyncEnumerable<TInner> inner, System.Func<TOuter, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TKey>> outerKeySelector, System.Func<TInner, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TKey>> innerKeySelector, System.Func<TOuter, TInner, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TResult>> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> LeftJoin<TOuter, TInner, TKey, TResult>(this System.Collections.Generic.IAsyncEnumerable<TOuter> outer, System.Collections.Generic.IAsyncEnumerable<TInner> inner, System.Func<TOuter, TKey> outerKeySelector, System.Func<TInner, TKey> innerKeySelector, System.Func<TOuter, TInner, TResult> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Int64> LongCountAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Int64> LongCountAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Int64> LongCountAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.Boolean>> predicate, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TSource> MaxAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Collections.Generic.IComparer<TSource> comparer, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TSource> MaxByAsync<TSource, TKey>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TKey>> keySelector, System.Collections.Generic.IComparer<TKey> comparer, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TSource> MaxByAsync<TSource, TKey>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IComparer<TKey> comparer, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TSource> MinAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Collections.Generic.IComparer<TSource> comparer, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TSource> MinByAsync<TSource, TKey>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TKey>> keySelector, System.Collections.Generic.IComparer<TKey> comparer, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TSource> MinByAsync<TSource, TKey>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IComparer<TKey> comparer, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> OfType<TResult>(this System.Collections.Generic.IAsyncEnumerable<System.Object> source)`
- `[ext] public static System.Linq.IOrderedAsyncEnumerable<T> Order<T>(this System.Collections.Generic.IAsyncEnumerable<T> source, System.Collections.Generic.IComparer<T> comparer)`
- `[ext] public static System.Linq.IOrderedAsyncEnumerable<TSource> OrderBy<TSource, TKey>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TKey>> keySelector, System.Collections.Generic.IComparer<TKey> comparer)`
- `[ext] public static System.Linq.IOrderedAsyncEnumerable<TSource> OrderBy<TSource, TKey>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IComparer<TKey> comparer)`
- `[ext] public static System.Linq.IOrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TKey>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TKey>> keySelector, System.Collections.Generic.IComparer<TKey> comparer)`
- `[ext] public static System.Linq.IOrderedAsyncEnumerable<TSource> OrderByDescending<TSource, TKey>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IComparer<TKey> comparer)`
- `[ext] public static System.Linq.IOrderedAsyncEnumerable<T> OrderDescending<T>(this System.Collections.Generic.IAsyncEnumerable<T> source, System.Collections.Generic.IComparer<T> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> Prepend<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, TSource element)`
- `public static System.Collections.Generic.IAsyncEnumerable<System.Int32> Range(System.Int32 start, System.Int32 count)`
- `public static System.Collections.Generic.IAsyncEnumerable<TResult> Repeat<TResult>(TResult element, System.Int32 count)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> Reverse<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> RightJoin<TOuter, TInner, TKey, TResult>(this System.Collections.Generic.IAsyncEnumerable<TOuter> outer, System.Collections.Generic.IAsyncEnumerable<TInner> inner, System.Func<TOuter, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TKey>> outerKeySelector, System.Func<TInner, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TKey>> innerKeySelector, System.Func<TOuter, TInner, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TResult>> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> RightJoin<TOuter, TInner, TKey, TResult>(this System.Collections.Generic.IAsyncEnumerable<TOuter> outer, System.Collections.Generic.IAsyncEnumerable<TInner> inner, System.Func<TOuter, TKey> outerKeySelector, System.Func<TInner, TKey> innerKeySelector, System.Func<TOuter, TInner, TResult> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> Select<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Int32, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TResult>> selector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> Select<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Int32, TResult> selector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> Select<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TResult>> selector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> Select<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, TResult> selector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> SelectMany<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Collections.Generic.IAsyncEnumerable<TResult>> selector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> SelectMany<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Collections.Generic.IEnumerable<TResult>> selector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> SelectMany<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Int32, System.Collections.Generic.IAsyncEnumerable<TResult>> selector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> SelectMany<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Int32, System.Collections.Generic.IEnumerable<TResult>> selector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> SelectMany<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Int32, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.Collections.Generic.IEnumerable<TResult>>> selector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> SelectMany<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.Collections.Generic.IEnumerable<TResult>>> selector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Collections.Generic.IAsyncEnumerable<TCollection>> collectionSelector, System.Func<TSource, TCollection, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TResult>> resultSelector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Collections.Generic.IAsyncEnumerable<TCollection>> collectionSelector, System.Func<TSource, TCollection, TResult> resultSelector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Collections.Generic.IEnumerable<TCollection>> collectionSelector, System.Func<TSource, TCollection, TResult> resultSelector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Int32, System.Collections.Generic.IAsyncEnumerable<TCollection>> collectionSelector, System.Func<TSource, TCollection, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TResult>> resultSelector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Int32, System.Collections.Generic.IEnumerable<TCollection>> collectionSelector, System.Func<TSource, TCollection, TResult> resultSelector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Int32, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.Collections.Generic.IEnumerable<TCollection>>> collectionSelector, System.Func<TSource, TCollection, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TResult>> resultSelector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.Collections.Generic.IEnumerable<TCollection>>> collectionSelector, System.Func<TSource, TCollection, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TResult>> resultSelector)`
- `public static System.Collections.Generic.IAsyncEnumerable<T> Sequence<T>(T start, T endInclusive, T step)`
- `where T : System.Numerics.INumber<T>`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Boolean> SequenceEqualAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> first, System.Collections.Generic.IAsyncEnumerable<TSource> second, System.Collections.Generic.IEqualityComparer<TSource> comparer, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> Shuffle<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source)`
- `[ext] public static System.Threading.Tasks.ValueTask<TSource> SingleAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TSource> SingleAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TSource> SingleAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.Boolean>> predicate, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TSource> SingleOrDefaultAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TSource> SingleOrDefaultAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TSource> SingleOrDefaultAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.Boolean>> predicate, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TSource> SingleOrDefaultAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, TSource defaultValue, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TSource> SingleOrDefaultAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate, TSource defaultValue, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TSource> SingleOrDefaultAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.Boolean>> predicate, TSource defaultValue, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> Skip<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Int32 count)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> SkipLast<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Int32 count)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> SkipWhile<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> SkipWhile<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Int32, System.Boolean> predicate)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> SkipWhile<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Int32, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.Boolean>> predicate)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> SkipWhile<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.Boolean>> predicate)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Decimal> SumAsync(this System.Collections.Generic.IAsyncEnumerable<System.Decimal> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Double> SumAsync(this System.Collections.Generic.IAsyncEnumerable<System.Double> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Int32> SumAsync(this System.Collections.Generic.IAsyncEnumerable<System.Int32> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Int64> SumAsync(this System.Collections.Generic.IAsyncEnumerable<System.Int64> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Decimal?> SumAsync(this System.Collections.Generic.IAsyncEnumerable<System.Decimal?> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Double?> SumAsync(this System.Collections.Generic.IAsyncEnumerable<System.Double?> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Int32?> SumAsync(this System.Collections.Generic.IAsyncEnumerable<System.Int32?> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Int64?> SumAsync(this System.Collections.Generic.IAsyncEnumerable<System.Int64?> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Single?> SumAsync(this System.Collections.Generic.IAsyncEnumerable<System.Single?> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Single> SumAsync(this System.Collections.Generic.IAsyncEnumerable<System.Single> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> Take<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Int32 count)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> Take<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Range range)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> TakeLast<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Int32 count)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> TakeWhile<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> TakeWhile<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Int32, System.Boolean> predicate)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> TakeWhile<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Int32, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.Boolean>> predicate)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> TakeWhile<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.Boolean>> predicate)`
- `[ext] public static System.Linq.IOrderedAsyncEnumerable<TSource> ThenBy<TSource, TKey>(this System.Linq.IOrderedAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TKey>> keySelector, System.Collections.Generic.IComparer<TKey> comparer)`
- `[ext] public static System.Linq.IOrderedAsyncEnumerable<TSource> ThenBy<TSource, TKey>(this System.Linq.IOrderedAsyncEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IComparer<TKey> comparer)`
- `[ext] public static System.Linq.IOrderedAsyncEnumerable<TSource> ThenByDescending<TSource, TKey>(this System.Linq.IOrderedAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TKey>> keySelector, System.Collections.Generic.IComparer<TKey> comparer)`
- `[ext] public static System.Linq.IOrderedAsyncEnumerable<TSource> ThenByDescending<TSource, TKey>(this System.Linq.IOrderedAsyncEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IComparer<TKey> comparer)`
- `[ext] public static System.Threading.Tasks.ValueTask<TSource[]> ToArrayAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> ToAsyncEnumerable<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Collections.Generic.Dictionary<TKey, TValue>> ToDictionaryAsync<TKey, TValue>(this System.Collections.Generic.IAsyncEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> source, System.Collections.Generic.IEqualityComparer<TKey> comparer, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Collections.Generic.Dictionary<TKey, TValue>> ToDictionaryAsync<TKey, TValue>(this System.Collections.Generic.IAsyncEnumerable<System.ValueTuple<TKey, TValue>> source, System.Collections.Generic.IEqualityComparer<TKey> comparer, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Collections.Generic.Dictionary<TKey, TSource>> ToDictionaryAsync<TSource, TKey>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TKey>> keySelector, System.Collections.Generic.IEqualityComparer<TKey> comparer, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Collections.Generic.Dictionary<TKey, TSource>> ToDictionaryAsync<TSource, TKey>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IEqualityComparer<TKey> comparer, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Collections.Generic.Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TKey>> keySelector, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TElement>> elementSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Collections.Generic.Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Collections.Generic.HashSet<TSource>> ToHashSetAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Collections.Generic.IEqualityComparer<TSource> comparer, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Collections.Generic.List<TSource>> ToListAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Linq.ILookup<TKey, TSource>> ToLookupAsync<TSource, TKey>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TKey>> keySelector, System.Collections.Generic.IEqualityComparer<TKey> comparer, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Linq.ILookup<TKey, TSource>> ToLookupAsync<TSource, TKey>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IEqualityComparer<TKey> comparer, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Linq.ILookup<TKey, TElement>> ToLookupAsync<TSource, TKey, TElement>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TKey>> keySelector, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TElement>> elementSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Linq.ILookup<TKey, TElement>> ToLookupAsync<TSource, TKey, TElement>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> Union<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> first, System.Collections.Generic.IAsyncEnumerable<TSource> second, System.Collections.Generic.IEqualityComparer<TSource> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> UnionBy<TSource, TKey>(this System.Collections.Generic.IAsyncEnumerable<TSource> first, System.Collections.Generic.IAsyncEnumerable<TSource> second, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TKey>> keySelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> UnionBy<TSource, TKey>(this System.Collections.Generic.IAsyncEnumerable<TSource> first, System.Collections.Generic.IAsyncEnumerable<TSource> second, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> Where<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> Where<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Int32, System.Boolean> predicate)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> Where<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Int32, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.Boolean>> predicate)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> Where<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.Boolean>> predicate)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<System.ValueTuple<TFirst, TSecond>> Zip<TFirst, TSecond>(this System.Collections.Generic.IAsyncEnumerable<TFirst> first, System.Collections.Generic.IAsyncEnumerable<TSecond> second)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<System.ValueTuple<TFirst, TSecond, TThird>> Zip<TFirst, TSecond, TThird>(this System.Collections.Generic.IAsyncEnumerable<TFirst> first, System.Collections.Generic.IAsyncEnumerable<TSecond> second, System.Collections.Generic.IAsyncEnumerable<TThird> third)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> Zip<TFirst, TSecond, TResult>(this System.Collections.Generic.IAsyncEnumerable<TFirst> first, System.Collections.Generic.IAsyncEnumerable<TSecond> second, System.Func<TFirst, TSecond, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TResult>> resultSelector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> Zip<TFirst, TSecond, TResult>(this System.Collections.Generic.IAsyncEnumerable<TFirst> first, System.Collections.Generic.IAsyncEnumerable<TSecond> second, System.Func<TFirst, TSecond, TResult> resultSelector)`

### Enumerable (class [static])

- `[ext] public static TSource Aggregate<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TSource, TSource> func)`
- `[ext] public static TAccumulate Aggregate<TSource, TAccumulate>(this System.Collections.Generic.IEnumerable<TSource> source, TAccumulate seed, System.Func<TAccumulate, TSource, TAccumulate> func)`
- `[ext] public static TResult Aggregate<TSource, TAccumulate, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, TAccumulate seed, System.Func<TAccumulate, TSource, TAccumulate> func, System.Func<TAccumulate, TResult> resultSelector)`
- `[ext] public static System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, TAccumulate seed, System.Func<TAccumulate, TSource, TAccumulate> func, System.Collections.Generic.IEqualityComparer<TKey> keyComparer)`
- `[ext] public static System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TKey, TAccumulate> seedSelector, System.Func<TAccumulate, TSource, TAccumulate> func, System.Collections.Generic.IEqualityComparer<TKey> keyComparer)`
- `[ext] public static System.Boolean All<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate)`
- `[ext] public static System.Boolean Any<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `[ext] public static System.Boolean Any<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> Append<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, TSource element)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> AsEnumerable<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `[ext] public static System.Decimal Average(this System.Collections.Generic.IEnumerable<System.Decimal> source)`
- `[ext] public static System.Double Average(this System.Collections.Generic.IEnumerable<System.Double> source)`
- `[ext] public static System.Double Average(this System.Collections.Generic.IEnumerable<System.Int32> source)`
- `[ext] public static System.Double Average(this System.Collections.Generic.IEnumerable<System.Int64> source)`
- `[ext] public static System.Decimal? Average(this System.Collections.Generic.IEnumerable<System.Decimal?> source)`
- `[ext] public static System.Double? Average(this System.Collections.Generic.IEnumerable<System.Double?> source)`
- `[ext] public static System.Double? Average(this System.Collections.Generic.IEnumerable<System.Int32?> source)`
- `[ext] public static System.Double? Average(this System.Collections.Generic.IEnumerable<System.Int64?> source)`
- `[ext] public static System.Single? Average(this System.Collections.Generic.IEnumerable<System.Single?> source)`
- `[ext] public static System.Single Average(this System.Collections.Generic.IEnumerable<System.Single> source)`
- `[ext] public static System.Decimal Average<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Decimal> selector)`
- `[ext] public static System.Double Average<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Double> selector)`
- `[ext] public static System.Double Average<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Int32> selector)`
- `[ext] public static System.Double Average<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Int64> selector)`
- `[ext] public static System.Decimal? Average<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Decimal?> selector)`
- `[ext] public static System.Double? Average<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Double?> selector)`
- `[ext] public static System.Double? Average<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Int32?> selector)`
- `[ext] public static System.Double? Average<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Int64?> selector)`
- `[ext] public static System.Single? Average<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Single?> selector)`
- `[ext] public static System.Single Average<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Single> selector)`
- `[ext] public static System.Collections.Generic.IEnumerable<TResult> Cast<TResult>(this System.Collections.IEnumerable source)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource[]> Chunk<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Int32 size)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> Concat<TSource>(this System.Collections.Generic.IEnumerable<TSource> first, System.Collections.Generic.IEnumerable<TSource> second)`
- `[ext] public static System.Boolean Contains<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, TSource value)`
- `[ext] public static System.Boolean Contains<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, TSource value, System.Collections.Generic.IEqualityComparer<TSource> comparer)`
- `[ext] public static System.Int32 Count<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `[ext] public static System.Int32 Count<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate)`
- `[ext] public static System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, System.Int32>> CountBy<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IEqualityComparer<TKey> keyComparer)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> DefaultIfEmpty<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> DefaultIfEmpty<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, TSource defaultValue)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> Distinct<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> Distinct<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Collections.Generic.IEqualityComparer<TSource> comparer)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> DistinctBy<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> DistinctBy<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static TSource ElementAt<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Index index)`
- `[ext] public static TSource ElementAt<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Int32 index)`
- `[ext] public static TSource ElementAtOrDefault<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Index index)`
- `[ext] public static TSource ElementAtOrDefault<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Int32 index)`
- `public static System.Collections.Generic.IEnumerable<TResult> Empty<TResult>()`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> Except<TSource>(this System.Collections.Generic.IEnumerable<TSource> first, System.Collections.Generic.IEnumerable<TSource> second)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> Except<TSource>(this System.Collections.Generic.IEnumerable<TSource> first, System.Collections.Generic.IEnumerable<TSource> second, System.Collections.Generic.IEqualityComparer<TSource> comparer)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> ExceptBy<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> first, System.Collections.Generic.IEnumerable<TKey> second, System.Func<TSource, TKey> keySelector)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> ExceptBy<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> first, System.Collections.Generic.IEnumerable<TKey> second, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static TSource First<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `[ext] public static TSource First<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate)`
- `[ext] public static TSource FirstOrDefault<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `[ext] public static TSource FirstOrDefault<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, TSource defaultValue)`
- `[ext] public static TSource FirstOrDefault<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate)`
- `[ext] public static TSource FirstOrDefault<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate, TSource defaultValue)`
- `[ext] public static System.Collections.Generic.IEnumerable<System.Linq.IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector)`
- `[ext] public static System.Collections.Generic.IEnumerable<System.Linq.IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IEnumerable<System.Linq.IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector)`
- `[ext] public static System.Collections.Generic.IEnumerable<TResult> GroupBy<TSource, TKey, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TKey, System.Collections.Generic.IEnumerable<TSource>, TResult> resultSelector)`
- `[ext] public static System.Collections.Generic.IEnumerable<System.Linq.IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IEnumerable<TResult> GroupBy<TSource, TKey, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TKey, System.Collections.Generic.IEnumerable<TSource>, TResult> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IEnumerable<TResult> GroupBy<TSource, TKey, TElement, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector, System.Func<TKey, System.Collections.Generic.IEnumerable<TElement>, TResult> resultSelector)`
- `[ext] public static System.Collections.Generic.IEnumerable<TResult> GroupBy<TSource, TKey, TElement, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector, System.Func<TKey, System.Collections.Generic.IEnumerable<TElement>, TResult> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IEnumerable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this System.Collections.Generic.IEnumerable<TOuter> outer, System.Collections.Generic.IEnumerable<TInner> inner, System.Func<TOuter, TKey> outerKeySelector, System.Func<TInner, TKey> innerKeySelector, System.Func<TOuter, System.Collections.Generic.IEnumerable<TInner>, TResult> resultSelector)`
- `[ext] public static System.Collections.Generic.IEnumerable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this System.Collections.Generic.IEnumerable<TOuter> outer, System.Collections.Generic.IEnumerable<TInner> inner, System.Func<TOuter, TKey> outerKeySelector, System.Func<TInner, TKey> innerKeySelector, System.Func<TOuter, System.Collections.Generic.IEnumerable<TInner>, TResult> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IEnumerable<System.ValueTuple<System.Int32, TSource>> Index<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `public static System.Collections.Generic.IEnumerable<T> InfiniteSequence<T>(T start, T step)`
- `where T : System.Numerics.IAdditionOperators<T, T, T>`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> Intersect<TSource>(this System.Collections.Generic.IEnumerable<TSource> first, System.Collections.Generic.IEnumerable<TSource> second)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> Intersect<TSource>(this System.Collections.Generic.IEnumerable<TSource> first, System.Collections.Generic.IEnumerable<TSource> second, System.Collections.Generic.IEqualityComparer<TSource> comparer)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> IntersectBy<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> first, System.Collections.Generic.IEnumerable<TKey> second, System.Func<TSource, TKey> keySelector)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> IntersectBy<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> first, System.Collections.Generic.IEnumerable<TKey> second, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IEnumerable<TResult> Join<TOuter, TInner, TKey, TResult>(this System.Collections.Generic.IEnumerable<TOuter> outer, System.Collections.Generic.IEnumerable<TInner> inner, System.Func<TOuter, TKey> outerKeySelector, System.Func<TInner, TKey> innerKeySelector, System.Func<TOuter, TInner, TResult> resultSelector)`
- `[ext] public static System.Collections.Generic.IEnumerable<TResult> Join<TOuter, TInner, TKey, TResult>(this System.Collections.Generic.IEnumerable<TOuter> outer, System.Collections.Generic.IEnumerable<TInner> inner, System.Func<TOuter, TKey> outerKeySelector, System.Func<TInner, TKey> innerKeySelector, System.Func<TOuter, TInner, TResult> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static TSource Last<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `[ext] public static TSource Last<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate)`
- `[ext] public static TSource LastOrDefault<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `[ext] public static TSource LastOrDefault<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, TSource defaultValue)`
- `[ext] public static TSource LastOrDefault<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate)`
- `[ext] public static TSource LastOrDefault<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate, TSource defaultValue)`
- `[ext] public static System.Collections.Generic.IEnumerable<TResult> LeftJoin<TOuter, TInner, TKey, TResult>(this System.Collections.Generic.IEnumerable<TOuter> outer, System.Collections.Generic.IEnumerable<TInner> inner, System.Func<TOuter, TKey> outerKeySelector, System.Func<TInner, TKey> innerKeySelector, System.Func<TOuter, TInner, TResult> resultSelector)`
- `[ext] public static System.Collections.Generic.IEnumerable<TResult> LeftJoin<TOuter, TInner, TKey, TResult>(this System.Collections.Generic.IEnumerable<TOuter> outer, System.Collections.Generic.IEnumerable<TInner> inner, System.Func<TOuter, TKey> outerKeySelector, System.Func<TInner, TKey> innerKeySelector, System.Func<TOuter, TInner, TResult> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Int64 LongCount<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `[ext] public static System.Int64 LongCount<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate)`
- `[ext] public static System.Decimal Max(this System.Collections.Generic.IEnumerable<System.Decimal> source)`
- `[ext] public static System.Double Max(this System.Collections.Generic.IEnumerable<System.Double> source)`
- `[ext] public static System.Int32 Max(this System.Collections.Generic.IEnumerable<System.Int32> source)`
- `[ext] public static System.Int64 Max(this System.Collections.Generic.IEnumerable<System.Int64> source)`
- `[ext] public static System.Decimal? Max(this System.Collections.Generic.IEnumerable<System.Decimal?> source)`
- `[ext] public static System.Double? Max(this System.Collections.Generic.IEnumerable<System.Double?> source)`
- `[ext] public static System.Int32? Max(this System.Collections.Generic.IEnumerable<System.Int32?> source)`
- `[ext] public static System.Int64? Max(this System.Collections.Generic.IEnumerable<System.Int64?> source)`
- `[ext] public static System.Single? Max(this System.Collections.Generic.IEnumerable<System.Single?> source)`
- `[ext] public static System.Single Max(this System.Collections.Generic.IEnumerable<System.Single> source)`
- `[ext] public static TSource Max<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `[ext] public static TSource Max<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Collections.Generic.IComparer<TSource> comparer)`
- `[ext] public static System.Decimal Max<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Decimal> selector)`
- `[ext] public static System.Double Max<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Double> selector)`
- `[ext] public static System.Int32 Max<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Int32> selector)`
- `[ext] public static System.Int64 Max<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Int64> selector)`
- `[ext] public static System.Decimal? Max<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Decimal?> selector)`
- `[ext] public static System.Double? Max<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Double?> selector)`
- `[ext] public static System.Int32? Max<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Int32?> selector)`
- `[ext] public static System.Int64? Max<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Int64?> selector)`
- `[ext] public static System.Single? Max<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Single?> selector)`
- `[ext] public static System.Single Max<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Single> selector)`
- `[ext] public static TResult Max<TSource, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TResult> selector)`
- `[ext] public static TSource MaxBy<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector)`
- `[ext] public static TSource MaxBy<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IComparer<TKey> comparer)`
- `[ext] public static System.Decimal Min(this System.Collections.Generic.IEnumerable<System.Decimal> source)`
- `[ext] public static System.Double Min(this System.Collections.Generic.IEnumerable<System.Double> source)`
- `[ext] public static System.Int32 Min(this System.Collections.Generic.IEnumerable<System.Int32> source)`
- `[ext] public static System.Int64 Min(this System.Collections.Generic.IEnumerable<System.Int64> source)`
- `[ext] public static System.Decimal? Min(this System.Collections.Generic.IEnumerable<System.Decimal?> source)`
- `[ext] public static System.Double? Min(this System.Collections.Generic.IEnumerable<System.Double?> source)`
- `[ext] public static System.Int32? Min(this System.Collections.Generic.IEnumerable<System.Int32?> source)`
- `[ext] public static System.Int64? Min(this System.Collections.Generic.IEnumerable<System.Int64?> source)`
- `[ext] public static System.Single? Min(this System.Collections.Generic.IEnumerable<System.Single?> source)`
- `[ext] public static System.Single Min(this System.Collections.Generic.IEnumerable<System.Single> source)`
- `[ext] public static TSource Min<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `[ext] public static TSource Min<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Collections.Generic.IComparer<TSource> comparer)`
- `[ext] public static System.Decimal Min<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Decimal> selector)`
- `[ext] public static System.Double Min<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Double> selector)`
- `[ext] public static System.Int32 Min<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Int32> selector)`
- `[ext] public static System.Int64 Min<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Int64> selector)`
- `[ext] public static System.Decimal? Min<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Decimal?> selector)`
- `[ext] public static System.Double? Min<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Double?> selector)`
- `[ext] public static System.Int32? Min<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Int32?> selector)`
- `[ext] public static System.Int64? Min<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Int64?> selector)`
- `[ext] public static System.Single? Min<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Single?> selector)`
- `[ext] public static System.Single Min<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Single> selector)`
- `[ext] public static TResult Min<TSource, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TResult> selector)`
- `[ext] public static TSource MinBy<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector)`
- `[ext] public static TSource MinBy<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IEnumerable<TResult> OfType<TResult>(this System.Collections.IEnumerable source)`
- `[ext] public static System.Linq.IOrderedEnumerable<T> Order<T>(this System.Collections.Generic.IEnumerable<T> source)`
- `[ext] public static System.Linq.IOrderedEnumerable<T> Order<T>(this System.Collections.Generic.IEnumerable<T> source, System.Collections.Generic.IComparer<T> comparer)`
- `[ext] public static System.Linq.IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector)`
- `[ext] public static System.Linq.IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IComparer<TKey> comparer)`
- `[ext] public static System.Linq.IOrderedEnumerable<TSource> OrderByDescending<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector)`
- `[ext] public static System.Linq.IOrderedEnumerable<TSource> OrderByDescending<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IComparer<TKey> comparer)`
- `[ext] public static System.Linq.IOrderedEnumerable<T> OrderDescending<T>(this System.Collections.Generic.IEnumerable<T> source)`
- `[ext] public static System.Linq.IOrderedEnumerable<T> OrderDescending<T>(this System.Collections.Generic.IEnumerable<T> source, System.Collections.Generic.IComparer<T> comparer)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> Prepend<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, TSource element)`
- `public static System.Collections.Generic.IEnumerable<System.Int32> Range(System.Int32 start, System.Int32 count)`
- `public static System.Collections.Generic.IEnumerable<TResult> Repeat<TResult>(TResult element, System.Int32 count)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> Reverse<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> Reverse<TSource>(this TSource[] source)`
- `[ext] public static System.Collections.Generic.IEnumerable<TResult> RightJoin<TOuter, TInner, TKey, TResult>(this System.Collections.Generic.IEnumerable<TOuter> outer, System.Collections.Generic.IEnumerable<TInner> inner, System.Func<TOuter, TKey> outerKeySelector, System.Func<TInner, TKey> innerKeySelector, System.Func<TOuter, TInner, TResult> resultSelector)`
- `[ext] public static System.Collections.Generic.IEnumerable<TResult> RightJoin<TOuter, TInner, TKey, TResult>(this System.Collections.Generic.IEnumerable<TOuter> outer, System.Collections.Generic.IEnumerable<TInner> inner, System.Func<TOuter, TKey> outerKeySelector, System.Func<TInner, TKey> innerKeySelector, System.Func<TOuter, TInner, TResult> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IEnumerable<TResult> Select<TSource, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Int32, TResult> selector)`
- `[ext] public static System.Collections.Generic.IEnumerable<TResult> Select<TSource, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TResult> selector)`
- `[ext] public static System.Collections.Generic.IEnumerable<TResult> SelectMany<TSource, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Collections.Generic.IEnumerable<TResult>> selector)`
- `[ext] public static System.Collections.Generic.IEnumerable<TResult> SelectMany<TSource, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Int32, System.Collections.Generic.IEnumerable<TResult>> selector)`
- `[ext] public static System.Collections.Generic.IEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Collections.Generic.IEnumerable<TCollection>> collectionSelector, System.Func<TSource, TCollection, TResult> resultSelector)`
- `[ext] public static System.Collections.Generic.IEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Int32, System.Collections.Generic.IEnumerable<TCollection>> collectionSelector, System.Func<TSource, TCollection, TResult> resultSelector)`
- `public static System.Collections.Generic.IEnumerable<T> Sequence<T>(T start, T endInclusive, T step)`
- `where T : System.Numerics.INumber<T>`
- `[ext] public static System.Boolean SequenceEqual<TSource>(this System.Collections.Generic.IEnumerable<TSource> first, System.Collections.Generic.IEnumerable<TSource> second)`
- `[ext] public static System.Boolean SequenceEqual<TSource>(this System.Collections.Generic.IEnumerable<TSource> first, System.Collections.Generic.IEnumerable<TSource> second, System.Collections.Generic.IEqualityComparer<TSource> comparer)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> Shuffle<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `[ext] public static TSource Single<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `[ext] public static TSource Single<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate)`
- `[ext] public static TSource SingleOrDefault<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `[ext] public static TSource SingleOrDefault<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, TSource defaultValue)`
- `[ext] public static TSource SingleOrDefault<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate)`
- `[ext] public static TSource SingleOrDefault<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate, TSource defaultValue)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> Skip<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Int32 count)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> SkipLast<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Int32 count)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> SkipWhile<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> SkipWhile<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Int32, System.Boolean> predicate)`
- `[ext] public static System.Decimal Sum(this System.Collections.Generic.IEnumerable<System.Decimal> source)`
- `[ext] public static System.Double Sum(this System.Collections.Generic.IEnumerable<System.Double> source)`
- `[ext] public static System.Int32 Sum(this System.Collections.Generic.IEnumerable<System.Int32> source)`
- `[ext] public static System.Int64 Sum(this System.Collections.Generic.IEnumerable<System.Int64> source)`
- `[ext] public static System.Decimal? Sum(this System.Collections.Generic.IEnumerable<System.Decimal?> source)`
- `[ext] public static System.Double? Sum(this System.Collections.Generic.IEnumerable<System.Double?> source)`
- `[ext] public static System.Int32? Sum(this System.Collections.Generic.IEnumerable<System.Int32?> source)`
- `[ext] public static System.Int64? Sum(this System.Collections.Generic.IEnumerable<System.Int64?> source)`
- `[ext] public static System.Single? Sum(this System.Collections.Generic.IEnumerable<System.Single?> source)`
- `[ext] public static System.Single Sum(this System.Collections.Generic.IEnumerable<System.Single> source)`
- `[ext] public static System.Decimal Sum<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Decimal> selector)`
- `[ext] public static System.Double Sum<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Double> selector)`
- `[ext] public static System.Int32 Sum<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Int32> selector)`
- `[ext] public static System.Int64 Sum<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Int64> selector)`
- `[ext] public static System.Decimal? Sum<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Decimal?> selector)`
- `[ext] public static System.Double? Sum<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Double?> selector)`
- `[ext] public static System.Int32? Sum<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Int32?> selector)`
- `[ext] public static System.Int64? Sum<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Int64?> selector)`
- `[ext] public static System.Single? Sum<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Single?> selector)`
- `[ext] public static System.Single Sum<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Single> selector)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> Take<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Int32 count)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> Take<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Range range)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> TakeLast<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Int32 count)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> TakeWhile<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> TakeWhile<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Int32, System.Boolean> predicate)`
- `[ext] public static System.Linq.IOrderedEnumerable<TSource> ThenBy<TSource, TKey>(this System.Linq.IOrderedEnumerable<TSource> source, System.Func<TSource, TKey> keySelector)`
- `[ext] public static System.Linq.IOrderedEnumerable<TSource> ThenBy<TSource, TKey>(this System.Linq.IOrderedEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IComparer<TKey> comparer)`
- `[ext] public static System.Linq.IOrderedEnumerable<TSource> ThenByDescending<TSource, TKey>(this System.Linq.IOrderedEnumerable<TSource> source, System.Func<TSource, TKey> keySelector)`
- `[ext] public static System.Linq.IOrderedEnumerable<TSource> ThenByDescending<TSource, TKey>(this System.Linq.IOrderedEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IComparer<TKey> comparer)`
- `[ext] public static TSource[] ToArray<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `[ext] public static System.Collections.Generic.Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> source)`
- `[ext] public static System.Collections.Generic.Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this System.Collections.Generic.IEnumerable<System.ValueTuple<TKey, TValue>> source)`
- `[ext] public static System.Collections.Generic.Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> source, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this System.Collections.Generic.IEnumerable<System.ValueTuple<TKey, TValue>> source, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector)`
- `[ext] public static System.Collections.Generic.Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector)`
- `[ext] public static System.Collections.Generic.Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.HashSet<TSource> ToHashSet<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `[ext] public static System.Collections.Generic.HashSet<TSource> ToHashSet<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Collections.Generic.IEqualityComparer<TSource> comparer)`
- `[ext] public static System.Collections.Generic.List<TSource> ToList<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `[ext] public static System.Linq.ILookup<TKey, TSource> ToLookup<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector)`
- `[ext] public static System.Linq.ILookup<TKey, TSource> ToLookup<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Linq.ILookup<TKey, TElement> ToLookup<TSource, TKey, TElement>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector)`
- `[ext] public static System.Linq.ILookup<TKey, TElement> ToLookup<TSource, TKey, TElement>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Boolean TryGetNonEnumeratedCount<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, out System.Int32& count)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> Union<TSource>(this System.Collections.Generic.IEnumerable<TSource> first, System.Collections.Generic.IEnumerable<TSource> second)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> Union<TSource>(this System.Collections.Generic.IEnumerable<TSource> first, System.Collections.Generic.IEnumerable<TSource> second, System.Collections.Generic.IEqualityComparer<TSource> comparer)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> UnionBy<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> first, System.Collections.Generic.IEnumerable<TSource> second, System.Func<TSource, TKey> keySelector)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> UnionBy<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> first, System.Collections.Generic.IEnumerable<TSource> second, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> Where<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> Where<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Int32, System.Boolean> predicate)`
- `[ext] public static System.Collections.Generic.IEnumerable<System.ValueTuple<TFirst, TSecond>> Zip<TFirst, TSecond>(this System.Collections.Generic.IEnumerable<TFirst> first, System.Collections.Generic.IEnumerable<TSecond> second)`
- `[ext] public static System.Collections.Generic.IEnumerable<System.ValueTuple<TFirst, TSecond, TThird>> Zip<TFirst, TSecond, TThird>(this System.Collections.Generic.IEnumerable<TFirst> first, System.Collections.Generic.IEnumerable<TSecond> second, System.Collections.Generic.IEnumerable<TThird> third)`
- `[ext] public static System.Collections.Generic.IEnumerable<TResult> Zip<TFirst, TSecond, TResult>(this System.Collections.Generic.IEnumerable<TFirst> first, System.Collections.Generic.IEnumerable<TSecond> second, System.Func<TFirst, TSecond, TResult> resultSelector)`

### IGrouping`2<TKey, TElement> (interface) : System.Collections.Generic.IEnumerable<TElement>, System.Collections.IEnumerable

- `public TKey Key { get; }`

### IOrderedEnumerable`1<TElement> (interface) : System.Collections.Generic.IEnumerable<TElement>, System.Collections.IEnumerable

- `public System.Linq.IOrderedEnumerable<TElement> CreateOrderedEnumerable<TKey>(System.Func<TElement, TKey> keySelector, System.Collections.Generic.IComparer<TKey> comparer, System.Boolean descending)`

### Lookup`2<TKey, TElement> (class) : System.Collections.Generic.IEnumerable<System.Linq.IGrouping<TKey, TElement>>, System.Collections.IEnumerable, System.Linq.ILookup<TKey, TElement>

- `public System.Int32 Count { get; }`
- `public System.Collections.Generic.IEnumerable<TElement> Item(TKey key) { get; }`
- `public System.Collections.Generic.IEnumerable<TResult> ApplyResultSelector<TResult>(System.Func<TKey, System.Collections.Generic.IEnumerable<TElement>, TResult> resultSelector)`
- `public System.Boolean Contains(TKey key)`
- `public System.Collections.Generic.IEnumerator<System.Linq.IGrouping<TKey, TElement>> GetEnumerator()`

