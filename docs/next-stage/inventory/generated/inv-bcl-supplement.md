# Public API inventory: bcl-supplement (F7 environment, F11 diagnostics/AOT/serialization)

Assemblies: System.Runtime 10.0.0.0, System.Threading 10.0.0.0, System.ComponentModel 10.0.0.0, System.Collections 10.0.0.0, System.Text.Json 10.0.0.0

Type count: 27

## System

### GC (class [static])

- `public static System.Int32 MaxGeneration { get; }`
- `public static System.Void AddMemoryPressure(System.Int64 bytesAllocated)`
- `public static T[] AllocateArray<T>(System.Int32 length, System.Boolean pinned)`
- `public static T[] AllocateUninitializedArray<T>(System.Int32 length, System.Boolean pinned)`
- `public static System.Void CancelFullGCNotification()`
- `public static System.Void Collect()`
- `public static System.Void Collect(System.Int32 generation)`
- `public static System.Void Collect(System.Int32 generation, System.GCCollectionMode mode)`
- `public static System.Void Collect(System.Int32 generation, System.GCCollectionMode mode, System.Boolean blocking)`
- `public static System.Void Collect(System.Int32 generation, System.GCCollectionMode mode, System.Boolean blocking, System.Boolean compacting)`
- `public static System.Int32 CollectionCount(System.Int32 generation)`
- `public static System.Void EndNoGCRegion()`
- `public static System.Int64 GetAllocatedBytesForCurrentThread()`
- `public static System.Collections.Generic.IReadOnlyDictionary<System.String, System.Object> GetConfigurationVariables()`
- `public static System.GCMemoryInfo GetGCMemoryInfo()`
- `public static System.GCMemoryInfo GetGCMemoryInfo(System.GCKind kind)`
- `public static System.Int32 GetGeneration(System.Object obj)`
- `public static System.Int32 GetGeneration(System.WeakReference wo)`
- `public static System.Int64 GetTotalAllocatedBytes(System.Boolean precise)`
- `public static System.Int64 GetTotalMemory(System.Boolean forceFullCollection)`
- `public static System.TimeSpan GetTotalPauseDuration()`
- `public static System.Void KeepAlive(System.Object obj)`
- `public static System.Void ReRegisterForFinalize(System.Object obj)`
- `public static System.Void RefreshMemoryLimit()`
- `public static System.Void RegisterForFullGCNotification(System.Int32 maxGenerationThreshold, System.Int32 largeObjectHeapThreshold)`
- `public static System.Void RegisterNoGCRegionCallback(System.Int64 totalSize, System.Action callback)`
- `public static System.Void RemoveMemoryPressure(System.Int64 bytesAllocated)`
- `public static System.Void SuppressFinalize(System.Object obj)`
- `public static System.Boolean TryStartNoGCRegion(System.Int64 totalSize)`
- `public static System.Boolean TryStartNoGCRegion(System.Int64 totalSize, System.Boolean disallowFullBlockingGC)`
- `public static System.Boolean TryStartNoGCRegion(System.Int64 totalSize, System.Int64 lohSize)`
- `public static System.Boolean TryStartNoGCRegion(System.Int64 totalSize, System.Int64 lohSize, System.Boolean disallowFullBlockingGC)`
- `public static System.GCNotificationStatus WaitForFullGCApproach()`
- `public static System.GCNotificationStatus WaitForFullGCApproach(System.Int32 millisecondsTimeout)`
- `public static System.GCNotificationStatus WaitForFullGCApproach(System.TimeSpan timeout)`
- `public static System.GCNotificationStatus WaitForFullGCComplete()`
- `public static System.GCNotificationStatus WaitForFullGCComplete(System.Int32 millisecondsTimeout)`
- `public static System.GCNotificationStatus WaitForFullGCComplete(System.TimeSpan timeout)`
- `public static System.Void WaitForPendingFinalizers()`

### GCCollectionMode (enum) : System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable

- `Default = 0`
- `Forced = 1`
- `Optimized = 2`
- `Aggressive = 3`

### GCKind (enum) : System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable

- `Any = 0`
- `Ephemeral = 1`
- `FullBlocking = 2`
- `Background = 3`

### GCMemoryInfo (struct [readonly struct])

- `public System.Boolean Compacted { get; }`
- `public System.Boolean Concurrent { get; }`
- `public System.Int64 FinalizationPendingCount { get; }`
- `public System.Int64 FragmentedBytes { get; }`
- `public System.Int32 Generation { get; }`
- `public System.ReadOnlySpan<System.GCGenerationInfo> GenerationInfo { get; }`
- `public System.Int64 HeapSizeBytes { get; }`
- `public System.Int64 HighMemoryLoadThresholdBytes { get; }`
- `public System.Int64 Index { get; }`
- `public System.Int64 MemoryLoadBytes { get; }`
- `public System.ReadOnlySpan<System.TimeSpan> PauseDurations { get; }`
- `public System.Double PauseTimePercentage { get; }`
- `public System.Int64 PinnedObjectsCount { get; }`
- `public System.Int64 PromotedBytes { get; }`
- `public System.Int64 TotalAvailableMemoryBytes { get; }`
- `public System.Int64 TotalCommittedBytes { get; }`

