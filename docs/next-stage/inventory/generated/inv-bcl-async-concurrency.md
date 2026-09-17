# Public API inventory: bcl-async-concurrency

Assemblies: System.Runtime 10.0.0.0, System.Threading.Channels 10.0.0.0, System.Threading.Tasks.Parallel 10.0.0.0, System.Threading.Tasks 10.0.0.0, System.Threading 10.0.0.0

Type count: 86

## System

### IAsyncDisposable (interface)

- `public System.Threading.Tasks.ValueTask DisposeAsync()`

### IDisposable (interface)

- `public System.Void Dispose()`

### OperationCanceledException (class) : System.SystemException, System.Runtime.Serialization.ISerializable

- `public OperationCanceledException()`
- `public OperationCanceledException(System.String message)`
- `public OperationCanceledException(System.Threading.CancellationToken token)`
- `public OperationCanceledException(System.String message, System.Exception innerException)`
- `public OperationCanceledException(System.String message, System.Threading.CancellationToken token)`
- `public OperationCanceledException(System.String message, System.Exception innerException, System.Threading.CancellationToken token)`
- `public System.Threading.CancellationToken CancellationToken { get; }`

### TimeProvider (class [abstract])

- `public System.TimeZoneInfo LocalTimeZone { get; }`
- `public static System.TimeProvider System { get; }`
- `public System.Int64 TimestampFrequency { get; }`
- `public System.Threading.ITimer CreateTimer(System.Threading.TimerCallback callback, System.Object state, System.TimeSpan dueTime, System.TimeSpan period)`
- `public System.TimeSpan GetElapsedTime(System.Int64 startingTimestamp)`
- `public System.TimeSpan GetElapsedTime(System.Int64 startingTimestamp, System.Int64 endingTimestamp)`
- `public System.DateTimeOffset GetLocalNow()`
- `public System.Int64 GetTimestamp()`
- `public System.DateTimeOffset GetUtcNow()`

### TimeoutException (class) : System.SystemException, System.Runtime.Serialization.ISerializable

- `public TimeoutException()`
- `public TimeoutException(System.String message)`
- `public TimeoutException(System.String message, System.Exception innerException)`

## System.Runtime.CompilerServices

### AsyncIteratorMethodBuilder (struct)

- `public System.Void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter& awaiter, ref TStateMachine& stateMachine)`
- `where TAwaiter : System.Runtime.CompilerServices.INotifyCompletion`
- `where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine`
- `public System.Void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter& awaiter, ref TStateMachine& stateMachine)`
- `where TAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion`
- `where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine`
- `public System.Void Complete()`
- `public static System.Runtime.CompilerServices.AsyncIteratorMethodBuilder Create()`
- `public System.Void MoveNext<TStateMachine>(ref TStateMachine& stateMachine)`
- `where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine`

### AsyncMethodBuilderAttribute (class [sealed]) : System.Attribute

- `public AsyncMethodBuilderAttribute(System.Type builderType)`
- `public System.Type BuilderType { get; }`

### AsyncTaskMethodBuilder (struct)

- `public System.Threading.Tasks.Task Task { get; }`
- `public System.Void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter& awaiter, ref TStateMachine& stateMachine)`
- `where TAwaiter : System.Runtime.CompilerServices.INotifyCompletion`
- `where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine`
- `public System.Void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter& awaiter, ref TStateMachine& stateMachine)`
- `where TAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion`
- `where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine`
- `public static System.Runtime.CompilerServices.AsyncTaskMethodBuilder Create()`
- `public System.Void SetException(System.Exception exception)`
- `public System.Void SetResult()`
- `public System.Void SetStateMachine(System.Runtime.CompilerServices.IAsyncStateMachine stateMachine)`
- `public System.Void Start<TStateMachine>(ref TStateMachine& stateMachine)`
- `where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine`

### AsyncTaskMethodBuilder`1<TResult> (struct)

- `public System.Threading.Tasks.Task<TResult> Task { get; }`
- `public System.Void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter& awaiter, ref TStateMachine& stateMachine)`
- `where TAwaiter : System.Runtime.CompilerServices.INotifyCompletion`
- `where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine`
- `public System.Void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter& awaiter, ref TStateMachine& stateMachine)`
- `where TAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion`
- `where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine`
- `public static System.Runtime.CompilerServices.AsyncTaskMethodBuilder<TResult> Create()`
- `public System.Void SetException(System.Exception exception)`
- `public System.Void SetResult(TResult result)`
- `public System.Void SetStateMachine(System.Runtime.CompilerServices.IAsyncStateMachine stateMachine)`
- `public System.Void Start<TStateMachine>(ref TStateMachine& stateMachine)`
- `where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine`

### AsyncValueTaskMethodBuilder (struct)

- `public System.Threading.Tasks.ValueTask Task { get; }`
- `public System.Void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter& awaiter, ref TStateMachine& stateMachine)`
- `where TAwaiter : System.Runtime.CompilerServices.INotifyCompletion`
- `where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine`
- `public System.Void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter& awaiter, ref TStateMachine& stateMachine)`
- `where TAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion`
- `where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine`
- `public static System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder Create()`
- `public System.Void SetException(System.Exception exception)`
- `public System.Void SetResult()`
- `public System.Void SetStateMachine(System.Runtime.CompilerServices.IAsyncStateMachine stateMachine)`
- `public System.Void Start<TStateMachine>(ref TStateMachine& stateMachine)`
- `where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine`

### AsyncValueTaskMethodBuilder`1<TResult> (struct)

- `public System.Threading.Tasks.ValueTask<TResult> Task { get; }`
- `public System.Void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter& awaiter, ref TStateMachine& stateMachine)`
- `where TAwaiter : System.Runtime.CompilerServices.INotifyCompletion`
- `where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine`
- `public System.Void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter& awaiter, ref TStateMachine& stateMachine)`
- `where TAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion`
- `where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine`
- `public static System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder<TResult> Create()`
- `public System.Void SetException(System.Exception exception)`
- `public System.Void SetResult(TResult result)`
- `public System.Void SetStateMachine(System.Runtime.CompilerServices.IAsyncStateMachine stateMachine)`
- `public System.Void Start<TStateMachine>(ref TStateMachine& stateMachine)`
- `where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine`

### ConfiguredTaskAwaitable (struct [readonly struct])

- `public System.Runtime.CompilerServices.ConfiguredTaskAwaitable+ConfiguredTaskAwaiter GetAwaiter()`

### ConfiguredTaskAwaitable`1<TResult> (struct [readonly struct])

- `public System.Runtime.CompilerServices.ConfiguredTaskAwaitable<TResult> GetAwaiter()`

### ConfiguredTaskAwaiter (struct [readonly struct]) : System.Runtime.CompilerServices.ICriticalNotifyCompletion, System.Runtime.CompilerServices.INotifyCompletion

- `public System.Boolean IsCompleted { get; }`
- `public System.Void GetResult()`
- `public System.Void OnCompleted(System.Action continuation)`
- `public System.Void UnsafeOnCompleted(System.Action continuation)`

### ConfiguredTaskAwaiter<TResult> (struct [readonly struct]) : System.Runtime.CompilerServices.ICriticalNotifyCompletion, System.Runtime.CompilerServices.INotifyCompletion

- `public System.Boolean IsCompleted { get; }`
- `public TResult GetResult()`
- `public System.Void OnCompleted(System.Action continuation)`
- `public System.Void UnsafeOnCompleted(System.Action continuation)`

### ConfiguredValueTaskAwaitable (struct [readonly struct])

- `public System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable+ConfiguredValueTaskAwaiter GetAwaiter()`

### ConfiguredValueTaskAwaitable`1<TResult> (struct [readonly struct])

- `public System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable<TResult> GetAwaiter()`

### ConfiguredValueTaskAwaiter (struct [readonly struct]) : System.Runtime.CompilerServices.ICriticalNotifyCompletion, System.Runtime.CompilerServices.INotifyCompletion

- `public System.Boolean IsCompleted { get; }`
- `public System.Void GetResult()`
- `public System.Void OnCompleted(System.Action continuation)`
- `public System.Void UnsafeOnCompleted(System.Action continuation)`

### ConfiguredValueTaskAwaiter<TResult> (struct [readonly struct]) : System.Runtime.CompilerServices.ICriticalNotifyCompletion, System.Runtime.CompilerServices.INotifyCompletion

- `public System.Boolean IsCompleted { get; }`
- `public TResult GetResult()`
- `public System.Void OnCompleted(System.Action continuation)`
- `public System.Void UnsafeOnCompleted(System.Action continuation)`

### EnumeratorCancellationAttribute (class [sealed]) : System.Attribute

- `public EnumeratorCancellationAttribute()`

### IAsyncStateMachine (interface)

- `public System.Void MoveNext()`
- `public System.Void SetStateMachine(System.Runtime.CompilerServices.IAsyncStateMachine stateMachine)`

### ICriticalNotifyCompletion (interface) : System.Runtime.CompilerServices.INotifyCompletion

- `public System.Void UnsafeOnCompleted(System.Action continuation)`

### INotifyCompletion (interface)

- `public System.Void OnCompleted(System.Action continuation)`

### PoolingAsyncValueTaskMethodBuilder (struct)

- `public System.Threading.Tasks.ValueTask Task { get; }`
- `public System.Void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter& awaiter, ref TStateMachine& stateMachine)`
- `where TAwaiter : System.Runtime.CompilerServices.INotifyCompletion`
- `where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine`
- `public System.Void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter& awaiter, ref TStateMachine& stateMachine)`
- `where TAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion`
- `where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine`
- `public static System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder Create()`
- `public System.Void SetException(System.Exception exception)`
- `public System.Void SetResult()`
- `public System.Void SetStateMachine(System.Runtime.CompilerServices.IAsyncStateMachine stateMachine)`
- `public System.Void Start<TStateMachine>(ref TStateMachine& stateMachine)`
- `where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine`

### PoolingAsyncValueTaskMethodBuilder`1<TResult> (struct)

- `public System.Threading.Tasks.ValueTask<TResult> Task { get; }`
- `public System.Void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter& awaiter, ref TStateMachine& stateMachine)`
- `where TAwaiter : System.Runtime.CompilerServices.INotifyCompletion`
- `where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine`
- `public System.Void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter& awaiter, ref TStateMachine& stateMachine)`
- `where TAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion`
- `where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine`
- `public static System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder<TResult> Create()`
- `public System.Void SetException(System.Exception exception)`
- `public System.Void SetResult(TResult result)`
- `public System.Void SetStateMachine(System.Runtime.CompilerServices.IAsyncStateMachine stateMachine)`
- `public System.Void Start<TStateMachine>(ref TStateMachine& stateMachine)`
- `where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine`

### TaskAwaiter (struct [readonly struct]) : System.Runtime.CompilerServices.ICriticalNotifyCompletion, System.Runtime.CompilerServices.INotifyCompletion

- `public System.Boolean IsCompleted { get; }`
- `public System.Void GetResult()`
- `public System.Void OnCompleted(System.Action continuation)`
- `public System.Void UnsafeOnCompleted(System.Action continuation)`

### TaskAwaiter`1<TResult> (struct [readonly struct]) : System.Runtime.CompilerServices.ICriticalNotifyCompletion, System.Runtime.CompilerServices.INotifyCompletion

- `public System.Boolean IsCompleted { get; }`
- `public TResult GetResult()`
- `public System.Void OnCompleted(System.Action continuation)`
- `public System.Void UnsafeOnCompleted(System.Action continuation)`

### ValueTaskAwaiter (struct [readonly struct]) : System.Runtime.CompilerServices.ICriticalNotifyCompletion, System.Runtime.CompilerServices.INotifyCompletion

- `public System.Boolean IsCompleted { get; }`
- `public System.Void GetResult()`
- `public System.Void OnCompleted(System.Action continuation)`
- `public System.Void UnsafeOnCompleted(System.Action continuation)`

### ValueTaskAwaiter`1<TResult> (struct [readonly struct]) : System.Runtime.CompilerServices.ICriticalNotifyCompletion, System.Runtime.CompilerServices.INotifyCompletion

- `public System.Boolean IsCompleted { get; }`
- `public TResult GetResult()`
- `public System.Void OnCompleted(System.Action continuation)`
- `public System.Void UnsafeOnCompleted(System.Action continuation)`

## System.Threading

### Barrier (class) : System.IDisposable

