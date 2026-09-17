# Public API inventory: System.Net.ServerSentEvents (F6/F10 streaming)

Assemblies: System.Net.ServerSentEvents 10.0.0.0

Type count: 5

## System.Net.ServerSentEvents

### SseFormatter (class [static])

- `public static System.Threading.Tasks.Task WriteAsync(System.Collections.Generic.IAsyncEnumerable<System.Net.ServerSentEvents.SseItem<System.String>> source, System.IO.Stream destination, System.Threading.CancellationToken cancellationToken)`
- `public static System.Threading.Tasks.Task WriteAsync<T>(System.Collections.Generic.IAsyncEnumerable<System.Net.ServerSentEvents.SseItem<T>> source, System.IO.Stream destination, System.Action<System.Net.ServerSentEvents.SseItem<T>, System.Buffers.IBufferWriter<System.Byte>> itemFormatter, System.Threading.CancellationToken cancellationToken)`

### SseItemParser`1<T> (delegate [sealed]) : System.ICloneable, System.Runtime.Serialization.ISerializable

- `public SseItemParser`1(System.Object object, System.IntPtr method)`
- `delegate T Invoke(System.String eventType, System.ReadOnlySpan<System.Byte> data)`
- `public System.IAsyncResult BeginInvoke(System.String eventType, System.ReadOnlySpan<System.Byte> data, System.AsyncCallback callback, System.Object object)`
- `public T EndInvoke(System.IAsyncResult result)`

### SseItem`1<T> (struct [readonly struct])

- `public SseItem`1(T data, System.String eventType)`
- `public T Data { get; }`
- `public System.String EventId { get; init; }`
- `public System.String EventType { get; }`
- `public System.TimeSpan? ReconnectionInterval { get; init; }`

### SseParser (class [static])

- `public const System.String EventTypeDefault`
- `public static System.Net.ServerSentEvents.SseParser<System.String> Create(System.IO.Stream sseStream)`
- `public static System.Net.ServerSentEvents.SseParser<T> Create<T>(System.IO.Stream sseStream, System.Net.ServerSentEvents.SseItemParser<T> itemParser)`

### SseParser`1<T> (class [sealed])

- `public System.String LastEventId { get; }`
- `public System.TimeSpan ReconnectionInterval { get; }`
- `public System.Collections.Generic.IEnumerable<System.Net.ServerSentEvents.SseItem<T>> Enumerate()`
- `public System.Collections.Generic.IAsyncEnumerable<System.Net.ServerSentEvents.SseItem<T>> EnumerateAsync(System.Threading.CancellationToken cancellationToken)`