### IServiceProvider (interface)

- `public System.Object GetService(System.Type serviceType)`

## System.Collections.Generic

### CollectionExtensions (class [static])

- `[ext] public static System.Void AddRange<T>(this System.Collections.Generic.List<T> list, System.ReadOnlySpan<T> source)`
- `[ext] public static System.Collections.ObjectModel.ReadOnlyCollection<T> AsReadOnly<T>(this System.Collections.Generic.IList<T> list)`
- `[ext] public static System.Collections.ObjectModel.ReadOnlySet<T> AsReadOnly<T>(this System.Collections.Generic.ISet<T> set)`
- `[ext] public static System.Collections.ObjectModel.ReadOnlyDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(this System.Collections.Generic.IDictionary<TKey, TValue> dictionary)`
- `[ext] public static System.Void CopyTo<T>(this System.Collections.Generic.List<T> list, System.Span<T> destination)`
- `[ext] public static TValue GetValueOrDefault<TKey, TValue>(this System.Collections.Generic.IReadOnlyDictionary<TKey, TValue> dictionary, TKey key)`
- `[ext] public static TValue GetValueOrDefault<TKey, TValue>(this System.Collections.Generic.IReadOnlyDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)`
- `[ext] public static System.Void InsertRange<T>(this System.Collections.Generic.List<T> list, System.Int32 index, System.ReadOnlySpan<T> source)`
- `[ext] public static System.Boolean Remove<TKey, TValue>(this System.Collections.Generic.IDictionary<TKey, TValue> dictionary, TKey key, out TValue& value)`
- `[ext] public static System.Boolean TryAdd<TKey, TValue>(this System.Collections.Generic.IDictionary<TKey, TValue> dictionary, TKey key, TValue value)`

## System.Diagnostics

### Stopwatch (class)

- `public Stopwatch()`
- `public static System.Int64 Frequency`
- `public static System.Boolean IsHighResolution`
- `public System.TimeSpan Elapsed { get; }`
- `public System.Int64 ElapsedMilliseconds { get; }`
- `public System.Int64 ElapsedTicks { get; }`
- `public System.Boolean IsRunning { get; }`
- `public static System.TimeSpan GetElapsedTime(System.Int64 startingTimestamp)`
- `public static System.TimeSpan GetElapsedTime(System.Int64 startingTimestamp, System.Int64 endingTimestamp)`
- `public static System.Int64 GetTimestamp()`
- `public System.Void Reset()`
- `public System.Void Restart()`
- `public System.Void Start()`
- `public static System.Diagnostics.Stopwatch StartNew()`
- `public System.Void Stop()`
- `public System.String ToString()`

## System.Diagnostics.CodeAnalysis

### ConstantExpectedAttribute (class [sealed]) : System.Attribute

- `public ConstantExpectedAttribute()`
- `public System.Object Max { get; set; }`
- `public System.Object Min { get; set; }`

### DynamicallyAccessedMemberTypes (enum) : System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable

- `All = -1`
- `None = 0`
- `PublicParameterlessConstructor = 1`
- `PublicConstructors = 3`
- `NonPublicConstructors = 4`
- `PublicMethods = 8`
- `NonPublicMethods = 16`
- `PublicFields = 32`
- `NonPublicFields = 64`
- `PublicNestedTypes = 128`
- `NonPublicNestedTypes = 256`
- `PublicProperties = 512`
- `NonPublicProperties = 1024`
- `PublicEvents = 2048`
- `NonPublicEvents = 4096`
- `Interfaces = 8192`
- `NonPublicConstructorsWithInherited = 16388`
- `NonPublicMethodsWithInherited = 32784`
- `AllMethods = 32792`
- `NonPublicFieldsWithInherited = 65600`
- `AllFields = 65632`
- `NonPublicNestedTypesWithInherited = 131328`
- `NonPublicPropertiesWithInherited = 263168`
- `AllProperties = 263680`
- `NonPublicEventsWithInherited = 528384`
- `AllEvents = 530432`
- `PublicConstructorsWithInherited = 1048579`
- `AllConstructors = 1064967`
- `PublicNestedTypesWithInherited = 2097280`
- `AllNestedTypes = 2228608`

### DynamicallyAccessedMembersAttribute (class [sealed]) : System.Attribute

