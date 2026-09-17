# Public API inventory: bcl-supplement-f2f3f11 (DataAnnotations accumulation, exception infra, unsafe/span primitives)

Assemblies: System.ComponentModel.Annotations 10.0.0.0, System.Runtime 10.0.0.0, System.Linq 10.0.0.0

Type count: 10

## System.Collections.Generic

### IReadOnlySet`1<T> (interface) : System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable, System.Collections.Generic.IReadOnlyCollection<T>

- `public System.Boolean Contains(T item)`
- `public System.Boolean IsProperSubsetOf(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean IsProperSupersetOf(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean IsSubsetOf(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean IsSupersetOf(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean Overlaps(System.Collections.Generic.IEnumerable<T> other)`
- `public System.Boolean SetEquals(System.Collections.Generic.IEnumerable<T> other)`

## System.ComponentModel.DataAnnotations

### IValidatableObject (interface)

- `public System.Collections.Generic.IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> Validate(System.ComponentModel.DataAnnotations.ValidationContext validationContext)`

### ValidationAttribute (class [abstract]) : System.Attribute

- `public System.String ErrorMessage { get; set; }`
- `public System.String ErrorMessageResourceName { get; set; }`
- `public System.Type ErrorMessageResourceType { get; set; }`
- `public System.Boolean RequiresValidationContext { get; }`
- `public System.String FormatErrorMessage(System.String name)`
- `public System.ComponentModel.DataAnnotations.ValidationResult GetValidationResult(System.Object value, System.ComponentModel.DataAnnotations.ValidationContext validationContext)`
- `public System.Boolean IsValid(System.Object value)`
- `public System.Void Validate(System.Object value, System.ComponentModel.DataAnnotations.ValidationContext validationContext)`
- `public System.Void Validate(System.Object value, System.String name)`

### ValidationContext (class [sealed]) : System.IServiceProvider

- `public ValidationContext(System.Object instance)`
- `public ValidationContext(System.Object instance, System.Collections.Generic.IDictionary<System.Object, System.Object> items)`
- `public ValidationContext(System.Object instance, System.IServiceProvider serviceProvider, System.Collections.Generic.IDictionary<System.Object, System.Object> items)`
- `public ValidationContext(System.Object instance, System.String displayName, System.IServiceProvider serviceProvider, System.Collections.Generic.IDictionary<System.Object, System.Object> items)`
- `public System.String DisplayName { get; set; }`
- `public System.Collections.Generic.IDictionary<System.Object, System.Object> Items { get; }`
- `public System.String MemberName { get; set; }`
- `public System.Object ObjectInstance { get; }`
- `public System.Type ObjectType { get; }`
- `public System.Object GetService(System.Type serviceType)`
- `public System.Void InitializeServiceProvider(System.Func<System.Type, System.Object> serviceProvider)`

### ValidationException (class) : System.Exception, System.Runtime.Serialization.ISerializable

- `public ValidationException()`
- `public ValidationException(System.String message)`
- `public ValidationException(System.String message, System.Exception innerException)`
- `public ValidationException(System.ComponentModel.DataAnnotations.ValidationResult validationResult, System.ComponentModel.DataAnnotations.ValidationAttribute validatingAttribute, System.Object value)`
- `public ValidationException(System.String errorMessage, System.ComponentModel.DataAnnotations.ValidationAttribute validatingAttribute, System.Object value)`
- `public System.ComponentModel.DataAnnotations.ValidationAttribute ValidationAttribute { get; }`
- `public System.ComponentModel.DataAnnotations.ValidationResult ValidationResult { get; }`
- `public System.Object Value { get; }`

### ValidationResult (class)

- `public ValidationResult(System.String errorMessage)`
- `public ValidationResult(System.String errorMessage, System.Collections.Generic.IEnumerable<System.String> memberNames)`
- `public static System.ComponentModel.DataAnnotations.ValidationResult Success`
- `public System.String ErrorMessage { get; set; }`
- `public System.Collections.Generic.IEnumerable<System.String> MemberNames { get; }`
- `public System.String ToString()`

### Validator (class [static])

- `public static System.Boolean TryValidateObject(System.Object instance, System.ComponentModel.DataAnnotations.ValidationContext validationContext, System.Collections.Generic.ICollection<System.ComponentModel.DataAnnotations.ValidationResult> validationResults)`
- `public static System.Boolean TryValidateObject(System.Object instance, System.ComponentModel.DataAnnotations.ValidationContext validationContext, System.Collections.Generic.ICollection<System.ComponentModel.DataAnnotations.ValidationResult> validationResults, System.Boolean validateAllProperties)`
- `public static System.Boolean TryValidateProperty(System.Object value, System.ComponentModel.DataAnnotations.ValidationContext validationContext, System.Collections.Generic.ICollection<System.ComponentModel.DataAnnotations.ValidationResult> validationResults)`
- `public static System.Boolean TryValidateValue(System.Object value, System.ComponentModel.DataAnnotations.ValidationContext validationContext, System.Collections.Generic.ICollection<System.ComponentModel.DataAnnotations.ValidationResult> validationResults, System.Collections.Generic.IEnumerable<System.ComponentModel.DataAnnotations.ValidationAttribute> validationAttributes)`
- `public static System.Void ValidateObject(System.Object instance, System.ComponentModel.DataAnnotations.ValidationContext validationContext)`
- `public static System.Void ValidateObject(System.Object instance, System.ComponentModel.DataAnnotations.ValidationContext validationContext, System.Boolean validateAllProperties)`
- `public static System.Void ValidateProperty(System.Object value, System.ComponentModel.DataAnnotations.ValidationContext validationContext)`
- `public static System.Void ValidateValue(System.Object value, System.ComponentModel.DataAnnotations.ValidationContext validationContext, System.Collections.Generic.IEnumerable<System.ComponentModel.DataAnnotations.ValidationAttribute> validationAttributes)`

