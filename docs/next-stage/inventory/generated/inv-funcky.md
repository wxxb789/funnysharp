# Public API inventory: funcky

Assemblies: Funcky 3.6.0.0

Type count: 85

## Funcky

### AsyncFunctional (class [static])

- `public static System.Threading.Tasks.ValueTask<TResult> RetryAsync<TResult>(System.Func<System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TResult>>> producer, System.Threading.CancellationToken cancellationToken)`
- `public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TResult>> RetryAsync<TResult>(System.Func<System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TResult>>> producer, Funcky.RetryPolicies.IRetryPolicy retryPolicy, System.Threading.CancellationToken cancellationToken)`
- `public static System.Threading.Tasks.ValueTask<TResult> RetryAsync<TResult>(System.Func<TResult> producer, System.Func<System.Exception, System.Boolean> shouldRetry, Funcky.RetryPolicies.IRetryPolicy retryPolicy, System.Threading.CancellationToken cancellationToken)`
- `public static System.Threading.Tasks.ValueTask RetryAsync(System.Action action, System.Func<System.Exception, System.Boolean> shouldRetry, Funcky.RetryPolicies.IRetryPolicy retryPolicy, System.Threading.CancellationToken cancellationToken)`
- `public static System.Threading.Tasks.ValueTask<TResult> RetryAwaitAsync<TResult>(System.Func<System.Threading.Tasks.ValueTask<TResult>> producer, System.Func<System.Exception, System.Boolean> shouldRetry, Funcky.RetryPolicies.IRetryPolicy retryPolicy, System.Threading.CancellationToken cancellationToken)`
- `public static System.Threading.Tasks.ValueTask RetryAwaitAsync(System.Func<System.Threading.Tasks.ValueTask> action, System.Func<System.Exception, System.Boolean> shouldRetry, Funcky.RetryPolicies.IRetryPolicy retryPolicy, System.Threading.CancellationToken cancellationToken)`

### AsyncSequence (class [static])

- `public static System.Collections.Generic.IAsyncEnumerable<TSource> Concat<TSource>(params System.Collections.Generic.IAsyncEnumerable<TSource>[] sources)`
- `public static System.Collections.Generic.IAsyncEnumerable<TSource> Concat<TSource>(System.Collections.Generic.IAsyncEnumerable<System.Collections.Generic.IAsyncEnumerable<TSource>> sources)`
- `public static System.Collections.Generic.IAsyncEnumerable<TSource> Concat<TSource>(System.Collections.Generic.IEnumerable<System.Collections.Generic.IAsyncEnumerable<TSource>> sources)`
- `public static System.Collections.Generic.IAsyncEnumerable<TSource> Concat<TSource>(System.Collections.Generic.IAsyncEnumerable<System.Collections.Generic.IEnumerable<TSource>> sources)`
- `public static System.Collections.Generic.IAsyncEnumerable<TResult> Cycle<TResult>(TResult? element)`
- `public static Funcky.IAsyncBuffer<TSource> CycleRange<TSource>(System.Collections.Generic.IAsyncEnumerable<TSource> sequence)`
- `public static System.Collections.Generic.IAsyncEnumerable<TResult> FromNullable<TResult>(TResult? element)`
- `where TResult : class`
- `public static System.Collections.Generic.IAsyncEnumerable<TResult> FromNullable<TResult>(TResult?? element)`
- `where TResult : struct`
- `public static Funcky.IAsyncBuffer<TSource> RepeatRange<TSource>(System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Int32 count)`
- `public static System.Collections.Generic.IAsyncEnumerable<TResult> Return<TResult>(TResult? element)`
- `public static System.Collections.Generic.IAsyncEnumerable<TResult> Return<TResult>(params TResult[] elements)`
- `public static System.Collections.Generic.IAsyncEnumerable<TResult> Successors<TResult>(Funcky.Monads.Option<TResult> first, System.Func<TResult, System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TResult>>> successor)`
- `public static System.Collections.Generic.IAsyncEnumerable<TResult> Successors<TResult>(TResult first, System.Func<TResult, System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TResult>>> successor)`
- `public static System.Collections.Generic.IAsyncEnumerable<TResult> Successors<TResult>(Funcky.Monads.Option<TResult> first, System.Func<TResult, System.Threading.Tasks.ValueTask<TResult>> successor)`
- `public static System.Collections.Generic.IAsyncEnumerable<TResult> Successors<TResult>(TResult? first, System.Func<TResult, System.Threading.Tasks.ValueTask<TResult>> successor)`

### Discard (class [static])

- `public static Funcky.Unit __`

### DownCast`1<TResult> (class [static])

- `where TResult : class`

- `public static Funcky.Monads.Option<TResult> From<TItem>(Funcky.Monads.Option<TItem> option)`
- `where TItem : class`
- `public static Funcky.Monads.Result<TResult> From<TItem>(Funcky.Monads.Result<TItem> result)`
- `where TItem : class`
- `public static Funcky.Monads.Either<TLeft, TResult> From<TLeft, TRight>(Funcky.Monads.Either<TLeft, TRight> either, System.Func<TLeft> failedCast)`
- `where TRight : class`

### EitherOrBoth (class [static])

- `public static Funcky.Monads.Option<Funcky.EitherOrBoth<TLeft, TRight>> FromOptions<TLeft, TRight>(Funcky.Monads.Option<TLeft> left, Funcky.Monads.Option<TRight> right)`

### EitherOrBoth`2<TLeft, TRight> (struct [readonly struct]) : System.IEquatable<Funcky.EitherOrBoth<TLeft, TRight>>

- `public static Funcky.EitherOrBoth<TLeft, TRight> Both(TLeft left, TRight right)`
- `public System.Boolean Equals(System.Object? obj)`
- `public System.Boolean Equals(Funcky.EitherOrBoth<TLeft, TRight> other)`
- `public System.Int32 GetHashCode()`
- `public static Funcky.EitherOrBoth<TLeft, TRight> Left(TLeft left)`
- `public TMatchResult Match<TMatchResult>(System.Func<TLeft, TMatchResult> left, System.Func<TRight, TMatchResult> right, System.Func<TLeft, TRight, TMatchResult> both)`
- `public static Funcky.EitherOrBoth<TLeft, TRight> Right(TRight right)`
- `public System.Void Switch(System.Action<TLeft> left, System.Action<TRight> right, System.Action<TLeft, TRight> both)`
- `public static System.Boolean op_Equality(Funcky.EitherOrBoth<TLeft, TRight> left, Funcky.EitherOrBoth<TLeft, TRight> right)`
- `public static System.Boolean op_Inequality(Funcky.EitherOrBoth<TLeft, TRight> left, Funcky.EitherOrBoth<TLeft, TRight> right)`

### Functional (class [static])

- `public static System.Func<Funcky.Unit> ActionToUnit(System.Action action)`
- `public static System.Func<T1, Funcky.Unit> ActionToUnit<T1>(System.Action<T1> action)`
- `public static System.Func<T1, T2, Funcky.Unit> ActionToUnit<T1, T2>(System.Action<T1, T2> action)`
- `public static System.Func<T1, T2, T3, Funcky.Unit> ActionToUnit<T1, T2, T3>(System.Action<T1, T2, T3> action)`
- `public static System.Func<T1, T2, T3, T4, Funcky.Unit> ActionToUnit<T1, T2, T3, T4>(System.Action<T1, T2, T3, T4> action)`
- `public static System.Func<T1, T2, T3, T4, T5, Funcky.Unit> ActionToUnit<T1, T2, T3, T4, T5>(System.Action<T1, T2, T3, T4, T5> action)`
- `public static System.Func<T1, T2, T3, T4, T5, T6, Funcky.Unit> ActionToUnit<T1, T2, T3, T4, T5, T6>(System.Action<T1, T2, T3, T4, T5, T6> action)`
- `public static System.Func<T1, T2, T3, T4, T5, T6, T7, Funcky.Unit> ActionToUnit<T1, T2, T3, T4, T5, T6, T7>(System.Action<T1, T2, T3, T4, T5, T6, T7> action)`
- `public static System.Func<T1, T2, T3, T4, T5, T6, T7, T8, Funcky.Unit> ActionToUnit<T1, T2, T3, T4, T5, T6, T7, T8>(System.Action<T1, T2, T3, T4, T5, T6, T7, T8> action)`
- `public static System.Func<T, System.Boolean> All<T>(params System.Func<T, System.Boolean>[] predicates)`
- `public static System.Func<T, System.Boolean> Any<T>(params System.Func<T, System.Boolean>[] predicates)`
- `public static System.Func<T1, TResult> Apply<T1, T2, TResult>(System.Func<T1, T2, TResult> func, Funcky.Unit p1, T2? p2)`
- `public static System.Func<T2, TResult> Apply<T1, T2, TResult>(System.Func<T1, T2, TResult> func, T1? p1, Funcky.Unit p2)`
- `public static System.Func<T1, TResult> Apply<T1, T2, T3, TResult>(System.Func<T1, T2, T3, TResult> func, Funcky.Unit p1, T2? p2, T3? p3)`
- `public static System.Func<T2, TResult> Apply<T1, T2, T3, TResult>(System.Func<T1, T2, T3, TResult> func, T1? p1, Funcky.Unit p2, T3? p3)`
- `public static System.Func<T1, T2, TResult> Apply<T1, T2, T3, TResult>(System.Func<T1, T2, T3, TResult> func, Funcky.Unit p1, Funcky.Unit p2, T3? p3)`
- `public static System.Func<T3, TResult> Apply<T1, T2, T3, TResult>(System.Func<T1, T2, T3, TResult> func, T1? p1, T2? p2, Funcky.Unit p3)`
- `public static System.Func<T1, T3, TResult> Apply<T1, T2, T3, TResult>(System.Func<T1, T2, T3, TResult> func, Funcky.Unit p1, T2? p2, Funcky.Unit p3)`
- `public static System.Func<T2, T3, TResult> Apply<T1, T2, T3, TResult>(System.Func<T1, T2, T3, TResult> func, T1? p1, Funcky.Unit p2, Funcky.Unit p3)`
- `public static System.Func<T1, TResult> Apply<T1, T2, T3, T4, TResult>(System.Func<T1, T2, T3, T4, TResult> func, Funcky.Unit p1, T2? p2, T3? p3, T4? p4)`
- `public static System.Func<T2, TResult> Apply<T1, T2, T3, T4, TResult>(System.Func<T1, T2, T3, T4, TResult> func, T1? p1, Funcky.Unit p2, T3? p3, T4? p4)`
- `public static System.Func<T1, T2, TResult> Apply<T1, T2, T3, T4, TResult>(System.Func<T1, T2, T3, T4, TResult> func, Funcky.Unit p1, Funcky.Unit p2, T3? p3, T4? p4)`
- `public static System.Func<T3, TResult> Apply<T1, T2, T3, T4, TResult>(System.Func<T1, T2, T3, T4, TResult> func, T1? p1, T2? p2, Funcky.Unit p3, T4? p4)`
- `public static System.Func<T1, T3, TResult> Apply<T1, T2, T3, T4, TResult>(System.Func<T1, T2, T3, T4, TResult> func, Funcky.Unit p1, T2? p2, Funcky.Unit p3, T4? p4)`
- `public static System.Func<T2, T3, TResult> Apply<T1, T2, T3, T4, TResult>(System.Func<T1, T2, T3, T4, TResult> func, T1? p1, Funcky.Unit p2, Funcky.Unit p3, T4? p4)`
- `public static System.Func<T1, T2, T3, TResult> Apply<T1, T2, T3, T4, TResult>(System.Func<T1, T2, T3, T4, TResult> func, Funcky.Unit p1, Funcky.Unit p2, Funcky.Unit p3, T4? p4)`
- `public static System.Func<T4, TResult> Apply<T1, T2, T3, T4, TResult>(System.Func<T1, T2, T3, T4, TResult> func, T1? p1, T2? p2, T3? p3, Funcky.Unit p4)`
- `public static System.Func<T1, T4, TResult> Apply<T1, T2, T3, T4, TResult>(System.Func<T1, T2, T3, T4, TResult> func, Funcky.Unit p1, T2? p2, T3? p3, Funcky.Unit p4)`
- `public static System.Func<T2, T4, TResult> Apply<T1, T2, T3, T4, TResult>(System.Func<T1, T2, T3, T4, TResult> func, T1? p1, Funcky.Unit p2, T3? p3, Funcky.Unit p4)`
- `public static System.Func<T1, T2, T4, TResult> Apply<T1, T2, T3, T4, TResult>(System.Func<T1, T2, T3, T4, TResult> func, Funcky.Unit p1, Funcky.Unit p2, T3? p3, Funcky.Unit p4)`
- `public static System.Func<T3, T4, TResult> Apply<T1, T2, T3, T4, TResult>(System.Func<T1, T2, T3, T4, TResult> func, T1? p1, T2? p2, Funcky.Unit p3, Funcky.Unit p4)`
- `public static System.Func<T1, T3, T4, TResult> Apply<T1, T2, T3, T4, TResult>(System.Func<T1, T2, T3, T4, TResult> func, Funcky.Unit p1, T2? p2, Funcky.Unit p3, Funcky.Unit p4)`
- `public static System.Func<T2, T3, T4, TResult> Apply<T1, T2, T3, T4, TResult>(System.Func<T1, T2, T3, T4, TResult> func, T1? p1, Funcky.Unit p2, Funcky.Unit p3, Funcky.Unit p4)`
- `public static System.Func<T1, TResult> Apply<T1, T2, T3, T4, T5, TResult>(System.Func<T1, T2, T3, T4, T5, TResult> func, Funcky.Unit p1, T2? p2, T3? p3, T4? p4, T5? p5)`
- `public static System.Func<T2, TResult> Apply<T1, T2, T3, T4, T5, TResult>(System.Func<T1, T2, T3, T4, T5, TResult> func, T1? p1, Funcky.Unit p2, T3? p3, T4? p4, T5? p5)`
- `public static System.Func<T1, T2, TResult> Apply<T1, T2, T3, T4, T5, TResult>(System.Func<T1, T2, T3, T4, T5, TResult> func, Funcky.Unit p1, Funcky.Unit p2, T3? p3, T4? p4, T5? p5)`
- `public static System.Func<T3, TResult> Apply<T1, T2, T3, T4, T5, TResult>(System.Func<T1, T2, T3, T4, T5, TResult> func, T1? p1, T2? p2, Funcky.Unit p3, T4? p4, T5? p5)`
- `public static System.Func<T1, T3, TResult> Apply<T1, T2, T3, T4, T5, TResult>(System.Func<T1, T2, T3, T4, T5, TResult> func, Funcky.Unit p1, T2? p2, Funcky.Unit p3, T4? p4, T5? p5)`
- `public static System.Func<T2, T3, TResult> Apply<T1, T2, T3, T4, T5, TResult>(System.Func<T1, T2, T3, T4, T5, TResult> func, T1? p1, Funcky.Unit p2, Funcky.Unit p3, T4? p4, T5? p5)`
- `public static System.Func<T1, T2, T3, TResult> Apply<T1, T2, T3, T4, T5, TResult>(System.Func<T1, T2, T3, T4, T5, TResult> func, Funcky.Unit p1, Funcky.Unit p2, Funcky.Unit p3, T4? p4, T5? p5)`
- `public static System.Func<T4, TResult> Apply<T1, T2, T3, T4, T5, TResult>(System.Func<T1, T2, T3, T4, T5, TResult> func, T1? p1, T2? p2, T3? p3, Funcky.Unit p4, T5? p5)`
- `public static System.Func<T1, T4, TResult> Apply<T1, T2, T3, T4, T5, TResult>(System.Func<T1, T2, T3, T4, T5, TResult> func, Funcky.Unit p1, T2? p2, T3? p3, Funcky.Unit p4, T5? p5)`
- `public static System.Func<T2, T4, TResult> Apply<T1, T2, T3, T4, T5, TResult>(System.Func<T1, T2, T3, T4, T5, TResult> func, T1? p1, Funcky.Unit p2, T3? p3, Funcky.Unit p4, T5? p5)`
- `public static System.Func<T1, T2, T4, TResult> Apply<T1, T2, T3, T4, T5, TResult>(System.Func<T1, T2, T3, T4, T5, TResult> func, Funcky.Unit p1, Funcky.Unit p2, T3? p3, Funcky.Unit p4, T5? p5)`
- `public static System.Func<T3, T4, TResult> Apply<T1, T2, T3, T4, T5, TResult>(System.Func<T1, T2, T3, T4, T5, TResult> func, T1? p1, T2? p2, Funcky.Unit p3, Funcky.Unit p4, T5? p5)`
- `public static System.Func<T1, T3, T4, TResult> Apply<T1, T2, T3, T4, T5, TResult>(System.Func<T1, T2, T3, T4, T5, TResult> func, Funcky.Unit p1, T2? p2, Funcky.Unit p3, Funcky.Unit p4, T5? p5)`
- `public static System.Func<T2, T3, T4, TResult> Apply<T1, T2, T3, T4, T5, TResult>(System.Func<T1, T2, T3, T4, T5, TResult> func, T1? p1, Funcky.Unit p2, Funcky.Unit p3, Funcky.Unit p4, T5? p5)`
- `public static System.Func<T1, T2, T3, T4, TResult> Apply<T1, T2, T3, T4, T5, TResult>(System.Func<T1, T2, T3, T4, T5, TResult> func, Funcky.Unit p1, Funcky.Unit p2, Funcky.Unit p3, Funcky.Unit p4, T5? p5)`
- `public static System.Func<T5, TResult> Apply<T1, T2, T3, T4, T5, TResult>(System.Func<T1, T2, T3, T4, T5, TResult> func, T1? p1, T2? p2, T3? p3, T4? p4, Funcky.Unit p5)`
- `public static System.Func<T1, T5, TResult> Apply<T1, T2, T3, T4, T5, TResult>(System.Func<T1, T2, T3, T4, T5, TResult> func, Funcky.Unit p1, T2? p2, T3? p3, T4? p4, Funcky.Unit p5)`
- `public static System.Func<T2, T5, TResult> Apply<T1, T2, T3, T4, T5, TResult>(System.Func<T1, T2, T3, T4, T5, TResult> func, T1? p1, Funcky.Unit p2, T3? p3, T4? p4, Funcky.Unit p5)`
- `public static System.Func<T1, T2, T5, TResult> Apply<T1, T2, T3, T4, T5, TResult>(System.Func<T1, T2, T3, T4, T5, TResult> func, Funcky.Unit p1, Funcky.Unit p2, T3? p3, T4? p4, Funcky.Unit p5)`
- `public static System.Func<T3, T5, TResult> Apply<T1, T2, T3, T4, T5, TResult>(System.Func<T1, T2, T3, T4, T5, TResult> func, T1? p1, T2? p2, Funcky.Unit p3, T4? p4, Funcky.Unit p5)`
- `public static System.Func<T1, T3, T5, TResult> Apply<T1, T2, T3, T4, T5, TResult>(System.Func<T1, T2, T3, T4, T5, TResult> func, Funcky.Unit p1, T2? p2, Funcky.Unit p3, T4? p4, Funcky.Unit p5)`
- `public static System.Func<T2, T3, T5, TResult> Apply<T1, T2, T3, T4, T5, TResult>(System.Func<T1, T2, T3, T4, T5, TResult> func, T1? p1, Funcky.Unit p2, Funcky.Unit p3, T4? p4, Funcky.Unit p5)`
- `public static System.Func<T1, T2, T3, T5, TResult> Apply<T1, T2, T3, T4, T5, TResult>(System.Func<T1, T2, T3, T4, T5, TResult> func, Funcky.Unit p1, Funcky.Unit p2, Funcky.Unit p3, T4? p4, Funcky.Unit p5)`
- `public static System.Func<T4, T5, TResult> Apply<T1, T2, T3, T4, T5, TResult>(System.Func<T1, T2, T3, T4, T5, TResult> func, T1? p1, T2? p2, T3? p3, Funcky.Unit p4, Funcky.Unit p5)`
- `public static System.Func<T1, T4, T5, TResult> Apply<T1, T2, T3, T4, T5, TResult>(System.Func<T1, T2, T3, T4, T5, TResult> func, Funcky.Unit p1, T2? p2, T3? p3, Funcky.Unit p4, Funcky.Unit p5)`
- `public static System.Func<T2, T4, T5, TResult> Apply<T1, T2, T3, T4, T5, TResult>(System.Func<T1, T2, T3, T4, T5, TResult> func, T1? p1, Funcky.Unit p2, T3? p3, Funcky.Unit p4, Funcky.Unit p5)`
- `public static System.Func<T1, T2, T4, T5, TResult> Apply<T1, T2, T3, T4, T5, TResult>(System.Func<T1, T2, T3, T4, T5, TResult> func, Funcky.Unit p1, Funcky.Unit p2, T3? p3, Funcky.Unit p4, Funcky.Unit p5)`
- `public static System.Func<T3, T4, T5, TResult> Apply<T1, T2, T3, T4, T5, TResult>(System.Func<T1, T2, T3, T4, T5, TResult> func, T1? p1, T2? p2, Funcky.Unit p3, Funcky.Unit p4, Funcky.Unit p5)`
- `public static System.Func<T1, T3, T4, T5, TResult> Apply<T1, T2, T3, T4, T5, TResult>(System.Func<T1, T2, T3, T4, T5, TResult> func, Funcky.Unit p1, T2? p2, Funcky.Unit p3, Funcky.Unit p4, Funcky.Unit p5)`
- `public static System.Func<T2, T3, T4, T5, TResult> Apply<T1, T2, T3, T4, T5, TResult>(System.Func<T1, T2, T3, T4, T5, TResult> func, T1? p1, Funcky.Unit p2, Funcky.Unit p3, Funcky.Unit p4, Funcky.Unit p5)`
- `public static System.Func<T1, System.Func<T2, TResult>> Curry<T1, T2, TResult>(System.Func<T1, T2, TResult> function)`
- `public static System.Func<T1, System.Func<T2, System.Func<T3, TResult>>> Curry<T1, T2, T3, TResult>(System.Func<T1, T2, T3, TResult> function)`
- `public static System.Func<T1, System.Func<T2, System.Func<T3, System.Func<T4, TResult>>>> Curry<T1, T2, T3, T4, TResult>(System.Func<T1, T2, T3, T4, TResult> function)`
- `public static System.Func<T1, System.Func<T2, System.Func<T3, System.Func<T4, System.Func<T5, TResult>>>>> Curry<T1, T2, T3, T4, T5, TResult>(System.Func<T1, T2, T3, T4, T5, TResult> function)`
- `public static System.Func<T1, System.Func<T2, System.Func<T3, System.Func<T4, System.Func<T5, System.Func<T6, TResult>>>>>> Curry<T1, T2, T3, T4, T5, T6, TResult>(System.Func<T1, T2, T3, T4, T5, T6, TResult> function)`
- `public static System.Func<T1, System.Func<T2, System.Func<T3, System.Func<T4, System.Func<T5, System.Func<T6, System.Func<T7, TResult>>>>>>> Curry<T1, T2, T3, T4, T5, T6, T7, TResult>(System.Func<T1, T2, T3, T4, T5, T6, T7, TResult> function)`
- `public static System.Func<T1, System.Func<T2, System.Func<T3, System.Func<T4, System.Func<T5, System.Func<T6, System.Func<T7, System.Func<T8, TResult>>>>>>>> Curry<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(System.Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> function)`
- `public static System.Func<T1, System.Action<T2>> Curry<T1, T2>(System.Action<T1, T2> action)`
- `public static System.Func<T1, System.Func<T2, System.Action<T3>>> Curry<T1, T2, T3>(System.Action<T1, T2, T3> action)`
- `public static System.Func<T1, System.Func<T2, System.Func<T3, System.Action<T4>>>> Curry<T1, T2, T3, T4>(System.Action<T1, T2, T3, T4> action)`
- `public static System.Func<T1, System.Func<T2, System.Func<T3, System.Func<T4, System.Action<T5>>>>> Curry<T1, T2, T3, T4, T5>(System.Action<T1, T2, T3, T4, T5> action)`
- `public static System.Func<T1, System.Func<T2, System.Func<T3, System.Func<T4, System.Func<T5, System.Action<T6>>>>>> Curry<T1, T2, T3, T4, T5, T6>(System.Action<T1, T2, T3, T4, T5, T6> action)`
- `public static System.Func<T1, System.Func<T2, System.Func<T3, System.Func<T4, System.Func<T5, System.Func<T6, System.Action<T7>>>>>>> Curry<T1, T2, T3, T4, T5, T6, T7>(System.Action<T1, T2, T3, T4, T5, T6, T7> action)`
- `public static System.Func<T1, System.Func<T2, System.Func<T3, System.Func<T4, System.Func<T5, System.Func<T6, System.Func<T7, System.Action<T8>>>>>>>> Curry<T1, T2, T3, T4, T5, T6, T7, T8>(System.Action<T1, T2, T3, T4, T5, T6, T7, T8> action)`
- `public static System.Boolean False()`
- `public static System.Boolean False<T1>(T1? ω1)`
- `public static System.Boolean False<T1, T2>(T1? ω1, T2? ω2)`
- `public static System.Boolean False<T1, T2, T3>(T1? ω1, T2? ω2, T3? ω3)`
- `public static System.Boolean False<T1, T2, T3, T4>(T1? ω1, T2? ω2, T3? ω3, T4? ω4)`
- `public static System.Func<T2, T1, TResult> Flip<T1, T2, TResult>(System.Func<T1, T2, TResult> function)`
- `public static System.Func<T2, T1, T3, TResult> Flip<T1, T2, T3, TResult>(System.Func<T1, T2, T3, TResult> function)`
- `public static System.Func<T2, T1, T3, T4, TResult> Flip<T1, T2, T3, T4, TResult>(System.Func<T1, T2, T3, T4, TResult> function)`
- `public static System.Func<T2, T1, T3, T4, T5, TResult> Flip<T1, T2, T3, T4, T5, TResult>(System.Func<T1, T2, T3, T4, T5, TResult> function)`
- `public static System.Func<T2, T1, T3, T4, T5, T6, TResult> Flip<T1, T2, T3, T4, T5, T6, TResult>(System.Func<T1, T2, T3, T4, T5, T6, TResult> function)`
- `public static System.Func<T2, T1, T3, T4, T5, T6, T7, TResult> Flip<T1, T2, T3, T4, T5, T6, T7, TResult>(System.Func<T1, T2, T3, T4, T5, T6, T7, TResult> function)`
- `public static System.Func<T2, T1, T3, T4, T5, T6, T7, T8, TResult> Flip<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(System.Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> function)`
- `public static System.Action<T2, T1> Flip<T1, T2>(System.Action<T1, T2> function)`
- `public static System.Action<T2, T1, T3> Flip<T1, T2, T3>(System.Action<T1, T2, T3> action)`
- `public static System.Action<T2, T1, T3, T4> Flip<T1, T2, T3, T4>(System.Action<T1, T2, T3, T4> action)`
- `public static System.Action<T2, T1, T3, T4, T5> Flip<T1, T2, T3, T4, T5>(System.Action<T1, T2, T3, T4, T5> action)`
- `public static System.Action<T2, T1, T3, T4, T5, T6> Flip<T1, T2, T3, T4, T5, T6>(System.Action<T1, T2, T3, T4, T5, T6> action)`
- `public static System.Action<T2, T1, T3, T4, T5, T6, T7> Flip<T1, T2, T3, T4, T5, T6, T7>(System.Action<T1, T2, T3, T4, T5, T6, T7> action)`
- `public static System.Action<T2, T1, T3, T4, T5, T6, T7, T8> Flip<T1, T2, T3, T4, T5, T6, T7, T8>(System.Action<T1, T2, T3, T4, T5, T6, T7, T8> action)`
- `public static T Fn<T>(T? value)`
- `public static T Identity<T>(T? value)`
- `public static System.Void NoOperation()`
- `public static System.Void NoOperation<T1>(T1? ω1)`
- `public static System.Void NoOperation<T1, T2>(T1? ω1, T2? ω2)`
- `public static System.Void NoOperation<T1, T2, T3>(T1? ω1, T2? ω2, T3? ω3)`
- `public static System.Void NoOperation<T1, T2, T3, T4>(T1? ω1, T2? ω2, T3? ω3, T4? ω4)`
- `public static System.Void NoOperation<T1, T2, T3, T4, T5>(T1? ω1, T2? ω2, T3? ω3, T4? ω4, T5? ω5)`
- `public static System.Void NoOperation<T1, T2, T3, T4, T5, T6>(T1? ω1, T2? ω2, T3? ω3, T4? ω4, T5? ω5, T6? ω6)`
- `public static System.Void NoOperation<T1, T2, T3, T4, T5, T6, T7>(T1? ω1, T2? ω2, T3? ω3, T4? ω4, T5? ω5, T6? ω6, T7? ω7)`
- `public static System.Void NoOperation<T1, T2, T3, T4, T5, T6, T7, T8>(T1? ω1, T2? ω2, T3? ω3, T4? ω4, T5? ω5, T6? ω6, T7? ω7, T8? ω8)`
- `public static System.Func<T, System.Boolean> Not<T>(System.Func<T, System.Boolean> predicate)`
- `public static TResult Retry<TResult>(System.Func<Funcky.Monads.Option<TResult>> producer)`
- `public static Funcky.Monads.Option<TResult> Retry<TResult>(System.Func<Funcky.Monads.Option<TResult>> producer, Funcky.RetryPolicies.IRetryPolicy retryPolicy)`
- `public static TResult Retry<TResult>(System.Func<TResult> producer, System.Func<System.Exception, System.Boolean> shouldRetry, Funcky.RetryPolicies.IRetryPolicy retryPolicy)`
- `public static System.Void Retry(System.Action action, System.Func<System.Exception, System.Boolean> shouldRetry, Funcky.RetryPolicies.IRetryPolicy retryPolicy)`
- `public static System.Boolean True()`
- `public static System.Boolean True<T1>(T1? ω1)`
- `public static System.Boolean True<T1, T2>(T1? ω1, T2? ω2)`
- `public static System.Boolean True<T1, T2, T3>(T1? ω1, T2? ω2, T3? ω3)`
- `public static System.Boolean True<T1, T2, T3, T4>(T1? ω1, T2? ω2, T3? ω3, T4? ω4)`
- `public static System.Func<T1, T2, TResult> Uncurry<T1, T2, TResult>(System.Func<T1, System.Func<T2, TResult>> function)`
- `public static System.Func<T1, T2, T3, TResult> Uncurry<T1, T2, T3, TResult>(System.Func<T1, System.Func<T2, System.Func<T3, TResult>>> function)`
- `public static System.Func<T1, T2, T3, T4, TResult> Uncurry<T1, T2, T3, T4, TResult>(System.Func<T1, System.Func<T2, System.Func<T3, System.Func<T4, TResult>>>> function)`
- `public static System.Func<T1, T2, T3, T4, T5, TResult> Uncurry<T1, T2, T3, T4, T5, TResult>(System.Func<T1, System.Func<T2, System.Func<T3, System.Func<T4, System.Func<T5, TResult>>>>> function)`
- `public static System.Func<T1, T2, T3, T4, T5, T6, TResult> Uncurry<T1, T2, T3, T4, T5, T6, TResult>(System.Func<T1, System.Func<T2, System.Func<T3, System.Func<T4, System.Func<T5, System.Func<T6, TResult>>>>>> function)`
- `public static System.Func<T1, T2, T3, T4, T5, T6, T7, TResult> Uncurry<T1, T2, T3, T4, T5, T6, T7, TResult>(System.Func<T1, System.Func<T2, System.Func<T3, System.Func<T4, System.Func<T5, System.Func<T6, System.Func<T7, TResult>>>>>>> function)`
- `public static System.Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> Uncurry<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(System.Func<T1, System.Func<T2, System.Func<T3, System.Func<T4, System.Func<T5, System.Func<T6, System.Func<T7, System.Func<T8, TResult>>>>>>>> function)`
- `public static System.Action<T1, T2> Uncurry<T1, T2>(System.Func<T1, System.Action<T2>> action)`
- `public static System.Action<T1, T2, T3> Uncurry<T1, T2, T3>(System.Func<T1, System.Func<T2, System.Action<T3>>> action)`
- `public static System.Action<T1, T2, T3, T4> Uncurry<T1, T2, T3, T4>(System.Func<T1, System.Func<T2, System.Func<T3, System.Action<T4>>>> action)`
- `public static System.Action<T1, T2, T3, T4, T5> Uncurry<T1, T2, T3, T4, T5>(System.Func<T1, System.Func<T2, System.Func<T3, System.Func<T4, System.Action<T5>>>>> action)`
- `public static System.Action<T1, T2, T3, T4, T5, T6> Uncurry<T1, T2, T3, T4, T5, T6>(System.Func<T1, System.Func<T2, System.Func<T3, System.Func<T4, System.Func<T5, System.Action<T6>>>>>> action)`
- `public static System.Action<T1, T2, T3, T4, T5, T6, T7> Uncurry<T1, T2, T3, T4, T5, T6, T7>(System.Func<T1, System.Func<T2, System.Func<T3, System.Func<T4, System.Func<T5, System.Func<T6, System.Action<T7>>>>>>> action)`
- `public static System.Action<T1, T2, T3, T4, T5, T6, T7, T8> Uncurry<T1, T2, T3, T4, T5, T6, T7, T8>(System.Func<T1, System.Func<T2, System.Func<T3, System.Func<T4, System.Func<T5, System.Func<T6, System.Func<T7, System.Action<T8>>>>>>>> action)`
- `public static System.Action UnitToAction(System.Func<Funcky.Unit> unitFunction)`
- `public static System.Action<T1> UnitToAction<T1>(System.Func<T1, Funcky.Unit> unitFunction)`
- `public static System.Action<T1, T2> UnitToAction<T1, T2>(System.Func<T1, T2, Funcky.Unit> unitFunction)`
- `public static System.Action<T1, T2, T3> UnitToAction<T1, T2, T3>(System.Func<T1, T2, T3, Funcky.Unit> unitFunction)`
- `public static System.Action<T1, T2, T3, T4> UnitToAction<T1, T2, T3, T4>(System.Func<T1, T2, T3, T4, Funcky.Unit> unitFunction)`
- `public static System.Action<T1, T2, T3, T4, T5> UnitToAction<T1, T2, T3, T4, T5>(System.Func<T1, T2, T3, T4, T5, Funcky.Unit> unitFunction)`
- `public static System.Action<T1, T2, T3, T4, T5, T6> UnitToAction<T1, T2, T3, T4, T5, T6>(System.Func<T1, T2, T3, T4, T5, T6, Funcky.Unit> unitFunction)`
- `public static System.Action<T1, T2, T3, T4, T5, T6, T7> UnitToAction<T1, T2, T3, T4, T5, T6, T7>(System.Func<T1, T2, T3, T4, T5, T6, T7, Funcky.Unit> unitFunction)`
- `public static System.Action<T1, T2, T3, T4, T5, T6, T7, T8> UnitToAction<T1, T2, T3, T4, T5, T6, T7, T8>(System.Func<T1, T2, T3, T4, T5, T6, T7, T8, Funcky.Unit> unitFunction)`