- `public DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes memberTypes)`
- `public System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes MemberTypes { get; }`

### ExperimentalAttribute (class [sealed]) : System.Attribute

- `public ExperimentalAttribute(System.String diagnosticId)`
- `public System.String DiagnosticId { get; }`
- `public System.String Message { get; set; }`
- `public System.String UrlFormat { get; set; }`

### FeatureGuardAttribute (class [sealed]) : System.Attribute

- `public FeatureGuardAttribute(System.Type featureType)`
- `public System.Type FeatureType { get; }`

### FeatureSwitchDefinitionAttribute (class [sealed]) : System.Attribute

- `public FeatureSwitchDefinitionAttribute(System.String switchName)`
- `public System.String SwitchName { get; }`

### RequiresAssemblyFilesAttribute (class [sealed]) : System.Attribute

- `public RequiresAssemblyFilesAttribute()`
- `public RequiresAssemblyFilesAttribute(System.String message)`
- `public System.String Message { get; }`
- `public System.String Url { get; set; }`

### RequiresDynamicCodeAttribute (class [sealed]) : System.Attribute

- `public RequiresDynamicCodeAttribute(System.String message)`
- `public System.Boolean ExcludeStatics { get; set; }`
- `public System.String Message { get; }`
- `public System.String Url { get; set; }`

### RequiresUnreferencedCodeAttribute (class [sealed]) : System.Attribute

- `public RequiresUnreferencedCodeAttribute(System.String message)`
- `public System.Boolean ExcludeStatics { get; set; }`
- `public System.String Message { get; }`
- `public System.String Url { get; set; }`

### UnconditionalSuppressMessageAttribute (class [sealed]) : System.Attribute

- `public UnconditionalSuppressMessageAttribute(System.String category, System.String checkId)`
- `public System.String Category { get; }`
- `public System.String CheckId { get; }`
- `public System.String Justification { get; set; }`
- `public System.String MessageId { get; set; }`
- `public System.String Scope { get; set; }`
- `public System.String Target { get; set; }`

## System.Runtime.CompilerServices

### MethodImplAttribute (class [sealed]) : System.Attribute

- `public MethodImplAttribute()`
- `public MethodImplAttribute(System.Int16 value)`
- `public MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions methodImplOptions)`
- `public System.Runtime.CompilerServices.MethodCodeType MethodCodeType`
- `public System.Runtime.CompilerServices.MethodImplOptions Value { get; }`

### MethodImplOptions (enum) : System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable

- `Unmanaged = 4`
- `NoInlining = 8`
- `ForwardRef = 16`
- `Synchronized = 32`
- `NoOptimization = 64`
- `PreserveSig = 128`
- `AggressiveInlining = 256`
- `AggressiveOptimization = 512`
- `InternalCall = 4096`
- `Async = 8192`

### RuntimeFeature (class [static])

- `public const System.String ByRefFields`
- `public const System.String ByRefLikeGenerics`
- `public const System.String CovariantReturnsOfClasses`
- `public const System.String DefaultImplementationsOfInterfaces`
- `public const System.String NumericIntPtr`
- `public const System.String PortablePdb`
- `public const System.String UnmanagedSignatureCallingConvention`
- `public const System.String VirtualStaticsInInterfaces`
- `public static System.Boolean IsDynamicCodeCompiled { get; }`
- `public static System.Boolean IsDynamicCodeSupported { get; }`
- `public static System.Boolean IsSupported(System.String feature)`

### RuntimeHelpers (class [static])

- `public static System.Int32 OffsetToStringData { get; }`
- `public static System.IntPtr AllocateTypeAssociatedMemory(System.Type type, System.Int32 size)`
- `public static System.Object Box(ref System.Byte& target, System.RuntimeTypeHandle type)`
- `public static System.ReadOnlySpan<T> CreateSpan<T>(System.RuntimeFieldHandle fldHandle)`
- `public static System.Void EnsureSufficientExecutionStack()`
- `public static System.Boolean Equals(System.Object o1, System.Object o2)`
- `public static System.Void ExecuteCodeWithGuaranteedCleanup(System.Runtime.CompilerServices.RuntimeHelpers+TryCode code, System.Runtime.CompilerServices.RuntimeHelpers+CleanupCode backoutCode, System.Object userData)`
- `public static System.Int32 GetHashCode(System.Object o)`
- `public static System.Object GetObjectValue(System.Object obj)`
- `public static T[] GetSubArray<T>(T[] array, System.Range range)`
- `public static System.Object GetUninitializedObject(System.Type type)`
- `public static System.Void InitializeArray(System.Array array, System.RuntimeFieldHandle fldHandle)`
- `public static System.Boolean IsReferenceOrContainsReferences<T>()`
- `public static System.Void PrepareConstrainedRegions()`
- `public static System.Void PrepareConstrainedRegionsNoOP()`
- `public static System.Void PrepareContractedDelegate(System.Delegate d)`
- `public static System.Void PrepareDelegate(System.Delegate d)`
- `public static System.Void PrepareMethod(System.RuntimeMethodHandle method)`
- `public static System.Void PrepareMethod(System.RuntimeMethodHandle method, System.RuntimeTypeHandle[] instantiation)`
- `public static System.Void ProbeForSufficientStack()`
- `public static System.Void RunClassConstructor(System.RuntimeTypeHandle type)`
- `public static System.Void RunModuleConstructor(System.ModuleHandle module)`
- `public static System.Int32 SizeOf(System.RuntimeTypeHandle type)`
- `public static System.Boolean TryEnsureSufficientExecutionStack()`