- `public Barrier(System.Int32 participantCount)`
- `public Barrier(System.Int32 participantCount, System.Action<System.Threading.Barrier> postPhaseAction)`
- `public System.Int64 CurrentPhaseNumber { get; }`
- `public System.Int32 ParticipantCount { get; }`
- `public System.Int32 ParticipantsRemaining { get; }`
- `public System.Int64 AddParticipant()`
- `public System.Int64 AddParticipants(System.Int32 participantCount)`
- `public System.Void Dispose()`
- `public System.Void RemoveParticipant()`
- `public System.Void RemoveParticipants(System.Int32 participantCount)`
- `public System.Void SignalAndWait()`
- `public System.Boolean SignalAndWait(System.Int32 millisecondsTimeout)`
- `public System.Void SignalAndWait(System.Threading.CancellationToken cancellationToken)`
- `public System.Boolean SignalAndWait(System.TimeSpan timeout)`
- `public System.Boolean SignalAndWait(System.Int32 millisecondsTimeout, System.Threading.CancellationToken cancellationToken)`
- `public System.Boolean SignalAndWait(System.TimeSpan timeout, System.Threading.CancellationToken cancellationToken)`

### BarrierPostPhaseException (class) : System.Exception, System.Runtime.Serialization.ISerializable

- `public BarrierPostPhaseException()`
- `public BarrierPostPhaseException(System.Exception innerException)`
- `public BarrierPostPhaseException(System.String message)`
- `public BarrierPostPhaseException(System.String message, System.Exception innerException)`

### CancellationToken (struct [readonly struct]) : System.IEquatable<System.Threading.CancellationToken>

- `public CancellationToken(System.Boolean canceled)`
- `public System.Boolean CanBeCanceled { get; }`
- `public System.Boolean IsCancellationRequested { get; }`
- `public static System.Threading.CancellationToken None { get; }`
- `public System.Threading.WaitHandle WaitHandle { get; }`
- `public System.Boolean Equals(System.Object other)`
- `public System.Boolean Equals(System.Threading.CancellationToken other)`
- `public System.Int32 GetHashCode()`
- `public System.Threading.CancellationTokenRegistration Register(System.Action callback)`
- `public System.Threading.CancellationTokenRegistration Register(System.Action callback, System.Boolean useSynchronizationContext)`
- `public System.Threading.CancellationTokenRegistration Register(System.Action<System.Object, System.Threading.CancellationToken> callback, System.Object state)`
- `public System.Threading.CancellationTokenRegistration Register(System.Action<System.Object> callback, System.Object state)`
- `public System.Threading.CancellationTokenRegistration Register(System.Action<System.Object> callback, System.Object state, System.Boolean useSynchronizationContext)`
- `public System.Void ThrowIfCancellationRequested()`
- `public System.Threading.CancellationTokenRegistration UnsafeRegister(System.Action<System.Object, System.Threading.CancellationToken> callback, System.Object state)`
- `public System.Threading.CancellationTokenRegistration UnsafeRegister(System.Action<System.Object> callback, System.Object state)`
- `public static System.Boolean op_Equality(System.Threading.CancellationToken left, System.Threading.CancellationToken right)`
- `public static System.Boolean op_Inequality(System.Threading.CancellationToken left, System.Threading.CancellationToken right)`

### CancellationTokenRegistration (struct [readonly struct]) : System.IAsyncDisposable, System.IDisposable, System.IEquatable<System.Threading.CancellationTokenRegistration>

- `public System.Threading.CancellationToken Token { get; }`
- `public System.Void Dispose()`
- `public System.Threading.Tasks.ValueTask DisposeAsync()`
- `public System.Boolean Equals(System.Object obj)`
- `public System.Boolean Equals(System.Threading.CancellationTokenRegistration other)`
- `public System.Int32 GetHashCode()`
- `public System.Boolean Unregister()`
- `public static System.Boolean op_Equality(System.Threading.CancellationTokenRegistration left, System.Threading.CancellationTokenRegistration right)`
- `public static System.Boolean op_Inequality(System.Threading.CancellationTokenRegistration left, System.Threading.CancellationTokenRegistration right)`

### CancellationTokenSource (class) : System.IDisposable

- `public CancellationTokenSource()`
- `public CancellationTokenSource(System.Int32 millisecondsDelay)`
- `public CancellationTokenSource(System.TimeSpan delay)`
- `public CancellationTokenSource(System.TimeSpan delay, System.TimeProvider timeProvider)`
- `public System.Boolean IsCancellationRequested { get; }`
- `public System.Threading.CancellationToken Token { get; }`
- `public System.Void Cancel()`
- `public System.Void Cancel(System.Boolean throwOnFirstException)`
- `public System.Void CancelAfter(System.Int32 millisecondsDelay)`
- `public System.Void CancelAfter(System.TimeSpan delay)`
- `public System.Threading.Tasks.Task CancelAsync()`
- `public static System.Threading.CancellationTokenSource CreateLinkedTokenSource(System.Threading.CancellationToken token)`
- `public static System.Threading.CancellationTokenSource CreateLinkedTokenSource(System.ReadOnlySpan<System.Threading.CancellationToken> tokens)`
- `public static System.Threading.CancellationTokenSource CreateLinkedTokenSource(params System.Threading.CancellationToken[] tokens)`
- `public static System.Threading.CancellationTokenSource CreateLinkedTokenSource(System.Threading.CancellationToken token1, System.Threading.CancellationToken token2)`
- `public System.Void Dispose()`
- `public System.Boolean TryReset()`

### CountdownEvent (class) : System.IDisposable

- `public CountdownEvent(System.Int32 initialCount)`
- `public System.Int32 CurrentCount { get; }`
- `public System.Int32 InitialCount { get; }`
- `public System.Boolean IsSet { get; }`
- `public System.Threading.WaitHandle WaitHandle { get; }`
- `public System.Void AddCount()`
- `public System.Void AddCount(System.Int32 signalCount)`
- `public System.Void Dispose()`
- `public System.Void Reset()`
- `public System.Void Reset(System.Int32 count)`
- `public System.Boolean Signal()`
- `public System.Boolean Signal(System.Int32 signalCount)`
- `public System.Boolean TryAddCount()`
- `public System.Boolean TryAddCount(System.Int32 signalCount)`
- `public System.Void Wait()`
- `public System.Boolean Wait(System.Int32 millisecondsTimeout)`
- `public System.Void Wait(System.Threading.CancellationToken cancellationToken)`
- `public System.Boolean Wait(System.TimeSpan timeout)`
- `public System.Boolean Wait(System.Int32 millisecondsTimeout, System.Threading.CancellationToken cancellationToken)`
- `public System.Boolean Wait(System.TimeSpan timeout, System.Threading.CancellationToken cancellationToken)`

### ITimer (interface) : System.IAsyncDisposable, System.IDisposable

- `public System.Boolean Change(System.TimeSpan dueTime, System.TimeSpan period)`

### Interlocked (class [static])

- `public static System.Int32 Add(ref System.Int32& location1, System.Int32 value)`
- `public static System.Int64 Add(ref System.Int64& location1, System.Int64 value)`
- `public static System.UInt32 Add(ref System.UInt32& location1, System.UInt32 value)`
- `public static System.UInt64 Add(ref System.UInt64& location1, System.UInt64 value)`
- `public static System.Int32 And(ref System.Int32& location1, System.Int32 value)`
- `public static System.Int64 And(ref System.Int64& location1, System.Int64 value)`
- `public static System.UInt32 And(ref System.UInt32& location1, System.UInt32 value)`
- `public static System.UInt64 And(ref System.UInt64& location1, System.UInt64 value)`
- `public static System.Double CompareExchange(ref System.Double& location1, System.Double value, System.Double comparand)`
- `public static System.Byte CompareExchange(ref System.Byte& location1, System.Byte value, System.Byte comparand)`
- `public static System.SByte CompareExchange(ref System.SByte& location1, System.SByte value, System.SByte comparand)`
- `public static System.Int16 CompareExchange(ref System.Int16& location1, System.Int16 value, System.Int16 comparand)`
- `public static System.UInt16 CompareExchange(ref System.UInt16& location1, System.UInt16 value, System.UInt16 comparand)`
- `public static System.Int32 CompareExchange(ref System.Int32& location1, System.Int32 value, System.Int32 comparand)`
- `public static System.Int64 CompareExchange(ref System.Int64& location1, System.Int64 value, System.Int64 comparand)`
- `public static System.IntPtr CompareExchange(ref System.IntPtr& location1, System.IntPtr value, System.IntPtr comparand)`
- `public static System.UIntPtr CompareExchange(ref System.UIntPtr& location1, System.UIntPtr value, System.UIntPtr comparand)`
- `public static System.Object CompareExchange(ref System.Object& location1, System.Object value, System.Object comparand)`
- `public static System.Single CompareExchange(ref System.Single& location1, System.Single value, System.Single comparand)`
- `public static System.UInt32 CompareExchange(ref System.UInt32& location1, System.UInt32 value, System.UInt32 comparand)`
- `public static System.UInt64 CompareExchange(ref System.UInt64& location1, System.UInt64 value, System.UInt64 comparand)`
- `public static T CompareExchange<T>(ref T& location1, T value, T comparand)`
- `public static System.Int32 Decrement(ref System.Int32& location)`
- `public static System.Int64 Decrement(ref System.Int64& location)`
- `public static System.UInt32 Decrement(ref System.UInt32& location)`
- `public static System.UInt64 Decrement(ref System.UInt64& location)`
- `public static System.Double Exchange(ref System.Double& location1, System.Double value)`
- `public static System.Byte Exchange(ref System.Byte& location1, System.Byte value)`
- `public static System.SByte Exchange(ref System.SByte& location1, System.SByte value)`
- `public static System.Int16 Exchange(ref System.Int16& location1, System.Int16 value)`
- `public static System.UInt16 Exchange(ref System.UInt16& location1, System.UInt16 value)`
- `public static System.Int32 Exchange(ref System.Int32& location1, System.Int32 value)`
- `public static System.Int64 Exchange(ref System.Int64& location1, System.Int64 value)`
- `public static System.IntPtr Exchange(ref System.IntPtr& location1, System.IntPtr value)`
- `public static System.UIntPtr Exchange(ref System.UIntPtr& location1, System.UIntPtr value)`
- `public static System.Object Exchange(ref System.Object& location1, System.Object value)`
- `public static System.Single Exchange(ref System.Single& location1, System.Single value)`
- `public static System.UInt32 Exchange(ref System.UInt32& location1, System.UInt32 value)`
- `public static System.UInt64 Exchange(ref System.UInt64& location1, System.UInt64 value)`
- `public static T Exchange<T>(ref T& location1, T value)`
- `public static System.Int32 Increment(ref System.Int32& location)`
- `public static System.Int64 Increment(ref System.Int64& location)`
- `public static System.UInt32 Increment(ref System.UInt32& location)`
- `public static System.UInt64 Increment(ref System.UInt64& location)`
- `public static System.Void MemoryBarrier()`
- `public static System.Void MemoryBarrierProcessWide()`
- `public static System.Int32 Or(ref System.Int32& location1, System.Int32 value)`
- `public static System.Int64 Or(ref System.Int64& location1, System.Int64 value)`
- `public static System.UInt32 Or(ref System.UInt32& location1, System.UInt32 value)`
- `public static System.UInt64 Or(ref System.UInt64& location1, System.UInt64 value)`
- `public static System.Int64 Read(in System.Int64& location)`
- `public static System.UInt64 Read(in System.UInt64& location)`

### LazyInitializer (class [static])

- `public static T EnsureInitialized<T>(ref T& target)`
- `where T : class`
- `public static T EnsureInitialized<T>(ref T& target, System.Func<T> valueFactory)`
- `where T : class`
- `public static T EnsureInitialized<T>(ref T& target, ref System.Boolean& initialized, ref System.Object& syncLock)`
- `public static T EnsureInitialized<T>(ref T& target, ref System.Object& syncLock, System.Func<T> valueFactory)`
- `where T : class`
- `public static T EnsureInitialized<T>(ref T& target, ref System.Boolean& initialized, ref System.Object& syncLock, System.Func<T> valueFactory)`

### Lock (class [sealed])

- `public Lock()`
- `public System.Boolean IsHeldByCurrentThread { get; }`
- `public System.Void Enter()`
- `public System.Threading.Lock+Scope EnterScope()`
- `public System.Void Exit()`
- `public System.Boolean TryEnter()`
- `public System.Boolean TryEnter(System.Int32 millisecondsTimeout)`
- `public System.Boolean TryEnter(System.TimeSpan timeout)`

### LockCookie (struct) : System.IEquatable<System.Threading.LockCookie>

- `public System.Boolean Equals(System.Object obj)`
- `public System.Boolean Equals(System.Threading.LockCookie obj)`
- `public System.Int32 GetHashCode()`
- `public static System.Boolean op_Equality(System.Threading.LockCookie a, System.Threading.LockCookie b)`
- `public static System.Boolean op_Inequality(System.Threading.LockCookie a, System.Threading.LockCookie b)`

### LockRecursionException (class) : System.Exception, System.Runtime.Serialization.ISerializable

- `public LockRecursionException()`
- `public LockRecursionException(System.String message)`
- `public LockRecursionException(System.String message, System.Exception innerException)`

