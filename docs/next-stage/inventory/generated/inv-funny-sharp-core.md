# FunnySharp core public API (commit 4dbebd9, 0.1.0)

Assemblies: FunnySharp 0.1.0.0

Type count: 33

## FunnySharp

### AsyncEnumerablePipelineExtensions (class [static])

- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> Choose<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, FunnySharp.Option<TResult>> chooser)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> ChooseValueAsync<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.Tasks.ValueTask<FunnySharp.Option<TResult>>> chooser)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> ChooseValueAsync<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<FunnySharp.Option<TResult>>> chooser)`

### AsyncSequenceExtensions (class [static])

- `[ext] public static System.Threading.Tasks.ValueTask<FunnySharp.Option<System.Collections.Generic.IReadOnlyList<TValue>>> SequenceAsync<TValue>(this System.Collections.Generic.IAsyncEnumerable<FunnySharp.Option<TValue>> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<FunnySharp.Result<System.Collections.Generic.IReadOnlyList<TValue>, TError>> SequenceAsync<TValue, TError>(this System.Collections.Generic.IAsyncEnumerable<FunnySharp.Result<TValue, TError>> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<FunnySharp.Validation<System.Collections.Generic.IReadOnlyList<TValue>, TError>> SequenceAsync<TValue, TError>(this System.Collections.Generic.IAsyncEnumerable<FunnySharp.Validation<TValue, TError>> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<FunnySharp.Option<System.Collections.Generic.IReadOnlyList<TResult>>> TraverseAsync<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, FunnySharp.Option<TResult>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<FunnySharp.Result<System.Collections.Generic.IReadOnlyList<TResult>, TError>> TraverseAsync<TSource, TResult, TError>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, FunnySharp.Result<TResult, TError>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<FunnySharp.Validation<System.Collections.Generic.IReadOnlyList<TResult>, TError>> TraverseAsync<TSource, TResult, TError>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, FunnySharp.Validation<TResult, TError>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<FunnySharp.Option<System.Collections.Generic.IReadOnlyList<TResult>>> TraverseValueAsync<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.Tasks.ValueTask<FunnySharp.Option<TResult>>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<FunnySharp.Option<System.Collections.Generic.IReadOnlyList<TResult>>> TraverseValueAsync<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<FunnySharp.Option<TResult>>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<FunnySharp.Result<System.Collections.Generic.IReadOnlyList<TResult>, TError>> TraverseValueAsync<TSource, TResult, TError>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.Tasks.ValueTask<FunnySharp.Result<TResult, TError>>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<FunnySharp.Result<System.Collections.Generic.IReadOnlyList<TResult>, TError>> TraverseValueAsync<TSource, TResult, TError>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<FunnySharp.Result<TResult, TError>>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<FunnySharp.Validation<System.Collections.Generic.IReadOnlyList<TResult>, TError>> TraverseValueAsync<TSource, TResult, TError>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.Tasks.ValueTask<FunnySharp.Validation<TResult, TError>>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<FunnySharp.Validation<System.Collections.Generic.IReadOnlyList<TResult>, TError>> TraverseValueAsync<TSource, TResult, TError>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<FunnySharp.Validation<TResult, TError>>> selector, System.Threading.CancellationToken cancellationToken)`

### ConcurrentEffectExtensions (class [static])

- `[ext] public static System.Threading.Tasks.ValueTask<FunnySharp.Validation<TValue, TError>> FirstSuccessAsync<TValue, TError>(this System.Collections.Generic.IEnumerable<FunnySharp.Effect<FunnySharp.Result<TValue, TError>>> effects, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<FunnySharp.Validation<TValue, TError>> FirstSuccessAsync<TValue, TError>(this System.Collections.Generic.IEnumerable<FunnySharp.Effect<FunnySharp.Result<TValue, TError>>> effects, System.TimeSpan timeout, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<FunnySharp.Validation<TValue, TError>> FirstSuccessAsync<TValue, TError>(this System.Collections.Generic.IEnumerable<FunnySharp.Effect<FunnySharp.Result<TValue, TError>>> effects, System.TimeSpan timeout, System.TimeProvider timeProvider, System.Threading.CancellationToken cancellationToken)`

### Effect (class [static])

- `public static FunnySharp.Effect<FunnySharp.Result<TValue, TError>> FromResult<TValue, TError>(FunnySharp.Result<TValue, TError> result)`
- `public static FunnySharp.Effect<T> FromSync<T>(System.Func<T> operation)`
- `public static FunnySharp.Effect<T> FromSync<T>(System.Func<System.Threading.CancellationToken, T> operation)`
- `public static FunnySharp.Effect<TEnvironment, T> FromSync<TEnvironment, T>(System.Func<TEnvironment, T> operation)`
- `public static FunnySharp.Effect<TEnvironment, T> FromSync<TEnvironment, T>(System.Func<TEnvironment, System.Threading.CancellationToken, T> operation)`
- `public static FunnySharp.Effect<T> FromTask<T>(System.Func<System.Threading.Tasks.Task<T>> operation)`
- `public static FunnySharp.Effect<T> FromTask<T>(System.Func<System.Threading.CancellationToken, System.Threading.Tasks.Task<T>> operation)`
- `public static FunnySharp.Effect<TEnvironment, T> FromTask<TEnvironment, T>(System.Func<TEnvironment, System.Threading.Tasks.Task<T>> operation)`
- `public static FunnySharp.Effect<TEnvironment, T> FromTask<TEnvironment, T>(System.Func<TEnvironment, System.Threading.CancellationToken, System.Threading.Tasks.Task<T>> operation)`
- `public static FunnySharp.Effect<T> FromValue<T>(T? value)`
- `public static FunnySharp.Effect<T> FromValueTask<T>(System.Func<System.Threading.Tasks.ValueTask<T>> operation)`
- `public static FunnySharp.Effect<T> FromValueTask<T>(System.Func<System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<T>> operation)`
- `public static FunnySharp.Effect<TEnvironment, T> FromValueTask<TEnvironment, T>(System.Func<TEnvironment, System.Threading.Tasks.ValueTask<T>> operation)`
- `public static FunnySharp.Effect<TEnvironment, T> FromValueTask<TEnvironment, T>(System.Func<TEnvironment, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<T>> operation)`

### EffectResourceExtensions (class [static])

- `[ext] public static FunnySharp.Effect<TResult> Using<TResource, TResult>(this FunnySharp.Effect<TResource> acquire, System.Func<TResource, FunnySharp.Effect<TResult>> use)`
- `where TResource : System.IDisposable`
- `[ext] public static FunnySharp.Effect<TEnvironment, TResult> Using<TEnvironment, TResource, TResult>(this FunnySharp.Effect<TEnvironment, TResource> acquire, System.Func<TResource, FunnySharp.Effect<TEnvironment, TResult>> use)`
- `where TResource : System.IDisposable`
- `[ext] public static FunnySharp.Effect<TResult> UsingAsync<TResource, TResult>(this FunnySharp.Effect<TResource> acquire, System.Func<TResource, FunnySharp.Effect<TResult>> use)`
- `where TResource : System.IAsyncDisposable`
- `[ext] public static FunnySharp.Effect<TEnvironment, TResult> UsingAsync<TEnvironment, TResource, TResult>(this FunnySharp.Effect<TEnvironment, TResource> acquire, System.Func<TResource, FunnySharp.Effect<TEnvironment, TResult>> use)`
- `where TResource : System.IAsyncDisposable`

### Effect`1<T> (struct [readonly struct])

- `public FunnySharp.Effect<TResult> Bind<TResult>(System.Func<T, FunnySharp.Effect<TResult>> binder)`
- `public FunnySharp.Effect<TResult> Map<TResult>(System.Func<T, TResult> selector)`
- `public System.Threading.Tasks.ValueTask<T> RunAsync(System.Threading.CancellationToken cancellationToken)`
- `public FunnySharp.Effect<TResult> Select<TResult>(System.Func<T, TResult> selector)`
- `public FunnySharp.Effect<TResult> SelectMany<TIntermediate, TResult>(System.Func<T, FunnySharp.Effect<TIntermediate>> binder, System.Func<T, TIntermediate, TResult> projector)`
- `public FunnySharp.Effect<TEnvironment, T> WithEnvironment<TEnvironment>()`

### Effect`2<TEnvironment, T> (struct [readonly struct])

- `public FunnySharp.Effect<TEnvironment, TResult> Bind<TResult>(System.Func<T, FunnySharp.Effect<TEnvironment, TResult>> binder)`
- `public FunnySharp.Effect<TEnvironment, TResult> Map<TResult>(System.Func<T, TResult> selector)`
- `public FunnySharp.Effect<T> Provide(TEnvironment? environment)`
- `public System.Threading.Tasks.ValueTask<T> RunAsync(TEnvironment? environment, System.Threading.CancellationToken cancellationToken)`
- `public FunnySharp.Effect<TEnvironment, TResult> Select<TResult>(System.Func<T, TResult> selector)`
- `public FunnySharp.Effect<TEnvironment, TResult> SelectMany<TIntermediate, TResult>(System.Func<T, FunnySharp.Effect<TEnvironment, TIntermediate>> binder, System.Func<T, TIntermediate, TResult> projector)`

### EnumerablePipelineExtensions (class [static])

- `[ext] public static System.Collections.Generic.IEnumerable<TResult> Choose<TSource, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, FunnySharp.Option<TResult>> chooser)`

### FunctionExtensions (class [static])

- `[ext] public static System.Func<T, TResult> Compose<T, TIntermediate, TResult>(this System.Func<T, TIntermediate> first, System.Func<TIntermediate, TResult> second)`
- `[ext] public static System.Func<T, System.Threading.Tasks.Task<TResult>> ComposeAsync<T, TIntermediate, TResult>(this System.Func<T, System.Threading.Tasks.Task<TIntermediate>> first, System.Func<TIntermediate, System.Threading.Tasks.Task<TResult>> second)`
- `[ext] public static System.Func<T, System.Threading.Tasks.ValueTask<TResult>> ComposeAsync<T, TIntermediate, TResult>(this System.Func<T, System.Threading.Tasks.ValueTask<TIntermediate>> first, System.Func<TIntermediate, System.Threading.Tasks.ValueTask<TResult>> second)`
- `[ext] public static System.Func<T, System.Threading.CancellationToken, System.Threading.Tasks.Task<TResult>> ComposeAsync<T, TIntermediate, TResult>(this System.Func<T, System.Threading.CancellationToken, System.Threading.Tasks.Task<TIntermediate>> first, System.Func<TIntermediate, System.Threading.CancellationToken, System.Threading.Tasks.Task<TResult>> second)`
- `[ext] public static System.Func<T, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TResult>> ComposeAsync<T, TIntermediate, TResult>(this System.Func<T, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TIntermediate>> first, System.Func<TIntermediate, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TResult>> second)`
- `[ext] public static System.Func<TFirst, System.Func<TSecond, TResult>> Curry<TFirst, TSecond, TResult>(this System.Func<TFirst, TSecond, TResult> function)`
- `[ext] public static System.Func<TSecond, TFirst, TResult> Flip<TFirst, TSecond, TResult>(this System.Func<TFirst, TSecond, TResult> function)`
- `[ext] public static System.Func<TSecond, TResult> Partial<TFirst, TSecond, TResult>(this System.Func<TFirst, TSecond, TResult> function, TFirst? first)`
- `[ext] public static TResult Pipe<T, TResult>(this T? value, System.Func<T, TResult> function)`
- `[ext] public static T Tap<T>(this T? value, System.Action<T> observer)`
- `[ext] public static System.Threading.Tasks.Task<T> TapAsync<T>(this T? value, System.Func<T, System.Threading.Tasks.Task> observer)`
- `[ext] public static System.Threading.Tasks.Task<T> TapAsync<T>(this T? value, System.Func<T, System.Threading.CancellationToken, System.Threading.Tasks.Task> observer, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<T> TapValueAsync<T>(this T? value, System.Func<T, System.Threading.Tasks.ValueTask> observer)`
- `[ext] public static System.Threading.Tasks.ValueTask<T> TapValueAsync<T>(this T? value, System.Func<T, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask> observer, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Func<TFirst, TSecond, TResult> Uncurry<TFirst, TSecond, TResult>(this System.Func<TFirst, System.Func<TSecond, TResult>> function)`

### Lens (class [static])

- `public static FunnySharp.Lens<TSource, TFocus> Create<TSource, TFocus>(System.Func<TSource, TFocus> get, System.Func<TSource, TFocus, TSource> set)`
- `public static FunnySharp.Lens<T, T> Identity<T>()`

### Lens`2<TSource, TFocus> (struct [readonly struct])

- `public FunnySharp.Lens<TSource, TNext> Compose<TNext>(FunnySharp.Lens<TFocus, TNext> next)`
- `public FunnySharp.Optional<TSource, TNext> Compose<TNext>(FunnySharp.Optional<TFocus, TNext> next)`
- `public TFocus Get(TSource? source)`
- `public TSource Set(TSource? source, TFocus? focus)`
- `public TSource Update(TSource? source, System.Func<TFocus, TFocus> update)`

### Option (class [static])

- `public static FunnySharp.Option<T> FromNullable<T>(T? value)`
- `where T : class`
- `public static FunnySharp.Option<T> FromNullable<T>(T?? value)`
- `where T : struct`
- `public static FunnySharp.Option<T> FromTry<T>(FunnySharp.TryOperation<T> operation)`
- `public static FunnySharp.Option<T> None<T>()`
- `public static FunnySharp.Option<T> Some<T>(T? value)`

### OptionExtensions (class [static])

- `[ext] public static System.Threading.Tasks.Task<FunnySharp.Option<TResult>> BindAsync<T, TResult>(this FunnySharp.Option<T> option, System.Func<T, System.Threading.Tasks.Task<FunnySharp.Option<TResult>>> binder)`
- `[ext] public static System.Threading.Tasks.Task<FunnySharp.Option<TResult>> BindAsync<T, TResult>(this FunnySharp.Option<T> option, System.Func<T, System.Threading.CancellationToken, System.Threading.Tasks.Task<FunnySharp.Option<TResult>>> binder, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<FunnySharp.Option<TResult>> BindValueAsync<T, TResult>(this FunnySharp.Option<T> option, System.Func<T, System.Threading.Tasks.ValueTask<FunnySharp.Option<TResult>>> binder)`
- `[ext] public static System.Threading.Tasks.ValueTask<FunnySharp.Option<TResult>> BindValueAsync<T, TResult>(this FunnySharp.Option<T> option, System.Func<T, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<FunnySharp.Option<TResult>>> binder, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static FunnySharp.Option<TValue> GetOption<TKey, TValue>(this System.Collections.Generic.IReadOnlyDictionary<TKey, TValue> source, TKey? key)`
- `[ext] public static System.Threading.Tasks.Task<FunnySharp.Option<TResult>> MapAsync<T, TResult>(this FunnySharp.Option<T> option, System.Func<T, System.Threading.Tasks.Task<TResult>> selector)`
- `[ext] public static System.Threading.Tasks.Task<FunnySharp.Option<TResult>> MapAsync<T, TResult>(this FunnySharp.Option<T> option, System.Func<T, System.Threading.CancellationToken, System.Threading.Tasks.Task<TResult>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<FunnySharp.Option<TResult>> MapValueAsync<T, TResult>(this FunnySharp.Option<T> option, System.Func<T, System.Threading.Tasks.ValueTask<TResult>> selector)`
- `[ext] public static System.Threading.Tasks.ValueTask<FunnySharp.Option<TResult>> MapValueAsync<T, TResult>(this FunnySharp.Option<T> option, System.Func<T, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TResult>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static FunnySharp.Option<T> ToOption<T>(this T? value)`
- `where T : class`
- `[ext] public static FunnySharp.Option<T> ToOption<T>(this T?? value)`
- `where T : struct`
- `[ext] public static System.Threading.Tasks.Task<FunnySharp.Option<T>> ToOptionAsync<T>(this System.Threading.Tasks.Task<T> task)`
- `where T : class`
- `[ext] public static System.Threading.Tasks.Task<FunnySharp.Option<T>> ToOptionAsync<T>(this System.Threading.Tasks.Task<T?> task)`
- `where T : struct`
- `[ext] public static System.Threading.Tasks.ValueTask<FunnySharp.Option<T>> ToOptionAsync<T>(this System.Threading.Tasks.ValueTask<T> task)`
- `where T : class`
- `[ext] public static System.Threading.Tasks.ValueTask<FunnySharp.Option<T>> ToOptionAsync<T>(this System.Threading.Tasks.ValueTask<T?> task)`
- `where T : struct`

### Option`1<T> (struct [readonly struct]) : System.IEquatable<FunnySharp.Option<T>>

- `public System.Boolean IsNone { get; }`
- `public System.Boolean IsSome { get; }`
- `public static FunnySharp.Option<T> None { get; }`
- `public FunnySharp.Option<TResult> Bind<TResult>(System.Func<T, FunnySharp.Option<TResult>> binder)`
- `public System.Boolean Equals(FunnySharp.Option<T> other)`
- `public System.Boolean Equals(System.Object? obj)`
- `public FunnySharp.Option<T> Filter(System.Func<T, System.Boolean> predicate)`
- `public System.Int32 GetHashCode()`
- `public T GetValueOr(T? fallback)`
- `public T GetValueOrDefault()`
- `public T GetValueOrElse(System.Func<T> fallbackFactory)`
- `public FunnySharp.Option<TResult> Map<TResult>(System.Func<T, TResult> selector)`
- `public TResult Match<TResult>(System.Func<T, TResult> some, System.Func<TResult> none)`
- `public System.Void Match(System.Action<T> some, System.Action none)`
- `public FunnySharp.Option<T> OrElse(FunnySharp.Option<T> fallback)`
- `public FunnySharp.Option<T> OrElseWith(System.Func<FunnySharp.Option<T>> fallbackFactory)`
- `public static FunnySharp.Option<T> Some(T? value)`
- `public System.String ToString()`
- `public System.Boolean TryGetValue(out T&? value)`
- `public FunnySharp.Option<System.ValueTuple<T, TSecond>> Zip<TSecond>(FunnySharp.Option<TSecond> second)`
- `public static System.Boolean op_Equality(FunnySharp.Option<T> left, FunnySharp.Option<T> right)`
- `public static System.Boolean op_Inequality(FunnySharp.Option<T> left, FunnySharp.Option<T> right)`

### Optional (class [static])

- `public static FunnySharp.Optional<TSource, TFocus> Create<TSource, TFocus>(System.Func<TSource, FunnySharp.Option<TFocus>> getOption, System.Func<TSource, TFocus, TSource> set)`

### Optional`2<TSource, TFocus> (struct [readonly struct])

- `public FunnySharp.Optional<TSource, TNext> Compose<TNext>(FunnySharp.Lens<TFocus, TNext> next)`
- `public FunnySharp.Optional<TSource, TNext> Compose<TNext>(FunnySharp.Optional<TFocus, TNext> next)`
- `public FunnySharp.Option<TFocus> GetOption(TSource? source)`
- `public TSource Set(TSource? source, TFocus? focus)`
- `public TSource Update(TSource? source, System.Func<TFocus, TFocus> update)`

### ParallelAsyncEnumerableExtensions (class [static])

- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> SelectParallelValueAsync<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Int32 maxConcurrency, System.Func<TSource, System.Threading.Tasks.ValueTask<TResult>> selector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> SelectParallelValueAsync<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Int32 maxConcurrency, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TResult>> selector)`

### ParallelAsyncSequenceExtensions (class [static])

- `[ext] public static System.Threading.Tasks.ValueTask<FunnySharp.Option<System.Collections.Generic.IReadOnlyList<TResult>>> TraverseParallelValueAsync<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Int32 maxConcurrency, System.Func<TSource, System.Threading.Tasks.ValueTask<FunnySharp.Option<TResult>>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<FunnySharp.Option<System.Collections.Generic.IReadOnlyList<TResult>>> TraverseParallelValueAsync<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Int32 maxConcurrency, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<FunnySharp.Option<TResult>>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<FunnySharp.Result<System.Collections.Generic.IReadOnlyList<TResult>, TError>> TraverseParallelValueAsync<TSource, TResult, TError>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Int32 maxConcurrency, System.Func<TSource, System.Threading.Tasks.ValueTask<FunnySharp.Result<TResult, TError>>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<FunnySharp.Result<System.Collections.Generic.IReadOnlyList<TResult>, TError>> TraverseParallelValueAsync<TSource, TResult, TError>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Int32 maxConcurrency, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<FunnySharp.Result<TResult, TError>>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<FunnySharp.Validation<System.Collections.Generic.IReadOnlyList<TResult>, TError>> TraverseParallelValueAsync<TSource, TResult, TError>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Int32 maxConcurrency, System.Func<TSource, System.Threading.Tasks.ValueTask<FunnySharp.Validation<TResult, TError>>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<FunnySharp.Validation<System.Collections.Generic.IReadOnlyList<TResult>, TError>> TraverseParallelValueAsync<TSource, TResult, TError>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Int32 maxConcurrency, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<FunnySharp.Validation<TResult, TError>>> selector, System.Threading.CancellationToken cancellationToken)`

### Result (class [static])

- `public static FunnySharp.Result<TValue, System.Exception> Try<TValue>(System.Func<TValue> operation)`
- `public static FunnySharp.Result<TValue, TError> Try<TValue, TError>(System.Func<TValue> operation, System.Func<System.Exception, TError> errorMapper)`
- `public static System.Threading.Tasks.Task<FunnySharp.Result<TValue, System.Exception>> TryAsync<TValue>(System.Func<System.Threading.Tasks.Task<TValue>> operation)`
- `public static System.Threading.Tasks.Task<FunnySharp.Result<TValue, TError>> TryAsync<TValue, TError>(System.Func<System.Threading.Tasks.Task<TValue>> operation, System.Func<System.Exception, TError> errorMapper)`
- `public static System.Threading.Tasks.ValueTask<FunnySharp.Result<TValue, System.Exception>> TryValueAsync<TValue>(System.Func<System.Threading.Tasks.ValueTask<TValue>> operation)`
- `public static System.Threading.Tasks.ValueTask<FunnySharp.Result<TValue, TError>> TryValueAsync<TValue, TError>(System.Func<System.Threading.Tasks.ValueTask<TValue>> operation, System.Func<System.Exception, TError> errorMapper)`

### ResultExtensions (class [static])

- `[ext] public static System.Threading.Tasks.Task<FunnySharp.Result<TResult, TError>> BindAsync<TValue, TError, TResult>(this FunnySharp.Result<TValue, TError> result, System.Func<TValue, System.Threading.Tasks.Task<FunnySharp.Result<TResult, TError>>> binder)`
- `[ext] public static System.Threading.Tasks.Task<FunnySharp.Result<TResult, TError>> BindAsync<TValue, TError, TResult>(this FunnySharp.Result<TValue, TError> result, System.Func<TValue, System.Threading.CancellationToken, System.Threading.Tasks.Task<FunnySharp.Result<TResult, TError>>> binder, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<FunnySharp.Result<TResult, TError>> BindValueAsync<TValue, TError, TResult>(this FunnySharp.Result<TValue, TError> result, System.Func<TValue, System.Threading.Tasks.ValueTask<FunnySharp.Result<TResult, TError>>> binder)`
- `[ext] public static System.Threading.Tasks.ValueTask<FunnySharp.Result<TResult, TError>> BindValueAsync<TValue, TError, TResult>(this FunnySharp.Result<TValue, TError> result, System.Func<TValue, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<FunnySharp.Result<TResult, TError>>> binder, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.Task<FunnySharp.Result<TResult, TError>> MapAsync<TValue, TError, TResult>(this FunnySharp.Result<TValue, TError> result, System.Func<TValue, System.Threading.Tasks.Task<TResult>> selector)`
- `[ext] public static System.Threading.Tasks.Task<FunnySharp.Result<TResult, TError>> MapAsync<TValue, TError, TResult>(this FunnySharp.Result<TValue, TError> result, System.Func<TValue, System.Threading.CancellationToken, System.Threading.Tasks.Task<TResult>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<FunnySharp.Result<TResult, TError>> MapValueAsync<TValue, TError, TResult>(this FunnySharp.Result<TValue, TError> result, System.Func<TValue, System.Threading.Tasks.ValueTask<TResult>> selector)`
- `[ext] public static System.Threading.Tasks.ValueTask<FunnySharp.Result<TResult, TError>> MapValueAsync<TValue, TError, TResult>(this FunnySharp.Result<TValue, TError> result, System.Func<TValue, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TResult>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static FunnySharp.Option<TValue> ToOption<TValue, TError>(this FunnySharp.Result<TValue, TError> result)`
- `[ext] public static FunnySharp.Result<TValue, TError> ToResult<TValue, TError>(this FunnySharp.Option<TValue> option, TError? error)`
- `[ext] public static FunnySharp.Result<TValue, TError> ToResult<TValue, TError>(this FunnySharp.Option<TValue> option, System.Func<TError> errorFactory)`

### Result`2<TValue, TError> (struct [readonly struct]) : System.IEquatable<FunnySharp.Result<TValue, TError>>

- `public System.Boolean IsFailure { get; }`
- `public System.Boolean IsSuccess { get; }`
- `public FunnySharp.Result<TResult, TError> Bind<TResult>(System.Func<TValue, FunnySharp.Result<TResult, TError>> binder)`
- `public FunnySharp.Result<TValue, TError> Ensure(System.Func<TValue, System.Boolean> predicate, TError? error)`
- `public FunnySharp.Result<TValue, TError> Ensure(System.Func<TValue, System.Boolean> predicate, System.Func<TValue, TError> errorFactory)`
- `public System.Boolean Equals(FunnySharp.Result<TValue, TError> other)`
- `public System.Boolean Equals(System.Object? obj)`
- `public static FunnySharp.Result<TValue, TError> Failure(TError? error)`
- `public System.Int32 GetHashCode()`
- `public FunnySharp.Result<TResult, TError> Map<TResult>(System.Func<TValue, TResult> selector)`
- `public FunnySharp.Result<TValue, TResultError> MapError<TResultError>(System.Func<TError, TResultError> selector)`
- `public TResult Match<TResult>(System.Func<TValue, TResult> success, System.Func<TError, TResult> failure)`
- `public System.Void Match(System.Action<TValue> success, System.Action<TError> failure)`
- `public FunnySharp.Result<TValue, TError> Recover(System.Func<TError, TValue> recovery)`
- `public FunnySharp.Result<TValue, TError> RecoverWith(System.Func<TError, FunnySharp.Result<TValue, TError>> recovery)`
- `public FunnySharp.Result<TResult, TError> Select<TResult>(System.Func<TValue, TResult> selector)`
- `public FunnySharp.Result<TResult, TError> SelectMany<TIntermediate, TResult>(System.Func<TValue, FunnySharp.Result<TIntermediate, TError>> binder, System.Func<TValue, TIntermediate, TResult> projector)`
- `public static FunnySharp.Result<TValue, TError> Success(TValue? value)`
- `public System.String ToString()`
- `public System.Boolean TryGetError(out TError&? error)`
- `public System.Boolean TryGetValue(out TValue&? value)`
- `public FunnySharp.Result<System.ValueTuple<TValue, TSecond>, TError> Zip<TSecond>(FunnySharp.Result<TSecond, TError> second)`
- `public FunnySharp.Result<System.ValueTuple<TValue, TSecond>, TError> ZipWith<TSecond>(System.Func<FunnySharp.Result<TSecond, TError>> secondFactory)`
- `public static System.Boolean op_Equality(FunnySharp.Result<TValue, TError> left, FunnySharp.Result<TValue, TError> right)`
- `public static System.Boolean op_Inequality(FunnySharp.Result<TValue, TError> left, FunnySharp.Result<TValue, TError> right)`

### SequenceExtensions (class [static])

- `[ext] public static FunnySharp.Option<System.Collections.Generic.IReadOnlyList<TValue>> Sequence<TValue>(this System.Collections.Generic.IEnumerable<FunnySharp.Option<TValue>> source)`
- `[ext] public static FunnySharp.Result<System.Collections.Generic.IReadOnlyList<TValue>, TError> Sequence<TValue, TError>(this System.Collections.Generic.IEnumerable<FunnySharp.Result<TValue, TError>> source)`
- `[ext] public static FunnySharp.Validation<System.Collections.Generic.IReadOnlyList<TValue>, TError> Sequence<TValue, TError>(this System.Collections.Generic.IEnumerable<FunnySharp.Validation<TValue, TError>> source)`
- `[ext] public static FunnySharp.Option<System.Collections.Generic.IReadOnlyList<TResult>> Traverse<TSource, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, FunnySharp.Option<TResult>> selector)`
- `[ext] public static FunnySharp.Result<System.Collections.Generic.IReadOnlyList<TResult>, TError> Traverse<TSource, TResult, TError>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, FunnySharp.Result<TResult, TError>> selector)`
- `[ext] public static FunnySharp.Validation<System.Collections.Generic.IReadOnlyList<TResult>, TError> Traverse<TSource, TResult, TError>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, FunnySharp.Validation<TResult, TError>> selector)`

### SpanPipelineExtensions (class [static])

- `[ext] public static System.Span<TResult> ChooseTo<TSource, TResult>(this System.ReadOnlySpan<TSource> source, System.Span<TResult> destination, System.Func<TSource, FunnySharp.Option<TResult>> chooser)`
- `[ext] public static System.Span<TResult> ChooseTo<TSource, TResult>(this System.Span<TSource> source, System.Span<TResult> destination, System.Func<TSource, FunnySharp.Option<TResult>> chooser)`
- `[ext] public static System.Memory<TResult> ChooseTo<TSource, TResult>(this System.ReadOnlyMemory<TSource> source, System.Memory<TResult> destination, System.Func<TSource, FunnySharp.Option<TResult>> chooser)`
- `[ext] public static System.Memory<TResult> ChooseTo<TSource, TResult>(this System.Memory<TSource> source, System.Memory<TResult> destination, System.Func<TSource, FunnySharp.Option<TResult>> chooser)`
- `[ext] public static System.Span<T> SelectInPlace<T>(this System.Span<T> source, System.Func<T, T> selector)`
- `[ext] public static System.Memory<T> SelectInPlace<T>(this System.Memory<T> source, System.Func<T, T> selector)`
- `[ext] public static System.Span<TResult> SelectTo<TSource, TResult>(this System.ReadOnlySpan<TSource> source, System.Span<TResult> destination, System.Func<TSource, TResult> selector)`
- `[ext] public static System.Span<TResult> SelectTo<TSource, TResult>(this System.Span<TSource> source, System.Span<TResult> destination, System.Func<TSource, TResult> selector)`
- `[ext] public static System.Memory<TResult> SelectTo<TSource, TResult>(this System.ReadOnlyMemory<TSource> source, System.Memory<TResult> destination, System.Func<TSource, TResult> selector)`
- `[ext] public static System.Memory<TResult> SelectTo<TSource, TResult>(this System.Memory<TSource> source, System.Memory<TResult> destination, System.Func<TSource, TResult> selector)`
- `[ext] public static System.Span<T> WhereInPlace<T>(this System.Span<T> source, System.Func<T, System.Boolean> predicate)`
- `[ext] public static System.Memory<T> WhereInPlace<T>(this System.Memory<T> source, System.Func<T, System.Boolean> predicate)`
- `[ext] public static System.Span<T> WhereTo<T>(this System.ReadOnlySpan<T> source, System.Span<T> destination, System.Func<T, System.Boolean> predicate)`
- `[ext] public static System.Span<T> WhereTo<T>(this System.Span<T> source, System.Span<T> destination, System.Func<T, System.Boolean> predicate)`
- `[ext] public static System.Memory<T> WhereTo<T>(this System.ReadOnlyMemory<T> source, System.Memory<T> destination, System.Func<T, System.Boolean> predicate)`
- `[ext] public static System.Memory<T> WhereTo<T>(this System.Memory<T> source, System.Memory<T> destination, System.Func<T, System.Boolean> predicate)`

### StateChange`2<TState, TOutput> (class [sealed]) : System.IEquatable<FunnySharp.StateChange<TState, TOutput>>

- `public System.Collections.Generic.IReadOnlyList<TOutput> Outputs { get; }`
- `public TState State { get; } (nullability: Nullable)`
- `public System.Boolean Equals(FunnySharp.StateChange<TState, TOutput>? other)`
- `public System.Boolean Equals(System.Object? obj)`
- `public System.Int32 GetHashCode()`
- `public static FunnySharp.StateChange<TState, TOutput> To(TState? state, params TOutput[] outputs)`
- `public System.String ToString()`

### StateMachineExtensions (class [static])

- `[ext] public static FunnySharp.StateMachine<TState, TEvent, TOutput, TError> OrElse<TState, TEvent, TOutput, TError>(this FunnySharp.StateMachine<TState, TEvent, TOutput, TError> machine, FunnySharp.StateMachine<TState, TEvent, TOutput, TError> fallback)`
- `[ext] public static FunnySharp.TransitionResult<TState, TOutput, TError> Replay<TState, TEvent, TOutput, TError>(this FunnySharp.StateMachine<TState, TEvent, TOutput, TError> machine, TState? initialState, System.Collections.Generic.IEnumerable<TEvent> events)`

### StateMachine`4<TState, TEvent, TOutput, TError> (delegate [sealed]) : System.ICloneable, System.Runtime.Serialization.ISerializable

- `public StateMachine`4(System.Object object, System.IntPtr method)`
- `delegate FunnySharp.TransitionResult<TState, TOutput, TError> Invoke(TState? state, TEvent? event)`
- `public System.IAsyncResult BeginInvoke(TState? state, TEvent? event, System.AsyncCallback callback, System.Object object)`
- `public FunnySharp.TransitionResult<TState, TOutput, TError> EndInvoke(System.IAsyncResult result)`

### StateTransitionExtensions (class [static])

- `[ext] public static FunnySharp.StateTransition<TState, TOutput> Then<TState, TOutput>(this FunnySharp.StateTransition<TState, TOutput> first, FunnySharp.StateTransition<TState, TOutput> second)`

### StateTransition`2<TState, TOutput> (delegate [sealed]) : System.ICloneable, System.Runtime.Serialization.ISerializable

- `public StateTransition`2(System.Object object, System.IntPtr method)`
- `delegate FunnySharp.StateChange<TState, TOutput> Invoke(TState? state)`
- `public System.IAsyncResult BeginInvoke(TState? state, System.AsyncCallback callback, System.Object object)`
- `public FunnySharp.StateChange<TState, TOutput> EndInvoke(System.IAsyncResult result)`

### TransitionResult`3<TState, TOutput, TError> (struct [readonly struct]) : System.IEquatable<FunnySharp.TransitionResult<TState, TOutput, TError>>

- `public System.Boolean IsApplied { get; }`
- `public System.Boolean IsFailed { get; }`
- `public System.Boolean IsRejected { get; }`
- `public System.Boolean IsUndefined { get; }`
- `public FunnySharp.TransitionStatus Status { get; }`
- `public static FunnySharp.TransitionResult<TState, TOutput, TError> Applied(FunnySharp.StateChange<TState, TOutput> change)`
- `public System.Boolean Equals(FunnySharp.TransitionResult<TState, TOutput, TError> other)`
- `public System.Boolean Equals(System.Object? obj)`
- `public static FunnySharp.TransitionResult<TState, TOutput, TError> Failed(TError? error)`
- `public System.Int32 GetHashCode()`
- `public TResult Match<TResult>(System.Func<FunnySharp.StateChange<TState, TOutput>, TResult> applied, System.Func<TError, TResult> rejected, System.Func<TError, TResult> failed, System.Func<TResult> undefined)`
- `public static FunnySharp.TransitionResult<TState, TOutput, TError> Rejected(TError? error)`
- `public System.String ToString()`
- `public System.Boolean TryGetChange(out FunnySharp.StateChange<TState, TOutput>&? change)`
- `public System.Boolean TryGetError(out TError&? error)`
- `public static FunnySharp.TransitionResult<TState, TOutput, TError> Undefined()`
- `public static System.Boolean op_Equality(FunnySharp.TransitionResult<TState, TOutput, TError> left, FunnySharp.TransitionResult<TState, TOutput, TError> right)`
- `public static System.Boolean op_Inequality(FunnySharp.TransitionResult<TState, TOutput, TError> left, FunnySharp.TransitionResult<TState, TOutput, TError> right)`

### TransitionStatus (enum) : System.IComparable, System.ISpanFormattable, System.IFormattable, System.IConvertible

- `Undefined = 0`
- `Applied = 1`
- `Rejected = 2`
- `Failed = 3`

### TryOperation`1<T> (delegate [sealed]) : System.ICloneable, System.Runtime.Serialization.ISerializable

- `public TryOperation`1(System.Object object, System.IntPtr method)`
- `delegate System.Boolean Invoke(out T&? value)`
- `public System.IAsyncResult BeginInvoke(out T&? value, System.AsyncCallback callback, System.Object object)`
- `public System.Boolean EndInvoke(out T&? value, System.IAsyncResult result)`

### ValidationExtensions (class [static])

- `[ext] public static FunnySharp.Validation<TResult, TError> Apply<TValue, TResult, TError>(this FunnySharp.Validation<System.Func<TValue, TResult>, TError> function, FunnySharp.Validation<TValue, TError> argument)`

### Validation`2<TValue, TError> (struct [readonly struct]) : System.IEquatable<FunnySharp.Validation<TValue, TError>>

- `public System.Boolean IsInvalid { get; }`
- `public System.Boolean IsValid { get; }`
- `public System.Boolean Equals(FunnySharp.Validation<TValue, TError> other)`
- `public System.Boolean Equals(System.Object? obj)`
- `public System.Int32 GetHashCode()`
- `public static FunnySharp.Validation<TValue, TError> Invalid(TError? error)`
- `public static FunnySharp.Validation<TValue, TError> InvalidMany(System.Collections.Generic.IEnumerable<TError> errors)`
- `public FunnySharp.Validation<TResult, TError> Map<TResult>(System.Func<TValue, TResult> selector)`
- `public FunnySharp.Validation<TValue, TResultError> MapErrors<TResultError>(System.Func<TError, TResultError> selector)`
- `public TResult Match<TResult>(System.Func<TValue, TResult> valid, System.Func<System.Collections.Generic.IReadOnlyList<TError>, TResult> invalid)`
- `public System.Void Match(System.Action<TValue> valid, System.Action<System.Collections.Generic.IReadOnlyList<TError>> invalid)`
- `public System.String ToString()`
- `public System.Boolean TryGetErrors(out System.Collections.Generic.IReadOnlyList<TError>&? errors)`
- `public System.Boolean TryGetValue(out TValue&? value)`
- `public static FunnySharp.Validation<TValue, TError> Valid(TValue? value)`
- `public FunnySharp.Validation<System.ValueTuple<TValue, TSecond>, TError> Zip<TSecond>(FunnySharp.Validation<TSecond, TError> second)`
- `public static System.Boolean op_Equality(FunnySharp.Validation<TValue, TError> left, FunnySharp.Validation<TValue, TError> right)`
- `public static System.Boolean op_Inequality(FunnySharp.Validation<TValue, TError> left, FunnySharp.Validation<TValue, TError> right)`