### IAsyncBuffer`1<T> (interface) : System.Collections.Generic.IAsyncEnumerable<T>, System.IAsyncDisposable


### IBuffer`1<T> (interface) : System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable, System.IDisposable


### RequireClass`1<T> (class [sealed])

- `where T : class`

- `public RequireClass`1()`

### RequireStruct`1<T> (class [sealed])

- `where T : struct`

- `public RequireStruct`1()`

### Sequence (class [static])

- `public static System.Collections.Generic.IEnumerable<TSource> Concat<TSource>(params System.Collections.Generic.IEnumerable<TSource>[] sources)`
- `public static System.Collections.Generic.IEnumerable<TSource> Concat<TSource>(System.Collections.Generic.IEnumerable<System.Collections.Generic.IEnumerable<TSource>> sources)`
- `public static System.Collections.Generic.IEnumerable<TResult> Cycle<TResult>(TResult? element)`
- `public static System.Collections.Generic.IEnumerable<TSource> CycleMaterialized<TSource>(System.Collections.Generic.IReadOnlyCollection<TSource> source)`
- `public static Funcky.IBuffer<TSource> CycleRange<TSource>(System.Collections.Generic.IEnumerable<TSource> source)`
- `public static System.Collections.Generic.IEnumerable<TResult> FromNullable<TResult>(TResult? element)`
- `where TResult : class`
- `public static System.Collections.Generic.IEnumerable<TResult> FromNullable<TResult>(TResult?? element)`
- `where TResult : struct`
- `public static System.Collections.Generic.IEnumerable<TSource> RepeatMaterialized<TSource>(System.Collections.Generic.IReadOnlyCollection<TSource> source, System.Int32 count)`
- `public static Funcky.IBuffer<TSource> RepeatRange<TSource>(System.Collections.Generic.IEnumerable<TSource> source, System.Int32 count)`
- `public static System.Collections.Generic.IReadOnlyList<TResult> Return<TResult>(TResult? element)`
- `public static System.Collections.Generic.IReadOnlyList<TResult> Return<TResult>(params TResult[] elements)`
- `public static System.Collections.Generic.IEnumerable<TResult> Successors<TResult>(Funcky.Monads.Option<TResult> first, System.Func<TResult, Funcky.Monads.Option<TResult>> successor)`
- `public static System.Collections.Generic.IEnumerable<TResult> Successors<TResult>(TResult first, System.Func<TResult, Funcky.Monads.Option<TResult>> successor)`
- `public static System.Collections.Generic.IEnumerable<TResult> Successors<TResult>(Funcky.Monads.Option<TResult> first, System.Func<TResult, TResult> successor)`
- `public static System.Collections.Generic.IEnumerable<TResult> Successors<TResult>(TResult? first, System.Func<TResult, TResult> successor)`

### Unit (struct [readonly struct]) : System.IEquatable<Funcky.Unit>, System.IComparable<Funcky.Unit>

- `public static Funcky.Unit Value { get; }`
- `public System.Int32 CompareTo(Funcky.Unit other)`
- `public System.Boolean Equals(Funcky.Unit other)`
- `public System.Boolean Equals(System.Object? obj)`
- `public System.Int32 GetHashCode()`
- `public static System.Boolean op_Equality(Funcky.Unit left, Funcky.Unit right)`
- `public static System.Boolean op_GreaterThan(Funcky.Unit left, Funcky.Unit right)`
- `public static System.Boolean op_GreaterThanOrEqual(Funcky.Unit left, Funcky.Unit right)`
- `public static System.Boolean op_Inequality(Funcky.Unit left, Funcky.Unit right)`
- `public static System.Boolean op_LessThan(Funcky.Unit left, Funcky.Unit right)`
- `public static System.Boolean op_LessThanOrEqual(Funcky.Unit left, Funcky.Unit right)`

### UpCast`1<TResult> (class [static])

- `public static Funcky.Monads.Option<TResult> From<TItem>(Funcky.Monads.Option<TItem> option)`
- `where TItem : TResult`
- `public static Funcky.Monads.Either<TLeft, TResult> From<TLeft, TRight>(Funcky.Monads.Either<TLeft, TRight> either)`
- `where TRight : TResult`
- `public static Funcky.Monads.Result<TResult> From<TValidResult>(Funcky.Monads.Result<TValidResult> result)`
- `where TValidResult : TResult`
- `public static System.Lazy<TResult> From<T>(System.Lazy<T> lazy)`
- `where T : TResult`

## Funcky.Extensions

### ActionExtensions (class [static])

- `[ext] public static System.Action<TInput> Compose<TInput, TIntermediate>(this System.Action<TIntermediate> f, System.Func<TInput, TIntermediate> g)`
- `[ext] public static System.Action Compose<TIntermediate>(this System.Action<TIntermediate> f, System.Func<TIntermediate> g)`
- `[ext] public static System.Func<T1, System.Action<T2>> Curry<T1, T2>(this System.Action<T1, T2> action)`
- `[ext] public static System.Func<T1, System.Func<T2, System.Action<T3>>> Curry<T1, T2, T3>(this System.Action<T1, T2, T3> action)`
- `[ext] public static System.Func<T1, System.Func<T2, System.Func<T3, System.Action<T4>>>> Curry<T1, T2, T3, T4>(this System.Action<T1, T2, T3, T4> action)`
- `[ext] public static System.Func<T1, System.Func<T2, System.Func<T3, System.Func<T4, System.Action<T5>>>>> Curry<T1, T2, T3, T4, T5>(this System.Action<T1, T2, T3, T4, T5> action)`
- `[ext] public static System.Func<T1, System.Func<T2, System.Func<T3, System.Func<T4, System.Func<T5, System.Action<T6>>>>>> Curry<T1, T2, T3, T4, T5, T6>(this System.Action<T1, T2, T3, T4, T5, T6> action)`
- `[ext] public static System.Func<T1, System.Func<T2, System.Func<T3, System.Func<T4, System.Func<T5, System.Func<T6, System.Action<T7>>>>>>> Curry<T1, T2, T3, T4, T5, T6, T7>(this System.Action<T1, T2, T3, T4, T5, T6, T7> action)`
- `[ext] public static System.Func<T1, System.Func<T2, System.Func<T3, System.Func<T4, System.Func<T5, System.Func<T6, System.Func<T7, System.Action<T8>>>>>>>> Curry<T1, T2, T3, T4, T5, T6, T7, T8>(this System.Action<T1, T2, T3, T4, T5, T6, T7, T8> action)`
- `[ext] public static System.Action<T2, T1> Flip<T1, T2>(this System.Action<T1, T2> function)`
- `[ext] public static System.Action<T2, T1, T3> Flip<T1, T2, T3>(this System.Action<T1, T2, T3> action)`
- `[ext] public static System.Action<T2, T1, T3, T4> Flip<T1, T2, T3, T4>(this System.Action<T1, T2, T3, T4> action)`
- `[ext] public static System.Action<T2, T1, T3, T4, T5> Flip<T1, T2, T3, T4, T5>(this System.Action<T1, T2, T3, T4, T5> action)`
- `[ext] public static System.Action<T2, T1, T3, T4, T5, T6> Flip<T1, T2, T3, T4, T5, T6>(this System.Action<T1, T2, T3, T4, T5, T6> action)`
- `[ext] public static System.Action<T2, T1, T3, T4, T5, T6, T7> Flip<T1, T2, T3, T4, T5, T6, T7>(this System.Action<T1, T2, T3, T4, T5, T6, T7> action)`
- `[ext] public static System.Action<T2, T1, T3, T4, T5, T6, T7, T8> Flip<T1, T2, T3, T4, T5, T6, T7, T8>(this System.Action<T1, T2, T3, T4, T5, T6, T7, T8> action)`
- `[ext] public static System.Action<T1, T2> Uncurry<T1, T2>(this System.Func<T1, System.Action<T2>> action)`
- `[ext] public static System.Action<T1, T2, T3> Uncurry<T1, T2, T3>(this System.Func<T1, System.Func<T2, System.Action<T3>>> action)`
- `[ext] public static System.Action<T1, T2, T3, T4> Uncurry<T1, T2, T3, T4>(this System.Func<T1, System.Func<T2, System.Func<T3, System.Action<T4>>>> action)`
- `[ext] public static System.Action<T1, T2, T3, T4, T5> Uncurry<T1, T2, T3, T4, T5>(this System.Func<T1, System.Func<T2, System.Func<T3, System.Func<T4, System.Action<T5>>>>> action)`
- `[ext] public static System.Action<T1, T2, T3, T4, T5, T6> Uncurry<T1, T2, T3, T4, T5, T6>(this System.Func<T1, System.Func<T2, System.Func<T3, System.Func<T4, System.Func<T5, System.Action<T6>>>>>> action)`
- `[ext] public static System.Action<T1, T2, T3, T4, T5, T6, T7> Uncurry<T1, T2, T3, T4, T5, T6, T7>(this System.Func<T1, System.Func<T2, System.Func<T3, System.Func<T4, System.Func<T5, System.Func<T6, System.Action<T7>>>>>>> action)`
- `[ext] public static System.Action<T1, T2, T3, T4, T5, T6, T7, T8> Uncurry<T1, T2, T3, T4, T5, T6, T7, T8>(this System.Func<T1, System.Func<T2, System.Func<T3, System.Func<T4, System.Func<T5, System.Func<T6, System.Func<T7, System.Action<T8>>>>>>>> action)`

### AsyncEnumerableExtensions (class [static])