### LockRecursionPolicy (enum) : System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable

- `NoRecursion = 0`
- `SupportsRecursion = 1`

### ManualResetEventSlim (class) : System.IDisposable

- `public ManualResetEventSlim()`
- `public ManualResetEventSlim(System.Boolean initialState)`
- `public ManualResetEventSlim(System.Boolean initialState, System.Int32 spinCount)`
- `public System.Boolean IsSet { get; }`
- `public System.Int32 SpinCount { get; }`
- `public System.Threading.WaitHandle WaitHandle { get; }`
- `public System.Void Dispose()`
- `public System.Void Reset()`
- `public System.Void Set()`
- `public System.Void Wait()`
- `public System.Boolean Wait(System.Int32 millisecondsTimeout)`
- `public System.Void Wait(System.Threading.CancellationToken cancellationToken)`
- `public System.Boolean Wait(System.TimeSpan timeout)`
- `public System.Boolean Wait(System.Int32 millisecondsTimeout, System.Threading.CancellationToken cancellationToken)`
- `public System.Boolean Wait(System.TimeSpan timeout, System.Threading.CancellationToken cancellationToken)`

### PeriodicTimer (class [sealed]) : System.IDisposable

- `public PeriodicTimer(System.TimeSpan period)`
- `public PeriodicTimer(System.TimeSpan period, System.TimeProvider timeProvider)`
- `public System.TimeSpan Period { get; set; }`
- `public System.Void Dispose()`
- `public System.Threading.Tasks.ValueTask<System.Boolean> WaitForNextTickAsync(System.Threading.CancellationToken cancellationToken)`

### ReaderWriterLockSlim (class) : System.IDisposable

- `public ReaderWriterLockSlim()`
- `public ReaderWriterLockSlim(System.Threading.LockRecursionPolicy recursionPolicy)`
- `public System.Int32 CurrentReadCount { get; }`
- `public System.Boolean IsReadLockHeld { get; }`
- `public System.Boolean IsUpgradeableReadLockHeld { get; }`
- `public System.Boolean IsWriteLockHeld { get; }`
- `public System.Threading.LockRecursionPolicy RecursionPolicy { get; }`
- `public System.Int32 RecursiveReadCount { get; }`
- `public System.Int32 RecursiveUpgradeCount { get; }`
- `public System.Int32 RecursiveWriteCount { get; }`
- `public System.Int32 WaitingReadCount { get; }`
- `public System.Int32 WaitingUpgradeCount { get; }`
- `public System.Int32 WaitingWriteCount { get; }`
- `public System.Void Dispose()`
- `public System.Void EnterReadLock()`
- `public System.Void EnterUpgradeableReadLock()`
- `public System.Void EnterWriteLock()`
- `public System.Void ExitReadLock()`
- `public System.Void ExitUpgradeableReadLock()`
- `public System.Void ExitWriteLock()`
- `public System.Boolean TryEnterReadLock(System.Int32 millisecondsTimeout)`
- `public System.Boolean TryEnterReadLock(System.TimeSpan timeout)`
- `public System.Boolean TryEnterUpgradeableReadLock(System.Int32 millisecondsTimeout)`
- `public System.Boolean TryEnterUpgradeableReadLock(System.TimeSpan timeout)`
- `public System.Boolean TryEnterWriteLock(System.Int32 millisecondsTimeout)`
- `public System.Boolean TryEnterWriteLock(System.TimeSpan timeout)`

### Scope (struct [ref struct])

- `public System.Void Dispose()`

### SemaphoreSlim (class) : System.IDisposable

- `public SemaphoreSlim(System.Int32 initialCount)`
- `public SemaphoreSlim(System.Int32 initialCount, System.Int32 maxCount)`
- `public System.Threading.WaitHandle AvailableWaitHandle { get; }`
- `public System.Int32 CurrentCount { get; }`
- `public System.Void Dispose()`
- `public System.Int32 Release()`
- `public System.Int32 Release(System.Int32 releaseCount)`
- `public System.Void Wait()`
- `public System.Boolean Wait(System.Int32 millisecondsTimeout)`
- `public System.Void Wait(System.Threading.CancellationToken cancellationToken)`
- `public System.Boolean Wait(System.TimeSpan timeout)`
- `public System.Boolean Wait(System.Int32 millisecondsTimeout, System.Threading.CancellationToken cancellationToken)`
- `public System.Boolean Wait(System.TimeSpan timeout, System.Threading.CancellationToken cancellationToken)`
- `public System.Threading.Tasks.Task WaitAsync()`
- `public System.Threading.Tasks.Task<System.Boolean> WaitAsync(System.Int32 millisecondsTimeout)`
- `public System.Threading.Tasks.Task WaitAsync(System.Threading.CancellationToken cancellationToken)`
- `public System.Threading.Tasks.Task<System.Boolean> WaitAsync(System.TimeSpan timeout)`
- `public System.Threading.Tasks.Task<System.Boolean> WaitAsync(System.Int32 millisecondsTimeout, System.Threading.CancellationToken cancellationToken)`
- `public System.Threading.Tasks.Task<System.Boolean> WaitAsync(System.TimeSpan timeout, System.Threading.CancellationToken cancellationToken)`

### Timeout (class [static])

- `public const System.Int32 Infinite`
- `public static System.TimeSpan InfiniteTimeSpan`

### Timer (class [sealed]) : System.MarshalByRefObject, System.IAsyncDisposable, System.IDisposable, System.Threading.ITimer

- `public Timer(System.Threading.TimerCallback callback)`
- `public Timer(System.Threading.TimerCallback callback, System.Object state, System.Int32 dueTime, System.Int32 period)`
- `public Timer(System.Threading.TimerCallback callback, System.Object state, System.Int64 dueTime, System.Int64 period)`
- `public Timer(System.Threading.TimerCallback callback, System.Object state, System.TimeSpan dueTime, System.TimeSpan period)`
- `public Timer(System.Threading.TimerCallback callback, System.Object state, System.UInt32 dueTime, System.UInt32 period)`
- `public static System.Int64 ActiveCount { get; }`
- `public System.Boolean Change(System.Int32 dueTime, System.Int32 period)`
- `public System.Boolean Change(System.Int64 dueTime, System.Int64 period)`
- `public System.Boolean Change(System.TimeSpan dueTime, System.TimeSpan period)`
- `public System.Boolean Change(System.UInt32 dueTime, System.UInt32 period)`
- `public System.Void Dispose()`
- `public System.Boolean Dispose(System.Threading.WaitHandle notifyObject)`
- `public System.Threading.Tasks.ValueTask DisposeAsync()`

### TimerCallback (delegate [sealed]) : System.ICloneable, System.Runtime.Serialization.ISerializable

- `public TimerCallback(System.Object object, System.IntPtr method)`
- `delegate System.Void Invoke(System.Object state)`
- `public System.IAsyncResult BeginInvoke(System.Object state, System.AsyncCallback callback, System.Object object)`
- `public System.Void EndInvoke(System.IAsyncResult result)`

### Volatile (class [static])

- `public static System.Boolean Read(in System.Boolean& location)`
- `public static System.Byte Read(in System.Byte& location)`
- `public static System.Double Read(in System.Double& location)`
- `public static System.Int16 Read(in System.Int16& location)`
- `public static System.Int32 Read(in System.Int32& location)`
- `public static System.Int64 Read(in System.Int64& location)`
- `public static System.IntPtr Read(in System.IntPtr& location)`
- `public static System.SByte Read(in System.SByte& location)`
- `public static System.Single Read(in System.Single& location)`
- `public static System.UInt16 Read(in System.UInt16& location)`
- `public static System.UInt32 Read(in System.UInt32& location)`
- `public static System.UInt64 Read(in System.UInt64& location)`
- `public static System.UIntPtr Read(in System.UIntPtr& location)`
- `public static T Read<T>(in T& location)`
- `where T : class`
- `public static System.Void ReadBarrier()`
- `public static System.Void Write(ref System.Boolean& location, System.Boolean value)`
- `public static System.Void Write(ref System.Byte& location, System.Byte value)`
- `public static System.Void Write(ref System.Double& location, System.Double value)`
- `public static System.Void Write(ref System.Int16& location, System.Int16 value)`
- `public static System.Void Write(ref System.Int32& location, System.Int32 value)`
- `public static System.Void Write(ref System.Int64& location, System.Int64 value)`
- `public static System.Void Write(ref System.IntPtr& location, System.IntPtr value)`
- `public static System.Void Write(ref System.SByte& location, System.SByte value)`
- `public static System.Void Write(ref System.Single& location, System.Single value)`
- `public static System.Void Write(ref System.UInt16& location, System.UInt16 value)`
- `public static System.Void Write(ref System.UInt32& location, System.UInt32 value)`
- `public static System.Void Write(ref System.UInt64& location, System.UInt64 value)`
- `public static System.Void Write(ref System.UIntPtr& location, System.UIntPtr value)`
- `public static System.Void Write<T>(ref T& location, T value)`
- `where T : class`
- `public static System.Void WriteBarrier()`

### WaitHandle (class [abstract]) : System.MarshalByRefObject, System.IDisposable

- `public const System.Int32 WaitTimeout`
- `public System.IntPtr Handle { get; set; }`
- `public Microsoft.Win32.SafeHandles.SafeWaitHandle SafeWaitHandle { get; set; }`
- `public System.Void Close()`
- `public System.Void Dispose()`
- `public static System.Boolean SignalAndWait(System.Threading.WaitHandle toSignal, System.Threading.WaitHandle toWaitOn)`
- `public static System.Boolean SignalAndWait(System.Threading.WaitHandle toSignal, System.Threading.WaitHandle toWaitOn, System.Int32 millisecondsTimeout, System.Boolean exitContext)`
- `public static System.Boolean SignalAndWait(System.Threading.WaitHandle toSignal, System.Threading.WaitHandle toWaitOn, System.TimeSpan timeout, System.Boolean exitContext)`
- `public static System.Boolean WaitAll(System.Threading.WaitHandle[] waitHandles)`
- `public static System.Boolean WaitAll(System.Threading.WaitHandle[] waitHandles, System.Int32 millisecondsTimeout)`
- `public static System.Boolean WaitAll(System.Threading.WaitHandle[] waitHandles, System.TimeSpan timeout)`
- `public static System.Boolean WaitAll(System.Threading.WaitHandle[] waitHandles, System.Int32 millisecondsTimeout, System.Boolean exitContext)`
- `public static System.Boolean WaitAll(System.Threading.WaitHandle[] waitHandles, System.TimeSpan timeout, System.Boolean exitContext)`
- `public static System.Int32 WaitAny(System.Threading.WaitHandle[] waitHandles)`
- `public static System.Int32 WaitAny(System.Threading.WaitHandle[] waitHandles, System.Int32 millisecondsTimeout)`
- `public static System.Int32 WaitAny(System.Threading.WaitHandle[] waitHandles, System.TimeSpan timeout)`
- `public static System.Int32 WaitAny(System.Threading.WaitHandle[] waitHandles, System.Int32 millisecondsTimeout, System.Boolean exitContext)`
- `public static System.Int32 WaitAny(System.Threading.WaitHandle[] waitHandles, System.TimeSpan timeout, System.Boolean exitContext)`
- `public System.Boolean WaitOne()`
- `public System.Boolean WaitOne(System.Int32 millisecondsTimeout)`
- `public System.Boolean WaitOne(System.TimeSpan timeout)`
- `public System.Boolean WaitOne(System.Int32 millisecondsTimeout, System.Boolean exitContext)`
- `public System.Boolean WaitOne(System.TimeSpan timeout, System.Boolean exitContext)`

### WaitHandleCannotBeOpenedException (class) : System.ApplicationException, System.Runtime.Serialization.ISerializable

- `public WaitHandleCannotBeOpenedException()`
- `public WaitHandleCannotBeOpenedException(System.String message)`
- `public WaitHandleCannotBeOpenedException(System.String message, System.Exception innerException)`

### WaitHandleExtensions (class [static])

- `[ext] public static Microsoft.Win32.SafeHandles.SafeWaitHandle GetSafeWaitHandle(this System.Threading.WaitHandle waitHandle)`
- `[ext] public static System.Void SetSafeWaitHandle(this System.Threading.WaitHandle waitHandle, Microsoft.Win32.SafeHandles.SafeWaitHandle value)`

## System.Threading.Channels

### BoundedChannelFullMode (enum) : System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable

- `Wait = 0`
- `DropNewest = 1`
- `DropOldest = 2`
- `DropWrite = 3`

### BoundedChannelOptions (class [sealed]) : System.Threading.Channels.ChannelOptions

- `public BoundedChannelOptions(System.Int32 capacity)`
- `public System.Int32 Capacity { get; set; }`
- `public System.Threading.Channels.BoundedChannelFullMode FullMode { get; set; }`

