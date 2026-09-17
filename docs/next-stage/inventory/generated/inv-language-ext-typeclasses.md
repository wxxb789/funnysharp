# Public API inventory: LanguageExt.Core 4.4.9 TypeClasses/Common/Thunks/DataTypes

Assemblies: LanguageExt.Core 4.0.0.0

Type count: 85

## LanguageExt.Common

### BottomError (class [sealed]) : LanguageExt.Common.Exceptional, System.IEquatable<LanguageExt.Common.Error>, System.IEquatable<LanguageExt.Common.Exceptional>, System.IEquatable<LanguageExt.Common.BottomError>

- `public BottomError()`
- `public static LanguageExt.Common.Error Default`
- `public System.Int32 Code { get; }`
- `public System.Boolean IsExceptional { get; }`
- `public System.Boolean IsExpected { get; }`
- `public System.String Message { get; }`
- `public LanguageExt.Common.Error <Clone>$()`
- `public System.Boolean Equals(System.Object obj)`
- `public System.Boolean Equals(LanguageExt.Common.Exceptional other)`
- `public System.Boolean Equals(LanguageExt.Common.BottomError other)`
- `public System.Int32 GetHashCode()`
- `public System.Boolean Is<E>()`
- `where E : System.Exception`
- `public System.Boolean Is(LanguageExt.Common.Error error)`
- `public LanguageExt.Common.ErrorException ToErrorException()`
- `public System.Exception ToException()`
- `public System.String ToString()`
- `public static System.Boolean op_Equality(LanguageExt.Common.BottomError left, LanguageExt.Common.BottomError right)`
- `public static System.Boolean op_Inequality(LanguageExt.Common.BottomError left, LanguageExt.Common.BottomError right)`

### BottomException (class) : LanguageExt.Common.ExceptionalException, System.Runtime.Serialization.ISerializable, System.Collections.Generic.IEnumerable<LanguageExt.Common.ErrorException>, System.Collections.IEnumerable

- `public BottomException()`
- `public static LanguageExt.Common.BottomException Default`
- `public System.Int32 Code { get; }`
- `public LanguageExt.Option<LanguageExt.Common.ErrorException> Inner { get; }`
- `public System.String Message { get; }`
- `public LanguageExt.Common.ErrorException Append(LanguageExt.Common.ErrorException error)`
- `public LanguageExt.Common.Error ToError()`

### Error (class [abstract]) : System.IEquatable<LanguageExt.Common.Error>

- `public System.Int32 Code { get; }`
- `public System.Int32 Count { get; }`
- `public LanguageExt.Option<System.Exception> Exception { get; }`
- `public LanguageExt.Option<LanguageExt.Common.Error> Inner { get; }`
- `public System.Boolean IsEmpty { get; }`
- `public System.Boolean IsExceptional { get; }`
- `public System.Boolean IsExpected { get; }`
- `public System.String Message { get; }`
- `public LanguageExt.Common.Error <Clone>$()`
- `public LanguageExt.Common.Error Append(LanguageExt.Common.Error error)`
- `public System.Collections.Generic.IEnumerable<LanguageExt.Common.Error> AsEnumerable()`
- `public System.Boolean Equals(System.Object obj)`
- `public System.Boolean Equals(LanguageExt.Common.Error other)`
- `public static LanguageExt.Common.Error FromObject(System.Object value)`
- `public System.Int32 GetHashCode()`
- `public LanguageExt.Common.Error Head()`
- `public System.Boolean Is<E>()`
- `where E : System.Exception`
- `public System.Boolean Is(LanguageExt.Common.Error error)`
- `public static LanguageExt.Common.Error Many(params LanguageExt.Common.Error[] errors)`
- `public static LanguageExt.Common.Error Many(LanguageExt.Seq<LanguageExt.Common.Error> errors)`
- `public static LanguageExt.Common.Error New(System.Exception thisException)`
- `public static LanguageExt.Common.Error New(System.String message)`
- `public static LanguageExt.Common.Error New(System.String message, System.Exception thisException)`
- `public static LanguageExt.Common.Error New(System.Int32 code, System.String message)`
- `public static LanguageExt.Common.Error New(System.String message, LanguageExt.Common.Error inner)`
- `public static LanguageExt.Common.Error New(System.Int32 code, System.String message, LanguageExt.Common.Error inner)`
- `public LanguageExt.Common.Error Tail()`
- `public LanguageExt.Unit Throw()`
- `public LanguageExt.Common.ErrorException ToErrorException()`
- `public System.Exception ToException()`
- `public System.String ToString()`
- `public static LanguageExt.Common.Error op_Addition(LanguageExt.Common.Error lhs, LanguageExt.Common.Error rhs)`
- `public static System.Boolean op_Equality(LanguageExt.Common.Error left, LanguageExt.Common.Error right)`
- `public static LanguageExt.Common.Error op_Implicit(System.String e)`
- `public static LanguageExt.Common.Error op_Implicit(System.ValueTuple<System.Int32, System.String> e)`
- `public static LanguageExt.Common.Error op_Implicit(System.Exception e)`
- `public static System.Exception op_Implicit(LanguageExt.Common.Error e)`
- `public static System.Boolean op_Inequality(LanguageExt.Common.Error left, LanguageExt.Common.Error right)`

### ErrorException (class [abstract]) : System.Exception, System.Runtime.Serialization.ISerializable, System.Collections.Generic.IEnumerable<LanguageExt.Common.ErrorException>, System.Collections.IEnumerable

- `public System.Int32 Code { get; }`
- `public LanguageExt.Option<LanguageExt.Common.ErrorException> Inner { get; }`
- `public System.Boolean IsEmpty { get; }`
- `public System.Boolean IsExceptional { get; }`
- `public System.Boolean IsExpected { get; }`
- `public LanguageExt.Common.ErrorException Append(LanguageExt.Common.ErrorException error)`
- `public System.Collections.Generic.IEnumerator<LanguageExt.Common.ErrorException> GetEnumerator()`
- `public static LanguageExt.Common.ErrorException Many(params LanguageExt.Common.ErrorException[] errors)`
- `public static LanguageExt.Common.ErrorException Many(LanguageExt.Seq<LanguageExt.Common.ErrorException> errors)`
- `public static LanguageExt.Common.ErrorException New(System.Exception thisException)`
- `public static LanguageExt.Common.ErrorException New(System.String message)`
- `public static LanguageExt.Common.ErrorException New(System.String message, System.Exception thisException)`
- `public static LanguageExt.Common.ErrorException New(System.Int32 code, System.String message)`
- `public static LanguageExt.Common.ErrorException New(System.String message, LanguageExt.Common.ErrorException inner)`
- `public static LanguageExt.Common.ErrorException New(System.Int32 code, System.String message, LanguageExt.Common.ErrorException inner)`
- `public LanguageExt.Common.Error ToError()`
- `public System.String ToString()`
- `public static LanguageExt.Common.ErrorException op_Addition(LanguageExt.Common.ErrorException lhs, LanguageExt.Common.ErrorException rhs)`

### Errors (class [static])

- `public static LanguageExt.Common.Error Bottom`
- `public const System.Int32 BottomCode`
- `public const System.String BottomText`
- `public static LanguageExt.Common.Error Cancelled`
- `public const System.Int32 CancelledCode`
- `public const System.String CancelledText`
- `public static LanguageExt.Common.Error Closed`
- `public const System.Int32 ClosedCode`
- `public const System.String ClosedText`
- `public const System.Int32 ManyErrorsCode`
- `public static LanguageExt.Common.Error None`
- `public const System.Int32 ParseErrorCode`
- `public static LanguageExt.Common.Error SequenceEmpty`
- `public const System.Int32 SequenceEmptyCode`
- `public const System.String SequenceEmptyText`
- `public static LanguageExt.Common.Error TimedOut`
- `public const System.Int32 TimedOutCode`
- `public const System.String TimedOutText`
- `public static LanguageExt.Common.Error ParseError(System.String msg)`

### Exceptional (class) : LanguageExt.Common.Error, System.IEquatable<LanguageExt.Common.Error>, System.IEquatable<LanguageExt.Common.Exceptional>

- `public Exceptional(System.String Message, System.Int32 Code)`
- `public System.Int32 Code { get; }`
- `public LanguageExt.Option<LanguageExt.Common.Error> Inner { get; }`
- `public System.Boolean IsExceptional { get; }`
- `public System.Boolean IsExpected { get; }`
- `public System.String Message { get; }`
- `public LanguageExt.Common.Error <Clone>$()`
- `public System.Void Deconstruct(out System.String& Message, out System.Int32& Code)`
- `public System.Boolean Equals(System.Object obj)`
- `public System.Boolean Equals(LanguageExt.Common.Error other)`
- `public System.Boolean Equals(LanguageExt.Common.Exceptional other)`
- `public System.Int32 GetHashCode()`
- `public System.Boolean Is<E>()`
- `where E : System.Exception`
- `public System.Boolean Is(LanguageExt.Common.Error error)`
- `public LanguageExt.Common.ErrorException ToErrorException()`
- `public System.Exception ToException()`
- `public System.String ToString()`
- `public static System.Boolean op_Equality(LanguageExt.Common.Exceptional left, LanguageExt.Common.Exceptional right)`
- `public static System.Boolean op_Inequality(LanguageExt.Common.Exceptional left, LanguageExt.Common.Exceptional right)`

### ExceptionalException (class) : LanguageExt.Common.ErrorException, System.Runtime.Serialization.ISerializable, System.Collections.Generic.IEnumerable<LanguageExt.Common.ErrorException>, System.Collections.IEnumerable

- `public ExceptionalException(System.Exception Exception)`
- `public ExceptionalException(System.String Message, System.Int32 Code)`
- `public ExceptionalException(System.String Message, System.Exception Exception)`
- `public readonly System.Exception Exception`
- `public System.Int32 Code { get; }`
- `public LanguageExt.Option<LanguageExt.Common.ErrorException> Inner { get; }`
- `public System.Boolean IsExceptional { get; }`
- `public System.Boolean IsExpected { get; }`
- `public System.String Message { get; }`
- `public LanguageExt.Common.ErrorException Append(LanguageExt.Common.ErrorException error)`
- `public LanguageExt.Common.Error ToError()`

### Expected (class) : LanguageExt.Common.Error, System.IEquatable<LanguageExt.Common.Error>, System.IEquatable<LanguageExt.Common.Expected>