### SkipLocalsInitAttribute (class [sealed]) : System.Attribute

- `public SkipLocalsInitAttribute()`

## System.Text.Json

### JsonSerializer (class [static])

- `public static System.Boolean IsReflectionEnabledByDefault { get; }`
- `public static System.Object Deserialize(System.IO.Stream utf8Json, System.Text.Json.Serialization.Metadata.JsonTypeInfo jsonTypeInfo)`
- `public static System.Object Deserialize(System.ReadOnlySpan<System.Byte> utf8Json, System.Text.Json.Serialization.Metadata.JsonTypeInfo jsonTypeInfo)`
- `public static System.Object Deserialize(System.ReadOnlySpan<System.Char> json, System.Text.Json.Serialization.Metadata.JsonTypeInfo jsonTypeInfo)`
- `public static System.Object Deserialize(System.String json, System.Text.Json.Serialization.Metadata.JsonTypeInfo jsonTypeInfo)`
- `[ext] public static System.Object Deserialize(this System.Text.Json.JsonDocument document, System.Text.Json.Serialization.Metadata.JsonTypeInfo jsonTypeInfo)`
- `[ext] public static System.Object Deserialize(this System.Text.Json.JsonElement element, System.Text.Json.Serialization.Metadata.JsonTypeInfo jsonTypeInfo)`
- `[ext] public static System.Object Deserialize(this System.Text.Json.Nodes.JsonNode node, System.Text.Json.Serialization.Metadata.JsonTypeInfo jsonTypeInfo)`
- `public static System.Object Deserialize(ref System.Text.Json.Utf8JsonReader& reader, System.Text.Json.Serialization.Metadata.JsonTypeInfo jsonTypeInfo)`
- `public static TValue Deserialize<TValue>(System.IO.Stream utf8Json, System.Text.Json.JsonSerializerOptions options)`
- `public static TValue Deserialize<TValue>(System.IO.Stream utf8Json, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo)`
- `public static TValue Deserialize<TValue>(System.ReadOnlySpan<System.Byte> utf8Json, System.Text.Json.JsonSerializerOptions options)`
- `public static TValue Deserialize<TValue>(System.ReadOnlySpan<System.Byte> utf8Json, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo)`
- `public static TValue Deserialize<TValue>(System.ReadOnlySpan<System.Char> json, System.Text.Json.JsonSerializerOptions options)`
- `public static TValue Deserialize<TValue>(System.ReadOnlySpan<System.Char> json, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo)`
- `public static TValue Deserialize<TValue>(System.String json, System.Text.Json.JsonSerializerOptions options)`
- `public static TValue Deserialize<TValue>(System.String json, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo)`
- `[ext] public static TValue Deserialize<TValue>(this System.Text.Json.JsonDocument document, System.Text.Json.JsonSerializerOptions options)`
- `[ext] public static TValue Deserialize<TValue>(this System.Text.Json.JsonDocument document, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo)`
- `[ext] public static TValue Deserialize<TValue>(this System.Text.Json.JsonElement element, System.Text.Json.JsonSerializerOptions options)`
- `[ext] public static TValue Deserialize<TValue>(this System.Text.Json.JsonElement element, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo)`
- `[ext] public static TValue Deserialize<TValue>(this System.Text.Json.Nodes.JsonNode node, System.Text.Json.JsonSerializerOptions options)`
- `[ext] public static TValue Deserialize<TValue>(this System.Text.Json.Nodes.JsonNode node, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo)`
- `public static TValue Deserialize<TValue>(ref System.Text.Json.Utf8JsonReader& reader, System.Text.Json.JsonSerializerOptions options)`
- `public static TValue Deserialize<TValue>(ref System.Text.Json.Utf8JsonReader& reader, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo)`
- `public static System.Object Deserialize(System.IO.Stream utf8Json, System.Type returnType, System.Text.Json.JsonSerializerOptions options)`
- `public static System.Object Deserialize(System.IO.Stream utf8Json, System.Type returnType, System.Text.Json.Serialization.JsonSerializerContext context)`
- `public static System.Object Deserialize(System.ReadOnlySpan<System.Byte> utf8Json, System.Type returnType, System.Text.Json.JsonSerializerOptions options)`
- `public static System.Object Deserialize(System.ReadOnlySpan<System.Byte> utf8Json, System.Type returnType, System.Text.Json.Serialization.JsonSerializerContext context)`
- `public static System.Object Deserialize(System.ReadOnlySpan<System.Char> json, System.Type returnType, System.Text.Json.JsonSerializerOptions options)`
- `public static System.Object Deserialize(System.ReadOnlySpan<System.Char> json, System.Type returnType, System.Text.Json.Serialization.JsonSerializerContext context)`
- `public static System.Object Deserialize(System.String json, System.Type returnType, System.Text.Json.JsonSerializerOptions options)`
- `public static System.Object Deserialize(System.String json, System.Type returnType, System.Text.Json.Serialization.JsonSerializerContext context)`
- `[ext] public static System.Object Deserialize(this System.Text.Json.JsonDocument document, System.Type returnType, System.Text.Json.JsonSerializerOptions options)`
- `[ext] public static System.Object Deserialize(this System.Text.Json.JsonDocument document, System.Type returnType, System.Text.Json.Serialization.JsonSerializerContext context)`
- `[ext] public static System.Object Deserialize(this System.Text.Json.JsonElement element, System.Type returnType, System.Text.Json.JsonSerializerOptions options)`
- `[ext] public static System.Object Deserialize(this System.Text.Json.JsonElement element, System.Type returnType, System.Text.Json.Serialization.JsonSerializerContext context)`
- `[ext] public static System.Object Deserialize(this System.Text.Json.Nodes.JsonNode node, System.Type returnType, System.Text.Json.JsonSerializerOptions options)`
- `[ext] public static System.Object Deserialize(this System.Text.Json.Nodes.JsonNode node, System.Type returnType, System.Text.Json.Serialization.JsonSerializerContext context)`
- `public static System.Object Deserialize(ref System.Text.Json.Utf8JsonReader& reader, System.Type returnType, System.Text.Json.JsonSerializerOptions options)`
- `public static System.Object Deserialize(ref System.Text.Json.Utf8JsonReader& reader, System.Type returnType, System.Text.Json.Serialization.JsonSerializerContext context)`
- `public static System.Threading.Tasks.ValueTask<System.Object> DeserializeAsync(System.IO.Pipelines.PipeReader utf8Json, System.Text.Json.Serialization.Metadata.JsonTypeInfo jsonTypeInfo, System.Threading.CancellationToken cancellationToken)`
- `public static System.Threading.Tasks.ValueTask<System.Object> DeserializeAsync(System.IO.Stream utf8Json, System.Text.Json.Serialization.Metadata.JsonTypeInfo jsonTypeInfo, System.Threading.CancellationToken cancellationToken)`
- `public static System.Threading.Tasks.ValueTask<TValue> DeserializeAsync<TValue>(System.IO.Pipelines.PipeReader utf8Json, System.Text.Json.JsonSerializerOptions options, System.Threading.CancellationToken cancellationToken)`
- `public static System.Threading.Tasks.ValueTask<TValue> DeserializeAsync<TValue>(System.IO.Pipelines.PipeReader utf8Json, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo, System.Threading.CancellationToken cancellationToken)`
- `public static System.Threading.Tasks.ValueTask<TValue> DeserializeAsync<TValue>(System.IO.Stream utf8Json, System.Text.Json.JsonSerializerOptions options, System.Threading.CancellationToken cancellationToken)`
- `public static System.Threading.Tasks.ValueTask<TValue> DeserializeAsync<TValue>(System.IO.Stream utf8Json, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo, System.Threading.CancellationToken cancellationToken)`
- `public static System.Threading.Tasks.ValueTask<System.Object> DeserializeAsync(System.IO.Pipelines.PipeReader utf8Json, System.Type returnType, System.Text.Json.JsonSerializerOptions options, System.Threading.CancellationToken cancellationToken)`
- `public static System.Threading.Tasks.ValueTask<System.Object> DeserializeAsync(System.IO.Pipelines.PipeReader utf8Json, System.Type returnType, System.Text.Json.Serialization.JsonSerializerContext context, System.Threading.CancellationToken cancellationToken)`
- `public static System.Threading.Tasks.ValueTask<System.Object> DeserializeAsync(System.IO.Stream utf8Json, System.Type returnType, System.Text.Json.JsonSerializerOptions options, System.Threading.CancellationToken cancellationToken)`
- `public static System.Threading.Tasks.ValueTask<System.Object> DeserializeAsync(System.IO.Stream utf8Json, System.Type returnType, System.Text.Json.Serialization.JsonSerializerContext context, System.Threading.CancellationToken cancellationToken)`
- `public static System.Collections.Generic.IAsyncEnumerable<TValue> DeserializeAsyncEnumerable<TValue>(System.IO.Pipelines.PipeReader utf8Json, System.Text.Json.JsonSerializerOptions options, System.Threading.CancellationToken cancellationToken)`
- `public static System.Collections.Generic.IAsyncEnumerable<TValue> DeserializeAsyncEnumerable<TValue>(System.IO.Pipelines.PipeReader utf8Json, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo, System.Threading.CancellationToken cancellationToken)`
- `public static System.Collections.Generic.IAsyncEnumerable<TValue> DeserializeAsyncEnumerable<TValue>(System.IO.Stream utf8Json, System.Text.Json.JsonSerializerOptions options, System.Threading.CancellationToken cancellationToken)`
- `public static System.Collections.Generic.IAsyncEnumerable<TValue> DeserializeAsyncEnumerable<TValue>(System.IO.Stream utf8Json, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo, System.Threading.CancellationToken cancellationToken)`
- `public static System.Collections.Generic.IAsyncEnumerable<TValue> DeserializeAsyncEnumerable<TValue>(System.IO.Pipelines.PipeReader utf8Json, System.Boolean topLevelValues, System.Text.Json.JsonSerializerOptions options, System.Threading.CancellationToken cancellationToken)`
- `public static System.Collections.Generic.IAsyncEnumerable<TValue> DeserializeAsyncEnumerable<TValue>(System.IO.Pipelines.PipeReader utf8Json, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo, System.Boolean topLevelValues, System.Threading.CancellationToken cancellationToken)`
- `public static System.Collections.Generic.IAsyncEnumerable<TValue> DeserializeAsyncEnumerable<TValue>(System.IO.Stream utf8Json, System.Boolean topLevelValues, System.Text.Json.JsonSerializerOptions options, System.Threading.CancellationToken cancellationToken)`
- `public static System.Collections.Generic.IAsyncEnumerable<TValue> DeserializeAsyncEnumerable<TValue>(System.IO.Stream utf8Json, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo, System.Boolean topLevelValues, System.Threading.CancellationToken cancellationToken)`
- `public static System.String Serialize(System.Object value, System.Text.Json.Serialization.Metadata.JsonTypeInfo jsonTypeInfo)`
- `public static System.String Serialize<TValue>(TValue value, System.Text.Json.JsonSerializerOptions options)`
- `public static System.String Serialize<TValue>(TValue value, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo)`
- `public static System.Void Serialize(System.IO.Stream utf8Json, System.Object value, System.Text.Json.Serialization.Metadata.JsonTypeInfo jsonTypeInfo)`
- `public static System.String Serialize(System.Object value, System.Type inputType, System.Text.Json.JsonSerializerOptions options)`
- `public static System.String Serialize(System.Object value, System.Type inputType, System.Text.Json.Serialization.JsonSerializerContext context)`
- `public static System.Void Serialize(System.Text.Json.Utf8JsonWriter writer, System.Object value, System.Text.Json.Serialization.Metadata.JsonTypeInfo jsonTypeInfo)`
- `public static System.Void Serialize<TValue>(System.IO.Stream utf8Json, TValue value, System.Text.Json.JsonSerializerOptions options)`
- `public static System.Void Serialize<TValue>(System.IO.Stream utf8Json, TValue value, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo)`
- `public static System.Void Serialize<TValue>(System.Text.Json.Utf8JsonWriter writer, TValue value, System.Text.Json.JsonSerializerOptions options)`
- `public static System.Void Serialize<TValue>(System.Text.Json.Utf8JsonWriter writer, TValue value, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo)`
- `public static System.Void Serialize(System.IO.Stream utf8Json, System.Object value, System.Type inputType, System.Text.Json.JsonSerializerOptions options)`
- `public static System.Void Serialize(System.IO.Stream utf8Json, System.Object value, System.Type inputType, System.Text.Json.Serialization.JsonSerializerContext context)`
- `public static System.Void Serialize(System.Text.Json.Utf8JsonWriter writer, System.Object value, System.Type inputType, System.Text.Json.JsonSerializerOptions options)`
- `public static System.Void Serialize(System.Text.Json.Utf8JsonWriter writer, System.Object value, System.Type inputType, System.Text.Json.Serialization.JsonSerializerContext context)`
- `public static System.Threading.Tasks.Task SerializeAsync(System.IO.Stream utf8Json, System.Object value, System.Text.Json.Serialization.Metadata.JsonTypeInfo jsonTypeInfo, System.Threading.CancellationToken cancellationToken)`
- `public static System.Threading.Tasks.Task SerializeAsync<TValue>(System.IO.Pipelines.PipeWriter utf8Json, TValue value, System.Text.Json.JsonSerializerOptions options, System.Threading.CancellationToken cancellationToken)`
- `public static System.Threading.Tasks.Task SerializeAsync<TValue>(System.IO.Stream utf8Json, TValue value, System.Text.Json.JsonSerializerOptions options, System.Threading.CancellationToken cancellationToken)`
- `public static System.Threading.Tasks.Task SerializeAsync<TValue>(System.IO.Stream utf8Json, TValue value, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo, System.Threading.CancellationToken cancellationToken)`
- `public static System.Threading.Tasks.Task SerializeAsync<TValue>(System.IO.Pipelines.PipeWriter utf8Json, TValue value, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo, System.Threading.CancellationToken cancellationToken)`
- `public static System.Threading.Tasks.Task SerializeAsync(System.IO.Pipelines.PipeWriter utf8Json, System.Object value, System.Text.Json.Serialization.Metadata.JsonTypeInfo jsonTypeInfo, System.Threading.CancellationToken cancellationToken)`
- `public static System.Threading.Tasks.Task SerializeAsync(System.IO.Stream utf8Json, System.Object value, System.Type inputType, System.Text.Json.JsonSerializerOptions options, System.Threading.CancellationToken cancellationToken)`
- `public static System.Threading.Tasks.Task SerializeAsync(System.IO.Stream utf8Json, System.Object value, System.Type inputType, System.Text.Json.Serialization.JsonSerializerContext context, System.Threading.CancellationToken cancellationToken)`
- `public static System.Threading.Tasks.Task SerializeAsync(System.IO.Pipelines.PipeWriter utf8Json, System.Object value, System.Type inputType, System.Text.Json.JsonSerializerOptions options, System.Threading.CancellationToken cancellationToken)`
- `public static System.Threading.Tasks.Task SerializeAsync(System.IO.Pipelines.PipeWriter utf8Json, System.Object value, System.Type inputType, System.Text.Json.Serialization.JsonSerializerContext context, System.Threading.CancellationToken cancellationToken)`
- `public static System.Text.Json.JsonDocument SerializeToDocument(System.Object value, System.Text.Json.Serialization.Metadata.JsonTypeInfo jsonTypeInfo)`
- `public static System.Text.Json.JsonDocument SerializeToDocument<TValue>(TValue value, System.Text.Json.JsonSerializerOptions options)`
- `public static System.Text.Json.JsonDocument SerializeToDocument<TValue>(TValue value, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo)`
- `public static System.Text.Json.JsonDocument SerializeToDocument(System.Object value, System.Type inputType, System.Text.Json.JsonSerializerOptions options)`
- `public static System.Text.Json.JsonDocument SerializeToDocument(System.Object value, System.Type inputType, System.Text.Json.Serialization.JsonSerializerContext context)`
- `public static System.Text.Json.JsonElement SerializeToElement(System.Object value, System.Text.Json.Serialization.Metadata.JsonTypeInfo jsonTypeInfo)`
- `public static System.Text.Json.JsonElement SerializeToElement<TValue>(TValue value, System.Text.Json.JsonSerializerOptions options)`
- `public static System.Text.Json.JsonElement SerializeToElement<TValue>(TValue value, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo)`
- `public static System.Text.Json.JsonElement SerializeToElement(System.Object value, System.Type inputType, System.Text.Json.JsonSerializerOptions options)`
- `public static System.Text.Json.JsonElement SerializeToElement(System.Object value, System.Type inputType, System.Text.Json.Serialization.JsonSerializerContext context)`
- `public static System.Text.Json.Nodes.JsonNode SerializeToNode(System.Object value, System.Text.Json.Serialization.Metadata.JsonTypeInfo jsonTypeInfo)`
- `public static System.Text.Json.Nodes.JsonNode SerializeToNode<TValue>(TValue value, System.Text.Json.JsonSerializerOptions options)`
- `public static System.Text.Json.Nodes.JsonNode SerializeToNode<TValue>(TValue value, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo)`
- `public static System.Text.Json.Nodes.JsonNode SerializeToNode(System.Object value, System.Type inputType, System.Text.Json.JsonSerializerOptions options)`
- `public static System.Text.Json.Nodes.JsonNode SerializeToNode(System.Object value, System.Type inputType, System.Text.Json.Serialization.JsonSerializerContext context)`
- `public static System.Byte[] SerializeToUtf8Bytes(System.Object value, System.Text.Json.Serialization.Metadata.JsonTypeInfo jsonTypeInfo)`
- `public static System.Byte[] SerializeToUtf8Bytes<TValue>(TValue value, System.Text.Json.JsonSerializerOptions options)`
- `public static System.Byte[] SerializeToUtf8Bytes<TValue>(TValue value, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo)`
- `public static System.Byte[] SerializeToUtf8Bytes(System.Object value, System.Type inputType, System.Text.Json.JsonSerializerOptions options)`
- `public static System.Byte[] SerializeToUtf8Bytes(System.Object value, System.Type inputType, System.Text.Json.Serialization.JsonSerializerContext context)`