- `[ext] public static System.Collections.Generic.IAsyncEnumerable<System.Linq.IGrouping<TKey, TSource>> AdjacentGroupBy<TSource, TKey>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, TKey> keySelector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<System.Linq.IGrouping<TKey, TSource>> AdjacentGroupBy<TSource, TKey>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<System.Linq.IGrouping<TKey, TElement>> AdjacentGroupBy<TSource, TKey, TElement>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> AdjacentGroupBy<TSource, TKey, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TKey, System.Collections.Generic.IEnumerable<TSource>, TResult> resultSelector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<System.Linq.IGrouping<TKey, TElement>> AdjacentGroupBy<TSource, TKey, TElement>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> AdjacentGroupBy<TSource, TKey, TElement, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector, System.Func<TKey, System.Collections.Generic.IEnumerable<TElement>, TResult> resultSelector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> AdjacentGroupBy<TSource, TKey, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TKey, System.Collections.Generic.IEnumerable<TSource>, TResult> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> AdjacentGroupBy<TSource, TKey, TElement, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector, System.Func<TKey, System.Collections.Immutable.IImmutableList<TElement>, TResult> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<System.Linq.IGrouping<TKey, TSource>> AdjacentGroupByAwait<TSource, TKey>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.Tasks.ValueTask<TKey>> keySelector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<System.Linq.IGrouping<TKey, TSource>> AdjacentGroupByAwait<TSource, TKey>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.Tasks.ValueTask<TKey>> keySelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<System.Linq.IGrouping<TKey, TElement>> AdjacentGroupByAwait<TSource, TKey, TElement>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.Tasks.ValueTask<TKey>> keySelector, System.Func<TSource, System.Threading.Tasks.ValueTask<TElement>> elementSelector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> AdjacentGroupByAwait<TSource, TKey, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.Tasks.ValueTask<TKey>> keySelector, System.Func<TKey, System.Collections.Generic.IEnumerable<TSource>, System.Threading.Tasks.ValueTask<TResult>> resultSelector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<System.Linq.IGrouping<TKey, TElement>> AdjacentGroupByAwait<TSource, TKey, TElement>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.Tasks.ValueTask<TKey>> keySelector, System.Func<TSource, System.Threading.Tasks.ValueTask<TElement>> elementSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> AdjacentGroupByAwait<TSource, TKey, TElement, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.Tasks.ValueTask<TKey>> keySelector, System.Func<TSource, System.Threading.Tasks.ValueTask<TElement>> elementSelector, System.Func<TKey, System.Collections.Generic.IEnumerable<TElement>, System.Threading.Tasks.ValueTask<TResult>> resultSelector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> AdjacentGroupByAwait<TSource, TKey, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.Tasks.ValueTask<TKey>> keySelector, System.Func<TKey, System.Collections.Generic.IEnumerable<TSource>, System.Threading.Tasks.ValueTask<TResult>> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> AdjacentGroupByAwait<TSource, TKey, TElement, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.Tasks.ValueTask<TKey>> keySelector, System.Func<TSource, System.Threading.Tasks.ValueTask<TElement>> elementSelector, System.Func<TKey, System.Collections.Immutable.IImmutableList<TElement>, System.Threading.Tasks.ValueTask<TResult>> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<System.Linq.IGrouping<TKey, TSource>> AdjacentGroupByAwaitWithCancellation<TSource, TKey>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TKey>> keySelector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<System.Linq.IGrouping<TKey, TSource>> AdjacentGroupByAwaitWithCancellation<TSource, TKey>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TKey>> keySelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<System.Linq.IGrouping<TKey, TElement>> AdjacentGroupByAwaitWithCancellation<TSource, TKey, TElement>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TKey>> keySelector, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TElement>> elementSelector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> AdjacentGroupByAwaitWithCancellation<TSource, TKey, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TKey>> keySelector, System.Func<TKey, System.Collections.Generic.IEnumerable<TSource>, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TResult>> resultSelector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<System.Linq.IGrouping<TKey, TElement>> AdjacentGroupByAwaitWithCancellation<TSource, TKey, TElement>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TKey>> keySelector, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TElement>> elementSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> AdjacentGroupByAwaitWithCancellation<TSource, TKey, TElement, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TKey>> keySelector, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TElement>> elementSelector, System.Func<TKey, System.Collections.Generic.IEnumerable<TElement>, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TResult>> resultSelector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> AdjacentGroupByAwaitWithCancellation<TSource, TKey, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TKey>> keySelector, System.Func<TKey, System.Collections.Generic.IEnumerable<TSource>, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TResult>> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> AdjacentGroupByAwaitWithCancellation<TSource, TKey, TElement, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TKey>> keySelector, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TElement>> elementSelector, System.Func<TKey, System.Collections.Immutable.IImmutableList<TElement>, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TResult>> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> AnyOrElse<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Collections.Generic.IAsyncEnumerable<TSource> fallback)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> AnyOrElse<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<System.Collections.Generic.IAsyncEnumerable<TSource>> fallback)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Double>> AverageOrNoneAsync(this System.Collections.Generic.IAsyncEnumerable<System.Int32> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Double>> AverageOrNoneAsync(this System.Collections.Generic.IAsyncEnumerable<Funcky.Monads.Option<System.Int32>> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Double>> AverageOrNoneAsync(this System.Collections.Generic.IAsyncEnumerable<System.Int64> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Double>> AverageOrNoneAsync(this System.Collections.Generic.IAsyncEnumerable<Funcky.Monads.Option<System.Int64>> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Single>> AverageOrNoneAsync(this System.Collections.Generic.IAsyncEnumerable<System.Single> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Single>> AverageOrNoneAsync(this System.Collections.Generic.IAsyncEnumerable<Funcky.Monads.Option<System.Single>> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Double>> AverageOrNoneAsync(this System.Collections.Generic.IAsyncEnumerable<System.Double> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Double>> AverageOrNoneAsync(this System.Collections.Generic.IAsyncEnumerable<Funcky.Monads.Option<System.Double>> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Decimal>> AverageOrNoneAsync(this System.Collections.Generic.IAsyncEnumerable<System.Decimal> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Decimal>> AverageOrNoneAsync(this System.Collections.Generic.IAsyncEnumerable<Funcky.Monads.Option<System.Decimal>> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Double>> AverageOrNoneAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Int32> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Double>> AverageOrNoneAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, Funcky.Monads.Option<System.Int32>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Double>> AverageOrNoneAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Int64> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Double>> AverageOrNoneAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, Funcky.Monads.Option<System.Int64>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Single>> AverageOrNoneAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Single> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Single>> AverageOrNoneAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, Funcky.Monads.Option<System.Single>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Double>> AverageOrNoneAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Double> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Double>> AverageOrNoneAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, Funcky.Monads.Option<System.Double>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Decimal>> AverageOrNoneAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Decimal> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Decimal>> AverageOrNoneAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, Funcky.Monads.Option<System.Decimal>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Double>> AverageOrNoneAwaitAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.Tasks.ValueTask<System.Int32>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Double>> AverageOrNoneAwaitAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Int32>>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Double>> AverageOrNoneAwaitAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.Tasks.ValueTask<System.Int64>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Double>> AverageOrNoneAwaitAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Int64>>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Single>> AverageOrNoneAwaitAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.Tasks.ValueTask<System.Single>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Single>> AverageOrNoneAwaitAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Single>>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Double>> AverageOrNoneAwaitAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.Tasks.ValueTask<System.Double>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Double>> AverageOrNoneAwaitAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Double>>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Decimal>> AverageOrNoneAwaitAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.Tasks.ValueTask<System.Decimal>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Decimal>> AverageOrNoneAwaitAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Decimal>>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Double>> AverageOrNoneAwaitWithCancellationAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.Int32>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Double>> AverageOrNoneAwaitWithCancellationAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Int32>>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Double>> AverageOrNoneAwaitWithCancellationAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.Int64>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Double>> AverageOrNoneAwaitWithCancellationAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Int64>>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Single>> AverageOrNoneAwaitWithCancellationAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.Single>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Single>> AverageOrNoneAwaitWithCancellationAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Single>>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Double>> AverageOrNoneAwaitWithCancellationAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.Double>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Double>> AverageOrNoneAwaitWithCancellationAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Double>>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Decimal>> AverageOrNoneAwaitWithCancellationAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.Decimal>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Decimal>> AverageOrNoneAwaitWithCancellationAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Decimal>>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.String> ConcatToStringAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TSource>> ElementAtOrNoneAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Int32 index, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TSource>> ElementAtOrNoneAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Index index, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TAccumulate> ExclusiveScan<TSource, TAccumulate>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, TAccumulate? seed, System.Func<TAccumulate, TSource, TAccumulate> accumulator)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TAccumulate> ExclusiveScanAwait<TSource, TAccumulate>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, TAccumulate? seed, System.Func<TAccumulate, TSource, System.Threading.Tasks.ValueTask<TAccumulate>> accumulator)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TAccumulate> ExclusiveScanAwaitWithCancellation<TSource, TAccumulate>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, TAccumulate? seed, System.Func<TAccumulate, TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TAccumulate>> accumulator)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TSource>> FirstOrNoneAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TSource>> FirstOrNoneAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TSource>> FirstOrNoneAwaitAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.Tasks.ValueTask<System.Boolean>> predicate, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TSource>> FirstOrNoneAwaitWithCancellationAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.Boolean>> predicate, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TAccumulate> InclusiveScan<TSource, TAccumulate>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, TAccumulate? seed, System.Func<TAccumulate, TSource, TAccumulate> accumulator)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TAccumulate> InclusiveScanAwait<TSource, TAccumulate>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, TAccumulate? seed, System.Func<TAccumulate, TSource, System.Threading.Tasks.ValueTask<TAccumulate>> accumulator)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TAccumulate> InclusiveScanAwaitWithCancellation<TSource, TAccumulate>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, TAccumulate? seed, System.Func<TAccumulate, TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TAccumulate>> accumulator)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> Inspect<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Action<TSource> inspector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> InspectAwait<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.Tasks.ValueTask> inspector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> InspectAwaitWithCancellation<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask> inspector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> InspectEmpty<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Action inspector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> Interleave<TSource>(this System.Collections.Generic.IEnumerable<System.Collections.Generic.IAsyncEnumerable<TSource>> source)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> Interleave<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, params System.Collections.Generic.IAsyncEnumerable<TSource>[] otherSources)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> Intersperse<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, TSource? element)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.String> JoinToStringAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Char separator, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.String> JoinToStringAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.String separator, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TSource>> LastOrNoneAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TSource>> LastOrNoneAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TSource>> LastOrNoneAwaitAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.Tasks.ValueTask<System.Boolean>> predicate, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TSource>> LastOrNoneAwaitWithCancellationAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.Boolean>> predicate, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Collections.Generic.IReadOnlyCollection<TSource>> MaterializeAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Collections.Generic.IReadOnlyCollection<TSource>> MaterializeAsync<TSource, TMaterialization>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<System.Collections.Generic.IAsyncEnumerable<TSource>, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TMaterialization>> materializer, System.Threading.CancellationToken cancellationToken)`
- `where TMaterialization : System.Collections.Generic.IReadOnlyCollection<TSource>`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TSource>> MaxOrNoneAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TSource>> MaxOrNoneAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<Funcky.Monads.Option<TSource>> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TResult>> MaxOrNoneAsync<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, TResult> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TResult>> MaxOrNoneAsync<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, Funcky.Monads.Option<TResult>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TResult>> MaxOrNoneAwaitAsync<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.Tasks.ValueTask<TResult>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TResult>> MaxOrNoneAwaitAsync<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TResult>>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TResult>> MaxOrNoneAwaitWithCancellationAsync<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TResult>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TResult>> MaxOrNoneAwaitWithCancellationAsync<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TResult>>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static Funcky.IAsyncBuffer<TSource> Memoize<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> Merge<TSource>(this System.Collections.Generic.IEnumerable<System.Collections.Generic.IAsyncEnumerable<TSource>> sources, Funcky.Monads.Option<System.Collections.Generic.IComparer<TSource>> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> Merge<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source1, System.Collections.Generic.IAsyncEnumerable<TSource> source2, Funcky.Monads.Option<System.Collections.Generic.IComparer<TSource>> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> Merge<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source1, System.Collections.Generic.IAsyncEnumerable<TSource> source2, System.Collections.Generic.IAsyncEnumerable<TSource> source3, Funcky.Monads.Option<System.Collections.Generic.IComparer<TSource>> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> Merge<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source1, System.Collections.Generic.IAsyncEnumerable<TSource> source2, System.Collections.Generic.IAsyncEnumerable<TSource> source3, System.Collections.Generic.IAsyncEnumerable<TSource> source4, Funcky.Monads.Option<System.Collections.Generic.IComparer<TSource>> comparer)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TSource>> MinOrNoneAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TSource>> MinOrNoneAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<Funcky.Monads.Option<TSource>> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TResult>> MinOrNoneAsync<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, TResult> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TResult>> MinOrNoneAsync<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, Funcky.Monads.Option<TResult>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TResult>> MinOrNoneAwaitAsync<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.Tasks.ValueTask<TResult>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TResult>> MinOrNoneAwaitAsync<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TResult>>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TResult>> MinOrNoneAwaitWithCancellationAsync<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TResult>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TResult>> MinOrNoneAwaitWithCancellationAsync<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TResult>>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Boolean> NoneAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Boolean> NoneAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Boolean> NoneAwaitAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.Tasks.ValueTask<System.Boolean>> predicate, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Boolean> NoneAwaitWithCancellationAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.Boolean>> predicate, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<System.ValueTuple<TSource, TSource>> Pairwise<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> Pairwise<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, TSource, TResult> resultSelector)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Extensions.EitherPartitions<TLeft, TRight>> PartitionAsync<TLeft, TRight>(this System.Collections.Generic.IAsyncEnumerable<Funcky.Monads.Either<TLeft, TRight>> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Extensions.ResultPartitions<TValidResult>> PartitionAsync<TValidResult>(this System.Collections.Generic.IAsyncEnumerable<Funcky.Monads.Result<TValidResult>> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Extensions.Partitions<TSource>> PartitionAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TResult> PartitionAsync<TLeft, TRight, TResult>(this System.Collections.Generic.IAsyncEnumerable<Funcky.Monads.Either<TLeft, TRight>> source, System.Func<System.Collections.Generic.IReadOnlyList<TLeft>, System.Collections.Generic.IReadOnlyList<TRight>, TResult> resultSelector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Extensions.EitherPartitions<TLeft, TRight>> PartitionAsync<TSource, TLeft, TRight>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, Funcky.Monads.Either<TLeft, TRight>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TResult> PartitionAsync<TValidResult, TResult>(this System.Collections.Generic.IAsyncEnumerable<Funcky.Monads.Result<TValidResult>> source, System.Func<System.Collections.Generic.IReadOnlyList<System.Exception>, System.Collections.Generic.IReadOnlyList<TValidResult>, TResult> resultSelector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TResult> PartitionAsync<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate, System.Func<System.Collections.Generic.IReadOnlyList<TSource>, System.Collections.Generic.IReadOnlyList<TSource>, TResult> resultSelector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TResult> PartitionAsync<TSource, TLeft, TRight, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, Funcky.Monads.Either<TLeft, TRight>> selector, System.Func<System.Collections.Generic.IReadOnlyList<TLeft>, System.Collections.Generic.IReadOnlyList<TRight>, TResult> resultSelector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Extensions.Partitions<TSource>> PartitionAwaitAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.Tasks.ValueTask<System.Boolean>> predicate, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TResult> PartitionAwaitAsync<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.Tasks.ValueTask<System.Boolean>> predicate, System.Func<System.Collections.Generic.IReadOnlyList<TSource>, System.Collections.Generic.IReadOnlyList<TSource>, System.Threading.Tasks.ValueTask<TResult>> resultSelector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Extensions.Partitions<TSource>> PartitionAwaitWithCancellationAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.Boolean>> predicate, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<TResult> PartitionAwaitWithCancellationAsync<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.Boolean>> predicate, System.Func<System.Collections.Generic.IReadOnlyList<TSource>, System.Collections.Generic.IReadOnlyList<TSource>, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<TResult>> resultSelector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<System.Collections.Generic.IEnumerable<TSource>> PowerSet<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source)`
- `[ext] public static Funcky.Monads.Reader<TEnvironment, System.Collections.Generic.IAsyncEnumerable<TSource>> Sequence<TEnvironment, TSource>(this System.Collections.Generic.IAsyncEnumerable<Funcky.Monads.Reader<TEnvironment, TSource>> source)`
- `[ext] public static System.Lazy<System.Collections.Generic.IAsyncEnumerable<TSource>> Sequence<TSource>(this System.Collections.Generic.IAsyncEnumerable<System.Lazy<TSource>> source)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Either<TLeft, System.Collections.Generic.IReadOnlyList<TSource>>> SequenceAsync<TLeft, TSource>(this System.Collections.Generic.IAsyncEnumerable<Funcky.Monads.Either<TLeft, TSource>> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Collections.Generic.IReadOnlyList<TSource>>> SequenceAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<Funcky.Monads.Option<TSource>> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Result<System.Collections.Generic.IReadOnlyList<TSource>>> SequenceAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<Funcky.Monads.Result<TSource>> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Collections.Generic.IReadOnlyList<TSource>> ShuffleAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<System.Collections.Generic.IReadOnlyList<TSource>> ShuffleAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Random random, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TSource>> SingleOrNoneAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TSource>> SingleOrNoneAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TSource>> SingleOrNoneAwaitAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.Tasks.ValueTask<System.Boolean>> predicate, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TSource>> SingleOrNoneAwaitWithCancellationAsync<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<System.Boolean>> predicate, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<System.Collections.Generic.IReadOnlyList<TSource>> SlidingWindow<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Int32 width)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<System.Collections.Generic.IReadOnlyList<TSource>> Split<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, TSource? separator)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<System.Collections.Generic.IReadOnlyList<TSource>> Split<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, TSource? separator, System.Collections.Generic.IEqualityComparer<TSource> comparer)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> Split<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, TSource separator, System.Collections.Generic.IEqualityComparer<TSource> comparer, System.Func<System.Collections.Generic.IReadOnlyList<TSource>, TResult> resultSelector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> TakeEvery<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Int32 interval)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<System.Collections.Generic.IEnumerable<TSource>> Transpose<TSource>(this System.Collections.Generic.IEnumerable<System.Collections.Generic.IAsyncEnumerable<TSource>> source)`
- `[ext] public static Funcky.Monads.Reader<TEnvironment, System.Collections.Generic.IAsyncEnumerable<TResult>> Traverse<TSource, TEnvironment, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, Funcky.Monads.Reader<TEnvironment, TResult>> selector)`
- `[ext] public static System.Lazy<System.Collections.Generic.IAsyncEnumerable<T>> Traverse<TSource, T>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Lazy<T>> selector)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Either<TLeft, System.Collections.Generic.IReadOnlyList<TRight>>> TraverseAsync<TSource, TLeft, TRight>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, Funcky.Monads.Either<TLeft, TRight>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<System.Collections.Generic.IReadOnlyList<TItem>>> TraverseAsync<TSource, TItem>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, Funcky.Monads.Option<TItem>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Result<System.Collections.Generic.IReadOnlyList<TValidResult>>> TraverseAsync<TSource, TValidResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, Funcky.Monads.Result<TValidResult>> selector, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> WhereNotNull<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source)`
- `where TSource : class`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> WhereNotNull<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource?> source)`
- `where TSource : struct`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TSource> WhereSelect<TSource>(this System.Collections.Generic.IAsyncEnumerable<Funcky.Monads.Option<TSource>> source)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> WhereSelect<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, Funcky.Monads.Option<TResult>> selector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> WhereSelect<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Int32, Funcky.Monads.Option<TResult>> selector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> WhereSelectAwait<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TResult>>> selector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> WhereSelectAwait<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Int32, System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TResult>>> selector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> WhereSelectAwaitWithCancellation<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TResult>>> selector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> WhereSelectAwaitWithCancellation<TSource, TResult>(this System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Int32, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TResult>>> selector)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<Funcky.Extensions.ValueWithFirst<TSource>> WithFirst<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<Funcky.Extensions.ValueWithIndex<TSource>> WithIndex<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<Funcky.Extensions.ValueWithLast<TSource>> WithLast<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<Funcky.Extensions.ValueWithPrevious<TSource>> WithPrevious<TSource>(this System.Collections.Generic.IAsyncEnumerable<TSource> source)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<Funcky.EitherOrBoth<TLeft, TRight>> ZipLongest<TLeft, TRight>(this System.Collections.Generic.IAsyncEnumerable<TLeft> left, System.Collections.Generic.IAsyncEnumerable<TRight> right)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TResult> ZipLongest<TLeft, TRight, TResult>(this System.Collections.Generic.IAsyncEnumerable<TLeft> left, System.Collections.Generic.IAsyncEnumerable<TRight> right, System.Func<Funcky.EitherOrBoth<TLeft, TRight>, TResult> resultSelector)`

### DictionaryExtensions (class [static])

- `[ext] public static Funcky.Monads.Option<TValue> GetValueOrNone<TKey, TValue>(this System.Collections.Generic.IDictionary<TKey, TValue> dictionary, TKey? key)`
- `[ext] public static Funcky.Monads.Option<TValue> GetValueOrNone<TKey, TValue>(this System.Collections.Generic.IReadOnlyDictionary<TKey, TValue> dictionary, TKey? readOnlyKey)`
- `[ext] public static Funcky.Monads.Option<TValue> RemoveOrNone<TKey, TValue>(this System.Collections.Generic.IDictionary<TKey, TValue> dictionary, TKey? key)`

### EitherPartitions (class [static])

- `public static Funcky.Extensions.EitherPartitions<TLeft, TRight> Create<TLeft, TRight>(System.Collections.Generic.IReadOnlyList<TLeft> left, System.Collections.Generic.IReadOnlyList<TRight> right)`

### EitherPartitions`2<TLeft, TRight> (struct [readonly struct])