- `public Expected(System.String Message, System.Int32 Code, LanguageExt.Option<LanguageExt.Common.Error> Inner)`
- `public System.Int32 Code { get; }`
- `public LanguageExt.Option<LanguageExt.Common.Error> Inner { get; }`
- `public System.Boolean IsExceptional { get; }`
- `public System.Boolean IsExpected { get; }`
- `public System.String Message { get; }`
- `public LanguageExt.Common.Error <Clone>$()`
- `public System.Void Deconstruct(out System.String& Message, out System.Int32& Code, out LanguageExt.Option<LanguageExt.Common.Error>& Inner)`
- `public System.Boolean Equals(System.Object obj)`
- `public System.Boolean Equals(LanguageExt.Common.Error other)`
- `public System.Boolean Equals(LanguageExt.Common.Expected other)`
- `public System.Int32 GetHashCode()`
- `public System.Boolean Is<E>()`
- `where E : System.Exception`
- `public LanguageExt.Common.ErrorException ToErrorException()`
- `public System.String ToString()`
- `public static System.Boolean op_Equality(LanguageExt.Common.Expected left, LanguageExt.Common.Expected right)`
- `public static System.Boolean op_Inequality(LanguageExt.Common.Expected left, LanguageExt.Common.Expected right)`

### ExpectedException (class [sealed]) : LanguageExt.Common.ErrorException, System.Runtime.Serialization.ISerializable, System.Collections.Generic.IEnumerable<LanguageExt.Common.ErrorException>, System.Collections.IEnumerable

- `public ExpectedException(System.String message, System.Int32 code, LanguageExt.Option<LanguageExt.Common.ErrorException> inner)`
- `public System.Int32 Code { get; }`
- `public LanguageExt.Option<LanguageExt.Common.ErrorException> Inner { get; }`
- `public System.Boolean IsExceptional { get; }`
- `public System.Boolean IsExpected { get; }`
- `public System.String Message { get; }`
- `public LanguageExt.Common.ErrorException Append(LanguageExt.Common.ErrorException error)`
- `public LanguageExt.Common.Error ToError()`

### ManyErrors (class [sealed]) : LanguageExt.Common.Error, System.IEquatable<LanguageExt.Common.Error>, System.IEquatable<LanguageExt.Common.ManyErrors>

- `public ManyErrors(LanguageExt.Seq<LanguageExt.Common.Error> Errors)`
- `public System.Int32 Code { get; }`
- `public System.Int32 Count { get; }`
- `public LanguageExt.Seq<LanguageExt.Common.Error> Errors { get; init; }`
- `public System.Boolean IsEmpty { get; }`
- `public System.Boolean IsExceptional { get; }`
- `public System.Boolean IsExpected { get; }`
- `public System.String Message { get; }`
- `public LanguageExt.Common.Error <Clone>$()`
- `public System.Collections.Generic.IEnumerable<LanguageExt.Common.Error> AsEnumerable()`
- `public System.Void Deconstruct(out LanguageExt.Seq<LanguageExt.Common.Error>& Errors)`
- `public System.Boolean Equals(System.Object obj)`
- `public System.Boolean Equals(LanguageExt.Common.Error other)`
- `public System.Boolean Equals(LanguageExt.Common.ManyErrors other)`
- `public System.Int32 GetHashCode()`
- `public LanguageExt.Common.Error Head()`
- `public System.Boolean Is<E>()`
- `where E : System.Exception`
- `public System.Boolean Is(LanguageExt.Common.Error error)`
- `public LanguageExt.Common.Error Tail()`
- `public LanguageExt.Common.ErrorException ToErrorException()`
- `public System.Exception ToException()`
- `public System.String ToString()`
- `public static System.Boolean op_Equality(LanguageExt.Common.ManyErrors left, LanguageExt.Common.ManyErrors right)`
- `public static System.Boolean op_Inequality(LanguageExt.Common.ManyErrors left, LanguageExt.Common.ManyErrors right)`

### ManyExceptions (class [sealed]) : LanguageExt.Common.ErrorException, System.Runtime.Serialization.ISerializable, System.Collections.Generic.IEnumerable<LanguageExt.Common.ErrorException>, System.Collections.IEnumerable

- `public ManyExceptions(LanguageExt.Seq<LanguageExt.Common.ErrorException> errors)`
- `public readonly LanguageExt.Seq<LanguageExt.Common.ErrorException> Errors`
- `public System.Int32 Code { get; }`
- `public LanguageExt.Option<LanguageExt.Common.ErrorException> Inner { get; }`
- `public System.Boolean IsEmpty { get; }`
- `public System.Boolean IsExceptional { get; }`
- `public System.Boolean IsExpected { get; }`
- `public System.String Message { get; }`
- `public LanguageExt.Common.ErrorException Append(LanguageExt.Common.ErrorException error)`
- `public System.Collections.Generic.IEnumerator<LanguageExt.Common.ErrorException> GetEnumerator()`
- `public LanguageExt.Common.Error ToError()`

### OptionalResult`1<A> (struct [readonly struct]) : System.IEquatable<LanguageExt.Common.OptionalResult<A>>, System.IComparable<LanguageExt.Common.OptionalResult<A>>

- `public OptionalResult`1(LanguageExt.Option<A> value)`
- `public OptionalResult`1(System.Exception e)`
- `public System.Boolean IsBottom { get; }`
- `public System.Boolean IsFaulted { get; }`
- `public System.Boolean IsFaultedOrNone { get; }`
- `public System.Boolean IsNone { get; }`
- `public System.Boolean IsSome { get; }`
- `public System.Int32 CompareTo(LanguageExt.Common.OptionalResult<A> other)`
- `public System.Boolean Equals(LanguageExt.Common.OptionalResult<A> other)`
- `public System.Boolean Equals(System.Object obj)`
- `public System.Int32 GetHashCode()`
- `public LanguageExt.Option<A> IfFail(System.Func<System.Exception, A> f)`
- `public LanguageExt.Unit IfFail(System.Action<System.Exception> f)`
- `public A IfFailOrNone(A defaultValue)`
- `public LanguageExt.Unit IfFailOrNone(System.Action f)`
- `public LanguageExt.Unit IfSucc(System.Action<A> f)`
- `public LanguageExt.Common.OptionalResult<B> Map<B>(System.Func<A, B> f)`
- `public System.Threading.Tasks.Task<LanguageExt.Common.OptionalResult<B>> MapAsync<B>(System.Func<A, System.Threading.Tasks.Task<B>> f)`
- `public R Match<R>(System.Func<A, R> Some, System.Func<R> None, System.Func<System.Exception, R> Fail)`
- `public static LanguageExt.Common.OptionalResult<A> Optional(A value)`
- `public static LanguageExt.Common.OptionalResult<A> Some(A value)`
- `public System.String ToString()`
- `public static System.Boolean op_Equality(LanguageExt.Common.OptionalResult<A> a, LanguageExt.Common.OptionalResult<A> b)`
- `public static System.Boolean op_GreaterThan(LanguageExt.Common.OptionalResult<A> a, LanguageExt.Common.OptionalResult<A> b)`
- `public static System.Boolean op_GreaterThanOrEqual(LanguageExt.Common.OptionalResult<A> a, LanguageExt.Common.OptionalResult<A> b)`
- `public static LanguageExt.Common.OptionalResult<A> op_Implicit(A value)`
- `public static LanguageExt.Common.OptionalResult<A> op_Implicit(LanguageExt.Option<A> value)`
- `public static LanguageExt.Common.OptionalResult<A> op_Implicit(LanguageExt.OptionNone value)`
- `public static System.Boolean op_Inequality(LanguageExt.Common.OptionalResult<A> a, LanguageExt.Common.OptionalResult<A> b)`
- `public static System.Boolean op_LessThan(LanguageExt.Common.OptionalResult<A> a, LanguageExt.Common.OptionalResult<A> b)`
- `public static System.Boolean op_LessThanOrEqual(LanguageExt.Common.OptionalResult<A> a, LanguageExt.Common.OptionalResult<A> b)`

### ResultState (enum) : System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable

- `Faulted = 0`
- `Success = 1`

### Result`1<A> (struct [readonly struct]) : System.IEquatable<LanguageExt.Common.Result<A>>, System.IComparable<LanguageExt.Common.Result<A>>

- `public Result`1(A value)`
- `public Result`1(System.Exception e)`
- `public static LanguageExt.Common.Result<A> Bottom`
- `public System.Boolean IsBottom { get; }`
- `public System.Boolean IsFaulted { get; }`
- `public System.Boolean IsSuccess { get; }`
- `public System.Int32 CompareTo(LanguageExt.Common.Result<A> other)`
- `public System.Boolean Equals(LanguageExt.Common.Result<A> other)`
- `public System.Boolean Equals(System.Object obj)`
- `public System.Int32 GetHashCode()`
- `public A IfFail(A defaultValue)`
- `public A IfFail(System.Func<System.Exception, A> f)`
- `public LanguageExt.Unit IfFail(System.Action<System.Exception> f)`
- `public LanguageExt.Unit IfSucc(System.Action<A> f)`
- `public LanguageExt.Common.Result<B> Map<B>(System.Func<A, B> f)`
- `public System.Threading.Tasks.Task<LanguageExt.Common.Result<B>> MapAsync<B>(System.Func<A, System.Threading.Tasks.Task<B>> f)`
- `public R Match<R>(System.Func<A, R> Succ, System.Func<System.Exception, R> Fail)`
- `public System.String ToString()`
- `public static System.Boolean op_Equality(LanguageExt.Common.Result<A> a, LanguageExt.Common.Result<A> b)`
- `public static System.Boolean op_GreaterThan(LanguageExt.Common.Result<A> a, LanguageExt.Common.Result<A> b)`
- `public static System.Boolean op_GreaterThanOrEqual(LanguageExt.Common.Result<A> a, LanguageExt.Common.Result<A> b)`
- `public static LanguageExt.Common.Result<A> op_Implicit(A value)`
- `public static System.Boolean op_Inequality(LanguageExt.Common.Result<A> a, LanguageExt.Common.Result<A> b)`
- `public static System.Boolean op_LessThan(LanguageExt.Common.Result<A> a, LanguageExt.Common.Result<A> b)`
- `public static System.Boolean op_LessThanOrEqual(LanguageExt.Common.Result<A> a, LanguageExt.Common.Result<A> b)`