### Channel (class [static])

- `public static System.Threading.Channels.Channel<T> CreateBounded<T>(System.Int32 capacity)`
- `public static System.Threading.Channels.Channel<T> CreateBounded<T>(System.Threading.Channels.BoundedChannelOptions options)`
- `public static System.Threading.Channels.Channel<T> CreateBounded<T>(System.Threading.Channels.BoundedChannelOptions options, System.Action<T> itemDropped)`
- `public static System.Threading.Channels.Channel<T> CreateUnbounded<T>()`
- `public static System.Threading.Channels.Channel<T> CreateUnbounded<T>(System.Threading.Channels.UnboundedChannelOptions options)`
- `public static System.Threading.Channels.Channel<T> CreateUnboundedPrioritized<T>()`
- `public static System.Threading.Channels.Channel<T> CreateUnboundedPrioritized<T>(System.Threading.Channels.UnboundedPrioritizedChannelOptions<T> options)`

### ChannelClosedException (class) : System.InvalidOperationException, System.Runtime.Serialization.ISerializable

- `public ChannelClosedException()`
- `public ChannelClosedException(System.Exception innerException)`
- `public ChannelClosedException(System.String message)`
- `public ChannelClosedException(System.String message, System.Exception innerException)`

### ChannelOptions (class [abstract])

- `public System.Boolean AllowSynchronousContinuations { get; set; }`
- `public System.Boolean SingleReader { get; set; }`
- `public System.Boolean SingleWriter { get; set; }`

### ChannelReader`1<T> (class [abstract])

- `public System.Boolean CanCount { get; }`
- `public System.Boolean CanPeek { get; }`
- `public System.Threading.Tasks.Task Completion { get; }`
- `public System.Int32 Count { get; }`
- `public System.Collections.Generic.IAsyncEnumerable<T> ReadAllAsync(System.Threading.CancellationToken cancellationToken)`
- `public System.Threading.Tasks.ValueTask<T> ReadAsync(System.Threading.CancellationToken cancellationToken)`
- `public System.Boolean TryPeek(out T& item)`
- `public System.Boolean TryRead(out T& item)`
- `public System.Threading.Tasks.ValueTask<System.Boolean> WaitToReadAsync(System.Threading.CancellationToken cancellationToken)`

### ChannelWriter`1<T> (class [abstract])

- `public System.Void Complete(System.Exception error)`
- `public System.Boolean TryComplete(System.Exception error)`
- `public System.Boolean TryWrite(T item)`
- `public System.Threading.Tasks.ValueTask<System.Boolean> WaitToWriteAsync(System.Threading.CancellationToken cancellationToken)`
- `public System.Threading.Tasks.ValueTask WriteAsync(T item, System.Threading.CancellationToken cancellationToken)`

### Channel`1<T> (class [abstract]) : System.Threading.Channels.Channel<T, T>


### Channel`2<TWrite, TRead> (class [abstract])

- `public System.Threading.Channels.ChannelReader<TRead> Reader { get; }`
- `public System.Threading.Channels.ChannelWriter<TWrite> Writer { get; }`
- `public static System.Threading.Channels.ChannelReader<TRead> op_Implicit(System.Threading.Channels.Channel<TWrite, TRead> channel)`
- `public static System.Threading.Channels.ChannelWriter<TWrite> op_Implicit(System.Threading.Channels.Channel<TWrite, TRead> channel)`

### UnboundedChannelOptions (class [sealed]) : System.Threading.Channels.ChannelOptions

- `public UnboundedChannelOptions()`

### UnboundedPrioritizedChannelOptions`1<T> (class [sealed]) : System.Threading.Channels.ChannelOptions