- `public EitherPartitions`2(System.Collections.Generic.IReadOnlyList<TLeft> left, System.Collections.Generic.IReadOnlyList<TRight> right)`
- `public System.Collections.Generic.IReadOnlyList<TLeft> Left { get; }`
- `public System.Collections.Generic.IReadOnlyList<TRight> Right { get; }`
- `public System.Void Deconstruct(out System.Collections.Generic.IReadOnlyList<TLeft>& left, out System.Collections.Generic.IReadOnlyList<TRight>& right)`

### EnumerableExtensions (class [static])

- `[ext] public static System.Collections.Generic.IEnumerable<System.Linq.IGrouping<TKey, TSource>> AdjacentGroupBy<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector)`
- `[ext] public static System.Collections.Generic.IEnumerable<System.Linq.IGrouping<TKey, TSource>> AdjacentGroupBy<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IEnumerable<System.Linq.IGrouping<TKey, TElement>> AdjacentGroupBy<TSource, TKey, TElement>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector)`
- `[ext] public static System.Collections.Generic.IEnumerable<TResult> AdjacentGroupBy<TSource, TKey, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TKey, System.Collections.Generic.IEnumerable<TSource>, TResult> resultSelector)`
- `[ext] public static System.Collections.Generic.IEnumerable<System.Linq.IGrouping<TKey, TElement>> AdjacentGroupBy<TSource, TKey, TElement>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IEnumerable<TResult> AdjacentGroupBy<TSource, TKey, TElement, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector, System.Func<TKey, System.Collections.Generic.IEnumerable<TElement>, TResult> resultSelector)`
- `[ext] public static System.Collections.Generic.IEnumerable<TResult> AdjacentGroupBy<TSource, TKey, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TKey, System.Collections.Generic.IEnumerable<TSource>, TResult> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IEnumerable<TResult> AdjacentGroupBy<TSource, TKey, TElement, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TElement> elementSelector, System.Func<TKey, System.Collections.Immutable.IImmutableList<TElement>, TResult> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> AnyOrElse<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Collections.Generic.IEnumerable<TSource> fallback)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> AnyOrElse<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<System.Collections.Generic.IEnumerable<TSource>> fallback)`
- `[ext] public static Funcky.Monads.Option<System.Double> AverageOrNone(this System.Collections.Generic.IEnumerable<System.Int32> source)`
- `[ext] public static Funcky.Monads.Option<System.Double> AverageOrNone(this System.Collections.Generic.IEnumerable<Funcky.Monads.Option<System.Int32>> source)`
- `[ext] public static Funcky.Monads.Option<System.Double> AverageOrNone(this System.Collections.Generic.IEnumerable<System.Int64> source)`
- `[ext] public static Funcky.Monads.Option<System.Double> AverageOrNone(this System.Collections.Generic.IEnumerable<Funcky.Monads.Option<System.Int64>> source)`
- `[ext] public static Funcky.Monads.Option<System.Single> AverageOrNone(this System.Collections.Generic.IEnumerable<System.Single> source)`
- `[ext] public static Funcky.Monads.Option<System.Single> AverageOrNone(this System.Collections.Generic.IEnumerable<Funcky.Monads.Option<System.Single>> source)`
- `[ext] public static Funcky.Monads.Option<System.Double> AverageOrNone(this System.Collections.Generic.IEnumerable<System.Double> source)`
- `[ext] public static Funcky.Monads.Option<System.Double> AverageOrNone(this System.Collections.Generic.IEnumerable<Funcky.Monads.Option<System.Double>> source)`
- `[ext] public static Funcky.Monads.Option<System.Decimal> AverageOrNone(this System.Collections.Generic.IEnumerable<System.Decimal> source)`
- `[ext] public static Funcky.Monads.Option<System.Decimal> AverageOrNone(this System.Collections.Generic.IEnumerable<Funcky.Monads.Option<System.Decimal>> source)`
- `[ext] public static Funcky.Monads.Option<System.Double> AverageOrNone<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Int32> selector)`
- `[ext] public static Funcky.Monads.Option<System.Double> AverageOrNone<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, Funcky.Monads.Option<System.Int32>> selector)`
- `[ext] public static Funcky.Monads.Option<System.Double> AverageOrNone<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Int64> selector)`
- `[ext] public static Funcky.Monads.Option<System.Double> AverageOrNone<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, Funcky.Monads.Option<System.Int64>> selector)`
- `[ext] public static Funcky.Monads.Option<System.Single> AverageOrNone<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Single> selector)`
- `[ext] public static Funcky.Monads.Option<System.Single> AverageOrNone<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, Funcky.Monads.Option<System.Single>> selector)`
- `[ext] public static Funcky.Monads.Option<System.Double> AverageOrNone<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Double> selector)`
- `[ext] public static Funcky.Monads.Option<System.Double> AverageOrNone<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, Funcky.Monads.Option<System.Double>> selector)`
- `[ext] public static Funcky.Monads.Option<System.Decimal> AverageOrNone<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Decimal> selector)`
- `[ext] public static Funcky.Monads.Option<System.Decimal> AverageOrNone<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, Funcky.Monads.Option<System.Decimal>> selector)`
- `public static System.Collections.Generic.IEnumerable<System.Collections.Generic.IReadOnlyList<TSource>> Chunk<TSource>(System.Collections.Generic.IEnumerable<TSource> source, System.Int32 size)`
- `[ext] public static System.Collections.Generic.IEnumerable<TResult> Chunk<TSource, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Int32 size, System.Func<System.Collections.Generic.IReadOnlyList<TSource>, TResult> resultSelector)`
- `[ext] public static System.String ConcatToString<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `[ext] public static Funcky.Monads.Option<TSource> ElementAtOrNone<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Int32 index)`
- `[ext] public static Funcky.Monads.Option<TSource> ElementAtOrNone<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Index index)`
- `[ext] public static System.Collections.Generic.IEnumerable<TAccumulate> ExclusiveScan<TSource, TAccumulate>(this System.Collections.Generic.IEnumerable<TSource> source, TAccumulate? seed, System.Func<TAccumulate, TSource, TAccumulate> accumulator)`
- `[ext] public static Funcky.Monads.Option<TSource> FirstOrNone<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `[ext] public static Funcky.Monads.Option<TSource> FirstOrNone<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate)`
- `[ext] public static System.Collections.Generic.IEnumerable<T> Flatten<T>(this System.Collections.Generic.IEnumerable<System.Collections.Generic.IEnumerable<T>> enumerable)`
- `[ext] public static Funcky.Unit ForEach<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Action<TSource> action)`
- `[ext] public static Funcky.Unit ForEach<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, Funcky.Unit> action)`
- `[ext] public static Funcky.Monads.Option<System.Int32> GetNonEnumeratedCountOrNone<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `[ext] public static System.Collections.Generic.IEnumerable<TAccumulate> InclusiveScan<TSource, TAccumulate>(this System.Collections.Generic.IEnumerable<TSource> source, TAccumulate? seed, System.Func<TAccumulate, TSource, TAccumulate> accumulator)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> Inspect<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Action<TSource> inspector)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> InspectEmpty<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Action inspector)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> Interleave<TSource>(this System.Collections.Generic.IEnumerable<System.Collections.Generic.IEnumerable<TSource>> source)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> Interleave<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, params System.Collections.Generic.IEnumerable<TSource>[] otherSources)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> Intersperse<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, TSource? element)`
- `[ext] public static System.String JoinToString<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Char separator)`
- `[ext] public static System.String JoinToString<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.String separator)`
- `[ext] public static System.String JoinToString(this System.Collections.Generic.IEnumerable<System.String> source, System.String separator)`
- `[ext] public static Funcky.Monads.Option<TSource> LastOrNone<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `[ext] public static Funcky.Monads.Option<TSource> LastOrNone<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate)`
- `[ext] public static System.Collections.Generic.IReadOnlyCollection<TSource> Materialize<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `[ext] public static System.Collections.Generic.IReadOnlyCollection<TSource> Materialize<TSource, TMaterialization>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<System.Collections.Generic.IEnumerable<TSource>, TMaterialization> materializer)`
- `where TMaterialization : System.Collections.Generic.IReadOnlyCollection<TSource>`
- `[ext] public static Funcky.Monads.Option<TSource> MaxByOrNone<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector)`
- `[ext] public static Funcky.Monads.Option<TSource> MaxByOrNone<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IComparer<TKey> comparer)`
- `[ext] public static Funcky.Monads.Option<TSource> MaxOrNone<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `[ext] public static Funcky.Monads.Option<TSource> MaxOrNone<TSource>(this System.Collections.Generic.IEnumerable<Funcky.Monads.Option<TSource>> source)`
- `[ext] public static Funcky.Monads.Option<TResult> MaxOrNone<TSource, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TResult> selector)`
- `[ext] public static Funcky.Monads.Option<TResult> MaxOrNone<TSource, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, Funcky.Monads.Option<TResult>> selector)`
- `[ext] public static Funcky.IBuffer<TSource> Memoize<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> Merge<TSource>(this System.Collections.Generic.IEnumerable<System.Collections.Generic.IEnumerable<TSource>> sources, Funcky.Monads.Option<System.Collections.Generic.IComparer<TSource>> comparer)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> Merge<TSource>(this System.Collections.Generic.IEnumerable<TSource> source1, System.Collections.Generic.IEnumerable<TSource> source2, Funcky.Monads.Option<System.Collections.Generic.IComparer<TSource>> comparer)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> Merge<TSource>(this System.Collections.Generic.IEnumerable<TSource> source1, System.Collections.Generic.IEnumerable<TSource> source2, System.Collections.Generic.IEnumerable<TSource> source3, Funcky.Monads.Option<System.Collections.Generic.IComparer<TSource>> comparer)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> Merge<TSource>(this System.Collections.Generic.IEnumerable<TSource> source1, System.Collections.Generic.IEnumerable<TSource> source2, System.Collections.Generic.IEnumerable<TSource> source3, System.Collections.Generic.IEnumerable<TSource> source4, Funcky.Monads.Option<System.Collections.Generic.IComparer<TSource>> comparer)`
- `[ext] public static Funcky.Monads.Option<TSource> MinByOrNone<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector)`
- `[ext] public static Funcky.Monads.Option<TSource> MinByOrNone<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IComparer<TKey> comparer)`
- `[ext] public static Funcky.Monads.Option<TSource> MinOrNone<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `[ext] public static Funcky.Monads.Option<TSource> MinOrNone<TSource>(this System.Collections.Generic.IEnumerable<Funcky.Monads.Option<TSource>> source)`
- `[ext] public static Funcky.Monads.Option<TResult> MinOrNone<TSource, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TResult> selector)`
- `[ext] public static Funcky.Monads.Option<TResult> MinOrNone<TSource, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, Funcky.Monads.Option<TResult>> selector)`
- `[ext] public static System.Boolean None<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `[ext] public static System.Boolean None<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate)`
- `[ext] public static System.Collections.Generic.IEnumerable<System.ValueTuple<TSource, TSource>> Pairwise<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `[ext] public static System.Collections.Generic.IEnumerable<TResult> Pairwise<TSource, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TSource, TResult> resultSelector)`
- `[ext] public static Funcky.Extensions.EitherPartitions<TLeft, TRight> Partition<TLeft, TRight>(this System.Collections.Generic.IEnumerable<Funcky.Monads.Either<TLeft, TRight>> source)`
- `[ext] public static Funcky.Extensions.ResultPartitions<TValidResult> Partition<TValidResult>(this System.Collections.Generic.IEnumerable<Funcky.Monads.Result<TValidResult>> source)`
- `[ext] public static Funcky.Extensions.Partitions<TSource> Partition<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate)`
- `[ext] public static TResult Partition<TLeft, TRight, TResult>(this System.Collections.Generic.IEnumerable<Funcky.Monads.Either<TLeft, TRight>> source, System.Func<System.Collections.Generic.IReadOnlyList<TLeft>, System.Collections.Generic.IReadOnlyList<TRight>, TResult> resultSelector)`
- `[ext] public static Funcky.Extensions.EitherPartitions<TLeft, TRight> Partition<TSource, TLeft, TRight>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, Funcky.Monads.Either<TLeft, TRight>> selector)`
- `[ext] public static TResult Partition<TValidResult, TResult>(this System.Collections.Generic.IEnumerable<Funcky.Monads.Result<TValidResult>> source, System.Func<System.Collections.Generic.IReadOnlyList<System.Exception>, System.Collections.Generic.IReadOnlyList<TValidResult>, TResult> resultSelector)`
- `[ext] public static TResult Partition<TSource, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate, System.Func<System.Collections.Generic.IReadOnlyList<TSource>, System.Collections.Generic.IReadOnlyList<TSource>, TResult> resultSelector)`
- `[ext] public static TResult Partition<TSource, TLeft, TRight, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, Funcky.Monads.Either<TLeft, TRight>> selector, System.Func<System.Collections.Generic.IReadOnlyList<TLeft>, System.Collections.Generic.IReadOnlyList<TRight>, TResult> resultSelector)`
- `[ext] public static System.Collections.Generic.IEnumerable<System.Collections.Generic.IEnumerable<TSource>> PowerSet<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `[ext] public static Funcky.Monads.Either<TLeft, System.Collections.Generic.IReadOnlyList<TSource>> Sequence<TLeft, TSource>(this System.Collections.Generic.IEnumerable<Funcky.Monads.Either<TLeft, TSource>> source)`
- `[ext] public static Funcky.Monads.Option<System.Collections.Generic.IReadOnlyList<TSource>> Sequence<TSource>(this System.Collections.Generic.IEnumerable<Funcky.Monads.Option<TSource>> source)`
- `[ext] public static Funcky.Monads.Result<System.Collections.Generic.IReadOnlyList<TSource>> Sequence<TSource>(this System.Collections.Generic.IEnumerable<Funcky.Monads.Result<TSource>> source)`
- `[ext] public static Funcky.Monads.Reader<TEnvironment, System.Collections.Generic.IEnumerable<TSource>> Sequence<TEnvironment, TSource>(this System.Collections.Generic.IEnumerable<Funcky.Monads.Reader<TEnvironment, TSource>> sequence)`
- `[ext] public static System.Lazy<System.Collections.Generic.IEnumerable<TSource>> Sequence<TSource>(this System.Collections.Generic.IEnumerable<System.Lazy<TSource>> sequence)`
- `[ext] public static System.Collections.Generic.IReadOnlyList<TSource> Shuffle<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Random random)`
- `[ext] public static Funcky.Monads.Option<TSource> SingleOrNone<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `[ext] public static Funcky.Monads.Option<TSource> SingleOrNone<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Boolean> predicate)`
- `[ext] public static System.Collections.Generic.IEnumerable<System.Collections.Generic.IReadOnlyList<TSource>> SlidingWindow<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Int32 width)`
- `[ext] public static System.Collections.Generic.IEnumerable<System.Collections.Generic.IReadOnlyList<TSource>> Split<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, TSource? separator)`
- `[ext] public static System.Collections.Generic.IEnumerable<System.Collections.Generic.IReadOnlyList<TSource>> Split<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, TSource? separator, System.Collections.Generic.IEqualityComparer<TSource> comparer)`
- `[ext] public static System.Collections.Generic.IEnumerable<TResult> Split<TSource, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, TSource? separator, System.Collections.Generic.IEqualityComparer<TSource> comparer, System.Func<System.Collections.Generic.IReadOnlyList<TSource>, TResult> resultSelector)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> TakeEvery<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Int32 interval)`
- `[ext] public static System.Collections.Generic.IEnumerable<System.Collections.Generic.IReadOnlyList<TSource>> Transpose<TSource>(this System.Collections.Generic.IEnumerable<System.Collections.Generic.IEnumerable<TSource>> source)`
- `[ext] public static Funcky.Monads.Either<TLeft, System.Collections.Generic.IReadOnlyList<TRight>> Traverse<TSource, TLeft, TRight>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, Funcky.Monads.Either<TLeft, TRight>> selector)`
- `[ext] public static Funcky.Monads.Option<System.Collections.Generic.IReadOnlyList<TItem>> Traverse<TSource, TItem>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, Funcky.Monads.Option<TItem>> selector)`
- `[ext] public static Funcky.Monads.Result<System.Collections.Generic.IReadOnlyList<TValidResult>> Traverse<TSource, TValidResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, Funcky.Monads.Result<TValidResult>> selector)`
- `[ext] public static Funcky.Monads.Reader<TEnvironment, System.Collections.Generic.IEnumerable<TResult>> Traverse<TSource, TEnvironment, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, Funcky.Monads.Reader<TEnvironment, TResult>> selector)`
- `[ext] public static System.Lazy<System.Collections.Generic.IEnumerable<T>> Traverse<TSource, T>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Lazy<T>> selector)`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> WhereNotNull<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `where TSource : class`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> WhereNotNull<TSource>(this System.Collections.Generic.IEnumerable<TSource?> source)`
- `where TSource : struct`
- `[ext] public static System.Collections.Generic.IEnumerable<TSource> WhereSelect<TSource>(this System.Collections.Generic.IEnumerable<Funcky.Monads.Option<TSource>> source)`
- `[ext] public static System.Collections.Generic.IEnumerable<TResult> WhereSelect<TSource, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, Funcky.Monads.Option<TResult>> selector)`
- `[ext] public static System.Collections.Generic.IEnumerable<TResult> WhereSelect<TSource, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Int32, Funcky.Monads.Option<TResult>> selector)`
- `[ext] public static System.Collections.Generic.IEnumerable<Funcky.Extensions.ValueWithFirst<TSource>> WithFirst<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `[ext] public static System.Collections.Generic.IEnumerable<Funcky.Extensions.ValueWithIndex<TSource>> WithIndex<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `[ext] public static System.Collections.Generic.IEnumerable<Funcky.Extensions.ValueWithLast<TSource>> WithLast<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `[ext] public static System.Collections.Generic.IEnumerable<Funcky.Extensions.ValueWithPrevious<TSource>> WithPrevious<TSource>(this System.Collections.Generic.IEnumerable<TSource> source)`
- `[ext] public static System.Collections.Generic.IEnumerable<Funcky.EitherOrBoth<TLeft, TRight>> ZipLongest<TLeft, TRight>(this System.Collections.Generic.IEnumerable<TLeft> left, System.Collections.Generic.IEnumerable<TRight> right)`
- `[ext] public static System.Collections.Generic.IEnumerable<TResult> ZipLongest<TLeft, TRight, TResult>(this System.Collections.Generic.IEnumerable<TLeft> left, System.Collections.Generic.IEnumerable<TRight> right, System.Func<Funcky.EitherOrBoth<TLeft, TRight>, TResult> resultSelector)`

### EnumeratorExtensions (class [static])

- `[ext] public static Funcky.Monads.Option<T> MoveNextOrNone<T>(this System.Collections.Generic.IEnumerator<T> enumerator)`

### FuncExtensions (class [static])

- `[ext] public static System.Func<T1, TResult> Apply<T1, T2, TResult>(this System.Func<T1, T2, TResult> func, Funcky.Unit p1, T2? p2)`
- `[ext] public static System.Func<T2, TResult> Apply<T1, T2, TResult>(this System.Func<T1, T2, TResult> func, T1? p1, Funcky.Unit p2)`
- `[ext] public static System.Func<T1, TResult> Apply<T1, T2, T3, TResult>(this System.Func<T1, T2, T3, TResult> func, Funcky.Unit p1, T2? p2, T3? p3)`
- `[ext] public static System.Func<T2, TResult> Apply<T1, T2, T3, TResult>(this System.Func<T1, T2, T3, TResult> func, T1? p1, Funcky.Unit p2, T3? p3)`
- `[ext] public static System.Func<T1, T2, TResult> Apply<T1, T2, T3, TResult>(this System.Func<T1, T2, T3, TResult> func, Funcky.Unit p1, Funcky.Unit p2, T3? p3)`
- `[ext] public static System.Func<T3, TResult> Apply<T1, T2, T3, TResult>(this System.Func<T1, T2, T3, TResult> func, T1? p1, T2? p2, Funcky.Unit p3)`
- `[ext] public static System.Func<T1, T3, TResult> Apply<T1, T2, T3, TResult>(this System.Func<T1, T2, T3, TResult> func, Funcky.Unit p1, T2? p2, Funcky.Unit p3)`
- `[ext] public static System.Func<T2, T3, TResult> Apply<T1, T2, T3, TResult>(this System.Func<T1, T2, T3, TResult> func, T1? p1, Funcky.Unit p2, Funcky.Unit p3)`
- `[ext] public static System.Func<T1, TResult> Apply<T1, T2, T3, T4, TResult>(this System.Func<T1, T2, T3, T4, TResult> func, Funcky.Unit p1, T2? p2, T3? p3, T4? p4)`
- `[ext] public static System.Func<T2, TResult> Apply<T1, T2, T3, T4, TResult>(this System.Func<T1, T2, T3, T4, TResult> func, T1? p1, Funcky.Unit p2, T3? p3, T4? p4)`
- `[ext] public static System.Func<T1, T2, TResult> Apply<T1, T2, T3, T4, TResult>(this System.Func<T1, T2, T3, T4, TResult> func, Funcky.Unit p1, Funcky.Unit p2, T3? p3, T4? p4)`
- `[ext] public static System.Func<T3, TResult> Apply<T1, T2, T3, T4, TResult>(this System.Func<T1, T2, T3, T4, TResult> func, T1? p1, T2? p2, Funcky.Unit p3, T4? p4)`
- `[ext] public static System.Func<T1, T3, TResult> Apply<T1, T2, T3, T4, TResult>(this System.Func<T1, T2, T3, T4, TResult> func, Funcky.Unit p1, T2? p2, Funcky.Unit p3, T4? p4)`
- `[ext] public static System.Func<T2, T3, TResult> Apply<T1, T2, T3, T4, TResult>(this System.Func<T1, T2, T3, T4, TResult> func, T1? p1, Funcky.Unit p2, Funcky.Unit p3, T4? p4)`
- `[ext] public static System.Func<T1, T2, T3, TResult> Apply<T1, T2, T3, T4, TResult>(this System.Func<T1, T2, T3, T4, TResult> func, Funcky.Unit p1, Funcky.Unit p2, Funcky.Unit p3, T4? p4)`
- `[ext] public static System.Func<T4, TResult> Apply<T1, T2, T3, T4, TResult>(this System.Func<T1, T2, T3, T4, TResult> func, T1? p1, T2? p2, T3? p3, Funcky.Unit p4)`
- `[ext] public static System.Func<T1, T4, TResult> Apply<T1, T2, T3, T4, TResult>(this System.Func<T1, T2, T3, T4, TResult> func, Funcky.Unit p1, T2? p2, T3? p3, Funcky.Unit p4)`
- `[ext] public static System.Func<T2, T4, TResult> Apply<T1, T2, T3, T4, TResult>(this System.Func<T1, T2, T3, T4, TResult> func, T1? p1, Funcky.Unit p2, T3? p3, Funcky.Unit p4)`
- `[ext] public static System.Func<T1, T2, T4, TResult> Apply<T1, T2, T3, T4, TResult>(this System.Func<T1, T2, T3, T4, TResult> func, Funcky.Unit p1, Funcky.Unit p2, T3? p3, Funcky.Unit p4)`
- `[ext] public static System.Func<T3, T4, TResult> Apply<T1, T2, T3, T4, TResult>(this System.Func<T1, T2, T3, T4, TResult> func, T1? p1, T2? p2, Funcky.Unit p3, Funcky.Unit p4)`
- `[ext] public static System.Func<T1, T3, T4, TResult> Apply<T1, T2, T3, T4, TResult>(this System.Func<T1, T2, T3, T4, TResult> func, Funcky.Unit p1, T2? p2, Funcky.Unit p3, Funcky.Unit p4)`
- `[ext] public static System.Func<T2, T3, T4, TResult> Apply<T1, T2, T3, T4, TResult>(this System.Func<T1, T2, T3, T4, TResult> func, T1? p1, Funcky.Unit p2, Funcky.Unit p3, Funcky.Unit p4)`
- `[ext] public static System.Func<T1, TResult> Apply<T1, T2, T3, T4, T5, TResult>(this System.Func<T1, T2, T3, T4, T5, TResult> func, Funcky.Unit p1, T2? p2, T3? p3, T4? p4, T5? p5)`
- `[ext] public static System.Func<T2, TResult> Apply<T1, T2, T3, T4, T5, TResult>(this System.Func<T1, T2, T3, T4, T5, TResult> func, T1? p1, Funcky.Unit p2, T3? p3, T4? p4, T5? p5)`
- `[ext] public static System.Func<T1, T2, TResult> Apply<T1, T2, T3, T4, T5, TResult>(this System.Func<T1, T2, T3, T4, T5, TResult> func, Funcky.Unit p1, Funcky.Unit p2, T3? p3, T4? p4, T5? p5)`
- `[ext] public static System.Func<T3, TResult> Apply<T1, T2, T3, T4, T5, TResult>(this System.Func<T1, T2, T3, T4, T5, TResult> func, T1? p1, T2? p2, Funcky.Unit p3, T4? p4, T5? p5)`
- `[ext] public static System.Func<T1, T3, TResult> Apply<T1, T2, T3, T4, T5, TResult>(this System.Func<T1, T2, T3, T4, T5, TResult> func, Funcky.Unit p1, T2? p2, Funcky.Unit p3, T4? p4, T5? p5)`
- `[ext] public static System.Func<T2, T3, TResult> Apply<T1, T2, T3, T4, T5, TResult>(this System.Func<T1, T2, T3, T4, T5, TResult> func, T1? p1, Funcky.Unit p2, Funcky.Unit p3, T4? p4, T5? p5)`
- `[ext] public static System.Func<T1, T2, T3, TResult> Apply<T1, T2, T3, T4, T5, TResult>(this System.Func<T1, T2, T3, T4, T5, TResult> func, Funcky.Unit p1, Funcky.Unit p2, Funcky.Unit p3, T4? p4, T5? p5)`
- `[ext] public static System.Func<T4, TResult> Apply<T1, T2, T3, T4, T5, TResult>(this System.Func<T1, T2, T3, T4, T5, TResult> func, T1? p1, T2? p2, T3? p3, Funcky.Unit p4, T5? p5)`
- `[ext] public static System.Func<T1, T4, TResult> Apply<T1, T2, T3, T4, T5, TResult>(this System.Func<T1, T2, T3, T4, T5, TResult> func, Funcky.Unit p1, T2? p2, T3? p3, Funcky.Unit p4, T5? p5)`
- `[ext] public static System.Func<T2, T4, TResult> Apply<T1, T2, T3, T4, T5, TResult>(this System.Func<T1, T2, T3, T4, T5, TResult> func, T1? p1, Funcky.Unit p2, T3? p3, Funcky.Unit p4, T5? p5)`
- `[ext] public static System.Func<T1, T2, T4, TResult> Apply<T1, T2, T3, T4, T5, TResult>(this System.Func<T1, T2, T3, T4, T5, TResult> func, Funcky.Unit p1, Funcky.Unit p2, T3? p3, Funcky.Unit p4, T5? p5)`
- `[ext] public static System.Func<T3, T4, TResult> Apply<T1, T2, T3, T4, T5, TResult>(this System.Func<T1, T2, T3, T4, T5, TResult> func, T1? p1, T2? p2, Funcky.Unit p3, Funcky.Unit p4, T5? p5)`
- `[ext] public static System.Func<T1, T3, T4, TResult> Apply<T1, T2, T3, T4, T5, TResult>(this System.Func<T1, T2, T3, T4, T5, TResult> func, Funcky.Unit p1, T2? p2, Funcky.Unit p3, Funcky.Unit p4, T5? p5)`
- `[ext] public static System.Func<T2, T3, T4, TResult> Apply<T1, T2, T3, T4, T5, TResult>(this System.Func<T1, T2, T3, T4, T5, TResult> func, T1? p1, Funcky.Unit p2, Funcky.Unit p3, Funcky.Unit p4, T5? p5)`
- `[ext] public static System.Func<T1, T2, T3, T4, TResult> Apply<T1, T2, T3, T4, T5, TResult>(this System.Func<T1, T2, T3, T4, T5, TResult> func, Funcky.Unit p1, Funcky.Unit p2, Funcky.Unit p3, Funcky.Unit p4, T5? p5)`
- `[ext] public static System.Func<T5, TResult> Apply<T1, T2, T3, T4, T5, TResult>(this System.Func<T1, T2, T3, T4, T5, TResult> func, T1? p1, T2? p2, T3? p3, T4? p4, Funcky.Unit p5)`
- `[ext] public static System.Func<T1, T5, TResult> Apply<T1, T2, T3, T4, T5, TResult>(this System.Func<T1, T2, T3, T4, T5, TResult> func, Funcky.Unit p1, T2? p2, T3? p3, T4? p4, Funcky.Unit p5)`
- `[ext] public static System.Func<T2, T5, TResult> Apply<T1, T2, T3, T4, T5, TResult>(this System.Func<T1, T2, T3, T4, T5, TResult> func, T1? p1, Funcky.Unit p2, T3? p3, T4? p4, Funcky.Unit p5)`
- `[ext] public static System.Func<T1, T2, T5, TResult> Apply<T1, T2, T3, T4, T5, TResult>(this System.Func<T1, T2, T3, T4, T5, TResult> func, Funcky.Unit p1, Funcky.Unit p2, T3? p3, T4? p4, Funcky.Unit p5)`
- `[ext] public static System.Func<T3, T5, TResult> Apply<T1, T2, T3, T4, T5, TResult>(this System.Func<T1, T2, T3, T4, T5, TResult> func, T1? p1, T2? p2, Funcky.Unit p3, T4? p4, Funcky.Unit p5)`
- `[ext] public static System.Func<T1, T3, T5, TResult> Apply<T1, T2, T3, T4, T5, TResult>(this System.Func<T1, T2, T3, T4, T5, TResult> func, Funcky.Unit p1, T2? p2, Funcky.Unit p3, T4? p4, Funcky.Unit p5)`
- `[ext] public static System.Func<T2, T3, T5, TResult> Apply<T1, T2, T3, T4, T5, TResult>(this System.Func<T1, T2, T3, T4, T5, TResult> func, T1? p1, Funcky.Unit p2, Funcky.Unit p3, T4? p4, Funcky.Unit p5)`
- `[ext] public static System.Func<T1, T2, T3, T5, TResult> Apply<T1, T2, T3, T4, T5, TResult>(this System.Func<T1, T2, T3, T4, T5, TResult> func, Funcky.Unit p1, Funcky.Unit p2, Funcky.Unit p3, T4? p4, Funcky.Unit p5)`
- `[ext] public static System.Func<T4, T5, TResult> Apply<T1, T2, T3, T4, T5, TResult>(this System.Func<T1, T2, T3, T4, T5, TResult> func, T1? p1, T2? p2, T3? p3, Funcky.Unit p4, Funcky.Unit p5)`
- `[ext] public static System.Func<T1, T4, T5, TResult> Apply<T1, T2, T3, T4, T5, TResult>(this System.Func<T1, T2, T3, T4, T5, TResult> func, Funcky.Unit p1, T2? p2, T3? p3, Funcky.Unit p4, Funcky.Unit p5)`
- `[ext] public static System.Func<T2, T4, T5, TResult> Apply<T1, T2, T3, T4, T5, TResult>(this System.Func<T1, T2, T3, T4, T5, TResult> func, T1? p1, Funcky.Unit p2, T3? p3, Funcky.Unit p4, Funcky.Unit p5)`
- `[ext] public static System.Func<T1, T2, T4, T5, TResult> Apply<T1, T2, T3, T4, T5, TResult>(this System.Func<T1, T2, T3, T4, T5, TResult> func, Funcky.Unit p1, Funcky.Unit p2, T3? p3, Funcky.Unit p4, Funcky.Unit p5)`
- `[ext] public static System.Func<T3, T4, T5, TResult> Apply<T1, T2, T3, T4, T5, TResult>(this System.Func<T1, T2, T3, T4, T5, TResult> func, T1? p1, T2? p2, Funcky.Unit p3, Funcky.Unit p4, Funcky.Unit p5)`
- `[ext] public static System.Func<T1, T3, T4, T5, TResult> Apply<T1, T2, T3, T4, T5, TResult>(this System.Func<T1, T2, T3, T4, T5, TResult> func, Funcky.Unit p1, T2? p2, Funcky.Unit p3, Funcky.Unit p4, Funcky.Unit p5)`
- `[ext] public static System.Func<T2, T3, T4, T5, TResult> Apply<T1, T2, T3, T4, T5, TResult>(this System.Func<T1, T2, T3, T4, T5, TResult> func, T1? p1, Funcky.Unit p2, Funcky.Unit p3, Funcky.Unit p4, Funcky.Unit p5)`
- `[ext] public static System.Func<TInput, TOutput> Compose<TInput, TIntermediate, TOutput>(this System.Func<TIntermediate, TOutput> f, System.Func<TInput, TIntermediate> g)`
- `[ext] public static System.Func<TOutput> Compose<TIntermediate, TOutput>(this System.Func<TIntermediate, TOutput> f, System.Func<TIntermediate> g)`
- `[ext] public static System.Func<T1, System.Func<T2, TResult>> Curry<T1, T2, TResult>(this System.Func<T1, T2, TResult> function)`
- `[ext] public static System.Func<T1, System.Func<T2, System.Func<T3, TResult>>> Curry<T1, T2, T3, TResult>(this System.Func<T1, T2, T3, TResult> function)`
- `[ext] public static System.Func<T1, System.Func<T2, System.Func<T3, System.Func<T4, TResult>>>> Curry<T1, T2, T3, T4, TResult>(this System.Func<T1, T2, T3, T4, TResult> function)`
- `[ext] public static System.Func<T1, System.Func<T2, System.Func<T3, System.Func<T4, System.Func<T5, TResult>>>>> Curry<T1, T2, T3, T4, T5, TResult>(this System.Func<T1, T2, T3, T4, T5, TResult> function)`
- `[ext] public static System.Func<T1, System.Func<T2, System.Func<T3, System.Func<T4, System.Func<T5, System.Func<T6, TResult>>>>>> Curry<T1, T2, T3, T4, T5, T6, TResult>(this System.Func<T1, T2, T3, T4, T5, T6, TResult> function)`
- `[ext] public static System.Func<T1, System.Func<T2, System.Func<T3, System.Func<T4, System.Func<T5, System.Func<T6, System.Func<T7, TResult>>>>>>> Curry<T1, T2, T3, T4, T5, T6, T7, TResult>(this System.Func<T1, T2, T3, T4, T5, T6, T7, TResult> function)`
- `[ext] public static System.Func<T1, System.Func<T2, System.Func<T3, System.Func<T4, System.Func<T5, System.Func<T6, System.Func<T7, System.Func<T8, TResult>>>>>>>> Curry<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(this System.Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> function)`
- `[ext] public static System.Func<T2, T1, TResult> Flip<T1, T2, TResult>(this System.Func<T1, T2, TResult> function)`
- `[ext] public static System.Func<T2, T1, T3, TResult> Flip<T1, T2, T3, TResult>(this System.Func<T1, T2, T3, TResult> function)`
- `[ext] public static System.Func<T2, T1, T3, T4, TResult> Flip<T1, T2, T3, T4, TResult>(this System.Func<T1, T2, T3, T4, TResult> function)`
- `[ext] public static System.Func<T2, T1, T3, T4, T5, TResult> Flip<T1, T2, T3, T4, T5, TResult>(this System.Func<T1, T2, T3, T4, T5, TResult> function)`
- `[ext] public static System.Func<T2, T1, T3, T4, T5, T6, TResult> Flip<T1, T2, T3, T4, T5, T6, TResult>(this System.Func<T1, T2, T3, T4, T5, T6, TResult> function)`
- `[ext] public static System.Func<T2, T1, T3, T4, T5, T6, T7, TResult> Flip<T1, T2, T3, T4, T5, T6, T7, TResult>(this System.Func<T1, T2, T3, T4, T5, T6, T7, TResult> function)`
- `[ext] public static System.Func<T2, T1, T3, T4, T5, T6, T7, T8, TResult> Flip<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(this System.Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> function)`
- `[ext] public static System.Func<T1, T2, TResult> Uncurry<T1, T2, TResult>(this System.Func<T1, System.Func<T2, TResult>> function)`
- `[ext] public static System.Func<T1, T2, T3, TResult> Uncurry<T1, T2, T3, TResult>(this System.Func<T1, System.Func<T2, System.Func<T3, TResult>>> function)`
- `[ext] public static System.Func<T1, T2, T3, T4, TResult> Uncurry<T1, T2, T3, T4, TResult>(this System.Func<T1, System.Func<T2, System.Func<T3, System.Func<T4, TResult>>>> function)`
- `[ext] public static System.Func<T1, T2, T3, T4, T5, TResult> Uncurry<T1, T2, T3, T4, T5, TResult>(this System.Func<T1, System.Func<T2, System.Func<T3, System.Func<T4, System.Func<T5, TResult>>>>> function)`
- `[ext] public static System.Func<T1, T2, T3, T4, T5, T6, TResult> Uncurry<T1, T2, T3, T4, T5, T6, TResult>(this System.Func<T1, System.Func<T2, System.Func<T3, System.Func<T4, System.Func<T5, System.Func<T6, TResult>>>>>> function)`
- `[ext] public static System.Func<T1, T2, T3, T4, T5, T6, T7, TResult> Uncurry<T1, T2, T3, T4, T5, T6, T7, TResult>(this System.Func<T1, System.Func<T2, System.Func<T3, System.Func<T4, System.Func<T5, System.Func<T6, System.Func<T7, TResult>>>>>>> function)`
- `[ext] public static System.Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> Uncurry<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(this System.Func<T1, System.Func<T2, System.Func<T3, System.Func<T4, System.Func<T5, System.Func<T6, System.Func<T7, System.Func<T8, TResult>>>>>>>> function)`