## LanguageExt.DataTypes.Serialisation

### EitherData (class [static])

- `public static LanguageExt.DataTypes.Serialisation.EitherData<L, R> Bottom<L, R>()`
- `public static LanguageExt.DataTypes.Serialisation.EitherData<L, R> Left<L, R>(L leftValue)`
- `public static LanguageExt.DataTypes.Serialisation.EitherData<L, R> Right<L, R>(R rightValue)`

### EitherDataExtensions (class [static])

- `[ext] public static LanguageExt.Either<L, R> ToEither<L, R>(this LanguageExt.DataTypes.Serialisation.EitherData<L, R> input)`
- `[ext] public static LanguageExt.EitherAsync<L, R> ToEitherAsync<L, R>(this LanguageExt.DataTypes.Serialisation.EitherData<L, R> input)`
- `[ext] public static System.Threading.Tasks.Task<LanguageExt.EitherAsync<L, R>> ToEitherAsync<L, R>(this System.Threading.Tasks.Task<LanguageExt.DataTypes.Serialisation.EitherData<L, R>> input)`
- `[ext] public static System.Threading.Tasks.Task<LanguageExt.EitherAsync<L, R>> ToEitherAsync<L, R>(this System.Threading.Tasks.ValueTask<LanguageExt.DataTypes.Serialisation.EitherData<L, R>> input)`
- `[ext] public static LanguageExt.EitherUnsafe<L, R> ToEitherUnsafe<L, R>(this LanguageExt.DataTypes.Serialisation.EitherData<L, R> input)`
- `[ext] public static LanguageExt.Fin<A> ToFin<E, A>(this LanguageExt.DataTypes.Serialisation.EitherData<E, A> input)`
- `where E : LanguageExt.Common.Error`
- `[ext] public static LanguageExt.Try<A> ToTry<E, A>(this LanguageExt.DataTypes.Serialisation.EitherData<E, A> input)`
- `where E : System.Exception`
- `[ext] public static LanguageExt.TryAsync<A> ToTryAsync<E, A>(this LanguageExt.DataTypes.Serialisation.EitherData<E, A> input)`
- `where E : System.Exception`
- `[ext] public static System.Threading.Tasks.Task<LanguageExt.TryAsync<A>> ToTryAsync<E, A>(this System.Threading.Tasks.Task<LanguageExt.DataTypes.Serialisation.EitherData<E, A>> input)`
- `where E : System.Exception`
- `[ext] public static System.Threading.Tasks.Task<LanguageExt.TryAsync<A>> ToTryAsync<E, A>(this System.Threading.Tasks.ValueTask<LanguageExt.DataTypes.Serialisation.EitherData<E, A>> input)`
- `where E : System.Exception`

### EitherData`2<L, R> (class) : System.IEquatable<LanguageExt.DataTypes.Serialisation.EitherData<L, R>>

- `public EitherData`2(LanguageExt.EitherStatus state, R right, L left)`
- `public static LanguageExt.DataTypes.Serialisation.EitherData<L, R> Bottom`
- `public readonly L Left`
- `public readonly R Right`
- `public readonly LanguageExt.EitherStatus State`
- `public System.Boolean Equals(LanguageExt.DataTypes.Serialisation.EitherData<L, R> other)`
- `public System.Boolean Equals(System.Object obj)`
- `public System.Int32 GetHashCode()`
- `public System.String ToString()`
- `public static System.Boolean op_Equality(LanguageExt.DataTypes.Serialisation.EitherData<L, R> x, LanguageExt.DataTypes.Serialisation.EitherData<L, R> y)`
- `public static System.Boolean op_Inequality(LanguageExt.DataTypes.Serialisation.EitherData<L, R> x, LanguageExt.DataTypes.Serialisation.EitherData<L, R> y)`

### ValidationData`2<FAIL, SUCCESS> (class) : LanguageExt.Record<LanguageExt.DataTypes.Serialisation.ValidationData<FAIL, SUCCESS>>, System.IEquatable<LanguageExt.DataTypes.Serialisation.ValidationData<FAIL, SUCCESS>>, System.IComparable<LanguageExt.DataTypes.Serialisation.ValidationData<FAIL, SUCCESS>>, System.IComparable

- `public ValidationData`2(LanguageExt.Validation+StateType state, SUCCESS success, LanguageExt.Lst<FAIL> fail)`
- `public readonly LanguageExt.Lst<FAIL> Fail`
- `public readonly LanguageExt.Validation+StateType State`
- `public readonly SUCCESS Success`

### ValidationData`3<MonoidFail, FAIL, SUCCESS> (class) : LanguageExt.Record<LanguageExt.DataTypes.Serialisation.ValidationData<MonoidFail, FAIL, SUCCESS>>, System.IEquatable<LanguageExt.DataTypes.Serialisation.ValidationData<MonoidFail, FAIL, SUCCESS>>, System.IComparable<LanguageExt.DataTypes.Serialisation.ValidationData<MonoidFail, FAIL, SUCCESS>>, System.IComparable

- `where MonoidFail : struct, LanguageExt.TypeClasses.Monoid<FAIL>, LanguageExt.TypeClasses.Eq<FAIL>`

- `public ValidationData`3(LanguageExt.Validation+StateType state, SUCCESS success, FAIL fail)`
- `public readonly FAIL Fail`
- `public readonly LanguageExt.Validation+StateType State`
- `public readonly SUCCESS Success`

## LanguageExt.Thunks

### Thunk (class [static])

- `public const System.Int32 Evaluating`
- `public const System.Int32 HasEvaluated`
- `public const System.Int32 IsCancelled`
- `public const System.Int32 IsFailed`
- `public const System.Int32 IsSuccess`
- `public const System.Int32 NotEvaluated`
- `[ext] public static LanguageExt.Thunks.ThunkAsync<Env, A> Flatten<Env, A>(this LanguageExt.Thunks.ThunkAsync<Env, LanguageExt.Thunks.ThunkAsync<Env, A>> mma)`
- `where Env : struct, LanguageExt.Effects.Traits.HasCancel<Env>`
- `[ext] public static LanguageExt.Thunks.ThunkAsync<Env, A> Flatten<Env, A>(this LanguageExt.Thunks.ThunkAsync<LanguageExt.Thunks.ThunkAsync<Env, A>> mma)`
- `where Env : struct, LanguageExt.Effects.Traits.HasCancel<Env>`
- `[ext] public static LanguageExt.Thunks.ThunkAsync<Env, A> Flatten<Env, A>(this LanguageExt.Thunks.ThunkAsync<LanguageExt.Thunks.Thunk<Env, A>> mma)`
- `where Env : struct, LanguageExt.Effects.Traits.HasCancel<Env>`
- `[ext] public static LanguageExt.Thunks.ThunkAsync<Env, A> Flatten<Env, A>(this LanguageExt.Thunks.Thunk<LanguageExt.Thunks.ThunkAsync<Env, A>> mma)`
- `where Env : struct, LanguageExt.Effects.Traits.HasCancel<Env>`
- `[ext] public static LanguageExt.Thunks.ThunkAsync<Env, A> Flatten<Env, A>(this LanguageExt.Thunks.ThunkAsync<Env, LanguageExt.Thunks.ThunkAsync<A>> mma)`
- `where Env : struct, LanguageExt.Effects.Traits.HasCancel<Env>`
- `[ext] public static LanguageExt.Thunks.ThunkAsync<Env, A> Flatten<Env, A>(this LanguageExt.Thunks.ThunkAsync<Env, LanguageExt.Thunks.Thunk<Env, A>> mma)`
- `where Env : struct, LanguageExt.Effects.Traits.HasCancel<Env>`
- `[ext] public static LanguageExt.Thunks.ThunkAsync<Env, A> Flatten<Env, A>(this LanguageExt.Thunks.Thunk<Env, LanguageExt.Thunks.ThunkAsync<Env, A>> mma)`
- `where Env : struct, LanguageExt.Effects.Traits.HasCancel<Env>`
- `[ext] public static LanguageExt.Thunks.ThunkAsync<Env, A> Flatten<Env, A>(this LanguageExt.Thunks.ThunkAsync<Env, LanguageExt.Thunks.Thunk<A>> mma)`
- `where Env : struct, LanguageExt.Effects.Traits.HasCancel<Env>`
- `[ext] public static LanguageExt.Thunks.ThunkAsync<Env, A> Flatten<Env, A>(this LanguageExt.Thunks.Thunk<Env, LanguageExt.Thunks.ThunkAsync<A>> mma)`
- `where Env : struct, LanguageExt.Effects.Traits.HasCancel<Env>`
- `[ext] public static LanguageExt.Thunks.ThunkAsync<A> Flatten<A>(this LanguageExt.Thunks.ThunkAsync<LanguageExt.Thunks.ThunkAsync<A>> mma)`
- `[ext] public static LanguageExt.Thunks.ThunkAsync<A> Flatten<A>(this LanguageExt.Thunks.ThunkAsync<LanguageExt.Thunks.Thunk<A>> mma)`
- `[ext] public static LanguageExt.Thunks.ThunkAsync<A> Flatten<A>(this LanguageExt.Thunks.Thunk<LanguageExt.Thunks.ThunkAsync<A>> mma)`
- `[ext] public static LanguageExt.Thunks.Thunk<Env, A> Flatten<Env, A>(this LanguageExt.Thunks.Thunk<Env, LanguageExt.Thunks.Thunk<Env, A>> mma)`
- `[ext] public static LanguageExt.Thunks.Thunk<Env, A> Flatten<Env, A>(this LanguageExt.Thunks.Thunk<LanguageExt.Thunks.Thunk<Env, A>> mma)`
- `[ext] public static LanguageExt.Thunks.Thunk<Env, A> Flatten<Env, A>(this LanguageExt.Thunks.Thunk<Env, LanguageExt.Thunks.Thunk<A>> mma)`
- `[ext] public static LanguageExt.Thunks.Thunk<A> Flatten<A>(this LanguageExt.Thunks.Thunk<LanguageExt.Thunks.Thunk<A>> mma)`

### ThunkAsync`1<A> (class)