## System.Runtime.CompilerServices

### ConfiguredCancelableAsyncEnumerable`1<T> (struct [readonly struct])

- `public System.Runtime.CompilerServices.ConfiguredCancelableAsyncEnumerable<T> ConfigureAwait(System.Boolean continueOnCapturedContext)`
- `public System.Runtime.CompilerServices.ConfiguredCancelableAsyncEnumerable<T> GetAsyncEnumerator()`
- `public System.Runtime.CompilerServices.ConfiguredCancelableAsyncEnumerable<T> WithCancellation(System.Threading.CancellationToken cancellationToken)`

### Unsafe (class [static])

- `public static System.Void* Add<T>(System.Void* source, System.Int32 elementOffset)`
- `public static T& Add<T>(ref T& source, System.Int32 elementOffset)`
- `public static T& Add<T>(ref T& source, System.IntPtr elementOffset)`
- `public static T& Add<T>(ref T& source, System.UIntPtr elementOffset)`
- `public static T& AddByteOffset<T>(ref T& source, System.IntPtr byteOffset)`
- `public static T& AddByteOffset<T>(ref T& source, System.UIntPtr byteOffset)`
- `public static System.Boolean AreSame<T>(in T& left, in T& right)`
- `public static T As<T>(System.Object o)`
- `where T : class`
- `public static TTo& As<TFrom, TTo>(ref TFrom& source)`
- `public static System.Void* AsPointer<T>(in T& value)`
- `public static T& AsRef<T>(System.Void* source)`
- `public static T& AsRef<T>(in T& source)`
- `public static TTo BitCast<TFrom, TTo>(TFrom source)`
- `public static System.IntPtr ByteOffset<T>(in T& origin, in T& target)`
- `public static System.Void Copy<T>(System.Void* destination, in T& source)`
- `public static System.Void Copy<T>(ref T& destination, System.Void* source)`
- `public static System.Void CopyBlock(ref System.Byte& destination, in System.Byte& source, System.UInt32 byteCount)`
- `public static System.Void CopyBlock(System.Void* destination, System.Void* source, System.UInt32 byteCount)`
- `public static System.Void CopyBlockUnaligned(ref System.Byte& destination, in System.Byte& source, System.UInt32 byteCount)`
- `public static System.Void CopyBlockUnaligned(System.Void* destination, System.Void* source, System.UInt32 byteCount)`
- `public static System.Void InitBlock(ref System.Byte& startAddress, System.Byte value, System.UInt32 byteCount)`
- `public static System.Void InitBlock(System.Void* startAddress, System.Byte value, System.UInt32 byteCount)`
- `public static System.Void InitBlockUnaligned(ref System.Byte& startAddress, System.Byte value, System.UInt32 byteCount)`
- `public static System.Void InitBlockUnaligned(System.Void* startAddress, System.Byte value, System.UInt32 byteCount)`
- `public static System.Boolean IsAddressGreaterThan<T>(in T& left, in T& right)`
- `public static System.Boolean IsAddressGreaterThanOrEqualTo<T>(in T& left, in T& right)`
- `public static System.Boolean IsAddressLessThan<T>(in T& left, in T& right)`
- `public static System.Boolean IsAddressLessThanOrEqualTo<T>(in T& left, in T& right)`
- `public static System.Boolean IsNullRef<T>(in T& source)`
- `public static T& NullRef<T>()`
- `public static T Read<T>(System.Void* source)`
- `public static T ReadUnaligned<T>(in System.Byte& source)`
- `public static T ReadUnaligned<T>(System.Void* source)`
- `public static System.Int32 SizeOf<T>()`
- `public static System.Void SkipInit<T>(out T& value)`
- `public static System.Void* Subtract<T>(System.Void* source, System.Int32 elementOffset)`
- `public static T& Subtract<T>(ref T& source, System.Int32 elementOffset)`
- `public static T& Subtract<T>(ref T& source, System.IntPtr elementOffset)`
- `public static T& Subtract<T>(ref T& source, System.UIntPtr elementOffset)`
- `public static T& SubtractByteOffset<T>(ref T& source, System.IntPtr byteOffset)`
- `public static T& SubtractByteOffset<T>(ref T& source, System.UIntPtr byteOffset)`
- `public static T& Unbox<T>(System.Object box)`
- `where T : struct`
- `public static System.Void Write<T>(System.Void* destination, T value)`
- `public static System.Void WriteUnaligned<T>(ref System.Byte& destination, T value)`
- `public static System.Void WriteUnaligned<T>(System.Void* destination, T value)`

## System.Runtime.ExceptionServices

### ExceptionDispatchInfo (class [sealed])

- `public System.Exception SourceException { get; }`
- `public static System.Runtime.ExceptionServices.ExceptionDispatchInfo Capture(System.Exception source)`
- `public static System.Exception SetCurrentStackTrace(System.Exception source)`
- `public static System.Exception SetRemoteStackTrace(System.Exception source, System.String stackTrace)`
- `public System.Void Throw()`
- `public static System.Void Throw(System.Exception source)`

