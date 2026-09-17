# Public API inventory: fsharp-core

Assemblies: FSharp.Core 10.1.0.0

Type count: 102

## Microsoft.FSharp.Collections

### ArrayModule (class [static])

- `public static System.Tuple<T1, T2>[] AllPairs<T1, T2>(T1[] array1, T2[] array2)`
- `public static T[] Append<T>(T[] array1, T[] array2)`
- `public static T Average<T>(T[] array)`
- `public static T Average$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<System.Int32, T>> divideByInt, Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, T> get_Zero, Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<T, T>> op_Addition, T[] array)`
- `public static TResult AverageBy<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, TResult> projection, T[] array)`
- `public static TResult AverageBy$W<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<TResult, Microsoft.FSharp.Core.FSharpFunc<System.Int32, TResult>> divideByInt, Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, TResult> get_Zero, Microsoft.FSharp.Core.FSharpFunc<TResult, Microsoft.FSharp.Core.FSharpFunc<TResult, TResult>> op_Addition, Microsoft.FSharp.Core.FSharpFunc<T, TResult> projection, T[] array)`
- `public static TResult[] Choose<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpOption<TResult>> chooser, T[] array)`
- `public static T[][] ChunkBySize<T>(System.Int32 chunkSize, T[] array)`
- `public static TResult[] Collect<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, TResult[]> mapping, T[] array)`
- `public static System.Int32 CompareWith<T>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<T, System.Int32>> comparer, T[] array1, T[] array2)`
- `public static T[] Concat<T>(System.Collections.Generic.IEnumerable<T[]> arrays)`
- `public static System.Boolean Contains<T>(T value, T[] array)`
- `public static T[] Copy<T>(T[] array)`
- `public static System.Void CopyTo<T>(T[] source, System.Int32 sourceIndex, T[] target, System.Int32 targetIndex, System.Int32 count)`
- `public static System.Tuple<TKey, System.Int32>[] CountBy<T, TKey>(Microsoft.FSharp.Core.FSharpFunc<T, TKey> projection, T[] array)`
- `public static T[] Create<T>(System.Int32 count, T value)`
- `public static T[] Distinct<T>(T[] array)`
- `public static T[] DistinctBy<T, TKey>(Microsoft.FSharp.Core.FSharpFunc<T, TKey> projection, T[] array)`
- `public static T[] Empty<T>()`
- `public static T ExactlyOne<T>(T[] array)`
- `public static T[] Except<T>(System.Collections.Generic.IEnumerable<T> itemsToExclude, T[] array)`
- `public static System.Boolean Exists<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, T[] array)`
- `public static System.Boolean Exists2<T1, T2>(Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, System.Boolean>> predicate, T1[] array1, T2[] array2)`
- `public static System.Void Fill<T>(T[] target, System.Int32 targetIndex, System.Int32 count, T value)`
- `public static T[] Filter<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, T[] array)`
- `public static T Find<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, T[] array)`
- `public static T FindBack<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, T[] array)`
- `public static System.Int32 FindIndex<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, T[] array)`
- `public static System.Int32 FindIndexBack<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, T[] array)`
- `public static TState Fold<T, TState>(Microsoft.FSharp.Core.FSharpFunc<TState, Microsoft.FSharp.Core.FSharpFunc<T, TState>> folder, TState state, T[] array)`
- `public static TState Fold2<T1, T2, TState>(Microsoft.FSharp.Core.FSharpFunc<TState, Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, TState>>> folder, TState state, T1[] array1, T2[] array2)`
- `public static TState FoldBack<T, TState>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<TState, TState>> folder, T[] array, TState state)`
- `public static TState FoldBack2<T1, T2, TState>(Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, Microsoft.FSharp.Core.FSharpFunc<TState, TState>>> folder, T1[] array1, T2[] array2, TState state)`
- `public static System.Boolean ForAll<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, T[] array)`
- `public static System.Boolean ForAll2<T1, T2>(Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, System.Boolean>> predicate, T1[] array1, T2[] array2)`
- `public static T Get<T>(T[] array, System.Int32 index)`
- `public static T[] GetSubArray<T>(T[] array, System.Int32 startIndex, System.Int32 count)`
- `public static System.Tuple<TKey, T[]>[] GroupBy<T, TKey>(Microsoft.FSharp.Core.FSharpFunc<T, TKey> projection, T[] array)`
- `public static T Head<T>(T[] array)`
- `public static System.Tuple<System.Int32, T>[] Indexed<T>(T[] array)`
- `public static T[] Initialize<T>(System.Int32 count, Microsoft.FSharp.Core.FSharpFunc<System.Int32, T> initializer)`
- `public static T[] InsertAt<T>(System.Int32 index, T value, T[] source)`
- `public static T[] InsertManyAt<T>(System.Int32 index, System.Collections.Generic.IEnumerable<T> values, T[] source)`
- `public static System.Boolean IsEmpty<T>(T[] array)`
- `public static T Item<T>(System.Int32 index, T[] array)`
- `public static System.Void Iterate<T>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.Unit> action, T[] array)`
- `public static System.Void Iterate2<T1, T2>(Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, Microsoft.FSharp.Core.Unit>> action, T1[] array1, T2[] array2)`
- `public static System.Void IterateIndexed<T>(Microsoft.FSharp.Core.FSharpFunc<System.Int32, Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.Unit>> action, T[] array)`
- `public static System.Void IterateIndexed2<T1, T2>(Microsoft.FSharp.Core.FSharpFunc<System.Int32, Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, Microsoft.FSharp.Core.Unit>>> action, T1[] array1, T2[] array2)`
- `public static T Last<T>(T[] array)`
- `public static System.Int32 Length<T>(T[] array)`
- `public static TResult[] Map<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, TResult> mapping, T[] array)`
- `public static TResult[] Map2<T1, T2, TResult>(Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, TResult>> mapping, T1[] array1, T2[] array2)`
- `public static TResult[] Map3<T1, T2, T3, TResult>(Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, Microsoft.FSharp.Core.FSharpFunc<T3, TResult>>> mapping, T1[] array1, T2[] array2, T3[] array3)`
- `public static System.Tuple<TResult[], TState> MapFold<T, TState, TResult>(Microsoft.FSharp.Core.FSharpFunc<TState, Microsoft.FSharp.Core.FSharpFunc<T, System.Tuple<TResult, TState>>> mapping, TState state, T[] array)`
- `public static System.Tuple<TResult[], TState> MapFoldBack<T, TState, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<TState, System.Tuple<TResult, TState>>> mapping, T[] array, TState state)`
- `public static TResult[] MapIndexed<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<System.Int32, Microsoft.FSharp.Core.FSharpFunc<T, TResult>> mapping, T[] array)`
- `public static TResult[] MapIndexed2<T1, T2, TResult>(Microsoft.FSharp.Core.FSharpFunc<System.Int32, Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, TResult>>> mapping, T1[] array1, T2[] array2)`
- `public static T Max<T>(T[] array)`
- `public static T MaxBy<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, TResult> projection, T[] array)`
- `public static T Min<T>(T[] array)`
- `public static T MinBy<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, TResult> projection, T[] array)`
- `public static T[] OfList<T>(Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static T[] OfSeq<T>(System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Tuple<T, T>[] Pairwise<T>(T[] array)`
- `public static System.Tuple<T[], T[]> Partition<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, T[] array)`
- `public static System.Tuple<T1[], T2[]> PartitionWith<T, T1, T2>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpChoice<T1, T2>> partitioner, T[] array)`
- `public static T[] Permute<T>(Microsoft.FSharp.Core.FSharpFunc<System.Int32, System.Int32> indexMap, T[] array)`
- `public static TResult Pick<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpOption<TResult>> chooser, T[] array)`
- `public static T RandomChoice<T>(T[] source)`
- `public static T RandomChoiceBy<T>(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, System.Double> randomizer, T[] source)`
- `public static T RandomChoiceWith<T>(System.Random random, T[] source)`
- `public static T[] RandomChoices<T>(System.Int32 count, T[] source)`
- `public static T[] RandomChoicesBy<T>(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, System.Double> randomizer, System.Int32 count, T[] source)`
- `public static T[] RandomChoicesWith<T>(System.Random random, System.Int32 count, T[] source)`
- `public static T[] RandomSample<T>(System.Int32 count, T[] source)`
- `public static T[] RandomSampleBy<T>(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, System.Double> randomizer, System.Int32 count, T[] source)`
- `public static T[] RandomSampleWith<T>(System.Random random, System.Int32 count, T[] source)`
- `public static T[] RandomShuffle<T>(T[] source)`
- `public static T[] RandomShuffleBy<T>(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, System.Double> randomizer, T[] source)`
- `public static System.Void RandomShuffleInPlace<T>(T[] source)`
- `public static System.Void RandomShuffleInPlaceBy<T>(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, System.Double> randomizer, T[] source)`
- `public static System.Void RandomShuffleInPlaceWith<T>(System.Random random, T[] source)`
- `public static T[] RandomShuffleWith<T>(System.Random random, T[] source)`
- `public static T Reduce<T>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<T, T>> reduction, T[] array)`
- `public static T ReduceBack<T>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<T, T>> reduction, T[] array)`
- `public static T[] RemoveAt<T>(System.Int32 index, T[] source)`
- `public static T[] RemoveManyAt<T>(System.Int32 index, System.Int32 count, T[] source)`
- `public static T[] Replicate<T>(System.Int32 count, T initial)`
- `public static T[] Reverse<T>(T[] array)`
- `public static TState[] Scan<T, TState>(Microsoft.FSharp.Core.FSharpFunc<TState, Microsoft.FSharp.Core.FSharpFunc<T, TState>> folder, TState state, T[] array)`
- `public static TState[] ScanBack<T, TState>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<TState, TState>> folder, T[] array, TState state)`
- `public static System.Void Set<T>(T[] array, System.Int32 index, T value)`
- `public static T[] Singleton<T>(T value)`
- `public static T[] Skip<T>(System.Int32 count, T[] array)`
- `public static T[] SkipWhile<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, T[] array)`
- `public static T[] Sort<T>(T[] array)`
- `public static T[] SortBy<T, TKey>(Microsoft.FSharp.Core.FSharpFunc<T, TKey> projection, T[] array)`
- `public static T[] SortByDescending<T, TKey>(Microsoft.FSharp.Core.FSharpFunc<T, TKey> projection, T[] array)`
- `public static T[] SortDescending<T>(T[] array)`
- `public static System.Void SortInPlace<T>(T[] array)`
- `public static System.Void SortInPlaceBy<T, TKey>(Microsoft.FSharp.Core.FSharpFunc<T, TKey> projection, T[] array)`
- `public static System.Void SortInPlaceWith<T>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<T, System.Int32>> comparer, T[] array)`
- `public static T[] SortWith<T>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<T, System.Int32>> comparer, T[] array)`
- `public static System.Tuple<T[], T[]> SplitAt<T>(System.Int32 index, T[] array)`
- `public static T[][] SplitInto<T>(System.Int32 count, T[] array)`
- `public static T Sum<T>(T[] array)`
- `public static T Sum$W<T>(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, T> get_Zero, Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<T, T>> op_Addition, T[] array)`
- `public static TResult SumBy<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, TResult> projection, T[] array)`
- `public static TResult SumBy$W<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, TResult> get_Zero, Microsoft.FSharp.Core.FSharpFunc<TResult, Microsoft.FSharp.Core.FSharpFunc<TResult, TResult>> op_Addition, Microsoft.FSharp.Core.FSharpFunc<T, TResult> projection, T[] array)`
- `public static T[] Tail<T>(T[] array)`
- `public static T[] Take<T>(System.Int32 count, T[] array)`
- `public static T[] TakeWhile<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, T[] array)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> ToList<T>(T[] array)`
- `public static System.Collections.Generic.IEnumerable<T> ToSeq<T>(T[] array)`
- `public static T[][] Transpose<T>(System.Collections.Generic.IEnumerable<T[]> arrays)`
- `public static T[] Truncate<T>(System.Int32 count, T[] array)`
- `public static Microsoft.FSharp.Core.FSharpOption<T> TryExactlyOne<T>(T[] array)`
- `public static Microsoft.FSharp.Core.FSharpOption<T> TryFind<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, T[] array)`
- `public static Microsoft.FSharp.Core.FSharpOption<T> TryFindBack<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, T[] array)`
- `public static Microsoft.FSharp.Core.FSharpOption<System.Int32> TryFindIndex<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, T[] array)`
- `public static Microsoft.FSharp.Core.FSharpOption<System.Int32> TryFindIndexBack<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, T[] array)`
- `public static Microsoft.FSharp.Core.FSharpOption<T> TryHead<T>(T[] array)`
- `public static Microsoft.FSharp.Core.FSharpOption<T> TryItem<T>(System.Int32 index, T[] array)`
- `public static Microsoft.FSharp.Core.FSharpOption<T> TryLast<T>(T[] array)`
- `public static Microsoft.FSharp.Core.FSharpOption<TResult> TryPick<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpOption<TResult>> chooser, T[] array)`
- `public static T[] Unfold<T, TState>(Microsoft.FSharp.Core.FSharpFunc<TState, Microsoft.FSharp.Core.FSharpOption<System.Tuple<T, TState>>> generator, TState state)`
- `public static System.Tuple<T1[], T2[]> Unzip<T1, T2>(System.Tuple<T1, T2>[] array)`
- `public static System.Tuple<T1[], T2[], T3[]> Unzip3<T1, T2, T3>(System.Tuple<T1, T2, T3>[] array)`
- `public static T[] UpdateAt<T>(System.Int32 index, T value, T[] source)`
- `public static T[] Where<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, T[] array)`
- `public static T[][] Windowed<T>(System.Int32 windowSize, T[] array)`
- `public static T[] ZeroCreate<T>(System.Int32 count)`
- `public static System.Tuple<T1, T2>[] Zip<T1, T2>(T1[] array1, T2[] array2)`
- `public static System.Tuple<T1, T2, T3>[] Zip3<T1, T2, T3>(T1[] array1, T2[] array2, T3[] array3)`

### FSharpList (class [static])

- `public static Microsoft.FSharp.Collections.FSharpList<T> Create<T>(System.ReadOnlySpan<T> items)`

### FSharpList`1<T> (class [sealed]) : System.IEquatable<Microsoft.FSharp.Collections.FSharpList<T>>, System.Collections.IStructuralEquatable, System.IComparable<Microsoft.FSharp.Collections.FSharpList<T>>, System.IComparable, System.Collections.IStructuralComparable, System.Collections.Generic.IReadOnlyList<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable

- `public FSharpList`1(T head, Microsoft.FSharp.Collections.FSharpList<T> tail)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> Empty { get; } (nullability: Unknown)`
- `public T Head { get; } (nullability: Unknown)`
- `public T HeadOrDefault { get; } (nullability: Unknown)`
- `public System.Boolean IsCons { get; }`
- `public System.Boolean IsEmpty { get; }`
- `public T Item(System.Int32 index) { get; } (nullability: Unknown)`
- `public System.Int32 Length { get; }`
- `public System.Int32 Tag { get; }`
- `public Microsoft.FSharp.Collections.FSharpList<T> Tail { get; } (nullability: Unknown)`
- `public Microsoft.FSharp.Collections.FSharpList<T> TailOrNull { get; } (nullability: Unknown)`
- `public System.Int32 CompareTo(Microsoft.FSharp.Collections.FSharpList<T> obj)`
- `public System.Int32 CompareTo(System.Object obj)`
- `public System.Int32 CompareTo(System.Object obj, System.Collections.IComparer comp)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> Cons(T head, Microsoft.FSharp.Collections.FSharpList<T> tail)`
- `public System.Boolean Equals(Microsoft.FSharp.Collections.FSharpList<T> obj)`
- `public System.Boolean Equals(System.Object obj)`
- `public System.Boolean Equals(Microsoft.FSharp.Collections.FSharpList<T> obj, System.Collections.IEqualityComparer comp)`
- `public System.Boolean Equals(System.Object obj, System.Collections.IEqualityComparer comp)`
- `public System.Int32 GetHashCode()`
- `public System.Int32 GetHashCode(System.Collections.IEqualityComparer comp)`
- `public System.Int32 GetReverseIndex(System.Int32 rank, System.Int32 offset)`
- `public Microsoft.FSharp.Collections.FSharpList<T> GetSlice(Microsoft.FSharp.Core.FSharpOption<System.Int32> startIndex, Microsoft.FSharp.Core.FSharpOption<System.Int32> endIndex)`
- `public System.String ToString()`
- `public static Microsoft.FSharp.Collections.FSharpList<T> get_Empty()`
- `public T get_HeadOrDefault()`
- `public System.Boolean get_IsCons()`
- `public System.Boolean get_IsEmpty()`
- `public System.Int32 get_Tag()`
- `public Microsoft.FSharp.Collections.FSharpList<T> get_TailOrNull()`

### FSharpMap`2<TKey, TValue> (class [sealed]) : System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.IEnumerable, System.IComparable, System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IDictionary<TKey, TValue>, System.Collections.IStructuralEquatable

- `public FSharpMap`2(System.Collections.Generic.IEnumerable<System.Tuple<TKey, TValue>> elements)`
- `public System.Int32 Count { get; }`
- `public System.Boolean IsEmpty { get; }`
- `public TValue Item(TKey key) { get; } (nullability: Unknown)`
- `public System.Collections.Generic.ICollection<TKey> Keys { get; } (nullability: Unknown)`
- `public System.Collections.Generic.ICollection<TValue> Values { get; } (nullability: Unknown)`
- `public Microsoft.FSharp.Collections.FSharpMap<TKey, TValue> Add(TKey key, TValue value)`
- `public Microsoft.FSharp.Collections.FSharpMap<TKey, TValue> Change(TKey key, Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.FSharpOption<TValue>, Microsoft.FSharp.Core.FSharpOption<TValue>> f)`
- `public System.Boolean ContainsKey(TKey key)`
- `public System.Boolean Equals(System.Object that)`
- `public System.Int32 GetHashCode()`
- `public Microsoft.FSharp.Collections.FSharpMap<TKey, TValue> Remove(TKey key)`
- `public System.String ToString()`
- `public Microsoft.FSharp.Core.FSharpOption<TValue> TryFind(TKey key)`
- `public System.Boolean TryGetValue(TKey key, out TValue& value)`

### FSharpSet (class [static])

- `public static Microsoft.FSharp.Collections.FSharpSet<T> Create<T>(System.ReadOnlySpan<T> items)`

### FSharpSet`1<T> (class [sealed]) : System.Collections.IEnumerable, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.ICollection<T>, System.Collections.IStructuralEquatable, System.IComparable