### HttpHeadersExtensions (class [static])

- `[ext] public static Funcky.Monads.Option<System.Collections.Generic.IEnumerable<System.String>> GetValuesOrNone(this System.Net.Http.Headers.HttpHeaders headers, System.String name)`

### HttpHeadersNonValidatedExtensions (class [static])

- `[ext] public static Funcky.Monads.Option<System.Net.Http.Headers.HeaderStringValues> GetValuesOrNone(this System.Net.Http.Headers.HttpHeadersNonValidated headers, System.String headerName)`

### ImmutableListExtensions (class [static])

- `public static Funcky.Monads.Option<System.Int32> IndexOfOrNone<TItem>(System.Collections.Immutable.IImmutableList<TItem> list, TItem? item)`
- `public static Funcky.Monads.Option<System.Int32> IndexOfOrNone<TItem>(System.Collections.Immutable.IImmutableList<TItem> list, TItem? item, System.Collections.Generic.IEqualityComparer<TItem>? equalityComparer)`
- `public static Funcky.Monads.Option<System.Int32> IndexOfOrNone<TItem>(System.Collections.Immutable.IImmutableList<TItem> list, TItem? item, System.Int32 startIndex)`
- `public static Funcky.Monads.Option<System.Int32> IndexOfOrNone<TItem>(System.Collections.Immutable.IImmutableList<TItem> list, TItem? item, System.Int32 startIndex, System.Int32 count)`
- `public static Funcky.Monads.Option<System.Int32> IndexOfOrNone<TItem>(System.Collections.Immutable.IImmutableList<TItem> list, TItem? item, System.Int32 startIndex, System.Int32 count, System.Collections.Generic.IEqualityComparer<TItem>? equalityComparer)`
- `public static Funcky.Monads.Option<System.Int32> LastIndexOfOrNone<TItem>(System.Collections.Immutable.IImmutableList<TItem> list, TItem? item)`
- `public static Funcky.Monads.Option<System.Int32> LastIndexOfOrNone<TItem>(System.Collections.Immutable.IImmutableList<TItem> list, TItem? item, System.Collections.Generic.IEqualityComparer<TItem>? equalityComparer)`
- `public static Funcky.Monads.Option<System.Int32> LastIndexOfOrNone<TItem>(System.Collections.Immutable.IImmutableList<TItem> list, TItem? item, System.Int32 startIndex)`
- `public static Funcky.Monads.Option<System.Int32> LastIndexOfOrNone<TItem>(System.Collections.Immutable.IImmutableList<TItem> list, TItem? item, System.Int32 startIndex, System.Int32 count)`
- `public static Funcky.Monads.Option<System.Int32> LastIndexOfOrNone<TItem>(System.Collections.Immutable.IImmutableList<TItem> list, TItem? item, System.Int32 startIndex, System.Int32 count, System.Collections.Generic.IEqualityComparer<TItem>? equalityComparer)`

### JsonSerializerOptionsExtensions (class [static])

- `[ext] public static Funcky.Monads.Option<System.Text.Json.Serialization.Metadata.JsonTypeInfo> GetTypeInfoOrNone(this System.Text.Json.JsonSerializerOptions options, System.Type type)`

### ListExtensions (class [static])

- `[ext] public static Funcky.Monads.Option<System.Int32> FindIndexOrNone<TValue>(this System.Collections.Generic.List<TValue> list, System.Predicate<TValue> match)`
- `[ext] public static Funcky.Monads.Option<System.Int32> FindIndexOrNone<TValue>(this System.Collections.Generic.List<TValue> list, System.Int32 startIndex, System.Predicate<TValue> match)`
- `[ext] public static Funcky.Monads.Option<System.Int32> FindIndexOrNone<TValue>(this System.Collections.Generic.List<TValue> list, System.Int32 startIndex, System.Int32 count, System.Predicate<TValue> match)`
- `[ext] public static Funcky.Monads.Option<System.Int32> FindLastIndexOrNone<TValue>(this System.Collections.Generic.List<TValue> list, System.Predicate<TValue> match)`
- `[ext] public static Funcky.Monads.Option<System.Int32> FindLastIndexOrNone<TValue>(this System.Collections.Generic.List<TValue> list, System.Int32 startIndex, System.Predicate<TValue> match)`
- `[ext] public static Funcky.Monads.Option<System.Int32> FindLastIndexOrNone<TValue>(this System.Collections.Generic.List<TValue> list, System.Int32 startIndex, System.Int32 count, System.Predicate<TValue> match)`
- `[ext] public static Funcky.Monads.Option<System.Int32> IndexOfOrNone<TItem>(this System.Collections.Immutable.IImmutableList<TItem> list, TItem? item)`
- `[ext] public static Funcky.Monads.Option<System.Int32> IndexOfOrNone<TValue>(this System.Collections.Generic.IList<TValue> list, TValue? value)`
- `[ext] public static Funcky.Monads.Option<System.Int32> IndexOfOrNone<TItem>(this System.Collections.Immutable.IImmutableList<TItem> list, TItem? item, System.Collections.Generic.IEqualityComparer<TItem>? equalityComparer)`
- `[ext] public static Funcky.Monads.Option<System.Int32> IndexOfOrNone<TItem>(this System.Collections.Immutable.IImmutableList<TItem> list, TItem? item, System.Int32 startIndex)`
- `[ext] public static Funcky.Monads.Option<System.Int32> IndexOfOrNone<TItem>(this System.Collections.Immutable.IImmutableList<TItem> list, TItem? item, System.Int32 startIndex, System.Int32 count)`
- `[ext] public static Funcky.Monads.Option<System.Int32> IndexOfOrNone<TItem>(this System.Collections.Immutable.IImmutableList<TItem> list, TItem? item, System.Int32 startIndex, System.Int32 count, System.Collections.Generic.IEqualityComparer<TItem>? equalityComparer)`
- `[ext] public static Funcky.Monads.Option<System.Int32> LastIndexOfOrNone<TItem>(this System.Collections.Immutable.IImmutableList<TItem> list, TItem? item)`
- `[ext] public static Funcky.Monads.Option<System.Int32> LastIndexOfOrNone<TItem>(this System.Collections.Immutable.IImmutableList<TItem> list, TItem? item, System.Collections.Generic.IEqualityComparer<TItem>? equalityComparer)`
- `[ext] public static Funcky.Monads.Option<System.Int32> LastIndexOfOrNone<TItem>(this System.Collections.Immutable.IImmutableList<TItem> list, TItem? item, System.Int32 startIndex)`
- `[ext] public static Funcky.Monads.Option<System.Int32> LastIndexOfOrNone<TItem>(this System.Collections.Immutable.IImmutableList<TItem> list, TItem? item, System.Int32 startIndex, System.Int32 count)`
- `[ext] public static Funcky.Monads.Option<System.Int32> LastIndexOfOrNone<TItem>(this System.Collections.Immutable.IImmutableList<TItem> list, TItem? item, System.Int32 startIndex, System.Int32 count, System.Collections.Generic.IEqualityComparer<TItem>? equalityComparer)`

### OrderedDictionaryExtensions (class [static])

- `[ext] public static Funcky.Monads.Option<System.Int32> IndexOfOrNone<TKey, TValue>(this System.Collections.Generic.OrderedDictionary<TKey, TValue> dictionary, TKey key)`

### ParseExtensions (class [static])