- `public LanguageExt.Thunks.ThunkAsync<B> BiMap<B>(System.Func<A, B> Succ, System.Func<LanguageExt.Common.Error, LanguageExt.Common.Error> Fail)`
- `public LanguageExt.Thunks.ThunkAsync<B> BiMapAsync<B>(System.Func<A, System.Threading.Tasks.ValueTask<B>> Succ, System.Func<LanguageExt.Common.Error, System.Threading.Tasks.ValueTask<LanguageExt.Common.Error>> Fail)`
- `public static LanguageExt.Thunks.ThunkAsync<A> Cancelled()`
- `public LanguageExt.Thunks.ThunkAsync<A> Clone()`
- `public static LanguageExt.Thunks.ThunkAsync<A> Fail(LanguageExt.Common.Error error)`
- `public static LanguageExt.Thunks.ThunkAsync<A> Lazy(System.Func<System.Threading.Tasks.ValueTask<LanguageExt.Fin<A>>> fun)`
- `public static LanguageExt.Thunks.ThunkAsync<A> Lazy(System.Func<System.Threading.Tasks.ValueTask<A>> fun)`
- `public LanguageExt.Thunks.ThunkAsync<B> Map<B>(System.Func<A, B> f)`
- `public LanguageExt.Thunks.ThunkAsync<B> MapAsync<B>(System.Func<A, System.Threading.Tasks.ValueTask<B>> f)`
- `public System.Threading.Tasks.ValueTask<LanguageExt.Fin<A>> ReValue()`
- `public static LanguageExt.Thunks.ThunkAsync<A> Success(A value)`
- `public System.String ToString()`
- `public System.Threading.Tasks.ValueTask<LanguageExt.Fin<A>> Value()`

### ThunkAsync`2<Env, A> (class)

- `where Env : struct, LanguageExt.Effects.Traits.HasCancel<Env>`

- `public LanguageExt.Thunks.ThunkAsync<Env, B> BiMap<B>(System.Func<A, B> Succ, System.Func<LanguageExt.Common.Error, LanguageExt.Common.Error> Fail)`
- `public LanguageExt.Thunks.ThunkAsync<Env, B> BiMapAsync<B>(System.Func<A, System.Threading.Tasks.ValueTask<B>> Succ, System.Func<LanguageExt.Common.Error, System.Threading.Tasks.ValueTask<LanguageExt.Common.Error>> Fail)`
- `public static LanguageExt.Thunks.ThunkAsync<Env, A> Cancelled()`
- `public LanguageExt.Thunks.ThunkAsync<Env, A> Clone()`
- `public static LanguageExt.Thunks.ThunkAsync<Env, A> Fail(LanguageExt.Common.Error error)`
- `public static LanguageExt.Thunks.ThunkAsync<Env, A> Lazy(System.Func<Env, System.Threading.Tasks.ValueTask<LanguageExt.Fin<A>>> fun)`
- `public static LanguageExt.Thunks.ThunkAsync<Env, A> Lazy(System.Func<Env, System.Threading.Tasks.ValueTask<A>> fun)`
- `public LanguageExt.Thunks.ThunkAsync<Env, B> Map<B>(System.Func<A, B> f)`
- `public LanguageExt.Thunks.ThunkAsync<Env, B> MapAsync<B>(System.Func<A, System.Threading.Tasks.ValueTask<B>> f)`
- `public System.Threading.Tasks.ValueTask<LanguageExt.Fin<A>> ReValue(Env env)`
- `public static LanguageExt.Thunks.ThunkAsync<Env, A> Success(A value)`
- `public System.String ToString()`
- `public System.Threading.Tasks.ValueTask<LanguageExt.Fin<A>> Value(Env env)`

### ThunkExt (class [static])

- `[ext] public static LanguageExt.Thunks.ThunkAsync<Env, B> MapAsync<Env, A, B>(this LanguageExt.Thunks.Thunk<Env, A> ma, System.Func<A, System.Threading.Tasks.ValueTask<B>> f)`
- `where Env : struct, LanguageExt.Effects.Traits.HasCancel<Env>`
- `[ext] public static LanguageExt.Thunks.ThunkAsync<Env, B> MapAsync<Env, A, B>(this LanguageExt.Thunks.Thunk<A> ma, System.Func<A, System.Threading.Tasks.ValueTask<B>> f)`
- `where Env : struct, LanguageExt.Effects.Traits.HasCancel<Env>`
- `[ext] public static LanguageExt.Thunks.ThunkAsync<B> MapAsync<A, B>(this LanguageExt.Thunks.Thunk<A> ma, System.Func<A, System.Threading.Tasks.ValueTask<B>> f)`

### Thunk`1<A> (class)

- `public LanguageExt.Thunks.Thunk<B> BiMap<B>(System.Func<A, B> Succ, System.Func<LanguageExt.Common.Error, LanguageExt.Common.Error> Fail)`
- `public static LanguageExt.Thunks.Thunk<A> Cancelled()`
- `public LanguageExt.Thunks.Thunk<A> Clone()`
- `public static LanguageExt.Thunks.Thunk<A> Fail(LanguageExt.Common.Error error)`
- `public static LanguageExt.Thunks.Thunk<A> Lazy(System.Func<LanguageExt.Fin<A>> fun)`
- `public LanguageExt.Thunks.Thunk<B> Map<B>(System.Func<A, B> f)`
- `public LanguageExt.Fin<A> ReValue()`
- `public static LanguageExt.Thunks.Thunk<A> Success(A value)`
- `public System.String ToString()`
- `public LanguageExt.Fin<A> Value()`

### Thunk`2<Env, A> (class)

- `public LanguageExt.Thunks.Thunk<Env, B> BiMap<B>(System.Func<A, B> Succ, System.Func<LanguageExt.Common.Error, LanguageExt.Common.Error> Fail)`
- `public static LanguageExt.Thunks.Thunk<Env, A> Cancelled()`
- `public LanguageExt.Thunks.Thunk<Env, A> Clone()`
- `public static LanguageExt.Thunks.Thunk<Env, A> Fail(LanguageExt.Common.Error error)`
- `public static LanguageExt.Thunks.Thunk<Env, A> Lazy(System.Func<Env, LanguageExt.Fin<A>> fun)`
- `public LanguageExt.Thunks.Thunk<Env, B> Map<B>(System.Func<A, B> f)`
- `public LanguageExt.Fin<A> ReValue(Env env)`
- `public static LanguageExt.Thunks.Thunk<Env, A> Success(A value)`
- `public System.String ToString()`
- `public LanguageExt.Fin<A> Value(Env env)`

## LanguageExt.TypeClasses

### Alternative`3<AltAB, A, B> (interface) : LanguageExt.TypeClasses.Typeclass

- `public AltAB Append(AltAB x, AltAB y)`
- `public AltAB Empty()`

### ApplicativeAsync`5<FAB, FA, FB, A, B> (interface) : LanguageExt.TypeClasses.ApplicativePureAsync<FA, A>, LanguageExt.TypeClasses.Typeclass

- `public FB Action(FA fa, FB fb)`
- `public FB Apply(FAB fab, FA fa)`

### ApplicativeAsync`8<FABC, FBC, FA, FB, FC, A, B, C> (interface) : LanguageExt.TypeClasses.ApplicativePureAsync<FA, A>, LanguageExt.TypeClasses.Typeclass

- `public FBC Apply(FABC fabc, FA fa)`
- `public FC Apply(FABC fabc, FA fa, FB fb)`

### ApplicativePureAsync`2<FA, A> (interface) : LanguageExt.TypeClasses.Typeclass

- `public FA PureAsync(System.Threading.Tasks.Task<A> x)`

### ApplicativePure`2<FA, A> (interface) : LanguageExt.TypeClasses.Typeclass

- `public FA Pure(A x)`

### Applicative`5<FAB, FA, FB, A, B> (interface) : LanguageExt.TypeClasses.ApplicativePure<FA, A>, LanguageExt.TypeClasses.Typeclass

- `public FB Action(FA fa, FB fb)`
- `public FB Apply(FAB fab, FA fa)`

### Applicative`8<FABC, FBC, FA, FB, FC, A, B, C> (interface) : LanguageExt.TypeClasses.ApplicativePure<FA, A>, LanguageExt.TypeClasses.Typeclass

- `public FBC Apply(FABC fabc, FA fa)`
- `public FC Apply(FABC fabc, FA fa, FB fb)`

### Arithmetic`1<A> (interface) : LanguageExt.TypeClasses.Typeclass

- `public A Negate(A x)`
- `public A Plus(A x, A y)`
- `public A Product(A x, A y)`
- `public A Subtract(A x, A y)`

### BiFoldableAsync`2<F, A> (interface) : LanguageExt.TypeClasses.Typeclass

- `public System.Threading.Tasks.Task<S> BiFold<S>(F foldable, S state, System.Func<S, A, S> fa, System.Func<S, S> fb)`
- `public System.Threading.Tasks.Task<S> BiFoldAsync<S>(F foldable, S state, System.Func<S, A, System.Threading.Tasks.Task<S>> fa, System.Func<S, S> fb)`
- `public System.Threading.Tasks.Task<S> BiFoldAsync<S>(F foldable, S state, System.Func<S, A, S> fa, System.Func<S, System.Threading.Tasks.Task<S>> fb)`
- `public System.Threading.Tasks.Task<S> BiFoldAsync<S>(F foldable, S state, System.Func<S, A, System.Threading.Tasks.Task<S>> fa, System.Func<S, System.Threading.Tasks.Task<S>> fb)`
- `public System.Threading.Tasks.Task<S> BiFoldBack<S>(F foldable, S state, System.Func<S, A, S> fa, System.Func<S, S> fb)`
- `public System.Threading.Tasks.Task<S> BiFoldBackAsync<S>(F foldable, S state, System.Func<S, A, System.Threading.Tasks.Task<S>> fa, System.Func<S, S> fb)`
- `public System.Threading.Tasks.Task<S> BiFoldBackAsync<S>(F foldable, S state, System.Func<S, A, S> fa, System.Func<S, System.Threading.Tasks.Task<S>> fb)`
- `public System.Threading.Tasks.Task<S> BiFoldBackAsync<S>(F foldable, S state, System.Func<S, A, System.Threading.Tasks.Task<S>> fa, System.Func<S, System.Threading.Tasks.Task<S>> fb)`

### BiFoldableAsync`3<F, A, B> (interface) : LanguageExt.TypeClasses.Typeclass