- `public UnboundedPrioritizedChannelOptions`1()`
- `public System.Collections.Generic.IComparer<T> Comparer { get; set; }`

## System.Threading.Tasks

### Parallel (class [static])

- `public static System.Threading.Tasks.ParallelLoopResult For(System.Int32 fromInclusive, System.Int32 toExclusive, System.Action<System.Int32, System.Threading.Tasks.ParallelLoopState> body)`
- `public static System.Threading.Tasks.ParallelLoopResult For(System.Int32 fromInclusive, System.Int32 toExclusive, System.Action<System.Int32> body)`
- `public static System.Threading.Tasks.ParallelLoopResult For(System.Int64 fromInclusive, System.Int64 toExclusive, System.Action<System.Int64, System.Threading.Tasks.ParallelLoopState> body)`
- `public static System.Threading.Tasks.ParallelLoopResult For(System.Int64 fromInclusive, System.Int64 toExclusive, System.Action<System.Int64> body)`
- `public static System.Threading.Tasks.ParallelLoopResult For(System.Int32 fromInclusive, System.Int32 toExclusive, System.Threading.Tasks.ParallelOptions parallelOptions, System.Action<System.Int32, System.Threading.Tasks.ParallelLoopState> body)`
- `public static System.Threading.Tasks.ParallelLoopResult For(System.Int32 fromInclusive, System.Int32 toExclusive, System.Threading.Tasks.ParallelOptions parallelOptions, System.Action<System.Int32> body)`
- `public static System.Threading.Tasks.ParallelLoopResult For(System.Int64 fromInclusive, System.Int64 toExclusive, System.Threading.Tasks.ParallelOptions parallelOptions, System.Action<System.Int64, System.Threading.Tasks.ParallelLoopState> body)`
- `public static System.Threading.Tasks.ParallelLoopResult For(System.Int64 fromInclusive, System.Int64 toExclusive, System.Threading.Tasks.ParallelOptions parallelOptions, System.Action<System.Int64> body)`
- `public static System.Threading.Tasks.ParallelLoopResult For<TLocal>(System.Int32 fromInclusive, System.Int32 toExclusive, System.Func<TLocal> localInit, System.Func<System.Int32, System.Threading.Tasks.ParallelLoopState, TLocal, TLocal> body, System.Action<TLocal> localFinally)`
- `public static System.Threading.Tasks.ParallelLoopResult For<TLocal>(System.Int64 fromInclusive, System.Int64 toExclusive, System.Func<TLocal> localInit, System.Func<System.Int64, System.Threading.Tasks.ParallelLoopState, TLocal, TLocal> body, System.Action<TLocal> localFinally)`
- `public static System.Threading.Tasks.ParallelLoopResult For<TLocal>(System.Int32 fromInclusive, System.Int32 toExclusive, System.Threading.Tasks.ParallelOptions parallelOptions, System.Func<TLocal> localInit, System.Func<System.Int32, System.Threading.Tasks.ParallelLoopState, TLocal, TLocal> body, System.Action<TLocal> localFinally)`
- `public static System.Threading.Tasks.ParallelLoopResult For<TLocal>(System.Int64 fromInclusive, System.Int64 toExclusive, System.Threading.Tasks.ParallelOptions parallelOptions, System.Func<TLocal> localInit, System.Func<System.Int64, System.Threading.Tasks.ParallelLoopState, TLocal, TLocal> body, System.Action<TLocal> localFinally)`
- `public static System.Threading.Tasks.Task ForAsync<T>(T fromInclusive, T toExclusive, System.Func<T, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask> body)`
- `where T : System.Numerics.IBinaryInteger<T>`
- `public static System.Threading.Tasks.Task ForAsync<T>(T fromInclusive, T toExclusive, System.Threading.CancellationToken cancellationToken, System.Func<T, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask> body)`
- `where T : System.Numerics.IBinaryInteger<T>`
- `public static System.Threading.Tasks.Task ForAsync<T>(T fromInclusive, T toExclusive, System.Threading.Tasks.ParallelOptions parallelOptions, System.Func<T, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask> body)`
- `where T : System.Numerics.IBinaryInteger<T>`
- `public static System.Threading.Tasks.ParallelLoopResult ForEach<TSource>(System.Collections.Concurrent.OrderablePartitioner<TSource> source, System.Action<TSource, System.Threading.Tasks.ParallelLoopState, System.Int64> body)`
- `public static System.Threading.Tasks.ParallelLoopResult ForEach<TSource>(System.Collections.Concurrent.Partitioner<TSource> source, System.Action<TSource, System.Threading.Tasks.ParallelLoopState> body)`
- `public static System.Threading.Tasks.ParallelLoopResult ForEach<TSource>(System.Collections.Concurrent.Partitioner<TSource> source, System.Action<TSource> body)`
- `public static System.Threading.Tasks.ParallelLoopResult ForEach<TSource>(System.Collections.Generic.IEnumerable<TSource> source, System.Action<TSource, System.Threading.Tasks.ParallelLoopState, System.Int64> body)`
- `public static System.Threading.Tasks.ParallelLoopResult ForEach<TSource>(System.Collections.Generic.IEnumerable<TSource> source, System.Action<TSource, System.Threading.Tasks.ParallelLoopState> body)`
- `public static System.Threading.Tasks.ParallelLoopResult ForEach<TSource>(System.Collections.Generic.IEnumerable<TSource> source, System.Action<TSource> body)`
- `public static System.Threading.Tasks.ParallelLoopResult ForEach<TSource>(System.Collections.Concurrent.OrderablePartitioner<TSource> source, System.Threading.Tasks.ParallelOptions parallelOptions, System.Action<TSource, System.Threading.Tasks.ParallelLoopState, System.Int64> body)`
- `public static System.Threading.Tasks.ParallelLoopResult ForEach<TSource>(System.Collections.Concurrent.Partitioner<TSource> source, System.Threading.Tasks.ParallelOptions parallelOptions, System.Action<TSource, System.Threading.Tasks.ParallelLoopState> body)`
- `public static System.Threading.Tasks.ParallelLoopResult ForEach<TSource>(System.Collections.Concurrent.Partitioner<TSource> source, System.Threading.Tasks.ParallelOptions parallelOptions, System.Action<TSource> body)`
- `public static System.Threading.Tasks.ParallelLoopResult ForEach<TSource>(System.Collections.Generic.IEnumerable<TSource> source, System.Threading.Tasks.ParallelOptions parallelOptions, System.Action<TSource, System.Threading.Tasks.ParallelLoopState, System.Int64> body)`
- `public static System.Threading.Tasks.ParallelLoopResult ForEach<TSource>(System.Collections.Generic.IEnumerable<TSource> source, System.Threading.Tasks.ParallelOptions parallelOptions, System.Action<TSource, System.Threading.Tasks.ParallelLoopState> body)`
- `public static System.Threading.Tasks.ParallelLoopResult ForEach<TSource>(System.Collections.Generic.IEnumerable<TSource> source, System.Threading.Tasks.ParallelOptions parallelOptions, System.Action<TSource> body)`
- `public static System.Threading.Tasks.ParallelLoopResult ForEach<TSource, TLocal>(System.Collections.Concurrent.OrderablePartitioner<TSource> source, System.Func<TLocal> localInit, System.Func<TSource, System.Threading.Tasks.ParallelLoopState, System.Int64, TLocal, TLocal> body, System.Action<TLocal> localFinally)`
- `public static System.Threading.Tasks.ParallelLoopResult ForEach<TSource, TLocal>(System.Collections.Concurrent.Partitioner<TSource> source, System.Func<TLocal> localInit, System.Func<TSource, System.Threading.Tasks.ParallelLoopState, TLocal, TLocal> body, System.Action<TLocal> localFinally)`
- `public static System.Threading.Tasks.ParallelLoopResult ForEach<TSource, TLocal>(System.Collections.Generic.IEnumerable<TSource> source, System.Func<TLocal> localInit, System.Func<TSource, System.Threading.Tasks.ParallelLoopState, System.Int64, TLocal, TLocal> body, System.Action<TLocal> localFinally)`
- `public static System.Threading.Tasks.ParallelLoopResult ForEach<TSource, TLocal>(System.Collections.Generic.IEnumerable<TSource> source, System.Func<TLocal> localInit, System.Func<TSource, System.Threading.Tasks.ParallelLoopState, TLocal, TLocal> body, System.Action<TLocal> localFinally)`
- `public static System.Threading.Tasks.ParallelLoopResult ForEach<TSource, TLocal>(System.Collections.Concurrent.OrderablePartitioner<TSource> source, System.Threading.Tasks.ParallelOptions parallelOptions, System.Func<TLocal> localInit, System.Func<TSource, System.Threading.Tasks.ParallelLoopState, System.Int64, TLocal, TLocal> body, System.Action<TLocal> localFinally)`
- `public static System.Threading.Tasks.ParallelLoopResult ForEach<TSource, TLocal>(System.Collections.Concurrent.Partitioner<TSource> source, System.Threading.Tasks.ParallelOptions parallelOptions, System.Func<TLocal> localInit, System.Func<TSource, System.Threading.Tasks.ParallelLoopState, TLocal, TLocal> body, System.Action<TLocal> localFinally)`
- `public static System.Threading.Tasks.ParallelLoopResult ForEach<TSource, TLocal>(System.Collections.Generic.IEnumerable<TSource> source, System.Threading.Tasks.ParallelOptions parallelOptions, System.Func<TLocal> localInit, System.Func<TSource, System.Threading.Tasks.ParallelLoopState, System.Int64, TLocal, TLocal> body, System.Action<TLocal> localFinally)`
- `public static System.Threading.Tasks.ParallelLoopResult ForEach<TSource, TLocal>(System.Collections.Generic.IEnumerable<TSource> source, System.Threading.Tasks.ParallelOptions parallelOptions, System.Func<TLocal> localInit, System.Func<TSource, System.Threading.Tasks.ParallelLoopState, TLocal, TLocal> body, System.Action<TLocal> localFinally)`
- `public static System.Threading.Tasks.Task ForEachAsync<TSource>(System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask> body)`
- `public static System.Threading.Tasks.Task ForEachAsync<TSource>(System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask> body)`
- `public static System.Threading.Tasks.Task ForEachAsync<TSource>(System.Collections.Generic.IEnumerable<TSource> source, System.Threading.CancellationToken cancellationToken, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask> body)`
- `public static System.Threading.Tasks.Task ForEachAsync<TSource>(System.Collections.Generic.IEnumerable<TSource> source, System.Threading.Tasks.ParallelOptions parallelOptions, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask> body)`
- `public static System.Threading.Tasks.Task ForEachAsync<TSource>(System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Threading.CancellationToken cancellationToken, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask> body)`
- `public static System.Threading.Tasks.Task ForEachAsync<TSource>(System.Collections.Generic.IAsyncEnumerable<TSource> source, System.Threading.Tasks.ParallelOptions parallelOptions, System.Func<TSource, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask> body)`
- `public static System.Void Invoke(params System.Action[] actions)`
- `public static System.Void Invoke(System.Threading.Tasks.ParallelOptions parallelOptions, params System.Action[] actions)`

### ParallelLoopResult (struct)

- `public System.Boolean IsCompleted { get; }`
- `public System.Int64? LowestBreakIteration { get; }`

### ParallelLoopState (class)

- `public System.Boolean IsExceptional { get; }`
- `public System.Boolean IsStopped { get; }`
- `public System.Int64? LowestBreakIteration { get; }`
- `public System.Boolean ShouldExitCurrentIteration { get; }`
- `public System.Void Break()`
- `public System.Void Stop()`

### ParallelOptions (class)

- `public ParallelOptions()`
- `public System.Threading.CancellationToken CancellationToken { get; set; }`
- `public System.Int32 MaxDegreeOfParallelism { get; set; }`
- `public System.Threading.Tasks.TaskScheduler TaskScheduler { get; set; }`

### Task (class) : System.IAsyncResult, System.IDisposable

- `public Task(System.Action action)`
- `public Task(System.Action action, System.Threading.CancellationToken cancellationToken)`
- `public Task(System.Action action, System.Threading.Tasks.TaskCreationOptions creationOptions)`
- `public Task(System.Action<System.Object> action, System.Object state)`
- `public Task(System.Action action, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions)`
- `public Task(System.Action<System.Object> action, System.Object state, System.Threading.CancellationToken cancellationToken)`
- `public Task(System.Action<System.Object> action, System.Object state, System.Threading.Tasks.TaskCreationOptions creationOptions)`
- `public Task(System.Action<System.Object> action, System.Object state, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions)`
- `public System.Object AsyncState { get; }`
- `public static System.Threading.Tasks.Task CompletedTask { get; }`
- `public System.Threading.Tasks.TaskCreationOptions CreationOptions { get; }`
- `public static System.Int32? CurrentId { get; }`
- `public System.AggregateException Exception { get; }`
- `public static System.Threading.Tasks.TaskFactory Factory { get; }`
- `public System.Int32 Id { get; }`
- `public System.Boolean IsCanceled { get; }`
- `public System.Boolean IsCompleted { get; }`
- `public System.Boolean IsCompletedSuccessfully { get; }`
- `public System.Boolean IsFaulted { get; }`
- `public System.Threading.Tasks.TaskStatus Status { get; }`
- `public System.Runtime.CompilerServices.ConfiguredTaskAwaitable ConfigureAwait(System.Boolean continueOnCapturedContext)`
- `public System.Runtime.CompilerServices.ConfiguredTaskAwaitable ConfigureAwait(System.Threading.Tasks.ConfigureAwaitOptions options)`
- `public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task> continuationAction)`
- `public System.Threading.Tasks.Task<TResult> ContinueWith<TResult>(System.Func<System.Threading.Tasks.Task, TResult> continuationFunction)`
- `public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task, System.Object> continuationAction, System.Object state)`
- `public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task> continuationAction, System.Threading.CancellationToken cancellationToken)`
- `public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task> continuationAction, System.Threading.Tasks.TaskContinuationOptions continuationOptions)`
- `public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task> continuationAction, System.Threading.Tasks.TaskScheduler scheduler)`
- `public System.Threading.Tasks.Task<TResult> ContinueWith<TResult>(System.Func<System.Threading.Tasks.Task, System.Object, TResult> continuationFunction, System.Object state)`
- `public System.Threading.Tasks.Task<TResult> ContinueWith<TResult>(System.Func<System.Threading.Tasks.Task, TResult> continuationFunction, System.Threading.CancellationToken cancellationToken)`
- `public System.Threading.Tasks.Task<TResult> ContinueWith<TResult>(System.Func<System.Threading.Tasks.Task, TResult> continuationFunction, System.Threading.Tasks.TaskContinuationOptions continuationOptions)`
- `public System.Threading.Tasks.Task<TResult> ContinueWith<TResult>(System.Func<System.Threading.Tasks.Task, TResult> continuationFunction, System.Threading.Tasks.TaskScheduler scheduler)`
- `public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task, System.Object> continuationAction, System.Object state, System.Threading.CancellationToken cancellationToken)`
- `public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task, System.Object> continuationAction, System.Object state, System.Threading.Tasks.TaskContinuationOptions continuationOptions)`
- `public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task, System.Object> continuationAction, System.Object state, System.Threading.Tasks.TaskScheduler scheduler)`
- `public System.Threading.Tasks.Task<TResult> ContinueWith<TResult>(System.Func<System.Threading.Tasks.Task, System.Object, TResult> continuationFunction, System.Object state, System.Threading.CancellationToken cancellationToken)`
- `public System.Threading.Tasks.Task<TResult> ContinueWith<TResult>(System.Func<System.Threading.Tasks.Task, System.Object, TResult> continuationFunction, System.Object state, System.Threading.Tasks.TaskContinuationOptions continuationOptions)`
- `public System.Threading.Tasks.Task<TResult> ContinueWith<TResult>(System.Func<System.Threading.Tasks.Task, System.Object, TResult> continuationFunction, System.Object state, System.Threading.Tasks.TaskScheduler scheduler)`
- `public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task> continuationAction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler)`
- `public System.Threading.Tasks.Task<TResult> ContinueWith<TResult>(System.Func<System.Threading.Tasks.Task, TResult> continuationFunction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler)`
- `public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task, System.Object> continuationAction, System.Object state, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler)`
- `public System.Threading.Tasks.Task<TResult> ContinueWith<TResult>(System.Func<System.Threading.Tasks.Task, System.Object, TResult> continuationFunction, System.Object state, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler)`
- `public static System.Threading.Tasks.Task Delay(System.Int32 millisecondsDelay)`
- `public static System.Threading.Tasks.Task Delay(System.TimeSpan delay)`
- `public static System.Threading.Tasks.Task Delay(System.Int32 millisecondsDelay, System.Threading.CancellationToken cancellationToken)`
- `public static System.Threading.Tasks.Task Delay(System.TimeSpan delay, System.Threading.CancellationToken cancellationToken)`
- `public static System.Threading.Tasks.Task Delay(System.TimeSpan delay, System.TimeProvider timeProvider)`
- `public static System.Threading.Tasks.Task Delay(System.TimeSpan delay, System.TimeProvider timeProvider, System.Threading.CancellationToken cancellationToken)`
- `public System.Void Dispose()`
- `public static System.Threading.Tasks.Task FromCanceled(System.Threading.CancellationToken cancellationToken)`
- `public static System.Threading.Tasks.Task<TResult> FromCanceled<TResult>(System.Threading.CancellationToken cancellationToken)`
- `public static System.Threading.Tasks.Task FromException(System.Exception exception)`
- `public static System.Threading.Tasks.Task<TResult> FromException<TResult>(System.Exception exception)`
- `public static System.Threading.Tasks.Task<TResult> FromResult<TResult>(TResult result)`
- `public System.Runtime.CompilerServices.TaskAwaiter GetAwaiter()`
- `public static System.Threading.Tasks.Task Run(System.Action action)`
- `public static System.Threading.Tasks.Task Run(System.Func<System.Threading.Tasks.Task> function)`
- `public static System.Threading.Tasks.Task<TResult> Run<TResult>(System.Func<System.Threading.Tasks.Task<TResult>> function)`
- `public static System.Threading.Tasks.Task<TResult> Run<TResult>(System.Func<TResult> function)`
- `public static System.Threading.Tasks.Task Run(System.Action action, System.Threading.CancellationToken cancellationToken)`
- `public static System.Threading.Tasks.Task Run(System.Func<System.Threading.Tasks.Task> function, System.Threading.CancellationToken cancellationToken)`
- `public static System.Threading.Tasks.Task<TResult> Run<TResult>(System.Func<System.Threading.Tasks.Task<TResult>> function, System.Threading.CancellationToken cancellationToken)`
- `public static System.Threading.Tasks.Task<TResult> Run<TResult>(System.Func<TResult> function, System.Threading.CancellationToken cancellationToken)`
- `public System.Void RunSynchronously()`
- `public System.Void RunSynchronously(System.Threading.Tasks.TaskScheduler scheduler)`
- `public System.Void Start()`
- `public System.Void Start(System.Threading.Tasks.TaskScheduler scheduler)`
- `public System.Void Wait()`
- `public System.Boolean Wait(System.Int32 millisecondsTimeout)`
- `public System.Void Wait(System.Threading.CancellationToken cancellationToken)`
- `public System.Boolean Wait(System.TimeSpan timeout)`
- `public System.Boolean Wait(System.Int32 millisecondsTimeout, System.Threading.CancellationToken cancellationToken)`
- `public System.Boolean Wait(System.TimeSpan timeout, System.Threading.CancellationToken cancellationToken)`
- `public static System.Void WaitAll(System.ReadOnlySpan<System.Threading.Tasks.Task> tasks)`
- `public static System.Void WaitAll(params System.Threading.Tasks.Task[] tasks)`
- `public static System.Void WaitAll(System.Collections.Generic.IEnumerable<System.Threading.Tasks.Task> tasks, System.Threading.CancellationToken cancellationToken)`
- `public static System.Boolean WaitAll(System.Threading.Tasks.Task[] tasks, System.Int32 millisecondsTimeout)`
- `public static System.Void WaitAll(System.Threading.Tasks.Task[] tasks, System.Threading.CancellationToken cancellationToken)`
- `public static System.Boolean WaitAll(System.Threading.Tasks.Task[] tasks, System.TimeSpan timeout)`
- `public static System.Boolean WaitAll(System.Threading.Tasks.Task[] tasks, System.Int32 millisecondsTimeout, System.Threading.CancellationToken cancellationToken)`
- `public static System.Int32 WaitAny(params System.Threading.Tasks.Task[] tasks)`
- `public static System.Int32 WaitAny(System.Threading.Tasks.Task[] tasks, System.Int32 millisecondsTimeout)`
- `public static System.Int32 WaitAny(System.Threading.Tasks.Task[] tasks, System.Threading.CancellationToken cancellationToken)`
- `public static System.Int32 WaitAny(System.Threading.Tasks.Task[] tasks, System.TimeSpan timeout)`
- `public static System.Int32 WaitAny(System.Threading.Tasks.Task[] tasks, System.Int32 millisecondsTimeout, System.Threading.CancellationToken cancellationToken)`
- `public System.Threading.Tasks.Task WaitAsync(System.Threading.CancellationToken cancellationToken)`
- `public System.Threading.Tasks.Task WaitAsync(System.TimeSpan timeout)`
- `public System.Threading.Tasks.Task WaitAsync(System.TimeSpan timeout, System.Threading.CancellationToken cancellationToken)`
- `public System.Threading.Tasks.Task WaitAsync(System.TimeSpan timeout, System.TimeProvider timeProvider)`
- `public System.Threading.Tasks.Task WaitAsync(System.TimeSpan timeout, System.TimeProvider timeProvider, System.Threading.CancellationToken cancellationToken)`
- `public static System.Threading.Tasks.Task WhenAll(System.Collections.Generic.IEnumerable<System.Threading.Tasks.Task> tasks)`
- `public static System.Threading.Tasks.Task WhenAll(System.ReadOnlySpan<System.Threading.Tasks.Task> tasks)`
- `public static System.Threading.Tasks.Task WhenAll(params System.Threading.Tasks.Task[] tasks)`
- `public static System.Threading.Tasks.Task<TResult[]> WhenAll<TResult>(System.Collections.Generic.IEnumerable<System.Threading.Tasks.Task<TResult>> tasks)`
- `public static System.Threading.Tasks.Task<TResult[]> WhenAll<TResult>(System.ReadOnlySpan<System.Threading.Tasks.Task<TResult>> tasks)`
- `public static System.Threading.Tasks.Task<TResult[]> WhenAll<TResult>(params System.Threading.Tasks.Task<TResult>[] tasks)`
- `public static System.Threading.Tasks.Task<System.Threading.Tasks.Task> WhenAny(System.Collections.Generic.IEnumerable<System.Threading.Tasks.Task> tasks)`
- `public static System.Threading.Tasks.Task<System.Threading.Tasks.Task> WhenAny(System.ReadOnlySpan<System.Threading.Tasks.Task> tasks)`
- `public static System.Threading.Tasks.Task<System.Threading.Tasks.Task> WhenAny(params System.Threading.Tasks.Task[] tasks)`
- `public static System.Threading.Tasks.Task<System.Threading.Tasks.Task<TResult>> WhenAny<TResult>(System.Collections.Generic.IEnumerable<System.Threading.Tasks.Task<TResult>> tasks)`
- `public static System.Threading.Tasks.Task<System.Threading.Tasks.Task<TResult>> WhenAny<TResult>(System.ReadOnlySpan<System.Threading.Tasks.Task<TResult>> tasks)`
- `public static System.Threading.Tasks.Task<System.Threading.Tasks.Task<TResult>> WhenAny<TResult>(params System.Threading.Tasks.Task<TResult>[] tasks)`
- `public static System.Threading.Tasks.Task<System.Threading.Tasks.Task> WhenAny(System.Threading.Tasks.Task task1, System.Threading.Tasks.Task task2)`
- `public static System.Threading.Tasks.Task<System.Threading.Tasks.Task<TResult>> WhenAny<TResult>(System.Threading.Tasks.Task<TResult> task1, System.Threading.Tasks.Task<TResult> task2)`
- `public static System.Collections.Generic.IAsyncEnumerable<System.Threading.Tasks.Task> WhenEach(System.Collections.Generic.IEnumerable<System.Threading.Tasks.Task> tasks)`
- `public static System.Collections.Generic.IAsyncEnumerable<System.Threading.Tasks.Task> WhenEach(params System.Threading.Tasks.Task[] tasks)`
- `public static System.Collections.Generic.IAsyncEnumerable<System.Threading.Tasks.Task> WhenEach(System.ReadOnlySpan<System.Threading.Tasks.Task> tasks)`
- `public static System.Collections.Generic.IAsyncEnumerable<System.Threading.Tasks.Task<TResult>> WhenEach<TResult>(System.Collections.Generic.IEnumerable<System.Threading.Tasks.Task<TResult>> tasks)`
- `public static System.Collections.Generic.IAsyncEnumerable<System.Threading.Tasks.Task<TResult>> WhenEach<TResult>(params System.Threading.Tasks.Task<TResult>[] tasks)`
- `public static System.Collections.Generic.IAsyncEnumerable<System.Threading.Tasks.Task<TResult>> WhenEach<TResult>(System.ReadOnlySpan<System.Threading.Tasks.Task<TResult>> tasks)`
- `public static System.Runtime.CompilerServices.YieldAwaitable Yield()`

### TaskAsyncEnumerableExtensions (class [static])

- `[ext] public static System.Runtime.CompilerServices.ConfiguredAsyncDisposable ConfigureAwait(this System.IAsyncDisposable source, System.Boolean continueOnCapturedContext)`
- `[ext] public static System.Runtime.CompilerServices.ConfiguredCancelableAsyncEnumerable<T> ConfigureAwait<T>(this System.Collections.Generic.IAsyncEnumerable<T> source, System.Boolean continueOnCapturedContext)`
- `[ext] public static System.Collections.Generic.IEnumerable<T> ToBlockingEnumerable<T>(this System.Collections.Generic.IAsyncEnumerable<T> source, System.Threading.CancellationToken cancellationToken)`
- `[ext] public static System.Runtime.CompilerServices.ConfiguredCancelableAsyncEnumerable<T> WithCancellation<T>(this System.Collections.Generic.IAsyncEnumerable<T> source, System.Threading.CancellationToken cancellationToken)`

### TaskCanceledException (class) : System.OperationCanceledException, System.Runtime.Serialization.ISerializable

- `public TaskCanceledException()`
- `public TaskCanceledException(System.String message)`
- `public TaskCanceledException(System.Threading.Tasks.Task task)`
- `public TaskCanceledException(System.String message, System.Exception innerException)`
- `public TaskCanceledException(System.String message, System.Exception innerException, System.Threading.CancellationToken token)`
- `public System.Threading.Tasks.Task Task { get; }`

### TaskCompletionSource (class)

- `public TaskCompletionSource()`
- `public TaskCompletionSource(System.Object state)`
- `public TaskCompletionSource(System.Threading.Tasks.TaskCreationOptions creationOptions)`
- `public TaskCompletionSource(System.Object state, System.Threading.Tasks.TaskCreationOptions creationOptions)`
- `public System.Threading.Tasks.Task Task { get; }`
- `public System.Void SetCanceled()`
- `public System.Void SetCanceled(System.Threading.CancellationToken cancellationToken)`
- `public System.Void SetException(System.Collections.Generic.IEnumerable<System.Exception> exceptions)`
- `public System.Void SetException(System.Exception exception)`
- `public System.Void SetFromTask(System.Threading.Tasks.Task completedTask)`
- `public System.Void SetResult()`
- `public System.Boolean TrySetCanceled()`
- `public System.Boolean TrySetCanceled(System.Threading.CancellationToken cancellationToken)`
- `public System.Boolean TrySetException(System.Collections.Generic.IEnumerable<System.Exception> exceptions)`
- `public System.Boolean TrySetException(System.Exception exception)`
- `public System.Boolean TrySetFromTask(System.Threading.Tasks.Task completedTask)`
- `public System.Boolean TrySetResult()`

### TaskCompletionSource`1<TResult> (class)