- `[ext] public static Funcky.Monads.Option<System.Reflection.Metadata.AssemblyNameInfo> ParseAssemblyNameInfoOrNone(this System.ReadOnlySpan<System.Char> candidate)`
- `[ext] public static Funcky.Monads.Option<System.Net.Http.Headers.AuthenticationHeaderValue> ParseAuthenticationHeaderValueOrNone(this System.String? candidate)`
- `[ext] public static Funcky.Monads.Option<System.Numerics.BigInteger> ParseBigIntegerOrNone(this System.ReadOnlySpan<System.Char> candidate)`
- `[ext] public static Funcky.Monads.Option<System.Numerics.BigInteger> ParseBigIntegerOrNone(this System.String? candidate)`
- `[ext] public static Funcky.Monads.Option<System.Numerics.BigInteger> ParseBigIntegerOrNone(this System.ReadOnlySpan<System.Char> candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Numerics.BigInteger> ParseBigIntegerOrNone(this System.String? candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Numerics.BigInteger> ParseBigIntegerOrNone(this System.ReadOnlySpan<System.Char> candidate, System.Globalization.NumberStyles style, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Numerics.BigInteger> ParseBigIntegerOrNone(this System.String? candidate, System.Globalization.NumberStyles style, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Boolean> ParseBooleanOrNone(this System.ReadOnlySpan<System.Char> candidate)`
- `[ext] public static Funcky.Monads.Option<System.Boolean> ParseBooleanOrNone(this System.String? candidate)`
- `[ext] public static Funcky.Monads.Option<System.Byte> ParseByteOrNone(this System.ReadOnlySpan<System.Byte> candidate)`
- `[ext] public static Funcky.Monads.Option<System.Byte> ParseByteOrNone(this System.ReadOnlySpan<System.Char> candidate)`
- `[ext] public static Funcky.Monads.Option<System.Byte> ParseByteOrNone(this System.String? candidate)`
- `[ext] public static Funcky.Monads.Option<System.Byte> ParseByteOrNone(this System.ReadOnlySpan<System.Byte> candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Byte> ParseByteOrNone(this System.ReadOnlySpan<System.Char> candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Byte> ParseByteOrNone(this System.String? candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Byte> ParseByteOrNone(this System.ReadOnlySpan<System.Byte> candidate, System.Globalization.NumberStyles style, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Byte> ParseByteOrNone(this System.ReadOnlySpan<System.Char> candidate, System.Globalization.NumberStyles style, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Byte> ParseByteOrNone(this System.String? candidate, System.Globalization.NumberStyles style, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Net.Http.Headers.CacheControlHeaderValue> ParseCacheControlHeaderValueOrNone(this System.String? candidate)`
- `[ext] public static Funcky.Monads.Option<System.Char> ParseCharOrNone(this System.String? candidate)`
- `[ext] public static Funcky.Monads.Option<System.Net.Http.Headers.ContentDispositionHeaderValue> ParseContentDispositionHeaderValueOrNone(this System.String? candidate)`
- `[ext] public static Funcky.Monads.Option<System.Net.Http.Headers.ContentRangeHeaderValue> ParseContentRangeHeaderValueOrNone(this System.String? candidate)`
- `[ext] public static Funcky.Monads.Option<System.DateOnly> ParseDateOnlyOrNone(this System.ReadOnlySpan<System.Char> candidate)`
- `[ext] public static Funcky.Monads.Option<System.DateOnly> ParseDateOnlyOrNone(this System.String? candidate)`
- `[ext] public static Funcky.Monads.Option<System.DateOnly> ParseDateOnlyOrNone(this System.ReadOnlySpan<System.Char> candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.DateOnly> ParseDateOnlyOrNone(this System.String? candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.DateOnly> ParseDateOnlyOrNone(this System.ReadOnlySpan<System.Char> candidate, System.IFormatProvider? provider, System.Globalization.DateTimeStyles style)`
- `[ext] public static Funcky.Monads.Option<System.DateOnly> ParseDateOnlyOrNone(this System.String? candidate, System.IFormatProvider? provider, System.Globalization.DateTimeStyles style)`
- `[ext] public static Funcky.Monads.Option<System.DateTimeOffset> ParseDateTimeOffsetOrNone(this System.ReadOnlySpan<System.Char> candidate)`
- `[ext] public static Funcky.Monads.Option<System.DateTimeOffset> ParseDateTimeOffsetOrNone(this System.String? candidate)`
- `[ext] public static Funcky.Monads.Option<System.DateTimeOffset> ParseDateTimeOffsetOrNone(this System.ReadOnlySpan<System.Char> candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.DateTimeOffset> ParseDateTimeOffsetOrNone(this System.String? candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.DateTimeOffset> ParseDateTimeOffsetOrNone(this System.ReadOnlySpan<System.Char> candidate, System.IFormatProvider? formatProvider, System.Globalization.DateTimeStyles styles)`
- `[ext] public static Funcky.Monads.Option<System.DateTimeOffset> ParseDateTimeOffsetOrNone(this System.String? candidate, System.IFormatProvider? formatProvider, System.Globalization.DateTimeStyles styles)`
- `[ext] public static Funcky.Monads.Option<System.DateTime> ParseDateTimeOrNone(this System.ReadOnlySpan<System.Char> candidate)`
- `[ext] public static Funcky.Monads.Option<System.DateTime> ParseDateTimeOrNone(this System.String? candidate)`
- `[ext] public static Funcky.Monads.Option<System.DateTime> ParseDateTimeOrNone(this System.ReadOnlySpan<System.Char> candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.DateTime> ParseDateTimeOrNone(this System.String? candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.DateTime> ParseDateTimeOrNone(this System.ReadOnlySpan<System.Char> candidate, System.IFormatProvider? provider, System.Globalization.DateTimeStyles styles)`
- `[ext] public static Funcky.Monads.Option<System.DateTime> ParseDateTimeOrNone(this System.String? candidate, System.IFormatProvider? provider, System.Globalization.DateTimeStyles styles)`
- `[ext] public static Funcky.Monads.Option<System.Decimal> ParseDecimalOrNone(this System.ReadOnlySpan<System.Byte> candidate)`
- `[ext] public static Funcky.Monads.Option<System.Decimal> ParseDecimalOrNone(this System.ReadOnlySpan<System.Char> candidate)`
- `[ext] public static Funcky.Monads.Option<System.Decimal> ParseDecimalOrNone(this System.String? candidate)`
- `[ext] public static Funcky.Monads.Option<System.Decimal> ParseDecimalOrNone(this System.ReadOnlySpan<System.Byte> candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Decimal> ParseDecimalOrNone(this System.ReadOnlySpan<System.Char> candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Decimal> ParseDecimalOrNone(this System.String? candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Decimal> ParseDecimalOrNone(this System.ReadOnlySpan<System.Byte> candidate, System.Globalization.NumberStyles style, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Decimal> ParseDecimalOrNone(this System.ReadOnlySpan<System.Char> candidate, System.Globalization.NumberStyles style, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Decimal> ParseDecimalOrNone(this System.String? candidate, System.Globalization.NumberStyles style, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Double> ParseDoubleOrNone(this System.ReadOnlySpan<System.Byte> candidate)`
- `[ext] public static Funcky.Monads.Option<System.Double> ParseDoubleOrNone(this System.ReadOnlySpan<System.Char> candidate)`
- `[ext] public static Funcky.Monads.Option<System.Double> ParseDoubleOrNone(this System.String? candidate)`
- `[ext] public static Funcky.Monads.Option<System.Double> ParseDoubleOrNone(this System.ReadOnlySpan<System.Byte> candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Double> ParseDoubleOrNone(this System.ReadOnlySpan<System.Char> candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Double> ParseDoubleOrNone(this System.String? candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Double> ParseDoubleOrNone(this System.ReadOnlySpan<System.Byte> candidate, System.Globalization.NumberStyles style, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Double> ParseDoubleOrNone(this System.ReadOnlySpan<System.Char> candidate, System.Globalization.NumberStyles style, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Double> ParseDoubleOrNone(this System.String? candidate, System.Globalization.NumberStyles style, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Net.Http.Headers.EntityTagHeaderValue> ParseEntityTagHeaderValueOrNone(this System.String? candidate)`
- `[ext] public static Funcky.Monads.Option<TEnum> ParseEnumOrNone<TEnum>(this System.String candidate)`
- `where TEnum : struct`
- `[ext] public static Funcky.Monads.Option<TEnum> ParseEnumOrNone<TEnum>(this System.ReadOnlySpan<System.Char> candidate)`
- `where TEnum : struct`
- `[ext] public static Funcky.Monads.Option<TEnum> ParseEnumOrNone<TEnum>(this System.String candidate, System.Boolean ignoreCase)`
- `where TEnum : struct`
- `[ext] public static Funcky.Monads.Option<System.Object> ParseEnumOrNone(this System.String candidate, System.Type enumType)`
- `[ext] public static Funcky.Monads.Option<TEnum> ParseEnumOrNone<TEnum>(this System.ReadOnlySpan<System.Char> candidate, System.Boolean ignoreCase)`
- `where TEnum : struct`
- `[ext] public static Funcky.Monads.Option<System.Object> ParseEnumOrNone(this System.ReadOnlySpan<System.Char> candidate, System.Type enumType)`
- `[ext] public static Funcky.Monads.Option<System.Object> ParseEnumOrNone(this System.String candidate, System.Type enumType, System.Boolean ignoreCase)`
- `[ext] public static Funcky.Monads.Option<System.Object> ParseEnumOrNone(this System.ReadOnlySpan<System.Char> candidate, System.Type enumType, System.Boolean ignoreCase)`
- `[ext] public static Funcky.Monads.Option<System.DateOnly> ParseExactDateOnlyOrNone(this System.ReadOnlySpan<System.Char> candidate, System.ReadOnlySpan<System.Char> format)`
- `[ext] public static Funcky.Monads.Option<System.DateOnly> ParseExactDateOnlyOrNone(this System.ReadOnlySpan<System.Char> candidate, System.String[]? formats)`
- `[ext] public static Funcky.Monads.Option<System.DateOnly> ParseExactDateOnlyOrNone(this System.String? candidate, System.String? format)`
- `[ext] public static Funcky.Monads.Option<System.DateOnly> ParseExactDateOnlyOrNone(this System.String? candidate, System.String[]? formats)`
- `[ext] public static Funcky.Monads.Option<System.DateOnly> ParseExactDateOnlyOrNone(this System.ReadOnlySpan<System.Char> candidate, System.ReadOnlySpan<System.Char> format, System.IFormatProvider? provider, System.Globalization.DateTimeStyles style)`
- `[ext] public static Funcky.Monads.Option<System.DateOnly> ParseExactDateOnlyOrNone(this System.ReadOnlySpan<System.Char> candidate, System.String[]? formats, System.IFormatProvider? provider, System.Globalization.DateTimeStyles style)`
- `[ext] public static Funcky.Monads.Option<System.DateOnly> ParseExactDateOnlyOrNone(this System.String? candidate, System.String? format, System.IFormatProvider? provider, System.Globalization.DateTimeStyles style)`
- `[ext] public static Funcky.Monads.Option<System.DateOnly> ParseExactDateOnlyOrNone(this System.String? candidate, System.String[]? formats, System.IFormatProvider? provider, System.Globalization.DateTimeStyles style)`
- `[ext] public static Funcky.Monads.Option<System.DateTimeOffset> ParseExactDateTimeOffsetOrNone(this System.ReadOnlySpan<System.Char> candidate, System.ReadOnlySpan<System.Char> format, System.IFormatProvider? formatProvider, System.Globalization.DateTimeStyles styles)`
- `[ext] public static Funcky.Monads.Option<System.DateTimeOffset> ParseExactDateTimeOffsetOrNone(this System.ReadOnlySpan<System.Char> candidate, System.String[]? formats, System.IFormatProvider? formatProvider, System.Globalization.DateTimeStyles styles)`
- `[ext] public static Funcky.Monads.Option<System.DateTimeOffset> ParseExactDateTimeOffsetOrNone(this System.String? candidate, System.String? format, System.IFormatProvider? formatProvider, System.Globalization.DateTimeStyles styles)`
- `[ext] public static Funcky.Monads.Option<System.DateTimeOffset> ParseExactDateTimeOffsetOrNone(this System.String? candidate, System.String[]? formats, System.IFormatProvider? formatProvider, System.Globalization.DateTimeStyles styles)`
- `[ext] public static Funcky.Monads.Option<System.DateTime> ParseExactDateTimeOrNone(this System.ReadOnlySpan<System.Char> candidate, System.ReadOnlySpan<System.Char> format, System.IFormatProvider? provider, System.Globalization.DateTimeStyles style)`
- `[ext] public static Funcky.Monads.Option<System.DateTime> ParseExactDateTimeOrNone(this System.ReadOnlySpan<System.Char> candidate, System.String[]? formats, System.IFormatProvider? provider, System.Globalization.DateTimeStyles style)`
- `[ext] public static Funcky.Monads.Option<System.DateTime> ParseExactDateTimeOrNone(this System.String? candidate, System.String? format, System.IFormatProvider? provider, System.Globalization.DateTimeStyles style)`
- `[ext] public static Funcky.Monads.Option<System.DateTime> ParseExactDateTimeOrNone(this System.String? candidate, System.String[]? formats, System.IFormatProvider? provider, System.Globalization.DateTimeStyles style)`
- `[ext] public static Funcky.Monads.Option<System.Guid> ParseExactGuidOrNone(this System.ReadOnlySpan<System.Char> candidate, System.ReadOnlySpan<System.Char> format)`
- `[ext] public static Funcky.Monads.Option<System.Guid> ParseExactGuidOrNone(this System.String? candidate, System.String? format)`
- `[ext] public static Funcky.Monads.Option<System.TimeOnly> ParseExactTimeOnlyOrNone(this System.ReadOnlySpan<System.Char> candidate, System.ReadOnlySpan<System.Char> format)`
- `[ext] public static Funcky.Monads.Option<System.TimeOnly> ParseExactTimeOnlyOrNone(this System.ReadOnlySpan<System.Char> candidate, System.String[]? formats)`
- `[ext] public static Funcky.Monads.Option<System.TimeOnly> ParseExactTimeOnlyOrNone(this System.String? candidate, System.String? format)`
- `[ext] public static Funcky.Monads.Option<System.TimeOnly> ParseExactTimeOnlyOrNone(this System.String? candidate, System.String[]? formats)`
- `[ext] public static Funcky.Monads.Option<System.TimeOnly> ParseExactTimeOnlyOrNone(this System.ReadOnlySpan<System.Char> candidate, System.ReadOnlySpan<System.Char> format, System.IFormatProvider? provider, System.Globalization.DateTimeStyles style)`
- `[ext] public static Funcky.Monads.Option<System.TimeOnly> ParseExactTimeOnlyOrNone(this System.ReadOnlySpan<System.Char> candidate, System.String[]? formats, System.IFormatProvider? provider, System.Globalization.DateTimeStyles style)`
- `[ext] public static Funcky.Monads.Option<System.TimeOnly> ParseExactTimeOnlyOrNone(this System.String? candidate, System.String? format, System.IFormatProvider? provider, System.Globalization.DateTimeStyles style)`
- `[ext] public static Funcky.Monads.Option<System.TimeOnly> ParseExactTimeOnlyOrNone(this System.String? candidate, System.String[]? formats, System.IFormatProvider? provider, System.Globalization.DateTimeStyles style)`
- `[ext] public static Funcky.Monads.Option<System.TimeSpan> ParseExactTimeSpanOrNone(this System.ReadOnlySpan<System.Char> candidate, System.ReadOnlySpan<System.Char> format, System.IFormatProvider? formatProvider)`
- `[ext] public static Funcky.Monads.Option<System.TimeSpan> ParseExactTimeSpanOrNone(this System.ReadOnlySpan<System.Char> candidate, System.String[]? formats, System.IFormatProvider? formatProvider)`
- `[ext] public static Funcky.Monads.Option<System.TimeSpan> ParseExactTimeSpanOrNone(this System.String? candidate, System.String? format, System.IFormatProvider? formatProvider)`
- `[ext] public static Funcky.Monads.Option<System.TimeSpan> ParseExactTimeSpanOrNone(this System.String? candidate, System.String[]? formats, System.IFormatProvider? formatProvider)`
- `[ext] public static Funcky.Monads.Option<System.TimeSpan> ParseExactTimeSpanOrNone(this System.ReadOnlySpan<System.Char> candidate, System.ReadOnlySpan<System.Char> format, System.IFormatProvider? formatProvider, System.Globalization.TimeSpanStyles styles)`
- `[ext] public static Funcky.Monads.Option<System.TimeSpan> ParseExactTimeSpanOrNone(this System.ReadOnlySpan<System.Char> candidate, System.String[]? formats, System.IFormatProvider? formatProvider, System.Globalization.TimeSpanStyles styles)`
- `[ext] public static Funcky.Monads.Option<System.TimeSpan> ParseExactTimeSpanOrNone(this System.String? candidate, System.String? format, System.IFormatProvider? formatProvider, System.Globalization.TimeSpanStyles styles)`
- `[ext] public static Funcky.Monads.Option<System.TimeSpan> ParseExactTimeSpanOrNone(this System.String? candidate, System.String[]? formats, System.IFormatProvider? formatProvider, System.Globalization.TimeSpanStyles styles)`
- `[ext] public static Funcky.Monads.Option<System.Guid> ParseGuidOrNone(this System.ReadOnlySpan<System.Byte> candidate)`
- `[ext] public static Funcky.Monads.Option<System.Guid> ParseGuidOrNone(this System.ReadOnlySpan<System.Char> candidate)`
- `[ext] public static Funcky.Monads.Option<System.Guid> ParseGuidOrNone(this System.String? candidate)`
- `[ext] public static Funcky.Monads.Option<System.Guid> ParseGuidOrNone(this System.ReadOnlySpan<System.Byte> candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Guid> ParseGuidOrNone(this System.ReadOnlySpan<System.Char> candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Guid> ParseGuidOrNone(this System.String? candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Net.IPAddress> ParseIPAddressOrNone(this System.ReadOnlySpan<System.Char> candidate)`
- `[ext] public static Funcky.Monads.Option<System.Net.IPAddress> ParseIPAddressOrNone(this System.String? candidate)`
- `[ext] public static Funcky.Monads.Option<System.Net.IPAddress> ParseIPAddressOrNone(this System.ReadOnlySpan<System.Byte> candidate)`
- `[ext] public static Funcky.Monads.Option<System.Net.IPEndPoint> ParseIPEndPointOrNone(this System.ReadOnlySpan<System.Char> candidate)`
- `[ext] public static Funcky.Monads.Option<System.Net.IPEndPoint> ParseIPEndPointOrNone(this System.String candidate)`
- `[ext] public static Funcky.Monads.Option<System.Net.IPNetwork> ParseIPNetworkOrNone(this System.ReadOnlySpan<System.Char> candidate)`
- `[ext] public static Funcky.Monads.Option<System.Net.IPNetwork> ParseIPNetworkOrNone(this System.String? candidate)`
- `[ext] public static Funcky.Monads.Option<System.Net.IPNetwork> ParseIPNetworkOrNone(this System.ReadOnlySpan<System.Byte> candidate)`
- `[ext] public static Funcky.Monads.Option<System.Int16> ParseInt16OrNone(this System.ReadOnlySpan<System.Byte> candidate)`
- `[ext] public static Funcky.Monads.Option<System.Int16> ParseInt16OrNone(this System.ReadOnlySpan<System.Char> candidate)`
- `[ext] public static Funcky.Monads.Option<System.Int16> ParseInt16OrNone(this System.String? candidate)`
- `[ext] public static Funcky.Monads.Option<System.Int16> ParseInt16OrNone(this System.ReadOnlySpan<System.Byte> candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Int16> ParseInt16OrNone(this System.ReadOnlySpan<System.Char> candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Int16> ParseInt16OrNone(this System.String? candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Int16> ParseInt16OrNone(this System.ReadOnlySpan<System.Byte> candidate, System.Globalization.NumberStyles style, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Int16> ParseInt16OrNone(this System.ReadOnlySpan<System.Char> candidate, System.Globalization.NumberStyles style, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Int16> ParseInt16OrNone(this System.String? candidate, System.Globalization.NumberStyles style, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Int32> ParseInt32OrNone(this System.ReadOnlySpan<System.Byte> candidate)`
- `[ext] public static Funcky.Monads.Option<System.Int32> ParseInt32OrNone(this System.ReadOnlySpan<System.Char> candidate)`
- `[ext] public static Funcky.Monads.Option<System.Int32> ParseInt32OrNone(this System.String? candidate)`
- `[ext] public static Funcky.Monads.Option<System.Int32> ParseInt32OrNone(this System.ReadOnlySpan<System.Byte> candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Int32> ParseInt32OrNone(this System.ReadOnlySpan<System.Char> candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Int32> ParseInt32OrNone(this System.String? candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Int32> ParseInt32OrNone(this System.ReadOnlySpan<System.Byte> candidate, System.Globalization.NumberStyles style, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Int32> ParseInt32OrNone(this System.ReadOnlySpan<System.Char> candidate, System.Globalization.NumberStyles style, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Int32> ParseInt32OrNone(this System.String? candidate, System.Globalization.NumberStyles style, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Int64> ParseInt64OrNone(this System.ReadOnlySpan<System.Byte> candidate)`
- `[ext] public static Funcky.Monads.Option<System.Int64> ParseInt64OrNone(this System.ReadOnlySpan<System.Char> candidate)`
- `[ext] public static Funcky.Monads.Option<System.Int64> ParseInt64OrNone(this System.String? candidate)`
- `[ext] public static Funcky.Monads.Option<System.Int64> ParseInt64OrNone(this System.ReadOnlySpan<System.Byte> candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Int64> ParseInt64OrNone(this System.ReadOnlySpan<System.Char> candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Int64> ParseInt64OrNone(this System.String? candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Int64> ParseInt64OrNone(this System.ReadOnlySpan<System.Byte> candidate, System.Globalization.NumberStyles style, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Int64> ParseInt64OrNone(this System.ReadOnlySpan<System.Char> candidate, System.Globalization.NumberStyles style, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Int64> ParseInt64OrNone(this System.String? candidate, System.Globalization.NumberStyles style, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Net.Http.Headers.MediaTypeHeaderValue> ParseMediaTypeHeaderValueOrNone(this System.String? candidate)`
- `[ext] public static Funcky.Monads.Option<System.Net.Http.Headers.MediaTypeWithQualityHeaderValue> ParseMediaTypeWithQualityHeaderValueOrNone(this System.String? candidate)`
- `[ext] public static Funcky.Monads.Option<System.Net.Http.Headers.NameValueHeaderValue> ParseNameValueHeaderValueOrNone(this System.String? candidate)`
- `[ext] public static Funcky.Monads.Option<System.Net.Http.Headers.NameValueWithParametersHeaderValue> ParseNameValueWithParametersHeaderValueOrNone(this System.String? candidate)`
- `[ext] public static Funcky.Monads.Option<TNumber> ParseNumberOrNone<TNumber>(this System.String value, System.Globalization.NumberStyles style, System.IFormatProvider? provider)`
- `where TNumber : System.Numerics.INumberBase<TNumber>`
- `[ext] public static Funcky.Monads.Option<TNumber> ParseNumberOrNone<TNumber>(this System.ReadOnlySpan<System.Char> value, System.Globalization.NumberStyles style, System.IFormatProvider? provider)`
- `where TNumber : System.Numerics.INumberBase<TNumber>`
- `[ext] public static Funcky.Monads.Option<TParsable> ParseOrNone<TParsable>(this System.ReadOnlySpan<System.Char> value, System.IFormatProvider? provider)`
- `where TParsable : System.ISpanParsable<TParsable>`
- `[ext] public static Funcky.Monads.Option<TParsable> ParseOrNone<TParsable>(this System.String? value, System.IFormatProvider? provider)`
- `where TParsable : System.IParsable<TParsable>`
- `[ext] public static Funcky.Monads.Option<TParsable> ParseOrNone<TParsable>(this System.ReadOnlySpan<System.Byte> utf8Text, System.IFormatProvider? provider)`
- `where TParsable : System.IUtf8SpanParsable<TParsable>`
- `[ext] public static Funcky.Monads.Option<System.Net.Http.Headers.ProductHeaderValue> ParseProductHeaderValueOrNone(this System.String? candidate)`
- `[ext] public static Funcky.Monads.Option<System.Net.Http.Headers.ProductInfoHeaderValue> ParseProductInfoHeaderValueOrNone(this System.String candidate)`
- `[ext] public static Funcky.Monads.Option<System.Net.Http.Headers.RangeConditionHeaderValue> ParseRangeConditionHeaderValueOrNone(this System.String? candidate)`
- `[ext] public static Funcky.Monads.Option<System.Net.Http.Headers.RangeHeaderValue> ParseRangeHeaderValueOrNone(this System.String? candidate)`
- `[ext] public static Funcky.Monads.Option<System.Net.Http.Headers.RetryConditionHeaderValue> ParseRetryConditionHeaderValueOrNone(this System.String? candidate)`
- `[ext] public static Funcky.Monads.Option<System.SByte> ParseSByteOrNone(this System.ReadOnlySpan<System.Byte> candidate)`
- `[ext] public static Funcky.Monads.Option<System.SByte> ParseSByteOrNone(this System.ReadOnlySpan<System.Char> candidate)`
- `[ext] public static Funcky.Monads.Option<System.SByte> ParseSByteOrNone(this System.String? candidate)`
- `[ext] public static Funcky.Monads.Option<System.SByte> ParseSByteOrNone(this System.ReadOnlySpan<System.Byte> candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.SByte> ParseSByteOrNone(this System.ReadOnlySpan<System.Char> candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.SByte> ParseSByteOrNone(this System.String? candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.SByte> ParseSByteOrNone(this System.ReadOnlySpan<System.Byte> candidate, System.Globalization.NumberStyles style, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.SByte> ParseSByteOrNone(this System.ReadOnlySpan<System.Char> candidate, System.Globalization.NumberStyles style, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.SByte> ParseSByteOrNone(this System.String? candidate, System.Globalization.NumberStyles style, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Single> ParseSingleOrNone(this System.ReadOnlySpan<System.Byte> candidate)`
- `[ext] public static Funcky.Monads.Option<System.Single> ParseSingleOrNone(this System.ReadOnlySpan<System.Char> candidate)`
- `[ext] public static Funcky.Monads.Option<System.Single> ParseSingleOrNone(this System.String? candidate)`
- `[ext] public static Funcky.Monads.Option<System.Single> ParseSingleOrNone(this System.ReadOnlySpan<System.Byte> candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Single> ParseSingleOrNone(this System.ReadOnlySpan<System.Char> candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Single> ParseSingleOrNone(this System.String? candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Single> ParseSingleOrNone(this System.ReadOnlySpan<System.Byte> candidate, System.Globalization.NumberStyles style, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Single> ParseSingleOrNone(this System.ReadOnlySpan<System.Char> candidate, System.Globalization.NumberStyles style, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Single> ParseSingleOrNone(this System.String? candidate, System.Globalization.NumberStyles style, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Net.Http.Headers.StringWithQualityHeaderValue> ParseStringWithQualityHeaderValueOrNone(this System.String? candidate)`
- `[ext] public static Funcky.Monads.Option<System.TimeOnly> ParseTimeOnlyOrNone(this System.ReadOnlySpan<System.Char> candidate)`
- `[ext] public static Funcky.Monads.Option<System.TimeOnly> ParseTimeOnlyOrNone(this System.String? candidate)`
- `[ext] public static Funcky.Monads.Option<System.TimeOnly> ParseTimeOnlyOrNone(this System.ReadOnlySpan<System.Char> candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.TimeOnly> ParseTimeOnlyOrNone(this System.String? candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.TimeOnly> ParseTimeOnlyOrNone(this System.ReadOnlySpan<System.Char> candidate, System.IFormatProvider? provider, System.Globalization.DateTimeStyles style)`
- `[ext] public static Funcky.Monads.Option<System.TimeOnly> ParseTimeOnlyOrNone(this System.String? candidate, System.IFormatProvider? provider, System.Globalization.DateTimeStyles style)`
- `[ext] public static Funcky.Monads.Option<System.TimeSpan> ParseTimeSpanOrNone(this System.ReadOnlySpan<System.Char> candidate)`
- `[ext] public static Funcky.Monads.Option<System.TimeSpan> ParseTimeSpanOrNone(this System.String? candidate)`
- `[ext] public static Funcky.Monads.Option<System.TimeSpan> ParseTimeSpanOrNone(this System.ReadOnlySpan<System.Char> candidate, System.IFormatProvider? formatProvider)`
- `[ext] public static Funcky.Monads.Option<System.TimeSpan> ParseTimeSpanOrNone(this System.String? candidate, System.IFormatProvider? formatProvider)`
- `[ext] public static Funcky.Monads.Option<System.Net.Http.Headers.TransferCodingHeaderValue> ParseTransferCodingHeaderValueOrNone(this System.String? candidate)`
- `[ext] public static Funcky.Monads.Option<System.Net.Http.Headers.TransferCodingWithQualityHeaderValue> ParseTransferCodingWithQualityHeaderValueOrNone(this System.String? candidate)`
- `[ext] public static Funcky.Monads.Option<System.Reflection.Metadata.TypeName> ParseTypeNameOrNone(this System.ReadOnlySpan<System.Char> candidate, System.Reflection.Metadata.TypeNameParseOptions? options)`
- `[ext] public static Funcky.Monads.Option<System.UInt16> ParseUInt16OrNone(this System.ReadOnlySpan<System.Byte> candidate)`
- `[ext] public static Funcky.Monads.Option<System.UInt16> ParseUInt16OrNone(this System.ReadOnlySpan<System.Char> candidate)`
- `[ext] public static Funcky.Monads.Option<System.UInt16> ParseUInt16OrNone(this System.String? candidate)`
- `[ext] public static Funcky.Monads.Option<System.UInt16> ParseUInt16OrNone(this System.ReadOnlySpan<System.Byte> candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.UInt16> ParseUInt16OrNone(this System.ReadOnlySpan<System.Char> candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.UInt16> ParseUInt16OrNone(this System.String? candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.UInt16> ParseUInt16OrNone(this System.ReadOnlySpan<System.Byte> candidate, System.Globalization.NumberStyles style, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.UInt16> ParseUInt16OrNone(this System.ReadOnlySpan<System.Char> candidate, System.Globalization.NumberStyles style, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.UInt16> ParseUInt16OrNone(this System.String? candidate, System.Globalization.NumberStyles style, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.UInt32> ParseUInt32OrNone(this System.ReadOnlySpan<System.Byte> candidate)`
- `[ext] public static Funcky.Monads.Option<System.UInt32> ParseUInt32OrNone(this System.ReadOnlySpan<System.Char> candidate)`
- `[ext] public static Funcky.Monads.Option<System.UInt32> ParseUInt32OrNone(this System.String? candidate)`
- `[ext] public static Funcky.Monads.Option<System.UInt32> ParseUInt32OrNone(this System.ReadOnlySpan<System.Byte> candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.UInt32> ParseUInt32OrNone(this System.ReadOnlySpan<System.Char> candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.UInt32> ParseUInt32OrNone(this System.String? candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.UInt32> ParseUInt32OrNone(this System.ReadOnlySpan<System.Byte> candidate, System.Globalization.NumberStyles style, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.UInt32> ParseUInt32OrNone(this System.ReadOnlySpan<System.Char> candidate, System.Globalization.NumberStyles style, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.UInt32> ParseUInt32OrNone(this System.String? candidate, System.Globalization.NumberStyles style, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.UInt64> ParseUInt64OrNone(this System.ReadOnlySpan<System.Byte> candidate)`
- `[ext] public static Funcky.Monads.Option<System.UInt64> ParseUInt64OrNone(this System.ReadOnlySpan<System.Char> candidate)`
- `[ext] public static Funcky.Monads.Option<System.UInt64> ParseUInt64OrNone(this System.String? candidate)`
- `[ext] public static Funcky.Monads.Option<System.UInt64> ParseUInt64OrNone(this System.ReadOnlySpan<System.Byte> candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.UInt64> ParseUInt64OrNone(this System.ReadOnlySpan<System.Char> candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.UInt64> ParseUInt64OrNone(this System.String? candidate, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.UInt64> ParseUInt64OrNone(this System.ReadOnlySpan<System.Byte> candidate, System.Globalization.NumberStyles style, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.UInt64> ParseUInt64OrNone(this System.ReadOnlySpan<System.Char> candidate, System.Globalization.NumberStyles style, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.UInt64> ParseUInt64OrNone(this System.String? candidate, System.Globalization.NumberStyles style, System.IFormatProvider? provider)`
- `[ext] public static Funcky.Monads.Option<System.Version> ParseVersionOrNone(this System.ReadOnlySpan<System.Byte> candidate)`
- `[ext] public static Funcky.Monads.Option<System.Version> ParseVersionOrNone(this System.ReadOnlySpan<System.Char> candidate)`
- `[ext] public static Funcky.Monads.Option<System.Version> ParseVersionOrNone(this System.String? candidate)`
- `[ext] public static Funcky.Monads.Option<System.Net.Http.Headers.ViaHeaderValue> ParseViaHeaderValueOrNone(this System.String? candidate)`
- `[ext] public static Funcky.Monads.Option<System.Net.Http.Headers.WarningHeaderValue> ParseWarningHeaderValueOrNone(this System.String? candidate)`