- `public System.Threading.Tasks.Task<S> BiFold<S>(F foldable, S state, System.Func<S, A, S> fa, System.Func<S, B, S> fb)`
- `public System.Threading.Tasks.Task<S> BiFoldAsync<S>(F foldable, S state, System.Func<S, A, System.Threading.Tasks.Task<S>> fa, System.Func<S, B, S> fb)`
- `public System.Threading.Tasks.Task<S> BiFoldAsync<S>(F foldable, S state, System.Func<S, A, S> fa, System.Func<S, B, System.Threading.Tasks.Task<S>> fb)`
- `public System.Threading.Tasks.Task<S> BiFoldAsync<S>(F foldable, S state, System.Func<S, A, System.Threading.Tasks.Task<S>> fa, System.Func<S, B, System.Threading.Tasks.Task<S>> fb)`
- `public System.Threading.Tasks.Task<S> BiFoldBack<S>(F foldable, S state, System.Func<S, A, S> fa, System.Func<S, B, S> fb)`
- `public System.Threading.Tasks.Task<S> BiFoldBackAsync<S>(F foldable, S state, System.Func<S, A, System.Threading.Tasks.Task<S>> fa, System.Func<S, B, S> fb)`
- `public System.Threading.Tasks.Task<S> BiFoldBackAsync<S>(F foldable, S state, System.Func<S, A, S> fa, System.Func<S, B, System.Threading.Tasks.Task<S>> fb)`
- `public System.Threading.Tasks.Task<S> BiFoldBackAsync<S>(F foldable, S state, System.Func<S, A, System.Threading.Tasks.Task<S>> fa, System.Func<S, B, System.Threading.Tasks.Task<S>> fb)`

### BiFoldable`2<F, A> (interface) : LanguageExt.TypeClasses.Foldable<F, A>, LanguageExt.TypeClasses.Foldable<LanguageExt.Unit, F, A>, LanguageExt.TypeClasses.Typeclass

- `public S BiFold<S>(F foldable, S state, System.Func<S, A, S> fa, System.Func<S, S> fb)`
- `public S BiFoldBack<S>(F foldable, S state, System.Func<S, A, S> fa, System.Func<S, S> fb)`

### BiFoldable`3<F, A, B> (interface) : LanguageExt.TypeClasses.Typeclass

- `public S BiFold<S>(F foldable, S state, System.Func<S, A, S> fa, System.Func<S, B, S> fb)`
- `public S BiFoldBack<S>(F foldable, S state, System.Func<S, A, S> fa, System.Func<S, B, S> fb)`

### BiFunctorAsync`6<FAB, FUV, A, B, U, V> (interface) : LanguageExt.TypeClasses.Typeclass

- `public FUV BiMapAsync(FAB ma, System.Func<A, U> fa, System.Func<B, V> fb)`
- `public FUV BiMapAsync(FAB ma, System.Func<A, System.Threading.Tasks.Task<U>> fa, System.Func<B, V> fb)`
- `public FUV BiMapAsync(FAB ma, System.Func<A, U> fa, System.Func<B, System.Threading.Tasks.Task<V>> fb)`
- `public FUV BiMapAsync(FAB ma, System.Func<A, System.Threading.Tasks.Task<U>> fa, System.Func<B, System.Threading.Tasks.Task<V>> fb)`

### BiFunctor`5<FAB, FR, A, B, R> (interface) : LanguageExt.TypeClasses.Typeclass

- `public FR BiMap(FAB ma, System.Func<A, R> fa, System.Func<B, R> fb)`

### BiFunctor`6<FAB, FUV, A, B, U, V> (interface) : LanguageExt.TypeClasses.Typeclass

- `public FUV BiMap(FAB ma, System.Func<A, U> fa, System.Func<B, V> fb)`

### Bool`1<A> (interface) : LanguageExt.TypeClasses.Typeclass

- `public A And(A a, A b)`
- `public A BiCondition(A a, A b)`
- `public A False()`
- `public A Implies(A a, A b)`
- `public A Not(A a)`
- `public A Or(A a, A b)`
- `public A True()`
- `public A XOr(A a, A b)`

### ChoiceAsync`3<CH, L, R> (interface) : LanguageExt.TypeClasses.Typeclass

- `public System.Threading.Tasks.Task<System.Boolean> IsBottom(CH choice)`
- `public System.Threading.Tasks.Task<System.Boolean> IsLeft(CH choice)`
- `public System.Threading.Tasks.Task<System.Boolean> IsRight(CH choice)`
- `public System.Threading.Tasks.Task<C> Match<C>(CH choice, System.Func<L, C> Left, System.Func<R, C> Right, System.Func<C> Bottom)`
- `public System.Threading.Tasks.Task<LanguageExt.Unit> Match(CH choice, System.Action<L> Left, System.Action<R> Right, System.Action Bottom)`
- `public System.Threading.Tasks.Task<C> MatchAsync<C>(CH choice, System.Func<L, System.Threading.Tasks.Task<C>> LeftAsync, System.Func<R, C> Right, System.Func<C> Bottom)`
- `public System.Threading.Tasks.Task<C> MatchAsync<C>(CH choice, System.Func<L, C> Left, System.Func<R, System.Threading.Tasks.Task<C>> RightAsync, System.Func<C> Bottom)`
- `public System.Threading.Tasks.Task<C> MatchAsync<C>(CH choice, System.Func<L, System.Threading.Tasks.Task<C>> LeftAsync, System.Func<R, System.Threading.Tasks.Task<C>> RightAsync, System.Func<C> Bottom)`
- `public System.Threading.Tasks.Task<LanguageExt.Unit> MatchAsync(CH choice, System.Func<L, System.Threading.Tasks.Task> LeftAsync, System.Action<R> Right, System.Action Bottom)`
- `public System.Threading.Tasks.Task<LanguageExt.Unit> MatchAsync(CH choice, System.Action<L> Left, System.Func<R, System.Threading.Tasks.Task> RightAsync, System.Action Bottom)`
- `public System.Threading.Tasks.Task<LanguageExt.Unit> MatchAsync(CH choice, System.Func<L, System.Threading.Tasks.Task> LeftAsync, System.Func<R, System.Threading.Tasks.Task> RightAsync, System.Action Bottom)`
- `public System.Threading.Tasks.Task<C> MatchUnsafe<C>(CH choice, System.Func<L, C> Left, System.Func<R, C> Right, System.Func<C> Bottom)`
- `public System.Threading.Tasks.Task<C> MatchUnsafeAsync<C>(CH choice, System.Func<L, System.Threading.Tasks.Task<C>> LeftAsync, System.Func<R, C> Right, System.Func<C> Bottom)`
- `public System.Threading.Tasks.Task<C> MatchUnsafeAsync<C>(CH choice, System.Func<L, C> Left, System.Func<R, System.Threading.Tasks.Task<C>> RightAsync, System.Func<C> Bottom)`
- `public System.Threading.Tasks.Task<C> MatchUnsafeAsync<C>(CH choice, System.Func<L, System.Threading.Tasks.Task<C>> LeftAsync, System.Func<R, System.Threading.Tasks.Task<C>> RightAsync, System.Func<C> Bottom)`

### ChoiceUnsafeAsync`3<CH, L, R> (interface) : LanguageExt.TypeClasses.Typeclass

- `public System.Threading.Tasks.Task<System.Boolean> IsBottom(CH choice)`
- `public System.Threading.Tasks.Task<System.Boolean> IsLeft(CH choice)`
- `public System.Threading.Tasks.Task<System.Boolean> IsRight(CH choice)`
- `public System.Threading.Tasks.Task<C> MatchUnsafe<C>(CH choice, System.Func<L, C> Left, System.Func<R, C> Right, System.Func<C> Bottom)`
- `public System.Threading.Tasks.Task<LanguageExt.Unit> MatchUnsafe(CH choice, System.Action<L> Left, System.Action<R> Right, System.Action Bottom)`
- `public System.Threading.Tasks.Task<C> MatchUnsafeAsync<C>(CH choice, System.Func<L, System.Threading.Tasks.Task<C>> LeftAsync, System.Func<R, C> Right, System.Func<C> Bottom)`
- `public System.Threading.Tasks.Task<C> MatchUnsafeAsync<C>(CH choice, System.Func<L, C> Left, System.Func<R, System.Threading.Tasks.Task<C>> RightAsync, System.Func<C> Bottom)`
- `public System.Threading.Tasks.Task<C> MatchUnsafeAsync<C>(CH choice, System.Func<L, System.Threading.Tasks.Task<C>> LeftAsync, System.Func<R, System.Threading.Tasks.Task<C>> RightAsync, System.Func<C> Bottom)`
- `public System.Threading.Tasks.Task<LanguageExt.Unit> MatchUnsafeAsync(CH choice, System.Func<L, System.Threading.Tasks.Task> LeftAsync, System.Action<R> Right, System.Action Bottom)`
- `public System.Threading.Tasks.Task<LanguageExt.Unit> MatchUnsafeAsync(CH choice, System.Action<L> Left, System.Func<R, System.Threading.Tasks.Task> RightAsync, System.Action Bottom)`
- `public System.Threading.Tasks.Task<LanguageExt.Unit> MatchUnsafeAsync(CH choice, System.Func<L, System.Threading.Tasks.Task> LeftAsync, System.Func<R, System.Threading.Tasks.Task> RightAsync, System.Action Bottom)`

### ChoiceUnsafe`3<CH, L, R> (interface) : LanguageExt.TypeClasses.Typeclass

- `public System.Boolean IsBottom(CH choice)`
- `public System.Boolean IsLeft(CH choice)`
- `public System.Boolean IsRight(CH choice)`
- `public LanguageExt.Unit Match(CH choice, System.Action<L> Left, System.Action<R> Right, System.Action Bottom)`
- `public C MatchUnsafe<C>(CH choice, System.Func<L, C> Left, System.Func<R, C> Right, System.Func<C> Bottom)`

### Choice`3<CH, L, R> (interface) : LanguageExt.TypeClasses.Typeclass

- `public System.Boolean IsBottom(CH choice)`
- `public System.Boolean IsLeft(CH choice)`
- `public System.Boolean IsRight(CH choice)`
- `public C Match<C>(CH choice, System.Func<L, C> Left, System.Func<R, C> Right, System.Func<C> Bottom)`
- `public LanguageExt.Unit Match(CH choice, System.Action<L> Left, System.Action<R> Right, System.Action Bottom)`

### Const`1<TYPE> (interface) : LanguageExt.TypeClasses.Typeclass

- `public TYPE Value { get; }`

### EqAsync`1<A> (interface) : LanguageExt.HashableAsync<A>, LanguageExt.TypeClasses.Typeclass