- `public TaskCompletionSource`1()`
- `public TaskCompletionSource`1(System.Object state)`
- `public TaskCompletionSource`1(System.Threading.Tasks.TaskCreationOptions creationOptions)`
- `public TaskCompletionSource`1(System.Object state, System.Threading.Tasks.TaskCreationOptions creationOptions)`
- `public System.Threading.Tasks.Task<TResult> Task { get; }`
- `public System.Void SetCanceled()`
- `public System.Void SetCanceled(System.Threading.CancellationToken cancellationToken)`
- `public System.Void SetException(System.Collections.Generic.IEnumerable<System.Exception> exceptions)`
- `public System.Void SetException(System.Exception exception)`
- `public System.Void SetFromTask(System.Threading.Tasks.Task<TResult> completedTask)`
- `public System.Void SetResult(TResult result)`
- `public System.Boolean TrySetCanceled()`
- `public System.Boolean TrySetCanceled(System.Threading.CancellationToken cancellationToken)`
- `public System.Boolean TrySetException(System.Collections.Generic.IEnumerable<System.Exception> exceptions)`
- `public System.Boolean TrySetException(System.Exception exception)`
- `public System.Boolean TrySetFromTask(System.Threading.Tasks.Task<TResult> completedTask)`
- `public System.Boolean TrySetResult(TResult result)`

### TaskContinuationOptions (enum) : System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable

- `None = 0`
- `PreferFairness = 1`
- `LongRunning = 2`
- `AttachedToParent = 4`
- `DenyChildAttach = 8`
- `HideScheduler = 16`
- `LazyCancellation = 32`
- `RunContinuationsAsynchronously = 64`
- `NotOnRanToCompletion = 65536`
- `NotOnFaulted = 131072`
- `OnlyOnCanceled = 196608`
- `NotOnCanceled = 262144`
- `OnlyOnFaulted = 327680`
- `OnlyOnRanToCompletion = 393216`
- `ExecuteSynchronously = 524288`

### TaskCreationOptions (enum) : System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable

- `None = 0`
- `PreferFairness = 1`
- `LongRunning = 2`
- `AttachedToParent = 4`
- `DenyChildAttach = 8`
- `HideScheduler = 16`
- `RunContinuationsAsynchronously = 64`

### TaskExtensions (class [static])

- `[ext] public static System.Threading.Tasks.Task Unwrap(this System.Threading.Tasks.Task<System.Threading.Tasks.Task> task)`
- `[ext] public static System.Threading.Tasks.Task<TResult> Unwrap<TResult>(this System.Threading.Tasks.Task<System.Threading.Tasks.Task<TResult>> task)`

### TaskFactory (class)