### Partitions (class [static])

- `public static Funcky.Extensions.Partitions<TSource> Create<TSource>(System.Collections.Generic.IReadOnlyList<TSource> true, System.Collections.Generic.IReadOnlyList<TSource> false)`

### Partitions`1<TSource> (struct [readonly struct])

- `public Partitions`1(System.Collections.Generic.IReadOnlyList<TSource> true, System.Collections.Generic.IReadOnlyList<TSource> false)`
- `public System.Collections.Generic.IReadOnlyList<TSource> False { get; }`
- `public System.Collections.Generic.IReadOnlyList<TSource> True { get; }`
- `public System.Void Deconstruct(out System.Collections.Generic.IReadOnlyList<TSource>& true, out System.Collections.Generic.IReadOnlyList<TSource>& false)`

### PriorityQueueExtensions (class [static])

- `[ext] public static Funcky.Monads.Option<System.ValueTuple<TElement, TPriority>> DequeueOrNone<TElement, TPriority>(this System.Collections.Generic.PriorityQueue<TElement, TPriority> priorityQueue)`
- `[ext] public static Funcky.Monads.Option<System.ValueTuple<TElement, TPriority>> PeekOrNone<TElement, TPriority>(this System.Collections.Generic.PriorityQueue<TElement, TPriority> priorityQueue)`

### QueryableExtensions (class [static])

- `[ext] public static Funcky.Monads.Option<TSource> ElementAtOrNone<TSource>(this System.Linq.IQueryable<TSource> source, System.Int32 index)`
- `[ext] public static Funcky.Monads.Option<TSource> ElementAtOrNone<TSource>(this System.Linq.IQueryable<TSource> source, System.Index index)`
- `[ext] public static Funcky.Monads.Option<TSource> FirstOrNone<TSource>(this System.Linq.IQueryable<TSource> source)`
- `[ext] public static Funcky.Monads.Option<TSource> FirstOrNone<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, System.Boolean>> predicate)`
- `[ext] public static Funcky.Monads.Option<TSource> LastOrNone<TSource>(this System.Linq.IQueryable<TSource> source)`
- `[ext] public static Funcky.Monads.Option<TSource> LastOrNone<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, System.Boolean>> predicate)`
- `[ext] public static Funcky.Monads.Option<TSource> SingleOrNone<TSource>(this System.Linq.IQueryable<TSource> source)`
- `[ext] public static Funcky.Monads.Option<TSource> SingleOrNone<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, System.Boolean>> predicate)`

### QueueExtensions (class [static])

- `[ext] public static Funcky.Monads.Option<TItem> DequeueOrNone<TItem>(this System.Collections.Generic.Queue<TItem> queue)`
- `[ext] public static Funcky.Monads.Option<TItem> DequeueOrNone<TItem>(this System.Collections.Concurrent.ConcurrentQueue<TItem> concurrentQueue)`
- `[ext] public static Funcky.Monads.Option<TItem> PeekOrNone<TItem>(this System.Collections.Generic.Queue<TItem> queue)`
- `[ext] public static Funcky.Monads.Option<TItem> PeekOrNone<TItem>(this System.Collections.Concurrent.ConcurrentQueue<TItem> concurrentQueue)`

### RangeExtensions (class [static])

- `[ext] public static System.Collections.Generic.IEnumerator<System.Int32> GetEnumerator(this System.Range range)`
- `[ext] public static System.Collections.Generic.IEnumerable<TResult> Select<TResult>(this System.Range source, System.Func<System.Int32, TResult> selector)`
- `[ext] public static System.Collections.Generic.IEnumerable<TResult> SelectMany<TResult>(this System.Range source, System.Func<System.Int32, System.Collections.Generic.IEnumerable<TResult>> selector)`
- `[ext] public static System.Collections.Generic.IEnumerable<TResult> SelectMany<TCollection, TResult>(this System.Range source, System.Func<System.Int32, System.Collections.Generic.IEnumerable<TCollection>> selector, System.Func<System.Int32, TCollection, TResult> resultSelector)`
- `[ext] public static System.Collections.Generic.IEnumerable<TResult> SelectMany<TResult>(this System.Range source, System.Func<System.Int32, System.Range> selector, System.Func<System.Int32, System.Int32, TResult> resultSelector)`
- `[ext] public static System.Collections.Generic.IEnumerable<TResult> SelectMany<TSource, TResult>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Range> selector, System.Func<TSource, System.Int32, TResult> resultSelector)`

### ResultPartitions (class [static])

- `public static Funcky.Extensions.ResultPartitions<TValidResult> Create<TValidResult>(System.Collections.Generic.IReadOnlyList<System.Exception> error, System.Collections.Generic.IReadOnlyList<TValidResult> ok)`

### ResultPartitions`1<TValidResult> (struct [readonly struct])

- `public ResultPartitions`1(System.Collections.Generic.IReadOnlyList<System.Exception> error, System.Collections.Generic.IReadOnlyList<TValidResult> ok)`
- `public System.Collections.Generic.IReadOnlyList<System.Exception> Error { get; }`
- `public System.Collections.Generic.IReadOnlyList<TValidResult> Ok { get; }`
- `public System.Void Deconstruct(out System.Collections.Generic.IReadOnlyList<System.Exception>& error, out System.Collections.Generic.IReadOnlyList<TValidResult>& ok)`

### StreamExtensions (class [static])

- `[ext] public static Funcky.Monads.Option<System.Int64> GetLengthOrNone(this System.IO.Stream stream)`
- `[ext] public static Funcky.Monads.Option<System.Int64> GetPositionOrNone(this System.IO.Stream stream)`
- `[ext] public static Funcky.Monads.Option<System.Int32> GetReadTimeoutOrNone(this System.IO.Stream stream)`
- `[ext] public static Funcky.Monads.Option<System.Int32> GetWriteTimeoutOrNone(this System.IO.Stream stream)`
- `[ext] public static Funcky.Monads.Option<System.Byte> ReadByteOrNone(this System.IO.Stream stream)`

### StringExtensions (class [static])

- `[ext] public static System.Collections.Generic.IEnumerable<System.String> Chunk(this System.String source, System.Int32 size)`
- `[ext] public static Funcky.Monads.Option<System.Int32> IndexOfAnyOrNone(this System.String haystack, System.Char[] anyOf)`
- `[ext] public static Funcky.Monads.Option<System.Int32> IndexOfAnyOrNone(this System.String haystack, System.Char[] anyOf, System.Int32 startIndex)`
- `[ext] public static Funcky.Monads.Option<System.Int32> IndexOfAnyOrNone(this System.String haystack, System.Char[] anyOf, System.Int32 startIndex, System.Int32 count)`
- `[ext] public static Funcky.Monads.Option<System.Int32> IndexOfOrNone(this System.String haystack, System.Char value)`
- `[ext] public static Funcky.Monads.Option<System.Int32> IndexOfOrNone(this System.String haystack, System.String value)`
- `[ext] public static Funcky.Monads.Option<System.Int32> IndexOfOrNone(this System.String haystack, System.Char value, System.Int32 startIndex)`
- `[ext] public static Funcky.Monads.Option<System.Int32> IndexOfOrNone(this System.String haystack, System.Char value, System.StringComparison comparisonType)`
- `[ext] public static Funcky.Monads.Option<System.Int32> IndexOfOrNone(this System.String haystack, System.String value, System.Int32 startIndex)`
- `[ext] public static Funcky.Monads.Option<System.Int32> IndexOfOrNone(this System.String haystack, System.String value, System.StringComparison comparisonType)`
- `[ext] public static Funcky.Monads.Option<System.Int32> IndexOfOrNone(this System.String haystack, System.Char value, System.Int32 startIndex, System.Int32 count)`
- `[ext] public static Funcky.Monads.Option<System.Int32> IndexOfOrNone(this System.String haystack, System.String value, System.Int32 startIndex, System.Int32 count)`
- `[ext] public static Funcky.Monads.Option<System.Int32> IndexOfOrNone(this System.String haystack, System.String value, System.Int32 startIndex, System.StringComparison comparisonType)`
- `[ext] public static Funcky.Monads.Option<System.Int32> IndexOfOrNone(this System.String haystack, System.String value, System.Int32 startIndex, System.Int32 count, System.StringComparison comparisonType)`
- `[ext] public static Funcky.Monads.Option<System.Int32> LastIndexOfAnyOrNone(this System.String haystack, System.Char[] anyOf)`
- `[ext] public static Funcky.Monads.Option<System.Int32> LastIndexOfAnyOrNone(this System.String haystack, System.Char[] anyOf, System.Int32 startIndex)`
- `[ext] public static Funcky.Monads.Option<System.Int32> LastIndexOfAnyOrNone(this System.String haystack, System.Char[] anyOf, System.Int32 startIndex, System.Int32 count)`
- `[ext] public static Funcky.Monads.Option<System.Int32> LastIndexOfOrNone(this System.String haystack, System.Char value)`
- `[ext] public static Funcky.Monads.Option<System.Int32> LastIndexOfOrNone(this System.String haystack, System.String value)`
- `[ext] public static Funcky.Monads.Option<System.Int32> LastIndexOfOrNone(this System.String haystack, System.Char value, System.Int32 startIndex)`
- `[ext] public static Funcky.Monads.Option<System.Int32> LastIndexOfOrNone(this System.String haystack, System.String value, System.Int32 startIndex)`
- `[ext] public static Funcky.Monads.Option<System.Int32> LastIndexOfOrNone(this System.String haystack, System.String value, System.StringComparison comparisonType)`
- `[ext] public static Funcky.Monads.Option<System.Int32> LastIndexOfOrNone(this System.String haystack, System.Char value, System.Int32 startIndex, System.Int32 count)`
- `[ext] public static Funcky.Monads.Option<System.Int32> LastIndexOfOrNone(this System.String haystack, System.String value, System.Int32 startIndex, System.Int32 count)`
- `[ext] public static Funcky.Monads.Option<System.Int32> LastIndexOfOrNone(this System.String haystack, System.String value, System.Int32 startIndex, System.StringComparison comparisonType)`
- `[ext] public static Funcky.Monads.Option<System.Int32> LastIndexOfOrNone(this System.String haystack, System.String value, System.Int32 startIndex, System.Int32 count, System.StringComparison comparisonType)`
- `[ext] public static System.Collections.Generic.IEnumerable<System.String> SlidingWindow(this System.String source, System.Int32 width)`
- `[ext] public static System.Collections.Generic.IEnumerable<System.String> SplitLazy(this System.String text, System.Char separator)`
- `[ext] public static System.Collections.Generic.IEnumerable<System.String> SplitLazy(this System.String text, params System.Char[] separators)`
- `[ext] public static System.Collections.Generic.IEnumerable<System.String> SplitLazy(this System.String text, System.String separator)`
- `[ext] public static System.Collections.Generic.IEnumerable<System.String> SplitLazy(this System.String text, params System.String[] separators)`
- `[ext] public static System.Collections.Generic.IEnumerable<System.String> SplitLines(this System.String text)`

### ValueWithFirst`1<TValue> (struct [readonly struct])

- `public ValueWithFirst`1(TValue? value, System.Boolean isFirst)`
- `public System.Boolean IsFirst { get; }`
- `public TValue Value { get; } (nullability: Nullable)`
- `public System.Void Deconstruct(out TValue&? value, out System.Boolean& isFirst)`

### ValueWithIndex`1<TValue> (struct [readonly struct])

- `public ValueWithIndex`1(TValue? value, System.Int32 index)`
- `public System.Int32 Index { get; }`
- `public TValue Value { get; } (nullability: Nullable)`
- `public System.Void Deconstruct(out TValue&? value, out System.Int32& index)`

### ValueWithLast`1<TValue> (struct [readonly struct])

- `public ValueWithLast`1(TValue? value, System.Boolean isLast)`
- `public System.Boolean IsLast { get; }`
- `public TValue Value { get; } (nullability: Nullable)`
- `public System.Void Deconstruct(out TValue&? value, out System.Boolean& isLast)`

### ValueWithPrevious`1<TValue> (struct [readonly struct])

- `public ValueWithPrevious`1(TValue value, Funcky.Monads.Option<TValue> previous)`
- `public Funcky.Monads.Option<TValue> Previous { get; }`
- `public TValue Value { get; }`
- `public System.Void Deconstruct(out TValue& value, out Funcky.Monads.Option<TValue>& previous)`

## Funcky.Monads

### ConfiguredOptionTaskAwaitable (struct [readonly struct])

- `public Funcky.Monads.ConfiguredOptionTaskAwaitable+ConfiguredOptionTaskAwaiter GetAwaiter()`

### ConfiguredOptionTaskAwaitable`1<TItem> (struct [readonly struct])

- `public Funcky.Monads.ConfiguredOptionTaskAwaitable<TItem> GetAwaiter()`

### ConfiguredOptionTaskAwaiter<TItem> (struct [readonly struct]) : System.Runtime.CompilerServices.INotifyCompletion

- `public System.Boolean IsCompleted { get; }`
- `public Funcky.Monads.Option<TItem> GetResult()`
- `public System.Void OnCompleted(System.Action continuation)`

### ConfiguredOptionTaskAwaiter (struct [readonly struct]) : System.Runtime.CompilerServices.INotifyCompletion

- `public System.Boolean IsCompleted { get; }`
- `public System.Void GetResult()`
- `public System.Void OnCompleted(System.Action continuation)`

### ConfiguredOptionValueTaskAwaitable (struct [readonly struct])

- `public Funcky.Monads.ConfiguredOptionValueTaskAwaitable+ConfiguredOptionValueTaskAwaiter GetAwaiter()`

### ConfiguredOptionValueTaskAwaitable`1<TItem> (struct [readonly struct])

- `public Funcky.Monads.ConfiguredOptionValueTaskAwaitable<TItem> GetAwaiter()`

### ConfiguredOptionValueTaskAwaiter<TItem> (struct [readonly struct]) : System.Runtime.CompilerServices.INotifyCompletion

- `public System.Boolean IsCompleted { get; }`
- `public Funcky.Monads.Option<TItem> GetResult()`
- `public System.Void OnCompleted(System.Action continuation)`

### ConfiguredOptionValueTaskAwaiter (struct [readonly struct]) : System.Runtime.CompilerServices.INotifyCompletion

- `public System.Boolean IsCompleted { get; }`
- `public System.Void GetResult()`
- `public System.Void OnCompleted(System.Action continuation)`

### EitherAsyncExtensions (class [static])

- `[ext] public static System.Collections.Generic.IAsyncEnumerable<Funcky.Monads.Either<TLeft, TRight>> Sequence<TLeft, TRight>(this Funcky.Monads.Either<TLeft, System.Collections.Generic.IAsyncEnumerable<TRight>> either)`
- `[ext] public static System.Threading.Tasks.Task<Funcky.Monads.Either<TLeft, TRight>> Sequence<TLeft, TRight>(this Funcky.Monads.Either<TLeft, System.Threading.Tasks.Task<TRight>> either)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Either<TLeft, TRight>> Sequence<TLeft, TRight>(this Funcky.Monads.Either<TLeft, System.Threading.Tasks.ValueTask<TRight>> either)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<Funcky.Monads.Either<TLeft, T>> Traverse<TLeft, TRight, T>(this Funcky.Monads.Either<TLeft, TRight> either, System.Func<TRight, System.Collections.Generic.IAsyncEnumerable<T>> selector)`
- `[ext] public static System.Threading.Tasks.Task<Funcky.Monads.Either<TLeft, T>> Traverse<TLeft, TRight, T>(this Funcky.Monads.Either<TLeft, TRight> either, System.Func<TRight, System.Threading.Tasks.Task<T>> selector)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Either<TLeft, T>> Traverse<TLeft, TRight, T>(this Funcky.Monads.Either<TLeft, TRight> either, System.Func<TRight, System.Threading.Tasks.ValueTask<T>> selector)`

### EitherExtensions (class [static])

- `[ext] public static Funcky.Monads.Either<TLeft, TRight> Flatten<TLeft, TRight>(this Funcky.Monads.Either<TLeft, Funcky.Monads.Either<TLeft, TRight>> either)`
- `[ext] public static Funcky.Monads.Option<TLeft> LeftOrNone<TLeft, TRight>(this Funcky.Monads.Either<TLeft, TRight> either)`
- `[ext] public static Funcky.Monads.Option<TRight> RightOrNone<TLeft, TRight>(this Funcky.Monads.Either<TLeft, TRight> either)`
- `[ext] public static Funcky.Monads.Option<Funcky.Monads.Either<TLeft, TRight>> Sequence<TLeft, TRight>(this Funcky.Monads.Either<TLeft, Funcky.Monads.Option<TRight>> either)`
- `[ext] public static Funcky.Monads.Result<Funcky.Monads.Either<TLeft, TRight>> Sequence<TLeft, TRight>(this Funcky.Monads.Either<TLeft, Funcky.Monads.Result<TRight>> either)`
- `[ext] public static System.Lazy<Funcky.Monads.Either<TLeft, TRight>> Sequence<TLeft, TRight>(this Funcky.Monads.Either<TLeft, System.Lazy<TRight>> either)`
- `[ext] public static System.Collections.Generic.IEnumerable<Funcky.Monads.Either<TLeft, TRight>> Sequence<TLeft, TRight>(this Funcky.Monads.Either<TLeft, System.Collections.Generic.IEnumerable<TRight>> either)`
- `[ext] public static Funcky.Monads.Reader<TEnvironment, Funcky.Monads.Either<TLeft, TRight>> Sequence<TLeft, TEnvironment, TRight>(this Funcky.Monads.Either<TLeft, Funcky.Monads.Reader<TEnvironment, TRight>> either)`
- `[ext] public static Funcky.Monads.Option<Funcky.Monads.Either<TLeft, TItem>> Traverse<TLeft, TRight, TItem>(this Funcky.Monads.Either<TLeft, TRight> either, System.Func<TRight, Funcky.Monads.Option<TItem>> selector)`
- `[ext] public static Funcky.Monads.Result<Funcky.Monads.Either<TLeft, TValidResult>> Traverse<TLeft, TRight, TValidResult>(this Funcky.Monads.Either<TLeft, TRight> either, System.Func<TRight, Funcky.Monads.Result<TValidResult>> selector)`
- `[ext] public static System.Lazy<Funcky.Monads.Either<TLeft, T>> Traverse<TLeft, TRight, T>(this Funcky.Monads.Either<TLeft, TRight> either, System.Func<TRight, System.Lazy<T>> selector)`
- `[ext] public static System.Collections.Generic.IEnumerable<Funcky.Monads.Either<TLeft, T>> Traverse<TLeft, TRight, T>(this Funcky.Monads.Either<TLeft, TRight> either, System.Func<TRight, System.Collections.Generic.IEnumerable<T>> selector)`
- `[ext] public static Funcky.Monads.Reader<TEnvironment, Funcky.Monads.Either<TLeft, TResult>> Traverse<TLeft, TRight, TEnvironment, TResult>(this Funcky.Monads.Either<TLeft, TRight> either, System.Func<TRight, Funcky.Monads.Reader<TEnvironment, TResult>> selector)`

### Either`1<TLeft> (class [static])

- `public static Funcky.Monads.Either<TLeft, TRight> Return<TRight>(TRight right)`

### Either`2<TLeft, TRight> (struct [readonly struct]) : System.IEquatable<Funcky.Monads.Either<TLeft, TRight>>, Funcky.Monads.IEither

- `public System.Boolean Equals(System.Object? obj)`
- `public System.Boolean Equals(Funcky.Monads.Either<TLeft, TRight> other)`
- `public Funcky.Monads.Either<TRight, TLeft> Flip()`
- `public System.Int32 GetHashCode()`
- `public TRight GetOrElse(TRight fallback)`
- `public TRight GetOrElse(System.Func<TLeft, TRight> fallback)`
- `public Funcky.Monads.Either<TLeft, TRight> Inspect(System.Action<TRight> inspector)`
- `public Funcky.Monads.Either<TLeft, TRight> InspectLeft(System.Action<TLeft> inspector)`
- `public static Funcky.Monads.Either<TLeft, TRight> Left(TLeft left)`
- `public TMatchResult Match<TMatchResult>(System.Func<TLeft, TMatchResult> left, System.Func<TRight, TMatchResult> right)`
- `public Funcky.Monads.Either<TLeft, TRight> OrElse(Funcky.Monads.Either<TLeft, TRight> fallback)`
- `public Funcky.Monads.Either<TLeft, TRight> OrElse(System.Func<TLeft, Funcky.Monads.Either<TLeft, TRight>> fallback)`
- `public static Funcky.Monads.Either<TLeft, TRight> Right(TRight right)`
- `public Funcky.Monads.Either<TLeft, TResult> Select<TResult>(System.Func<TRight, TResult> selector)`
- `public Funcky.Monads.Either<TResult, TRight> SelectLeft<TResult>(System.Func<TLeft, TResult> selector)`
- `public Funcky.Monads.Either<TLeft, TResult> SelectMany<TResult>(System.Func<TRight, Funcky.Monads.Either<TLeft, TResult>> selector)`
- `public Funcky.Monads.Either<TLeft, TResult> SelectMany<TEither, TResult>(System.Func<TRight, Funcky.Monads.Either<TLeft, TEither>> selector, System.Func<TRight, TEither, TResult> resultSelector)`
- `public System.Void Switch(System.Action<TLeft> left, System.Action<TRight> right)`
- `public System.String ToString()`
- `public static System.Boolean op_Equality(Funcky.Monads.Either<TLeft, TRight> left, Funcky.Monads.Either<TLeft, TRight> right)`
- `public static Funcky.Monads.Either<TLeft, TRight> op_Implicit(TRight right)`
- `public static System.Boolean op_Inequality(Funcky.Monads.Either<TLeft, TRight> left, Funcky.Monads.Either<TLeft, TRight> right)`

### IEither (interface)


### IOption (interface)


### Lazy (class [static])

- `public static System.Lazy<T> FromFunc<T>(System.Func<T> valueFactory)`
- `public static System.Lazy<T> Return<T>(T? value)`

### LazyExtensions (class [static])