### JsonSerializerDefaults (enum) : System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable

- `General = 0`
- `Web = 1`
- `Strict = 2`

### JsonSerializerOptions (class [sealed])

- `public JsonSerializerOptions()`
- `public JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults defaults)`
- `public JsonSerializerOptions(System.Text.Json.JsonSerializerOptions options)`
- `public System.Boolean AllowDuplicateProperties { get; set; }`
- `public System.Boolean AllowOutOfOrderMetadataProperties { get; set; }`
- `public System.Boolean AllowTrailingCommas { get; set; }`
- `public System.Collections.Generic.IList<System.Text.Json.Serialization.JsonConverter> Converters { get; }`
- `public static System.Text.Json.JsonSerializerOptions Default { get; }`
- `public System.Int32 DefaultBufferSize { get; set; }`
- `public System.Text.Json.Serialization.JsonIgnoreCondition DefaultIgnoreCondition { get; set; }`
- `public System.Text.Json.JsonNamingPolicy DictionaryKeyPolicy { get; set; }`
- `public System.Text.Encodings.Web.JavaScriptEncoder Encoder { get; set; }`
- `public System.Boolean IgnoreNullValues { get; set; }`
- `public System.Boolean IgnoreReadOnlyFields { get; set; }`
- `public System.Boolean IgnoreReadOnlyProperties { get; set; }`
- `public System.Boolean IncludeFields { get; set; }`
- `public System.Char IndentCharacter { get; set; }`
- `public System.Int32 IndentSize { get; set; }`
- `public System.Boolean IsReadOnly { get; }`
- `public System.Int32 MaxDepth { get; set; }`
- `public System.String NewLine { get; set; }`
- `public System.Text.Json.Serialization.JsonNumberHandling NumberHandling { get; set; }`
- `public System.Text.Json.Serialization.JsonObjectCreationHandling PreferredObjectCreationHandling { get; set; }`
- `public System.Boolean PropertyNameCaseInsensitive { get; set; }`
- `public System.Text.Json.JsonNamingPolicy PropertyNamingPolicy { get; set; }`
- `public System.Text.Json.JsonCommentHandling ReadCommentHandling { get; set; }`
- `public System.Text.Json.Serialization.ReferenceHandler ReferenceHandler { get; set; }`
- `public System.Boolean RespectNullableAnnotations { get; set; }`
- `public System.Boolean RespectRequiredConstructorParameters { get; set; }`
- `public static System.Text.Json.JsonSerializerOptions Strict { get; }`
- `public System.Text.Json.Serialization.Metadata.IJsonTypeInfoResolver TypeInfoResolver { get; set; }`
- `public System.Collections.Generic.IList<System.Text.Json.Serialization.Metadata.IJsonTypeInfoResolver> TypeInfoResolverChain { get; }`
- `public System.Text.Json.Serialization.JsonUnknownTypeHandling UnknownTypeHandling { get; set; }`
- `public System.Text.Json.Serialization.JsonUnmappedMemberHandling UnmappedMemberHandling { get; set; }`
- `public static System.Text.Json.JsonSerializerOptions Web { get; }`
- `public System.Boolean WriteIndented { get; set; }`
- `public System.Void AddContext<TContext>()`
- `where TContext : System.Text.Json.Serialization.JsonSerializerContext, new()`
- `public System.Text.Json.Serialization.JsonConverter GetConverter(System.Type typeToConvert)`
- `public System.Text.Json.Serialization.Metadata.JsonTypeInfo GetTypeInfo(System.Type type)`
- `public System.Void MakeReadOnly()`
- `public System.Void MakeReadOnly(System.Boolean populateMissingResolver)`
- `public System.Boolean TryGetTypeInfo(System.Type type, out System.Text.Json.Serialization.Metadata.JsonTypeInfo& typeInfo)`

## System.Threading

### AsyncLocal`1<T> (class [sealed])

- `public AsyncLocal`1()`
- `public AsyncLocal`1(System.Action<System.Threading.AsyncLocalValueChangedArgs<T>> valueChangedHandler)`
- `public T Value { get; set; }`

## System.Threading.Tasks

### ConfigureAwaitOptions (enum) : System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable

- `None = 0`
- `ContinueOnCapturedContext = 1`
- `SuppressThrowing = 2`
- `ForceYielding = 4`