- `public TaskFactory()`
- `public TaskFactory(System.Threading.CancellationToken cancellationToken)`
- `public TaskFactory(System.Threading.Tasks.TaskScheduler scheduler)`
- `public TaskFactory(System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskContinuationOptions continuationOptions)`
- `public TaskFactory(System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler)`
- `public System.Threading.CancellationToken CancellationToken { get; }`
- `public System.Threading.Tasks.TaskContinuationOptions ContinuationOptions { get; }`
- `public System.Threading.Tasks.TaskCreationOptions CreationOptions { get; }`
- `public System.Threading.Tasks.TaskScheduler Scheduler { get; }`
- `public System.Threading.Tasks.Task ContinueWhenAll(System.Threading.Tasks.Task[] tasks, System.Action<System.Threading.Tasks.Task[]> continuationAction)`
- `public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TResult>(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task[], TResult> continuationFunction)`
- `public System.Threading.Tasks.Task ContinueWhenAll<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Action<System.Threading.Tasks.Task<TAntecedentResult>[]> continuationAction)`
- `public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>[], TResult> continuationFunction)`
- `public System.Threading.Tasks.Task ContinueWhenAll(System.Threading.Tasks.Task[] tasks, System.Action<System.Threading.Tasks.Task[]> continuationAction, System.Threading.CancellationToken cancellationToken)`
- `public System.Threading.Tasks.Task ContinueWhenAll(System.Threading.Tasks.Task[] tasks, System.Action<System.Threading.Tasks.Task[]> continuationAction, System.Threading.Tasks.TaskContinuationOptions continuationOptions)`
- `public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TResult>(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task[], TResult> continuationFunction, System.Threading.CancellationToken cancellationToken)`
- `public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TResult>(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task[], TResult> continuationFunction, System.Threading.Tasks.TaskContinuationOptions continuationOptions)`
- `public System.Threading.Tasks.Task ContinueWhenAll<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Action<System.Threading.Tasks.Task<TAntecedentResult>[]> continuationAction, System.Threading.CancellationToken cancellationToken)`
- `public System.Threading.Tasks.Task ContinueWhenAll<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Action<System.Threading.Tasks.Task<TAntecedentResult>[]> continuationAction, System.Threading.Tasks.TaskContinuationOptions continuationOptions)`
- `public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>[], TResult> continuationFunction, System.Threading.CancellationToken cancellationToken)`
- `public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>[], TResult> continuationFunction, System.Threading.Tasks.TaskContinuationOptions continuationOptions)`
- `public System.Threading.Tasks.Task ContinueWhenAll(System.Threading.Tasks.Task[] tasks, System.Action<System.Threading.Tasks.Task[]> continuationAction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler)`
- `public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TResult>(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task[], TResult> continuationFunction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler)`
- `public System.Threading.Tasks.Task ContinueWhenAll<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Action<System.Threading.Tasks.Task<TAntecedentResult>[]> continuationAction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler)`
- `public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>[], TResult> continuationFunction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler)`
- `public System.Threading.Tasks.Task ContinueWhenAny(System.Threading.Tasks.Task[] tasks, System.Action<System.Threading.Tasks.Task> continuationAction)`
- `public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TResult>(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task, TResult> continuationFunction)`
- `public System.Threading.Tasks.Task ContinueWhenAny<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Action<System.Threading.Tasks.Task<TAntecedentResult>> continuationAction)`
- `public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>, TResult> continuationFunction)`
- `public System.Threading.Tasks.Task ContinueWhenAny(System.Threading.Tasks.Task[] tasks, System.Action<System.Threading.Tasks.Task> continuationAction, System.Threading.CancellationToken cancellationToken)`
- `public System.Threading.Tasks.Task ContinueWhenAny(System.Threading.Tasks.Task[] tasks, System.Action<System.Threading.Tasks.Task> continuationAction, System.Threading.Tasks.TaskContinuationOptions continuationOptions)`
- `public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TResult>(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task, TResult> continuationFunction, System.Threading.CancellationToken cancellationToken)`
- `public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TResult>(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task, TResult> continuationFunction, System.Threading.Tasks.TaskContinuationOptions continuationOptions)`
- `public System.Threading.Tasks.Task ContinueWhenAny<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Action<System.Threading.Tasks.Task<TAntecedentResult>> continuationAction, System.Threading.CancellationToken cancellationToken)`
- `public System.Threading.Tasks.Task ContinueWhenAny<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Action<System.Threading.Tasks.Task<TAntecedentResult>> continuationAction, System.Threading.Tasks.TaskContinuationOptions continuationOptions)`
- `public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>, TResult> continuationFunction, System.Threading.CancellationToken cancellationToken)`
- `public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>, TResult> continuationFunction, System.Threading.Tasks.TaskContinuationOptions continuationOptions)`
- `public System.Threading.Tasks.Task ContinueWhenAny(System.Threading.Tasks.Task[] tasks, System.Action<System.Threading.Tasks.Task> continuationAction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler)`
- `public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TResult>(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task, TResult> continuationFunction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler)`
- `public System.Threading.Tasks.Task ContinueWhenAny<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Action<System.Threading.Tasks.Task<TAntecedentResult>> continuationAction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler)`
- `public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>, TResult> continuationFunction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler)`
- `public System.Threading.Tasks.Task FromAsync(System.IAsyncResult asyncResult, System.Action<System.IAsyncResult> endMethod)`
- `public System.Threading.Tasks.Task<TResult> FromAsync<TResult>(System.IAsyncResult asyncResult, System.Func<System.IAsyncResult, TResult> endMethod)`
- `public System.Threading.Tasks.Task FromAsync(System.Func<System.AsyncCallback, System.Object, System.IAsyncResult> beginMethod, System.Action<System.IAsyncResult> endMethod, System.Object state)`
- `public System.Threading.Tasks.Task FromAsync(System.IAsyncResult asyncResult, System.Action<System.IAsyncResult> endMethod, System.Threading.Tasks.TaskCreationOptions creationOptions)`
- `public System.Threading.Tasks.Task<TResult> FromAsync<TResult>(System.Func<System.AsyncCallback, System.Object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, System.Object state)`
- `public System.Threading.Tasks.Task<TResult> FromAsync<TResult>(System.IAsyncResult asyncResult, System.Func<System.IAsyncResult, TResult> endMethod, System.Threading.Tasks.TaskCreationOptions creationOptions)`
- `public System.Threading.Tasks.Task FromAsync(System.Func<System.AsyncCallback, System.Object, System.IAsyncResult> beginMethod, System.Action<System.IAsyncResult> endMethod, System.Object state, System.Threading.Tasks.TaskCreationOptions creationOptions)`
- `public System.Threading.Tasks.Task FromAsync(System.IAsyncResult asyncResult, System.Action<System.IAsyncResult> endMethod, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskScheduler scheduler)`
- `public System.Threading.Tasks.Task<TResult> FromAsync<TResult>(System.Func<System.AsyncCallback, System.Object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, System.Object state, System.Threading.Tasks.TaskCreationOptions creationOptions)`
- `public System.Threading.Tasks.Task FromAsync<TArg1>(System.Func<TArg1, System.AsyncCallback, System.Object, System.IAsyncResult> beginMethod, System.Action<System.IAsyncResult> endMethod, TArg1 arg1, System.Object state)`
- `public System.Threading.Tasks.Task<TResult> FromAsync<TResult>(System.IAsyncResult asyncResult, System.Func<System.IAsyncResult, TResult> endMethod, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskScheduler scheduler)`
- `public System.Threading.Tasks.Task<TResult> FromAsync<TArg1, TResult>(System.Func<TArg1, System.AsyncCallback, System.Object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, System.Object state)`
- `public System.Threading.Tasks.Task FromAsync<TArg1>(System.Func<TArg1, System.AsyncCallback, System.Object, System.IAsyncResult> beginMethod, System.Action<System.IAsyncResult> endMethod, TArg1 arg1, System.Object state, System.Threading.Tasks.TaskCreationOptions creationOptions)`
- `public System.Threading.Tasks.Task<TResult> FromAsync<TArg1, TResult>(System.Func<TArg1, System.AsyncCallback, System.Object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, System.Object state, System.Threading.Tasks.TaskCreationOptions creationOptions)`
- `public System.Threading.Tasks.Task FromAsync<TArg1, TArg2>(System.Func<TArg1, TArg2, System.AsyncCallback, System.Object, System.IAsyncResult> beginMethod, System.Action<System.IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, System.Object state)`
- `public System.Threading.Tasks.Task<TResult> FromAsync<TArg1, TArg2, TResult>(System.Func<TArg1, TArg2, System.AsyncCallback, System.Object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, System.Object state)`
- `public System.Threading.Tasks.Task FromAsync<TArg1, TArg2>(System.Func<TArg1, TArg2, System.AsyncCallback, System.Object, System.IAsyncResult> beginMethod, System.Action<System.IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, System.Object state, System.Threading.Tasks.TaskCreationOptions creationOptions)`
- `public System.Threading.Tasks.Task<TResult> FromAsync<TArg1, TArg2, TResult>(System.Func<TArg1, TArg2, System.AsyncCallback, System.Object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, System.Object state, System.Threading.Tasks.TaskCreationOptions creationOptions)`
- `public System.Threading.Tasks.Task FromAsync<TArg1, TArg2, TArg3>(System.Func<TArg1, TArg2, TArg3, System.AsyncCallback, System.Object, System.IAsyncResult> beginMethod, System.Action<System.IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, System.Object state)`
- `public System.Threading.Tasks.Task<TResult> FromAsync<TArg1, TArg2, TArg3, TResult>(System.Func<TArg1, TArg2, TArg3, System.AsyncCallback, System.Object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, System.Object state)`
- `public System.Threading.Tasks.Task FromAsync<TArg1, TArg2, TArg3>(System.Func<TArg1, TArg2, TArg3, System.AsyncCallback, System.Object, System.IAsyncResult> beginMethod, System.Action<System.IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, System.Object state, System.Threading.Tasks.TaskCreationOptions creationOptions)`
- `public System.Threading.Tasks.Task<TResult> FromAsync<TArg1, TArg2, TArg3, TResult>(System.Func<TArg1, TArg2, TArg3, System.AsyncCallback, System.Object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, System.Object state, System.Threading.Tasks.TaskCreationOptions creationOptions)`
- `public System.Threading.Tasks.Task StartNew(System.Action action)`
- `public System.Threading.Tasks.Task<TResult> StartNew<TResult>(System.Func<TResult> function)`
- `public System.Threading.Tasks.Task StartNew(System.Action action, System.Threading.CancellationToken cancellationToken)`
- `public System.Threading.Tasks.Task StartNew(System.Action action, System.Threading.Tasks.TaskCreationOptions creationOptions)`
- `public System.Threading.Tasks.Task StartNew(System.Action<System.Object> action, System.Object state)`
- `public System.Threading.Tasks.Task<TResult> StartNew<TResult>(System.Func<System.Object, TResult> function, System.Object state)`
- `public System.Threading.Tasks.Task<TResult> StartNew<TResult>(System.Func<TResult> function, System.Threading.CancellationToken cancellationToken)`
- `public System.Threading.Tasks.Task<TResult> StartNew<TResult>(System.Func<TResult> function, System.Threading.Tasks.TaskCreationOptions creationOptions)`
- `public System.Threading.Tasks.Task StartNew(System.Action<System.Object> action, System.Object state, System.Threading.CancellationToken cancellationToken)`
- `public System.Threading.Tasks.Task StartNew(System.Action<System.Object> action, System.Object state, System.Threading.Tasks.TaskCreationOptions creationOptions)`
- `public System.Threading.Tasks.Task<TResult> StartNew<TResult>(System.Func<System.Object, TResult> function, System.Object state, System.Threading.CancellationToken cancellationToken)`
- `public System.Threading.Tasks.Task<TResult> StartNew<TResult>(System.Func<System.Object, TResult> function, System.Object state, System.Threading.Tasks.TaskCreationOptions creationOptions)`
- `public System.Threading.Tasks.Task StartNew(System.Action action, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskScheduler scheduler)`
- `public System.Threading.Tasks.Task<TResult> StartNew<TResult>(System.Func<TResult> function, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskScheduler scheduler)`
- `public System.Threading.Tasks.Task StartNew(System.Action<System.Object> action, System.Object state, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskScheduler scheduler)`
- `public System.Threading.Tasks.Task<TResult> StartNew<TResult>(System.Func<System.Object, TResult> function, System.Object state, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskScheduler scheduler)`

### TaskFactory`1<TResult> (class)

- `public TaskFactory`1()`
- `public TaskFactory`1(System.Threading.CancellationToken cancellationToken)`
- `public TaskFactory`1(System.Threading.Tasks.TaskScheduler scheduler)`
- `public TaskFactory`1(System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskContinuationOptions continuationOptions)`
- `public TaskFactory`1(System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler)`
- `public System.Threading.CancellationToken CancellationToken { get; }`
- `public System.Threading.Tasks.TaskContinuationOptions ContinuationOptions { get; }`
- `public System.Threading.Tasks.TaskCreationOptions CreationOptions { get; }`
- `public System.Threading.Tasks.TaskScheduler Scheduler { get; }`
- `public System.Threading.Tasks.Task<TResult> ContinueWhenAll(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task[], TResult> continuationFunction)`
- `public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>[], TResult> continuationFunction)`
- `public System.Threading.Tasks.Task<TResult> ContinueWhenAll(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task[], TResult> continuationFunction, System.Threading.CancellationToken cancellationToken)`
- `public System.Threading.Tasks.Task<TResult> ContinueWhenAll(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task[], TResult> continuationFunction, System.Threading.Tasks.TaskContinuationOptions continuationOptions)`
- `public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>[], TResult> continuationFunction, System.Threading.CancellationToken cancellationToken)`
- `public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>[], TResult> continuationFunction, System.Threading.Tasks.TaskContinuationOptions continuationOptions)`
- `public System.Threading.Tasks.Task<TResult> ContinueWhenAll(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task[], TResult> continuationFunction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler)`
- `public System.Threading.Tasks.Task<TResult> ContinueWhenAll<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>[], TResult> continuationFunction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler)`
- `public System.Threading.Tasks.Task<TResult> ContinueWhenAny(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task, TResult> continuationFunction)`
- `public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>, TResult> continuationFunction)`
- `public System.Threading.Tasks.Task<TResult> ContinueWhenAny(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task, TResult> continuationFunction, System.Threading.CancellationToken cancellationToken)`
- `public System.Threading.Tasks.Task<TResult> ContinueWhenAny(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task, TResult> continuationFunction, System.Threading.Tasks.TaskContinuationOptions continuationOptions)`
- `public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>, TResult> continuationFunction, System.Threading.CancellationToken cancellationToken)`
- `public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>, TResult> continuationFunction, System.Threading.Tasks.TaskContinuationOptions continuationOptions)`
- `public System.Threading.Tasks.Task<TResult> ContinueWhenAny(System.Threading.Tasks.Task[] tasks, System.Func<System.Threading.Tasks.Task, TResult> continuationFunction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler)`
- `public System.Threading.Tasks.Task<TResult> ContinueWhenAny<TAntecedentResult>(System.Threading.Tasks.Task<TAntecedentResult>[] tasks, System.Func<System.Threading.Tasks.Task<TAntecedentResult>, TResult> continuationFunction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler)`
- `public System.Threading.Tasks.Task<TResult> FromAsync(System.IAsyncResult asyncResult, System.Func<System.IAsyncResult, TResult> endMethod)`
- `public System.Threading.Tasks.Task<TResult> FromAsync(System.Func<System.AsyncCallback, System.Object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, System.Object state)`
- `public System.Threading.Tasks.Task<TResult> FromAsync(System.IAsyncResult asyncResult, System.Func<System.IAsyncResult, TResult> endMethod, System.Threading.Tasks.TaskCreationOptions creationOptions)`
- `public System.Threading.Tasks.Task<TResult> FromAsync(System.Func<System.AsyncCallback, System.Object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, System.Object state, System.Threading.Tasks.TaskCreationOptions creationOptions)`
- `public System.Threading.Tasks.Task<TResult> FromAsync(System.IAsyncResult asyncResult, System.Func<System.IAsyncResult, TResult> endMethod, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskScheduler scheduler)`
- `public System.Threading.Tasks.Task<TResult> FromAsync<TArg1>(System.Func<TArg1, System.AsyncCallback, System.Object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, System.Object state)`
- `public System.Threading.Tasks.Task<TResult> FromAsync<TArg1>(System.Func<TArg1, System.AsyncCallback, System.Object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, System.Object state, System.Threading.Tasks.TaskCreationOptions creationOptions)`
- `public System.Threading.Tasks.Task<TResult> FromAsync<TArg1, TArg2>(System.Func<TArg1, TArg2, System.AsyncCallback, System.Object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, System.Object state)`
- `public System.Threading.Tasks.Task<TResult> FromAsync<TArg1, TArg2>(System.Func<TArg1, TArg2, System.AsyncCallback, System.Object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, System.Object state, System.Threading.Tasks.TaskCreationOptions creationOptions)`
- `public System.Threading.Tasks.Task<TResult> FromAsync<TArg1, TArg2, TArg3>(System.Func<TArg1, TArg2, TArg3, System.AsyncCallback, System.Object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, System.Object state)`
- `public System.Threading.Tasks.Task<TResult> FromAsync<TArg1, TArg2, TArg3>(System.Func<TArg1, TArg2, TArg3, System.AsyncCallback, System.Object, System.IAsyncResult> beginMethod, System.Func<System.IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, System.Object state, System.Threading.Tasks.TaskCreationOptions creationOptions)`
- `public System.Threading.Tasks.Task<TResult> StartNew(System.Func<TResult> function)`
- `public System.Threading.Tasks.Task<TResult> StartNew(System.Func<System.Object, TResult> function, System.Object state)`
- `public System.Threading.Tasks.Task<TResult> StartNew(System.Func<TResult> function, System.Threading.CancellationToken cancellationToken)`
- `public System.Threading.Tasks.Task<TResult> StartNew(System.Func<TResult> function, System.Threading.Tasks.TaskCreationOptions creationOptions)`
- `public System.Threading.Tasks.Task<TResult> StartNew(System.Func<System.Object, TResult> function, System.Object state, System.Threading.CancellationToken cancellationToken)`
- `public System.Threading.Tasks.Task<TResult> StartNew(System.Func<System.Object, TResult> function, System.Object state, System.Threading.Tasks.TaskCreationOptions creationOptions)`
- `public System.Threading.Tasks.Task<TResult> StartNew(System.Func<TResult> function, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskScheduler scheduler)`
- `public System.Threading.Tasks.Task<TResult> StartNew(System.Func<System.Object, TResult> function, System.Object state, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions, System.Threading.Tasks.TaskScheduler scheduler)`

### TaskScheduler (class [abstract])

- `public static System.Threading.Tasks.TaskScheduler Current { get; }`
- `public static System.Threading.Tasks.TaskScheduler Default { get; }`
- `public System.Int32 Id { get; }`
- `public System.Int32 MaximumConcurrencyLevel { get; }`
- `public static System.Threading.Tasks.TaskScheduler FromCurrentSynchronizationContext()`
- `public event System.EventHandler<System.Threading.Tasks.UnobservedTaskExceptionEventArgs> UnobservedTaskException`

### TaskSchedulerException (class) : System.Exception, System.Runtime.Serialization.ISerializable

- `public TaskSchedulerException()`
- `public TaskSchedulerException(System.Exception innerException)`
- `public TaskSchedulerException(System.String message)`
- `public TaskSchedulerException(System.String message, System.Exception innerException)`

### TaskStatus (enum) : System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable

- `Created = 0`
- `WaitingForActivation = 1`
- `WaitingToRun = 2`
- `Running = 3`
- `WaitingForChildrenToComplete = 4`
- `RanToCompletion = 5`
- `Canceled = 6`
- `Faulted = 7`

### TaskToAsyncResult (class [static])

- `public static System.IAsyncResult Begin(System.Threading.Tasks.Task task, System.AsyncCallback callback, System.Object state)`
- `public static System.Void End(System.IAsyncResult asyncResult)`
- `public static TResult End<TResult>(System.IAsyncResult asyncResult)`
- `public static System.Threading.Tasks.Task Unwrap(System.IAsyncResult asyncResult)`
- `public static System.Threading.Tasks.Task<TResult> Unwrap<TResult>(System.IAsyncResult asyncResult)`

### Task`1<TResult> (class) : System.Threading.Tasks.Task, System.IAsyncResult, System.IDisposable