- `public System.Threading.Tasks.Task<System.Boolean> EqualsAsync(A x, A y)`

### EqExt (class [static])

- `[ext] public static System.Collections.Generic.IEqualityComparer<A> ToEqualityComparer<A>(this LanguageExt.TypeClasses.Eq<A> self)`

### Eq`1<A> (interface) : LanguageExt.Hashable<A>, LanguageExt.HashableAsync<A>, LanguageExt.TypeClasses.Typeclass, LanguageExt.TypeClasses.EqAsync<A>

- `public System.Boolean Equals(A x, A y)`

### Floating`1<A> (interface) : LanguageExt.TypeClasses.Fraction<A>, LanguageExt.TypeClasses.Num<A>, LanguageExt.TypeClasses.Ord<A>, LanguageExt.TypeClasses.Eq<A>, LanguageExt.Hashable<A>, LanguageExt.HashableAsync<A>, LanguageExt.TypeClasses.Typeclass, LanguageExt.TypeClasses.EqAsync<A>, LanguageExt.TypeClasses.OrdAsync<A>, LanguageExt.TypeClasses.Monoid<A>, LanguageExt.TypeClasses.Semigroup<A>, LanguageExt.TypeClasses.Arithmetic<A>

- `public A Acos(A x)`
- `public A Acosh(A x)`
- `public A Asin(A x)`
- `public A Asinh(A x)`
- `public A Atan(A x)`
- `public A Atanh(A x)`
- `public A Cos(A x)`
- `public A Cosh(A x)`
- `public A Exp(A x)`
- `public A Log(A x)`
- `public A LogBase(A x, A y)`
- `public A Pi()`
- `public A Pow(A x, A y)`
- `public A Sin(A x)`
- `public A Sinh(A x)`
- `public A Sqrt(A x)`
- `public A Tan(A x)`
- `public A Tanh(A x)`

### FoldableAsync`2<FA, A> (interface) : LanguageExt.TypeClasses.FoldableAsync<LanguageExt.Unit, FA, A>, LanguageExt.TypeClasses.Typeclass


### FoldableAsync`3<Env, FA, A> (interface) : LanguageExt.TypeClasses.Typeclass

- `public System.Func<Env, System.Threading.Tasks.Task<System.Int32>> Count(FA fa)`
- `public System.Func<Env, System.Threading.Tasks.Task<S>> Fold<S>(FA fa, S state, System.Func<S, A, S> f)`
- `public System.Func<Env, System.Threading.Tasks.Task<S>> FoldAsync<S>(FA fa, S state, System.Func<S, A, System.Threading.Tasks.Task<S>> f)`
- `public System.Func<Env, System.Threading.Tasks.Task<S>> FoldBack<S>(FA fa, S state, System.Func<S, A, S> f)`
- `public System.Func<Env, System.Threading.Tasks.Task<S>> FoldBackAsync<S>(FA fa, S state, System.Func<S, A, System.Threading.Tasks.Task<S>> f)`

### Foldable`2<FA, A> (interface) : LanguageExt.TypeClasses.Foldable<LanguageExt.Unit, FA, A>, LanguageExt.TypeClasses.Typeclass


### Foldable`3<Env, FA, A> (interface) : LanguageExt.TypeClasses.Typeclass

- `public System.Func<Env, System.Int32> Count(FA fa)`
- `public System.Func<Env, S> Fold<S>(FA fa, S state, System.Func<S, A, S> f)`
- `public System.Func<Env, S> FoldBack<S>(FA fa, S state, System.Func<S, A, S> f)`

### Fraction`1<A> (interface) : LanguageExt.TypeClasses.Num<A>, LanguageExt.TypeClasses.Ord<A>, LanguageExt.TypeClasses.Eq<A>, LanguageExt.Hashable<A>, LanguageExt.HashableAsync<A>, LanguageExt.TypeClasses.Typeclass, LanguageExt.TypeClasses.EqAsync<A>, LanguageExt.TypeClasses.OrdAsync<A>, LanguageExt.TypeClasses.Monoid<A>, LanguageExt.TypeClasses.Semigroup<A>, LanguageExt.TypeClasses.Arithmetic<A>

- `public A FromRational(LanguageExt.Ratio<System.Int32> x)`

### FunctorAsync`4<FA, FB, A, B> (interface) : LanguageExt.TypeClasses.Functor<FA, FB, A, B>, LanguageExt.TypeClasses.Typeclass

- `public FB MapAsync(FA ma, System.Func<A, System.Threading.Tasks.Task<B>> f)`

### Functor`4<FA, FB, A, B> (interface) : LanguageExt.TypeClasses.Typeclass

- `public FB Map(FA ma, System.Func<A, B> f)`

### Indexable`3<A, KEY, VALUE> (interface)

- `public VALUE Get(A ma, KEY key)`
- `public LanguageExt.Option<VALUE> TryGet(A ma, KEY key)`

### Liftable`2<LA, A> (interface) : LanguageExt.TypeClasses.Typeclass

- `public LA Lift(A x)`

### MonadAsync`2<MA, A> (interface) : LanguageExt.TypeClasses.MonadAsync<LanguageExt.Unit, LanguageExt.Unit, MA, A>, LanguageExt.TypeClasses.FoldableAsync<LanguageExt.Unit, MA, A>, LanguageExt.TypeClasses.Typeclass, LanguageExt.TypeClasses.FoldableAsync<MA, A>

- `public MA ReturnAsync(System.Threading.Tasks.Task<A> x)`

### MonadAsync`4<Env, Out, MA, A> (interface) : LanguageExt.TypeClasses.FoldableAsync<Env, MA, A>, LanguageExt.TypeClasses.Typeclass

- `public MA Apply(System.Func<A, A, A> f, MA ma, MA mb)`
- `public MB Bind<MONADB, MB, B>(MA ma, System.Func<A, MB> f)`
- `where MONADB : struct, LanguageExt.TypeClasses.MonadAsync<Env, Out, MB, B>`
- `public MB BindAsync<MONADB, MB, B>(MA ma, System.Func<A, System.Threading.Tasks.Task<MB>> f)`
- `where MONADB : struct, LanguageExt.TypeClasses.MonadAsync<Env, Out, MB, B>`
- `public MA BindReturn(Out outputma, MA mb)`
- `public MA Fail(System.Object err)`
- `public MA Plus(MA ma, MA mb)`
- `public MA ReturnAsync(System.Func<Env, System.Threading.Tasks.Task<A>> f)`
- `public MA RunAsync(System.Func<Env, System.Threading.Tasks.Task<MA>> ma)`
- `public MA Zero()`

### MonadRWS`5<MonoidW, R, W, S, A> (interface) : LanguageExt.TypeClasses.Typeclass

- `where MonoidW : struct, LanguageExt.TypeClasses.Monoid<W>`

- `public LanguageExt.RWS<MonoidW, R, W, S, R> Ask()`
- `public LanguageExt.RWS<MonoidW, R, W, S, S> Get()`
- `public LanguageExt.RWS<MonoidW, R, W, S, System.ValueTuple<A, B>> Listen<B>(LanguageExt.RWS<MonoidW, R, W, S, A> ma, System.Func<W, B> f)`
- `public LanguageExt.RWS<MonoidW, R, W, S, A> Local(LanguageExt.RWS<MonoidW, R, W, S, A> ma, System.Func<R, R> f)`
- `public LanguageExt.RWS<MonoidW, R, W, S, LanguageExt.Unit> Put(S state)`
- `public LanguageExt.RWS<MonoidW, R, W, S, LanguageExt.Unit> Tell(W what)`

### MonadReader`2<Env, A> (interface) : LanguageExt.TypeClasses.Typeclass

- `public LanguageExt.Reader<Env, Env> Ask()`
- `public LanguageExt.Reader<Env, A> Local(LanguageExt.Reader<Env, A> ma, System.Func<Env, Env> f)`
- `public LanguageExt.Reader<Env, A> Reader(System.Func<Env, A> f)`

### MonadState`2<S, A> (interface) : LanguageExt.TypeClasses.Typeclass

- `public LanguageExt.State<S, S> Get()`
- `public LanguageExt.State<S, LanguageExt.Unit> Put(S state)`
- `public LanguageExt.State<S, A> State(System.Func<S, A> f)`

### MonadTransAsyncSync`5<OuterMonad, OuterType, InnerMonad, InnerType, A> (interface) : LanguageExt.TypeClasses.Typeclass

- `where OuterMonad : struct, LanguageExt.TypeClasses.MonadAsync<OuterType, InnerType>`
- `where InnerMonad : struct, LanguageExt.TypeClasses.Monad<InnerType, A>`

- `public NewOuterType Bind<NewOuterMonad, NewOuterType, NewInnerMonad, NewInnerType, B>(OuterType ma, System.Func<A, NewInnerType> f)`
- `where NewOuterMonad : struct, LanguageExt.TypeClasses.MonadAsync<NewOuterType, NewInnerType>`
- `where NewInnerMonad : struct, LanguageExt.TypeClasses.Monad<NewInnerType, B>`
- `public System.Threading.Tasks.Task<S> Fold<S>(OuterType ma, S state, System.Func<S, A, S> f)`
- `public System.Threading.Tasks.Task<S> FoldBack<S>(OuterType ma, S state, System.Func<S, A, S> f)`
- `public NewOuterType Map<NewOuterMonad, NewOuterType, NewInnerMonad, NewInnerType, B>(OuterType ma, System.Func<A, B> f)`
- `where NewOuterMonad : struct, LanguageExt.TypeClasses.MonadAsync<NewOuterType, NewInnerType>`
- `where NewInnerMonad : struct, LanguageExt.TypeClasses.Monad<NewInnerType, B>`
- `public OuterType Plus(OuterType a, OuterType b)`
- `public OuterType Zero()`

### MonadTransAsync`5<OuterMonad, OuterType, InnerMonad, InnerType, A> (interface) : LanguageExt.TypeClasses.Typeclass

- `where OuterMonad : struct, LanguageExt.TypeClasses.MonadAsync<OuterType, InnerType>`
- `where InnerMonad : struct, LanguageExt.TypeClasses.MonadAsync<InnerType, A>`