- `[ext] public static System.Lazy<T> Flatten<T>(this System.Lazy<System.Lazy<T>> lazy)`
- `[ext] public static System.Lazy<TResult> Select<T, TResult>(this System.Lazy<T> lazy, System.Func<T, TResult> selector)`
- `[ext] public static System.Lazy<TResult> SelectMany<T, TResult>(this System.Lazy<T> lazy, System.Func<T, System.Lazy<TResult>> selector)`
- `[ext] public static System.Lazy<TResult> SelectMany<T, TLazy, TResult>(this System.Lazy<T> lazy, System.Func<T, System.Lazy<TLazy>> selector, System.Func<T, TLazy, TResult> resultSelector)`

### Option (class [static])

- `public static Funcky.Monads.Option<Funcky.Unit> FromBoolean(System.Boolean boolean)`
- `public static Funcky.Monads.Option<TItem> FromBoolean<TItem>(System.Boolean boolean, TItem item)`
- `public static Funcky.Monads.Option<TItem> FromBoolean<TItem>(System.Boolean boolean, System.Func<TItem> selector)`
- `public static Funcky.Monads.Option<TItem> FromNullable<TItem>(TItem? item)`
- `where TItem : class`
- `public static Funcky.Monads.Option<TItem> FromNullable<TItem>(TItem?? item)`
- `where TItem : struct`
- `public static Funcky.Monads.Option<TItem> Return<TItem>(TItem item)`
- `public static Funcky.Monads.Option<TItem> Some<TItem>(TItem item)`

### OptionAsyncExtensions (class [static])

- `[ext] public static Funcky.Monads.ConfiguredOptionTaskAwaitable ConfigureAwait(this Funcky.Monads.Option<System.Threading.Tasks.Task> option, System.Boolean continueOnCapturedContext)`
- `[ext] public static Funcky.Monads.ConfiguredOptionTaskAwaitable<TItem> ConfigureAwait<TItem>(this Funcky.Monads.Option<System.Threading.Tasks.Task<TItem>> option, System.Boolean continueOnCapturedContext)`
- `[ext] public static Funcky.Monads.ConfiguredOptionValueTaskAwaitable ConfigureAwait(this Funcky.Monads.Option<System.Threading.Tasks.ValueTask> option, System.Boolean continueOnCapturedContext)`
- `[ext] public static Funcky.Monads.ConfiguredOptionValueTaskAwaitable<TItem> ConfigureAwait<TItem>(this Funcky.Monads.Option<System.Threading.Tasks.ValueTask<TItem>> option, System.Boolean continueOnCapturedContext)`
- `[ext] public static Funcky.Monads.OptionTaskAwaiter GetAwaiter(this Funcky.Monads.Option<System.Threading.Tasks.Task> option)`
- `[ext] public static Funcky.Monads.OptionTaskAwaiter<TItem> GetAwaiter<TItem>(this Funcky.Monads.Option<System.Threading.Tasks.Task<TItem>> option)`
- `[ext] public static Funcky.Monads.OptionValueTaskAwaiter GetAwaiter(this Funcky.Monads.Option<System.Threading.Tasks.ValueTask> option)`
- `[ext] public static Funcky.Monads.OptionValueTaskAwaiter<TItem> GetAwaiter<TItem>(this Funcky.Monads.Option<System.Threading.Tasks.ValueTask<TItem>> option)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<Funcky.Monads.Option<TItem>> Sequence<TItem>(this Funcky.Monads.Option<System.Collections.Generic.IAsyncEnumerable<TItem>> option)`
- `[ext] public static System.Threading.Tasks.Task<Funcky.Monads.Option<TItem>> Sequence<TItem>(this Funcky.Monads.Option<System.Threading.Tasks.Task<TItem>> option)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<TItem>> Sequence<TItem>(this Funcky.Monads.Option<System.Threading.Tasks.ValueTask<TItem>> option)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<TItem> ToAsyncEnumerable<TItem>(this Funcky.Monads.Option<TItem> option)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<Funcky.Monads.Option<T>> Traverse<TItem, T>(this Funcky.Monads.Option<TItem> option, System.Func<TItem, System.Collections.Generic.IAsyncEnumerable<T>> selector)`
- `[ext] public static System.Threading.Tasks.Task<Funcky.Monads.Option<T>> Traverse<TItem, T>(this Funcky.Monads.Option<TItem> option, System.Func<TItem, System.Threading.Tasks.Task<T>> selector)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Option<T>> Traverse<TItem, T>(this Funcky.Monads.Option<TItem> option, System.Func<TItem, System.Threading.Tasks.ValueTask<T>> selector)`

### OptionComparer (class [static])

- `public static System.Collections.Generic.Comparer<Funcky.Monads.Option<TItem>> Create<TItem>(System.Collections.Generic.IComparer<TItem> comparer)`

### OptionComparer`1<TItem> (class [static])

- `public static System.Collections.Generic.Comparer<Funcky.Monads.Option<TItem>> Default { get; }`
- `public static System.Collections.Generic.Comparer<Funcky.Monads.Option<TItem>> Create(System.Comparison<TItem> comparison)`

### OptionEqualityComparer (class [static])

- `public static System.Collections.Generic.EqualityComparer<Funcky.Monads.Option<TItem>> Create<TItem>(System.Collections.Generic.IEqualityComparer<TItem> comparer)`

### OptionEqualityComparer`1<TItem> (class [static])

- `public static System.Collections.Generic.EqualityComparer<Funcky.Monads.Option<TItem>> Default { get; }`

### OptionExtensions (class [static])

- `[ext] public static Funcky.Monads.Option<T> Flatten<T>(this Funcky.Monads.Option<Funcky.Monads.Option<T>> option)`
- `[ext] public static Funcky.Monads.Either<TLeft, Funcky.Monads.Option<TItem>> Sequence<TLeft, TItem>(this Funcky.Monads.Option<Funcky.Monads.Either<TLeft, TItem>> option)`
- `[ext] public static Funcky.Monads.Result<Funcky.Monads.Option<TItem>> Sequence<TItem>(this Funcky.Monads.Option<Funcky.Monads.Result<TItem>> option)`
- `[ext] public static System.Lazy<Funcky.Monads.Option<TItem>> Sequence<TItem>(this Funcky.Monads.Option<System.Lazy<TItem>> option)`
- `[ext] public static System.Collections.Generic.IEnumerable<Funcky.Monads.Option<TItem>> Sequence<TItem>(this Funcky.Monads.Option<System.Collections.Generic.IEnumerable<TItem>> option)`
- `[ext] public static Funcky.Monads.Reader<TEnvironment, Funcky.Monads.Option<TItem>> Sequence<TEnvironment, TItem>(this Funcky.Monads.Option<Funcky.Monads.Reader<TEnvironment, TItem>> option)`
- `[ext] public static Funcky.Monads.Either<TLeft, TRight> ToEither<TLeft, TRight>(this Funcky.Monads.Option<TRight> option, TLeft left)`
- `[ext] public static Funcky.Monads.Either<TLeft, TRight> ToEither<TLeft, TRight>(this Funcky.Monads.Option<TRight> option, System.Func<TLeft> left)`
- `[ext] public static TItem? ToNullable<TItem>(this Funcky.Monads.Option<TItem> option, Funcky.RequireStruct<TItem>? ω)`
- `where TItem : struct`
- `[ext] public static TItem ToNullable<TItem>(this Funcky.Monads.Option<TItem> option, Funcky.RequireClass<TItem>? ω)`
- `where TItem : class`
- `[ext] public static Funcky.Monads.Either<TLeft, Funcky.Monads.Option<TRight>> Traverse<TItem, TLeft, TRight>(this Funcky.Monads.Option<TItem> option, System.Func<TItem, Funcky.Monads.Either<TLeft, TRight>> selector)`
- `[ext] public static Funcky.Monads.Result<Funcky.Monads.Option<TValidResult>> Traverse<TItem, TValidResult>(this Funcky.Monads.Option<TItem> option, System.Func<TItem, Funcky.Monads.Result<TValidResult>> selector)`
- `[ext] public static System.Lazy<Funcky.Monads.Option<T>> Traverse<TItem, T>(this Funcky.Monads.Option<TItem> option, System.Func<TItem, System.Lazy<T>> selector)`
- `[ext] public static System.Collections.Generic.IEnumerable<Funcky.Monads.Option<T>> Traverse<TItem, T>(this Funcky.Monads.Option<TItem> option, System.Func<TItem, System.Collections.Generic.IEnumerable<T>> selector)`
- `[ext] public static Funcky.Monads.Reader<TEnvironment, Funcky.Monads.Option<TResult>> Traverse<TItem, TEnvironment, TResult>(this Funcky.Monads.Option<TItem> option, System.Func<TItem, Funcky.Monads.Reader<TEnvironment, TResult>> selector)`

### OptionJsonConverter (class [sealed]) : System.Text.Json.Serialization.JsonConverterFactory

- `public OptionJsonConverter()`
- `public System.Boolean CanConvert(System.Type typeToConvert)`
- `public System.Text.Json.Serialization.JsonConverter CreateConverter(System.Type typeToConvert, System.Text.Json.JsonSerializerOptions options)`

### OptionTaskAwaiter (struct [readonly struct]) : System.Runtime.CompilerServices.INotifyCompletion

- `public System.Boolean IsCompleted { get; }`
- `public System.Void GetResult()`
- `public System.Void OnCompleted(System.Action continuation)`

### OptionTaskAwaiter`1<TItem> (struct [readonly struct]) : System.Runtime.CompilerServices.INotifyCompletion

- `public System.Boolean IsCompleted { get; }`
- `public Funcky.Monads.Option<TItem> GetResult()`
- `public System.Void OnCompleted(System.Action continuation)`

### OptionValueTaskAwaiter (struct [readonly struct]) : System.Runtime.CompilerServices.INotifyCompletion

- `public System.Boolean IsCompleted { get; }`
- `public System.Void GetResult()`
- `public System.Void OnCompleted(System.Action continuation)`

### OptionValueTaskAwaiter`1<TItem> (struct [readonly struct]) : System.Runtime.CompilerServices.INotifyCompletion

- `public System.Boolean IsCompleted { get; }`
- `public Funcky.Monads.Option<TItem> GetResult()`
- `public System.Void OnCompleted(System.Action continuation)`

### Option`1<TItem> (struct [readonly struct]) : System.IComparable<Funcky.Monads.Option<TItem>>, System.IComparable, Funcky.Monads.IOption, System.IEquatable<Funcky.Monads.Option<TItem>>

- `public System.Int32 Count { get; }`
- `public TItem Item(System.Int32 index) { get; }`
- `public static Funcky.Monads.Option<TItem> None { get; }`
- `public Funcky.Monads.Option<TResult> AndThen<TResult>(System.Func<TItem, TResult> selector)`
- `public Funcky.Monads.Option<TResult> AndThen<TResult>(System.Func<TItem, Funcky.Monads.Option<TResult>> selector)`
- `public System.Void AndThen(System.Action<TItem> action)`
- `public System.Int32 CompareTo(System.Object? obj)`
- `public System.Int32 CompareTo(Funcky.Monads.Option<TItem> other)`
- `public System.Boolean Equals(System.Object? obj)`
- `public System.Boolean Equals(Funcky.Monads.Option<TItem> other)`
- `public System.Int32 GetHashCode()`
- `public TItem GetOrElse(TItem fallback)`
- `public TItem GetOrElse(System.Func<TItem> fallback)`
- `public Funcky.Monads.Option<TItem> Inspect(System.Action<TItem> inspector)`
- `public Funcky.Monads.Option<TItem> InspectNone(System.Action inspector)`
- `public TResult Match<TResult>(TResult? none, System.Func<TItem, TResult> some)`
- `public TResult Match<TResult>(System.Func<TResult> none, System.Func<TItem, TResult> some)`
- `public Funcky.Monads.Option<TItem> OrElse(Funcky.Monads.Option<TItem> fallback)`
- `public Funcky.Monads.Option<TItem> OrElse(System.Func<Funcky.Monads.Option<TItem>> fallback)`
- `public Funcky.Monads.Option<TResult> Select<TResult>(System.Func<TItem, TResult> selector)`
- `public Funcky.Monads.Option<TResult> SelectMany<TResult>(System.Func<TItem, Funcky.Monads.Option<TResult>> selector)`
- `public Funcky.Monads.Option<TResult> SelectMany<TOption, TResult>(System.Func<TItem, Funcky.Monads.Option<TOption>> selector, System.Func<TItem, TOption, TResult> resultSelector)`
- `public System.Void Switch(System.Action none, System.Action<TItem> some)`
- `public System.Collections.Generic.IEnumerable<TItem> ToEnumerable()`
- `public System.String ToString()`
- `public System.Boolean TryGetValue(out TItem&? item)`
- `public Funcky.Monads.Option<TItem> Where(System.Func<TItem, System.Boolean> predicate)`
- `public static System.Boolean op_Equality(Funcky.Monads.Option<TItem> left, Funcky.Monads.Option<TItem> right)`
- `public static System.Boolean op_GreaterThan(Funcky.Monads.Option<TItem> left, Funcky.Monads.Option<TItem> right)`
- `public static System.Boolean op_GreaterThanOrEqual(Funcky.Monads.Option<TItem> left, Funcky.Monads.Option<TItem> right)`
- `public static Funcky.Monads.Option<TItem> op_Implicit(TItem item)`
- `public static System.Boolean op_Inequality(Funcky.Monads.Option<TItem> left, Funcky.Monads.Option<TItem> right)`
- `public static System.Boolean op_LessThan(Funcky.Monads.Option<TItem> left, Funcky.Monads.Option<TItem> right)`
- `public static System.Boolean op_LessThanOrEqual(Funcky.Monads.Option<TItem> left, Funcky.Monads.Option<TItem> right)`

### ReaderExtensions (class [static])

- `[ext] public static Funcky.Monads.Reader<TEnvironment, TItem> Flatten<TEnvironment, TItem>(this Funcky.Monads.Reader<TEnvironment, Funcky.Monads.Reader<TEnvironment, TItem>> reader)`
- `[ext] public static Funcky.Monads.Reader<TEnvironment, TResult> Select<TEnvironment, TSource, TResult>(this Funcky.Monads.Reader<TEnvironment, TSource> source, System.Func<TSource, TResult> selector)`
- `[ext] public static Funcky.Monads.Reader<TEnvironment, TResult> SelectMany<TEnvironment, TSource, TResult>(this Funcky.Monads.Reader<TEnvironment, TSource> source, System.Func<TSource, Funcky.Monads.Reader<TEnvironment, TResult>> selector)`
- `[ext] public static Funcky.Monads.Reader<TEnvironment, TResult> SelectMany<TEnvironment, TSource, TReader, TResult>(this Funcky.Monads.Reader<TEnvironment, TSource> source, System.Func<TSource, Funcky.Monads.Reader<TEnvironment, TReader>> selector, System.Func<TSource, TReader, TResult> resultSelector)`

### Reader`1<TEnvironment> (class [static])

- `public static Funcky.Monads.Reader<TEnvironment, Funcky.Unit> FromAction(System.Action<TEnvironment> action)`
- `public static Funcky.Monads.Reader<TEnvironment, TResult> FromFunc<TResult>(System.Func<TEnvironment, TResult> function)`
- `public static Funcky.Monads.Reader<TEnvironment, TResult> Return<TResult>(TResult value)`

### Reader`2<TEnvironment, TResult> (delegate [sealed]) : System.ICloneable, System.Runtime.Serialization.ISerializable

- `public Reader`2(System.Object object, System.IntPtr method)`
- `delegate TResult Invoke(TEnvironment environment)`
- `public System.IAsyncResult BeginInvoke(TEnvironment environment, System.AsyncCallback callback, System.Object object)`
- `public TResult EndInvoke(System.IAsyncResult result)`

### Result (class [static])

- `public static Funcky.Monads.Result<TValidResult> Ok<TValidResult>(TValidResult result)`
- `public static Funcky.Monads.Result<TValidResult> Return<TValidResult>(TValidResult result)`

### ResultAsyncExtensions (class [static])

- `[ext] public static System.Collections.Generic.IAsyncEnumerable<Funcky.Monads.Result<TValidResult>> Sequence<TValidResult>(this Funcky.Monads.Result<System.Collections.Generic.IAsyncEnumerable<TValidResult>> result)`
- `[ext] public static System.Threading.Tasks.Task<Funcky.Monads.Result<TValidResult>> Sequence<TValidResult>(this Funcky.Monads.Result<System.Threading.Tasks.Task<TValidResult>> result)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Result<TValidResult>> Sequence<TValidResult>(this Funcky.Monads.Result<System.Threading.Tasks.ValueTask<TValidResult>> result)`
- `[ext] public static System.Collections.Generic.IAsyncEnumerable<Funcky.Monads.Result<T>> Traverse<TValidResult, T>(this Funcky.Monads.Result<TValidResult> result, System.Func<TValidResult, System.Collections.Generic.IAsyncEnumerable<T>> selector)`
- `[ext] public static System.Threading.Tasks.Task<Funcky.Monads.Result<T>> Traverse<TValidResult, T>(this Funcky.Monads.Result<TValidResult> result, System.Func<TValidResult, System.Threading.Tasks.Task<T>> selector)`
- `[ext] public static System.Threading.Tasks.ValueTask<Funcky.Monads.Result<T>> Traverse<TValidResult, T>(this Funcky.Monads.Result<TValidResult> result, System.Func<TValidResult, System.Threading.Tasks.ValueTask<T>> selector)`

### ResultExtensions (class [static])

- `[ext] public static Funcky.Monads.Result<T> Flatten<T>(this Funcky.Monads.Result<Funcky.Monads.Result<T>> result)`
- `[ext] public static Funcky.Monads.Either<TLeft, Funcky.Monads.Result<TValidResult>> Sequence<TLeft, TValidResult>(this Funcky.Monads.Result<Funcky.Monads.Either<TLeft, TValidResult>> result)`
- `[ext] public static Funcky.Monads.Option<Funcky.Monads.Result<TValidResult>> Sequence<TValidResult>(this Funcky.Monads.Result<Funcky.Monads.Option<TValidResult>> result)`
- `[ext] public static System.Lazy<Funcky.Monads.Result<TValidResult>> Sequence<TValidResult>(this Funcky.Monads.Result<System.Lazy<TValidResult>> result)`
- `[ext] public static System.Collections.Generic.IEnumerable<Funcky.Monads.Result<TValidResult>> Sequence<TValidResult>(this Funcky.Monads.Result<System.Collections.Generic.IEnumerable<TValidResult>> result)`
- `[ext] public static Funcky.Monads.Reader<TEnvironment, Funcky.Monads.Result<TValidResult>> Sequence<TEnvironment, TValidResult>(this Funcky.Monads.Result<Funcky.Monads.Reader<TEnvironment, TValidResult>> result)`
- `[ext] public static Funcky.Monads.Either<TLeft, Funcky.Monads.Result<TRight>> Traverse<TValidResult, TLeft, TRight>(this Funcky.Monads.Result<TValidResult> result, System.Func<TValidResult, Funcky.Monads.Either<TLeft, TRight>> selector)`
- `[ext] public static Funcky.Monads.Option<Funcky.Monads.Result<TItem>> Traverse<TValidResult, TItem>(this Funcky.Monads.Result<TValidResult> result, System.Func<TValidResult, Funcky.Monads.Option<TItem>> selector)`
- `[ext] public static System.Lazy<Funcky.Monads.Result<T>> Traverse<TValidResult, T>(this Funcky.Monads.Result<TValidResult> result, System.Func<TValidResult, System.Lazy<T>> selector)`
- `[ext] public static System.Collections.Generic.IEnumerable<Funcky.Monads.Result<T>> Traverse<TValidResult, T>(this Funcky.Monads.Result<TValidResult> result, System.Func<TValidResult, System.Collections.Generic.IEnumerable<T>> selector)`
- `[ext] public static Funcky.Monads.Reader<TEnvironment, Funcky.Monads.Result<TResult>> Traverse<TValidResult, TEnvironment, TResult>(this Funcky.Monads.Result<TValidResult> result, System.Func<TValidResult, Funcky.Monads.Reader<TEnvironment, TResult>> selector)`

### Result`1<TValidResult> (struct [readonly struct]) : System.IEquatable<Funcky.Monads.Result<TValidResult>>

- `public System.Boolean Equals(System.Object? obj)`
- `public System.Boolean Equals(Funcky.Monads.Result<TValidResult> other)`
- `public static Funcky.Monads.Result<TValidResult> Error(System.Exception exception)`
- `public System.Int32 GetHashCode()`
- `public TValidResult GetOrElse(TValidResult fallback)`
- `public TValidResult GetOrElse(System.Func<System.Exception, TValidResult> fallback)`
- `public TValidResult GetOrThrow()`
- `public Funcky.Monads.Result<TValidResult> Inspect(System.Action<TValidResult> inspector)`
- `public Funcky.Monads.Result<TValidResult> InspectError(System.Action<System.Exception> inspector)`
- `public TMatchResult Match<TMatchResult>(System.Func<TValidResult, TMatchResult> ok, System.Func<System.Exception, TMatchResult> error)`
- `public Funcky.Monads.Result<TValidResult> OrElse(Funcky.Monads.Result<TValidResult> fallback)`
- `public Funcky.Monads.Result<TValidResult> OrElse(System.Func<System.Exception, Funcky.Monads.Result<TValidResult>> fallback)`
- `public Funcky.Monads.Result<TResult> Select<TResult>(System.Func<TValidResult, TResult> selector)`
- `public Funcky.Monads.Result<TResult> SelectMany<TResult>(System.Func<TValidResult, Funcky.Monads.Result<TResult>> selector)`
- `public Funcky.Monads.Result<TResult> SelectMany<TSelectedResult, TResult>(System.Func<TValidResult, Funcky.Monads.Result<TSelectedResult>> selector, System.Func<TValidResult, TSelectedResult, TResult> resultSelector)`
- `public System.Void Switch(System.Action<TValidResult> ok, System.Action<System.Exception> error)`
- `public System.String ToString()`
- `public static System.Boolean op_Equality(Funcky.Monads.Result<TValidResult> left, Funcky.Monads.Result<TValidResult> right)`
- `public static Funcky.Monads.Result<TValidResult> op_Implicit(TValidResult result)`
- `public static System.Boolean op_Inequality(Funcky.Monads.Result<TValidResult> left, Funcky.Monads.Result<TValidResult> right)`

## Funcky.RetryPolicies

### ConstantDelayPolicy (class) : Funcky.RetryPolicies.IRetryPolicy

- `public ConstantDelayPolicy(System.Int32 maxRetries, System.TimeSpan delay)`
- `public System.Int32 MaxRetries { get; }`
- `public System.TimeSpan Delay(System.Int32 retryCount)`

### DoNotRetryPolicy (class [sealed]) : Funcky.RetryPolicies.IRetryPolicy

- `public DoNotRetryPolicy()`
- `public System.Int32 MaxRetries { get; }`
- `public System.TimeSpan Delay(System.Int32 retryCount)`

### ExponentialBackOffRetryPolicy (class [sealed]) : Funcky.RetryPolicies.IRetryPolicy

- `public ExponentialBackOffRetryPolicy(System.Int32 maxRetries, System.TimeSpan firstDelay)`
- `public System.Int32 MaxRetries { get; }`
- `public System.TimeSpan Delay(System.Int32 retryCount)`

### IRetryPolicy (interface)

- `public System.Int32 MaxRetries { get; }`
- `public System.TimeSpan Delay(System.Int32 retryCount)`

### LinearBackOffRetryPolicy (class [sealed]) : Funcky.RetryPolicies.IRetryPolicy

- `public LinearBackOffRetryPolicy(System.Int32 maxRetries, System.TimeSpan firstDelay)`
- `public System.Int32 MaxRetries { get; }`
- `public System.TimeSpan Delay(System.Int32 retryCount)`

### NoDelayRetryPolicy (class [sealed]) : Funcky.RetryPolicies.ConstantDelayPolicy, Funcky.RetryPolicies.IRetryPolicy

- `public NoDelayRetryPolicy(System.Int32 maxRetries)`