- `public FSharpSet`1(System.Collections.Generic.IEnumerable<T> elements)`
- `public System.Int32 Count { get; }`
- `public System.Boolean IsEmpty { get; }`
- `public T MaximumElement { get; } (nullability: Unknown)`
- `public T MinimumElement { get; } (nullability: Unknown)`
- `public Microsoft.FSharp.Collections.FSharpSet<T> Add(T value)`
- `public System.Boolean Contains(T value)`
- `public System.Boolean Equals(System.Object that)`
- `public System.Int32 GetHashCode()`
- `public System.Boolean IsProperSubsetOf(Microsoft.FSharp.Collections.FSharpSet<T> otherSet)`
- `public System.Boolean IsProperSupersetOf(Microsoft.FSharp.Collections.FSharpSet<T> otherSet)`
- `public System.Boolean IsSubsetOf(Microsoft.FSharp.Collections.FSharpSet<T> otherSet)`
- `public System.Boolean IsSupersetOf(Microsoft.FSharp.Collections.FSharpSet<T> otherSet)`
- `public Microsoft.FSharp.Collections.FSharpSet<T> Remove(T value)`
- `public System.String ToString()`
- `public static Microsoft.FSharp.Collections.FSharpSet<T> op_Addition(Microsoft.FSharp.Collections.FSharpSet<T> set1, Microsoft.FSharp.Collections.FSharpSet<T> set2)`
- `public static Microsoft.FSharp.Collections.FSharpSet<T> op_Subtraction(Microsoft.FSharp.Collections.FSharpSet<T> set1, Microsoft.FSharp.Collections.FSharpSet<T> set2)`

### ListModule (class [static])

- `public static Microsoft.FSharp.Collections.FSharpList<System.Tuple<T1, T2>> AllPairs<T1, T2>(Microsoft.FSharp.Collections.FSharpList<T1> list1, Microsoft.FSharp.Collections.FSharpList<T2> list2)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> Append<T>(Microsoft.FSharp.Collections.FSharpList<T> list1, Microsoft.FSharp.Collections.FSharpList<T> list2)`
- `public static T Average<T>(Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static T Average$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<System.Int32, T>> divideByInt, Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, T> get_Zero, Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<T, T>> op_Addition, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static TResult AverageBy<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, TResult> projection, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static TResult AverageBy$W<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<TResult, Microsoft.FSharp.Core.FSharpFunc<System.Int32, TResult>> divideByInt, Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, TResult> get_Zero, Microsoft.FSharp.Core.FSharpFunc<TResult, Microsoft.FSharp.Core.FSharpFunc<TResult, TResult>> op_Addition, Microsoft.FSharp.Core.FSharpFunc<T, TResult> projection, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static Microsoft.FSharp.Collections.FSharpList<TResult> Choose<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpOption<TResult>> chooser, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static Microsoft.FSharp.Collections.FSharpList<Microsoft.FSharp.Collections.FSharpList<T>> ChunkBySize<T>(System.Int32 chunkSize, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static Microsoft.FSharp.Collections.FSharpList<TResult> Collect<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Collections.FSharpList<TResult>> mapping, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static System.Int32 CompareWith<T>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<T, System.Int32>> comparer, Microsoft.FSharp.Collections.FSharpList<T> list1, Microsoft.FSharp.Collections.FSharpList<T> list2)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> Concat<T>(System.Collections.Generic.IEnumerable<Microsoft.FSharp.Collections.FSharpList<T>> lists)`
- `public static System.Boolean Contains<T>(T value, Microsoft.FSharp.Collections.FSharpList<T> source)`
- `public static Microsoft.FSharp.Collections.FSharpList<System.Tuple<TKey, System.Int32>> CountBy<T, TKey>(Microsoft.FSharp.Core.FSharpFunc<T, TKey> projection, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> Distinct<T>(Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> DistinctBy<T, TKey>(Microsoft.FSharp.Core.FSharpFunc<T, TKey> projection, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> Empty<T>()`
- `public static T ExactlyOne<T>(Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> Except<T>(System.Collections.Generic.IEnumerable<T> itemsToExclude, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static System.Boolean Exists<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static System.Boolean Exists2<T1, T2>(Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, System.Boolean>> predicate, Microsoft.FSharp.Collections.FSharpList<T1> list1, Microsoft.FSharp.Collections.FSharpList<T2> list2)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> Filter<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static T Find<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static T FindBack<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static System.Int32 FindIndex<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static System.Int32 FindIndexBack<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static TState Fold<T, TState>(Microsoft.FSharp.Core.FSharpFunc<TState, Microsoft.FSharp.Core.FSharpFunc<T, TState>> folder, TState state, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static TState Fold2<T1, T2, TState>(Microsoft.FSharp.Core.FSharpFunc<TState, Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, TState>>> folder, TState state, Microsoft.FSharp.Collections.FSharpList<T1> list1, Microsoft.FSharp.Collections.FSharpList<T2> list2)`
- `public static TState FoldBack<T, TState>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<TState, TState>> folder, Microsoft.FSharp.Collections.FSharpList<T> list, TState state)`
- `public static TState FoldBack2<T1, T2, TState>(Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, Microsoft.FSharp.Core.FSharpFunc<TState, TState>>> folder, Microsoft.FSharp.Collections.FSharpList<T1> list1, Microsoft.FSharp.Collections.FSharpList<T2> list2, TState state)`
- `public static System.Boolean ForAll<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static System.Boolean ForAll2<T1, T2>(Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, System.Boolean>> predicate, Microsoft.FSharp.Collections.FSharpList<T1> list1, Microsoft.FSharp.Collections.FSharpList<T2> list2)`
- `public static T Get<T>(Microsoft.FSharp.Collections.FSharpList<T> list, System.Int32 index)`
- `public static Microsoft.FSharp.Collections.FSharpList<System.Tuple<TKey, Microsoft.FSharp.Collections.FSharpList<T>>> GroupBy<T, TKey>(Microsoft.FSharp.Core.FSharpFunc<T, TKey> projection, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static T Head<T>(Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static Microsoft.FSharp.Collections.FSharpList<System.Tuple<System.Int32, T>> Indexed<T>(Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> Initialize<T>(System.Int32 length, Microsoft.FSharp.Core.FSharpFunc<System.Int32, T> initializer)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> InsertAt<T>(System.Int32 index, T value, Microsoft.FSharp.Collections.FSharpList<T> source)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> InsertManyAt<T>(System.Int32 index, System.Collections.Generic.IEnumerable<T> values, Microsoft.FSharp.Collections.FSharpList<T> source)`
- `public static System.Boolean IsEmpty<T>(Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static T Item<T>(System.Int32 index, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static System.Void Iterate<T>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.Unit> action, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static System.Void Iterate2<T1, T2>(Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, Microsoft.FSharp.Core.Unit>> action, Microsoft.FSharp.Collections.FSharpList<T1> list1, Microsoft.FSharp.Collections.FSharpList<T2> list2)`
- `public static System.Void IterateIndexed<T>(Microsoft.FSharp.Core.FSharpFunc<System.Int32, Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.Unit>> action, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static System.Void IterateIndexed2<T1, T2>(Microsoft.FSharp.Core.FSharpFunc<System.Int32, Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, Microsoft.FSharp.Core.Unit>>> action, Microsoft.FSharp.Collections.FSharpList<T1> list1, Microsoft.FSharp.Collections.FSharpList<T2> list2)`
- `public static T Last<T>(Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static System.Int32 Length<T>(Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static Microsoft.FSharp.Collections.FSharpList<TResult> Map<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, TResult> mapping, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static Microsoft.FSharp.Collections.FSharpList<TResult> Map2<T1, T2, TResult>(Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, TResult>> mapping, Microsoft.FSharp.Collections.FSharpList<T1> list1, Microsoft.FSharp.Collections.FSharpList<T2> list2)`
- `public static Microsoft.FSharp.Collections.FSharpList<TResult> Map3<T1, T2, T3, TResult>(Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, Microsoft.FSharp.Core.FSharpFunc<T3, TResult>>> mapping, Microsoft.FSharp.Collections.FSharpList<T1> list1, Microsoft.FSharp.Collections.FSharpList<T2> list2, Microsoft.FSharp.Collections.FSharpList<T3> list3)`
- `public static System.Tuple<Microsoft.FSharp.Collections.FSharpList<TResult>, TState> MapFold<T, TState, TResult>(Microsoft.FSharp.Core.FSharpFunc<TState, Microsoft.FSharp.Core.FSharpFunc<T, System.Tuple<TResult, TState>>> mapping, TState state, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static System.Tuple<Microsoft.FSharp.Collections.FSharpList<TResult>, TState> MapFoldBack<T, TState, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<TState, System.Tuple<TResult, TState>>> mapping, Microsoft.FSharp.Collections.FSharpList<T> list, TState state)`
- `public static Microsoft.FSharp.Collections.FSharpList<TResult> MapIndexed<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<System.Int32, Microsoft.FSharp.Core.FSharpFunc<T, TResult>> mapping, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static Microsoft.FSharp.Collections.FSharpList<TResult> MapIndexed2<T1, T2, TResult>(Microsoft.FSharp.Core.FSharpFunc<System.Int32, Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, TResult>>> mapping, Microsoft.FSharp.Collections.FSharpList<T1> list1, Microsoft.FSharp.Collections.FSharpList<T2> list2)`
- `public static T Max<T>(Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static T MaxBy<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, TResult> projection, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static T Min<T>(Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static T MinBy<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, TResult> projection, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> OfArray<T>(T[] array)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> OfSeq<T>(System.Collections.Generic.IEnumerable<T> source)`
- `public static Microsoft.FSharp.Collections.FSharpList<System.Tuple<T, T>> Pairwise<T>(Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static System.Tuple<Microsoft.FSharp.Collections.FSharpList<T>, Microsoft.FSharp.Collections.FSharpList<T>> Partition<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static System.Tuple<Microsoft.FSharp.Collections.FSharpList<T1>, Microsoft.FSharp.Collections.FSharpList<T2>> PartitionWith<T, T1, T2>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpChoice<T1, T2>> partitioner, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> Permute<T>(Microsoft.FSharp.Core.FSharpFunc<System.Int32, System.Int32> indexMap, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static TResult Pick<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpOption<TResult>> chooser, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static T RandomChoice<T>(Microsoft.FSharp.Collections.FSharpList<T> source)`
- `public static T RandomChoiceBy<T>(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, System.Double> randomizer, Microsoft.FSharp.Collections.FSharpList<T> source)`
- `public static T RandomChoiceWith<T>(System.Random random, Microsoft.FSharp.Collections.FSharpList<T> source)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> RandomChoices<T>(System.Int32 count, Microsoft.FSharp.Collections.FSharpList<T> source)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> RandomChoicesBy<T>(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, System.Double> randomizer, System.Int32 count, Microsoft.FSharp.Collections.FSharpList<T> source)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> RandomChoicesWith<T>(System.Random random, System.Int32 count, Microsoft.FSharp.Collections.FSharpList<T> source)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> RandomSample<T>(System.Int32 count, Microsoft.FSharp.Collections.FSharpList<T> source)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> RandomSampleBy<T>(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, System.Double> randomizer, System.Int32 count, Microsoft.FSharp.Collections.FSharpList<T> source)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> RandomSampleWith<T>(System.Random random, System.Int32 count, Microsoft.FSharp.Collections.FSharpList<T> source)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> RandomShuffle<T>(Microsoft.FSharp.Collections.FSharpList<T> source)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> RandomShuffleBy<T>(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, System.Double> randomizer, Microsoft.FSharp.Collections.FSharpList<T> source)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> RandomShuffleWith<T>(System.Random random, Microsoft.FSharp.Collections.FSharpList<T> source)`
- `public static T Reduce<T>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<T, T>> reduction, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static T ReduceBack<T>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<T, T>> reduction, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> RemoveAt<T>(System.Int32 index, Microsoft.FSharp.Collections.FSharpList<T> source)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> RemoveManyAt<T>(System.Int32 index, System.Int32 count, Microsoft.FSharp.Collections.FSharpList<T> source)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> Replicate<T>(System.Int32 count, T initial)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> Reverse<T>(Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static Microsoft.FSharp.Collections.FSharpList<TState> Scan<T, TState>(Microsoft.FSharp.Core.FSharpFunc<TState, Microsoft.FSharp.Core.FSharpFunc<T, TState>> folder, TState state, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static Microsoft.FSharp.Collections.FSharpList<TState> ScanBack<T, TState>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<TState, TState>> folder, Microsoft.FSharp.Collections.FSharpList<T> list, TState state)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> Singleton<T>(T value)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> Skip<T>(System.Int32 count, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> SkipWhile<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> Sort<T>(Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> SortBy<T, TKey>(Microsoft.FSharp.Core.FSharpFunc<T, TKey> projection, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> SortByDescending<T, TKey>(Microsoft.FSharp.Core.FSharpFunc<T, TKey> projection, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> SortDescending<T>(Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> SortWith<T>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<T, System.Int32>> comparer, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static System.Tuple<Microsoft.FSharp.Collections.FSharpList<T>, Microsoft.FSharp.Collections.FSharpList<T>> SplitAt<T>(System.Int32 index, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static Microsoft.FSharp.Collections.FSharpList<Microsoft.FSharp.Collections.FSharpList<T>> SplitInto<T>(System.Int32 count, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static T Sum<T>(Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static T Sum$W<T>(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, T> get_Zero, Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<T, T>> op_Addition, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static TResult SumBy<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, TResult> projection, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static TResult SumBy$W<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, TResult> get_Zero, Microsoft.FSharp.Core.FSharpFunc<TResult, Microsoft.FSharp.Core.FSharpFunc<TResult, TResult>> op_Addition, Microsoft.FSharp.Core.FSharpFunc<T, TResult> projection, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> Tail<T>(Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> Take<T>(System.Int32 count, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> TakeWhile<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static T[] ToArray<T>(Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static System.Collections.Generic.IEnumerable<T> ToSeq<T>(Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static Microsoft.FSharp.Collections.FSharpList<Microsoft.FSharp.Collections.FSharpList<T>> Transpose<T>(System.Collections.Generic.IEnumerable<Microsoft.FSharp.Collections.FSharpList<T>> lists)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> Truncate<T>(System.Int32 count, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static Microsoft.FSharp.Core.FSharpOption<T> TryExactlyOne<T>(Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static Microsoft.FSharp.Core.FSharpOption<T> TryFind<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static Microsoft.FSharp.Core.FSharpOption<T> TryFindBack<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static Microsoft.FSharp.Core.FSharpOption<System.Int32> TryFindIndex<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static Microsoft.FSharp.Core.FSharpOption<System.Int32> TryFindIndexBack<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static Microsoft.FSharp.Core.FSharpOption<T> TryHead<T>(Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static Microsoft.FSharp.Core.FSharpOption<T> TryItem<T>(System.Int32 index, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static Microsoft.FSharp.Core.FSharpOption<T> TryLast<T>(Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static Microsoft.FSharp.Core.FSharpOption<TResult> TryPick<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpOption<TResult>> chooser, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> Unfold<T, TState>(Microsoft.FSharp.Core.FSharpFunc<TState, Microsoft.FSharp.Core.FSharpOption<System.Tuple<T, TState>>> generator, TState state)`
- `public static System.Tuple<Microsoft.FSharp.Collections.FSharpList<T1>, Microsoft.FSharp.Collections.FSharpList<T2>> Unzip<T1, T2>(Microsoft.FSharp.Collections.FSharpList<System.Tuple<T1, T2>> list)`
- `public static System.Tuple<Microsoft.FSharp.Collections.FSharpList<T1>, Microsoft.FSharp.Collections.FSharpList<T2>, Microsoft.FSharp.Collections.FSharpList<T3>> Unzip3<T1, T2, T3>(Microsoft.FSharp.Collections.FSharpList<System.Tuple<T1, T2, T3>> list)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> UpdateAt<T>(System.Int32 index, T value, Microsoft.FSharp.Collections.FSharpList<T> source)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> Where<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static Microsoft.FSharp.Collections.FSharpList<Microsoft.FSharp.Collections.FSharpList<T>> Windowed<T>(System.Int32 windowSize, Microsoft.FSharp.Collections.FSharpList<T> list)`
- `public static Microsoft.FSharp.Collections.FSharpList<System.Tuple<T1, T2>> Zip<T1, T2>(Microsoft.FSharp.Collections.FSharpList<T1> list1, Microsoft.FSharp.Collections.FSharpList<T2> list2)`
- `public static Microsoft.FSharp.Collections.FSharpList<System.Tuple<T1, T2, T3>> Zip3<T1, T2, T3>(Microsoft.FSharp.Collections.FSharpList<T1> list1, Microsoft.FSharp.Collections.FSharpList<T2> list2, Microsoft.FSharp.Collections.FSharpList<T3> list3)`

### MapModule (class [static])

- `public static Microsoft.FSharp.Collections.FSharpMap<TKey, T> Add<TKey, T>(TKey key, T value, Microsoft.FSharp.Collections.FSharpMap<TKey, T> table)`
- `public static Microsoft.FSharp.Collections.FSharpMap<TKey, T> Change<TKey, T>(TKey key, Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.FSharpOption<T>, Microsoft.FSharp.Core.FSharpOption<T>> f, Microsoft.FSharp.Collections.FSharpMap<TKey, T> table)`
- `public static System.Boolean ContainsKey<TKey, T>(TKey key, Microsoft.FSharp.Collections.FSharpMap<TKey, T> table)`
- `public static System.Int32 Count<TKey, T>(Microsoft.FSharp.Collections.FSharpMap<TKey, T> table)`
- `public static Microsoft.FSharp.Collections.FSharpMap<TKey, T> Empty<TKey, T>()`
- `public static System.Boolean Exists<TKey, T>(Microsoft.FSharp.Core.FSharpFunc<TKey, Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean>> predicate, Microsoft.FSharp.Collections.FSharpMap<TKey, T> table)`
- `public static Microsoft.FSharp.Collections.FSharpMap<TKey, T> Filter<TKey, T>(Microsoft.FSharp.Core.FSharpFunc<TKey, Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean>> predicate, Microsoft.FSharp.Collections.FSharpMap<TKey, T> table)`
- `public static T Find<TKey, T>(TKey key, Microsoft.FSharp.Collections.FSharpMap<TKey, T> table)`
- `public static TKey FindKey<TKey, T>(Microsoft.FSharp.Core.FSharpFunc<TKey, Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean>> predicate, Microsoft.FSharp.Collections.FSharpMap<TKey, T> table)`
- `public static TState Fold<TKey, T, TState>(Microsoft.FSharp.Core.FSharpFunc<TState, Microsoft.FSharp.Core.FSharpFunc<TKey, Microsoft.FSharp.Core.FSharpFunc<T, TState>>> folder, TState state, Microsoft.FSharp.Collections.FSharpMap<TKey, T> table)`
- `public static TState FoldBack<TKey, T, TState>(Microsoft.FSharp.Core.FSharpFunc<TKey, Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<TState, TState>>> folder, Microsoft.FSharp.Collections.FSharpMap<TKey, T> table, TState state)`
- `public static System.Boolean ForAll<TKey, T>(Microsoft.FSharp.Core.FSharpFunc<TKey, Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean>> predicate, Microsoft.FSharp.Collections.FSharpMap<TKey, T> table)`
- `public static System.Boolean IsEmpty<TKey, T>(Microsoft.FSharp.Collections.FSharpMap<TKey, T> table)`
- `public static System.Void Iterate<TKey, T>(Microsoft.FSharp.Core.FSharpFunc<TKey, Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.Unit>> action, Microsoft.FSharp.Collections.FSharpMap<TKey, T> table)`
- `public static System.Collections.Generic.ICollection<TKey> Keys<TKey, T>(Microsoft.FSharp.Collections.FSharpMap<TKey, T> table)`
- `public static Microsoft.FSharp.Collections.FSharpMap<TKey, TResult> Map<TKey, T, TResult>(Microsoft.FSharp.Core.FSharpFunc<TKey, Microsoft.FSharp.Core.FSharpFunc<T, TResult>> mapping, Microsoft.FSharp.Collections.FSharpMap<TKey, T> table)`
- `public static System.Tuple<TKey, T> MaxKeyValue<TKey, T>(Microsoft.FSharp.Collections.FSharpMap<TKey, T> table)`
- `public static System.Tuple<TKey, T> MinKeyValue<TKey, T>(Microsoft.FSharp.Collections.FSharpMap<TKey, T> table)`
- `public static Microsoft.FSharp.Collections.FSharpMap<TKey, T> OfArray<TKey, T>(System.Tuple<TKey, T>[] elements)`
- `public static Microsoft.FSharp.Collections.FSharpMap<TKey, T> OfList<TKey, T>(Microsoft.FSharp.Collections.FSharpList<System.Tuple<TKey, T>> elements)`
- `public static Microsoft.FSharp.Collections.FSharpMap<TKey, T> OfSeq<TKey, T>(System.Collections.Generic.IEnumerable<System.Tuple<TKey, T>> elements)`
- `public static System.Tuple<Microsoft.FSharp.Collections.FSharpMap<TKey, T>, Microsoft.FSharp.Collections.FSharpMap<TKey, T>> Partition<TKey, T>(Microsoft.FSharp.Core.FSharpFunc<TKey, Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean>> predicate, Microsoft.FSharp.Collections.FSharpMap<TKey, T> table)`
- `public static TResult Pick<TKey, T, TResult>(Microsoft.FSharp.Core.FSharpFunc<TKey, Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpOption<TResult>>> chooser, Microsoft.FSharp.Collections.FSharpMap<TKey, T> table)`
- `public static Microsoft.FSharp.Collections.FSharpMap<TKey, T> Remove<TKey, T>(TKey key, Microsoft.FSharp.Collections.FSharpMap<TKey, T> table)`
- `public static System.Tuple<TKey, T>[] ToArray<TKey, T>(Microsoft.FSharp.Collections.FSharpMap<TKey, T> table)`
- `public static Microsoft.FSharp.Collections.FSharpList<System.Tuple<TKey, T>> ToList<TKey, T>(Microsoft.FSharp.Collections.FSharpMap<TKey, T> table)`
- `public static System.Collections.Generic.IEnumerable<System.Tuple<TKey, T>> ToSeq<TKey, T>(Microsoft.FSharp.Collections.FSharpMap<TKey, T> table)`
- `public static Microsoft.FSharp.Core.FSharpOption<T> TryFind<TKey, T>(TKey key, Microsoft.FSharp.Collections.FSharpMap<TKey, T> table)`
- `public static Microsoft.FSharp.Core.FSharpOption<TKey> TryFindKey<TKey, T>(Microsoft.FSharp.Core.FSharpFunc<TKey, Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean>> predicate, Microsoft.FSharp.Collections.FSharpMap<TKey, T> table)`
- `public static Microsoft.FSharp.Core.FSharpOption<TResult> TryPick<TKey, T, TResult>(Microsoft.FSharp.Core.FSharpFunc<TKey, Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpOption<TResult>>> chooser, Microsoft.FSharp.Collections.FSharpMap<TKey, T> table)`
- `public static System.Collections.Generic.ICollection<T> Values<TKey, T>(Microsoft.FSharp.Collections.FSharpMap<TKey, T> table)`

### Parallel (class [static])

- `public static T Average<T>(T[] array)`
- `public static T Average$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<System.Int32, T>> divideByInt, Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<T, T>> op_Addition, T[] array)`
- `public static TResult AverageBy<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, TResult> projection, T[] array)`
- `public static TResult AverageBy$W<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<TResult, Microsoft.FSharp.Core.FSharpFunc<System.Int32, TResult>> divideByInt, Microsoft.FSharp.Core.FSharpFunc<TResult, Microsoft.FSharp.Core.FSharpFunc<TResult, TResult>> op_Addition, Microsoft.FSharp.Core.FSharpFunc<T, TResult> projection, T[] array)`
- `public static TResult[] Choose<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpOption<TResult>> chooser, T[] array)`
- `public static TResult[] Collect<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, TResult[]> mapping, T[] array)`
- `public static System.Boolean Exists<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, T[] array)`
- `public static T[] Filter<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, T[] array)`
- `public static System.Boolean ForAll<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, T[] array)`
- `public static System.Tuple<TKey, T[]>[] GroupBy<T, TKey>(Microsoft.FSharp.Core.FSharpFunc<T, TKey> projection, T[] array)`
- `public static T[] Initialize<T>(System.Int32 count, Microsoft.FSharp.Core.FSharpFunc<System.Int32, T> initializer)`
- `public static System.Void Iterate<T>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.Unit> action, T[] array)`
- `public static System.Void IterateIndexed<T>(Microsoft.FSharp.Core.FSharpFunc<System.Int32, Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.Unit>> action, T[] array)`
- `public static TResult[] Map<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, TResult> mapping, T[] array)`
- `public static TResult[] MapIndexed<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<System.Int32, Microsoft.FSharp.Core.FSharpFunc<T, TResult>> mapping, T[] array)`
- `public static T Max<T>(T[] array)`
- `public static T MaxBy<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, TResult> projection, T[] array)`
- `public static T Min<T>(T[] array)`
- `public static T MinBy<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, TResult> projection, T[] array)`
- `public static System.Tuple<T[], T[]> Partition<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, T[] array)`
- `public static System.Tuple<T1[], T2[]> PartitionWith<T, T1, T2>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpChoice<T1, T2>> partitioner, T[] array)`
- `public static T Reduce<T>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<T, T>> reduction, T[] array)`
- `public static TResult ReduceBy<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, TResult> projection, Microsoft.FSharp.Core.FSharpFunc<TResult, Microsoft.FSharp.Core.FSharpFunc<TResult, TResult>> reduction, T[] array)`
- `public static T[] Sort<T>(T[] array)`
- `public static T[] SortBy<T, TKey>(Microsoft.FSharp.Core.FSharpFunc<T, TKey> projection, T[] array)`
- `public static T[] SortByDescending<T, TKey>(Microsoft.FSharp.Core.FSharpFunc<T, TKey> projection, T[] array)`
- `public static T[] SortDescending<T>(T[] array)`
- `public static System.Void SortInPlace<T>(T[] array)`
- `public static System.Void SortInPlaceBy<T, TKey>(Microsoft.FSharp.Core.FSharpFunc<T, TKey> projection, T[] array)`
- `public static System.Void SortInPlaceWith<T>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<T, System.Int32>> comparer, T[] array)`
- `public static T[] SortWith<T>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<T, System.Int32>> comparer, T[] array)`
- `public static T Sum<T>(T[] array)`
- `public static T Sum$W<T>(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, T> get_Zero, Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<T, T>> op_Addition, T[] array)`
- `public static TResult SumBy<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, TResult> projection, T[] array)`
- `public static TResult SumBy$W<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, TResult> get_Zero, Microsoft.FSharp.Core.FSharpFunc<TResult, Microsoft.FSharp.Core.FSharpFunc<TResult, TResult>> op_Addition, Microsoft.FSharp.Core.FSharpFunc<T, TResult> projection, T[] array)`
- `public static Microsoft.FSharp.Core.FSharpOption<T> TryFind<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, T[] array)`
- `public static Microsoft.FSharp.Core.FSharpOption<System.Int32> TryFindIndex<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, T[] array)`
- `public static Microsoft.FSharp.Core.FSharpOption<TResult> TryPick<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpOption<TResult>> chooser, T[] array)`
- `public static System.Tuple<T1, T2>[] Zip<T1, T2>(T1[] array1, T2[] array2)`

### SeqModule (class [static])

- `public static System.Collections.Generic.IEnumerable<System.Tuple<T1, T2>> AllPairs<T1, T2>(System.Collections.Generic.IEnumerable<T1> source1, System.Collections.Generic.IEnumerable<T2> source2)`
- `public static System.Collections.Generic.IEnumerable<T> Append<T>(System.Collections.Generic.IEnumerable<T> source1, System.Collections.Generic.IEnumerable<T> source2)`
- `public static T Average<T>(System.Collections.Generic.IEnumerable<T> source)`
- `public static T Average$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<System.Int32, T>> divideByInt, Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, T> get_Zero, Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<T, T>> op_Addition, System.Collections.Generic.IEnumerable<T> source)`
- `public static TResult AverageBy<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, TResult> projection, System.Collections.Generic.IEnumerable<T> source)`
- `public static TResult AverageBy$W<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<TResult, Microsoft.FSharp.Core.FSharpFunc<System.Int32, TResult>> divideByInt, Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, TResult> get_Zero, Microsoft.FSharp.Core.FSharpFunc<TResult, Microsoft.FSharp.Core.FSharpFunc<TResult, TResult>> op_Addition, Microsoft.FSharp.Core.FSharpFunc<T, TResult> projection, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<T> Cache<T>(System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<T> Cast<T>(System.Collections.IEnumerable source)`
- `public static System.Collections.Generic.IEnumerable<TResult> Choose<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpOption<TResult>> chooser, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<T[]> ChunkBySize<T>(System.Int32 chunkSize, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<TResult> Collect<T, TCollection, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, TCollection> mapping, System.Collections.Generic.IEnumerable<T> source)`
- `where TCollection : System.Collections.Generic.IEnumerable<TResult>`
- `public static System.Int32 CompareWith<T>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<T, System.Int32>> comparer, System.Collections.Generic.IEnumerable<T> source1, System.Collections.Generic.IEnumerable<T> source2)`
- `public static System.Collections.Generic.IEnumerable<T> Concat<TCollection, T>(System.Collections.Generic.IEnumerable<TCollection> sources)`
- `where TCollection : System.Collections.Generic.IEnumerable<T>`
- `public static System.Boolean Contains<T>(T value, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<System.Tuple<TKey, System.Int32>> CountBy<T, TKey>(Microsoft.FSharp.Core.FSharpFunc<T, TKey> projection, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<T> Delay<T>(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, System.Collections.Generic.IEnumerable<T>> generator)`
- `public static System.Collections.Generic.IEnumerable<T> Distinct<T>(System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<T> DistinctBy<T, TKey>(Microsoft.FSharp.Core.FSharpFunc<T, TKey> projection, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<T> Empty<T>()`
- `public static T ExactlyOne<T>(System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<T> Except<T>(System.Collections.Generic.IEnumerable<T> itemsToExclude, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Boolean Exists<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Boolean Exists2<T1, T2>(Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, System.Boolean>> predicate, System.Collections.Generic.IEnumerable<T1> source1, System.Collections.Generic.IEnumerable<T2> source2)`
- `public static System.Collections.Generic.IEnumerable<T> Filter<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, System.Collections.Generic.IEnumerable<T> source)`
- `public static T Find<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, System.Collections.Generic.IEnumerable<T> source)`
- `public static T FindBack<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Int32 FindIndex<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Int32 FindIndexBack<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, System.Collections.Generic.IEnumerable<T> source)`
- `public static TState Fold<T, TState>(Microsoft.FSharp.Core.FSharpFunc<TState, Microsoft.FSharp.Core.FSharpFunc<T, TState>> folder, TState state, System.Collections.Generic.IEnumerable<T> source)`
- `public static TState Fold2<T1, T2, TState>(Microsoft.FSharp.Core.FSharpFunc<TState, Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, TState>>> folder, TState state, System.Collections.Generic.IEnumerable<T1> source1, System.Collections.Generic.IEnumerable<T2> source2)`
- `public static TState FoldBack<T, TState>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<TState, TState>> folder, System.Collections.Generic.IEnumerable<T> source, TState state)`
- `public static TState FoldBack2<T1, T2, TState>(Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, Microsoft.FSharp.Core.FSharpFunc<TState, TState>>> folder, System.Collections.Generic.IEnumerable<T1> source1, System.Collections.Generic.IEnumerable<T2> source2, TState state)`
- `public static System.Boolean ForAll<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Boolean ForAll2<T1, T2>(Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, System.Boolean>> predicate, System.Collections.Generic.IEnumerable<T1> source1, System.Collections.Generic.IEnumerable<T2> source2)`
- `public static T Get<T>(System.Int32 index, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<System.Tuple<TKey, System.Collections.Generic.IEnumerable<T>>> GroupBy<T, TKey>(Microsoft.FSharp.Core.FSharpFunc<T, TKey> projection, System.Collections.Generic.IEnumerable<T> source)`
- `public static T Head<T>(System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<System.Tuple<System.Int32, T>> Indexed<T>(System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<T> Initialize<T>(System.Int32 count, Microsoft.FSharp.Core.FSharpFunc<System.Int32, T> initializer)`
- `public static System.Collections.Generic.IEnumerable<T> InitializeInfinite<T>(Microsoft.FSharp.Core.FSharpFunc<System.Int32, T> initializer)`
- `public static System.Collections.Generic.IEnumerable<T> InsertAt<T>(System.Int32 index, T value, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<T> InsertManyAt<T>(System.Int32 index, System.Collections.Generic.IEnumerable<T> values, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Boolean IsEmpty<T>(System.Collections.Generic.IEnumerable<T> source)`
- `public static T Item<T>(System.Int32 index, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Void Iterate<T>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.Unit> action, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Void Iterate2<T1, T2>(Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, Microsoft.FSharp.Core.Unit>> action, System.Collections.Generic.IEnumerable<T1> source1, System.Collections.Generic.IEnumerable<T2> source2)`
- `public static System.Void IterateIndexed<T>(Microsoft.FSharp.Core.FSharpFunc<System.Int32, Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.Unit>> action, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Void IterateIndexed2<T1, T2>(Microsoft.FSharp.Core.FSharpFunc<System.Int32, Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, Microsoft.FSharp.Core.Unit>>> action, System.Collections.Generic.IEnumerable<T1> source1, System.Collections.Generic.IEnumerable<T2> source2)`
- `public static T Last<T>(System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Int32 Length<T>(System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<TResult> Map<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, TResult> mapping, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<TResult> Map2<T1, T2, TResult>(Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, TResult>> mapping, System.Collections.Generic.IEnumerable<T1> source1, System.Collections.Generic.IEnumerable<T2> source2)`
- `public static System.Collections.Generic.IEnumerable<TResult> Map3<T1, T2, T3, TResult>(Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, Microsoft.FSharp.Core.FSharpFunc<T3, TResult>>> mapping, System.Collections.Generic.IEnumerable<T1> source1, System.Collections.Generic.IEnumerable<T2> source2, System.Collections.Generic.IEnumerable<T3> source3)`
- `public static System.Tuple<System.Collections.Generic.IEnumerable<TResult>, TState> MapFold<T, TState, TResult>(Microsoft.FSharp.Core.FSharpFunc<TState, Microsoft.FSharp.Core.FSharpFunc<T, System.Tuple<TResult, TState>>> mapping, TState state, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Tuple<System.Collections.Generic.IEnumerable<TResult>, TState> MapFoldBack<T, TState, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<TState, System.Tuple<TResult, TState>>> mapping, System.Collections.Generic.IEnumerable<T> source, TState state)`
- `public static System.Collections.Generic.IEnumerable<TResult> MapIndexed<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<System.Int32, Microsoft.FSharp.Core.FSharpFunc<T, TResult>> mapping, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<TResult> MapIndexed2<T1, T2, TResult>(Microsoft.FSharp.Core.FSharpFunc<System.Int32, Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, TResult>>> mapping, System.Collections.Generic.IEnumerable<T1> source1, System.Collections.Generic.IEnumerable<T2> source2)`
- `public static T Max<T>(System.Collections.Generic.IEnumerable<T> source)`
- `public static T MaxBy<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, TResult> projection, System.Collections.Generic.IEnumerable<T> source)`
- `public static T Min<T>(System.Collections.Generic.IEnumerable<T> source)`
- `public static T MinBy<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, TResult> projection, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<T> OfArray<T>(T[] source)`
- `public static System.Collections.Generic.IEnumerable<T> OfList<T>(Microsoft.FSharp.Collections.FSharpList<T> source)`
- `public static System.Collections.Generic.IEnumerable<System.Tuple<T, T>> Pairwise<T>(System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<T> Permute<T>(Microsoft.FSharp.Core.FSharpFunc<System.Int32, System.Int32> indexMap, System.Collections.Generic.IEnumerable<T> source)`
- `public static TResult Pick<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpOption<TResult>> chooser, System.Collections.Generic.IEnumerable<T> source)`
- `public static T RandomChoice<T>(System.Collections.Generic.IEnumerable<T> source)`
- `public static T RandomChoiceBy<T>(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, System.Double> randomizer, System.Collections.Generic.IEnumerable<T> source)`
- `public static T RandomChoiceWith<T>(System.Random random, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<T> RandomChoices<T>(System.Int32 count, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<T> RandomChoicesBy<T>(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, System.Double> randomizer, System.Int32 count, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<T> RandomChoicesWith<T>(System.Random random, System.Int32 count, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<T> RandomSample<T>(System.Int32 count, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<T> RandomSampleBy<T>(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, System.Double> randomizer, System.Int32 count, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<T> RandomSampleWith<T>(System.Random random, System.Int32 count, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<T> RandomShuffle<T>(System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<T> RandomShuffleBy<T>(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, System.Double> randomizer, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<T> RandomShuffleWith<T>(System.Random random, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<T> ReadOnly<T>(System.Collections.Generic.IEnumerable<T> source)`
- `public static T Reduce<T>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<T, T>> reduction, System.Collections.Generic.IEnumerable<T> source)`
- `public static T ReduceBack<T>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<T, T>> reduction, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<T> RemoveAt<T>(System.Int32 index, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<T> RemoveManyAt<T>(System.Int32 index, System.Int32 count, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<T> Replicate<T>(System.Int32 count, T initial)`
- `public static System.Collections.Generic.IEnumerable<T> Reverse<T>(System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<TState> Scan<T, TState>(Microsoft.FSharp.Core.FSharpFunc<TState, Microsoft.FSharp.Core.FSharpFunc<T, TState>> folder, TState state, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<TState> ScanBack<T, TState>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<TState, TState>> folder, System.Collections.Generic.IEnumerable<T> source, TState state)`
- `public static System.Collections.Generic.IEnumerable<T> Singleton<T>(T value)`
- `public static System.Collections.Generic.IEnumerable<T> Skip<T>(System.Int32 count, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<T> SkipWhile<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<T> Sort<T>(System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<T> SortBy<T, TKey>(Microsoft.FSharp.Core.FSharpFunc<T, TKey> projection, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<T> SortByDescending<T, TKey>(Microsoft.FSharp.Core.FSharpFunc<T, TKey> projection, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<T> SortDescending<T>(System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<T> SortWith<T>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<T, System.Int32>> comparer, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<T[]> SplitInto<T>(System.Int32 count, System.Collections.Generic.IEnumerable<T> source)`
- `public static T Sum<T>(System.Collections.Generic.IEnumerable<T> source)`
- `public static T Sum$W<T>(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, T> get_Zero, Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<T, T>> op_Addition, System.Collections.Generic.IEnumerable<T> source)`
- `public static TResult SumBy<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, TResult> projection, System.Collections.Generic.IEnumerable<T> source)`
- `public static TResult SumBy$W<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, TResult> get_Zero, Microsoft.FSharp.Core.FSharpFunc<TResult, Microsoft.FSharp.Core.FSharpFunc<TResult, TResult>> op_Addition, Microsoft.FSharp.Core.FSharpFunc<T, TResult> projection, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<T> Tail<T>(System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<T> Take<T>(System.Int32 count, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<T> TakeWhile<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, System.Collections.Generic.IEnumerable<T> source)`
- `public static T[] ToArray<T>(System.Collections.Generic.IEnumerable<T> source)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> ToList<T>(System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<System.Collections.Generic.IEnumerable<T>> Transpose<TCollection, T>(System.Collections.Generic.IEnumerable<TCollection> source)`
- `where TCollection : System.Collections.Generic.IEnumerable<T>`
- `public static System.Collections.Generic.IEnumerable<T> Truncate<T>(System.Int32 count, System.Collections.Generic.IEnumerable<T> source)`
- `public static Microsoft.FSharp.Core.FSharpOption<T> TryExactlyOne<T>(System.Collections.Generic.IEnumerable<T> source)`
- `public static Microsoft.FSharp.Core.FSharpOption<T> TryFind<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, System.Collections.Generic.IEnumerable<T> source)`
- `public static Microsoft.FSharp.Core.FSharpOption<T> TryFindBack<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, System.Collections.Generic.IEnumerable<T> source)`
- `public static Microsoft.FSharp.Core.FSharpOption<System.Int32> TryFindIndex<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, System.Collections.Generic.IEnumerable<T> source)`
- `public static Microsoft.FSharp.Core.FSharpOption<System.Int32> TryFindIndexBack<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, System.Collections.Generic.IEnumerable<T> source)`
- `public static Microsoft.FSharp.Core.FSharpOption<T> TryHead<T>(System.Collections.Generic.IEnumerable<T> source)`
- `public static Microsoft.FSharp.Core.FSharpOption<T> TryItem<T>(System.Int32 index, System.Collections.Generic.IEnumerable<T> source)`
- `public static Microsoft.FSharp.Core.FSharpOption<T> TryLast<T>(System.Collections.Generic.IEnumerable<T> source)`
- `public static Microsoft.FSharp.Core.FSharpOption<TResult> TryPick<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpOption<TResult>> chooser, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<T> Unfold<TState, T>(Microsoft.FSharp.Core.FSharpFunc<TState, Microsoft.FSharp.Core.FSharpOption<System.Tuple<T, TState>>> generator, TState state)`
- `public static System.Collections.Generic.IEnumerable<T> UpdateAt<T>(System.Int32 index, T value, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<T> Where<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<T[]> Windowed<T>(System.Int32 windowSize, System.Collections.Generic.IEnumerable<T> source)`
- `public static System.Collections.Generic.IEnumerable<System.Tuple<T1, T2>> Zip<T1, T2>(System.Collections.Generic.IEnumerable<T1> source1, System.Collections.Generic.IEnumerable<T2> source2)`
- `public static System.Collections.Generic.IEnumerable<System.Tuple<T1, T2, T3>> Zip3<T1, T2, T3>(System.Collections.Generic.IEnumerable<T1> source1, System.Collections.Generic.IEnumerable<T2> source2, System.Collections.Generic.IEnumerable<T3> source3)`

### SetModule (class [static])

- `public static Microsoft.FSharp.Collections.FSharpSet<T> Add<T>(T value, Microsoft.FSharp.Collections.FSharpSet<T> set)`
- `public static System.Boolean Contains<T>(T element, Microsoft.FSharp.Collections.FSharpSet<T> set)`
- `public static System.Int32 Count<T>(Microsoft.FSharp.Collections.FSharpSet<T> set)`
- `public static Microsoft.FSharp.Collections.FSharpSet<T> Difference<T>(Microsoft.FSharp.Collections.FSharpSet<T> set1, Microsoft.FSharp.Collections.FSharpSet<T> set2)`
- `public static Microsoft.FSharp.Collections.FSharpSet<T> Empty<T>()`
- `public static System.Boolean Exists<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, Microsoft.FSharp.Collections.FSharpSet<T> set)`
- `public static Microsoft.FSharp.Collections.FSharpSet<T> Filter<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, Microsoft.FSharp.Collections.FSharpSet<T> set)`
- `public static TState Fold<T, TState>(Microsoft.FSharp.Core.FSharpFunc<TState, Microsoft.FSharp.Core.FSharpFunc<T, TState>> folder, TState state, Microsoft.FSharp.Collections.FSharpSet<T> set)`
- `public static TState FoldBack<T, TState>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<TState, TState>> folder, Microsoft.FSharp.Collections.FSharpSet<T> set, TState state)`
- `public static System.Boolean ForAll<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, Microsoft.FSharp.Collections.FSharpSet<T> set)`
- `public static Microsoft.FSharp.Collections.FSharpSet<T> Intersect<T>(Microsoft.FSharp.Collections.FSharpSet<T> set1, Microsoft.FSharp.Collections.FSharpSet<T> set2)`
- `public static Microsoft.FSharp.Collections.FSharpSet<T> IntersectMany<T>(System.Collections.Generic.IEnumerable<Microsoft.FSharp.Collections.FSharpSet<T>> sets)`
- `public static System.Boolean IsEmpty<T>(Microsoft.FSharp.Collections.FSharpSet<T> set)`
- `public static System.Boolean IsProperSubset<T>(Microsoft.FSharp.Collections.FSharpSet<T> set1, Microsoft.FSharp.Collections.FSharpSet<T> set2)`
- `public static System.Boolean IsProperSuperset<T>(Microsoft.FSharp.Collections.FSharpSet<T> set1, Microsoft.FSharp.Collections.FSharpSet<T> set2)`
- `public static System.Boolean IsSubset<T>(Microsoft.FSharp.Collections.FSharpSet<T> set1, Microsoft.FSharp.Collections.FSharpSet<T> set2)`
- `public static System.Boolean IsSuperset<T>(Microsoft.FSharp.Collections.FSharpSet<T> set1, Microsoft.FSharp.Collections.FSharpSet<T> set2)`
- `public static System.Void Iterate<T>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.Unit> action, Microsoft.FSharp.Collections.FSharpSet<T> set)`
- `public static Microsoft.FSharp.Collections.FSharpSet<TResult> Map<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, TResult> mapping, Microsoft.FSharp.Collections.FSharpSet<T> set)`
- `public static T MaxElement<T>(Microsoft.FSharp.Collections.FSharpSet<T> set)`
- `public static T MinElement<T>(Microsoft.FSharp.Collections.FSharpSet<T> set)`
- `public static Microsoft.FSharp.Collections.FSharpSet<T> OfArray<T>(T[] array)`
- `public static Microsoft.FSharp.Collections.FSharpSet<T> OfList<T>(Microsoft.FSharp.Collections.FSharpList<T> elements)`
- `public static Microsoft.FSharp.Collections.FSharpSet<T> OfSeq<T>(System.Collections.Generic.IEnumerable<T> elements)`
- `public static System.Tuple<Microsoft.FSharp.Collections.FSharpSet<T>, Microsoft.FSharp.Collections.FSharpSet<T>> Partition<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, Microsoft.FSharp.Collections.FSharpSet<T> set)`
- `public static System.Tuple<Microsoft.FSharp.Collections.FSharpSet<T1>, Microsoft.FSharp.Collections.FSharpSet<T2>> PartitionWith<T, T1, T2>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpChoice<T1, T2>> partitioner, Microsoft.FSharp.Collections.FSharpSet<T> set)`
- `public static Microsoft.FSharp.Collections.FSharpSet<T> Remove<T>(T value, Microsoft.FSharp.Collections.FSharpSet<T> set)`
- `public static Microsoft.FSharp.Collections.FSharpSet<T> Singleton<T>(T value)`
- `public static T[] ToArray<T>(Microsoft.FSharp.Collections.FSharpSet<T> set)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> ToList<T>(Microsoft.FSharp.Collections.FSharpSet<T> set)`
- `public static System.Collections.Generic.IEnumerable<T> ToSeq<T>(Microsoft.FSharp.Collections.FSharpSet<T> set)`
- `public static Microsoft.FSharp.Collections.FSharpSet<T> Union<T>(Microsoft.FSharp.Collections.FSharpSet<T> set1, Microsoft.FSharp.Collections.FSharpSet<T> set2)`
- `public static Microsoft.FSharp.Collections.FSharpSet<T> UnionMany<T>(System.Collections.Generic.IEnumerable<Microsoft.FSharp.Collections.FSharpSet<T>> sets)`

### Tags<T> (class [static])

- `public const System.Int32 Cons`
- `public const System.Int32 Empty`

## Microsoft.FSharp.Control

### EventModule (class [static])

- `public static System.Void Add<T, TDel>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.Unit> callback, Microsoft.FSharp.Control.IEvent<TDel, T> sourceEvent)`
- `where TDel : System.Delegate`
- `public static Microsoft.FSharp.Control.IEvent<Microsoft.FSharp.Control.FSharpHandler<TResult>, TResult> Choose<T, TResult, TDel>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpOption<TResult>> chooser, Microsoft.FSharp.Control.IEvent<TDel, T> sourceEvent)`
- `where TDel : System.Delegate`
- `public static Microsoft.FSharp.Control.IEvent<Microsoft.FSharp.Control.FSharpHandler<T>, T> Filter<T, TDel>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, Microsoft.FSharp.Control.IEvent<TDel, T> sourceEvent)`
- `where TDel : System.Delegate`
- `public static Microsoft.FSharp.Control.IEvent<Microsoft.FSharp.Control.FSharpHandler<TResult>, TResult> Map<T, TResult, TDel>(Microsoft.FSharp.Core.FSharpFunc<T, TResult> mapping, Microsoft.FSharp.Control.IEvent<TDel, T> sourceEvent)`
- `where TDel : System.Delegate`
- `public static Microsoft.FSharp.Control.IEvent<Microsoft.FSharp.Control.FSharpHandler<T>, T> Merge<TDel1, T, TDel2>(Microsoft.FSharp.Control.IEvent<TDel1, T> event1, Microsoft.FSharp.Control.IEvent<TDel2, T> event2)`
- `where TDel1 : System.Delegate`
- `where TDel2 : System.Delegate`
- `public static Microsoft.FSharp.Control.IEvent<Microsoft.FSharp.Control.FSharpHandler<System.Tuple<T, T>>, System.Tuple<T, T>> Pairwise<TDel, T>(Microsoft.FSharp.Control.IEvent<TDel, T> sourceEvent)`
- `where TDel : System.Delegate`
- `public static System.Tuple<Microsoft.FSharp.Control.IEvent<Microsoft.FSharp.Control.FSharpHandler<T>, T>, Microsoft.FSharp.Control.IEvent<Microsoft.FSharp.Control.FSharpHandler<T>, T>> Partition<T, TDel>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, Microsoft.FSharp.Control.IEvent<TDel, T> sourceEvent)`
- `where TDel : System.Delegate`
- `public static Microsoft.FSharp.Control.IEvent<Microsoft.FSharp.Control.FSharpHandler<TResult>, TResult> Scan<TResult, T, TDel>(Microsoft.FSharp.Core.FSharpFunc<TResult, Microsoft.FSharp.Core.FSharpFunc<T, TResult>> collector, TResult state, Microsoft.FSharp.Control.IEvent<TDel, T> sourceEvent)`
- `where TDel : System.Delegate`
- `public static System.Tuple<Microsoft.FSharp.Control.IEvent<Microsoft.FSharp.Control.FSharpHandler<TResult1>, TResult1>, Microsoft.FSharp.Control.IEvent<Microsoft.FSharp.Control.FSharpHandler<TResult2>, TResult2>> Split<T, TResult1, TResult2, TDel>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpChoice<TResult1, TResult2>> splitter, Microsoft.FSharp.Control.IEvent<TDel, T> sourceEvent)`
- `where TDel : System.Delegate`

### FSharpAsync (class [sealed])

- `public static Microsoft.FSharp.Control.FSharpAsync<System.Threading.CancellationToken> CancellationToken { get; } (nullability: Unknown)`
- `public static System.Threading.CancellationToken DefaultCancellationToken { get; }`
- `public static System.Tuple<Microsoft.FSharp.Core.FSharpFunc<System.Tuple<TArg, System.AsyncCallback, System.Object>, System.IAsyncResult>, Microsoft.FSharp.Core.FSharpFunc<System.IAsyncResult, T>, Microsoft.FSharp.Core.FSharpFunc<System.IAsyncResult, Microsoft.FSharp.Core.Unit>> AsBeginEnd<TArg, T>(Microsoft.FSharp.Core.FSharpFunc<TArg, Microsoft.FSharp.Control.FSharpAsync<T>> computation)`
- `public static Microsoft.FSharp.Control.FSharpAsync<T> AwaitEvent<TDel, T>(Microsoft.FSharp.Control.IEvent<TDel, T> event, Microsoft.FSharp.Core.FSharpOption<Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, Microsoft.FSharp.Core.Unit>> cancelAction)`
- `where TDel : System.Delegate`
- `public static Microsoft.FSharp.Control.FSharpAsync<System.Boolean> AwaitIAsyncResult(System.IAsyncResult iar, Microsoft.FSharp.Core.FSharpOption<System.Int32> millisecondsTimeout)`
- `public static Microsoft.FSharp.Control.FSharpAsync<T> AwaitTask<T>(System.Threading.Tasks.Task<T> task)`
- `public static Microsoft.FSharp.Control.FSharpAsync<Microsoft.FSharp.Core.Unit> AwaitTask(System.Threading.Tasks.Task task)`
- `public static Microsoft.FSharp.Control.FSharpAsync<System.Boolean> AwaitWaitHandle(System.Threading.WaitHandle waitHandle, Microsoft.FSharp.Core.FSharpOption<System.Int32> millisecondsTimeout)`
- `public static System.Void CancelDefaultToken()`
- `public static Microsoft.FSharp.Control.FSharpAsync<Microsoft.FSharp.Core.FSharpChoice<T, System.Exception>> Catch<T>(Microsoft.FSharp.Control.FSharpAsync<T> computation)`
- `public static Microsoft.FSharp.Control.FSharpAsync<Microsoft.FSharp.Core.FSharpOption<T>> Choice<T>(System.Collections.Generic.IEnumerable<Microsoft.FSharp.Control.FSharpAsync<Microsoft.FSharp.Core.FSharpOption<T>>> computations)`
- `public static Microsoft.FSharp.Control.FSharpAsync<T> FromBeginEnd<T>(Microsoft.FSharp.Core.FSharpFunc<System.Tuple<System.AsyncCallback, System.Object>, System.IAsyncResult> beginAction, Microsoft.FSharp.Core.FSharpFunc<System.IAsyncResult, T> endAction, Microsoft.FSharp.Core.FSharpOption<Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, Microsoft.FSharp.Core.Unit>> cancelAction)`
- `public static Microsoft.FSharp.Control.FSharpAsync<T> FromBeginEnd<TArg1, T>(TArg1 arg, Microsoft.FSharp.Core.FSharpFunc<System.Tuple<TArg1, System.AsyncCallback, System.Object>, System.IAsyncResult> beginAction, Microsoft.FSharp.Core.FSharpFunc<System.IAsyncResult, T> endAction, Microsoft.FSharp.Core.FSharpOption<Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, Microsoft.FSharp.Core.Unit>> cancelAction)`
- `public static Microsoft.FSharp.Control.FSharpAsync<T> FromBeginEnd<TArg1, TArg2, T>(TArg1 arg1, TArg2 arg2, Microsoft.FSharp.Core.FSharpFunc<System.Tuple<TArg1, TArg2, System.AsyncCallback, System.Object>, System.IAsyncResult> beginAction, Microsoft.FSharp.Core.FSharpFunc<System.IAsyncResult, T> endAction, Microsoft.FSharp.Core.FSharpOption<Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, Microsoft.FSharp.Core.Unit>> cancelAction)`
- `public static Microsoft.FSharp.Control.FSharpAsync<T> FromBeginEnd<TArg1, TArg2, TArg3, T>(TArg1 arg1, TArg2 arg2, TArg3 arg3, Microsoft.FSharp.Core.FSharpFunc<System.Tuple<TArg1, TArg2, TArg3, System.AsyncCallback, System.Object>, System.IAsyncResult> beginAction, Microsoft.FSharp.Core.FSharpFunc<System.IAsyncResult, T> endAction, Microsoft.FSharp.Core.FSharpOption<Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, Microsoft.FSharp.Core.Unit>> cancelAction)`
- `public static Microsoft.FSharp.Control.FSharpAsync<T> FromContinuations<T>(Microsoft.FSharp.Core.FSharpFunc<System.Tuple<Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.Unit>, Microsoft.FSharp.Core.FSharpFunc<System.Exception, Microsoft.FSharp.Core.Unit>, Microsoft.FSharp.Core.FSharpFunc<System.OperationCanceledException, Microsoft.FSharp.Core.Unit>>, Microsoft.FSharp.Core.Unit> callback)`
- `public static Microsoft.FSharp.Control.FSharpAsync<Microsoft.FSharp.Core.Unit> Ignore<T>(Microsoft.FSharp.Control.FSharpAsync<T> computation)`
- `public static Microsoft.FSharp.Control.FSharpAsync<System.IDisposable> OnCancel(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, Microsoft.FSharp.Core.Unit> interruption)`
- `public static Microsoft.FSharp.Control.FSharpAsync<T[]> Parallel<T>(System.Collections.Generic.IEnumerable<Microsoft.FSharp.Control.FSharpAsync<T>> computations)`
- `public static Microsoft.FSharp.Control.FSharpAsync<T[]> Parallel<T>(System.Collections.Generic.IEnumerable<Microsoft.FSharp.Control.FSharpAsync<T>> computations, Microsoft.FSharp.Core.FSharpOption<System.Int32> maxDegreeOfParallelism)`
- `public static T RunSynchronously<T>(Microsoft.FSharp.Control.FSharpAsync<T> computation, Microsoft.FSharp.Core.FSharpOption<System.Int32> timeout, Microsoft.FSharp.Core.FSharpOption<System.Threading.CancellationToken> cancellationToken)`
- `public static Microsoft.FSharp.Control.FSharpAsync<T[]> Sequential<T>(System.Collections.Generic.IEnumerable<Microsoft.FSharp.Control.FSharpAsync<T>> computations)`
- `public static Microsoft.FSharp.Control.FSharpAsync<Microsoft.FSharp.Core.Unit> Sleep(System.Int32 millisecondsDueTime)`
- `public static Microsoft.FSharp.Control.FSharpAsync<Microsoft.FSharp.Core.Unit> Sleep(System.TimeSpan dueTime)`
- `public static System.Void Start(Microsoft.FSharp.Control.FSharpAsync<Microsoft.FSharp.Core.Unit> computation, Microsoft.FSharp.Core.FSharpOption<System.Threading.CancellationToken> cancellationToken)`
- `public static System.Threading.Tasks.Task<T> StartAsTask<T>(Microsoft.FSharp.Control.FSharpAsync<T> computation, Microsoft.FSharp.Core.FSharpOption<System.Threading.Tasks.TaskCreationOptions> taskCreationOptions, Microsoft.FSharp.Core.FSharpOption<System.Threading.CancellationToken> cancellationToken)`
- `public static Microsoft.FSharp.Control.FSharpAsync<Microsoft.FSharp.Control.FSharpAsync<T>> StartChild<T>(Microsoft.FSharp.Control.FSharpAsync<T> computation, Microsoft.FSharp.Core.FSharpOption<System.Int32> millisecondsTimeout)`
- `public static Microsoft.FSharp.Control.FSharpAsync<System.Threading.Tasks.Task<T>> StartChildAsTask<T>(Microsoft.FSharp.Control.FSharpAsync<T> computation, Microsoft.FSharp.Core.FSharpOption<System.Threading.Tasks.TaskCreationOptions> taskCreationOptions)`
- `public static System.Void StartImmediate(Microsoft.FSharp.Control.FSharpAsync<Microsoft.FSharp.Core.Unit> computation, Microsoft.FSharp.Core.FSharpOption<System.Threading.CancellationToken> cancellationToken)`
- `public static System.Threading.Tasks.Task<T> StartImmediateAsTask<T>(Microsoft.FSharp.Control.FSharpAsync<T> computation, Microsoft.FSharp.Core.FSharpOption<System.Threading.CancellationToken> cancellationToken)`
- `public static System.Void StartWithContinuations<T>(Microsoft.FSharp.Control.FSharpAsync<T> computation, Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.Unit> continuation, Microsoft.FSharp.Core.FSharpFunc<System.Exception, Microsoft.FSharp.Core.Unit> exceptionContinuation, Microsoft.FSharp.Core.FSharpFunc<System.OperationCanceledException, Microsoft.FSharp.Core.Unit> cancellationContinuation, Microsoft.FSharp.Core.FSharpOption<System.Threading.CancellationToken> cancellationToken)`
- `public static Microsoft.FSharp.Control.FSharpAsync<Microsoft.FSharp.Core.Unit> SwitchToContext(System.Threading.SynchronizationContext syncContext)`
- `public static Microsoft.FSharp.Control.FSharpAsync<Microsoft.FSharp.Core.Unit> SwitchToNewThread()`
- `public static Microsoft.FSharp.Control.FSharpAsync<Microsoft.FSharp.Core.Unit> SwitchToThreadPool()`
- `public static Microsoft.FSharp.Control.FSharpAsync<T> TryCancelled<T>(Microsoft.FSharp.Control.FSharpAsync<T> computation, Microsoft.FSharp.Core.FSharpFunc<System.OperationCanceledException, Microsoft.FSharp.Core.Unit> compensation)`

### FSharpAsyncBuilder (class [sealed])

- `public Microsoft.FSharp.Control.FSharpAsync<TResult> Bind<T, TResult>(Microsoft.FSharp.Control.FSharpAsync<T> computation, Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Control.FSharpAsync<TResult>> binder)`
- `public Microsoft.FSharp.Control.FSharpAsync<T> Combine<T>(Microsoft.FSharp.Control.FSharpAsync<Microsoft.FSharp.Core.Unit> computation1, Microsoft.FSharp.Control.FSharpAsync<T> computation2)`
- `public Microsoft.FSharp.Control.FSharpAsync<T> Delay<T>(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, Microsoft.FSharp.Control.FSharpAsync<T>> generator)`
- `public Microsoft.FSharp.Control.FSharpAsync<Microsoft.FSharp.Core.Unit> For<T>(System.Collections.Generic.IEnumerable<T> sequence, Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Control.FSharpAsync<Microsoft.FSharp.Core.Unit>> body)`
- `public Microsoft.FSharp.Control.FSharpAsync<T> Return<T>(T value)`
- `public Microsoft.FSharp.Control.FSharpAsync<T> ReturnFrom<T>(Microsoft.FSharp.Control.FSharpAsync<T> computation)`
- `public Microsoft.FSharp.Control.FSharpAsync<T> TryFinally<T>(Microsoft.FSharp.Control.FSharpAsync<T> computation, Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, Microsoft.FSharp.Core.Unit> compensation)`
- `public Microsoft.FSharp.Control.FSharpAsync<T> TryWith<T>(Microsoft.FSharp.Control.FSharpAsync<T> computation, Microsoft.FSharp.Core.FSharpFunc<System.Exception, Microsoft.FSharp.Control.FSharpAsync<T>> catchHandler)`
- `public Microsoft.FSharp.Control.FSharpAsync<TResult> Using<T, TResult>(T resource, Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Control.FSharpAsync<TResult>> binder)`
- `where T : System.IDisposable`
- `public Microsoft.FSharp.Control.FSharpAsync<Microsoft.FSharp.Core.Unit> While(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, System.Boolean> guard, Microsoft.FSharp.Control.FSharpAsync<Microsoft.FSharp.Core.Unit> computation)`
- `public Microsoft.FSharp.Control.FSharpAsync<Microsoft.FSharp.Core.Unit> Zero()`

### FSharpAsyncReplyChannel`1<TReply> (class [sealed])

- `public System.Void Reply(TReply value)`

### FSharpAsync`1<T> (class [sealed])


### FSharpEvent`1<T> (class)

- `public FSharpEvent`1()`
- `public Microsoft.FSharp.Control.IEvent<Microsoft.FSharp.Control.FSharpHandler<T>, T> Publish { get; } (nullability: Unknown)`
- `public System.Void Trigger(T arg)`

### FSharpEvent`2<TDelegate, TArgs> (class)

- `where TDelegate : class, System.Delegate`

- `public FSharpEvent`2()`
- `public Microsoft.FSharp.Control.IEvent<TDelegate, TArgs> Publish { get; } (nullability: Unknown)`
- `public System.Void Trigger(System.Object sender, TArgs args)`

### FSharpMailboxProcessor`1<TMsg> (class [sealed]) : System.IDisposable

- `public FSharpMailboxProcessor`1(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Control.FSharpMailboxProcessor<TMsg>, Microsoft.FSharp.Control.FSharpAsync<Microsoft.FSharp.Core.Unit>> body, Microsoft.FSharp.Core.FSharpOption<System.Threading.CancellationToken> cancellationToken)`
- `public FSharpMailboxProcessor`1(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Control.FSharpMailboxProcessor<TMsg>, Microsoft.FSharp.Control.FSharpAsync<Microsoft.FSharp.Core.Unit>> body, System.Boolean isThrowExceptionAfterDisposed, Microsoft.FSharp.Core.FSharpOption<System.Threading.CancellationToken> cancellationToken)`
- `public System.Int32 CurrentQueueLength { get; }`
- `public System.Int32 DefaultTimeout { get; set; }`
- `public System.Void Dispose()`
- `public System.Void Post(TMsg message)`
- `public Microsoft.FSharp.Control.FSharpAsync<TReply> PostAndAsyncReply<TReply>(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Control.FSharpAsyncReplyChannel<TReply>, TMsg> buildMessage, Microsoft.FSharp.Core.FSharpOption<System.Int32> timeout)`
- `public TReply PostAndReply<TReply>(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Control.FSharpAsyncReplyChannel<TReply>, TMsg> buildMessage, Microsoft.FSharp.Core.FSharpOption<System.Int32> timeout)`
- `public Microsoft.FSharp.Control.FSharpAsync<Microsoft.FSharp.Core.FSharpOption<TReply>> PostAndTryAsyncReply<TReply>(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Control.FSharpAsyncReplyChannel<TReply>, TMsg> buildMessage, Microsoft.FSharp.Core.FSharpOption<System.Int32> timeout)`
- `public Microsoft.FSharp.Control.FSharpAsync<TMsg> Receive(Microsoft.FSharp.Core.FSharpOption<System.Int32> timeout)`
- `public Microsoft.FSharp.Control.FSharpAsync<T> Scan<T>(Microsoft.FSharp.Core.FSharpFunc<TMsg, Microsoft.FSharp.Core.FSharpOption<Microsoft.FSharp.Control.FSharpAsync<T>>> scanner, Microsoft.FSharp.Core.FSharpOption<System.Int32> timeout)`
- `public System.Void Start()`
- `public static Microsoft.FSharp.Control.FSharpMailboxProcessor<TMsg> Start(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Control.FSharpMailboxProcessor<TMsg>, Microsoft.FSharp.Control.FSharpAsync<Microsoft.FSharp.Core.Unit>> body, Microsoft.FSharp.Core.FSharpOption<System.Threading.CancellationToken> cancellationToken)`
- `public static Microsoft.FSharp.Control.FSharpMailboxProcessor<TMsg> Start(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Control.FSharpMailboxProcessor<TMsg>, Microsoft.FSharp.Control.FSharpAsync<Microsoft.FSharp.Core.Unit>> body, System.Boolean isThrowExceptionAfterDisposed, Microsoft.FSharp.Core.FSharpOption<System.Threading.CancellationToken> cancellationToken)`
- `public System.Void StartImmediate()`
- `public static Microsoft.FSharp.Control.FSharpMailboxProcessor<TMsg> StartImmediate(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Control.FSharpMailboxProcessor<TMsg>, Microsoft.FSharp.Control.FSharpAsync<Microsoft.FSharp.Core.Unit>> body, Microsoft.FSharp.Core.FSharpOption<System.Threading.CancellationToken> cancellationToken)`
- `public static Microsoft.FSharp.Control.FSharpMailboxProcessor<TMsg> StartImmediate(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Control.FSharpMailboxProcessor<TMsg>, Microsoft.FSharp.Control.FSharpAsync<Microsoft.FSharp.Core.Unit>> body, System.Boolean isThrowExceptionAfterDisposed, Microsoft.FSharp.Core.FSharpOption<System.Threading.CancellationToken> cancellationToken)`
- `public Microsoft.FSharp.Core.FSharpOption<TReply> TryPostAndReply<TReply>(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Control.FSharpAsyncReplyChannel<TReply>, TMsg> buildMessage, Microsoft.FSharp.Core.FSharpOption<System.Int32> timeout)`
- `public Microsoft.FSharp.Control.FSharpAsync<Microsoft.FSharp.Core.FSharpOption<TMsg>> TryReceive(Microsoft.FSharp.Core.FSharpOption<System.Int32> timeout)`
- `public Microsoft.FSharp.Control.FSharpAsync<Microsoft.FSharp.Core.FSharpOption<T>> TryScan<T>(Microsoft.FSharp.Core.FSharpFunc<TMsg, Microsoft.FSharp.Core.FSharpOption<Microsoft.FSharp.Control.FSharpAsync<T>>> scanner, Microsoft.FSharp.Core.FSharpOption<System.Int32> timeout)`
- `public event Microsoft.FSharp.Control.FSharpHandler<System.Exception> Error`

### IEvent`2<TDelegate, TArgs> (interface) : System.IObservable<TArgs>, Microsoft.FSharp.Control.IDelegateEvent<TDelegate>

- `where TDelegate : System.Delegate`


### LazyExtensions (class [static])

- `public static System.Lazy<T> Create<T>(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, T> creator)`
- `public static System.Lazy<T> CreateFromValue<T>(T value)`
- `public static T Force<T>(System.Lazy<T> )`

### TaskBuilder (class) : Microsoft.FSharp.Control.TaskBuilderBase

- `public System.Threading.Tasks.Task<T> Run<T>(Microsoft.FSharp.Core.CompilerServices.ResumableCode<Microsoft.FSharp.Control.TaskStateMachineData<T>, T> code)`
- `public static System.Threading.Tasks.Task<T> RunDynamic<T>(Microsoft.FSharp.Core.CompilerServices.ResumableCode<Microsoft.FSharp.Control.TaskStateMachineData<T>, T> code)`

### TaskBuilderBase (class)

- `public Microsoft.FSharp.Core.CompilerServices.ResumableCode<Microsoft.FSharp.Control.TaskStateMachineData<TOverall>, T> Combine<TOverall, T>(Microsoft.FSharp.Core.CompilerServices.ResumableCode<Microsoft.FSharp.Control.TaskStateMachineData<TOverall>, Microsoft.FSharp.Core.Unit> task1, Microsoft.FSharp.Core.CompilerServices.ResumableCode<Microsoft.FSharp.Control.TaskStateMachineData<TOverall>, T> task2)`
- `public Microsoft.FSharp.Core.CompilerServices.ResumableCode<Microsoft.FSharp.Control.TaskStateMachineData<TOverall>, T> Delay<TOverall, T>(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, Microsoft.FSharp.Core.CompilerServices.ResumableCode<Microsoft.FSharp.Control.TaskStateMachineData<TOverall>, T>> generator)`
- `public Microsoft.FSharp.Core.CompilerServices.ResumableCode<Microsoft.FSharp.Control.TaskStateMachineData<TOverall>, Microsoft.FSharp.Core.Unit> For<T, TOverall>(System.Collections.Generic.IEnumerable<T> sequence, Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.CompilerServices.ResumableCode<Microsoft.FSharp.Control.TaskStateMachineData<TOverall>, Microsoft.FSharp.Core.Unit>> body)`
- `public Microsoft.FSharp.Core.CompilerServices.ResumableCode<Microsoft.FSharp.Control.TaskStateMachineData<T>, T> Return<T>(T value)`
- `public Microsoft.FSharp.Core.CompilerServices.ResumableCode<Microsoft.FSharp.Control.TaskStateMachineData<TOverall>, T> TryFinally<TOverall, T>(Microsoft.FSharp.Core.CompilerServices.ResumableCode<Microsoft.FSharp.Control.TaskStateMachineData<TOverall>, T> body, Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, Microsoft.FSharp.Core.Unit> compensation)`
- `public Microsoft.FSharp.Core.CompilerServices.ResumableCode<Microsoft.FSharp.Control.TaskStateMachineData<TOverall>, T> TryWith<TOverall, T>(Microsoft.FSharp.Core.CompilerServices.ResumableCode<Microsoft.FSharp.Control.TaskStateMachineData<TOverall>, T> body, Microsoft.FSharp.Core.FSharpFunc<System.Exception, Microsoft.FSharp.Core.CompilerServices.ResumableCode<Microsoft.FSharp.Control.TaskStateMachineData<TOverall>, T>> catch)`
- `public Microsoft.FSharp.Core.CompilerServices.ResumableCode<Microsoft.FSharp.Control.TaskStateMachineData<TOverall>, T> Using<TResource, TOverall, T>(TResource resource, Microsoft.FSharp.Core.FSharpFunc<TResource, Microsoft.FSharp.Core.CompilerServices.ResumableCode<Microsoft.FSharp.Control.TaskStateMachineData<TOverall>, T>> body)`
- `where TResource : System.IAsyncDisposable`
- `public Microsoft.FSharp.Core.CompilerServices.ResumableCode<Microsoft.FSharp.Control.TaskStateMachineData<TOverall>, Microsoft.FSharp.Core.Unit> While<TOverall>(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, System.Boolean> condition, Microsoft.FSharp.Core.CompilerServices.ResumableCode<Microsoft.FSharp.Control.TaskStateMachineData<TOverall>, Microsoft.FSharp.Core.Unit> body)`
- `public Microsoft.FSharp.Core.CompilerServices.ResumableCode<Microsoft.FSharp.Control.TaskStateMachineData<TOverall>, Microsoft.FSharp.Core.Unit> Zero<TOverall>()`

### TaskBuilderModule (class [static])

- `public static Microsoft.FSharp.Control.BackgroundTaskBuilder backgroundTask { get; } (nullability: Unknown)`
- `public static Microsoft.FSharp.Control.TaskBuilder task { get; } (nullability: Unknown)`

## Microsoft.FSharp.Control.TaskBuilderExtensions

### HighPriority (class [static])

- `public static System.Threading.Tasks.Task<System.ValueTuple<TResult1, TResult2>> BackgroundTaskBuilder.MergeSources<TResult1, TResult2>(Microsoft.FSharp.Control.BackgroundTaskBuilder this, System.Threading.Tasks.Task<TResult1> task1, System.Threading.Tasks.Task<TResult2> task2)`
- `public static System.Threading.Tasks.Task<System.ValueTuple<TResult1, TResult2>> TaskBuilder.MergeSources<TResult1, TResult2>(Microsoft.FSharp.Control.TaskBuilder this, System.Threading.Tasks.Task<TResult1> task1, System.Threading.Tasks.Task<TResult2> task2)`
- `public static Microsoft.FSharp.Core.CompilerServices.ResumableCode<Microsoft.FSharp.Control.TaskStateMachineData<TOverall>, TResult2> TaskBuilderBase.Bind<TResult1, TOverall, TResult2>(Microsoft.FSharp.Control.TaskBuilderBase _, System.Threading.Tasks.Task<TResult1> task, Microsoft.FSharp.Core.FSharpFunc<TResult1, Microsoft.FSharp.Core.CompilerServices.ResumableCode<Microsoft.FSharp.Control.TaskStateMachineData<TOverall>, TResult2>> continuation)`
- `public static System.Boolean TaskBuilderBase.BindDynamic.Static<TOverall, TResult1, TResult2>(ref Microsoft.FSharp.Core.CompilerServices.ResumableStateMachine<Microsoft.FSharp.Control.TaskStateMachineData<TOverall>>& sm, System.Threading.Tasks.Task<TResult1> task, Microsoft.FSharp.Core.FSharpFunc<TResult1, Microsoft.FSharp.Core.CompilerServices.ResumableCode<Microsoft.FSharp.Control.TaskStateMachineData<TOverall>, TResult2>> continuation)`
- `public static Microsoft.FSharp.Core.CompilerServices.ResumableCode<Microsoft.FSharp.Control.TaskStateMachineData<T>, T> TaskBuilderBase.ReturnFrom<T>(Microsoft.FSharp.Control.TaskBuilderBase this, System.Threading.Tasks.Task<T> task)`

### LowPlusPriority (class [static])

- `public static System.Threading.Tasks.Task<System.ValueTuple<TResult1, TResult2>> BackgroundTaskBuilder.MergeSources<TTaskLike2, TResult1, TResult2, TAwaiter2>(Microsoft.FSharp.Control.BackgroundTaskBuilder this, Microsoft.FSharp.Control.FSharpAsync<TResult1> computation, TTaskLike2 task)`
- `where TAwaiter2 : System.Runtime.CompilerServices.ICriticalNotifyCompletion`
- `public static System.Threading.Tasks.Task<System.ValueTuple<TResult1, TResult2>> BackgroundTaskBuilder.MergeSources<TTaskLike1, TResult1, TResult2, TAwaiter1>(Microsoft.FSharp.Control.BackgroundTaskBuilder this, TTaskLike1 task, Microsoft.FSharp.Control.FSharpAsync<TResult2> computation)`
- `where TAwaiter1 : System.Runtime.CompilerServices.ICriticalNotifyCompletion`
- `public static System.Threading.Tasks.Task<System.ValueTuple<TResult1, TResult2>> BackgroundTaskBuilder.MergeSources$W<TTaskLike2, TResult1, TResult2, TAwaiter2>(Microsoft.FSharp.Core.FSharpFunc<TTaskLike2, TAwaiter2> getAwaiter, Microsoft.FSharp.Core.FSharpFunc<TAwaiter2, TResult2> getResult, Microsoft.FSharp.Core.FSharpFunc<TAwaiter2, System.Boolean> get_IsCompleted, Microsoft.FSharp.Control.BackgroundTaskBuilder this, Microsoft.FSharp.Control.FSharpAsync<TResult1> computation, TTaskLike2 task)`
- `where TAwaiter2 : System.Runtime.CompilerServices.ICriticalNotifyCompletion`
- `public static System.Threading.Tasks.Task<System.ValueTuple<TResult1, TResult2>> BackgroundTaskBuilder.MergeSources$W<TTaskLike1, TResult1, TResult2, TAwaiter1>(Microsoft.FSharp.Core.FSharpFunc<TTaskLike1, TAwaiter1> getAwaiter, Microsoft.FSharp.Core.FSharpFunc<TAwaiter1, TResult1> getResult, Microsoft.FSharp.Core.FSharpFunc<TAwaiter1, System.Boolean> get_IsCompleted, Microsoft.FSharp.Control.BackgroundTaskBuilder this, TTaskLike1 task, Microsoft.FSharp.Control.FSharpAsync<TResult2> computation)`
- `where TAwaiter1 : System.Runtime.CompilerServices.ICriticalNotifyCompletion`
- `public static System.Threading.Tasks.Task<System.ValueTuple<TResult1, TResult2>> TaskBuilder.MergeSources<TTaskLike2, TResult1, TResult2, TAwaiter2>(Microsoft.FSharp.Control.TaskBuilder this, Microsoft.FSharp.Control.FSharpAsync<TResult1> computation, TTaskLike2 task)`
- `where TAwaiter2 : System.Runtime.CompilerServices.ICriticalNotifyCompletion`
- `public static System.Threading.Tasks.Task<System.ValueTuple<TResult1, TResult2>> TaskBuilder.MergeSources<TTaskLike1, TResult1, TResult2, TAwaiter1>(Microsoft.FSharp.Control.TaskBuilder this, TTaskLike1 task, Microsoft.FSharp.Control.FSharpAsync<TResult2> computation)`
- `where TAwaiter1 : System.Runtime.CompilerServices.ICriticalNotifyCompletion`
- `public static System.Threading.Tasks.Task<System.ValueTuple<TResult1, TResult2>> TaskBuilder.MergeSources$W<TTaskLike2, TResult1, TResult2, TAwaiter2>(Microsoft.FSharp.Core.FSharpFunc<TTaskLike2, TAwaiter2> getAwaiter, Microsoft.FSharp.Core.FSharpFunc<TAwaiter2, TResult2> getResult, Microsoft.FSharp.Core.FSharpFunc<TAwaiter2, System.Boolean> get_IsCompleted, Microsoft.FSharp.Control.TaskBuilder this, Microsoft.FSharp.Control.FSharpAsync<TResult1> computation, TTaskLike2 task)`
- `where TAwaiter2 : System.Runtime.CompilerServices.ICriticalNotifyCompletion`
- `public static System.Threading.Tasks.Task<System.ValueTuple<TResult1, TResult2>> TaskBuilder.MergeSources$W<TTaskLike1, TResult1, TResult2, TAwaiter1>(Microsoft.FSharp.Core.FSharpFunc<TTaskLike1, TAwaiter1> getAwaiter, Microsoft.FSharp.Core.FSharpFunc<TAwaiter1, TResult1> getResult, Microsoft.FSharp.Core.FSharpFunc<TAwaiter1, System.Boolean> get_IsCompleted, Microsoft.FSharp.Control.TaskBuilder this, TTaskLike1 task, Microsoft.FSharp.Control.FSharpAsync<TResult2> computation)`
- `where TAwaiter1 : System.Runtime.CompilerServices.ICriticalNotifyCompletion`

### LowPriority (class [static])

- `public static System.Threading.Tasks.Task<System.ValueTuple<TResult1, TResult2>> BackgroundTaskBuilder.MergeSources<TTaskLike1, TTaskLike2, TResult1, TResult2, TAwaiter1, TAwaiter2>(Microsoft.FSharp.Control.BackgroundTaskBuilder this, TTaskLike1 task1, TTaskLike2 task2)`
- `where TAwaiter1 : System.Runtime.CompilerServices.ICriticalNotifyCompletion`
- `where TAwaiter2 : System.Runtime.CompilerServices.ICriticalNotifyCompletion`
- `public static System.Threading.Tasks.Task<System.ValueTuple<TResult1, TResult2>> BackgroundTaskBuilder.MergeSources$W<TTaskLike1, TTaskLike2, TResult1, TResult2, TAwaiter1, TAwaiter2>(Microsoft.FSharp.Core.FSharpFunc<TTaskLike1, TAwaiter1> getAwaiter, Microsoft.FSharp.Core.FSharpFunc<TTaskLike2, TAwaiter2> getAwaiter1, Microsoft.FSharp.Core.FSharpFunc<TAwaiter1, TResult1> getResult, Microsoft.FSharp.Core.FSharpFunc<TAwaiter2, TResult2> getResult3, Microsoft.FSharp.Core.FSharpFunc<TAwaiter1, System.Boolean> get_IsCompleted, Microsoft.FSharp.Core.FSharpFunc<TAwaiter2, System.Boolean> get_IsCompleted5, Microsoft.FSharp.Control.BackgroundTaskBuilder this, TTaskLike1 task1, TTaskLike2 task2)`
- `where TAwaiter1 : System.Runtime.CompilerServices.ICriticalNotifyCompletion`
- `where TAwaiter2 : System.Runtime.CompilerServices.ICriticalNotifyCompletion`
- `public static System.Threading.Tasks.Task<System.ValueTuple<TResult1, TResult2>> TaskBuilder.MergeSources<TTaskLike1, TTaskLike2, TResult1, TResult2, TAwaiter1, TAwaiter2>(Microsoft.FSharp.Control.TaskBuilder this, TTaskLike1 task1, TTaskLike2 task2)`
- `where TAwaiter1 : System.Runtime.CompilerServices.ICriticalNotifyCompletion`
- `where TAwaiter2 : System.Runtime.CompilerServices.ICriticalNotifyCompletion`
- `public static System.Threading.Tasks.Task<System.ValueTuple<TResult1, TResult2>> TaskBuilder.MergeSources$W<TTaskLike1, TTaskLike2, TResult1, TResult2, TAwaiter1, TAwaiter2>(Microsoft.FSharp.Core.FSharpFunc<TTaskLike1, TAwaiter1> getAwaiter, Microsoft.FSharp.Core.FSharpFunc<TTaskLike2, TAwaiter2> getAwaiter1, Microsoft.FSharp.Core.FSharpFunc<TAwaiter1, TResult1> getResult, Microsoft.FSharp.Core.FSharpFunc<TAwaiter2, TResult2> getResult3, Microsoft.FSharp.Core.FSharpFunc<TAwaiter1, System.Boolean> get_IsCompleted, Microsoft.FSharp.Core.FSharpFunc<TAwaiter2, System.Boolean> get_IsCompleted5, Microsoft.FSharp.Control.TaskBuilder this, TTaskLike1 task1, TTaskLike2 task2)`
- `where TAwaiter1 : System.Runtime.CompilerServices.ICriticalNotifyCompletion`
- `where TAwaiter2 : System.Runtime.CompilerServices.ICriticalNotifyCompletion`
- `public static Microsoft.FSharp.Core.CompilerServices.ResumableCode<Microsoft.FSharp.Control.TaskStateMachineData<TOverall>, TResult2> TaskBuilderBase.Bind<TTaskLike, TResult1, TResult2, TAwaiter, TOverall>(Microsoft.FSharp.Control.TaskBuilderBase _, TTaskLike task, Microsoft.FSharp.Core.FSharpFunc<TResult1, Microsoft.FSharp.Core.CompilerServices.ResumableCode<Microsoft.FSharp.Control.TaskStateMachineData<TOverall>, TResult2>> continuation)`
- `where TAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion`
- `public static Microsoft.FSharp.Core.CompilerServices.ResumableCode<Microsoft.FSharp.Control.TaskStateMachineData<TOverall>, TResult2> TaskBuilderBase.Bind$W<TTaskLike, TResult1, TResult2, TAwaiter, TOverall>(Microsoft.FSharp.Core.FSharpFunc<TTaskLike, TAwaiter> getAwaiter, Microsoft.FSharp.Core.FSharpFunc<TAwaiter, TResult1> getResult, Microsoft.FSharp.Core.FSharpFunc<TAwaiter, System.Boolean> get_IsCompleted, Microsoft.FSharp.Control.TaskBuilderBase _, TTaskLike task, Microsoft.FSharp.Core.FSharpFunc<TResult1, Microsoft.FSharp.Core.CompilerServices.ResumableCode<Microsoft.FSharp.Control.TaskStateMachineData<TOverall>, TResult2>> continuation)`
- `where TAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion`
- `public static System.Boolean TaskBuilderBase.BindDynamic.Static<TTaskLike, TResult1, TResult2, TAwaiter, TOverall>(ref Microsoft.FSharp.Core.CompilerServices.ResumableStateMachine<Microsoft.FSharp.Control.TaskStateMachineData<TOverall>>& sm, TTaskLike task, Microsoft.FSharp.Core.FSharpFunc<TResult1, Microsoft.FSharp.Core.CompilerServices.ResumableCode<Microsoft.FSharp.Control.TaskStateMachineData<TOverall>, TResult2>> continuation)`
- `where TAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion`
- `public static System.Boolean TaskBuilderBase.BindDynamic.Static$W<TTaskLike, TResult1, TResult2, TAwaiter, TOverall>(Microsoft.FSharp.Core.FSharpFunc<TTaskLike, TAwaiter> getAwaiter, Microsoft.FSharp.Core.FSharpFunc<TAwaiter, TResult1> getResult, Microsoft.FSharp.Core.FSharpFunc<TAwaiter, System.Boolean> get_IsCompleted, ref Microsoft.FSharp.Core.CompilerServices.ResumableStateMachine<Microsoft.FSharp.Control.TaskStateMachineData<TOverall>>& sm, TTaskLike task, Microsoft.FSharp.Core.FSharpFunc<TResult1, Microsoft.FSharp.Core.CompilerServices.ResumableCode<Microsoft.FSharp.Control.TaskStateMachineData<TOverall>, TResult2>> continuation)`
- `where TAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion`
- `public static Microsoft.FSharp.Core.CompilerServices.ResumableCode<Microsoft.FSharp.Control.TaskStateMachineData<T>, T> TaskBuilderBase.ReturnFrom<TTaskLike, TAwaiter, T>(Microsoft.FSharp.Control.TaskBuilderBase this, TTaskLike task)`
- `where TAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion`
- `public static Microsoft.FSharp.Core.CompilerServices.ResumableCode<Microsoft.FSharp.Control.TaskStateMachineData<T>, T> TaskBuilderBase.ReturnFrom$W<TTaskLike, TAwaiter, T>(Microsoft.FSharp.Core.FSharpFunc<TTaskLike, TAwaiter> getAwaiter, Microsoft.FSharp.Core.FSharpFunc<TAwaiter, T> getResult, Microsoft.FSharp.Core.FSharpFunc<TAwaiter, System.Boolean> get_IsCompleted, Microsoft.FSharp.Control.TaskBuilderBase this, TTaskLike task)`
- `where TAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion`
- `public static Microsoft.FSharp.Core.CompilerServices.ResumableCode<Microsoft.FSharp.Control.TaskStateMachineData<TOverall>, T> TaskBuilderBase.Using<TResource, TOverall, T>(Microsoft.FSharp.Control.TaskBuilderBase _, TResource resource, Microsoft.FSharp.Core.FSharpFunc<TResource, Microsoft.FSharp.Core.CompilerServices.ResumableCode<Microsoft.FSharp.Control.TaskStateMachineData<TOverall>, T>> body)`
- `where TResource : System.IDisposable`

### MediumPriority (class [static])

- `public static System.Threading.Tasks.Task<System.ValueTuple<TResult1, TResult2>> BackgroundTaskBuilder.MergeSources<TTaskLike2, TResult1, TResult2, TAwaiter2>(Microsoft.FSharp.Control.BackgroundTaskBuilder this, System.Threading.Tasks.Task<TResult1> task1, TTaskLike2 task2)`
- `where TAwaiter2 : System.Runtime.CompilerServices.ICriticalNotifyCompletion`
- `public static System.Threading.Tasks.Task<System.ValueTuple<TResult1, TResult2>> BackgroundTaskBuilder.MergeSources<TTaskLike1, TResult1, TResult2, TAwaiter1>(Microsoft.FSharp.Control.BackgroundTaskBuilder this, TTaskLike1 task1, System.Threading.Tasks.Task<TResult2> task2)`
- `where TAwaiter1 : System.Runtime.CompilerServices.ICriticalNotifyCompletion`
- `public static System.Threading.Tasks.Task<System.ValueTuple<TResult1, TResult2>> BackgroundTaskBuilder.MergeSources<TResult1, TResult2>(Microsoft.FSharp.Control.BackgroundTaskBuilder this, Microsoft.FSharp.Control.FSharpAsync<TResult1> computation1, Microsoft.FSharp.Control.FSharpAsync<TResult2> computation2)`
- `public static System.Threading.Tasks.Task<System.ValueTuple<TResult1, TResult2>> BackgroundTaskBuilder.MergeSources<TResult1, TResult2>(Microsoft.FSharp.Control.BackgroundTaskBuilder this, System.Threading.Tasks.Task<TResult1> task, Microsoft.FSharp.Control.FSharpAsync<TResult2> computation)`
- `public static System.Threading.Tasks.Task<System.ValueTuple<TResult1, TResult2>> BackgroundTaskBuilder.MergeSources<TResult1, TResult2>(Microsoft.FSharp.Control.BackgroundTaskBuilder this, Microsoft.FSharp.Control.FSharpAsync<TResult1> computation, System.Threading.Tasks.Task<TResult2> task)`
- `public static System.Threading.Tasks.Task<System.ValueTuple<TResult1, TResult2>> BackgroundTaskBuilder.MergeSources$W<TTaskLike2, TResult1, TResult2, TAwaiter2>(Microsoft.FSharp.Core.FSharpFunc<TTaskLike2, TAwaiter2> getAwaiter, Microsoft.FSharp.Core.FSharpFunc<TAwaiter2, TResult2> getResult, Microsoft.FSharp.Core.FSharpFunc<TAwaiter2, System.Boolean> get_IsCompleted, Microsoft.FSharp.Control.BackgroundTaskBuilder this, System.Threading.Tasks.Task<TResult1> task1, TTaskLike2 task2)`
- `where TAwaiter2 : System.Runtime.CompilerServices.ICriticalNotifyCompletion`
- `public static System.Threading.Tasks.Task<System.ValueTuple<TResult1, TResult2>> BackgroundTaskBuilder.MergeSources$W<TTaskLike1, TResult1, TResult2, TAwaiter1>(Microsoft.FSharp.Core.FSharpFunc<TTaskLike1, TAwaiter1> getAwaiter, Microsoft.FSharp.Core.FSharpFunc<TAwaiter1, TResult1> getResult, Microsoft.FSharp.Core.FSharpFunc<TAwaiter1, System.Boolean> get_IsCompleted, Microsoft.FSharp.Control.BackgroundTaskBuilder this, TTaskLike1 task1, System.Threading.Tasks.Task<TResult2> task2)`
- `where TAwaiter1 : System.Runtime.CompilerServices.ICriticalNotifyCompletion`
- `public static System.Threading.Tasks.Task<System.ValueTuple<TResult1, TResult2>> TaskBuilder.MergeSources<TTaskLike2, TResult1, TResult2, TAwaiter2>(Microsoft.FSharp.Control.TaskBuilder this, System.Threading.Tasks.Task<TResult1> task1, TTaskLike2 task2)`
- `where TAwaiter2 : System.Runtime.CompilerServices.ICriticalNotifyCompletion`
- `public static System.Threading.Tasks.Task<System.ValueTuple<TResult1, TResult2>> TaskBuilder.MergeSources<TTaskLike1, TResult1, TResult2, TAwaiter1>(Microsoft.FSharp.Control.TaskBuilder this, TTaskLike1 task1, System.Threading.Tasks.Task<TResult2> task2)`
- `where TAwaiter1 : System.Runtime.CompilerServices.ICriticalNotifyCompletion`
- `public static System.Threading.Tasks.Task<System.ValueTuple<TResult1, TResult2>> TaskBuilder.MergeSources<TResult1, TResult2>(Microsoft.FSharp.Control.TaskBuilder this, Microsoft.FSharp.Control.FSharpAsync<TResult1> computation1, Microsoft.FSharp.Control.FSharpAsync<TResult2> computation2)`
- `public static System.Threading.Tasks.Task<System.ValueTuple<TResult1, TResult2>> TaskBuilder.MergeSources<TResult1, TResult2>(Microsoft.FSharp.Control.TaskBuilder this, System.Threading.Tasks.Task<TResult1> task, Microsoft.FSharp.Control.FSharpAsync<TResult2> computation)`
- `public static System.Threading.Tasks.Task<System.ValueTuple<TResult1, TResult2>> TaskBuilder.MergeSources<TResult1, TResult2>(Microsoft.FSharp.Control.TaskBuilder this, Microsoft.FSharp.Control.FSharpAsync<TResult1> computation, System.Threading.Tasks.Task<TResult2> task)`
- `public static System.Threading.Tasks.Task<System.ValueTuple<TResult1, TResult2>> TaskBuilder.MergeSources$W<TTaskLike2, TResult1, TResult2, TAwaiter2>(Microsoft.FSharp.Core.FSharpFunc<TTaskLike2, TAwaiter2> getAwaiter, Microsoft.FSharp.Core.FSharpFunc<TAwaiter2, TResult2> getResult, Microsoft.FSharp.Core.FSharpFunc<TAwaiter2, System.Boolean> get_IsCompleted, Microsoft.FSharp.Control.TaskBuilder this, System.Threading.Tasks.Task<TResult1> task1, TTaskLike2 task2)`
- `where TAwaiter2 : System.Runtime.CompilerServices.ICriticalNotifyCompletion`
- `public static System.Threading.Tasks.Task<System.ValueTuple<TResult1, TResult2>> TaskBuilder.MergeSources$W<TTaskLike1, TResult1, TResult2, TAwaiter1>(Microsoft.FSharp.Core.FSharpFunc<TTaskLike1, TAwaiter1> getAwaiter, Microsoft.FSharp.Core.FSharpFunc<TAwaiter1, TResult1> getResult, Microsoft.FSharp.Core.FSharpFunc<TAwaiter1, System.Boolean> get_IsCompleted, Microsoft.FSharp.Control.TaskBuilder this, TTaskLike1 task1, System.Threading.Tasks.Task<TResult2> task2)`
- `where TAwaiter1 : System.Runtime.CompilerServices.ICriticalNotifyCompletion`
- `public static Microsoft.FSharp.Core.CompilerServices.ResumableCode<Microsoft.FSharp.Control.TaskStateMachineData<TOverall>, TResult2> TaskBuilderBase.Bind<TResult1, TOverall, TResult2>(Microsoft.FSharp.Control.TaskBuilderBase this, Microsoft.FSharp.Control.FSharpAsync<TResult1> computation, Microsoft.FSharp.Core.FSharpFunc<TResult1, Microsoft.FSharp.Core.CompilerServices.ResumableCode<Microsoft.FSharp.Control.TaskStateMachineData<TOverall>, TResult2>> continuation)`
- `public static Microsoft.FSharp.Core.CompilerServices.ResumableCode<Microsoft.FSharp.Control.TaskStateMachineData<T>, T> TaskBuilderBase.ReturnFrom<T>(Microsoft.FSharp.Control.TaskBuilderBase this, Microsoft.FSharp.Control.FSharpAsync<T> computation)`

## Microsoft.FSharp.Core

### ArrayExtensions (class [static])

- `public static System.Int32 String.GetReverseIndex(System.String str, System.Int32 rank, System.Int32 offset)`
- `public static System.Int32 [,,,]`1.GetReverseIndex<T>(T[] arr, System.Int32 dim, System.Int32 offset)`
- `public static System.Int32 [,,]`1.GetReverseIndex<T>(T[] arr, System.Int32 dim, System.Int32 offset)`
- `public static System.Int32 [,]`1.GetReverseIndex<T>(T[] arr, System.Int32 dim, System.Int32 offset)`
- `public static System.Int32 []`1.GetReverseIndex<T>(T[] arr, System.Int32 rank, System.Int32 offset)`

### Checked (class [static])

- `public static System.Byte ToByte<T>(T value)`
- `public static System.Byte ToByte$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Byte> op_Explicit, T value)`
- `public static System.SByte ToSByte<T>(T value)`
- `public static System.SByte ToSByte$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.SByte> op_Explicit, T value)`

### Checked (class [static])

- `public static System.Byte ToByte<T>(T value)`
- `public static System.Byte ToByte$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Byte> op_Explicit, T value)`
- `public static System.Char ToChar<T>(T value)`
- `public static System.Char ToChar$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Char> op_Explicit, T value)`
- `public static System.Int32 ToInt<T>(T value)`
- `public static System.Int32 ToInt$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Int32> op_Explicit, T value)`
- `public static System.Int16 ToInt16<T>(T value)`
- `public static System.Int16 ToInt16$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Int16> op_Explicit, T value)`
- `public static System.Int32 ToInt32<T>(T value)`
- `public static System.Int32 ToInt32$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Int32> op_Explicit, T value)`
- `public static System.Int64 ToInt64<T>(T value)`
- `public static System.Int64 ToInt64$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Int64> op_Explicit, T value)`
- `public static System.IntPtr ToIntPtr<T>(T value)`
- `public static System.IntPtr ToIntPtr$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.IntPtr> op_Explicit, T value)`
- `public static System.SByte ToSByte<T>(T value)`
- `public static System.SByte ToSByte$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.SByte> op_Explicit, T value)`
- `public static System.UInt16 ToUInt16<T>(T value)`
- `public static System.UInt16 ToUInt16$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.UInt16> op_Explicit, T value)`
- `public static System.UInt32 ToUInt32<T>(T value)`
- `public static System.UInt32 ToUInt32$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.UInt32> op_Explicit, T value)`
- `public static System.UInt64 ToUInt64<T>(T value)`
- `public static System.UInt64 ToUInt64$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.UInt64> op_Explicit, T value)`
- `public static System.UIntPtr ToUIntPtr<T>(T value)`
- `public static System.UIntPtr ToUIntPtr$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.UIntPtr> op_Explicit, T value)`
- `public static T3 op_Addition(T1 x, T2 y)`
- `public static T3 op_Addition$W(Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, T3>> op_Addition, T1 x, T2 y)`
- `public static T3 op_Multiply(T1 x, T2 y)`
- `public static T3 op_Multiply$W(Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, T3>> op_Multiply, T1 x, T2 y)`
- `public static T3 op_Subtraction(T1 x, T2 y)`
- `public static T3 op_Subtraction$W(Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, T3>> op_Subtraction, T1 x, T2 y)`
- `public static T op_UnaryNegation(T value)`
- `public static T op_UnaryNegation$W(Microsoft.FSharp.Core.FSharpFunc<T, T> op_UnaryNegation, T value)`

### Choice1Of2<T1, T2> (class) : Microsoft.FSharp.Core.FSharpChoice<T1, T2>, System.IEquatable<Microsoft.FSharp.Core.FSharpChoice<T1, T2>>, System.Collections.IStructuralEquatable, System.IComparable<Microsoft.FSharp.Core.FSharpChoice<T1, T2>>, System.IComparable, System.Collections.IStructuralComparable

- `public T1 Item { get; } (nullability: Unknown)`
- `public T1 get_Item()`

### Choice1Of3<T1, T2, T3> (class) : Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3>, System.IEquatable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3>>, System.Collections.IStructuralEquatable, System.IComparable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3>>, System.IComparable, System.Collections.IStructuralComparable

- `public T1 Item { get; } (nullability: Unknown)`
- `public T1 get_Item()`

### Choice1Of4<T1, T2, T3, T4> (class) : Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4>, System.IEquatable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4>>, System.Collections.IStructuralEquatable, System.IComparable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4>>, System.IComparable, System.Collections.IStructuralComparable

- `public T1 Item { get; } (nullability: Unknown)`
- `public T1 get_Item()`

### Choice1Of5<T1, T2, T3, T4, T5> (class) : Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5>, System.IEquatable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5>>, System.Collections.IStructuralEquatable, System.IComparable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5>>, System.IComparable, System.Collections.IStructuralComparable

- `public T1 Item { get; } (nullability: Unknown)`
- `public T1 get_Item()`

### Choice1Of6<T1, T2, T3, T4, T5, T6> (class) : Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6>, System.IEquatable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6>>, System.Collections.IStructuralEquatable, System.IComparable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6>>, System.IComparable, System.Collections.IStructuralComparable

- `public T1 Item { get; } (nullability: Unknown)`
- `public T1 get_Item()`

### Choice1Of7<T1, T2, T3, T4, T5, T6, T7> (class) : Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6, T7>, System.IEquatable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6, T7>>, System.Collections.IStructuralEquatable, System.IComparable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6, T7>>, System.IComparable, System.Collections.IStructuralComparable

- `public T1 Item { get; } (nullability: Unknown)`
- `public T1 get_Item()`

### Choice2Of2<T1, T2> (class) : Microsoft.FSharp.Core.FSharpChoice<T1, T2>, System.IEquatable<Microsoft.FSharp.Core.FSharpChoice<T1, T2>>, System.Collections.IStructuralEquatable, System.IComparable<Microsoft.FSharp.Core.FSharpChoice<T1, T2>>, System.IComparable, System.Collections.IStructuralComparable

- `public T2 Item { get; } (nullability: Unknown)`
- `public T2 get_Item()`

### Choice2Of3<T1, T2, T3> (class) : Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3>, System.IEquatable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3>>, System.Collections.IStructuralEquatable, System.IComparable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3>>, System.IComparable, System.Collections.IStructuralComparable

- `public T2 Item { get; } (nullability: Unknown)`
- `public T2 get_Item()`

### Choice2Of4<T1, T2, T3, T4> (class) : Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4>, System.IEquatable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4>>, System.Collections.IStructuralEquatable, System.IComparable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4>>, System.IComparable, System.Collections.IStructuralComparable

- `public T2 Item { get; } (nullability: Unknown)`
- `public T2 get_Item()`

### Choice2Of5<T1, T2, T3, T4, T5> (class) : Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5>, System.IEquatable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5>>, System.Collections.IStructuralEquatable, System.IComparable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5>>, System.IComparable, System.Collections.IStructuralComparable

- `public T2 Item { get; } (nullability: Unknown)`
- `public T2 get_Item()`

### Choice2Of6<T1, T2, T3, T4, T5, T6> (class) : Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6>, System.IEquatable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6>>, System.Collections.IStructuralEquatable, System.IComparable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6>>, System.IComparable, System.Collections.IStructuralComparable

- `public T2 Item { get; } (nullability: Unknown)`
- `public T2 get_Item()`

### Choice2Of7<T1, T2, T3, T4, T5, T6, T7> (class) : Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6, T7>, System.IEquatable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6, T7>>, System.Collections.IStructuralEquatable, System.IComparable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6, T7>>, System.IComparable, System.Collections.IStructuralComparable

- `public T2 Item { get; } (nullability: Unknown)`
- `public T2 get_Item()`

### Choice3Of3<T1, T2, T3> (class) : Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3>, System.IEquatable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3>>, System.Collections.IStructuralEquatable, System.IComparable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3>>, System.IComparable, System.Collections.IStructuralComparable

- `public T3 Item { get; } (nullability: Unknown)`
- `public T3 get_Item()`

### Choice3Of4<T1, T2, T3, T4> (class) : Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4>, System.IEquatable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4>>, System.Collections.IStructuralEquatable, System.IComparable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4>>, System.IComparable, System.Collections.IStructuralComparable

- `public T3 Item { get; } (nullability: Unknown)`
- `public T3 get_Item()`

### Choice3Of5<T1, T2, T3, T4, T5> (class) : Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5>, System.IEquatable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5>>, System.Collections.IStructuralEquatable, System.IComparable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5>>, System.IComparable, System.Collections.IStructuralComparable

- `public T3 Item { get; } (nullability: Unknown)`
- `public T3 get_Item()`

### Choice3Of6<T1, T2, T3, T4, T5, T6> (class) : Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6>, System.IEquatable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6>>, System.Collections.IStructuralEquatable, System.IComparable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6>>, System.IComparable, System.Collections.IStructuralComparable

- `public T3 Item { get; } (nullability: Unknown)`
- `public T3 get_Item()`

### Choice3Of7<T1, T2, T3, T4, T5, T6, T7> (class) : Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6, T7>, System.IEquatable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6, T7>>, System.Collections.IStructuralEquatable, System.IComparable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6, T7>>, System.IComparable, System.Collections.IStructuralComparable

- `public T3 Item { get; } (nullability: Unknown)`
- `public T3 get_Item()`

### Choice4Of4<T1, T2, T3, T4> (class) : Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4>, System.IEquatable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4>>, System.Collections.IStructuralEquatable, System.IComparable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4>>, System.IComparable, System.Collections.IStructuralComparable

- `public T4 Item { get; } (nullability: Unknown)`
- `public T4 get_Item()`

### Choice4Of5<T1, T2, T3, T4, T5> (class) : Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5>, System.IEquatable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5>>, System.Collections.IStructuralEquatable, System.IComparable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5>>, System.IComparable, System.Collections.IStructuralComparable

- `public T4 Item { get; } (nullability: Unknown)`
- `public T4 get_Item()`

### Choice4Of6<T1, T2, T3, T4, T5, T6> (class) : Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6>, System.IEquatable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6>>, System.Collections.IStructuralEquatable, System.IComparable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6>>, System.IComparable, System.Collections.IStructuralComparable

- `public T4 Item { get; } (nullability: Unknown)`
- `public T4 get_Item()`

### Choice4Of7<T1, T2, T3, T4, T5, T6, T7> (class) : Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6, T7>, System.IEquatable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6, T7>>, System.Collections.IStructuralEquatable, System.IComparable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6, T7>>, System.IComparable, System.Collections.IStructuralComparable

- `public T4 Item { get; } (nullability: Unknown)`
- `public T4 get_Item()`

### Choice5Of5<T1, T2, T3, T4, T5> (class) : Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5>, System.IEquatable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5>>, System.Collections.IStructuralEquatable, System.IComparable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5>>, System.IComparable, System.Collections.IStructuralComparable

- `public T5 Item { get; } (nullability: Unknown)`
- `public T5 get_Item()`

### Choice5Of6<T1, T2, T3, T4, T5, T6> (class) : Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6>, System.IEquatable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6>>, System.Collections.IStructuralEquatable, System.IComparable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6>>, System.IComparable, System.Collections.IStructuralComparable

- `public T5 Item { get; } (nullability: Unknown)`
- `public T5 get_Item()`

### Choice5Of7<T1, T2, T3, T4, T5, T6, T7> (class) : Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6, T7>, System.IEquatable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6, T7>>, System.Collections.IStructuralEquatable, System.IComparable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6, T7>>, System.IComparable, System.Collections.IStructuralComparable

- `public T5 Item { get; } (nullability: Unknown)`
- `public T5 get_Item()`

### Choice6Of6<T1, T2, T3, T4, T5, T6> (class) : Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6>, System.IEquatable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6>>, System.Collections.IStructuralEquatable, System.IComparable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6>>, System.IComparable, System.Collections.IStructuralComparable

- `public T6 Item { get; } (nullability: Unknown)`
- `public T6 get_Item()`

### Choice6Of7<T1, T2, T3, T4, T5, T6, T7> (class) : Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6, T7>, System.IEquatable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6, T7>>, System.Collections.IStructuralEquatable, System.IComparable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6, T7>>, System.IComparable, System.Collections.IStructuralComparable

- `public T6 Item { get; } (nullability: Unknown)`
- `public T6 get_Item()`

### Choice7Of7<T1, T2, T3, T4, T5, T6, T7> (class) : Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6, T7>, System.IEquatable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6, T7>>, System.Collections.IStructuralEquatable, System.IComparable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6, T7>>, System.IComparable, System.Collections.IStructuralComparable

- `public T7 Item { get; } (nullability: Unknown)`
- `public T7 get_Item()`

### ErrorStrings (class [static])

- `public static System.String AddressOpNotFirstClassString { get; } (nullability: Unknown)`
- `public static System.String InputArrayEmptyString { get; } (nullability: Unknown)`
- `public static System.String InputMustBeNonNegativeString { get; } (nullability: Unknown)`
- `public static System.String InputSequenceEmptyString { get; } (nullability: Unknown)`
- `public static System.String NoNegateMinValueString { get; } (nullability: Unknown)`

### ExtraTopLevelOperators (class [static])

- `public static Microsoft.FSharp.Control.FSharpAsyncBuilder DefaultAsyncBuilder { get; } (nullability: Unknown)`
- `public static Microsoft.FSharp.Linq.QueryBuilder query { get; } (nullability: Unknown)`
- `public static T[] CreateArray2D<a, T>(System.Collections.Generic.IEnumerable<a> rows)`
- `where a : System.Collections.Generic.IEnumerable<T>`
- `public static System.Collections.Generic.IDictionary<TKey, TValue> CreateDictionary<TKey, TValue>(System.Collections.Generic.IEnumerable<System.Tuple<TKey, TValue>> keyValuePairs)`
- `public static System.Collections.Generic.IReadOnlyDictionary<TKey, TValue> CreateReadOnlyDictionary<TKey, TValue>(System.Collections.Generic.IEnumerable<System.Tuple<TKey, TValue>> keyValuePairs)`
- `public static Microsoft.FSharp.Collections.FSharpSet<T> CreateSet<T>(System.Collections.Generic.IEnumerable<T> elements)`
- `public static T LazyPattern<T>(System.Lazy<T> input)`
- `public static T PrintFormat<T>(Microsoft.FSharp.Core.PrintfFormat<T, System.IO.TextWriter, Microsoft.FSharp.Core.Unit, Microsoft.FSharp.Core.Unit> format)`
- `public static T PrintFormatLine<T>(Microsoft.FSharp.Core.PrintfFormat<T, System.IO.TextWriter, Microsoft.FSharp.Core.Unit, Microsoft.FSharp.Core.Unit> format)`
- `public static T PrintFormatLineToError<T>(Microsoft.FSharp.Core.PrintfFormat<T, System.IO.TextWriter, Microsoft.FSharp.Core.Unit, Microsoft.FSharp.Core.Unit> format)`
- `public static T PrintFormatLineToTextWriter<T>(System.IO.TextWriter textWriter, Microsoft.FSharp.Core.PrintfFormat<T, System.IO.TextWriter, Microsoft.FSharp.Core.Unit, Microsoft.FSharp.Core.Unit> format)`
- `public static T PrintFormatToError<T>(Microsoft.FSharp.Core.PrintfFormat<T, System.IO.TextWriter, Microsoft.FSharp.Core.Unit, Microsoft.FSharp.Core.Unit> format)`
- `public static T PrintFormatToString<T>(Microsoft.FSharp.Core.PrintfFormat<T, Microsoft.FSharp.Core.Unit, System.String, System.String> format)`
- `public static T PrintFormatToStringThenFail<T, TResult>(Microsoft.FSharp.Core.PrintfFormat<T, Microsoft.FSharp.Core.Unit, System.String, TResult> format)`
- `public static T PrintFormatToTextWriter<T>(System.IO.TextWriter textWriter, Microsoft.FSharp.Core.PrintfFormat<T, System.IO.TextWriter, Microsoft.FSharp.Core.Unit, Microsoft.FSharp.Core.Unit> format)`
- `public static T SpliceExpression<T>(Microsoft.FSharp.Quotations.FSharpExpr<T> expression)`
- `public static T SpliceUntypedExpression<T>(Microsoft.FSharp.Quotations.FSharpExpr expression)`
- `public static System.Byte ToByte<T>(T value)`
- `public static System.Byte ToByte$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Byte> op_Explicit, T value)`
- `public static System.Double ToDouble<T>(T value)`
- `public static System.Double ToDouble$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Double> op_Explicit, T value)`
- `public static System.SByte ToSByte<T>(T value)`
- `public static System.SByte ToSByte$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.SByte> op_Explicit, T value)`
- `public static System.Single ToSingle<T>(T value)`
- `public static System.Single ToSingle$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Single> op_Explicit, T value)`

### FSharpChoice`2<T1, T2> (class [abstract]) : System.IEquatable<Microsoft.FSharp.Core.FSharpChoice<T1, T2>>, System.Collections.IStructuralEquatable, System.IComparable<Microsoft.FSharp.Core.FSharpChoice<T1, T2>>, System.IComparable, System.Collections.IStructuralComparable

- `public System.Boolean IsChoice1Of2 { get; }`
- `public System.Boolean IsChoice2Of2 { get; }`
- `public System.Int32 Tag { get; }`
- `public System.Int32 CompareTo(Microsoft.FSharp.Core.FSharpChoice<T1, T2> obj)`
- `public System.Int32 CompareTo(System.Object obj)`
- `public System.Int32 CompareTo(System.Object obj, System.Collections.IComparer comp)`
- `public System.Boolean Equals(Microsoft.FSharp.Core.FSharpChoice<T1, T2> obj)`
- `public System.Boolean Equals(System.Object obj)`
- `public System.Boolean Equals(Microsoft.FSharp.Core.FSharpChoice<T1, T2> obj, System.Collections.IEqualityComparer comp)`
- `public System.Boolean Equals(System.Object obj, System.Collections.IEqualityComparer comp)`
- `public System.Int32 GetHashCode()`
- `public System.Int32 GetHashCode(System.Collections.IEqualityComparer comp)`
- `public static Microsoft.FSharp.Core.FSharpChoice<T1, T2> NewChoice1Of2(T1 item)`
- `public static Microsoft.FSharp.Core.FSharpChoice<T1, T2> NewChoice2Of2(T2 item)`
- `public System.Boolean get_IsChoice1Of2()`
- `public System.Boolean get_IsChoice2Of2()`
- `public System.Int32 get_Tag()`

### FSharpChoice`3<T1, T2, T3> (class [abstract]) : System.IEquatable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3>>, System.Collections.IStructuralEquatable, System.IComparable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3>>, System.IComparable, System.Collections.IStructuralComparable

- `public System.Boolean IsChoice1Of3 { get; }`
- `public System.Boolean IsChoice2Of3 { get; }`
- `public System.Boolean IsChoice3Of3 { get; }`
- `public System.Int32 Tag { get; }`
- `public System.Int32 CompareTo(Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3> obj)`
- `public System.Int32 CompareTo(System.Object obj)`
- `public System.Int32 CompareTo(System.Object obj, System.Collections.IComparer comp)`
- `public System.Boolean Equals(Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3> obj)`
- `public System.Boolean Equals(System.Object obj)`
- `public System.Boolean Equals(Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3> obj, System.Collections.IEqualityComparer comp)`
- `public System.Boolean Equals(System.Object obj, System.Collections.IEqualityComparer comp)`
- `public System.Int32 GetHashCode()`
- `public System.Int32 GetHashCode(System.Collections.IEqualityComparer comp)`
- `public static Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3> NewChoice1Of3(T1 item)`
- `public static Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3> NewChoice2Of3(T2 item)`
- `public static Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3> NewChoice3Of3(T3 item)`
- `public System.Boolean get_IsChoice1Of3()`
- `public System.Boolean get_IsChoice2Of3()`
- `public System.Boolean get_IsChoice3Of3()`
- `public System.Int32 get_Tag()`

### FSharpChoice`4<T1, T2, T3, T4> (class [abstract]) : System.IEquatable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4>>, System.Collections.IStructuralEquatable, System.IComparable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4>>, System.IComparable, System.Collections.IStructuralComparable

- `public System.Boolean IsChoice1Of4 { get; }`
- `public System.Boolean IsChoice2Of4 { get; }`
- `public System.Boolean IsChoice3Of4 { get; }`
- `public System.Boolean IsChoice4Of4 { get; }`
- `public System.Int32 Tag { get; }`
- `public System.Int32 CompareTo(Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4> obj)`
- `public System.Int32 CompareTo(System.Object obj)`
- `public System.Int32 CompareTo(System.Object obj, System.Collections.IComparer comp)`
- `public System.Boolean Equals(Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4> obj)`
- `public System.Boolean Equals(System.Object obj)`
- `public System.Boolean Equals(Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4> obj, System.Collections.IEqualityComparer comp)`
- `public System.Boolean Equals(System.Object obj, System.Collections.IEqualityComparer comp)`
- `public System.Int32 GetHashCode()`
- `public System.Int32 GetHashCode(System.Collections.IEqualityComparer comp)`
- `public static Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4> NewChoice1Of4(T1 item)`
- `public static Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4> NewChoice2Of4(T2 item)`
- `public static Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4> NewChoice3Of4(T3 item)`
- `public static Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4> NewChoice4Of4(T4 item)`
- `public System.Boolean get_IsChoice1Of4()`
- `public System.Boolean get_IsChoice2Of4()`
- `public System.Boolean get_IsChoice3Of4()`
- `public System.Boolean get_IsChoice4Of4()`
- `public System.Int32 get_Tag()`

### FSharpChoice`5<T1, T2, T3, T4, T5> (class [abstract]) : System.IEquatable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5>>, System.Collections.IStructuralEquatable, System.IComparable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5>>, System.IComparable, System.Collections.IStructuralComparable

- `public System.Boolean IsChoice1Of5 { get; }`
- `public System.Boolean IsChoice2Of5 { get; }`
- `public System.Boolean IsChoice3Of5 { get; }`
- `public System.Boolean IsChoice4Of5 { get; }`
- `public System.Boolean IsChoice5Of5 { get; }`
- `public System.Int32 Tag { get; }`
- `public System.Int32 CompareTo(Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5> obj)`
- `public System.Int32 CompareTo(System.Object obj)`
- `public System.Int32 CompareTo(System.Object obj, System.Collections.IComparer comp)`
- `public System.Boolean Equals(Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5> obj)`
- `public System.Boolean Equals(System.Object obj)`
- `public System.Boolean Equals(Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5> obj, System.Collections.IEqualityComparer comp)`
- `public System.Boolean Equals(System.Object obj, System.Collections.IEqualityComparer comp)`
- `public System.Int32 GetHashCode()`
- `public System.Int32 GetHashCode(System.Collections.IEqualityComparer comp)`
- `public static Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5> NewChoice1Of5(T1 item)`
- `public static Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5> NewChoice2Of5(T2 item)`
- `public static Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5> NewChoice3Of5(T3 item)`
- `public static Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5> NewChoice4Of5(T4 item)`
- `public static Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5> NewChoice5Of5(T5 item)`
- `public System.Boolean get_IsChoice1Of5()`
- `public System.Boolean get_IsChoice2Of5()`
- `public System.Boolean get_IsChoice3Of5()`
- `public System.Boolean get_IsChoice4Of5()`
- `public System.Boolean get_IsChoice5Of5()`
- `public System.Int32 get_Tag()`

### FSharpChoice`6<T1, T2, T3, T4, T5, T6> (class [abstract]) : System.IEquatable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6>>, System.Collections.IStructuralEquatable, System.IComparable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6>>, System.IComparable, System.Collections.IStructuralComparable

- `public System.Boolean IsChoice1Of6 { get; }`
- `public System.Boolean IsChoice2Of6 { get; }`
- `public System.Boolean IsChoice3Of6 { get; }`
- `public System.Boolean IsChoice4Of6 { get; }`
- `public System.Boolean IsChoice5Of6 { get; }`
- `public System.Boolean IsChoice6Of6 { get; }`
- `public System.Int32 Tag { get; }`
- `public System.Int32 CompareTo(Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6> obj)`
- `public System.Int32 CompareTo(System.Object obj)`
- `public System.Int32 CompareTo(System.Object obj, System.Collections.IComparer comp)`
- `public System.Boolean Equals(Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6> obj)`
- `public System.Boolean Equals(System.Object obj)`
- `public System.Boolean Equals(Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6> obj, System.Collections.IEqualityComparer comp)`
- `public System.Boolean Equals(System.Object obj, System.Collections.IEqualityComparer comp)`
- `public System.Int32 GetHashCode()`
- `public System.Int32 GetHashCode(System.Collections.IEqualityComparer comp)`
- `public static Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6> NewChoice1Of6(T1 item)`
- `public static Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6> NewChoice2Of6(T2 item)`
- `public static Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6> NewChoice3Of6(T3 item)`
- `public static Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6> NewChoice4Of6(T4 item)`
- `public static Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6> NewChoice5Of6(T5 item)`
- `public static Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6> NewChoice6Of6(T6 item)`
- `public System.Boolean get_IsChoice1Of6()`
- `public System.Boolean get_IsChoice2Of6()`
- `public System.Boolean get_IsChoice3Of6()`
- `public System.Boolean get_IsChoice4Of6()`
- `public System.Boolean get_IsChoice5Of6()`
- `public System.Boolean get_IsChoice6Of6()`
- `public System.Int32 get_Tag()`

### FSharpChoice`7<T1, T2, T3, T4, T5, T6, T7> (class [abstract]) : System.IEquatable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6, T7>>, System.Collections.IStructuralEquatable, System.IComparable<Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6, T7>>, System.IComparable, System.Collections.IStructuralComparable

- `public System.Boolean IsChoice1Of7 { get; }`
- `public System.Boolean IsChoice2Of7 { get; }`
- `public System.Boolean IsChoice3Of7 { get; }`
- `public System.Boolean IsChoice4Of7 { get; }`
- `public System.Boolean IsChoice5Of7 { get; }`
- `public System.Boolean IsChoice6Of7 { get; }`
- `public System.Boolean IsChoice7Of7 { get; }`
- `public System.Int32 Tag { get; }`
- `public System.Int32 CompareTo(Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6, T7> obj)`
- `public System.Int32 CompareTo(System.Object obj)`
- `public System.Int32 CompareTo(System.Object obj, System.Collections.IComparer comp)`
- `public System.Boolean Equals(Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6, T7> obj)`
- `public System.Boolean Equals(System.Object obj)`
- `public System.Boolean Equals(Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6, T7> obj, System.Collections.IEqualityComparer comp)`
- `public System.Boolean Equals(System.Object obj, System.Collections.IEqualityComparer comp)`
- `public System.Int32 GetHashCode()`
- `public System.Int32 GetHashCode(System.Collections.IEqualityComparer comp)`
- `public static Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6, T7> NewChoice1Of7(T1 item)`
- `public static Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6, T7> NewChoice2Of7(T2 item)`
- `public static Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6, T7> NewChoice3Of7(T3 item)`
- `public static Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6, T7> NewChoice4Of7(T4 item)`
- `public static Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6, T7> NewChoice5Of7(T5 item)`
- `public static Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6, T7> NewChoice6Of7(T6 item)`
- `public static Microsoft.FSharp.Core.FSharpChoice<T1, T2, T3, T4, T5, T6, T7> NewChoice7Of7(T7 item)`
- `public System.Boolean get_IsChoice1Of7()`
- `public System.Boolean get_IsChoice2Of7()`
- `public System.Boolean get_IsChoice3Of7()`
- `public System.Boolean get_IsChoice4Of7()`
- `public System.Boolean get_IsChoice5Of7()`
- `public System.Boolean get_IsChoice6Of7()`
- `public System.Boolean get_IsChoice7Of7()`
- `public System.Int32 get_Tag()`

### FSharpFunc`2<T, TResult> (class [abstract])

- `public FSharpFunc`2()`
- `public static Microsoft.FSharp.Core.FSharpFunc<T, TResult> FromConverter(System.Converter<T, TResult> converter)`
- `public TResult Invoke(T func)`
- `public static V InvokeFast<V>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<TResult, V>> func, T arg1, TResult arg2)`
- `public static W InvokeFast<V, W>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<TResult, Microsoft.FSharp.Core.FSharpFunc<V, W>>> func, T arg1, TResult arg2, V arg3)`
- `public static X InvokeFast<V, W, X>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<TResult, Microsoft.FSharp.Core.FSharpFunc<V, Microsoft.FSharp.Core.FSharpFunc<W, X>>>> func, T arg1, TResult arg2, V arg3, W arg4)`
- `public static Y InvokeFast<V, W, X, Y>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<TResult, Microsoft.FSharp.Core.FSharpFunc<V, Microsoft.FSharp.Core.FSharpFunc<W, Microsoft.FSharp.Core.FSharpFunc<X, Y>>>>> func, T arg1, TResult arg2, V arg3, W arg4, X arg5)`
- `public static System.Converter<T, TResult> ToConverter(Microsoft.FSharp.Core.FSharpFunc<T, TResult> func)`
- `public static Microsoft.FSharp.Core.FSharpFunc<T, TResult> op_Implicit(System.Converter<T, TResult> f)`
- `public static System.Converter<T, TResult> op_Implicit(Microsoft.FSharp.Core.FSharpFunc<T, TResult> func)`

### FSharpFunc`3<T1, T2, TResult> (class [abstract]) : Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, TResult>>

- `public FSharpFunc`3()`
- `public static Microsoft.FSharp.Core.OptimizedClosures+FSharpFunc<T1, T2, TResult> Adapt(Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, TResult>> func)`
- `public Microsoft.FSharp.Core.FSharpFunc<T2, TResult> Invoke(T1 t)`
- `public TResult Invoke(T1 arg1, T2 arg2)`

### FSharpFunc`4<T1, T2, T3, TResult> (class [abstract]) : Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, Microsoft.FSharp.Core.FSharpFunc<T3, TResult>>>

- `public FSharpFunc`4()`
- `public static Microsoft.FSharp.Core.OptimizedClosures+FSharpFunc<T1, T2, T3, TResult> Adapt(Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, Microsoft.FSharp.Core.FSharpFunc<T3, TResult>>> func)`
- `public Microsoft.FSharp.Core.FSharpFunc<T2, Microsoft.FSharp.Core.FSharpFunc<T3, TResult>> Invoke(T1 t)`
- `public TResult Invoke(T1 arg1, T2 arg2, T3 arg3)`

### FSharpFunc`5<T1, T2, T3, T4, TResult> (class [abstract]) : Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, Microsoft.FSharp.Core.FSharpFunc<T3, Microsoft.FSharp.Core.FSharpFunc<T4, TResult>>>>

- `public FSharpFunc`5()`
- `public static Microsoft.FSharp.Core.OptimizedClosures+FSharpFunc<T1, T2, T3, T4, TResult> Adapt(Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, Microsoft.FSharp.Core.FSharpFunc<T3, Microsoft.FSharp.Core.FSharpFunc<T4, TResult>>>> func)`
- `public Microsoft.FSharp.Core.FSharpFunc<T2, Microsoft.FSharp.Core.FSharpFunc<T3, Microsoft.FSharp.Core.FSharpFunc<T4, TResult>>> Invoke(T1 t)`
- `public TResult Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4)`

### FSharpFunc`6<T1, T2, T3, T4, T5, TResult> (class [abstract]) : Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, Microsoft.FSharp.Core.FSharpFunc<T3, Microsoft.FSharp.Core.FSharpFunc<T4, Microsoft.FSharp.Core.FSharpFunc<T5, TResult>>>>>

- `public FSharpFunc`6()`
- `public static Microsoft.FSharp.Core.OptimizedClosures+FSharpFunc<T1, T2, T3, T4, T5, TResult> Adapt(Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, Microsoft.FSharp.Core.FSharpFunc<T3, Microsoft.FSharp.Core.FSharpFunc<T4, Microsoft.FSharp.Core.FSharpFunc<T5, TResult>>>>> func)`
- `public Microsoft.FSharp.Core.FSharpFunc<T2, Microsoft.FSharp.Core.FSharpFunc<T3, Microsoft.FSharp.Core.FSharpFunc<T4, Microsoft.FSharp.Core.FSharpFunc<T5, TResult>>>> Invoke(T1 t)`
- `public TResult Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)`

### FSharpOption`1<T> (class [sealed]) : System.IEquatable<Microsoft.FSharp.Core.FSharpOption<T>>, System.Collections.IStructuralEquatable, System.IComparable<Microsoft.FSharp.Core.FSharpOption<T>>, System.IComparable, System.Collections.IStructuralComparable

- `public FSharpOption`1(T value)`
- `public static System.Boolean IsNone(Microsoft.FSharp.Core.FSharpOption<T> ) { get; }`
- `public static System.Boolean IsSome(Microsoft.FSharp.Core.FSharpOption<T> ) { get; }`
- `public static Microsoft.FSharp.Core.FSharpOption<T> None { get; } (nullability: Unknown)`
- `public T Value { get; } (nullability: Unknown)`
- `public System.Int32 CompareTo(Microsoft.FSharp.Core.FSharpOption<T> obj)`
- `public System.Int32 CompareTo(System.Object obj)`
- `public System.Int32 CompareTo(System.Object obj, System.Collections.IComparer comp)`
- `public System.Boolean Equals(Microsoft.FSharp.Core.FSharpOption<T> obj)`
- `public System.Boolean Equals(System.Object obj)`
- `public System.Boolean Equals(System.Object obj, System.Collections.IEqualityComparer comp)`
- `public static System.Boolean Equals(Microsoft.FSharp.Core.FSharpOption<T> this, Microsoft.FSharp.Core.FSharpOption<T> obj, System.Collections.IEqualityComparer comp)`
- `public System.Int32 GetHashCode()`
- `public System.Int32 GetHashCode(System.Collections.IEqualityComparer comp)`
- `public static System.Int32 GetTag(Microsoft.FSharp.Core.FSharpOption<T> )`
- `public static Microsoft.FSharp.Core.FSharpOption<T> Some(T value)`
- `public System.String ToString()`
- `public static Microsoft.FSharp.Core.FSharpOption<T> get_None()`
- `public T get_Value()`
- `public static Microsoft.FSharp.Core.FSharpOption<T> op_Implicit(T value)`

### FSharpResult`2<T, TError> (struct) : System.IEquatable<Microsoft.FSharp.Core.FSharpResult<T, TError>>, System.Collections.IStructuralEquatable, System.IComparable<Microsoft.FSharp.Core.FSharpResult<T, TError>>, System.IComparable, System.Collections.IStructuralComparable

- `public TError ErrorValue { get; } (nullability: Unknown)`
- `public System.Boolean IsError { get; }`
- `public System.Boolean IsOk { get; }`
- `public T ResultValue { get; } (nullability: Unknown)`
- `public System.Int32 Tag { get; }`
- `public System.Int32 CompareTo(Microsoft.FSharp.Core.FSharpResult<T, TError> obj)`
- `public System.Int32 CompareTo(System.Object obj)`
- `public System.Int32 CompareTo(System.Object obj, System.Collections.IComparer comp)`
- `public System.Boolean Equals(Microsoft.FSharp.Core.FSharpResult<T, TError> obj)`
- `public System.Boolean Equals(System.Object obj)`
- `public System.Boolean Equals(Microsoft.FSharp.Core.FSharpResult<T, TError> obj, System.Collections.IEqualityComparer comp)`
- `public System.Boolean Equals(System.Object obj, System.Collections.IEqualityComparer comp)`
- `public System.Int32 GetHashCode()`
- `public System.Int32 GetHashCode(System.Collections.IEqualityComparer comp)`
- `public static Microsoft.FSharp.Core.FSharpResult<T, TError> NewError(TError errorValue)`
- `public static Microsoft.FSharp.Core.FSharpResult<T, TError> NewOk(T resultValue)`
- `public TError get_ErrorValue()`
- `public System.Boolean get_IsError()`
- `public System.Boolean get_IsOk()`
- `public T get_ResultValue()`
- `public System.Int32 get_Tag()`

### FSharpTypeFunc (class [abstract])

- `public FSharpTypeFunc()`
- `public System.Object Specialize<T>()`

### FSharpValueOption`1<T> (struct) : System.IEquatable<Microsoft.FSharp.Core.FSharpValueOption<T>>, System.Collections.IStructuralEquatable, System.IComparable<Microsoft.FSharp.Core.FSharpValueOption<T>>, System.IComparable, System.Collections.IStructuralComparable

- `public System.Boolean IsNone { get; }`
- `public System.Boolean IsSome { get; }`
- `public System.Boolean IsValueNone { get; }`
- `public System.Boolean IsValueSome { get; }`
- `public T Item { get; } (nullability: Unknown)`
- `public static Microsoft.FSharp.Core.FSharpValueOption<T> None { get; }`
- `public System.Int32 Tag { get; }`
- `public T Value { get; } (nullability: Unknown)`
- `public static Microsoft.FSharp.Core.FSharpValueOption<T> ValueNone { get; }`
- `public System.Int32 CompareTo(Microsoft.FSharp.Core.FSharpValueOption<T> obj)`
- `public System.Int32 CompareTo(System.Object obj)`
- `public System.Int32 CompareTo(System.Object obj, System.Collections.IComparer comp)`
- `public System.Boolean Equals(Microsoft.FSharp.Core.FSharpValueOption<T> obj)`
- `public System.Boolean Equals(System.Object obj)`
- `public System.Boolean Equals(Microsoft.FSharp.Core.FSharpValueOption<T> obj, System.Collections.IEqualityComparer comp)`
- `public System.Boolean Equals(System.Object obj, System.Collections.IEqualityComparer comp)`
- `public System.Int32 GetHashCode()`
- `public System.Int32 GetHashCode(System.Collections.IEqualityComparer comp)`
- `public static Microsoft.FSharp.Core.FSharpValueOption<T> NewValueSome(T item)`
- `public static Microsoft.FSharp.Core.FSharpValueOption<T> Some(T value)`
- `public System.String ToString()`
- `public System.Boolean get_IsValueNone()`
- `public System.Boolean get_IsValueSome()`
- `public T get_Item()`
- `public System.Int32 get_Tag()`
- `public static Microsoft.FSharp.Core.FSharpValueOption<T> get_ValueNone()`
- `public static Microsoft.FSharp.Core.FSharpValueOption<T> op_Implicit(T value)`

### FuncConvert (class [static])

- `public static Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, Microsoft.FSharp.Core.Unit> FromAction(System.Action action)`
- `public static Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.Unit> FromAction<T>(System.Action<T> action)`
- `public static Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, Microsoft.FSharp.Core.Unit>> FromAction<T1, T2>(System.Action<T1, T2> action)`
- `public static Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, Microsoft.FSharp.Core.FSharpFunc<T3, Microsoft.FSharp.Core.Unit>>> FromAction<T1, T2, T3>(System.Action<T1, T2, T3> action)`
- `public static Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, Microsoft.FSharp.Core.FSharpFunc<T3, Microsoft.FSharp.Core.FSharpFunc<T4, Microsoft.FSharp.Core.Unit>>>> FromAction<T1, T2, T3, T4>(System.Action<T1, T2, T3, T4> action)`
- `public static Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, Microsoft.FSharp.Core.FSharpFunc<T3, Microsoft.FSharp.Core.FSharpFunc<T4, Microsoft.FSharp.Core.FSharpFunc<T5, Microsoft.FSharp.Core.Unit>>>>> FromAction<T1, T2, T3, T4, T5>(System.Action<T1, T2, T3, T4, T5> action)`
- `public static Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, T> FromFunc<T>(System.Func<T> func)`
- `public static Microsoft.FSharp.Core.FSharpFunc<T, TResult> FromFunc<T, TResult>(System.Func<T, TResult> func)`
- `public static Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, TResult>> FromFunc<T1, T2, TResult>(System.Func<T1, T2, TResult> func)`
- `public static Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, Microsoft.FSharp.Core.FSharpFunc<T3, TResult>>> FromFunc<T1, T2, T3, TResult>(System.Func<T1, T2, T3, TResult> func)`
- `public static Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, Microsoft.FSharp.Core.FSharpFunc<T3, Microsoft.FSharp.Core.FSharpFunc<T4, TResult>>>> FromFunc<T1, T2, T3, T4, TResult>(System.Func<T1, T2, T3, T4, TResult> func)`
- `public static Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, Microsoft.FSharp.Core.FSharpFunc<T3, Microsoft.FSharp.Core.FSharpFunc<T4, Microsoft.FSharp.Core.FSharpFunc<T5, TResult>>>>> FromFunc<T1, T2, T3, T4, T5, TResult>(System.Func<T1, T2, T3, T4, T5, TResult> func)`
- `public static Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, TResult>> FuncFromTupled<T1, T2, TResult>(Microsoft.FSharp.Core.FSharpFunc<System.Tuple<T1, T2>, TResult> func)`
- `public static Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, Microsoft.FSharp.Core.FSharpFunc<T3, TResult>>> FuncFromTupled<T1, T2, T3, TResult>(Microsoft.FSharp.Core.FSharpFunc<System.Tuple<T1, T2, T3>, TResult> func)`
- `public static Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, Microsoft.FSharp.Core.FSharpFunc<T3, Microsoft.FSharp.Core.FSharpFunc<T4, TResult>>>> FuncFromTupled<T1, T2, T3, T4, TResult>(Microsoft.FSharp.Core.FSharpFunc<System.Tuple<T1, T2, T3, T4>, TResult> func)`
- `public static Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, Microsoft.FSharp.Core.FSharpFunc<T3, Microsoft.FSharp.Core.FSharpFunc<T4, Microsoft.FSharp.Core.FSharpFunc<T5, TResult>>>>> FuncFromTupled<T1, T2, T3, T4, T5, TResult>(Microsoft.FSharp.Core.FSharpFunc<System.Tuple<T1, T2, T3, T4, T5>, TResult> func)`
- `public static Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.Unit> ToFSharpFunc<T>(System.Action<T> action)`
- `public static Microsoft.FSharp.Core.FSharpFunc<T, TResult> ToFSharpFunc<T, TResult>(System.Converter<T, TResult> converter)`

### HashCompare (class [static])

- `public static System.Int32 FastCompareTuple2<T1, T2>(System.Collections.IComparer comparer, System.Tuple<T1, T2> x1, System.Tuple<T1, T2> y1)`
- `public static System.Int32 FastCompareTuple3<T1, T2, T3>(System.Collections.IComparer comparer, System.Tuple<T1, T2, T3> x1, System.Tuple<T1, T2, T3> y1)`
- `public static System.Int32 FastCompareTuple4<T1, T2, T3, T4>(System.Collections.IComparer comparer, System.Tuple<T1, T2, T3, T4> x1, System.Tuple<T1, T2, T3, T4> y1)`
- `public static System.Int32 FastCompareTuple5<T1, T2, T3, T4, T5>(System.Collections.IComparer comparer, System.Tuple<T1, T2, T3, T4, T5> x1, System.Tuple<T1, T2, T3, T4, T5> y1)`
- `public static System.Boolean FastEqualsTuple2<T1, T2>(System.Collections.IEqualityComparer comparer, System.Tuple<T1, T2> x1, System.Tuple<T1, T2> y1)`
- `public static System.Boolean FastEqualsTuple3<T1, T2, T3>(System.Collections.IEqualityComparer comparer, System.Tuple<T1, T2, T3> x1, System.Tuple<T1, T2, T3> y1)`
- `public static System.Boolean FastEqualsTuple4<T1, T2, T3, T4>(System.Collections.IEqualityComparer comparer, System.Tuple<T1, T2, T3, T4> x1, System.Tuple<T1, T2, T3, T4> y1)`
- `public static System.Boolean FastEqualsTuple5<T1, T2, T3, T4, T5>(System.Collections.IEqualityComparer comparer, System.Tuple<T1, T2, T3, T4, T5> x1, System.Tuple<T1, T2, T3, T4, T5> y1)`
- `public static System.Int32 FastHashTuple2<T1, T2>(System.Collections.IEqualityComparer comparer, System.Tuple<T1, T2> x1)`
- `public static System.Int32 FastHashTuple3<T1, T2, T3>(System.Collections.IEqualityComparer comparer, System.Tuple<T1, T2, T3> x1)`
- `public static System.Int32 FastHashTuple4<T1, T2, T3, T4>(System.Collections.IEqualityComparer comparer, System.Tuple<T1, T2, T3, T4> x1)`
- `public static System.Int32 FastHashTuple5<T1, T2, T3, T4, T5>(System.Collections.IEqualityComparer comparer, System.Tuple<T1, T2, T3, T4, T5> x1)`
- `public static System.Int32 GenericComparisonIntrinsic<T>(T x, T y)`
- `public static System.Int32 GenericComparisonWithComparerIntrinsic<T>(System.Collections.IComparer comp, T x, T y)`
- `public static System.Boolean GenericEqualityERIntrinsic<T>(T x, T y)`
- `public static System.Boolean GenericEqualityIntrinsic<T>(T x, T y)`
- `public static System.Boolean GenericEqualityWithComparerIntrinsic<T>(System.Collections.IEqualityComparer comp, T x, T y)`
- `public static System.Boolean GenericGreaterOrEqualIntrinsic<T>(T x, T y)`
- `public static System.Boolean GenericGreaterThanIntrinsic<T>(T x, T y)`
- `public static System.Int32 GenericHashIntrinsic<T>(T input)`
- `public static System.Int32 GenericHashWithComparerIntrinsic<T>(System.Collections.IEqualityComparer comp, T input)`
- `public static System.Boolean GenericLessOrEqualIntrinsic<T>(T x, T y)`
- `public static System.Boolean GenericLessThanIntrinsic<T>(T x, T y)`
- `public static System.Int32 LimitedGenericHashIntrinsic<T>(System.Int32 limit, T input)`
- `public static System.Boolean PhysicalEqualityIntrinsic<T>(T x, T y)`
- `where T : class`
- `public static System.Int32 PhysicalHashIntrinsic<T>(T input)`
- `where T : class`

### IntrinsicFunctions (class [static])

- `public static T CheckThis<T>(T x)`
- `where T : class`
- `public static T CreateInstance<T>()`
- `where T : new()`
- `public static System.Void Dispose<T>(T resource)`
- `where T : System.IDisposable`
- `public static System.Void FailInit()`
- `public static System.Void FailStaticInit()`
- `public static T GetArray<T>(T[] source, System.Int32 index)`
- `public static T GetArray2D<T>(T[] source, System.Int32 index1, System.Int32 index2)`
- `public static T GetArray3D<T>(T[] source, System.Int32 index1, System.Int32 index2, System.Int32 index3)`
- `public static T GetArray4D<T>(T[] source, System.Int32 index1, System.Int32 index2, System.Int32 index3, System.Int32 index4)`
- `public static System.Char GetString(System.String source, System.Int32 index)`
- `public static System.Decimal MakeDecimal(System.Int32 low, System.Int32 medium, System.Int32 high, System.Boolean isNegative, System.Byte scale)`
- `public static System.Void SetArray<T>(T[] target, System.Int32 index, T value)`
- `public static System.Void SetArray2D<T>(T[] target, System.Int32 index1, System.Int32 index2, T value)`
- `public static System.Void SetArray3D<T>(T[] target, System.Int32 index1, System.Int32 index2, System.Int32 index3, T value)`
- `public static System.Void SetArray4D<T>(T[] target, System.Int32 index1, System.Int32 index2, System.Int32 index3, System.Int32 index4, T value)`
- `public static System.Boolean TypeTestFast<T>(System.Object source)`
- `public static System.Boolean TypeTestGeneric<T>(System.Object source)`
- `public static T UnboxFast<T>(System.Object source)`
- `public static T UnboxGeneric<T>(System.Object source)`

### IntrinsicOperators (class [static])

- `public static System.Boolean Or(System.Boolean e1, System.Boolean e2)`
- `public static T& op_AddressOf(T obj)`
- `public static System.Boolean op_Amp(System.Boolean e1, System.Boolean e2)`
- `public static System.Boolean op_BooleanAnd(System.Boolean e1, System.Boolean e2)`
- `public static System.Boolean op_BooleanOr(System.Boolean e1, System.Boolean e2)`
- `public static System.IntPtr op_IntegerAddressOf(T obj)`

### LanguagePrimitives (class [static])

- `public static System.Collections.IComparer GenericComparer { get; } (nullability: Unknown)`
- `public static System.Collections.IEqualityComparer GenericEqualityComparer { get; } (nullability: Unknown)`
- `public static System.Collections.IEqualityComparer GenericEqualityERComparer { get; } (nullability: Unknown)`
- `public static TResult AdditionDynamic<T1, T2, TResult>(T1 x, T2 y)`
- `public static TResult BitwiseAndDynamic<T1, T2, TResult>(T1 x, T2 y)`
- `public static TResult BitwiseOrDynamic<T1, T2, TResult>(T1 x, T2 y)`
- `public static System.Byte ByteWithMeasure(System.Byte f)`
- `public static TResult CheckedAdditionDynamic<T1, T2, TResult>(T1 x, T2 y)`
- `public static TResult CheckedExplicitDynamic<T, TResult>(T value)`
- `public static TResult CheckedMultiplyDynamic<T1, T2, TResult>(T1 x, T2 y)`
- `public static TResult CheckedSubtractionDynamic<T1, T2, TResult>(T1 x, T2 y)`
- `public static TResult CheckedUnaryNegationDynamic<T, TResult>(T value)`
- `public static System.Decimal DecimalWithMeasure(System.Decimal f)`
- `public static T DivideByInt<T>(T x, System.Int32 y)`
- `public static T DivideByInt$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<System.Int32, T>> divideByInt, T x, System.Int32 y)`
- `public static T DivideByIntDynamic<T>(T x, System.Int32 n)`
- `public static TResult DivisionDynamic<T1, T2, TResult>(T1 x, T2 y)`
- `public static TEnum EnumOfValue<T, TEnum>(T value)`
- `public static T EnumToValue<TEnum, T>(TEnum enum)`
- `public static TResult EqualityDynamic<T1, T2, TResult>(T1 x, T2 y)`
- `public static TResult ExclusiveOrDynamic<T1, T2, TResult>(T1 x, T2 y)`
- `public static TResult ExplicitDynamic<T, TResult>(T value)`
- `public static System.Collections.Generic.IComparer<T> FastGenericComparer<T>()`
- `public static System.Collections.Generic.IComparer<T> FastGenericComparerFromTable<T>()`
- `public static System.Collections.Generic.IEqualityComparer<T> FastGenericEqualityComparer<T>()`
- `public static System.Collections.Generic.IEqualityComparer<T> FastGenericEqualityComparerFromTable<T>()`
- `public static System.Collections.Generic.IEqualityComparer<T> FastLimitedGenericEqualityComparer<T>(System.Int32 limit)`
- `public static System.Single Float32WithMeasure(System.Single f)`
- `public static System.Double FloatWithMeasure(System.Double f)`
- `public static System.Int32 GenericComparison<T>(T e1, T e2)`
- `public static System.Int32 GenericComparisonWithComparer<T>(System.Collections.IComparer comp, T e1, T e2)`
- `public static System.Boolean GenericEquality<T>(T e1, T e2)`
- `public static System.Boolean GenericEqualityER<T>(T e1, T e2)`
- `public static System.Boolean GenericEqualityWithComparer<T>(System.Collections.IEqualityComparer comp, T e1, T e2)`
- `public static System.Boolean GenericGreaterOrEqual<T>(T e1, T e2)`
- `public static System.Boolean GenericGreaterThan<T>(T e1, T e2)`
- `public static System.Int32 GenericHash<T>(T obj)`
- `public static System.Int32 GenericHashWithComparer<T>(System.Collections.IEqualityComparer comparer, T obj)`
- `public static System.Boolean GenericLessOrEqual<T>(T e1, T e2)`
- `public static System.Boolean GenericLessThan<T>(T e1, T e2)`
- `public static System.Int32 GenericLimitedHash<T>(System.Int32 limit, T obj)`
- `public static T GenericMaximum<T>(T e1, T e2)`
- `public static T GenericMinimum<T>(T e1, T e2)`
- `public static T GenericOne<T>()`
- `public static T GenericOne$W<T>(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, T> get_One)`
- `public static T GenericOneDynamic<T>()`
- `public static T GenericZero<T>()`
- `public static T GenericZero$W<T>(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, T> get_Zero)`
- `public static T GenericZeroDynamic<T>()`
- `public static TResult GreaterThanDynamic<T1, T2, TResult>(T1 x, T2 y)`
- `public static TResult GreaterThanOrEqualDynamic<T1, T2, TResult>(T1 x, T2 y)`
- `public static TResult InequalityDynamic<T1, T2, TResult>(T1 x, T2 y)`
- `public static System.Int16 Int16WithMeasure(System.Int16 f)`
- `public static System.Int32 Int32WithMeasure(System.Int32 f)`
- `public static System.Int64 Int64WithMeasure(System.Int64 f)`
- `public static System.IntPtr IntPtrWithMeasure(System.IntPtr f)`
- `public static TResult LeftShiftDynamic<T1, T2, TResult>(T1 value, T2 shift)`
- `public static TResult LessThanDynamic<T1, T2, TResult>(T1 x, T2 y)`
- `public static TResult LessThanOrEqualDynamic<T1, T2, TResult>(T1 x, T2 y)`
- `public static TResult LogicalNotDynamic<T, TResult>(T value)`
- `public static TResult ModulusDynamic<T1, T2, TResult>(T1 x, T2 y)`
- `public static TResult MultiplyDynamic<T1, T2, TResult>(T1 x, T2 y)`
- `public static System.Int32 ParseInt32(System.String s)`
- `public static System.Int64 ParseInt64(System.String s)`
- `public static System.UInt32 ParseUInt32(System.String s)`
- `public static System.UInt64 ParseUInt64(System.String s)`
- `public static System.Boolean PhysicalEquality<T>(T e1, T e2)`
- `where T : class`
- `public static System.Int32 PhysicalHash<T>(T obj)`
- `where T : class`
- `public static TResult RightShiftDynamic<T1, T2, TResult>(T1 value, T2 shift)`
- `public static System.SByte SByteWithMeasure(System.SByte f)`
- `public static TResult SubtractionDynamic<T1, T2, TResult>(T1 x, T2 y)`
- `public static System.UInt16 UInt16WithMeasure(System.UInt16 f)`
- `public static System.UInt32 UInt32WithMeasure(System.UInt32 f)`
- `public static System.UInt64 UInt64WithMeasure(System.UInt64 f)`
- `public static System.UIntPtr UIntPtrWithMeasure(System.UIntPtr f)`
- `public static TResult UnaryNegationDynamic<T, TResult>(T value)`

### MatchFailureException (class) : System.Exception, System.Runtime.Serialization.ISerializable, System.Collections.IStructuralEquatable

- `public MatchFailureException()`
- `public MatchFailureException(System.String data0, System.Int32 data1, System.Int32 data2)`
- `public System.String Data0 { get; } (nullability: Unknown)`
- `public System.Int32 Data1 { get; }`
- `public System.Int32 Data2 { get; }`
- `public System.String Message { get; } (nullability: Unknown)`
- `public System.Boolean Equals(System.Object obj)`
- `public System.Boolean Equals(System.Exception obj, System.Collections.IEqualityComparer comp)`
- `public System.Boolean Equals(System.Object obj, System.Collections.IEqualityComparer comp)`
- `public System.Int32 GetHashCode()`
- `public System.Int32 GetHashCode(System.Collections.IEqualityComparer comp)`

### NonStructuralComparison (class [static])

- `public static System.Int32 Compare<T>(T e1, T e2)`
- `public static System.Int32 Compare$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean>> op_GreaterThan, Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean>> op_LessThan, T e1, T e2)`
- `public static System.Int32 Hash<T>(T value)`
- `public static T Max<T>(T e1, T e2)`
- `public static T Max$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean>> op_LessThan, T e1, T e2)`
- `public static T Min<T>(T e1, T e2)`
- `public static T Min$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean>> op_LessThan, T e1, T e2)`
- `public static System.Boolean op_Equality(T x, T y)`
- `public static System.Boolean op_Equality$W(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean>> op_Equality, T x, T y)`
- `public static System.Boolean op_GreaterThan(T x, TResult y)`
- `public static System.Boolean op_GreaterThan$W(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<TResult, System.Boolean>> op_GreaterThan, T x, TResult y)`
- `public static System.Boolean op_GreaterThanOrEqual(T x, TResult y)`
- `public static System.Boolean op_GreaterThanOrEqual$W(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<TResult, System.Boolean>> op_GreaterThanOrEqual, T x, TResult y)`
- `public static System.Boolean op_Inequality(T x, T y)`
- `public static System.Boolean op_Inequality$W(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean>> op_Inequality, T x, T y)`
- `public static System.Boolean op_LessThan(T x, TResult y)`
- `public static System.Boolean op_LessThan$W(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<TResult, System.Boolean>> op_LessThan, T x, TResult y)`
- `public static System.Boolean op_LessThanOrEqual(T x, TResult y)`
- `public static System.Boolean op_LessThanOrEqual$W(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<TResult, System.Boolean>> op_LessThanOrEqual, T x, TResult y)`

### OperatorIntrinsics (class [static])

- `public static T AbsDynamic<T>(T x)`
- `public static T AcosDynamic<T>(T x)`
- `public static T AsinDynamic<T>(T x)`
- `public static T2 Atan2Dynamic<T1, T2>(T1 y, T1 x)`
- `public static T AtanDynamic<T>(T x)`
- `public static T CeilingDynamic<T>(T x)`
- `public static T CosDynamic<T>(T x)`
- `public static T CoshDynamic<T>(T x)`
- `public static T ExpDynamic<T>(T x)`
- `public static T FloorDynamic<T>(T x)`
- `public static T[] GetArraySlice<T>(T[] source, Microsoft.FSharp.Core.FSharpOption<System.Int32> start, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish)`
- `public static T[] GetArraySlice2D<T>(T[] source, Microsoft.FSharp.Core.FSharpOption<System.Int32> start1, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish1, Microsoft.FSharp.Core.FSharpOption<System.Int32> start2, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish2)`
- `public static T[] GetArraySlice2DFixed1<T>(T[] source, System.Int32 index1, Microsoft.FSharp.Core.FSharpOption<System.Int32> start2, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish2)`
- `public static T[] GetArraySlice2DFixed2<T>(T[] source, Microsoft.FSharp.Core.FSharpOption<System.Int32> start1, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish1, System.Int32 index2)`
- `public static T[] GetArraySlice3D<T>(T[] source, Microsoft.FSharp.Core.FSharpOption<System.Int32> start1, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish1, Microsoft.FSharp.Core.FSharpOption<System.Int32> start2, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish2, Microsoft.FSharp.Core.FSharpOption<System.Int32> start3, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish3)`
- `public static T[] GetArraySlice3DFixedDouble1<T>(T[] source, System.Int32 index1, System.Int32 index2, Microsoft.FSharp.Core.FSharpOption<System.Int32> start3, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish3)`
- `public static T[] GetArraySlice3DFixedDouble2<T>(T[] source, System.Int32 index1, Microsoft.FSharp.Core.FSharpOption<System.Int32> start2, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish2, System.Int32 index3)`
- `public static T[] GetArraySlice3DFixedDouble3<T>(T[] source, Microsoft.FSharp.Core.FSharpOption<System.Int32> start1, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish1, System.Int32 index2, System.Int32 index3)`
- `public static T[] GetArraySlice3DFixedSingle1<T>(T[] source, System.Int32 index1, Microsoft.FSharp.Core.FSharpOption<System.Int32> start2, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish2, Microsoft.FSharp.Core.FSharpOption<System.Int32> start3, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish3)`
- `public static T[] GetArraySlice3DFixedSingle2<T>(T[] source, Microsoft.FSharp.Core.FSharpOption<System.Int32> start1, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish1, System.Int32 index2, Microsoft.FSharp.Core.FSharpOption<System.Int32> start3, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish3)`
- `public static T[] GetArraySlice3DFixedSingle3<T>(T[] source, Microsoft.FSharp.Core.FSharpOption<System.Int32> start1, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish1, Microsoft.FSharp.Core.FSharpOption<System.Int32> start2, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish2, System.Int32 index3)`
- `public static T[] GetArraySlice4D<T>(T[] source, Microsoft.FSharp.Core.FSharpOption<System.Int32> start1, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish1, Microsoft.FSharp.Core.FSharpOption<System.Int32> start2, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish2, Microsoft.FSharp.Core.FSharpOption<System.Int32> start3, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish3, Microsoft.FSharp.Core.FSharpOption<System.Int32> start4, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish4)`
- `public static T[] GetArraySlice4DFixedDouble1<T>(T[] source, System.Int32 index1, System.Int32 index2, Microsoft.FSharp.Core.FSharpOption<System.Int32> start3, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish3, Microsoft.FSharp.Core.FSharpOption<System.Int32> start4, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish4)`
- `public static T[] GetArraySlice4DFixedDouble2<T>(T[] source, System.Int32 index1, Microsoft.FSharp.Core.FSharpOption<System.Int32> start2, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish2, System.Int32 index3, Microsoft.FSharp.Core.FSharpOption<System.Int32> start4, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish4)`
- `public static T[] GetArraySlice4DFixedDouble3<T>(T[] source, System.Int32 index1, Microsoft.FSharp.Core.FSharpOption<System.Int32> start2, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish2, Microsoft.FSharp.Core.FSharpOption<System.Int32> start3, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish3, System.Int32 index4)`
- `public static T[] GetArraySlice4DFixedDouble4<T>(T[] source, Microsoft.FSharp.Core.FSharpOption<System.Int32> start1, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish1, System.Int32 index2, System.Int32 index3, Microsoft.FSharp.Core.FSharpOption<System.Int32> start4, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish4)`
- `public static T[] GetArraySlice4DFixedDouble5<T>(T[] source, Microsoft.FSharp.Core.FSharpOption<System.Int32> start1, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish1, System.Int32 index2, Microsoft.FSharp.Core.FSharpOption<System.Int32> start3, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish3, System.Int32 index4)`
- `public static T[] GetArraySlice4DFixedDouble6<T>(T[] source, Microsoft.FSharp.Core.FSharpOption<System.Int32> start1, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish1, Microsoft.FSharp.Core.FSharpOption<System.Int32> start2, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish2, System.Int32 index3, System.Int32 index4)`
- `public static T[] GetArraySlice4DFixedSingle1<T>(T[] source, System.Int32 index1, Microsoft.FSharp.Core.FSharpOption<System.Int32> start2, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish2, Microsoft.FSharp.Core.FSharpOption<System.Int32> start3, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish3, Microsoft.FSharp.Core.FSharpOption<System.Int32> start4, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish4)`
- `public static T[] GetArraySlice4DFixedSingle2<T>(T[] source, Microsoft.FSharp.Core.FSharpOption<System.Int32> start1, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish1, System.Int32 index2, Microsoft.FSharp.Core.FSharpOption<System.Int32> start3, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish3, Microsoft.FSharp.Core.FSharpOption<System.Int32> start4, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish4)`
- `public static T[] GetArraySlice4DFixedSingle3<T>(T[] source, Microsoft.FSharp.Core.FSharpOption<System.Int32> start1, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish1, Microsoft.FSharp.Core.FSharpOption<System.Int32> start2, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish2, System.Int32 index3, Microsoft.FSharp.Core.FSharpOption<System.Int32> start4, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish4)`
- `public static T[] GetArraySlice4DFixedSingle4<T>(T[] source, Microsoft.FSharp.Core.FSharpOption<System.Int32> start1, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish1, Microsoft.FSharp.Core.FSharpOption<System.Int32> start2, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish2, Microsoft.FSharp.Core.FSharpOption<System.Int32> start3, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish3, System.Int32 index4)`
- `public static T[] GetArraySlice4DFixedTriple1<T>(T[] source, Microsoft.FSharp.Core.FSharpOption<System.Int32> start1, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish1, System.Int32 index2, System.Int32 index3, System.Int32 index4)`
- `public static T[] GetArraySlice4DFixedTriple2<T>(T[] source, System.Int32 index1, Microsoft.FSharp.Core.FSharpOption<System.Int32> start2, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish2, System.Int32 index3, System.Int32 index4)`
- `public static T[] GetArraySlice4DFixedTriple3<T>(T[] source, System.Int32 index1, System.Int32 index2, Microsoft.FSharp.Core.FSharpOption<System.Int32> start3, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish3, System.Int32 index4)`
- `public static T[] GetArraySlice4DFixedTriple4<T>(T[] source, System.Int32 index1, System.Int32 index2, System.Int32 index3, Microsoft.FSharp.Core.FSharpOption<System.Int32> start4, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish4)`
- `public static System.String GetStringSlice(System.String source, Microsoft.FSharp.Core.FSharpOption<System.Int32> start, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish)`
- `public static T Log10Dynamic<T>(T x)`
- `public static T LogDynamic<T>(T x)`
- `public static System.Byte PowByte(System.Byte x, System.Int32 n)`
- `public static System.Decimal PowDecimal(System.Decimal x, System.Int32 n)`
- `public static System.Double PowDouble(System.Double x, System.Int32 n)`
- `public static T PowDynamic<T, TResult>(T x, TResult y)`
- `public static T PowGeneric<T>(T one, Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<T, T>> mul, T value, System.Int32 exponent)`
- `public static System.Int16 PowInt16(System.Int16 x, System.Int32 n)`
- `public static System.Int32 PowInt32(System.Int32 x, System.Int32 n)`
- `public static System.Int64 PowInt64(System.Int64 x, System.Int32 n)`
- `public static System.IntPtr PowIntPtr(System.IntPtr x, System.Int32 n)`
- `public static System.SByte PowSByte(System.SByte x, System.Int32 n)`
- `public static System.Single PowSingle(System.Single x, System.Int32 n)`
- `public static System.UInt16 PowUInt16(System.UInt16 x, System.Int32 n)`
- `public static System.UInt32 PowUInt32(System.UInt32 x, System.Int32 n)`
- `public static System.UInt64 PowUInt64(System.UInt64 x, System.Int32 n)`
- `public static System.UIntPtr PowUIntPtr(System.UIntPtr x, System.Int32 n)`
- `public static System.Collections.Generic.IEnumerable<System.Byte> RangeByte(System.Byte start, System.Byte step, System.Byte stop)`
- `public static System.Collections.Generic.IEnumerable<System.Char> RangeChar(System.Char start, System.Char stop)`
- `public static System.Collections.Generic.IEnumerable<System.Double> RangeDouble(System.Double start, System.Double step, System.Double stop)`
- `public static System.Collections.Generic.IEnumerable<T> RangeGeneric<T>(T one, Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<T, T>> add, T start, T stop)`
- `public static System.Collections.Generic.IEnumerable<System.Int16> RangeInt16(System.Int16 start, System.Int16 step, System.Int16 stop)`
- `public static System.Collections.Generic.IEnumerable<System.Int32> RangeInt32(System.Int32 start, System.Int32 step, System.Int32 stop)`
- `public static System.Collections.Generic.IEnumerable<System.Int64> RangeInt64(System.Int64 start, System.Int64 step, System.Int64 stop)`
- `public static System.Collections.Generic.IEnumerable<System.IntPtr> RangeIntPtr(System.IntPtr start, System.IntPtr step, System.IntPtr stop)`
- `public static System.Collections.Generic.IEnumerable<System.SByte> RangeSByte(System.SByte start, System.SByte step, System.SByte stop)`
- `public static System.Collections.Generic.IEnumerable<System.Single> RangeSingle(System.Single start, System.Single step, System.Single stop)`
- `public static System.Collections.Generic.IEnumerable<T> RangeStepGeneric<TStep, T>(TStep zero, Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<TStep, T>> add, T start, TStep step, T stop)`
- `public static System.Collections.Generic.IEnumerable<System.UInt16> RangeUInt16(System.UInt16 start, System.UInt16 step, System.UInt16 stop)`
- `public static System.Collections.Generic.IEnumerable<System.UInt32> RangeUInt32(System.UInt32 start, System.UInt32 step, System.UInt32 stop)`
- `public static System.Collections.Generic.IEnumerable<System.UInt64> RangeUInt64(System.UInt64 start, System.UInt64 step, System.UInt64 stop)`
- `public static System.Collections.Generic.IEnumerable<System.UIntPtr> RangeUIntPtr(System.UIntPtr start, System.UIntPtr step, System.UIntPtr stop)`
- `public static T RoundDynamic<T>(T x)`
- `public static System.Void SetArraySlice<T>(T[] target, Microsoft.FSharp.Core.FSharpOption<System.Int32> start, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish, T[] source)`
- `public static System.Void SetArraySlice2D<T>(T[] target, Microsoft.FSharp.Core.FSharpOption<System.Int32> start1, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish1, Microsoft.FSharp.Core.FSharpOption<System.Int32> start2, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish2, T[] source)`
- `public static System.Void SetArraySlice2DFixed1<T>(T[] target, System.Int32 index1, Microsoft.FSharp.Core.FSharpOption<System.Int32> start2, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish2, T[] source)`
- `public static System.Void SetArraySlice2DFixed2<T>(T[] target, Microsoft.FSharp.Core.FSharpOption<System.Int32> start1, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish1, System.Int32 index2, T[] source)`
- `public static System.Void SetArraySlice3D<T>(T[] target, Microsoft.FSharp.Core.FSharpOption<System.Int32> start1, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish1, Microsoft.FSharp.Core.FSharpOption<System.Int32> start2, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish2, Microsoft.FSharp.Core.FSharpOption<System.Int32> start3, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish3, T[] source)`
- `public static System.Void SetArraySlice3DFixedDouble1<T>(T[] target, System.Int32 index1, System.Int32 index2, Microsoft.FSharp.Core.FSharpOption<System.Int32> start3, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish3, T[] source)`
- `public static System.Void SetArraySlice3DFixedDouble2<T>(T[] target, System.Int32 index1, Microsoft.FSharp.Core.FSharpOption<System.Int32> start2, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish2, System.Int32 index3, T[] source)`
- `public static System.Void SetArraySlice3DFixedDouble3<T>(T[] target, Microsoft.FSharp.Core.FSharpOption<System.Int32> start1, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish1, System.Int32 index2, System.Int32 index3, T[] source)`
- `public static System.Void SetArraySlice3DFixedSingle1<T>(T[] target, System.Int32 index, Microsoft.FSharp.Core.FSharpOption<System.Int32> start2, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish2, Microsoft.FSharp.Core.FSharpOption<System.Int32> start3, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish3, T[] source)`
- `public static System.Void SetArraySlice3DFixedSingle2<T>(T[] target, Microsoft.FSharp.Core.FSharpOption<System.Int32> start1, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish1, System.Int32 index, Microsoft.FSharp.Core.FSharpOption<System.Int32> start3, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish3, T[] source)`
- `public static System.Void SetArraySlice3DFixedSingle3<T>(T[] target, Microsoft.FSharp.Core.FSharpOption<System.Int32> start1, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish1, Microsoft.FSharp.Core.FSharpOption<System.Int32> start2, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish2, System.Int32 index, T[] source)`
- `public static System.Void SetArraySlice4D<T>(T[] target, Microsoft.FSharp.Core.FSharpOption<System.Int32> start1, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish1, Microsoft.FSharp.Core.FSharpOption<System.Int32> start2, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish2, Microsoft.FSharp.Core.FSharpOption<System.Int32> start3, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish3, Microsoft.FSharp.Core.FSharpOption<System.Int32> start4, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish4, T[] source)`
- `public static System.Void SetArraySlice4DFixedDouble1<T>(T[] target, System.Int32 index1, System.Int32 index2, Microsoft.FSharp.Core.FSharpOption<System.Int32> start3, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish3, Microsoft.FSharp.Core.FSharpOption<System.Int32> start4, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish4, T[] source)`
- `public static System.Void SetArraySlice4DFixedDouble2<T>(T[] target, System.Int32 index1, Microsoft.FSharp.Core.FSharpOption<System.Int32> start2, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish2, System.Int32 index3, Microsoft.FSharp.Core.FSharpOption<System.Int32> start4, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish4, T[] source)`
- `public static System.Void SetArraySlice4DFixedDouble3<T>(T[] target, System.Int32 index1, Microsoft.FSharp.Core.FSharpOption<System.Int32> start2, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish2, Microsoft.FSharp.Core.FSharpOption<System.Int32> start3, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish3, System.Int32 index4, T[] source)`
- `public static System.Void SetArraySlice4DFixedDouble4<T>(T[] target, Microsoft.FSharp.Core.FSharpOption<System.Int32> start1, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish1, System.Int32 index2, System.Int32 index3, Microsoft.FSharp.Core.FSharpOption<System.Int32> start4, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish4, T[] source)`
- `public static System.Void SetArraySlice4DFixedDouble5<T>(T[] target, Microsoft.FSharp.Core.FSharpOption<System.Int32> start1, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish1, System.Int32 index2, Microsoft.FSharp.Core.FSharpOption<System.Int32> start3, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish3, System.Int32 index4, T[] source)`
- `public static System.Void SetArraySlice4DFixedDouble6<T>(T[] target, Microsoft.FSharp.Core.FSharpOption<System.Int32> start1, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish1, Microsoft.FSharp.Core.FSharpOption<System.Int32> start2, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish2, System.Int32 index3, System.Int32 index4, T[] source)`
- `public static System.Void SetArraySlice4DFixedSingle1<T>(T[] target, System.Int32 index1, Microsoft.FSharp.Core.FSharpOption<System.Int32> start2, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish2, Microsoft.FSharp.Core.FSharpOption<System.Int32> start3, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish3, Microsoft.FSharp.Core.FSharpOption<System.Int32> start4, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish4, T[] source)`
- `public static System.Void SetArraySlice4DFixedSingle2<T>(T[] target, Microsoft.FSharp.Core.FSharpOption<System.Int32> start1, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish1, System.Int32 index2, Microsoft.FSharp.Core.FSharpOption<System.Int32> start3, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish3, Microsoft.FSharp.Core.FSharpOption<System.Int32> start4, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish4, T[] source)`
- `public static System.Void SetArraySlice4DFixedSingle3<T>(T[] target, Microsoft.FSharp.Core.FSharpOption<System.Int32> start1, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish1, Microsoft.FSharp.Core.FSharpOption<System.Int32> start2, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish2, System.Int32 index3, Microsoft.FSharp.Core.FSharpOption<System.Int32> start4, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish4, T[] source)`
- `public static System.Void SetArraySlice4DFixedSingle4<T>(T[] target, Microsoft.FSharp.Core.FSharpOption<System.Int32> start1, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish1, Microsoft.FSharp.Core.FSharpOption<System.Int32> start2, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish2, Microsoft.FSharp.Core.FSharpOption<System.Int32> start3, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish3, System.Int32 index4, T[] source)`
- `public static System.Void SetArraySlice4DFixedTriple1<T>(T[] target, Microsoft.FSharp.Core.FSharpOption<System.Int32> start1, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish1, System.Int32 index2, System.Int32 index3, System.Int32 index4, T[] source)`
- `public static System.Void SetArraySlice4DFixedTriple2<T>(T[] target, System.Int32 index1, Microsoft.FSharp.Core.FSharpOption<System.Int32> start2, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish2, System.Int32 index3, System.Int32 index4, T[] source)`
- `public static System.Void SetArraySlice4DFixedTriple3<T>(T[] target, System.Int32 index1, System.Int32 index2, Microsoft.FSharp.Core.FSharpOption<System.Int32> start3, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish3, System.Int32 index4, T[] source)`
- `public static System.Void SetArraySlice4DFixedTriple4<T>(T[] target, System.Int32 index1, System.Int32 index2, System.Int32 index3, Microsoft.FSharp.Core.FSharpOption<System.Int32> start4, Microsoft.FSharp.Core.FSharpOption<System.Int32> finish4, T[] source)`
- `public static System.Int32 SignDynamic<T>(T x)`
- `public static T SinDynamic<T>(T x)`
- `public static T SinhDynamic<T>(T x)`
- `public static T2 SqrtDynamic<T1, T2>(T1 x)`
- `public static T TanDynamic<T>(T x)`
- `public static T TanhDynamic<T>(T x)`
- `public static T TruncateDynamic<T>(T x)`

### Operators (class [static])

- `public static System.Double Infinity { get; }`
- `public static System.Single InfinitySingle { get; }`
- `public static System.Double NaN { get; }`
- `public static System.Single NaNSingle { get; }`
- `public static T Abs<T>(T value)`
- `public static T Abs$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, T> abs, T value)`
- `public static T Acos<T>(T value)`
- `public static T Acos$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, T> acos, T value)`
- `public static T Asin<T>(T value)`
- `public static T Asin$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, T> asin, T value)`
- `public static T Atan<T>(T value)`
- `public static T Atan$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, T> atan, T value)`
- `public static T2 Atan2<T1, T2>(T1 y, T1 x)`
- `public static T2 Atan2$W<T1, T2>(Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T1, T2>> atan2, T1 y, T1 x)`
- `public static System.Object Box<T>(T value)`
- `public static T Ceiling<T>(T value)`
- `public static T Ceiling$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, T> ceiling, T value)`
- `public static System.Int32 Compare<T>(T e1, T e2)`
- `public static System.IO.TextWriter ConsoleError<T>()`
- `public static System.IO.TextReader ConsoleIn<T>()`
- `public static System.IO.TextWriter ConsoleOut<T>()`
- `public static T Cos<T>(T value)`
- `public static T Cos$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, T> cos, T value)`
- `public static T Cosh<T>(T value)`
- `public static T Cosh$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, T> cosh, T value)`
- `public static System.Collections.Generic.IEnumerable<T> CreateSequence<T>(System.Collections.Generic.IEnumerable<T> sequence)`
- `public static System.Void Decrement(Microsoft.FSharp.Core.FSharpRef<System.Int32> cell)`
- `public static T DefaultArg<T>(Microsoft.FSharp.Core.FSharpOption<T> arg, T defaultValue)`
- `public static T DefaultIfNull<T>(T defaultValue, T arg)`
- `where T : class`
- `public static T DefaultIfNullV<T>(T defaultValue, T?? arg)`
- `where T : struct`
- `public static T DefaultValueArg<T>(Microsoft.FSharp.Core.FSharpValueOption<T> arg, T defaultValue)`
- `public static T Exit<T>(System.Int32 exitcode)`
- `public static T Exp<T>(T value)`
- `public static T Exp$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, T> exp, T value)`
- `public static T FailWith<T>(System.String message)`
- `public static System.Exception Failure(System.String message)`
- `public static Microsoft.FSharp.Core.FSharpOption<System.String> FailurePattern(System.Exception error)`
- `public static T Floor<T>(T value)`
- `public static T Floor$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, T> floor, T value)`
- `public static T1 Fst<T1, T2>(System.Tuple<T1, T2> a)`
- `public static System.Int32 Hash<T>(T obj)`
- `public static T Identity<T>(T x)`
- `public static System.Void Ignore<T>(T value)`
- `public static System.Void Increment(Microsoft.FSharp.Core.FSharpRef<System.Int32> cell)`
- `public static T InvalidArg<T>(System.String argumentName, System.String message)`
- `public static T InvalidOp<T>(System.String message)`
- `public static System.Boolean IsNull<T>(T value)`
- `where T : class`
- `public static System.Boolean IsNullV<T>(T?? value)`
- `where T : struct`
- `public static System.Tuple<TKey, TValue> KeyValuePattern<TKey, TValue>(System.Collections.Generic.KeyValuePair<TKey, TValue> keyValuePair)`
- `public static T Lock<TLock, T>(TLock lockObject, Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, T> action)`
- `where TLock : class`
- `public static T Log<T>(T value)`
- `public static T Log$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, T> log, T value)`
- `public static T Log10<T>(T value)`
- `public static T Log10$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, T> log10, T value)`
- `public static T Max<T>(T e1, T e2)`
- `public static T Min<T>(T e1, T e2)`
- `public static System.String NameOf<T>(T _arg1)`
- `public static T NonNull<T>(T value)`
- `where T : class`
- `public static T NonNullQuickPattern<T>(T value)`
- `where T : class`
- `public static T NonNullQuickValuePattern<T>(T?? value)`
- `where T : struct`
- `public static T NonNullV<T>(T?? value)`
- `where T : struct`
- `public static System.Boolean Not(System.Boolean value)`
- `public static T NullArg<T>(System.String argumentName)`
- `public static T NullArgCheck<T>(System.String argumentName, T value)`
- `where T : class`
- `public static Microsoft.FSharp.Core.FSharpChoice<Microsoft.FSharp.Core.Unit, T> NullMatchPattern<T>(T value)`
- `where T : class`
- `public static T? NullV<T>()`
- `where T : struct`
- `public static Microsoft.FSharp.Core.FSharpChoice<Microsoft.FSharp.Core.Unit, T> NullValueMatchPattern<T>(T?? value)`
- `where T : struct`
- `public static T PowInteger<T>(T x, System.Int32 n)`
- `public static T PowInteger$W<T>(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, T> get_One, Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<T, T>> op_Division, Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<T, T>> op_Multiply, T x, System.Int32 n)`
- `public static T Raise<T>(System.Exception exn)`
- `public static Microsoft.FSharp.Core.FSharpRef<T> Ref<T>(T value)`
- `public static T Reraise<T>()`
- `public static T Rethrow<T>()`
- `public static T Round<T>(T value)`
- `public static T Round$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, T> round, T value)`
- `public static System.Int32 Sign<T>(T value)`
- `public static System.Int32 Sign$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Int32> get_Sign, T value)`
- `public static T Sin<T>(T value)`
- `public static T Sin$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, T> sin, T value)`
- `public static T Sinh<T>(T value)`
- `public static T Sinh$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, T> sinh, T value)`
- `public static System.Int32 SizeOf<T>()`
- `public static T2 Snd<T1, T2>(System.Tuple<T1, T2> tuple)`
- `public static TResult Sqrt<T, TResult>(T value)`
- `public static TResult Sqrt$W<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, TResult> sqrt, T value)`
- `public static T Tan<T>(T value)`
- `public static T Tan$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, T> tan, T value)`
- `public static T Tanh<T>(T value)`
- `public static T Tanh$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, T> tanh, T value)`
- `public static System.Byte ToByte<T>(T value)`
- `public static System.Byte ToByte$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Byte> op_Explicit, T value)`
- `public static System.Char ToChar<T>(T value)`
- `public static System.Char ToChar$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Char> op_Explicit, T value)`
- `public static System.Decimal ToDecimal<T>(T value)`
- `public static System.Decimal ToDecimal$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Decimal> op_Explicit, T value)`
- `public static System.Double ToDouble<T>(T value)`
- `public static System.Double ToDouble$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Double> op_Explicit, T value)`
- `public static TResult ToEnum<TResult>(System.Int32 value)`
- `public static System.Int32 ToInt<T>(T value)`
- `public static System.Int32 ToInt$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Int32> op_Explicit, T value)`
- `public static System.Int16 ToInt16<T>(T value)`
- `public static System.Int16 ToInt16$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Int16> op_Explicit, T value)`
- `public static System.Int32 ToInt32<T>(T value)`
- `public static System.Int32 ToInt32$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Int32> op_Explicit, T value)`
- `public static System.Int64 ToInt64<T>(T value)`
- `public static System.Int64 ToInt64$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Int64> op_Explicit, T value)`
- `public static System.IntPtr ToIntPtr<T>(T value)`
- `public static System.IntPtr ToIntPtr$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.IntPtr> op_Explicit, T value)`
- `public static System.SByte ToSByte<T>(T value)`
- `public static System.SByte ToSByte$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.SByte> op_Explicit, T value)`
- `public static System.Single ToSingle<T>(T value)`
- `public static System.Single ToSingle$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Single> op_Explicit, T value)`
- `public static System.String ToString<T>(T value)`
- `public static System.UInt32 ToUInt<T>(T value)`
- `public static System.UInt32 ToUInt$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.UInt32> op_Explicit, T value)`
- `public static System.UInt16 ToUInt16<T>(T value)`
- `public static System.UInt16 ToUInt16$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.UInt16> op_Explicit, T value)`
- `public static System.UInt32 ToUInt32<T>(T value)`
- `public static System.UInt32 ToUInt32$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.UInt32> op_Explicit, T value)`
- `public static System.UInt64 ToUInt64<T>(T value)`
- `public static System.UInt64 ToUInt64$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.UInt64> op_Explicit, T value)`
- `public static System.UIntPtr ToUIntPtr<T>(T value)`
- `public static System.UIntPtr ToUIntPtr$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.UIntPtr> op_Explicit, T value)`
- `public static T Truncate<T>(T value)`
- `public static T Truncate$W<T>(Microsoft.FSharp.Core.FSharpFunc<T, T> truncate, T value)`
- `public static Microsoft.FSharp.Core.FSharpOption<T> TryUnbox<T>(System.Object value)`
- `public static System.Type TypeDefOf<T>()`
- `public static System.Type TypeOf<T>()`
- `public static T Unbox<T>(System.Object value)`
- `public static TResult Using<T, TResult>(T resource, Microsoft.FSharp.Core.FSharpFunc<T, TResult> action)`
- `where T : System.IDisposable`
- `public static T WithNull<T>(T value)`
- `where T : class`
- `public static T? WithNullV<T>(T value)`
- `where T : struct`
- `public static System.Int32 limitedHash<T>(System.Int32 limit, T obj)`
- `public static T3 op_Addition(T1 x, T2 y)`
- `public static T3 op_Addition$W(Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, T3>> op_Addition, T1 x, T2 y)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> op_Append(Microsoft.FSharp.Collections.FSharpList<T> list1, Microsoft.FSharp.Collections.FSharpList<T> list2)`
- `public static T op_BitwiseAnd(T x, T y)`
- `public static T op_BitwiseAnd$W(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<T, T>> op_BitwiseAnd, T x, T y)`
- `public static T op_BitwiseOr(T x, T y)`
- `public static T op_BitwiseOr$W(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<T, T>> op_BitwiseOr, T x, T y)`
- `public static System.Void op_ColonEquals(Microsoft.FSharp.Core.FSharpRef<T> cell, T value)`
- `public static Microsoft.FSharp.Core.FSharpFunc<T1, T3> op_ComposeLeft(Microsoft.FSharp.Core.FSharpFunc<T2, T3> func2, Microsoft.FSharp.Core.FSharpFunc<T1, T2> func1)`
- `public static Microsoft.FSharp.Core.FSharpFunc<T1, T3> op_ComposeRight(Microsoft.FSharp.Core.FSharpFunc<T1, T2> func1, Microsoft.FSharp.Core.FSharpFunc<T2, T3> func2)`
- `public static System.String op_Concatenate(System.String s1, System.String s2)`
- `public static T op_Dereference(Microsoft.FSharp.Core.FSharpRef<T> cell)`
- `public static T3 op_Division(T1 x, T2 y)`
- `public static T3 op_Division$W(Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, T3>> op_Division, T1 x, T2 y)`
- `public static System.Boolean op_Equality(T x, T y)`
- `public static T op_ExclusiveOr(T x, T y)`
- `public static T op_ExclusiveOr$W(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<T, T>> op_ExclusiveOr, T x, T y)`
- `public static T op_Exponentiation(T x, TResult y)`
- `public static T op_Exponentiation$W(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<TResult, T>> pow, T x, TResult y)`
- `public static System.Boolean op_GreaterThan(T x, T y)`
- `public static System.Boolean op_GreaterThanOrEqual(T x, T y)`
- `public static System.Boolean op_Inequality(T x, T y)`
- `public static T op_LeftShift(T value, System.Int32 shift)`
- `public static T op_LeftShift$W(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<System.Int32, T>> op_LeftShift, T value, System.Int32 shift)`
- `public static System.Boolean op_LessThan(T x, T y)`
- `public static System.Boolean op_LessThanOrEqual(T x, T y)`
- `public static T op_LogicalNot(T value)`
- `public static T op_LogicalNot$W(Microsoft.FSharp.Core.FSharpFunc<T, T> op_LogicalNot, T value)`
- `public static T3 op_Modulus(T1 x, T2 y)`
- `public static T3 op_Modulus$W(Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, T3>> op_Modulus, T1 x, T2 y)`
- `public static T3 op_Multiply(T1 x, T2 y)`
- `public static T3 op_Multiply$W(Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, T3>> op_Multiply, T1 x, T2 y)`
- `public static TResult op_PipeLeft(Microsoft.FSharp.Core.FSharpFunc<T, TResult> func, T arg1)`
- `public static TResult op_PipeLeft2(Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, TResult>> func, T1 arg1, T2 arg2)`
- `public static TResult op_PipeLeft3(Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, Microsoft.FSharp.Core.FSharpFunc<T3, TResult>>> func, T1 arg1, T2 arg2, T3 arg3)`
- `public static TResult op_PipeRight(T1 arg, Microsoft.FSharp.Core.FSharpFunc<T1, TResult> func)`
- `public static TResult op_PipeRight2(T1 arg1, T2 arg2, Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, TResult>> func)`
- `public static TResult op_PipeRight3(T1 arg1, T2 arg2, T3 arg3, Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, Microsoft.FSharp.Core.FSharpFunc<T3, TResult>>> func)`
- `public static System.Collections.Generic.IEnumerable<T> op_Range(T start, T finish)`
- `public static System.Collections.Generic.IEnumerable<T> op_Range$W(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, T> get_One, Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<T, T>> op_Addition, T start, T finish)`
- `public static System.Collections.Generic.IEnumerable<T> op_RangeStep(T start, TStep step, T finish)`
- `public static System.Collections.Generic.IEnumerable<T> op_RangeStep$W(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, TStep> get_Zero, Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<TStep, T>> op_Addition, T start, TStep step, T finish)`
- `public static T op_RightShift(T value, System.Int32 shift)`
- `public static T op_RightShift$W(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<System.Int32, T>> op_RightShift, T value, System.Int32 shift)`
- `public static T3 op_Subtraction(T1 x, T2 y)`
- `public static T3 op_Subtraction$W(Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, T3>> op_Subtraction, T1 x, T2 y)`
- `public static T op_UnaryNegation(T n)`
- `public static T op_UnaryNegation$W(Microsoft.FSharp.Core.FSharpFunc<T, T> op_UnaryNegation, T n)`
- `public static T op_UnaryPlus(T value)`
- `public static T op_UnaryPlus$W(Microsoft.FSharp.Core.FSharpFunc<T, T> op_UnaryPlus, T value)`

### OptimizedClosures (class [static])


### OptionModule (class [static])

- `public static Microsoft.FSharp.Core.FSharpOption<TResult> Bind<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpOption<TResult>> binder, Microsoft.FSharp.Core.FSharpOption<T> option)`
- `public static System.Boolean Contains<T>(T value, Microsoft.FSharp.Core.FSharpOption<T> option)`
- `public static System.Int32 Count<T>(Microsoft.FSharp.Core.FSharpOption<T> option)`
- `public static T DefaultValue<T>(T value, Microsoft.FSharp.Core.FSharpOption<T> option)`
- `public static T DefaultWith<T>(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, T> defThunk, Microsoft.FSharp.Core.FSharpOption<T> option)`
- `public static System.Boolean Exists<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, Microsoft.FSharp.Core.FSharpOption<T> option)`
- `public static Microsoft.FSharp.Core.FSharpOption<T> Filter<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, Microsoft.FSharp.Core.FSharpOption<T> option)`
- `public static Microsoft.FSharp.Core.FSharpOption<T> Flatten<T>(Microsoft.FSharp.Core.FSharpOption<Microsoft.FSharp.Core.FSharpOption<T>> option)`
- `public static TState Fold<T, TState>(Microsoft.FSharp.Core.FSharpFunc<TState, Microsoft.FSharp.Core.FSharpFunc<T, TState>> folder, TState state, Microsoft.FSharp.Core.FSharpOption<T> option)`
- `public static TState FoldBack<T, TState>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<TState, TState>> folder, Microsoft.FSharp.Core.FSharpOption<T> option, TState state)`
- `public static System.Boolean ForAll<T>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, Microsoft.FSharp.Core.FSharpOption<T> option)`
- `public static T GetValue<T>(Microsoft.FSharp.Core.FSharpOption<T> option)`
- `public static System.Boolean IsNone<T>(Microsoft.FSharp.Core.FSharpOption<T> option)`
- `public static System.Boolean IsSome<T>(Microsoft.FSharp.Core.FSharpOption<T> option)`
- `public static System.Void Iterate<T>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.Unit> action, Microsoft.FSharp.Core.FSharpOption<T> option)`
- `public static Microsoft.FSharp.Core.FSharpOption<TResult> Map<T, TResult>(Microsoft.FSharp.Core.FSharpFunc<T, TResult> mapping, Microsoft.FSharp.Core.FSharpOption<T> option)`
- `public static Microsoft.FSharp.Core.FSharpOption<TResult> Map2<T1, T2, TResult>(Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, TResult>> mapping, Microsoft.FSharp.Core.FSharpOption<T1> option1, Microsoft.FSharp.Core.FSharpOption<T2> option2)`
- `public static Microsoft.FSharp.Core.FSharpOption<TResult> Map3<T1, T2, T3, TResult>(Microsoft.FSharp.Core.FSharpFunc<T1, Microsoft.FSharp.Core.FSharpFunc<T2, Microsoft.FSharp.Core.FSharpFunc<T3, TResult>>> mapping, Microsoft.FSharp.Core.FSharpOption<T1> option1, Microsoft.FSharp.Core.FSharpOption<T2> option2, Microsoft.FSharp.Core.FSharpOption<T3> option3)`
- `public static Microsoft.FSharp.Core.FSharpOption<T> OfNullable<T>(T?? value)`
- `where T : struct`
- `public static Microsoft.FSharp.Core.FSharpOption<T> OfObj<T>(T value)`
- `where T : class`
- `public static Microsoft.FSharp.Core.FSharpOption<T> OfValueOption<T>(Microsoft.FSharp.Core.FSharpValueOption<T> voption)`
- `public static Microsoft.FSharp.Core.FSharpOption<T> OrElse<T>(Microsoft.FSharp.Core.FSharpOption<T> ifNone, Microsoft.FSharp.Core.FSharpOption<T> option)`
- `public static Microsoft.FSharp.Core.FSharpOption<T> OrElseWith<T>(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, Microsoft.FSharp.Core.FSharpOption<T>> ifNoneThunk, Microsoft.FSharp.Core.FSharpOption<T> option)`
- `public static T[] ToArray<T>(Microsoft.FSharp.Core.FSharpOption<T> option)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> ToList<T>(Microsoft.FSharp.Core.FSharpOption<T> option)`
- `public static T? ToNullable<T>(Microsoft.FSharp.Core.FSharpOption<T> option)`
- `where T : struct`
- `public static T ToObj<T>(Microsoft.FSharp.Core.FSharpOption<T> value)`
- `where T : class`
- `public static Microsoft.FSharp.Core.FSharpValueOption<T> ToValueOption<T>(Microsoft.FSharp.Core.FSharpOption<T> option)`

### PrintfFormat`4<TPrinter, TState, TResidue, TResult> (class)

- `public PrintfFormat`4(System.String value)`
- `public PrintfFormat`4(System.String value, System.Object[] captures, System.Type[] captureTys)`
- `public System.Type[] CaptureTypes { get; } (nullability: Unknown)`
- `public System.Object[] Captures { get; } (nullability: Unknown)`
- `public System.String Value { get; } (nullability: Unknown)`
- `public System.String ToString()`

### PrintfFormat`5<TPrinter, TState, TResidue, TResult, TTuple> (class) : Microsoft.FSharp.Core.PrintfFormat<TPrinter, TState, TResidue, TResult>

- `public PrintfFormat`5(System.String value)`
- `public PrintfFormat`5(System.String value, System.Object[] captures, System.Type[] captureTys)`

### PrintfModule (class [static])

- `public static T PrintFormat<T>(Microsoft.FSharp.Core.PrintfFormat<T, System.IO.TextWriter, Microsoft.FSharp.Core.Unit, Microsoft.FSharp.Core.Unit> format)`
- `public static T PrintFormatLine<T>(Microsoft.FSharp.Core.PrintfFormat<T, System.IO.TextWriter, Microsoft.FSharp.Core.Unit, Microsoft.FSharp.Core.Unit> format)`
- `public static T PrintFormatLineToError<T>(Microsoft.FSharp.Core.PrintfFormat<T, System.IO.TextWriter, Microsoft.FSharp.Core.Unit, Microsoft.FSharp.Core.Unit> format)`
- `public static T PrintFormatLineToTextWriter<T>(System.IO.TextWriter textWriter, Microsoft.FSharp.Core.PrintfFormat<T, System.IO.TextWriter, Microsoft.FSharp.Core.Unit, Microsoft.FSharp.Core.Unit> format)`
- `public static T PrintFormatThen<TResult, T>(Microsoft.FSharp.Core.FSharpFunc<System.String, TResult> continuation, Microsoft.FSharp.Core.PrintfFormat<T, Microsoft.FSharp.Core.Unit, System.String, TResult> format)`
- `public static T PrintFormatToError<T>(Microsoft.FSharp.Core.PrintfFormat<T, System.IO.TextWriter, Microsoft.FSharp.Core.Unit, Microsoft.FSharp.Core.Unit> format)`
- `public static T PrintFormatToStringBuilder<T>(System.Text.StringBuilder builder, Microsoft.FSharp.Core.PrintfFormat<T, System.Text.StringBuilder, Microsoft.FSharp.Core.Unit, Microsoft.FSharp.Core.Unit> format)`
- `public static T PrintFormatToStringBuilderThen<TResult, T>(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, TResult> continuation, System.Text.StringBuilder builder, Microsoft.FSharp.Core.PrintfFormat<T, System.Text.StringBuilder, Microsoft.FSharp.Core.Unit, TResult> format)`
- `public static T PrintFormatToStringThen<T>(Microsoft.FSharp.Core.PrintfFormat<T, Microsoft.FSharp.Core.Unit, System.String, System.String> format)`
- `public static T PrintFormatToStringThen<TResult, T>(Microsoft.FSharp.Core.FSharpFunc<System.String, TResult> continuation, Microsoft.FSharp.Core.PrintfFormat<T, Microsoft.FSharp.Core.Unit, System.String, TResult> format)`
- `public static T PrintFormatToStringThenFail<T, TResult>(Microsoft.FSharp.Core.PrintfFormat<T, Microsoft.FSharp.Core.Unit, System.String, TResult> format)`
- `public static T PrintFormatToTextWriter<T>(System.IO.TextWriter textWriter, Microsoft.FSharp.Core.PrintfFormat<T, System.IO.TextWriter, Microsoft.FSharp.Core.Unit, Microsoft.FSharp.Core.Unit> format)`
- `public static T PrintFormatToTextWriterThen<TResult, T>(Microsoft.FSharp.Core.FSharpFunc<Microsoft.FSharp.Core.Unit, TResult> continuation, System.IO.TextWriter textWriter, Microsoft.FSharp.Core.PrintfFormat<T, System.IO.TextWriter, Microsoft.FSharp.Core.Unit, TResult> format)`

### ResultModule (class [static])

- `public static Microsoft.FSharp.Core.FSharpResult<TResult, TError> Bind<T, TResult, TError>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpResult<TResult, TError>> binder, Microsoft.FSharp.Core.FSharpResult<T, TError> result)`
- `public static System.Boolean Contains<T, TError>(T value, Microsoft.FSharp.Core.FSharpResult<T, TError> result)`
- `public static System.Int32 Count<T, TError>(Microsoft.FSharp.Core.FSharpResult<T, TError> result)`
- `public static T DefaultValue<T, TError>(T value, Microsoft.FSharp.Core.FSharpResult<T, TError> result)`
- `public static T DefaultWith<TError, T>(Microsoft.FSharp.Core.FSharpFunc<TError, T> defThunk, Microsoft.FSharp.Core.FSharpResult<T, TError> result)`
- `public static System.Boolean Exists<T, TError>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, Microsoft.FSharp.Core.FSharpResult<T, TError> result)`
- `public static TState Fold<T, TError, TState>(Microsoft.FSharp.Core.FSharpFunc<TState, Microsoft.FSharp.Core.FSharpFunc<T, TState>> folder, TState state, Microsoft.FSharp.Core.FSharpResult<T, TError> result)`
- `public static TState FoldBack<T, TError, TState>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.FSharpFunc<TState, TState>> folder, Microsoft.FSharp.Core.FSharpResult<T, TError> result, TState state)`
- `public static System.Boolean ForAll<T, TError>(Microsoft.FSharp.Core.FSharpFunc<T, System.Boolean> predicate, Microsoft.FSharp.Core.FSharpResult<T, TError> result)`
- `public static System.Boolean IsError<T, TError>(Microsoft.FSharp.Core.FSharpResult<T, TError> result)`
- `public static System.Boolean IsOk<T, TError>(Microsoft.FSharp.Core.FSharpResult<T, TError> result)`
- `public static System.Void Iterate<T, TError>(Microsoft.FSharp.Core.FSharpFunc<T, Microsoft.FSharp.Core.Unit> action, Microsoft.FSharp.Core.FSharpResult<T, TError> result)`
- `public static Microsoft.FSharp.Core.FSharpResult<TResult, TError> Map<T, TResult, TError>(Microsoft.FSharp.Core.FSharpFunc<T, TResult> mapping, Microsoft.FSharp.Core.FSharpResult<T, TError> result)`
- `public static Microsoft.FSharp.Core.FSharpResult<T, TResult> MapError<TError, TResult, T>(Microsoft.FSharp.Core.FSharpFunc<TError, TResult> mapping, Microsoft.FSharp.Core.FSharpResult<T, TError> result)`
- `public static T[] ToArray<T, TError>(Microsoft.FSharp.Core.FSharpResult<T, TError> result)`
- `public static Microsoft.FSharp.Collections.FSharpList<T> ToList<T, TError>(Microsoft.FSharp.Core.FSharpResult<T, TError> result)`
- `public static Microsoft.FSharp.Core.FSharpOption<T> ToOption<T, TError>(Microsoft.FSharp.Core.FSharpResult<T, TError> result)`
- `public static Microsoft.FSharp.Core.FSharpValueOption<T> ToValueOption<T, TError>(Microsoft.FSharp.Core.FSharpResult<T, TError> result)`

### Tags<T1, T2> (class [static])

- `public const System.Int32 Choice1Of2`
- `public const System.Int32 Choice2Of2`

### Tags<T1, T2, T3> (class [static])

- `public const System.Int32 Choice1Of3`
- `public const System.Int32 Choice2Of3`
- `public const System.Int32 Choice3Of3`

### Tags<T1, T2, T3, T4> (class [static])

- `public const System.Int32 Choice1Of4`
- `public const System.Int32 Choice2Of4`
- `public const System.Int32 Choice3Of4`
- `public const System.Int32 Choice4Of4`

### Tags<T1, T2, T3, T4, T5> (class [static])

- `public const System.Int32 Choice1Of5`
- `public const System.Int32 Choice2Of5`
- `public const System.Int32 Choice3Of5`
- `public const System.Int32 Choice4Of5`
- `public const System.Int32 Choice5Of5`

### Tags<T1, T2, T3, T4, T5, T6> (class [static])

- `public const System.Int32 Choice1Of6`
- `public const System.Int32 Choice2Of6`
- `public const System.Int32 Choice3Of6`
- `public const System.Int32 Choice4Of6`
- `public const System.Int32 Choice5Of6`
- `public const System.Int32 Choice6Of6`

### Tags<T1, T2, T3, T4, T5, T6, T7> (class [static])

- `public const System.Int32 Choice1Of7`
- `public const System.Int32 Choice2Of7`
- `public const System.Int32 Choice3Of7`
- `public const System.Int32 Choice4Of7`
- `public const System.Int32 Choice5Of7`
- `public const System.Int32 Choice6Of7`
- `public const System.Int32 Choice7Of7`

### Tags<T> (class [static])

- `public const System.Int32 None`
- `public const System.Int32 Some`

### Tags<T, TError> (class [static])

- `public const System.Int32 Error`
- `public const System.Int32 Ok`

### Tags<T> (class [static])

- `public const System.Int32 ValueNone`
- `public const System.Int32 ValueSome`

### Unchecked (class [static])

- `public static System.Int32 Compare<T>(T x, T y)`
- `public static T DefaultOf<T>()`
- `public static System.Boolean Equals<T>(T x, T y)`
- `public static System.Int32 Hash<T>(T x)`
- `public static T NonNull<T>(T x)`
- `where T : class`
- `public static T NonNullQuickPattern<T>(T value)`
- `where T : class`
- `public static T Unbox<T>(System.Object v)`

### Unit (class [sealed]) : System.IComparable

- `public System.Boolean Equals(System.Object obj)`
- `public System.Int32 GetHashCode()`