- `public NewOuterType Bind<NewOuterMonad, NewOuterType, NewInnerMonad, NewInnerType, B>(OuterType ma, System.Func<A, NewOuterType> f)`
- `where NewOuterMonad : struct, LanguageExt.TypeClasses.MonadAsync<NewOuterType, NewInnerType>`
- `where NewInnerMonad : struct, LanguageExt.TypeClasses.MonadAsync<NewInnerType, B>`
- `public NewOuterType Bind<NewOuterMonad, NewOuterType, NewInnerMonad, NewInnerType, B>(OuterType ma, System.Func<A, NewInnerType> f)`
- `where NewOuterMonad : struct, LanguageExt.TypeClasses.MonadAsync<NewOuterType, NewInnerType>`
- `where NewInnerMonad : struct, LanguageExt.TypeClasses.MonadAsync<NewInnerType, B>`
- `public NewOuterType BindAsync<NewOuterMonad, NewOuterType, NewInnerMonad, NewInnerType, B>(OuterType ma, System.Func<A, System.Threading.Tasks.Task<NewOuterType>> f)`
- `where NewOuterMonad : struct, LanguageExt.TypeClasses.MonadAsync<NewOuterType, NewInnerType>`
- `where NewInnerMonad : struct, LanguageExt.TypeClasses.MonadAsync<NewInnerType, B>`
- `public NewOuterType BindAsync<NewOuterMonad, NewOuterType, NewInnerMonad, NewInnerType, B>(OuterType ma, System.Func<A, System.Threading.Tasks.Task<NewInnerType>> f)`
- `where NewOuterMonad : struct, LanguageExt.TypeClasses.MonadAsync<NewOuterType, NewInnerType>`
- `where NewInnerMonad : struct, LanguageExt.TypeClasses.MonadAsync<NewInnerType, B>`
- `public System.Threading.Tasks.Task<S> Fold<S>(OuterType ma, S state, System.Func<S, A, S> f)`
- `public System.Threading.Tasks.Task<S> FoldAsync<S>(OuterType ma, S state, System.Func<S, A, System.Threading.Tasks.Task<S>> f)`
- `public System.Threading.Tasks.Task<S> FoldBack<S>(OuterType ma, S state, System.Func<S, A, S> f)`
- `public System.Threading.Tasks.Task<S> FoldBackAsync<S>(OuterType ma, S state, System.Func<S, A, System.Threading.Tasks.Task<S>> f)`
- `public NewOuterType Map<NewOuterMonad, NewOuterType, NewInnerMonad, NewInnerType, B>(OuterType ma, System.Func<A, B> f)`
- `where NewOuterMonad : struct, LanguageExt.TypeClasses.MonadAsync<NewOuterType, NewInnerType>`
- `where NewInnerMonad : struct, LanguageExt.TypeClasses.MonadAsync<NewInnerType, B>`
- `public NewOuterType MapAsync<NewOuterMonad, NewOuterType, NewInnerMonad, NewInnerType, B>(OuterType ma, System.Func<A, System.Threading.Tasks.Task<B>> f)`
- `where NewOuterMonad : struct, LanguageExt.TypeClasses.MonadAsync<NewOuterType, NewInnerType>`
- `where NewInnerMonad : struct, LanguageExt.TypeClasses.MonadAsync<NewInnerType, B>`
- `public OuterType Plus(OuterType a, OuterType b)`
- `public NewOuterType Sequence<NewOuterMonad, NewOuterType, NewInnerMonad, NewInnerType>(OuterType ma)`
- `where NewOuterMonad : struct, LanguageExt.TypeClasses.MonadAsync<NewOuterType, NewInnerType>`
- `where NewInnerMonad : struct, LanguageExt.TypeClasses.MonadAsync<NewInnerType, A>`
- `public NewOuterType Traverse<NewOuterMonad, NewOuterType, NewInnerMonad, NewInnerType, B>(OuterType ma, System.Func<A, B> f)`
- `where NewOuterMonad : struct, LanguageExt.TypeClasses.MonadAsync<NewOuterType, NewInnerType>`
- `where NewInnerMonad : struct, LanguageExt.TypeClasses.MonadAsync<NewInnerType, B>`
- `public NewOuterType TraverseAsync<NewOuterMonad, NewOuterType, NewInnerMonad, NewInnerType, B>(OuterType ma, System.Func<A, System.Threading.Tasks.Task<B>> f)`
- `where NewOuterMonad : struct, LanguageExt.TypeClasses.MonadAsync<NewOuterType, NewInnerType>`
- `where NewInnerMonad : struct, LanguageExt.TypeClasses.MonadAsync<NewInnerType, B>`
- `public OuterType Zero()`

### MonadTransSyncAsync`5<OuterMonad, OuterType, InnerMonad, InnerType, A> (interface) : LanguageExt.TypeClasses.Typeclass

- `where OuterMonad : struct, LanguageExt.TypeClasses.Monad<OuterType, InnerType>`
- `where InnerMonad : struct, LanguageExt.TypeClasses.MonadAsync<InnerType, A>`

- `public NewOuterType Bind<NewOuterMonad, NewOuterType, NewInnerMonad, NewInnerType, B>(OuterType ma, System.Func<A, NewInnerType> f)`
- `where NewOuterMonad : struct, LanguageExt.TypeClasses.Monad<NewOuterType, NewInnerType>`
- `where NewInnerMonad : struct, LanguageExt.TypeClasses.MonadAsync<NewInnerType, B>`
- `public NewOuterType BindAsync<NewOuterMonad, NewOuterType, NewInnerMonad, NewInnerType, B>(OuterType ma, System.Func<A, System.Threading.Tasks.Task<NewInnerType>> f)`
- `where NewOuterMonad : struct, LanguageExt.TypeClasses.Monad<NewOuterType, NewInnerType>`
- `where NewInnerMonad : struct, LanguageExt.TypeClasses.MonadAsync<NewInnerType, B>`
- `public System.Threading.Tasks.Task<S> Fold<S>(OuterType ma, S state, System.Func<S, A, S> f)`
- `public System.Threading.Tasks.Task<S> FoldAsync<S>(OuterType ma, S state, System.Func<S, A, System.Threading.Tasks.Task<S>> f)`
- `public System.Threading.Tasks.Task<S> FoldBack<S>(OuterType ma, S state, System.Func<S, A, S> f)`
- `public System.Threading.Tasks.Task<S> FoldBackAsync<S>(OuterType ma, S state, System.Func<S, A, System.Threading.Tasks.Task<S>> f)`
- `public NewOuterType Map<NewOuterMonad, NewOuterType, NewInnerMonad, NewInnerType, B>(OuterType ma, System.Func<A, B> f)`
- `where NewOuterMonad : struct, LanguageExt.TypeClasses.Monad<NewOuterType, NewInnerType>`
- `where NewInnerMonad : struct, LanguageExt.TypeClasses.MonadAsync<NewInnerType, B>`
- `public NewOuterType MapAsync<NewOuterMonad, NewOuterType, NewInnerMonad, NewInnerType, B>(OuterType ma, System.Func<A, System.Threading.Tasks.Task<B>> f)`
- `where NewOuterMonad : struct, LanguageExt.TypeClasses.Monad<NewOuterType, NewInnerType>`
- `where NewInnerMonad : struct, LanguageExt.TypeClasses.MonadAsync<NewInnerType, B>`
- `public OuterType Plus(OuterType a, OuterType b)`
- `public NewOuterType Sequence<NewOuterMonad, NewOuterType, NewInnerMonad, NewInnerType>(OuterType ma)`
- `where NewOuterMonad : struct, LanguageExt.TypeClasses.MonadAsync<NewOuterType, NewInnerType>`
- `where NewInnerMonad : struct, LanguageExt.TypeClasses.Monad<NewInnerType, A>`
- `public NewOuterType Traverse<NewOuterMonad, NewOuterType, NewInnerMonad, NewInnerType, B>(OuterType ma, System.Func<A, B> f)`
- `where NewOuterMonad : struct, LanguageExt.TypeClasses.MonadAsync<NewOuterType, NewInnerType>`
- `where NewInnerMonad : struct, LanguageExt.TypeClasses.Monad<NewInnerType, B>`
- `public NewOuterType TraverseAsync<NewOuterMonad, NewOuterType, NewInnerMonad, NewInnerType, B>(OuterType ma, System.Func<A, System.Threading.Tasks.Task<B>> f)`
- `where NewOuterMonad : struct, LanguageExt.TypeClasses.MonadAsync<NewOuterType, NewInnerType>`
- `where NewInnerMonad : struct, LanguageExt.TypeClasses.Monad<NewInnerType, B>`
- `public OuterType Zero()`

### MonadTrans`5<OuterMonad, OuterType, InnerMonad, InnerType, A> (interface) : LanguageExt.TypeClasses.Typeclass

- `where OuterMonad : struct, LanguageExt.TypeClasses.Monad<OuterType, InnerType>`
- `where InnerMonad : struct, LanguageExt.TypeClasses.Monad<InnerType, A>`

- `public NewOuterType Bind<NewOuterMonad, NewOuterType, NewInnerMonad, NewInnerType, B>(OuterType ma, System.Func<A, NewOuterType> f)`
- `where NewOuterMonad : struct, LanguageExt.TypeClasses.Monad<NewOuterType, NewInnerType>`
- `where NewInnerMonad : struct, LanguageExt.TypeClasses.Monad<NewInnerType, B>`
- `public NewOuterType Bind<NewOuterMonad, NewOuterType, NewInnerMonad, NewInnerType, B>(OuterType ma, System.Func<A, NewInnerType> f)`
- `where NewOuterMonad : struct, LanguageExt.TypeClasses.Monad<NewOuterType, NewInnerType>`
- `where NewInnerMonad : struct, LanguageExt.TypeClasses.Monad<NewInnerType, B>`
- `public NewOuterType BindAsync<NewOuterMonad, NewOuterType, NewInnerMonad, NewInnerType, B>(OuterType ma, System.Func<A, NewOuterType> f)`
- `where NewOuterMonad : struct, LanguageExt.TypeClasses.MonadAsync<NewOuterType, NewInnerType>`
- `where NewInnerMonad : struct, LanguageExt.TypeClasses.Monad<NewInnerType, B>`
- `public S Fold<S>(OuterType ma, S state, System.Func<S, A, S> f)`
- `public S FoldBack<S>(OuterType ma, S state, System.Func<S, A, S> f)`
- `public NewOuterType Map<NewOuterMonad, NewOuterType, NewInnerMonad, NewInnerType, B>(OuterType ma, System.Func<A, B> f)`
- `where NewOuterMonad : struct, LanguageExt.TypeClasses.Monad<NewOuterType, NewInnerType>`
- `where NewInnerMonad : struct, LanguageExt.TypeClasses.Monad<NewInnerType, B>`
- `public OuterType Plus(OuterType a, OuterType b)`
- `public NewOuterType Sequence<NewOuterMonad, NewOuterType, NewInnerMonad, NewInnerType>(OuterType ma)`
- `where NewOuterMonad : struct, LanguageExt.TypeClasses.Monad<NewOuterType, NewInnerType>`
- `where NewInnerMonad : struct, LanguageExt.TypeClasses.Monad<NewInnerType, A>`
- `public NewOuterType Traverse<NewOuterMonad, NewOuterType, NewInnerMonad, NewInnerType, B>(OuterType ma, System.Func<A, B> f)`
- `where NewOuterMonad : struct, LanguageExt.TypeClasses.Monad<NewOuterType, NewInnerType>`
- `where NewInnerMonad : struct, LanguageExt.TypeClasses.Monad<NewInnerType, B>`
- `public OuterType Zero()`