- `public Task`1(System.Func<TResult> function)`
- `public Task`1(System.Func<System.Object, TResult> function, System.Object state)`
- `public Task`1(System.Func<TResult> function, System.Threading.CancellationToken cancellationToken)`
- `public Task`1(System.Func<TResult> function, System.Threading.Tasks.TaskCreationOptions creationOptions)`
- `public Task`1(System.Func<System.Object, TResult> function, System.Object state, System.Threading.CancellationToken cancellationToken)`
- `public Task`1(System.Func<System.Object, TResult> function, System.Object state, System.Threading.Tasks.TaskCreationOptions creationOptions)`
- `public Task`1(System.Func<TResult> function, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions)`
- `public Task`1(System.Func<System.Object, TResult> function, System.Object state, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskCreationOptions creationOptions)`
- `public static System.Threading.Tasks.TaskFactory<TResult> Factory { get; }`
- `public TResult Result { get; }`
- `public System.Runtime.CompilerServices.ConfiguredTaskAwaitable<TResult> ConfigureAwait(System.Boolean continueOnCapturedContext)`
- `public System.Runtime.CompilerServices.ConfiguredTaskAwaitable<TResult> ConfigureAwait(System.Threading.Tasks.ConfigureAwaitOptions options)`
- `public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task<TResult>> continuationAction)`
- `public System.Threading.Tasks.Task<TNewResult> ContinueWith<TNewResult>(System.Func<System.Threading.Tasks.Task<TResult>, TNewResult> continuationFunction)`
- `public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task<TResult>, System.Object> continuationAction, System.Object state)`
- `public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task<TResult>> continuationAction, System.Threading.CancellationToken cancellationToken)`
- `public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task<TResult>> continuationAction, System.Threading.Tasks.TaskContinuationOptions continuationOptions)`
- `public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task<TResult>> continuationAction, System.Threading.Tasks.TaskScheduler scheduler)`
- `public System.Threading.Tasks.Task<TNewResult> ContinueWith<TNewResult>(System.Func<System.Threading.Tasks.Task<TResult>, System.Object, TNewResult> continuationFunction, System.Object state)`
- `public System.Threading.Tasks.Task<TNewResult> ContinueWith<TNewResult>(System.Func<System.Threading.Tasks.Task<TResult>, TNewResult> continuationFunction, System.Threading.CancellationToken cancellationToken)`
- `public System.Threading.Tasks.Task<TNewResult> ContinueWith<TNewResult>(System.Func<System.Threading.Tasks.Task<TResult>, TNewResult> continuationFunction, System.Threading.Tasks.TaskContinuationOptions continuationOptions)`
- `public System.Threading.Tasks.Task<TNewResult> ContinueWith<TNewResult>(System.Func<System.Threading.Tasks.Task<TResult>, TNewResult> continuationFunction, System.Threading.Tasks.TaskScheduler scheduler)`
- `public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task<TResult>, System.Object> continuationAction, System.Object state, System.Threading.CancellationToken cancellationToken)`
- `public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task<TResult>, System.Object> continuationAction, System.Object state, System.Threading.Tasks.TaskContinuationOptions continuationOptions)`
- `public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task<TResult>, System.Object> continuationAction, System.Object state, System.Threading.Tasks.TaskScheduler scheduler)`
- `public System.Threading.Tasks.Task<TNewResult> ContinueWith<TNewResult>(System.Func<System.Threading.Tasks.Task<TResult>, System.Object, TNewResult> continuationFunction, System.Object state, System.Threading.CancellationToken cancellationToken)`
- `public System.Threading.Tasks.Task<TNewResult> ContinueWith<TNewResult>(System.Func<System.Threading.Tasks.Task<TResult>, System.Object, TNewResult> continuationFunction, System.Object state, System.Threading.Tasks.TaskContinuationOptions continuationOptions)`
- `public System.Threading.Tasks.Task<TNewResult> ContinueWith<TNewResult>(System.Func<System.Threading.Tasks.Task<TResult>, System.Object, TNewResult> continuationFunction, System.Object state, System.Threading.Tasks.TaskScheduler scheduler)`
- `public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task<TResult>> continuationAction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler)`
- `public System.Threading.Tasks.Task<TNewResult> ContinueWith<TNewResult>(System.Func<System.Threading.Tasks.Task<TResult>, TNewResult> continuationFunction, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler)`
- `public System.Threading.Tasks.Task ContinueWith(System.Action<System.Threading.Tasks.Task<TResult>, System.Object> continuationAction, System.Object state, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler)`
- `public System.Threading.Tasks.Task<TNewResult> ContinueWith<TNewResult>(System.Func<System.Threading.Tasks.Task<TResult>, System.Object, TNewResult> continuationFunction, System.Object state, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.TaskContinuationOptions continuationOptions, System.Threading.Tasks.TaskScheduler scheduler)`
- `public System.Runtime.CompilerServices.TaskAwaiter<TResult> GetAwaiter()`
- `public System.Threading.Tasks.Task<TResult> WaitAsync(System.Threading.CancellationToken cancellationToken)`
- `public System.Threading.Tasks.Task<TResult> WaitAsync(System.TimeSpan timeout)`
- `public System.Threading.Tasks.Task<TResult> WaitAsync(System.TimeSpan timeout, System.Threading.CancellationToken cancellationToken)`
- `public System.Threading.Tasks.Task<TResult> WaitAsync(System.TimeSpan timeout, System.TimeProvider timeProvider)`
- `public System.Threading.Tasks.Task<TResult> WaitAsync(System.TimeSpan timeout, System.TimeProvider timeProvider, System.Threading.CancellationToken cancellationToken)`

### ValueTask (struct [readonly struct]) : System.IEquatable<System.Threading.Tasks.ValueTask>

- `public ValueTask(System.Threading.Tasks.Task task)`
- `public ValueTask(System.Threading.Tasks.Sources.IValueTaskSource source, System.Int16 token)`
- `public static System.Threading.Tasks.ValueTask CompletedTask { get; }`
- `public System.Boolean IsCanceled { get; }`
- `public System.Boolean IsCompleted { get; }`
- `public System.Boolean IsCompletedSuccessfully { get; }`
- `public System.Boolean IsFaulted { get; }`
- `public System.Threading.Tasks.Task AsTask()`
- `public System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable ConfigureAwait(System.Boolean continueOnCapturedContext)`
- `public System.Boolean Equals(System.Object obj)`
- `public System.Boolean Equals(System.Threading.Tasks.ValueTask other)`
- `public static System.Threading.Tasks.ValueTask FromCanceled(System.Threading.CancellationToken cancellationToken)`
- `public static System.Threading.Tasks.ValueTask<TResult> FromCanceled<TResult>(System.Threading.CancellationToken cancellationToken)`
- `public static System.Threading.Tasks.ValueTask FromException(System.Exception exception)`
- `public static System.Threading.Tasks.ValueTask<TResult> FromException<TResult>(System.Exception exception)`
- `public static System.Threading.Tasks.ValueTask<TResult> FromResult<TResult>(TResult result)`
- `public System.Runtime.CompilerServices.ValueTaskAwaiter GetAwaiter()`
- `public System.Int32 GetHashCode()`
- `public System.Threading.Tasks.ValueTask Preserve()`
- `public static System.Boolean op_Equality(System.Threading.Tasks.ValueTask left, System.Threading.Tasks.ValueTask right)`
- `public static System.Boolean op_Inequality(System.Threading.Tasks.ValueTask left, System.Threading.Tasks.ValueTask right)`

### ValueTask`1<TResult> (struct [readonly struct]) : System.IEquatable<System.Threading.Tasks.ValueTask<TResult>>

- `public ValueTask`1(System.Threading.Tasks.Task<TResult> task)`
- `public ValueTask`1(TResult result)`
- `public ValueTask`1(System.Threading.Tasks.Sources.IValueTaskSource<TResult> source, System.Int16 token)`
- `public System.Boolean IsCanceled { get; }`
- `public System.Boolean IsCompleted { get; }`
- `public System.Boolean IsCompletedSuccessfully { get; }`
- `public System.Boolean IsFaulted { get; }`
- `public TResult Result { get; }`
- `public System.Threading.Tasks.Task<TResult> AsTask()`
- `public System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable<TResult> ConfigureAwait(System.Boolean continueOnCapturedContext)`
- `public System.Boolean Equals(System.Object obj)`
- `public System.Boolean Equals(System.Threading.Tasks.ValueTask<TResult> other)`
- `public System.Runtime.CompilerServices.ValueTaskAwaiter<TResult> GetAwaiter()`
- `public System.Int32 GetHashCode()`
- `public System.Threading.Tasks.ValueTask<TResult> Preserve()`
- `public System.String ToString()`
- `public static System.Boolean op_Equality(System.Threading.Tasks.ValueTask<TResult> left, System.Threading.Tasks.ValueTask<TResult> right)`
- `public static System.Boolean op_Inequality(System.Threading.Tasks.ValueTask<TResult> left, System.Threading.Tasks.ValueTask<TResult> right)`