### MonadWriter`3<MonoidW, W, A> (interface) : LanguageExt.TypeClasses.Typeclass

- `where MonoidW : struct, LanguageExt.TypeClasses.Monoid<W>`

- `public LanguageExt.Writer<MonoidW, W, System.ValueTuple<A, B>> Listen<B>(LanguageExt.Writer<MonoidW, W, A> ma, System.Func<W, B> f)`
- `public LanguageExt.Writer<MonoidW, W, LanguageExt.Unit> Tell(W what)`

### Monad`2<MA, A> (interface) : LanguageExt.TypeClasses.Monad<LanguageExt.Unit, LanguageExt.Unit, MA, A>, LanguageExt.TypeClasses.Foldable<LanguageExt.Unit, MA, A>, LanguageExt.TypeClasses.Typeclass, LanguageExt.TypeClasses.Foldable<MA, A>

- `public MA Return(A x)`

### Monad`4<Env, Out, MA, A> (interface) : LanguageExt.TypeClasses.Foldable<Env, MA, A>, LanguageExt.TypeClasses.Typeclass

- `public MA Apply(System.Func<A, A, A> f, MA ma, MA mb)`
- `public MB Bind<MONADB, MB, B>(MA ma, System.Func<A, MB> f)`
- `where MONADB : struct, LanguageExt.TypeClasses.Monad<Env, Out, MB, B>`
- `public MB BindAsync<MonadB, MB, B>(MA ma, System.Func<A, MB> f)`
- `where MonadB : struct, LanguageExt.TypeClasses.MonadAsync<Env, Out, MB, B>`
- `public MA BindReturn(Out outputma, MA mb)`
- `public MA Fail(System.Object err)`
- `public MA Plus(MA ma, MA mb)`
- `public MA Return(System.Func<Env, A> f)`
- `public MA Run(System.Func<Env, MA> ma)`
- `public MA Zero()`

### Monoid`1<A> (interface) : LanguageExt.TypeClasses.Semigroup<A>, LanguageExt.TypeClasses.Typeclass

- `public A Empty()`

### Num`1<A> (interface) : LanguageExt.TypeClasses.Ord<A>, LanguageExt.TypeClasses.Eq<A>, LanguageExt.Hashable<A>, LanguageExt.HashableAsync<A>, LanguageExt.TypeClasses.Typeclass, LanguageExt.TypeClasses.EqAsync<A>, LanguageExt.TypeClasses.OrdAsync<A>, LanguageExt.TypeClasses.Monoid<A>, LanguageExt.TypeClasses.Semigroup<A>, LanguageExt.TypeClasses.Arithmetic<A>

- `public A Abs(A x)`
- `public A Divide(A x, A y)`
- `public A FromDecimal(System.Decimal x)`
- `public A FromDouble(System.Double x)`
- `public A FromFloat(System.Single x)`
- `public A FromInteger(System.Int32 x)`
- `public A Signum(A x)`

### OptionalAsync`2<OA, A> (interface) : LanguageExt.TypeClasses.Typeclass

- `public OA None { get; }`
- `public System.Threading.Tasks.Task<System.Boolean> IsNone(OA opt)`
- `public System.Threading.Tasks.Task<System.Boolean> IsSome(OA opt)`
- `public System.Threading.Tasks.Task<B> Match<B>(OA opt, System.Func<A, B> Some, System.Func<B> None)`
- `public System.Threading.Tasks.Task<LanguageExt.Unit> Match(OA opt, System.Action<A> Some, System.Action None)`
- `public System.Threading.Tasks.Task<B> MatchAsync<B>(OA opt, System.Func<A, System.Threading.Tasks.Task<B>> SomeAsync, System.Func<B> None)`
- `public System.Threading.Tasks.Task<B> MatchAsync<B>(OA opt, System.Func<A, B> Some, System.Func<System.Threading.Tasks.Task<B>> NoneAsync)`
- `public System.Threading.Tasks.Task<B> MatchAsync<B>(OA opt, System.Func<A, System.Threading.Tasks.Task<B>> SomeAsync, System.Func<System.Threading.Tasks.Task<B>> NoneAsync)`
- `public System.Threading.Tasks.Task<LanguageExt.Unit> MatchAsync(OA opt, System.Func<A, System.Threading.Tasks.Task> SomeAsync, System.Action None)`
- `public System.Threading.Tasks.Task<LanguageExt.Unit> MatchAsync(OA opt, System.Func<A, System.Threading.Tasks.Task> SomeAsync, System.Func<System.Threading.Tasks.Task> NoneAsync)`
- `public System.Threading.Tasks.Task<LanguageExt.Unit> MatchAsync(OA opt, System.Action<A> Some, System.Func<System.Threading.Tasks.Task> NoneAsync)`
- `public OA Optional(A value)`
- `public OA OptionalAsync(System.Threading.Tasks.Task<A> value)`
- `public OA Some(A value)`
- `public OA SomeAsync(System.Threading.Tasks.Task<A> value)`

### OptionalUnsafeAsync`2<OA, A> (interface) : LanguageExt.TypeClasses.Typeclass

- `public OA None { get; }`
- `public System.Threading.Tasks.Task<System.Boolean> IsNone(OA opt)`
- `public System.Threading.Tasks.Task<System.Boolean> IsSome(OA opt)`
- `public System.Threading.Tasks.Task<LanguageExt.Unit> Match(OA opt, System.Action<A> Some, System.Action None)`
- `public System.Threading.Tasks.Task<B> MatchUnsafe<B>(OA opt, System.Func<A, B> Some, System.Func<B> None)`
- `public System.Threading.Tasks.Task<B> MatchUnsafeAsync<B>(OA opt, System.Func<A, System.Threading.Tasks.Task<B>> Some, System.Func<B> None)`
- `public System.Threading.Tasks.Task<B> MatchUnsafeAsync<B>(OA opt, System.Func<A, B> Some, System.Func<System.Threading.Tasks.Task<B>> None)`
- `public System.Threading.Tasks.Task<B> MatchUnsafeAsync<B>(OA opt, System.Func<A, System.Threading.Tasks.Task<B>> Some, System.Func<System.Threading.Tasks.Task<B>> None)`
- `public OA Optional(A value)`
- `public OA OptionalAsync(System.Threading.Tasks.Task<A> value)`
- `public OA Some(A value)`
- `public OA SomeAsync(System.Threading.Tasks.Task<A> value)`

### OptionalUnsafe`2<OA, A> (interface) : LanguageExt.TypeClasses.Typeclass

- `public OA None { get; }`
- `public System.Boolean IsNone(OA opt)`
- `public System.Boolean IsSome(OA opt)`
- `public LanguageExt.Unit Match(OA opt, System.Action<A> Some, System.Action None)`
- `public B MatchUnsafe<B>(OA opt, System.Func<A, B> Some, System.Func<B> None)`
- `public B MatchUnsafe<B>(OA opt, System.Func<A, B> Some, B None)`
- `public OA Optional(A value)`
- `public OA Some(A value)`

### Optional`2<OA, A> (interface) : LanguageExt.TypeClasses.Typeclass

- `public OA None { get; }`
- `public System.Boolean IsNone(OA opt)`
- `public System.Boolean IsSome(OA opt)`
- `public B Match<B>(OA opt, System.Func<A, B> Some, System.Func<B> None)`
- `public B Match<B>(OA opt, System.Func<A, B> Some, B None)`
- `public LanguageExt.Unit Match(OA opt, System.Action<A> Some, System.Action None)`
- `public OA Optional(A value)`
- `public OA Some(A value)`

### OrdAsync`1<A> (interface) : LanguageExt.TypeClasses.EqAsync<A>, LanguageExt.HashableAsync<A>, LanguageExt.TypeClasses.Typeclass

- `public System.Threading.Tasks.Task<System.Int32> CompareAsync(A x, A y)`

### OrdExt (class [static])

- `[ext] public static System.Collections.Generic.IComparer<A> ToComparable<A>(this LanguageExt.TypeClasses.Ord<A> self)`
- `[ext] public static System.Collections.Generic.IComparer<A> ToComparer<A>(this LanguageExt.TypeClasses.Ord<A> self)`

### Ord`1<A> (interface) : LanguageExt.TypeClasses.Eq<A>, LanguageExt.Hashable<A>, LanguageExt.HashableAsync<A>, LanguageExt.TypeClasses.Typeclass, LanguageExt.TypeClasses.EqAsync<A>, LanguageExt.TypeClasses.OrdAsync<A>

- `public System.Int32 Compare(A x, A y)`

### Pred`1<A> (interface) : LanguageExt.TypeClasses.Typeclass

- `public System.Boolean True(A value)`

### Semigroup`1<A> (interface) : LanguageExt.TypeClasses.Typeclass

- `public A Append(A x, A y)`

### TriFunctor`6<FABC, FR, A, B, C, R> (interface) : LanguageExt.TypeClasses.Typeclass

- `public FR TriMap(FABC ma, System.Func<A, R> fa, System.Func<B, R> fb, System.Func<B, R> fc)`

### TriFunctor`8<FABC, FTUV, A, B, C, T, U, V> (interface) : LanguageExt.TypeClasses.Typeclass

- `public FTUV TriMap(FABC ma, System.Func<A, T> fa, System.Func<B, U> fb, System.Func<C, V> fc)`

### Typeclass (interface)


