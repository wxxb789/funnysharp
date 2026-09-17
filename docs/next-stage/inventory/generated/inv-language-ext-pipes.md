# Public API inventory: LanguageExt.Core 4.4.9 Pipes

Assemblies: LanguageExt.Core 4.0.0.0

Type count: 63

## LanguageExt.Pipes

### Await<IN, A> (class) : LanguageExt.Pipes.Consumer<IN, A>

- `public Await(System.Func<IN, LanguageExt.Pipes.Consumer<IN, A>> next)`
- `public readonly System.Func<IN, LanguageExt.Pipes.Consumer<IN, A>> Next`
- `public LanguageExt.Pipes.Consumer<RT, IN, A> Interpret<RT>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Consumer<IN, B> Select<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Consumer<IN, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Consumer<IN, B>> f)`
- `public LanguageExt.Pipes.ConsumerLift<RT, IN, B> SelectMany<RT, B>(System.Func<A, LanguageExt.Pipes.ConsumerLift<RT, IN, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Consumer<RT, IN, B> SelectMany<RT, B>(System.Func<A, LanguageExt.Pipes.Consumer<RT, IN, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<IN, OUT, B> SelectMany<OUT, B>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<RT, OUT, B>(System.Func<A, LanguageExt.Pipes.ProducerLift<RT, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.ConsumerLift<RT, IN, A> ToConsumerLift<RT>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<IN, OUT, A> ToPipe<OUT>()`

### Await<RT, IN, A> (class) : LanguageExt.Pipes.ConsumerLift<RT, IN, A>

- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

- `public Await(System.Func<IN, LanguageExt.Pipes.ConsumerLift<RT, IN, A>> next)`
- `public readonly System.Func<IN, LanguageExt.Pipes.ConsumerLift<RT, IN, A>> Next`
- `public LanguageExt.Pipes.Consumer<RT, IN, A> Interpret()`
- `public LanguageExt.Pipes.ConsumerLift<RT, IN, B> Select<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.ConsumerLift<RT, IN, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.ConsumerLift<RT, IN, B>> f)`
- `public LanguageExt.Pipes.Consumer<RT, IN, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Consumer<RT, IN, B>> f)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<OUT, B>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, A> ToPipe<OUT>()`

### Await<IN, OUT, A> (class) : LanguageExt.Pipes.Pipe<IN, OUT, A>

- `public Await(System.Func<IN, LanguageExt.Pipes.Pipe<IN, OUT, A>> next)`
- `public readonly System.Func<IN, LanguageExt.Pipes.Pipe<IN, OUT, A>> Next`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, A> Interpret<RT>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<IN, OUT, B> Select<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Pipe<IN, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Pipe<IN, OUT, B>> f)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<RT, B>(System.Func<A, LanguageExt.Pipes.Pipe<RT, IN, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<IN, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Consumer<IN, B>> f)`
- `public LanguageExt.Pipes.Pipe<IN, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`

### Client (class)

- `public Client()`
- `public static LanguageExt.Pipes.Client<RT, REQ, RES, R> Pure<RT, REQ, RES, R>(R value)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Client<RT, REQ, RES, R> lift<RT, REQ, RES, R>(LanguageExt.Aff<R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Client<RT, REQ, RES, R> lift<RT, REQ, RES, R>(LanguageExt.Eff<R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Client<RT, REQ, RES, R> lift<RT, REQ, RES, R>(LanguageExt.Aff<RT, R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Client<RT, REQ, RES, R> lift<RT, REQ, RES, R>(LanguageExt.Eff<RT, R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Client<RT, REQ, RES, LanguageExt.Unit> release<RT, REQ, RES, R>(R dispose)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Client<RT, REQ, RES, RES> request<RT, REQ, RES>(REQ value)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Client<RT, REQ, RES, R> use<RT, REQ, RES, R>(LanguageExt.Aff<R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `where R : System.IDisposable`
- `public static LanguageExt.Pipes.Client<RT, REQ, RES, R> use<RT, REQ, RES, R>(LanguageExt.Eff<R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `where R : System.IDisposable`
- `public static LanguageExt.Pipes.Client<RT, REQ, RES, R> use<RT, REQ, RES, R>(LanguageExt.Aff<RT, R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `where R : System.IDisposable`
- `public static LanguageExt.Pipes.Client<RT, REQ, RES, R> use<RT, REQ, RES, R>(LanguageExt.Eff<RT, R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `where R : System.IDisposable`
- `public static LanguageExt.Pipes.Client<RT, REQ, RES, R> use<RT, REQ, RES, R>(LanguageExt.Aff<R> ma, System.Func<R, LanguageExt.Unit> dispose)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Client<RT, REQ, RES, R> use<RT, REQ, RES, R>(LanguageExt.Eff<R> ma, System.Func<R, LanguageExt.Unit> dispose)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Client<RT, REQ, RES, R> use<RT, REQ, RES, R>(LanguageExt.Aff<RT, R> ma, System.Func<R, LanguageExt.Unit> dispose)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Client<RT, REQ, RES, R> use<RT, REQ, RES, R>(LanguageExt.Eff<RT, R> ma, System.Func<R, LanguageExt.Unit> dispose)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

### Client`4<RT, REQ, RES, A> (class) : LanguageExt.Pipes.Proxy<RT, REQ, RES, LanguageExt.Unit, LanguageExt.Pipes.Void, A>

- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

- `public Client`4(LanguageExt.Pipes.Proxy<RT, REQ, RES, LanguageExt.Unit, LanguageExt.Pipes.Void, A> value)`
- `public readonly LanguageExt.Pipes.Proxy<RT, REQ, RES, LanguageExt.Unit, LanguageExt.Pipes.Void, A> Value`
- `public LanguageExt.Pipes.Proxy<RT, REQ, RES, LanguageExt.Unit, LanguageExt.Pipes.Void, S> Action<S>(LanguageExt.Pipes.Proxy<RT, REQ, RES, LanguageExt.Unit, LanguageExt.Pipes.Void, S> r)`
- `public LanguageExt.Pipes.Proxy<RT, REQ, RES, LanguageExt.Unit, LanguageExt.Pipes.Void, S> Bind<S>(System.Func<A, LanguageExt.Pipes.Proxy<RT, REQ, RES, LanguageExt.Unit, LanguageExt.Pipes.Void, S>> f)`
- `public LanguageExt.Pipes.Client<RT, REQ, RES, B> Bind<B>(System.Func<A, LanguageExt.Pipes.Client<RT, REQ, RES, B>> f)`
- `public System.Void Deconstruct(out LanguageExt.Pipes.Proxy<RT, REQ, RES, LanguageExt.Unit, LanguageExt.Pipes.Void, A>& value)`
- `public LanguageExt.Pipes.Proxy<RT, REQ, RES, C1, C, A> For<C1, C>(System.Func<LanguageExt.Pipes.Void, LanguageExt.Pipes.Proxy<RT, REQ, RES, C1, C, LanguageExt.Unit>> body)`
- `public LanguageExt.Pipes.Proxy<RT, REQ, RES, LanguageExt.Unit, LanguageExt.Pipes.Void, S> Map<S>(System.Func<A, S> f)`
- `public LanguageExt.Pipes.Proxy<RT, REQ, RES, LanguageExt.Unit, LanguageExt.Pipes.Void, A> Observe()`
- `public LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, LanguageExt.Unit, LanguageExt.Pipes.Void, A> PairEachRequestWithRespond<UOutA, AUInA>(System.Func<REQ, LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, REQ, RES, A>> lhs)`
- `public LanguageExt.Pipes.Proxy<RT, REQ, RES, DInC, DOutC, A> PairEachRespondWithRequest<DInC, DOutC>(System.Func<LanguageExt.Pipes.Void, LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, LanguageExt.Pipes.Void, DInC, DOutC, A>> rhs)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, RES, REQ, A> Reflect()`
- `public LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, LanguageExt.Unit, LanguageExt.Pipes.Void, A> ReplaceRequest<UOutA, AUInA>(System.Func<REQ, LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, LanguageExt.Unit, LanguageExt.Pipes.Void, RES>> lhs)`
- `public LanguageExt.Pipes.Proxy<RT, REQ, RES, DInC, DOutC, A> ReplaceRespond<DInC, DOutC>(System.Func<LanguageExt.Pipes.Void, LanguageExt.Pipes.Proxy<RT, REQ, RES, DInC, DOutC, LanguageExt.Unit>> rhs)`
- `public LanguageExt.Pipes.Client<RT, REQ, RES, B> Select<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Client<RT, REQ, RES, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Client<RT, REQ, RES, B>> f)`
- `public LanguageExt.Pipes.Client<RT, REQ, RES, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Release<B>> bind)`
- `public LanguageExt.Pipes.Client<RT, REQ, RES, C> SelectMany<B, C>(System.Func<A, LanguageExt.Pipes.Client<RT, REQ, RES, B>> f, System.Func<A, B, C> project)`
- `public LanguageExt.Pipes.Client<RT, REQ, RES, C> SelectMany<B, C>(System.Func<A, LanguageExt.Pipes.Release<B>> bind, System.Func<A, B, C> project)`
- `public LanguageExt.Pipes.Proxy<RT, REQ, RES, LanguageExt.Unit, LanguageExt.Pipes.Void, A> ToProxy()`
- `public static LanguageExt.Pipes.Client<RT, REQ, RES, A> op_BitwiseAnd(LanguageExt.Pipes.Client<RT, REQ, RES, A> lhs, LanguageExt.Pipes.Client<RT, REQ, RES, A> rhs)`
- `public static LanguageExt.Pipes.Effect<RT, A> op_BitwiseOr(System.Func<REQ, LanguageExt.Pipes.Server<RT, REQ, RES, A>> x, LanguageExt.Pipes.Client<RT, REQ, RES, A> y)`

### Consumer (class [static])

- `public static LanguageExt.Pipes.Consumer<RT, A, R> Pure<RT, A, R>(R value)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Consumer<RT, A, A> awaiting<RT, A>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Consumer<RT, A, R> lift<RT, A, R>(LanguageExt.Aff<R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Consumer<RT, A, R> lift<RT, A, R>(LanguageExt.Eff<R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Consumer<RT, A, R> lift<RT, A, R>(LanguageExt.Aff<RT, R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Consumer<RT, A, R> lift<RT, A, R>(LanguageExt.Eff<RT, R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Consumer<RT, A, LanguageExt.Unit> lift<RT, A>(LanguageExt.Aff<RT, LanguageExt.Unit> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Consumer<RT, A, LanguageExt.Unit> lift<RT, A>(LanguageExt.Eff<RT, LanguageExt.Unit> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Consumer<RT, A, R> mapM<RT, A, R>(System.Func<A, LanguageExt.Aff<RT, LanguageExt.Unit>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Consumer<RT, A, LanguageExt.Unit> mapM<RT, A>(System.Func<A, LanguageExt.Aff<RT, LanguageExt.Unit>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Consumer<RT, A, R> mapM<RT, A, R>(System.Func<A, LanguageExt.Eff<RT, LanguageExt.Unit>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Consumer<RT, A, LanguageExt.Unit> mapM<RT, A>(System.Func<A, LanguageExt.Eff<RT, LanguageExt.Unit>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Consumer<RT, A, R> mapM<RT, A, R>(System.Func<A, LanguageExt.Aff<LanguageExt.Unit>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Consumer<RT, A, LanguageExt.Unit> mapM<RT, A>(System.Func<A, LanguageExt.Aff<LanguageExt.Unit>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Consumer<RT, A, R> mapM<RT, A, R>(System.Func<A, LanguageExt.Eff<LanguageExt.Unit>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Consumer<RT, A, LanguageExt.Unit> mapM<RT, A>(System.Func<A, LanguageExt.Eff<LanguageExt.Unit>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Consumer<RT, IN, LanguageExt.Unit> release<RT, IN, R>(R dispose)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Consumer<RT, IN, R> use<RT, IN, R>(LanguageExt.Aff<R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `where R : System.IDisposable`
- `public static LanguageExt.Pipes.Consumer<RT, IN, R> use<RT, IN, R>(LanguageExt.Eff<R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `where R : System.IDisposable`
- `public static LanguageExt.Pipes.Consumer<RT, IN, R> use<RT, IN, R>(LanguageExt.Aff<RT, R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `where R : System.IDisposable`
- `public static LanguageExt.Pipes.Consumer<RT, IN, R> use<RT, IN, R>(LanguageExt.Eff<RT, R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `where R : System.IDisposable`
- `public static LanguageExt.Pipes.Consumer<RT, IN, R> use<RT, IN, R>(LanguageExt.Aff<R> ma, System.Func<R, LanguageExt.Unit> dispose)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Consumer<RT, IN, R> use<RT, IN, R>(LanguageExt.Eff<R> ma, System.Func<R, LanguageExt.Unit> dispose)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Consumer<RT, IN, R> use<RT, IN, R>(LanguageExt.Aff<RT, R> ma, System.Func<R, LanguageExt.Unit> dispose)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Consumer<RT, IN, R> use<RT, IN, R>(LanguageExt.Eff<RT, R> ma, System.Func<R, LanguageExt.Unit> dispose)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

### ConsumerLift`3<RT, IN, A> (class [abstract])

- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

- `public LanguageExt.Pipes.ConsumerLift<RT, IN, B> Bind<B>(System.Func<A, LanguageExt.Pipes.ConsumerLift<RT, IN, B>> f)`
- `public LanguageExt.Pipes.Consumer<RT, IN, B> Bind<B>(System.Func<A, LanguageExt.Pipes.Consumer<RT, IN, B>> f)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, B> Bind<OUT, B>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`
- `public LanguageExt.Pipes.Consumer<RT, IN, A> Interpret()`
- `public LanguageExt.Pipes.ConsumerLift<RT, IN, B> Map<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.ConsumerLift<RT, IN, B> Select<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.ConsumerLift<RT, IN, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.ConsumerLift<RT, IN, B>> f)`
- `public LanguageExt.Pipes.Consumer<RT, IN, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Consumer<RT, IN, B>> f)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<OUT, B>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`
- `public LanguageExt.Pipes.ConsumerLift<RT, IN, C> SelectMany<B, C>(System.Func<A, LanguageExt.Pipes.ConsumerLift<RT, IN, B>> f, System.Func<A, B, C> project)`
- `public LanguageExt.Pipes.Consumer<RT, IN, C> SelectMany<B, C>(System.Func<A, LanguageExt.Pipes.Consumer<RT, IN, B>> f, System.Func<A, B, C> project)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, C> SelectMany<OUT, B, C>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f, System.Func<A, B, C> project)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, A> ToPipe<OUT>()`
- `public static LanguageExt.Pipes.ConsumerLift<RT, IN, A> op_BitwiseAnd(LanguageExt.Pipes.ConsumerLift<RT, IN, A> lhs, LanguageExt.Pipes.ConsumerLift<RT, IN, A> rhs)`
- `public static LanguageExt.Pipes.ConsumerLift<RT, IN, A> op_Implicit(LanguageExt.Pipes.Pure<A> ma)`

### Consumer`2<IN, A> (class [abstract])

- `public LanguageExt.Pipes.Consumer<IN, B> Bind<B>(System.Func<A, LanguageExt.Pipes.Consumer<IN, B>> f)`
- `public LanguageExt.Pipes.ConsumerLift<RT, IN, B> Bind<RT, B>(System.Func<A, LanguageExt.Pipes.ConsumerLift<RT, IN, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Consumer<RT, IN, B> Bind<RT, B>(System.Func<A, LanguageExt.Pipes.Consumer<RT, IN, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<IN, OUT, B> Bind<OUT, B>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`
- `public LanguageExt.Pipes.Consumer<RT, IN, A> Interpret<RT>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Consumer<IN, B> Map<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Consumer<IN, B> Select<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Consumer<IN, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Consumer<IN, B>> f)`
- `public LanguageExt.Pipes.ConsumerLift<RT, IN, B> SelectMany<RT, B>(System.Func<A, LanguageExt.Pipes.ConsumerLift<RT, IN, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Consumer<RT, IN, B> SelectMany<RT, B>(System.Func<A, LanguageExt.Pipes.Consumer<RT, IN, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<IN, OUT, B> SelectMany<OUT, B>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<RT, OUT, B>(System.Func<A, LanguageExt.Pipes.ProducerLift<RT, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Consumer<IN, C> SelectMany<B, C>(System.Func<A, LanguageExt.Pipes.Consumer<IN, B>> f, System.Func<A, B, C> project)`
- `public LanguageExt.Pipes.ConsumerLift<RT, IN, C> SelectMany<RT, B, C>(System.Func<A, LanguageExt.Pipes.ConsumerLift<RT, IN, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Consumer<RT, IN, C> SelectMany<RT, B, C>(System.Func<A, LanguageExt.Pipes.Consumer<RT, IN, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<IN, OUT, C> SelectMany<OUT, B, C>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f, System.Func<A, B, C> project)`
- `public LanguageExt.Pipes.ConsumerLift<RT, IN, A> ToConsumerLift<RT>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<IN, OUT, A> ToPipe<OUT>()`
- `public static LanguageExt.Pipes.Consumer<IN, A> op_BitwiseAnd(LanguageExt.Pipes.Consumer<IN, A> lhs, LanguageExt.Pipes.Consumer<IN, A> rhs)`
- `public static LanguageExt.Pipes.Consumer<IN, A> op_Implicit(LanguageExt.Pipes.Pure<A> ma)`

### Consumer`3<RT, IN, A> (class) : LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, IN, LanguageExt.Unit, LanguageExt.Pipes.Void, A>

- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

- `public Consumer`3(LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, IN, LanguageExt.Unit, LanguageExt.Pipes.Void, A> value)`
- `public readonly LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, IN, LanguageExt.Unit, LanguageExt.Pipes.Void, A> Value`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, IN, LanguageExt.Unit, LanguageExt.Pipes.Void, S> Action<S>(LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, IN, LanguageExt.Unit, LanguageExt.Pipes.Void, S> r)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, IN, LanguageExt.Unit, LanguageExt.Pipes.Void, S> Bind<S>(System.Func<A, LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, IN, LanguageExt.Unit, LanguageExt.Pipes.Void, S>> f)`
- `public LanguageExt.Pipes.Consumer<RT, IN, B> Bind<B>(System.Func<A, LanguageExt.Pipes.Consumer<RT, IN, B>> f)`
- `public System.Void Deconstruct(out LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, IN, LanguageExt.Unit, LanguageExt.Pipes.Void, A>& value)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, IN, C1, C, A> For<C1, C>(System.Func<LanguageExt.Pipes.Void, LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, IN, C1, C, LanguageExt.Unit>> body)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, IN, LanguageExt.Unit, LanguageExt.Pipes.Void, S> Map<S>(System.Func<A, S> f)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, IN, LanguageExt.Unit, LanguageExt.Pipes.Void, A> Observe()`
- `public LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, LanguageExt.Unit, LanguageExt.Pipes.Void, A> PairEachRequestWithRespond<UOutA, AUInA>(System.Func<LanguageExt.Unit, LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, LanguageExt.Unit, IN, A>> lhs)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, IN, DInC, DOutC, A> PairEachRespondWithRequest<DInC, DOutC>(System.Func<LanguageExt.Pipes.Void, LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, LanguageExt.Pipes.Void, DInC, DOutC, A>> rhs)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, IN, LanguageExt.Unit, A> Reflect()`
- `public LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, LanguageExt.Unit, LanguageExt.Pipes.Void, A> ReplaceRequest<UOutA, AUInA>(System.Func<LanguageExt.Unit, LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, LanguageExt.Unit, LanguageExt.Pipes.Void, IN>> lhs)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, IN, DInC, DOutC, A> ReplaceRespond<DInC, DOutC>(System.Func<LanguageExt.Pipes.Void, LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, IN, DInC, DOutC, LanguageExt.Unit>> rhs)`
- `public LanguageExt.Pipes.Consumer<RT, IN, B> Select<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Consumer<RT, IN, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Consumer<RT, IN, B>> f)`
- `public LanguageExt.Pipes.Consumer<RT, IN, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Release<B>> bind)`
- `public LanguageExt.Pipes.Consumer<RT, IN, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Consumer<IN, B>> bind)`
- `public LanguageExt.Pipes.Consumer<RT, IN, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.ConsumerLift<RT, IN, B>> bind)`
- `public LanguageExt.Pipes.Consumer<RT, IN, C> SelectMany<B, C>(System.Func<A, LanguageExt.Pipes.Consumer<RT, IN, B>> f, System.Func<A, B, C> project)`
- `public LanguageExt.Pipes.Consumer<RT, IN, C> SelectMany<B, C>(System.Func<A, LanguageExt.Pipes.Release<B>> bind, System.Func<A, B, C> project)`
- `public LanguageExt.Pipes.Consumer<RT, IN, C> SelectMany<B, C>(System.Func<A, LanguageExt.Pipes.Consumer<IN, B>> bind, System.Func<A, B, C> project)`
- `public LanguageExt.Pipes.Consumer<RT, IN, C> SelectMany<B, C>(System.Func<A, LanguageExt.Pipes.ConsumerLift<RT, IN, B>> bind, System.Func<A, B, C> project)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, IN, LanguageExt.Unit, LanguageExt.Pipes.Void, A> ToProxy()`
- `public static LanguageExt.Pipes.Consumer<RT, IN, A> op_BitwiseAnd(LanguageExt.Pipes.Consumer<RT, IN, A> lhs, LanguageExt.Pipes.Consumer<RT, IN, A> rhs)`
- `public static LanguageExt.Pipes.Effect<RT, A> op_BitwiseOr(IN p1, LanguageExt.Pipes.Consumer<RT, IN, A> p2)`
- `public static LanguageExt.Pipes.Consumer<RT, IN, A> op_Implicit(LanguageExt.Pipes.Consumer<IN, A> c)`
- `public static LanguageExt.Pipes.Consumer<RT, IN, A> op_Implicit(LanguageExt.Pipes.ConsumerLift<RT, IN, A> c)`
- `public static LanguageExt.Pipes.Consumer<RT, IN, A> op_Implicit(LanguageExt.Pipes.Pure<A> p)`

### Do<OUT, A> (class) : LanguageExt.Pipes.Enumerate<OUT, A>

- `public Do(System.Collections.Generic.IEnumerable<OUT> values, System.Func<LanguageExt.Unit, LanguageExt.Pipes.Enumerate<OUT, A>> next)`
- `public Do(System.Collections.Generic.IAsyncEnumerable<OUT> values, System.Func<LanguageExt.Unit, LanguageExt.Pipes.Enumerate<OUT, A>> next)`
- `public Do(System.IObservable<OUT> values, System.Func<LanguageExt.Unit, LanguageExt.Pipes.Enumerate<OUT, A>> next)`
- `public readonly System.Func<LanguageExt.Unit, LanguageExt.Pipes.Enumerate<OUT, A>> Next`
- `public LanguageExt.Pipes.Producer<RT, OUT, A> Interpret<RT>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, A> Interpret<RT, IN>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Enumerate<OUT, B> Select<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Enumerate<OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Enumerate<OUT, B>> f)`
- `public LanguageExt.Pipes.Enumerate<OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Release<B>> f)`
- `public LanguageExt.Pipes.Producer<OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`
- `public LanguageExt.Pipes.Producer<RT, OUT, B> SelectMany<RT, B>(System.Func<A, LanguageExt.Pipes.Producer<RT, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<IN, OUT, B> SelectMany<IN, B>(System.Func<A, LanguageExt.Pipes.Pipe<IN, OUT, B>> f)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<RT, IN, B>(System.Func<A, LanguageExt.Pipes.Pipe<RT, IN, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

### Do`1<RT, A, X> (class) : LanguageExt.Pipes.Lift<RT, A>

- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

- `public Do`1(LanguageExt.Aff<RT, X> value, System.Func<X, LanguageExt.Pipes.Lift<RT, A>> next)`
- `public readonly LanguageExt.Aff<RT, X> Effect`
- `public readonly System.Func<X, LanguageExt.Pipes.Lift<RT, A>> Next`
- `public LanguageExt.Pipes.Lift<RT, B> Bind<B>(System.Func<A, LanguageExt.Pipes.Lift<RT, B>> f)`
- `public LanguageExt.Pipes.Lift<RT, B> Map<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Consumer<RT, IN, A> ToConsumer<IN>()`
- `public LanguageExt.Pipes.ConsumerLift<RT, IN, A> ToConsumerLift<IN>()`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, A> ToPipe<IN, OUT>()`
- `public LanguageExt.Pipes.Producer<RT, OUT, A> ToProducer<OUT>()`
- `public LanguageExt.Pipes.ProducerLift<RT, OUT, A> ToProducerLift<OUT>()`

### Do`1<A, X> (class) : LanguageExt.Pipes.Release<A>

- `public Do`1(X value, System.Func<LanguageExt.Unit, LanguageExt.Pipes.Release<A>> next)`
- `public LanguageExt.Pipes.Client<RT, REQ, RES, A> InterpretClient<RT, REQ, RES>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Consumer<RT, IN, A> InterpretConsumer<RT, IN>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, A> InterpretPipe<RT, IN, OUT>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Producer<RT, OUT, A> InterpretProducer<RT, OUT>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Server<RT, REQ, RES, A> InterpretServer<RT, REQ, RES>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Release<B> Select<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Release<B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Release<B>> f)`
- `public LanguageExt.Pipes.Enumerate<OUT, B> SelectMany<OUT, B>(System.Func<A, LanguageExt.Pipes.Enumerate<OUT, B>> f)`
- `public LanguageExt.Pipes.Consumer<IN, B> SelectMany<IN, B>(System.Func<A, LanguageExt.Pipes.Consumer<IN, B>> f)`
- `public LanguageExt.Pipes.ConsumerLift<RT, IN, B> SelectMany<RT, IN, B>(System.Func<A, LanguageExt.Pipes.ConsumerLift<RT, IN, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Consumer<RT, IN, B> SelectMany<RT, IN, B>(System.Func<A, LanguageExt.Pipes.Consumer<RT, IN, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Producer<OUT, B> SelectMany<OUT, B>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`
- `public LanguageExt.Pipes.Producer<RT, OUT, B> SelectMany<RT, OUT, B>(System.Func<A, LanguageExt.Pipes.Producer<RT, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<IN, OUT, B> SelectMany<IN, OUT, B>(System.Func<A, LanguageExt.Pipes.Pipe<IN, OUT, B>> f)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<RT, IN, OUT, B>(System.Func<A, LanguageExt.Pipes.Pipe<RT, IN, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Consumer<IN, A> ToConsumer<IN>()`
- `public LanguageExt.Pipes.ConsumerLift<RT, IN, A> ToConsumerLift<RT, IN>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Enumerate<OUT, A> ToEnumerate<OUT>()`
- `public LanguageExt.Pipes.Pipe<IN, OUT, A> ToPipe<IN, OUT>()`
- `public LanguageExt.Pipes.Producer<OUT, A> ToProducer<OUT>()`

### Effect (class [static])

- `[ext] public static LanguageExt.Aff<RT, R> RunEffect<RT, R>(this LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, LanguageExt.Unit, LanguageExt.Pipes.Void, R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Aff<RT, LanguageExt.Unit> RunEffectUnit<RT>(this LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, LanguageExt.Unit, LanguageExt.Pipes.Void, LanguageExt.Unit> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Effect<RT, R> lift<RT, R>(LanguageExt.Aff<R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Effect<RT, R> lift<RT, R>(LanguageExt.Eff<R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Effect<RT, R> lift<RT, R>(LanguageExt.Aff<RT, R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Effect<RT, R> lift<RT, R>(LanguageExt.Eff<RT, R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

### Effect`2<RT, A> (class) : LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, LanguageExt.Unit, LanguageExt.Pipes.Void, A>

- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

- `public Effect`2(LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, LanguageExt.Unit, LanguageExt.Pipes.Void, A> value)`
- `public readonly LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, LanguageExt.Unit, LanguageExt.Pipes.Void, A> Value`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, LanguageExt.Unit, LanguageExt.Pipes.Void, S> Action<S>(LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, LanguageExt.Unit, LanguageExt.Pipes.Void, S> r)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, LanguageExt.Unit, LanguageExt.Pipes.Void, S> Bind<S>(System.Func<A, LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, LanguageExt.Unit, LanguageExt.Pipes.Void, S>> f)`
- `public System.Void Deconstruct(out LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, LanguageExt.Unit, LanguageExt.Pipes.Void, A>& value)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, C1, C, A> For<C1, C>(System.Func<LanguageExt.Pipes.Void, LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, C1, C, LanguageExt.Unit>> body)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, LanguageExt.Unit, LanguageExt.Pipes.Void, S> Map<S>(System.Func<A, S> f)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, LanguageExt.Unit, LanguageExt.Pipes.Void, A> Observe()`
- `public LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, LanguageExt.Unit, LanguageExt.Pipes.Void, A> PairEachRequestWithRespond<UOutA, AUInA>(System.Func<LanguageExt.Pipes.Void, LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, LanguageExt.Pipes.Void, LanguageExt.Unit, A>> lhs)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, DInC, DOutC, A> PairEachRespondWithRequest<DInC, DOutC>(System.Func<LanguageExt.Pipes.Void, LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, LanguageExt.Pipes.Void, DInC, DOutC, A>> rhs)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, LanguageExt.Unit, LanguageExt.Pipes.Void, A> Reflect()`
- `public LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, LanguageExt.Unit, LanguageExt.Pipes.Void, A> ReplaceRequest<UOutA, AUInA>(System.Func<LanguageExt.Pipes.Void, LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, LanguageExt.Unit, LanguageExt.Pipes.Void, LanguageExt.Unit>> lhs)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, DInC, DOutC, A> ReplaceRespond<DInC, DOutC>(System.Func<LanguageExt.Pipes.Void, LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, DInC, DOutC, LanguageExt.Unit>> rhs)`
- `public LanguageExt.Aff<RT, C> SelectMany<B, C>(System.Func<A, LanguageExt.Aff<RT, B>> bind, System.Func<A, B, C> project)`
- `public LanguageExt.Aff<RT, C> SelectMany<B, C>(System.Func<A, LanguageExt.Aff<B>> bind, System.Func<A, B, C> project)`
- `public LanguageExt.Aff<RT, C> SelectMany<B, C>(System.Func<A, LanguageExt.Eff<RT, B>> bind, System.Func<A, B, C> project)`
- `public LanguageExt.Aff<RT, C> SelectMany<B, C>(System.Func<A, LanguageExt.Eff<B>> bind, System.Func<A, B, C> project)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, LanguageExt.Unit, LanguageExt.Pipes.Void, A> ToProxy()`
- `public static LanguageExt.Pipes.Effect<RT, A> op_BitwiseAnd(LanguageExt.Pipes.Effect<RT, A> lhs, LanguageExt.Pipes.Effect<RT, A> rhs)`

### Enumerate<IN, OUT, A> (class) : LanguageExt.Pipes.Pipe<IN, OUT, A>

- `public readonly System.Func<LanguageExt.Unit, LanguageExt.Pipes.Pipe<IN, OUT, A>> Next`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, A> Interpret<RT>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<IN, OUT, B> Select<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Pipe<IN, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Pipe<IN, OUT, B>> f)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<RT, B>(System.Func<A, LanguageExt.Pipes.Pipe<RT, IN, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<IN, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Consumer<IN, B>> f)`
- `public LanguageExt.Pipes.Pipe<IN, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`

### Enumerate<OUT, A> (class) : LanguageExt.Pipes.Producer<OUT, A>

- `public Enumerate(System.Collections.Generic.IEnumerable<OUT> values, System.Func<LanguageExt.Unit, LanguageExt.Pipes.Producer<OUT, A>> next)`
- `public Enumerate(System.Collections.Generic.IAsyncEnumerable<OUT> values, System.Func<LanguageExt.Unit, LanguageExt.Pipes.Producer<OUT, A>> next)`
- `public Enumerate(System.IObservable<OUT> values, System.Func<LanguageExt.Unit, LanguageExt.Pipes.Producer<OUT, A>> next)`
- `public readonly System.Func<LanguageExt.Unit, LanguageExt.Pipes.Producer<OUT, A>> Next`
- `public LanguageExt.Pipes.Producer<RT, OUT, A> Interpret<RT>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Producer<OUT, B> Select<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Producer<OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`
- `public LanguageExt.Pipes.Producer<RT, OUT, B> SelectMany<RT, B>(System.Func<A, LanguageExt.Pipes.Producer<RT, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<IN, OUT, A> ToPipe<IN>()`
- `public LanguageExt.Pipes.ProducerLift<RT, OUT, A> ToProducerLift<RT>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

### Enumerate<RT, OUT, A> (class) : LanguageExt.Pipes.ProducerLift<RT, OUT, A>

- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

- `public Enumerate(System.Collections.Generic.IEnumerable<OUT> values, System.Func<LanguageExt.Unit, LanguageExt.Pipes.ProducerLift<RT, OUT, A>> next)`
- `public Enumerate(System.Collections.Generic.IAsyncEnumerable<OUT> values, System.Func<LanguageExt.Unit, LanguageExt.Pipes.ProducerLift<RT, OUT, A>> next)`
- `public Enumerate(System.IObservable<OUT> values, System.Func<LanguageExt.Unit, LanguageExt.Pipes.ProducerLift<RT, OUT, A>> next)`
- `public readonly System.Func<LanguageExt.Unit, LanguageExt.Pipes.ProducerLift<RT, OUT, A>> Next`
- `public LanguageExt.Pipes.Producer<RT, OUT, A> Interpret()`
- `public LanguageExt.Pipes.ProducerLift<RT, OUT, B> Select<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.ProducerLift<RT, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`
- `public LanguageExt.Pipes.ProducerLift<RT, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.ProducerLift<RT, OUT, B>> f)`
- `public LanguageExt.Pipes.Producer<RT, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Producer<RT, OUT, B>> f)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, A> ToPipe<IN>()`

### Enumerate`2<OUT, A> (class [abstract])

- `public LanguageExt.Pipes.Enumerate<OUT, B> Bind<B>(System.Func<A, LanguageExt.Pipes.Enumerate<OUT, B>> f)`
- `public LanguageExt.Pipes.Enumerate<OUT, B> Bind<B>(System.Func<A, LanguageExt.Pipes.Release<B>> f)`
- `public LanguageExt.Pipes.Producer<OUT, B> Bind<B>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`
- `public LanguageExt.Pipes.Producer<RT, OUT, B> Bind<RT, B>(System.Func<A, LanguageExt.Pipes.Producer<RT, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, B> Bind<RT, IN, B>(System.Func<A, LanguageExt.Pipes.Pipe<RT, IN, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Producer<RT, OUT, A> Interpret<RT>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, A> Interpret<RT, IN>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Enumerate<OUT, B> Map<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Enumerate<OUT, B> Select<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Enumerate<OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Enumerate<OUT, B>> f)`
- `public LanguageExt.Pipes.Enumerate<OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Release<B>> f)`
- `public LanguageExt.Pipes.Producer<OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`
- `public LanguageExt.Pipes.Producer<RT, OUT, B> SelectMany<RT, B>(System.Func<A, LanguageExt.Pipes.Producer<RT, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<IN, OUT, B> SelectMany<IN, B>(System.Func<A, LanguageExt.Pipes.Pipe<IN, OUT, B>> f)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<RT, IN, B>(System.Func<A, LanguageExt.Pipes.Pipe<RT, IN, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Enumerate<OUT, C> SelectMany<B, C>(System.Func<A, LanguageExt.Pipes.Enumerate<OUT, B>> f, System.Func<A, B, C> project)`
- `public LanguageExt.Pipes.Enumerate<OUT, C> SelectMany<B, C>(System.Func<A, LanguageExt.Pipes.Release<B>> f, System.Func<A, B, C> project)`
- `public LanguageExt.Pipes.Producer<OUT, C> SelectMany<B, C>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f, System.Func<A, B, C> project)`
- `public LanguageExt.Pipes.Producer<RT, OUT, C> SelectMany<RT, B, C>(System.Func<A, LanguageExt.Pipes.Producer<RT, OUT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<IN, OUT, C> SelectMany<IN, B, C>(System.Func<A, LanguageExt.Pipes.Pipe<IN, OUT, B>> f, System.Func<A, B, C> project)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, C> SelectMany<RT, IN, B, C>(System.Func<A, LanguageExt.Pipes.Pipe<RT, IN, OUT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Enumerate<OUT, A> op_Implicit(LanguageExt.Pipes.Pure<A> ma)`

### Lift (class [static])

- `public static LanguageExt.Pipes.Lift<RT, A> Aff<RT, A>(LanguageExt.Aff<RT, A> value)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Lift<RT, A> Eff<RT, A>(LanguageExt.Eff<RT, A> value)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Lift<RT, A> Pure<RT, A>(A value)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Lift<RT, B> Select<RT, A, B>(this LanguageExt.Pipes.Lift<RT, A> ma, System.Func<A, B> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Lift<RT, B> SelectMany<RT, A, B>(this LanguageExt.Pipes.Lift<RT, A> ma, System.Func<A, LanguageExt.Pipes.Lift<RT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Lift<RT, B> SelectMany<RT, A, B>(this LanguageExt.Pipes.Lift<RT, A> ma, System.Func<A, LanguageExt.Aff<RT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Lift<RT, B> SelectMany<RT, A, B>(this LanguageExt.Pipes.Lift<RT, A> ma, System.Func<A, LanguageExt.Eff<RT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Lift<RT, B> SelectMany<RT, A, B>(this LanguageExt.Pipes.Lift<RT, A> ma, System.Func<A, LanguageExt.Aff<B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Lift<RT, B> SelectMany<RT, A, B>(this LanguageExt.Pipes.Lift<RT, A> ma, System.Func<A, LanguageExt.Eff<B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Consumer<RT, IN, B> SelectMany<RT, IN, A, B>(this LanguageExt.Pipes.Lift<RT, A> ma, System.Func<A, LanguageExt.Pipes.Consumer<IN, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.ProducerLift<RT, OUT, B> SelectMany<RT, OUT, A, B>(this LanguageExt.Pipes.Lift<RT, A> ma, System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.ProducerLift<RT, OUT, B> SelectMany<RT, OUT, A, B>(this LanguageExt.Pipes.Lift<RT, A> ma, System.Func<A, LanguageExt.Pipes.ProducerLift<RT, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<RT, IN, OUT, A, B>(this LanguageExt.Pipes.Lift<RT, A> ma, System.Func<A, LanguageExt.Pipes.Pipe<IN, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.ConsumerLift<RT, IN, B> SelectMany<RT, IN, A, B>(this LanguageExt.Pipes.Consumer<IN, A> ma, System.Func<A, LanguageExt.Pipes.Lift<RT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.ProducerLift<RT, OUT, B> SelectMany<RT, OUT, A, B>(this LanguageExt.Pipes.Producer<OUT, A> ma, System.Func<A, LanguageExt.Pipes.Lift<RT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<RT, IN, OUT, A, B>(this LanguageExt.Pipes.Pipe<IN, OUT, A> ma, System.Func<A, LanguageExt.Pipes.Lift<RT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Consumer<RT, IN, B> SelectMany<RT, IN, A, B>(this LanguageExt.Pipes.Lift<RT, A> ma, System.Func<A, LanguageExt.Pipes.Consumer<RT, IN, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Producer<RT, OUT, B> SelectMany<RT, OUT, A, B>(this LanguageExt.Pipes.Lift<RT, A> ma, System.Func<A, LanguageExt.Pipes.Producer<RT, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<RT, IN, OUT, A, B>(this LanguageExt.Pipes.Lift<RT, A> ma, System.Func<A, LanguageExt.Pipes.Pipe<RT, IN, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Consumer<RT, IN, B> SelectMany<RT, IN, A, B>(this LanguageExt.Pipes.Consumer<RT, IN, A> ma, System.Func<A, LanguageExt.Pipes.Lift<RT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Producer<RT, OUT, B> SelectMany<RT, OUT, A, B>(this LanguageExt.Pipes.Producer<RT, OUT, A> ma, System.Func<A, LanguageExt.Pipes.Lift<RT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<RT, IN, OUT, A, B>(this LanguageExt.Pipes.Pipe<RT, IN, OUT, A> ma, System.Func<A, LanguageExt.Pipes.Lift<RT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Lift<RT, C> SelectMany<RT, A, B, C>(this LanguageExt.Pipes.Lift<RT, A> ma, System.Func<A, LanguageExt.Pipes.Lift<RT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Lift<RT, C> SelectMany<RT, A, B, C>(this LanguageExt.Pipes.Lift<RT, A> ma, System.Func<A, LanguageExt.Aff<RT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Lift<RT, C> SelectMany<RT, A, B, C>(this LanguageExt.Pipes.Lift<RT, A> ma, System.Func<A, LanguageExt.Eff<RT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Lift<RT, C> SelectMany<RT, A, B, C>(this LanguageExt.Pipes.Lift<RT, A> ma, System.Func<A, LanguageExt.Aff<B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Lift<RT, C> SelectMany<RT, A, B, C>(this LanguageExt.Pipes.Lift<RT, A> ma, System.Func<A, LanguageExt.Eff<B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Consumer<RT, IN, C> SelectMany<RT, IN, A, B, C>(this LanguageExt.Pipes.Lift<RT, A> ma, System.Func<A, LanguageExt.Pipes.Consumer<IN, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.ProducerLift<RT, OUT, C> SelectMany<RT, OUT, A, B, C>(this LanguageExt.Pipes.Lift<RT, A> ma, System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.ProducerLift<RT, OUT, C> SelectMany<RT, OUT, A, B, C>(this LanguageExt.Pipes.Lift<RT, A> ma, System.Func<A, LanguageExt.Pipes.ProducerLift<RT, OUT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Pipe<RT, IN, OUT, C> SelectMany<RT, IN, OUT, A, B, C>(this LanguageExt.Pipes.Lift<RT, A> ma, System.Func<A, LanguageExt.Pipes.Pipe<IN, OUT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.ConsumerLift<RT, IN, C> SelectMany<RT, IN, A, B, C>(this LanguageExt.Pipes.Consumer<IN, A> ma, System.Func<A, LanguageExt.Pipes.Lift<RT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.ProducerLift<RT, OUT, C> SelectMany<RT, OUT, A, B, C>(this LanguageExt.Pipes.Producer<OUT, A> ma, System.Func<A, LanguageExt.Pipes.Lift<RT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Pipe<RT, IN, OUT, C> SelectMany<RT, IN, OUT, A, B, C>(this LanguageExt.Pipes.Pipe<IN, OUT, A> ma, System.Func<A, LanguageExt.Pipes.Lift<RT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Consumer<RT, IN, C> SelectMany<RT, IN, A, B, C>(this LanguageExt.Pipes.Lift<RT, A> ma, System.Func<A, LanguageExt.Pipes.Consumer<RT, IN, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Producer<RT, OUT, C> SelectMany<RT, OUT, A, B, C>(this LanguageExt.Pipes.Lift<RT, A> ma, System.Func<A, LanguageExt.Pipes.Producer<RT, OUT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Pipe<RT, IN, OUT, C> SelectMany<RT, IN, OUT, A, B, C>(this LanguageExt.Pipes.Lift<RT, A> ma, System.Func<A, LanguageExt.Pipes.Pipe<RT, IN, OUT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Consumer<RT, IN, C> SelectMany<RT, IN, A, B, C>(this LanguageExt.Pipes.Consumer<RT, IN, A> ma, System.Func<A, LanguageExt.Pipes.Lift<RT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Producer<RT, OUT, C> SelectMany<RT, OUT, A, B, C>(this LanguageExt.Pipes.Producer<RT, OUT, A> ma, System.Func<A, LanguageExt.Pipes.Lift<RT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Pipe<RT, IN, OUT, C> SelectMany<RT, IN, OUT, A, B, C>(this LanguageExt.Pipes.Pipe<RT, IN, OUT, A> ma, System.Func<A, LanguageExt.Pipes.Lift<RT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

### Lift`1<RT, IN, A, X> (class) : LanguageExt.Pipes.ConsumerLift<RT, IN, A>

- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

- `public Lift`1(LanguageExt.Aff<RT, X> value, System.Func<X, LanguageExt.Pipes.ConsumerLift<RT, IN, A>> next)`
- `public readonly System.Func<X, LanguageExt.Pipes.ConsumerLift<RT, IN, A>> Next`
- `public readonly LanguageExt.Aff<RT, X> Value`
- `public LanguageExt.Pipes.Consumer<RT, IN, A> Interpret()`
- `public LanguageExt.Pipes.ConsumerLift<RT, IN, B> Select<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.ConsumerLift<RT, IN, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.ConsumerLift<RT, IN, B>> f)`
- `public LanguageExt.Pipes.Consumer<RT, IN, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Consumer<RT, IN, B>> f)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<OUT, B>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, A> ToPipe<OUT>()`

### Lift`1<RT, OUT, A, X> (class) : LanguageExt.Pipes.ProducerLift<RT, OUT, A>

- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

- `public Lift`1(LanguageExt.Aff<RT, X> value, System.Func<X, LanguageExt.Pipes.ProducerLift<RT, OUT, A>> next)`
- `public readonly System.Func<X, LanguageExt.Pipes.ProducerLift<RT, OUT, A>> Next`
- `public readonly LanguageExt.Aff<RT, X> Value`
- `public LanguageExt.Pipes.Producer<RT, OUT, A> Interpret()`
- `public LanguageExt.Pipes.ProducerLift<RT, OUT, B> Select<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.ProducerLift<RT, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`
- `public LanguageExt.Pipes.ProducerLift<RT, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.ProducerLift<RT, OUT, B>> f)`
- `public LanguageExt.Pipes.Producer<RT, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Producer<RT, OUT, B>> f)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, A> ToPipe<IN>()`

### Lift`2<RT, A> (class [abstract])

- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

- `public LanguageExt.Pipes.Lift<RT, B> Bind<B>(System.Func<A, LanguageExt.Pipes.Lift<RT, B>> f)`
- `public LanguageExt.Pipes.Lift<RT, B> Map<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Consumer<RT, IN, A> ToConsumer<IN>()`
- `public LanguageExt.Pipes.ConsumerLift<RT, IN, A> ToConsumerLift<IN>()`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, A> ToPipe<IN, OUT>()`
- `public LanguageExt.Pipes.Producer<RT, OUT, A> ToProducer<OUT>()`
- `public LanguageExt.Pipes.ProducerLift<RT, OUT, A> ToProducerLift<OUT>()`
- `public static LanguageExt.Pipes.Lift<RT, A> op_Implicit(LanguageExt.Pipes.Pure<A> ma)`

### M`6<RT, UOut, UIn, DIn, DOut, A> (class) : LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, A>

- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

- `public M`6(LanguageExt.Aff<RT, LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, A>> value)`
- `public readonly LanguageExt.Aff<RT, LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, A>> Value`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, S> Action<S>(LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, S> r)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, S> Bind<S>(System.Func<A, LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, S>> f)`
- `public System.Void Deconstruct(out LanguageExt.Aff<RT, LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, A>>& value)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, C1, C, A> For<C1, C>(System.Func<DOut, LanguageExt.Pipes.Proxy<RT, UOut, UIn, C1, C, DIn>> body)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, S> Map<S>(System.Func<A, S> f)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, A> Observe()`
- `public LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, DIn, DOut, A> PairEachRequestWithRespond<UOutA, AUInA>(System.Func<UOut, LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, UOut, UIn, A>> fb1)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DInC, DOutC, A> PairEachRespondWithRequest<DInC, DOutC>(System.Func<DOut, LanguageExt.Pipes.Proxy<RT, DIn, DOut, DInC, DOutC, A>> rhs)`
- `public LanguageExt.Pipes.Proxy<RT, DOut, DIn, UIn, UOut, A> Reflect()`
- `public LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, DIn, DOut, A> ReplaceRequest<UOutA, AUInA>(System.Func<UOut, LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, DIn, DOut, UIn>> lhs)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DInC, DOutC, A> ReplaceRespond<DInC, DOutC>(System.Func<DOut, LanguageExt.Pipes.Proxy<RT, UOut, UIn, DInC, DOutC, DIn>> rhs)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, A> ToProxy()`

### Pipe (class [static])

- `public static LanguageExt.Pipes.Pipe<RT, A, B, R> Pure<RT, A, B, R>(R value)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Pipe<RT, A, Y, A> awaiting<RT, A, Y>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Pipe<RT, A, A, LanguageExt.Unit> filter<RT, A>(System.Func<A, System.Boolean> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Pipe<RT, IN, OUT, LanguageExt.Unit> foldUntil<RT, IN, OUT>(OUT Initial, System.Func<OUT, IN, OUT> Fold, System.Func<OUT, System.Boolean> UntilState)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Pipe<RT, IN, OUT, LanguageExt.Unit> foldUntil<RT, IN, OUT>(OUT Initial, System.Func<OUT, IN, OUT> Fold, System.Func<IN, System.Boolean> UntilValue)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Pipe<RT, IN, OUT, LanguageExt.Unit> foldWhile<RT, IN, OUT>(OUT Initial, System.Func<OUT, IN, OUT> Fold, System.Func<OUT, System.Boolean> WhileState)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Pipe<RT, IN, OUT, LanguageExt.Unit> foldWhile<RT, IN, OUT>(OUT Initial, System.Func<OUT, IN, OUT> Fold, System.Func<IN, System.Boolean> WhileValue)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Pipe<RT, A, B, R> lift<RT, A, B, R>(LanguageExt.Aff<R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Pipe<RT, A, B, R> lift<RT, A, B, R>(LanguageExt.Eff<R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Pipe<RT, A, B, R> lift<RT, A, B, R>(LanguageExt.Aff<RT, R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Pipe<RT, A, B, R> lift<RT, A, B, R>(LanguageExt.Eff<RT, R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Pipe<RT, A, B, R> map<RT, A, B, R>(System.Func<A, B> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Pipe<RT, A, B, LanguageExt.Unit> map<RT, A, B>(System.Func<A, B> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Pipe<A, B, LanguageExt.Unit> map<A, B>(System.Func<A, B> f)`
- `public static LanguageExt.Pipes.Pipe<RT, A, B, R> mapM<RT, A, B, R>(System.Func<A, LanguageExt.Aff<RT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Pipe<RT, A, A, LanguageExt.Unit> mapM<RT, A>(System.Func<A, LanguageExt.Aff<RT, A>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Pipe<RT, A, B, R> mapM<RT, A, B, R>(System.Func<A, LanguageExt.Aff<B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Pipe<RT, A, B, R> mapM<RT, A, B, R>(System.Func<A, LanguageExt.Eff<RT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Pipe<RT, A, A, LanguageExt.Unit> mapM<RT, A>(System.Func<A, LanguageExt.Eff<RT, A>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Pipe<RT, A, B, R> mapM<RT, A, B, R>(System.Func<A, LanguageExt.Eff<B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Pipe<RT, IN, OUT, LanguageExt.Unit> release<RT, IN, OUT, R>(R dispose)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Pipe<RT, IN, OUT, LanguageExt.Unit> scan<RT, IN, OUT, S>(System.Func<S, IN, S> Step, S Begin, System.Func<S, OUT> Done)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Pipe<RT, IN, OUT, R> use<RT, IN, OUT, R>(LanguageExt.Aff<R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `where R : System.IDisposable`
- `public static LanguageExt.Pipes.Pipe<RT, IN, OUT, R> use<RT, IN, OUT, R>(LanguageExt.Eff<R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `where R : System.IDisposable`
- `public static LanguageExt.Pipes.Pipe<RT, IN, OUT, R> use<RT, IN, OUT, R>(LanguageExt.Aff<RT, R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `where R : System.IDisposable`
- `public static LanguageExt.Pipes.Pipe<RT, IN, OUT, R> use<RT, IN, OUT, R>(LanguageExt.Eff<RT, R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `where R : System.IDisposable`
- `public static LanguageExt.Pipes.Pipe<RT, IN, OUT, R> use<RT, IN, OUT, R>(LanguageExt.Aff<R> ma, System.Func<R, LanguageExt.Unit> dispose)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Pipe<RT, IN, OUT, R> use<RT, IN, OUT, R>(LanguageExt.Eff<R> ma, System.Func<R, LanguageExt.Unit> dispose)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Pipe<RT, IN, OUT, R> use<RT, IN, OUT, R>(LanguageExt.Aff<RT, R> ma, System.Func<R, LanguageExt.Unit> dispose)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Pipe<RT, IN, OUT, R> use<RT, IN, OUT, R>(LanguageExt.Eff<RT, R> ma, System.Func<R, LanguageExt.Unit> dispose)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Pipe<RT, IN, OUT, LanguageExt.Unit> yield<RT, IN, OUT>(OUT value)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Pipe<RT, IN, OUT, LanguageExt.Unit> yieldAll<RT, IN, OUT>(System.Collections.Generic.IAsyncEnumerable<OUT> xs)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Pipe<RT, IN, OUT, LanguageExt.Unit> yieldAll<RT, IN, OUT>(System.IObservable<OUT> xs)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

### Pipe`3<IN, OUT, A> (class [abstract])

- `public LanguageExt.Pipes.Pipe<IN, OUT, B> Bind<B>(System.Func<A, LanguageExt.Pipes.Pipe<IN, OUT, B>> f)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, B> Bind<RT, B>(System.Func<A, LanguageExt.Pipes.Pipe<RT, IN, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<IN, OUT, B> Bind<B>(System.Func<A, LanguageExt.Pipes.Consumer<IN, B>> f)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, A> Interpret<RT>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<IN, OUT, B> Map<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Pipe<IN, OUT, B> Select<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Pipe<IN, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Pipe<IN, OUT, B>> f)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<RT, B>(System.Func<A, LanguageExt.Pipes.Pipe<RT, IN, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<IN, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Consumer<IN, B>> f)`
- `public LanguageExt.Pipes.Pipe<IN, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`
- `public LanguageExt.Pipes.Pipe<IN, OUT, C> SelectMany<B, C>(System.Func<A, LanguageExt.Pipes.Pipe<IN, OUT, B>> f, System.Func<A, B, C> project)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, C> SelectMany<RT, B, C>(System.Func<A, LanguageExt.Pipes.Pipe<RT, IN, OUT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<IN, OUT, C> SelectMany<B, C>(System.Func<A, LanguageExt.Pipes.Consumer<IN, B>> f, System.Func<A, B, C> project)`
- `public LanguageExt.Pipes.Pipe<IN, OUT, C> SelectMany<B, C>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f, System.Func<A, B, C> project)`
- `public static LanguageExt.Pipes.Pipe<IN, OUT, A> op_BitwiseAnd(LanguageExt.Pipes.Pipe<IN, OUT, A> lhs, LanguageExt.Pipes.Pipe<IN, OUT, A> rhs)`
- `public static LanguageExt.Pipes.Pipe<IN, OUT, A> op_Implicit(LanguageExt.Pipes.Pure<A> ma)`

### Pipe`4<RT, IN, OUT, A> (class) : LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, IN, LanguageExt.Unit, OUT, A>

- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

- `public Pipe`4(LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, IN, LanguageExt.Unit, OUT, A> value)`
- `public readonly LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, IN, LanguageExt.Unit, OUT, A> Value`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, IN, LanguageExt.Unit, OUT, S> Action<S>(LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, IN, LanguageExt.Unit, OUT, S> r)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, IN, LanguageExt.Unit, OUT, S> Bind<S>(System.Func<A, LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, IN, LanguageExt.Unit, OUT, S>> f)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, B> Bind<B>(System.Func<A, LanguageExt.Pipes.Pipe<RT, IN, OUT, B>> f)`
- `public System.Void Deconstruct(out LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, IN, LanguageExt.Unit, OUT, A>& value)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, IN, C1, C, A> For<C1, C>(System.Func<OUT, LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, IN, C1, C, LanguageExt.Unit>> body)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, IN, LanguageExt.Unit, OUT, S> Map<S>(System.Func<A, S> f)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, IN, LanguageExt.Unit, OUT, A> Observe()`
- `public LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, LanguageExt.Unit, OUT, A> PairEachRequestWithRespond<UOutA, AUInA>(System.Func<LanguageExt.Unit, LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, LanguageExt.Unit, IN, A>> lhs)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, IN, DInC, DOutC, A> PairEachRespondWithRequest<DInC, DOutC>(System.Func<OUT, LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, OUT, DInC, DOutC, A>> rhs)`
- `public LanguageExt.Pipes.Proxy<RT, OUT, LanguageExt.Unit, IN, LanguageExt.Unit, A> Reflect()`
- `public LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, LanguageExt.Unit, OUT, A> ReplaceRequest<UOutA, AUInA>(System.Func<LanguageExt.Unit, LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, LanguageExt.Unit, OUT, IN>> lhs)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, IN, DInC, DOutC, A> ReplaceRespond<DInC, DOutC>(System.Func<OUT, LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, IN, DInC, DOutC, LanguageExt.Unit>> rhs)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, B> Select<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Release<B>> bind)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Pipe<IN, OUT, B>> bind)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Pipe<RT, IN, OUT, B>> f)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, C> SelectMany<B, C>(System.Func<A, LanguageExt.Pipes.Release<B>> bind, System.Func<A, B, C> project)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, C> SelectMany<B, C>(System.Func<A, LanguageExt.Pipes.Pipe<IN, OUT, B>> bind, System.Func<A, B, C> project)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, C> SelectMany<B, C>(System.Func<A, LanguageExt.Pipes.Pipe<RT, IN, OUT, B>> f, System.Func<A, B, C> project)`
- `public LanguageExt.Pipes.Pipe<RT, IN, C, A> Then<C>(LanguageExt.Pipes.Pipe<RT, OUT, C, A> pipe)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, IN, LanguageExt.Unit, OUT, A> ToProxy()`
- `public static LanguageExt.Pipes.Pipe<RT, IN, OUT, A> op_BitwiseAnd(LanguageExt.Pipes.Pipe<RT, IN, OUT, A> lhs, LanguageExt.Pipes.Pipe<RT, IN, OUT, A> rhs)`
- `public static LanguageExt.Pipes.Producer<RT, OUT, A> op_BitwiseOr(LanguageExt.Pipes.Producer<RT, IN, A> p1, LanguageExt.Pipes.Pipe<RT, IN, OUT, A> p2)`
- `public static LanguageExt.Pipes.Producer<RT, OUT, A> op_BitwiseOr(LanguageExt.Pipes.Producer<IN, A> p1, LanguageExt.Pipes.Pipe<RT, IN, OUT, A> p2)`
- `public static LanguageExt.Pipes.Producer<RT, OUT, A> op_BitwiseOr(LanguageExt.Pipes.Producer<OUT, IN> p1, LanguageExt.Pipes.Pipe<RT, IN, OUT, A> p2)`
- `public static LanguageExt.Pipes.Consumer<RT, IN, A> op_BitwiseOr(LanguageExt.Pipes.Pipe<RT, IN, OUT, A> p1, LanguageExt.Pipes.Consumer<OUT, A> p2)`
- `public static LanguageExt.Pipes.Consumer<RT, IN, A> op_BitwiseOr(LanguageExt.Pipes.Pipe<RT, IN, OUT, A> p1, LanguageExt.Pipes.Consumer<RT, OUT, A> p2)`
- `public static LanguageExt.Pipes.Pipe<RT, IN, OUT, A> op_Implicit(LanguageExt.Pipes.Pipe<IN, OUT, A> p)`
- `public static LanguageExt.Pipes.Pipe<RT, IN, OUT, A> op_Implicit(LanguageExt.Pipes.Pure<A> p)`

### Producer (class [static])

- `[ext] public static LanguageExt.Pipes.Producer<RT, S, LanguageExt.Unit> FoldUntil<RT, S, A>(this LanguageExt.Pipes.Producer<RT, S, A> ma, S Initial, System.Func<S, A, S> Fold, System.Func<A, System.Boolean> UntilValue)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Producer<RT, S, LanguageExt.Unit> FoldUntil<RT, S, A>(this LanguageExt.Pipes.Producer<RT, S, A> ma, S Initial, System.Func<S, A, S> Fold, System.Func<S, System.Boolean> UntilState)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Producer<RT, S, LanguageExt.Unit> FoldWhile<RT, S, A>(this LanguageExt.Pipes.Producer<RT, S, A> ma, S Initial, System.Func<S, A, S> Fold, System.Func<A, System.Boolean> WhileValue)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Producer<RT, S, LanguageExt.Unit> FoldWhile<RT, S, A>(this LanguageExt.Pipes.Producer<RT, S, A> ma, S Initial, System.Func<S, A, S> Fold, System.Func<S, System.Boolean> WhileState)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Producer<RT, OUT, R> Pure<RT, OUT, R>(R value)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Producer<RT, OUT, R> lift<RT, OUT, R>(LanguageExt.Eff<R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Producer<RT, OUT, R> lift<RT, OUT, R>(LanguageExt.Aff<R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Producer<RT, OUT, R> lift<RT, OUT, R>(LanguageExt.Eff<RT, R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Producer<RT, OUT, R> lift<RT, OUT, R>(LanguageExt.Aff<RT, R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Producer<RT, OUT, LanguageExt.Unit> merge<RT, OUT>(LanguageExt.Seq<LanguageExt.Pipes.Producer<RT, OUT, LanguageExt.Unit>> ms)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Producer<RT, OUT, LanguageExt.Unit> merge<RT, OUT>(params LanguageExt.Pipes.Queue<RT, OUT, LanguageExt.Unit>[] ms)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Producer<RT, OUT, LanguageExt.Unit> merge<RT, OUT>(params LanguageExt.Pipes.Producer<RT, OUT, LanguageExt.Unit>[] ms)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Producer<RT, OUT, LanguageExt.Unit> merge<RT, OUT>(params LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, LanguageExt.Unit, OUT, LanguageExt.Unit>[] ms)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Producer<RT, OUT, LanguageExt.Unit> merge<RT, OUT>(LanguageExt.Seq<LanguageExt.Pipes.Queue<RT, OUT, LanguageExt.Unit>> ms)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Producer<RT, OUT, LanguageExt.Unit> merge<RT, OUT>(LanguageExt.Seq<LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, LanguageExt.Unit, OUT, LanguageExt.Unit>> ms)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Producer<RT, OUT, LanguageExt.Unit> release<RT, OUT, R>(R dispose)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Producer<RT, A, LanguageExt.Unit> repeatM<RT, A>(LanguageExt.Aff<RT, A> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Producer<RT, A, LanguageExt.Unit> repeatM<RT, A>(LanguageExt.Eff<RT, A> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Producer<RT, A, LanguageExt.Unit> repeatM<RT, A>(LanguageExt.Aff<A> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Producer<RT, A, LanguageExt.Unit> repeatM<RT, A>(LanguageExt.Eff<A> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Producer<RT, OUT, R> use<RT, OUT, R>(LanguageExt.Aff<R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `where R : System.IDisposable`
- `public static LanguageExt.Pipes.Producer<RT, OUT, R> use<RT, OUT, R>(LanguageExt.Eff<R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `where R : System.IDisposable`
- `public static LanguageExt.Pipes.Producer<RT, OUT, R> use<RT, OUT, R>(LanguageExt.Aff<RT, R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `where R : System.IDisposable`
- `public static LanguageExt.Pipes.Producer<RT, OUT, R> use<RT, OUT, R>(LanguageExt.Eff<RT, R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `where R : System.IDisposable`
- `public static LanguageExt.Pipes.Producer<RT, OUT, R> use<RT, OUT, R>(LanguageExt.Aff<R> ma, System.Func<R, LanguageExt.Unit> dispose)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Producer<RT, OUT, R> use<RT, OUT, R>(LanguageExt.Eff<R> ma, System.Func<R, LanguageExt.Unit> dispose)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Producer<RT, OUT, R> use<RT, OUT, R>(LanguageExt.Aff<RT, R> ma, System.Func<R, LanguageExt.Unit> dispose)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Producer<RT, OUT, R> use<RT, OUT, R>(LanguageExt.Eff<RT, R> ma, System.Func<R, LanguageExt.Unit> dispose)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Producer<RT, OUT, LanguageExt.Unit> yield<RT, OUT>(OUT value)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Producer<RT, X, LanguageExt.Unit> yieldAll<RT, X>(System.Collections.Generic.IEnumerable<X> xs)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Producer<RT, X, LanguageExt.Unit> yieldAll<RT, X>(System.Collections.Generic.IAsyncEnumerable<X> xs)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Producer<RT, X, LanguageExt.Unit> yieldAll<RT, X>(System.IObservable<X> xs)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

### ProducerLift`3<RT, OUT, A> (class [abstract])

- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

- `public LanguageExt.Pipes.ProducerLift<RT, OUT, B> Bind<B>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`
- `public LanguageExt.Pipes.ProducerLift<RT, OUT, B> Bind<B>(System.Func<A, LanguageExt.Pipes.ProducerLift<RT, OUT, B>> f)`
- `public LanguageExt.Pipes.Producer<RT, OUT, B> Bind<B>(System.Func<A, LanguageExt.Pipes.Producer<RT, OUT, B>> f)`
- `public LanguageExt.Pipes.Producer<RT, OUT, A> Interpret()`
- `public LanguageExt.Pipes.ProducerLift<RT, OUT, B> Map<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.ProducerLift<RT, OUT, B> Select<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.ProducerLift<RT, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`
- `public LanguageExt.Pipes.ProducerLift<RT, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.ProducerLift<RT, OUT, B>> f)`
- `public LanguageExt.Pipes.Producer<RT, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Producer<RT, OUT, B>> f)`
- `public LanguageExt.Pipes.ProducerLift<RT, OUT, C> SelectMany<B, C>(System.Func<A, LanguageExt.Pipes.ProducerLift<RT, OUT, B>> f, System.Func<A, B, C> project)`
- `public LanguageExt.Pipes.Producer<RT, OUT, C> SelectMany<B, C>(System.Func<A, LanguageExt.Pipes.Producer<RT, OUT, B>> f, System.Func<A, B, C> project)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, A> ToPipe<IN>()`
- `public static LanguageExt.Pipes.ProducerLift<RT, OUT, A> op_BitwiseAnd(LanguageExt.Pipes.ProducerLift<RT, OUT, A> lhs, LanguageExt.Pipes.ProducerLift<RT, OUT, A> rhs)`
- `public static LanguageExt.Pipes.ProducerLift<RT, OUT, A> op_Implicit(LanguageExt.Pipes.Pure<A> ma)`

### Producer`2<OUT, A> (class [abstract])

- `public LanguageExt.Pipes.Producer<OUT, B> Bind<B>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`
- `public LanguageExt.Pipes.Producer<RT, OUT, B> Bind<RT, B>(System.Func<A, LanguageExt.Pipes.Producer<RT, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Producer<RT, OUT, A> Interpret<RT>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Producer<OUT, B> Map<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Producer<OUT, B> Select<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Producer<OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`
- `public LanguageExt.Pipes.Producer<RT, OUT, B> SelectMany<RT, B>(System.Func<A, LanguageExt.Pipes.Producer<RT, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.ProducerLift<RT, OUT, B> SelectMany<RT, B>(System.Func<A, LanguageExt.Pipes.ProducerLift<RT, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Producer<OUT, C> SelectMany<B, C>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f, System.Func<A, B, C> project)`
- `public LanguageExt.Pipes.ProducerLift<RT, OUT, C> SelectMany<RT, B, C>(System.Func<A, LanguageExt.Pipes.ProducerLift<RT, OUT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Producer<RT, OUT, C> SelectMany<RT, B, C>(System.Func<A, LanguageExt.Pipes.Producer<RT, OUT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<IN, OUT, A> ToPipe<IN>()`
- `public LanguageExt.Pipes.ProducerLift<RT, OUT, A> ToProducerLift<RT>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Producer<OUT, A> op_BitwiseAnd(LanguageExt.Pipes.Producer<OUT, A> lhs, LanguageExt.Pipes.Producer<OUT, A> rhs)`
- `public static LanguageExt.Pipes.Producer<OUT, A> op_Implicit(LanguageExt.Pipes.Pure<A> ma)`

### Producer`3<RT, OUT, A> (class) : LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, LanguageExt.Unit, OUT, A>

- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

- `public Producer`3(LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, LanguageExt.Unit, OUT, A> value)`
- `public readonly LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, LanguageExt.Unit, OUT, A> Value`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, LanguageExt.Unit, OUT, B> Action<B>(LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, LanguageExt.Unit, OUT, B> r)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, LanguageExt.Unit, OUT, S> Bind<S>(System.Func<A, LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, LanguageExt.Unit, OUT, S>> f)`
- `public LanguageExt.Pipes.Producer<RT, OUT, B> Bind<B>(System.Func<A, LanguageExt.Pipes.Producer<RT, OUT, B>> f)`
- `public System.Void Deconstruct(out LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, LanguageExt.Unit, OUT, A>& value)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, C1, C, A> For<C1, C>(System.Func<OUT, LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, C1, C, LanguageExt.Unit>> body)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, LanguageExt.Unit, OUT, B> Map<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, LanguageExt.Unit, OUT, A> Observe()`
- `public LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, LanguageExt.Unit, OUT, A> PairEachRequestWithRespond<UOutA, AUInA>(System.Func<LanguageExt.Pipes.Void, LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, LanguageExt.Pipes.Void, LanguageExt.Unit, A>> lhs)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, DInC, DOutC, A> PairEachRespondWithRequest<DInC, DOutC>(System.Func<OUT, LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, OUT, DInC, DOutC, A>> rhs)`
- `public LanguageExt.Pipes.Proxy<RT, OUT, LanguageExt.Unit, LanguageExt.Unit, LanguageExt.Pipes.Void, A> Reflect()`
- `public LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, LanguageExt.Unit, OUT, A> ReplaceRequest<UOutA, AUInA>(System.Func<LanguageExt.Pipes.Void, LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, LanguageExt.Unit, OUT, LanguageExt.Unit>> lhs)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, DInC, DOutC, A> ReplaceRespond<DInC, DOutC>(System.Func<OUT, LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, DInC, DOutC, LanguageExt.Unit>> rhs)`
- `public LanguageExt.Pipes.Producer<RT, OUT, B> Select<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Producer<RT, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Producer<RT, OUT, B>> f)`
- `public LanguageExt.Pipes.Producer<RT, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Release<B>> bind)`
- `public LanguageExt.Pipes.Producer<RT, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> bind)`
- `public LanguageExt.Pipes.Producer<RT, OUT, C> SelectMany<B, C>(System.Func<A, LanguageExt.Pipes.Producer<RT, OUT, B>> f, System.Func<A, B, C> project)`
- `public LanguageExt.Pipes.Producer<RT, OUT, C> SelectMany<B, C>(System.Func<A, LanguageExt.Pipes.Release<B>> bind, System.Func<A, B, C> project)`
- `public LanguageExt.Pipes.Producer<RT, OUT, C> SelectMany<B, C>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> bind, System.Func<A, B, C> project)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, LanguageExt.Unit, OUT, A> ToProxy()`
- `public static LanguageExt.Pipes.Producer<RT, OUT, A> op_BitwiseAnd(LanguageExt.Pipes.Producer<RT, OUT, A> lhs, LanguageExt.Pipes.Producer<RT, OUT, A> rhs)`
- `public static LanguageExt.Pipes.Effect<RT, A> op_BitwiseOr(LanguageExt.Pipes.Producer<RT, OUT, A> p1, LanguageExt.Pipes.Consumer<RT, OUT, A> p2)`
- `public static LanguageExt.Pipes.Effect<RT, A> op_BitwiseOr(LanguageExt.Pipes.Producer<RT, OUT, A> p1, LanguageExt.Pipes.Consumer<OUT, A> p2)`
- `public static LanguageExt.Pipes.Producer<RT, OUT, A> op_Implicit(LanguageExt.Pipes.Producer<OUT, A> p)`
- `public static LanguageExt.Pipes.Producer<RT, OUT, A> op_Implicit(LanguageExt.Pipes.ProducerLift<RT, OUT, A> p)`
- `public static LanguageExt.Pipes.Producer<RT, OUT, A> op_Implicit(LanguageExt.Pipes.Pure<A> p)`

### Proxy (class [static])

- `[ext] public static LanguageExt.Pipes.Proxy<RT, A1, A, B1, B, S> Action<RT, A1, A, B1, B, R, S>(this LanguageExt.Pipes.Proxy<RT, A1, A, B1, B, R> l, LanguageExt.Pipes.Proxy<RT, A1, A, B1, B, S> r)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Proxy<RT, A1, A, B1, B, S> Apply<RT, A1, A, B1, B, R, S>(this LanguageExt.Pipes.Proxy<RT, A1, A, B1, B, System.Func<R, S>> pf, LanguageExt.Pipes.Proxy<RT, A1, A, B1, B, R> px)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Producer<RT, OUT_B, A> ForEach<RT, OUT_A, OUT_B, A>(this LanguageExt.Pipes.Producer<RT, OUT_A, A> p, System.Func<OUT_A, LanguageExt.Pipes.Producer<RT, OUT_B, LanguageExt.Unit>> body)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Effect<RT, A> ForEach<RT, OUT, A>(this LanguageExt.Pipes.Producer<RT, OUT, A> p, System.Func<OUT, LanguageExt.Pipes.Effect<RT, LanguageExt.Unit>> fb)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Consumer<RT, IN, A> ForEach<RT, IN, OUT, A>(this LanguageExt.Pipes.Pipe<RT, IN, OUT, A> p0, System.Func<OUT, LanguageExt.Pipes.Consumer<RT, IN, LanguageExt.Unit>> fb)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Pipe<RT, IN, OUT, R> ForEach<RT, IN, B, OUT, R>(this LanguageExt.Pipes.Pipe<RT, IN, B, R> p0, System.Func<B, LanguageExt.Pipes.Pipe<RT, IN, OUT, LanguageExt.Unit>> fb)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Pure<A> Pure<A>(A value)`
- `public static LanguageExt.Pipes.Proxy<RT, A1, A, B1, B, R> Pure<RT, A1, A, B1, B, R>(R value)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Queue<RT, A, LanguageExt.Unit> Queue<RT, A>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static System.Func<A, LanguageExt.Pipes.Proxy<RT, X1, X, C1, C, A1>> Then<RT, X1, X, A1, A, B1, B, C1, C>(this System.Func<A, LanguageExt.Pipes.Proxy<RT, X1, X, B1, B, A1>> fa, System.Func<B, LanguageExt.Pipes.Proxy<RT, X1, X, C1, C, B1>> fb)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Proxy<RT, X1, X, C1, C, A1> Then<RT, X1, X, A1, B1, C1, C, B>(this LanguageExt.Pipes.Proxy<RT, X1, X, B1, B, A1> p0, System.Func<B, LanguageExt.Pipes.Proxy<RT, X1, X, C1, C, B1>> fb)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Proxy<RT, A1, A, B1, B, S> apply<RT, A1, A, B1, B, R, S>(LanguageExt.Pipes.Proxy<RT, A1, A, B1, B, System.Func<R, S>> pf, LanguageExt.Pipes.Proxy<RT, A1, A, B1, B, R> px)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Consumer<A, A> awaiting<A>()`
- `public static LanguageExt.Pipes.Pipe<RT, A, A, R> cat<RT, A, R>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static A closed<A>(LanguageExt.Pipes.Void value)`
- `public static LanguageExt.Pipes.Lift<RT, System.ValueTuple<A, B>> collect<RT, A, B>(LanguageExt.Pipes.Effect<RT, A> ma, LanguageExt.Pipes.Effect<RT, B> mb)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Lift<RT, System.ValueTuple<A, B, C>> collect<RT, A, B, C>(LanguageExt.Pipes.Effect<RT, A> ma, LanguageExt.Pipes.Effect<RT, B> mb, LanguageExt.Pipes.Effect<RT, C> mc)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Lift<RT, System.ValueTuple<A, B, C, D>> collect<RT, A, B, C, D>(LanguageExt.Pipes.Effect<RT, A> ma, LanguageExt.Pipes.Effect<RT, B> mb, LanguageExt.Pipes.Effect<RT, C> mc, LanguageExt.Pipes.Effect<RT, D> md)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Lift<RT, System.ValueTuple<A, B, C, D, E>> collect<RT, A, B, C, D, E>(LanguageExt.Pipes.Effect<RT, A> ma, LanguageExt.Pipes.Effect<RT, B> mb, LanguageExt.Pipes.Effect<RT, C> mc, LanguageExt.Pipes.Effect<RT, D> md, LanguageExt.Pipes.Effect<RT, E> me)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, B> compose<RT, UOut, UIn, DIn, DOut, A, B>(LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, A> p1, LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, A, DIn, DOut, B> p2)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Effect<RT, A> compose<RT, OUT, A>(LanguageExt.Pipes.Effect<RT, OUT> p1, LanguageExt.Pipes.Consumer<RT, OUT, A> p2)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Consumer<RT, A, C> compose<RT, A, B, C>(LanguageExt.Pipes.Consumer<RT, A, B> p1, LanguageExt.Pipes.Consumer<RT, B, C> p2)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Producer<RT, OUT, C> compose<RT, OUT, IN, C>(LanguageExt.Pipes.Producer<RT, OUT, IN> p1, LanguageExt.Pipes.Pipe<RT, IN, OUT, C> p2)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Pipe<RT, A, Y, C> compose<RT, Y, A, B, C>(LanguageExt.Pipes.Pipe<RT, A, Y, B> p1, LanguageExt.Pipes.Pipe<RT, B, Y, C> p2)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Proxy<RT, A1, A, Y1, Y, C> compose<RT, A1, A, Y1, Y, B, C>(LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, B, Y1, Y, C> p2, LanguageExt.Pipes.Proxy<RT, A1, A, Y1, Y, B> p1)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Proxy<RT, A1, A, Y1, Y, C> compose<RT, A1, A, B1, B, Y1, Y, C>(System.Func<B1, LanguageExt.Pipes.Proxy<RT, A1, A, Y1, Y, B>> fb1, LanguageExt.Pipes.Proxy<RT, B1, B, Y1, Y, C> p0)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Proxy<RT, A1, A, C1, C, R> compose<RT, A1, A, B1, B, C1, C, R>(LanguageExt.Pipes.Proxy<RT, A1, A, B1, B, R> p, System.Func<B, LanguageExt.Pipes.Proxy<RT, B1, B, C1, C, R>> fb)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Proxy<RT, A1, A, C1, C, R> compose<RT, A1, A, B1, B, C1, C, R>(System.Func<B1, LanguageExt.Pipes.Proxy<RT, A1, A, B1, B, R>> fb1, LanguageExt.Pipes.Proxy<RT, B1, B, C1, C, R> p)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Proxy<RT, A1, A, C1, C, R> compose<RT, A1, A, B, C1, C, R>(LanguageExt.Pipes.Proxy<RT, A1, A, LanguageExt.Unit, B, R> p1, LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, B, C1, C, R> p2)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Effect<RT, R> compose<RT, B, R>(LanguageExt.Pipes.Producer<RT, B, R> p1, LanguageExt.Pipes.Consumer<RT, B, R> p2)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Producer<RT, C, R> compose<RT, B, C, R>(LanguageExt.Pipes.Producer<RT, B, R> p1, LanguageExt.Pipes.Pipe<RT, B, C, R> p2)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Consumer<RT, A, R> compose<RT, A, B, R>(LanguageExt.Pipes.Pipe<RT, A, B, R> p1, LanguageExt.Pipes.Consumer<RT, B, R> p2)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Pipe<RT, A, C, R> compose<RT, A, B, C, R>(LanguageExt.Pipes.Pipe<RT, A, B, R> p1, LanguageExt.Pipes.Pipe<RT, B, C, R> p2)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static System.Func<A, LanguageExt.Pipes.Proxy<RT, X1, X, C1, C, A1>> compose<RT, X1, X, A1, A, B1, B, C1, C>(System.Func<A, LanguageExt.Pipes.Proxy<RT, X1, X, B1, B, A1>> fa, System.Func<B, LanguageExt.Pipes.Proxy<RT, X1, X, C1, C, B1>> fb)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Proxy<RT, X1, X, C1, C, A1> compose<RT, X1, X, A1, B1, C1, C, B>(LanguageExt.Pipes.Proxy<RT, X1, X, B1, B, A1> p0, System.Func<B, LanguageExt.Pipes.Proxy<RT, X1, X, C1, C, B1>> fb)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static System.Func<C1, LanguageExt.Pipes.Proxy<RT, A1, A, Y1, Y, C>> compose<RT, A1, A, B1, B, Y1, Y, C1, C>(System.Func<B1, LanguageExt.Pipes.Proxy<RT, A1, A, Y1, Y, B>> fb1, System.Func<C1, LanguageExt.Pipes.Proxy<RT, B1, B, Y1, Y, C>> fc1)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Pipe<A, A, LanguageExt.Unit> filter<A>(System.Func<A, System.Boolean> f)`
- `public static LanguageExt.Pipes.Pipe<IN, OUT, LanguageExt.Unit> foldUntil<IN, OUT>(OUT Initial, System.Func<OUT, IN, OUT> Fold, System.Func<OUT, System.Boolean> State)`
- `public static LanguageExt.Pipes.Pipe<IN, OUT, LanguageExt.Unit> foldUntil<IN, OUT>(OUT Initial, System.Func<OUT, IN, OUT> Fold, System.Func<IN, System.Boolean> Value)`
- `public static LanguageExt.Pipes.Pipe<IN, OUT, LanguageExt.Unit> foldWhile<IN, OUT>(OUT Initial, System.Func<OUT, IN, OUT> Fold, System.Func<OUT, System.Boolean> State)`
- `public static LanguageExt.Pipes.Pipe<IN, OUT, LanguageExt.Unit> foldWhile<IN, OUT>(OUT Initial, System.Func<OUT, IN, OUT> Fold, System.Func<IN, System.Boolean> Value)`
- `public static LanguageExt.Pipes.Lift<RT, R> lift<RT, R>(LanguageExt.Eff<RT, R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Lift<RT, R> lift<RT, R>(LanguageExt.Aff<RT, R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Lift<RT, R> lift<RT, R>(LanguageExt.Eff<R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Lift<RT, R> lift<RT, R>(LanguageExt.Aff<R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Proxy<RT, A1, A, B1, B, R> lift<RT, A1, A, B1, B, R>(LanguageExt.Aff<R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Proxy<RT, A1, A, B1, B, R> lift<RT, A1, A, B1, B, R>(LanguageExt.Eff<R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Proxy<RT, A1, A, B1, B, R> lift<RT, A1, A, B1, B, R>(LanguageExt.Aff<RT, R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Proxy<RT, A1, A, B1, B, R> lift<RT, A1, A, B1, B, R>(LanguageExt.Eff<RT, R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Pipe<A, B, LanguageExt.Unit> map<A, B>(System.Func<A, B> f)`
- `public static LanguageExt.Pipes.Proxy<RT, A1, A, B1, B, R> observe<RT, A1, A, B1, B, R>(LanguageExt.Pipes.Proxy<RT, A1, A, B1, B, R> p0)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Proxy<RT, UOut, UIn, UOut, UIn, A> pull<RT, UOut, UIn, A>(UOut a1)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Proxy<RT, UOut, UIn, UOut, UIn, A> push<RT, UOut, UIn, A>(UIn a)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Proxy<RT, DOut, DIn, UIn, UOut, R> reflect<RT, UOut, UIn, DIn, DOut, R>(LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, R> p)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Release<LanguageExt.Unit> release<A>(A value)`
- `public static LanguageExt.Pipes.Proxy<RT, A1, A, B1, B, LanguageExt.Unit> release<RT, A1, A, B1, B, R>(R dispose)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Producer<RT, OUT, LanguageExt.Unit> repeat<RT, OUT, R>(LanguageExt.Pipes.Producer<RT, OUT, R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Consumer<RT, IN, LanguageExt.Unit> repeat<RT, IN, R>(LanguageExt.Pipes.Consumer<RT, IN, R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Pipe<RT, IN, OUT, LanguageExt.Unit> repeat<RT, IN, OUT, R>(LanguageExt.Pipes.Pipe<RT, IN, OUT, R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Proxy<RT, UOut, UIn, Y1, Y, UIn> request<RT, UOut, UIn, Y1, Y>(UOut value)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Proxy<RT, X1, X, DIn, DOut, DIn> respond<RT, X1, X, DIn, DOut>(DOut value)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Lift<RT, R> use<RT, R>(LanguageExt.Eff<RT, R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `where R : System.IDisposable`
- `public static LanguageExt.Pipes.Lift<RT, R> use<RT, R>(LanguageExt.Aff<RT, R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `where R : System.IDisposable`
- `public static LanguageExt.Pipes.Lift<RT, R> use<RT, R>(LanguageExt.Eff<R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `where R : System.IDisposable`
- `public static LanguageExt.Pipes.Lift<RT, R> use<RT, R>(LanguageExt.Aff<R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `where R : System.IDisposable`
- `public static LanguageExt.Pipes.Proxy<RT, A1, A, B1, B, R> use<RT, A1, A, B1, B, R>(LanguageExt.Aff<R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `where R : System.IDisposable`
- `public static LanguageExt.Pipes.Proxy<RT, A1, A, B1, B, R> use<RT, A1, A, B1, B, R>(LanguageExt.Eff<R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `where R : System.IDisposable`
- `public static LanguageExt.Pipes.Proxy<RT, A1, A, B1, B, R> use<RT, A1, A, B1, B, R>(LanguageExt.Aff<RT, R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `where R : System.IDisposable`
- `public static LanguageExt.Pipes.Proxy<RT, A1, A, B1, B, R> use<RT, A1, A, B1, B, R>(LanguageExt.Eff<RT, R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `where R : System.IDisposable`
- `public static LanguageExt.Pipes.Proxy<RT, A1, A, B1, B, R> use<RT, A1, A, B1, B, R>(LanguageExt.Aff<R> ma, System.Func<R, LanguageExt.Unit> dispose)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Proxy<RT, A1, A, B1, B, R> use<RT, A1, A, B1, B, R>(LanguageExt.Eff<R> ma, System.Func<R, LanguageExt.Unit> dispose)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Proxy<RT, A1, A, B1, B, R> use<RT, A1, A, B1, B, R>(LanguageExt.Aff<RT, R> ma, System.Func<R, LanguageExt.Unit> dispose)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Proxy<RT, A1, A, B1, B, R> use<RT, A1, A, B1, B, R>(LanguageExt.Eff<RT, R> ma, System.Func<R, LanguageExt.Unit> dispose)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Producer<A, LanguageExt.Unit> yield<A>(A value)`
- `public static LanguageExt.Pipes.ProducerLift<RT, System.ValueTuple<A, B>, LanguageExt.Unit> yield<RT, A, B>(LanguageExt.Pipes.Effect<RT, A> ma, LanguageExt.Pipes.Effect<RT, B> mb)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.ProducerLift<RT, System.ValueTuple<A, B, C>, LanguageExt.Unit> yield<RT, A, B, C>(LanguageExt.Pipes.Effect<RT, A> ma, LanguageExt.Pipes.Effect<RT, B> mb, LanguageExt.Pipes.Effect<RT, C> mc)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.ProducerLift<RT, System.ValueTuple<A, B, C, D>, LanguageExt.Unit> yield<RT, A, B, C, D>(LanguageExt.Pipes.Effect<RT, A> ma, LanguageExt.Pipes.Effect<RT, B> mb, LanguageExt.Pipes.Effect<RT, C> mc, LanguageExt.Pipes.Effect<RT, D> md)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.ProducerLift<RT, System.ValueTuple<A, B, C, D, E>, LanguageExt.Unit> yield<RT, A, B, C, D, E>(LanguageExt.Pipes.Effect<RT, A> ma, LanguageExt.Pipes.Effect<RT, B> mb, LanguageExt.Pipes.Effect<RT, C> mc, LanguageExt.Pipes.Effect<RT, D> md, LanguageExt.Pipes.Effect<RT, E> me)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Producer<X, LanguageExt.Unit> yieldAll<X>(System.Collections.Generic.IEnumerable<X> xs)`
- `public static LanguageExt.Pipes.Producer<X, LanguageExt.Unit> yieldAll<X>(System.Collections.Generic.IAsyncEnumerable<X> xs)`
- `public static LanguageExt.Pipes.Producer<X, LanguageExt.Unit> yieldAll<X>(System.IObservable<X> xs)`

### Proxy`6<RT, UOut, UIn, DIn, DOut, A> (class [abstract])

- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, B> Action<B>(LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, B> r)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, B> Bind<B>(System.Func<A, LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, B>> f)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, C1, C, A> For<C1, C>(System.Func<DOut, LanguageExt.Pipes.Proxy<RT, UOut, UIn, C1, C, DIn>> body)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, B> Map<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, A> Observe()`
- `public LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, DIn, DOut, A> PairEachRequestWithRespond<UOutA, AUInA>(System.Func<UOut, LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, UOut, UIn, A>> lhs)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DInC, DOutC, A> PairEachRespondWithRequest<DInC, DOutC>(System.Func<DOut, LanguageExt.Pipes.Proxy<RT, DIn, DOut, DInC, DOutC, A>> rhs)`
- `public LanguageExt.Pipes.Proxy<RT, DOut, DIn, UIn, UOut, A> Reflect()`
- `public LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, DIn, DOut, A> ReplaceRequest<UOutA, AUInA>(System.Func<UOut, LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, DIn, DOut, UIn>> lhs)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DInC, DOutC, A> ReplaceRespond<DInC, DOutC>(System.Func<DOut, LanguageExt.Pipes.Proxy<RT, UOut, UIn, DInC, DOutC, DIn>> rhs)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, B> Select<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, B>> f)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, C> SelectMany<B, C>(System.Func<A, LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, B>> f, System.Func<A, B, C> project)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, A> ToProxy()`

### Pure<IN, A> (class) : LanguageExt.Pipes.Consumer<IN, A>

- `public Pure(A value)`
- `public readonly A Value`
- `public LanguageExt.Pipes.Consumer<RT, IN, A> Interpret<RT>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Consumer<IN, B> Select<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Consumer<IN, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Consumer<IN, B>> f)`
- `public LanguageExt.Pipes.ConsumerLift<RT, IN, B> SelectMany<RT, B>(System.Func<A, LanguageExt.Pipes.ConsumerLift<RT, IN, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Consumer<RT, IN, B> SelectMany<RT, B>(System.Func<A, LanguageExt.Pipes.Consumer<RT, IN, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<IN, OUT, B> SelectMany<OUT, B>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<RT, OUT, B>(System.Func<A, LanguageExt.Pipes.ProducerLift<RT, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.ConsumerLift<RT, IN, A> ToConsumerLift<RT>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<IN, OUT, A> ToPipe<OUT>()`

### Pure<RT, IN, A> (class) : LanguageExt.Pipes.ConsumerLift<RT, IN, A>

- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

- `public Pure(A value)`
- `public readonly A Value`
- `public LanguageExt.Pipes.Consumer<RT, IN, A> Interpret()`
- `public LanguageExt.Pipes.ConsumerLift<RT, IN, B> Select<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.ConsumerLift<RT, IN, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.ConsumerLift<RT, IN, B>> f)`
- `public LanguageExt.Pipes.Consumer<RT, IN, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Consumer<RT, IN, B>> f)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<OUT, B>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, A> ToPipe<OUT>()`

### Pure<OUT, A> (class) : LanguageExt.Pipes.Enumerate<OUT, A>

- `public Pure(A value)`
- `public readonly A Value`
- `public LanguageExt.Pipes.Producer<RT, OUT, A> Interpret<RT>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, A> Interpret<RT, IN>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Enumerate<OUT, B> Select<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Enumerate<OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Release<B>> f)`
- `public LanguageExt.Pipes.Producer<OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`
- `public LanguageExt.Pipes.Enumerate<OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Enumerate<OUT, B>> f)`
- `public LanguageExt.Pipes.Producer<RT, OUT, B> SelectMany<RT, B>(System.Func<A, LanguageExt.Pipes.Producer<RT, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<IN, OUT, B> SelectMany<IN, B>(System.Func<A, LanguageExt.Pipes.Pipe<IN, OUT, B>> f)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<RT, IN, B>(System.Func<A, LanguageExt.Pipes.Pipe<RT, IN, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

### Pure<RT, A> (class) : LanguageExt.Pipes.Lift<RT, A>

- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

- `public Pure(A value)`
- `public readonly A Value`
- `public LanguageExt.Pipes.Lift<RT, B> Bind<B>(System.Func<A, LanguageExt.Pipes.Lift<RT, B>> f)`
- `public LanguageExt.Pipes.Lift<RT, B> Map<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Consumer<RT, IN, A> ToConsumer<IN>()`
- `public LanguageExt.Pipes.ConsumerLift<RT, IN, A> ToConsumerLift<IN>()`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, A> ToPipe<IN, OUT>()`
- `public LanguageExt.Pipes.Producer<RT, OUT, A> ToProducer<OUT>()`
- `public LanguageExt.Pipes.ProducerLift<RT, OUT, A> ToProducerLift<OUT>()`

### Pure<IN, OUT, A> (class) : LanguageExt.Pipes.Pipe<IN, OUT, A>

- `public Pure(A value)`
- `public readonly A Value`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, A> Interpret<RT>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<IN, OUT, B> Select<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Pipe<IN, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Pipe<IN, OUT, B>> f)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<RT, B>(System.Func<A, LanguageExt.Pipes.Pipe<RT, IN, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<IN, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Consumer<IN, B>> f)`
- `public LanguageExt.Pipes.Pipe<IN, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`

### Pure<OUT, A> (class) : LanguageExt.Pipes.Producer<OUT, A>

- `public Pure(A value)`
- `public readonly A Value`
- `public LanguageExt.Pipes.Producer<RT, OUT, A> Interpret<RT>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Producer<OUT, B> Select<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Producer<OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`
- `public LanguageExt.Pipes.Producer<RT, OUT, B> SelectMany<RT, B>(System.Func<A, LanguageExt.Pipes.Producer<RT, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<IN, OUT, A> ToPipe<IN>()`
- `public LanguageExt.Pipes.ProducerLift<RT, OUT, A> ToProducerLift<RT>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

### Pure<RT, OUT, A> (class) : LanguageExt.Pipes.ProducerLift<RT, OUT, A>

- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

- `public Pure(A value)`
- `public readonly A Value`
- `public LanguageExt.Pipes.Producer<RT, OUT, A> Interpret()`
- `public LanguageExt.Pipes.ProducerLift<RT, OUT, B> Select<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.ProducerLift<RT, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`
- `public LanguageExt.Pipes.ProducerLift<RT, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.ProducerLift<RT, OUT, B>> f)`
- `public LanguageExt.Pipes.Producer<RT, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Producer<RT, OUT, B>> f)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, A> ToPipe<IN>()`

### Pure<A> (class) : LanguageExt.Pipes.Release<A>

- `public Pure(A value)`
- `public readonly A Value`
- `public LanguageExt.Pipes.Client<RT, REQ, RES, A> InterpretClient<RT, REQ, RES>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Consumer<RT, IN, A> InterpretConsumer<RT, IN>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, A> InterpretPipe<RT, IN, OUT>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Producer<RT, OUT, A> InterpretProducer<RT, OUT>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Server<RT, REQ, RES, A> InterpretServer<RT, REQ, RES>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Release<B> Select<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Release<B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Release<B>> f)`
- `public LanguageExt.Pipes.Enumerate<OUT, B> SelectMany<OUT, B>(System.Func<A, LanguageExt.Pipes.Enumerate<OUT, B>> f)`
- `public LanguageExt.Pipes.Consumer<IN, B> SelectMany<IN, B>(System.Func<A, LanguageExt.Pipes.Consumer<IN, B>> f)`
- `public LanguageExt.Pipes.ConsumerLift<RT, IN, B> SelectMany<RT, IN, B>(System.Func<A, LanguageExt.Pipes.ConsumerLift<RT, IN, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Consumer<RT, IN, B> SelectMany<RT, IN, B>(System.Func<A, LanguageExt.Pipes.Consumer<RT, IN, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Producer<OUT, B> SelectMany<OUT, B>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`
- `public LanguageExt.Pipes.Producer<RT, OUT, B> SelectMany<RT, OUT, B>(System.Func<A, LanguageExt.Pipes.Producer<RT, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<IN, OUT, B> SelectMany<IN, OUT, B>(System.Func<A, LanguageExt.Pipes.Pipe<IN, OUT, B>> f)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<RT, IN, OUT, B>(System.Func<A, LanguageExt.Pipes.Pipe<RT, IN, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Consumer<IN, A> ToConsumer<IN>()`
- `public LanguageExt.Pipes.ConsumerLift<RT, IN, A> ToConsumerLift<RT, IN>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Enumerate<OUT, A> ToEnumerate<OUT>()`
- `public LanguageExt.Pipes.Pipe<IN, OUT, A> ToPipe<IN, OUT>()`
- `public LanguageExt.Pipes.Producer<OUT, A> ToProducer<OUT>()`

### PureProxy (class [static])

- `public static LanguageExt.Pipes.Consumer<IN, IN> ConsumerAwait<IN>()`
- `public static LanguageExt.Pipes.ConsumerLift<RT, IN, A> ConsumerLiftPure<RT, IN, A>(A value)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Consumer<IN, A> ConsumerPure<IN, A>(A value)`
- `public static LanguageExt.Pipes.Enumerate<OUT, A> EnumeratePure<OUT, A>(A value)`
- `public static LanguageExt.Pipes.Pipe<IN, OUT, IN> PipeAwait<IN, OUT>()`
- `public static LanguageExt.Pipes.Pipe<IN, OUT, A> PipePure<IN, OUT, A>(A value)`
- `public static LanguageExt.Pipes.Pipe<IN, OUT, LanguageExt.Unit> PipeYield<IN, OUT>(OUT value)`
- `public static LanguageExt.Pipes.Producer<OUT, LanguageExt.Unit> ProducerEnumerate<OUT>(System.Collections.Generic.IEnumerable<OUT> xs)`
- `public static LanguageExt.Pipes.Producer<OUT, LanguageExt.Unit> ProducerEnumerate<OUT>(System.Collections.Generic.IAsyncEnumerable<OUT> xs)`
- `public static LanguageExt.Pipes.Producer<OUT, LanguageExt.Unit> ProducerObserve<OUT>(System.IObservable<OUT> xs)`
- `public static LanguageExt.Pipes.Producer<OUT, A> ProducerPure<OUT, A>(A value)`
- `public static LanguageExt.Pipes.Producer<OUT, LanguageExt.Unit> ProducerYield<OUT>(OUT value)`
- `public static LanguageExt.Pipes.Pure<A> Pure<A>(A value)`
- `public static LanguageExt.Pipes.Release<A> ReleasePure<A>(A value)`
- `[ext] public static LanguageExt.Pipes.ConsumerLift<RT, IN, B> SelectMany<RT, IN, A, B>(this LanguageExt.Pipes.ConsumerLift<RT, IN, A> ma, System.Func<A, LanguageExt.Aff<RT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.ConsumerLift<RT, IN, B> SelectMany<RT, IN, A, B>(this LanguageExt.Pipes.Consumer<IN, A> ma, System.Func<A, LanguageExt.Aff<RT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Producer<RT, OUT, B> SelectMany<RT, OUT, A, B>(this LanguageExt.Pipes.Producer<OUT, A> ma, System.Func<A, LanguageExt.Aff<RT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<RT, IN, OUT, A, B>(this LanguageExt.Pipes.Pipe<IN, OUT, A> ma, System.Func<A, LanguageExt.Aff<RT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Consumer<RT, IN, B> SelectMany<RT, IN, A, B>(this LanguageExt.Pipes.Consumer<RT, IN, A> ma, System.Func<A, LanguageExt.Aff<RT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Producer<RT, OUT, B> SelectMany<RT, OUT, A, B>(this LanguageExt.Pipes.Producer<RT, OUT, A> ma, System.Func<A, LanguageExt.Aff<RT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<RT, IN, OUT, A, B>(this LanguageExt.Pipes.Pipe<RT, IN, OUT, A> ma, System.Func<A, LanguageExt.Aff<RT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.ConsumerLift<RT, IN, B> SelectMany<RT, IN, A, B>(this LanguageExt.Pipes.ConsumerLift<RT, IN, A> ma, System.Func<A, LanguageExt.Eff<RT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.ConsumerLift<RT, IN, B> SelectMany<RT, IN, A, B>(this LanguageExt.Pipes.Consumer<IN, A> ma, System.Func<A, LanguageExt.Eff<RT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Producer<RT, OUT, B> SelectMany<RT, OUT, A, B>(this LanguageExt.Pipes.Producer<OUT, A> ma, System.Func<A, LanguageExt.Eff<RT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<RT, IN, OUT, A, B>(this LanguageExt.Pipes.Pipe<IN, OUT, A> ma, System.Func<A, LanguageExt.Eff<RT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Consumer<RT, IN, B> SelectMany<RT, IN, A, B>(this LanguageExt.Pipes.Consumer<RT, IN, A> ma, System.Func<A, LanguageExt.Eff<RT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Producer<RT, OUT, B> SelectMany<RT, OUT, A, B>(this LanguageExt.Pipes.Producer<RT, OUT, A> ma, System.Func<A, LanguageExt.Eff<RT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<RT, IN, OUT, A, B>(this LanguageExt.Pipes.Pipe<RT, IN, OUT, A> ma, System.Func<A, LanguageExt.Eff<RT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.ConsumerLift<RT, IN, B> SelectMany<RT, IN, A, B>(this LanguageExt.Pipes.ConsumerLift<RT, IN, A> ma, System.Func<A, LanguageExt.Aff<B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Consumer<RT, IN, B> SelectMany<RT, IN, A, B>(this LanguageExt.Pipes.Consumer<RT, IN, A> ma, System.Func<A, LanguageExt.Aff<B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Producer<RT, OUT, B> SelectMany<RT, OUT, A, B>(this LanguageExt.Pipes.Producer<RT, OUT, A> ma, System.Func<A, LanguageExt.Aff<B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<RT, IN, OUT, A, B>(this LanguageExt.Pipes.Pipe<RT, IN, OUT, A> ma, System.Func<A, LanguageExt.Aff<B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.ConsumerLift<RT, IN, B> SelectMany<RT, IN, A, B>(this LanguageExt.Pipes.ConsumerLift<RT, IN, A> ma, System.Func<A, LanguageExt.Eff<B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Consumer<RT, IN, B> SelectMany<RT, IN, A, B>(this LanguageExt.Pipes.Consumer<RT, IN, A> ma, System.Func<A, LanguageExt.Eff<B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Producer<RT, OUT, B> SelectMany<RT, OUT, A, B>(this LanguageExt.Pipes.Producer<RT, OUT, A> ma, System.Func<A, LanguageExt.Eff<B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<RT, IN, OUT, A, B>(this LanguageExt.Pipes.Pipe<RT, IN, OUT, A> ma, System.Func<A, LanguageExt.Eff<B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.ConsumerLift<RT, IN, B> SelectMany<RT, IN, A, B>(this LanguageExt.Aff<RT, A> ma, System.Func<A, LanguageExt.Pipes.ConsumerLift<RT, IN, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.ConsumerLift<RT, IN, B> SelectMany<RT, IN, A, B>(this LanguageExt.Aff<RT, A> ma, System.Func<A, LanguageExt.Pipes.Consumer<IN, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Producer<RT, OUT, B> SelectMany<RT, OUT, A, B>(this LanguageExt.Aff<RT, A> ma, System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<RT, IN, OUT, A, B>(this LanguageExt.Aff<RT, A> ma, System.Func<A, LanguageExt.Pipes.Pipe<IN, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Consumer<RT, IN, B> SelectMany<RT, IN, A, B>(this LanguageExt.Aff<RT, A> ma, System.Func<A, LanguageExt.Pipes.Consumer<RT, IN, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Producer<RT, OUT, B> SelectMany<RT, OUT, A, B>(this LanguageExt.Aff<RT, A> ma, System.Func<A, LanguageExt.Pipes.Producer<RT, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<RT, IN, OUT, A, B>(this LanguageExt.Aff<RT, A> ma, System.Func<A, LanguageExt.Pipes.Pipe<RT, IN, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.ConsumerLift<RT, IN, B> SelectMany<RT, IN, A, B>(this LanguageExt.Eff<RT, A> ma, System.Func<A, LanguageExt.Pipes.ConsumerLift<RT, IN, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.ConsumerLift<RT, IN, B> SelectMany<RT, IN, A, B>(this LanguageExt.Eff<RT, A> ma, System.Func<A, LanguageExt.Pipes.Consumer<IN, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Producer<RT, OUT, B> SelectMany<RT, OUT, A, B>(this LanguageExt.Eff<RT, A> ma, System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<RT, IN, OUT, A, B>(this LanguageExt.Eff<RT, A> ma, System.Func<A, LanguageExt.Pipes.Pipe<IN, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Consumer<RT, IN, B> SelectMany<RT, IN, A, B>(this LanguageExt.Eff<RT, A> ma, System.Func<A, LanguageExt.Pipes.Consumer<RT, IN, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Producer<RT, OUT, B> SelectMany<RT, OUT, A, B>(this LanguageExt.Eff<RT, A> ma, System.Func<A, LanguageExt.Pipes.Producer<RT, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<RT, IN, OUT, A, B>(this LanguageExt.Eff<RT, A> ma, System.Func<A, LanguageExt.Pipes.Pipe<RT, IN, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.ConsumerLift<RT, IN, B> SelectMany<RT, IN, A, B>(this LanguageExt.Aff<A> ma, System.Func<A, LanguageExt.Pipes.ConsumerLift<RT, IN, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Consumer<RT, IN, B> SelectMany<RT, IN, A, B>(this LanguageExt.Aff<A> ma, System.Func<A, LanguageExt.Pipes.Consumer<RT, IN, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Producer<RT, OUT, B> SelectMany<RT, OUT, A, B>(this LanguageExt.Aff<A> ma, System.Func<A, LanguageExt.Pipes.Producer<RT, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<RT, IN, OUT, A, B>(this LanguageExt.Aff<A> ma, System.Func<A, LanguageExt.Pipes.Pipe<RT, IN, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.ConsumerLift<RT, IN, B> SelectMany<RT, IN, A, B>(this LanguageExt.Eff<A> ma, System.Func<A, LanguageExt.Pipes.ConsumerLift<RT, IN, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<RT, IN, OUT, A, B>(this LanguageExt.Eff<A> ma, System.Func<A, LanguageExt.Pipes.Pipe<IN, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Consumer<RT, IN, B> SelectMany<RT, IN, A, B>(this LanguageExt.Eff<A> ma, System.Func<A, LanguageExt.Pipes.Consumer<RT, IN, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Producer<RT, OUT, B> SelectMany<RT, OUT, A, B>(this LanguageExt.Eff<A> ma, System.Func<A, LanguageExt.Pipes.Producer<RT, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<RT, IN, OUT, A, B>(this LanguageExt.Eff<A> ma, System.Func<A, LanguageExt.Pipes.Pipe<RT, IN, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.ConsumerLift<RT, IN, C> SelectMany<RT, IN, A, B, C>(this LanguageExt.Pipes.ConsumerLift<RT, IN, A> ma, System.Func<A, LanguageExt.Aff<RT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.ConsumerLift<RT, IN, C> SelectMany<RT, IN, A, B, C>(this LanguageExt.Pipes.Consumer<IN, A> ma, System.Func<A, LanguageExt.Aff<RT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Producer<RT, OUT, C> SelectMany<RT, OUT, A, B, C>(this LanguageExt.Pipes.Producer<OUT, A> ma, System.Func<A, LanguageExt.Aff<RT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Pipe<RT, IN, OUT, C> SelectMany<RT, IN, OUT, A, B, C>(this LanguageExt.Pipes.Pipe<IN, OUT, A> ma, System.Func<A, LanguageExt.Aff<RT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Consumer<RT, IN, C> SelectMany<RT, IN, A, B, C>(this LanguageExt.Pipes.Consumer<RT, IN, A> ma, System.Func<A, LanguageExt.Aff<RT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Producer<RT, OUT, C> SelectMany<RT, OUT, A, B, C>(this LanguageExt.Pipes.Producer<RT, OUT, A> ma, System.Func<A, LanguageExt.Aff<RT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Pipe<RT, IN, OUT, C> SelectMany<RT, IN, OUT, A, B, C>(this LanguageExt.Pipes.Pipe<RT, IN, OUT, A> ma, System.Func<A, LanguageExt.Aff<RT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.ConsumerLift<RT, IN, C> SelectMany<RT, IN, A, B, C>(this LanguageExt.Pipes.ConsumerLift<RT, IN, A> ma, System.Func<A, LanguageExt.Eff<RT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.ConsumerLift<RT, IN, C> SelectMany<RT, IN, A, B, C>(this LanguageExt.Pipes.Consumer<IN, A> ma, System.Func<A, LanguageExt.Eff<RT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Producer<RT, OUT, C> SelectMany<RT, OUT, A, B, C>(this LanguageExt.Pipes.Producer<OUT, A> ma, System.Func<A, LanguageExt.Eff<RT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Pipe<RT, IN, OUT, C> SelectMany<RT, IN, OUT, A, B, C>(this LanguageExt.Pipes.Pipe<IN, OUT, A> ma, System.Func<A, LanguageExt.Eff<RT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Consumer<RT, IN, C> SelectMany<RT, IN, A, B, C>(this LanguageExt.Pipes.Consumer<RT, IN, A> ma, System.Func<A, LanguageExt.Eff<RT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Producer<RT, OUT, C> SelectMany<RT, OUT, A, B, C>(this LanguageExt.Pipes.Producer<RT, OUT, A> ma, System.Func<A, LanguageExt.Eff<RT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Pipe<RT, IN, OUT, C> SelectMany<RT, IN, OUT, A, B, C>(this LanguageExt.Pipes.Pipe<RT, IN, OUT, A> ma, System.Func<A, LanguageExt.Eff<RT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.ConsumerLift<RT, IN, C> SelectMany<RT, IN, A, B, C>(this LanguageExt.Pipes.ConsumerLift<RT, IN, A> ma, System.Func<A, LanguageExt.Aff<B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Consumer<RT, IN, C> SelectMany<RT, IN, A, B, C>(this LanguageExt.Pipes.Consumer<RT, IN, A> ma, System.Func<A, LanguageExt.Aff<B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Producer<RT, OUT, C> SelectMany<RT, OUT, A, B, C>(this LanguageExt.Pipes.Producer<RT, OUT, A> ma, System.Func<A, LanguageExt.Aff<B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Pipe<RT, IN, OUT, C> SelectMany<RT, IN, OUT, A, B, C>(this LanguageExt.Pipes.Pipe<RT, IN, OUT, A> ma, System.Func<A, LanguageExt.Aff<B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.ConsumerLift<RT, IN, C> SelectMany<RT, IN, A, B, C>(this LanguageExt.Pipes.ConsumerLift<RT, IN, A> ma, System.Func<A, LanguageExt.Eff<B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Consumer<RT, IN, C> SelectMany<RT, IN, A, B, C>(this LanguageExt.Pipes.Consumer<RT, IN, A> ma, System.Func<A, LanguageExt.Eff<B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Producer<RT, OUT, C> SelectMany<RT, OUT, A, B, C>(this LanguageExt.Pipes.Producer<RT, OUT, A> ma, System.Func<A, LanguageExt.Eff<B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Pipe<RT, IN, OUT, C> SelectMany<RT, IN, OUT, A, B, C>(this LanguageExt.Pipes.Pipe<RT, IN, OUT, A> ma, System.Func<A, LanguageExt.Eff<B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.ConsumerLift<RT, IN, C> SelectMany<RT, IN, A, B, C>(this LanguageExt.Aff<RT, A> ma, System.Func<A, LanguageExt.Pipes.ConsumerLift<RT, IN, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.ConsumerLift<RT, IN, C> SelectMany<RT, IN, A, B, C>(this LanguageExt.Aff<RT, A> ma, System.Func<A, LanguageExt.Pipes.Consumer<IN, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Producer<RT, OUT, C> SelectMany<RT, OUT, A, B, C>(this LanguageExt.Aff<RT, A> ma, System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Pipe<RT, IN, OUT, C> SelectMany<RT, IN, OUT, A, B, C>(this LanguageExt.Aff<RT, A> ma, System.Func<A, LanguageExt.Pipes.Pipe<IN, OUT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Consumer<RT, IN, C> SelectMany<RT, IN, A, B, C>(this LanguageExt.Aff<RT, A> ma, System.Func<A, LanguageExt.Pipes.Consumer<RT, IN, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Producer<RT, OUT, C> SelectMany<RT, OUT, A, B, C>(this LanguageExt.Aff<RT, A> ma, System.Func<A, LanguageExt.Pipes.Producer<RT, OUT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Pipe<RT, IN, OUT, C> SelectMany<RT, IN, OUT, A, B, C>(this LanguageExt.Aff<RT, A> ma, System.Func<A, LanguageExt.Pipes.Pipe<RT, IN, OUT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.ConsumerLift<RT, IN, C> SelectMany<RT, IN, A, B, C>(this LanguageExt.Eff<RT, A> ma, System.Func<A, LanguageExt.Pipes.ConsumerLift<RT, IN, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.ConsumerLift<RT, IN, C> SelectMany<RT, IN, A, B, C>(this LanguageExt.Eff<RT, A> ma, System.Func<A, LanguageExt.Pipes.Consumer<IN, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Producer<RT, OUT, C> SelectMany<RT, OUT, A, B, C>(this LanguageExt.Eff<RT, A> ma, System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Pipe<RT, IN, OUT, C> SelectMany<RT, IN, OUT, A, B, C>(this LanguageExt.Eff<RT, A> ma, System.Func<A, LanguageExt.Pipes.Pipe<IN, OUT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Consumer<RT, IN, C> SelectMany<RT, IN, A, B, C>(this LanguageExt.Eff<RT, A> ma, System.Func<A, LanguageExt.Pipes.Consumer<RT, IN, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Producer<RT, OUT, C> SelectMany<RT, OUT, A, B, C>(this LanguageExt.Eff<RT, A> ma, System.Func<A, LanguageExt.Pipes.Producer<RT, OUT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Pipe<RT, IN, OUT, C> SelectMany<RT, IN, OUT, A, B, C>(this LanguageExt.Eff<RT, A> ma, System.Func<A, LanguageExt.Pipes.Pipe<RT, IN, OUT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.ConsumerLift<RT, IN, C> SelectMany<RT, IN, A, B, C>(this LanguageExt.Aff<A> ma, System.Func<A, LanguageExt.Pipes.ConsumerLift<RT, IN, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Consumer<RT, IN, C> SelectMany<RT, IN, A, B, C>(this LanguageExt.Aff<A> ma, System.Func<A, LanguageExt.Pipes.Consumer<RT, IN, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Producer<RT, OUT, C> SelectMany<RT, OUT, A, B, C>(this LanguageExt.Aff<A> ma, System.Func<A, LanguageExt.Pipes.Producer<RT, OUT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Pipe<RT, IN, OUT, C> SelectMany<RT, IN, OUT, A, B, C>(this LanguageExt.Aff<A> ma, System.Func<A, LanguageExt.Pipes.Pipe<RT, IN, OUT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.ConsumerLift<RT, IN, C> SelectMany<RT, IN, A, B, C>(this LanguageExt.Eff<A> ma, System.Func<A, LanguageExt.Pipes.ConsumerLift<RT, IN, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Pipe<RT, IN, OUT, C> SelectMany<RT, IN, OUT, A, B, C>(this LanguageExt.Eff<A> ma, System.Func<A, LanguageExt.Pipes.Pipe<IN, OUT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Consumer<RT, IN, C> SelectMany<RT, IN, A, B, C>(this LanguageExt.Eff<A> ma, System.Func<A, LanguageExt.Pipes.Consumer<RT, IN, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Producer<RT, OUT, C> SelectMany<RT, OUT, A, B, C>(this LanguageExt.Eff<A> ma, System.Func<A, LanguageExt.Pipes.Producer<RT, OUT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `[ext] public static LanguageExt.Pipes.Pipe<RT, IN, OUT, C> SelectMany<RT, IN, OUT, A, B, C>(this LanguageExt.Eff<A> ma, System.Func<A, LanguageExt.Pipes.Pipe<RT, IN, OUT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

### Pure`1<A> (class)

- `public Pure`1(A value)`
- `public readonly A Value`

### Pure`6<RT, UOut, UIn, DIn, DOut, A> (class) : LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, A>

- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

- `public Pure`6(A value)`
- `public readonly A Value`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, B> Action<B>(LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, B> r)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, B> Bind<B>(System.Func<A, LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, B>> f)`
- `public System.Void Deconstruct(out A& value)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, C1, C, A> For<C1, C>(System.Func<DOut, LanguageExt.Pipes.Proxy<RT, UOut, UIn, C1, C, DIn>> body)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, B> Map<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, A> Observe()`
- `public LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, DIn, DOut, A> PairEachRequestWithRespond<UOutA, AUInA>(System.Func<UOut, LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, UOut, UIn, A>> _)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DInC, DOutC, A> PairEachRespondWithRequest<DInC, DOutC>(System.Func<DOut, LanguageExt.Pipes.Proxy<RT, DIn, DOut, DInC, DOutC, A>> _)`
- `public LanguageExt.Pipes.Proxy<RT, DOut, DIn, UIn, UOut, A> Reflect()`
- `public LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, DIn, DOut, A> ReplaceRequest<UOutA, AUInA>(System.Func<UOut, LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, DIn, DOut, UIn>> _)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DInC, DOutC, A> ReplaceRespond<DInC, DOutC>(System.Func<DOut, LanguageExt.Pipes.Proxy<RT, UOut, UIn, DInC, DOutC, DIn>> _)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, A> ToProxy()`

### Queue`3<RT, OUT, A> (class) : LanguageExt.Pipes.Producer<RT, OUT, A>

- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

- `public LanguageExt.Eff<RT, LanguageExt.Unit> DoneEff { get; }`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, LanguageExt.Unit, OUT, B> Action<B>(LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, LanguageExt.Unit, OUT, B> r)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, LanguageExt.Unit, OUT, S> Bind<S>(System.Func<A, LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, LanguageExt.Unit, OUT, S>> f)`
- `public LanguageExt.Unit Done()`
- `public LanguageExt.Unit Enqueue(OUT value)`
- `public LanguageExt.Eff<RT, LanguageExt.Unit> EnqueueEff(OUT value)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, C1, C, A> For<C1, C>(System.Func<OUT, LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, C1, C, LanguageExt.Unit>> body)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, LanguageExt.Unit, OUT, B> Map<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, LanguageExt.Unit, OUT, A> Observe()`
- `public LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, LanguageExt.Unit, OUT, A> PairEachRequestWithRespond<UOutA, AUInA>(System.Func<LanguageExt.Pipes.Void, LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, LanguageExt.Pipes.Void, LanguageExt.Unit, A>> lhs)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, DInC, DOutC, A> PairEachRespondWithRequest<DInC, DOutC>(System.Func<OUT, LanguageExt.Pipes.Proxy<RT, LanguageExt.Unit, OUT, DInC, DOutC, A>> rhs)`
- `public LanguageExt.Pipes.Proxy<RT, OUT, LanguageExt.Unit, LanguageExt.Unit, LanguageExt.Pipes.Void, A> Reflect()`
- `public LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, LanguageExt.Unit, OUT, A> ReplaceRequest<UOutA, AUInA>(System.Func<LanguageExt.Pipes.Void, LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, LanguageExt.Unit, OUT, LanguageExt.Unit>> lhs)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, DInC, DOutC, A> ReplaceRespond<DInC, DOutC>(System.Func<OUT, LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, DInC, DOutC, LanguageExt.Unit>> rhs)`
- `public LanguageExt.Pipes.Producer<RT, OUT, B> Select<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, LanguageExt.Unit, OUT, A> ToProxy()`
- `public static LanguageExt.Pipes.Effect<RT, A> op_BitwiseOr(LanguageExt.Pipes.Queue<RT, OUT, A> p1, LanguageExt.Pipes.Consumer<RT, OUT, A> p2)`
- `public static LanguageExt.Pipes.Effect<RT, A> op_BitwiseOr(LanguageExt.Pipes.Queue<RT, OUT, A> p1, LanguageExt.Pipes.Consumer<OUT, A> p2)`

### Release`1<A> (class [abstract])

- `public LanguageExt.Pipes.Release<B> Bind<B>(System.Func<A, LanguageExt.Pipes.Release<B>> f)`
- `public LanguageExt.Pipes.Enumerate<OUT, B> Bind<OUT, B>(System.Func<A, LanguageExt.Pipes.Enumerate<OUT, B>> f)`
- `public LanguageExt.Pipes.Consumer<IN, B> Bind<IN, B>(System.Func<A, LanguageExt.Pipes.Consumer<IN, B>> f)`
- `public LanguageExt.Pipes.Consumer<RT, IN, B> Bind<RT, IN, B>(System.Func<A, LanguageExt.Pipes.Consumer<RT, IN, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Producer<OUT, B> Bind<OUT, B>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`
- `public LanguageExt.Pipes.Producer<RT, OUT, B> Bind<RT, OUT, B>(System.Func<A, LanguageExt.Pipes.Producer<RT, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<IN, OUT, B> Bind<IN, OUT, B>(System.Func<A, LanguageExt.Pipes.Pipe<IN, OUT, B>> f)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, B> Bind<RT, IN, OUT, B>(System.Func<A, LanguageExt.Pipes.Pipe<RT, IN, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Client<RT, REQ, RES, A> InterpretClient<RT, REQ, RES>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Consumer<RT, IN, A> InterpretConsumer<RT, IN>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, A> InterpretPipe<RT, IN, OUT>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Producer<RT, OUT, A> InterpretProducer<RT, OUT>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Server<RT, REQ, RES, A> InterpretServer<RT, REQ, RES>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Release<B> Map<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Release<B> Select<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Release<B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Release<B>> f)`
- `public LanguageExt.Pipes.Enumerate<OUT, B> SelectMany<OUT, B>(System.Func<A, LanguageExt.Pipes.Enumerate<OUT, B>> f)`
- `public LanguageExt.Pipes.Consumer<IN, B> SelectMany<IN, B>(System.Func<A, LanguageExt.Pipes.Consumer<IN, B>> f)`
- `public LanguageExt.Pipes.Consumer<RT, IN, B> SelectMany<RT, IN, B>(System.Func<A, LanguageExt.Pipes.Consumer<RT, IN, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.ConsumerLift<RT, IN, B> SelectMany<RT, IN, B>(System.Func<A, LanguageExt.Pipes.ConsumerLift<RT, IN, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Producer<OUT, B> SelectMany<OUT, B>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`
- `public LanguageExt.Pipes.Producer<RT, OUT, B> SelectMany<RT, OUT, B>(System.Func<A, LanguageExt.Pipes.Producer<RT, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<IN, OUT, B> SelectMany<IN, OUT, B>(System.Func<A, LanguageExt.Pipes.Pipe<IN, OUT, B>> f)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<RT, IN, OUT, B>(System.Func<A, LanguageExt.Pipes.Pipe<RT, IN, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Release<C> SelectMany<B, C>(System.Func<A, LanguageExt.Pipes.Release<B>> f, System.Func<A, B, C> project)`
- `public LanguageExt.Pipes.Enumerate<OUT, C> SelectMany<OUT, B, C>(System.Func<A, LanguageExt.Pipes.Enumerate<OUT, B>> f, System.Func<A, B, C> project)`
- `public LanguageExt.Pipes.Consumer<IN, C> SelectMany<IN, B, C>(System.Func<A, LanguageExt.Pipes.Consumer<IN, B>> f, System.Func<A, B, C> project)`
- `public LanguageExt.Pipes.Consumer<RT, IN, C> SelectMany<RT, IN, B, C>(System.Func<A, LanguageExt.Pipes.Consumer<RT, IN, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Producer<OUT, C> SelectMany<OUT, B, C>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f, System.Func<A, B, C> project)`
- `public LanguageExt.Pipes.Producer<RT, OUT, C> SelectMany<RT, OUT, B, C>(System.Func<A, LanguageExt.Pipes.Producer<RT, OUT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<IN, OUT, C> SelectMany<IN, OUT, B, C>(System.Func<A, LanguageExt.Pipes.Pipe<IN, OUT, B>> f, System.Func<A, B, C> project)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, C> SelectMany<RT, IN, OUT, B, C>(System.Func<A, LanguageExt.Pipes.Pipe<RT, IN, OUT, B>> f, System.Func<A, B, C> project)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Consumer<IN, A> ToConsumer<IN>()`
- `public LanguageExt.Pipes.ConsumerLift<RT, IN, A> ToConsumerLift<RT, IN>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Enumerate<OUT, A> ToEnumerate<OUT>()`
- `public LanguageExt.Pipes.Pipe<IN, OUT, A> ToPipe<IN, OUT>()`
- `public LanguageExt.Pipes.Producer<OUT, A> ToProducer<OUT>()`
- `public static LanguageExt.Pipes.Release<A> op_Implicit(LanguageExt.Pipes.Pure<A> ma)`

### Release`1<IN, A, X> (class) : LanguageExt.Pipes.Consumer<IN, A>

- `public Release`1(X value, System.Func<LanguageExt.Unit, LanguageExt.Pipes.Consumer<IN, A>> next)`
- `public LanguageExt.Pipes.Consumer<RT, IN, A> Interpret<RT>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Consumer<IN, B> Select<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Consumer<IN, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Consumer<IN, B>> f)`
- `public LanguageExt.Pipes.ConsumerLift<RT, IN, B> SelectMany<RT, B>(System.Func<A, LanguageExt.Pipes.ConsumerLift<RT, IN, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Consumer<RT, IN, B> SelectMany<RT, B>(System.Func<A, LanguageExt.Pipes.Consumer<RT, IN, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<IN, OUT, B> SelectMany<OUT, B>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<RT, OUT, B>(System.Func<A, LanguageExt.Pipes.ProducerLift<RT, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.ConsumerLift<RT, IN, A> ToConsumerLift<RT>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<IN, OUT, A> ToPipe<OUT>()`

### Release`1<RT, IN, A, X> (class) : LanguageExt.Pipes.ConsumerLift<RT, IN, A>

- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

- `public Release`1(X value, System.Func<LanguageExt.Unit, LanguageExt.Pipes.ConsumerLift<RT, IN, A>> next)`
- `public LanguageExt.Pipes.Consumer<RT, IN, A> Interpret()`
- `public LanguageExt.Pipes.ConsumerLift<RT, IN, B> Select<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.ConsumerLift<RT, IN, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.ConsumerLift<RT, IN, B>> f)`
- `public LanguageExt.Pipes.Consumer<RT, IN, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Consumer<RT, IN, B>> f)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<OUT, B>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, A> ToPipe<OUT>()`

### Release`1<OUT, A, X> (class) : LanguageExt.Pipes.Enumerate<OUT, A>

- `public Release`1(X value, System.Func<LanguageExt.Unit, LanguageExt.Pipes.Enumerate<OUT, A>> next)`
- `public LanguageExt.Pipes.Producer<RT, OUT, A> Interpret<RT>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, A> Interpret<RT, IN>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Enumerate<OUT, B> Select<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Enumerate<OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Enumerate<OUT, B>> f)`
- `public LanguageExt.Pipes.Enumerate<OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Release<B>> f)`
- `public LanguageExt.Pipes.Producer<OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`
- `public LanguageExt.Pipes.Producer<RT, OUT, B> SelectMany<RT, B>(System.Func<A, LanguageExt.Pipes.Producer<RT, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<IN, OUT, B> SelectMany<IN, B>(System.Func<A, LanguageExt.Pipes.Pipe<IN, OUT, B>> f)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<RT, IN, B>(System.Func<A, LanguageExt.Pipes.Pipe<RT, IN, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

### Release`1<IN, OUT, A, X> (class) : LanguageExt.Pipes.Pipe<IN, OUT, A>

- `public Release`1(X value, System.Func<LanguageExt.Unit, LanguageExt.Pipes.Pipe<IN, OUT, A>> next)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, A> Interpret<RT>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<IN, OUT, B> Select<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Pipe<IN, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Pipe<IN, OUT, B>> f)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<RT, B>(System.Func<A, LanguageExt.Pipes.Pipe<RT, IN, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<IN, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Consumer<IN, B>> f)`
- `public LanguageExt.Pipes.Pipe<IN, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`

### Release`1<OUT, A, X> (class) : LanguageExt.Pipes.Producer<OUT, A>

- `public Release`1(X value, System.Func<LanguageExt.Unit, LanguageExt.Pipes.Producer<OUT, A>> next)`
- `public LanguageExt.Pipes.Producer<RT, OUT, A> Interpret<RT>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Producer<OUT, B> Select<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Producer<OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`
- `public LanguageExt.Pipes.Producer<RT, OUT, B> SelectMany<RT, B>(System.Func<A, LanguageExt.Pipes.Producer<RT, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<IN, OUT, A> ToPipe<IN>()`
- `public LanguageExt.Pipes.ProducerLift<RT, OUT, A> ToProducerLift<RT>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

### Release`1<RT, OUT, A, X> (class) : LanguageExt.Pipes.ProducerLift<RT, OUT, A>

- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

- `public Release`1(X value, System.Func<LanguageExt.Unit, LanguageExt.Pipes.ProducerLift<RT, OUT, A>> next)`
- `public LanguageExt.Pipes.Producer<RT, OUT, A> Interpret()`
- `public LanguageExt.Pipes.ProducerLift<RT, OUT, B> Select<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.ProducerLift<RT, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`
- `public LanguageExt.Pipes.ProducerLift<RT, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.ProducerLift<RT, OUT, B>> f)`
- `public LanguageExt.Pipes.Producer<RT, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Producer<RT, OUT, B>> f)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, A> ToPipe<IN>()`

### Release`6<RT, UOut, UIn, DIn, DOut, A> (class [abstract]) : LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, A>

- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`


### Release`7<RT, UOut, UIn, DIn, DOut, X, A> (class) : LanguageExt.Pipes.Release<RT, UOut, UIn, DIn, DOut, A>

- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

- `public Release`7(X value, System.Func<LanguageExt.Unit, LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, A>> next)`
- `public readonly System.Func<LanguageExt.Unit, LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, A>> Next`
- `public readonly X Value`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, B> Action<B>(LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, B> rhs)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, B> Bind<B>(System.Func<A, LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, B>> f)`
- `public System.Void Deconstruct(out X& value, out System.Func<LanguageExt.Unit, LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, A>>& next)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, C1, C, A> For<C1, C>(System.Func<DOut, LanguageExt.Pipes.Proxy<RT, UOut, UIn, C1, C, DIn>> body)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, B> Map<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, A> Observe()`
- `public LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, DIn, DOut, A> PairEachRequestWithRespond<UOutA, AUInA>(System.Func<UOut, LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, UOut, UIn, A>> lhs)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DInC, DOutC, A> PairEachRespondWithRequest<DInC, DOutC>(System.Func<DOut, LanguageExt.Pipes.Proxy<RT, DIn, DOut, DInC, DOutC, A>> rhs)`
- `public LanguageExt.Pipes.Proxy<RT, DOut, DIn, UIn, UOut, A> Reflect()`
- `public LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, DIn, DOut, A> ReplaceRequest<UOutA, AUInA>(System.Func<UOut, LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, DIn, DOut, UIn>> lhs)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DInC, DOutC, A> ReplaceRespond<DInC, DOutC>(System.Func<DOut, LanguageExt.Pipes.Proxy<RT, UOut, UIn, DInC, DOutC, DIn>> rhs)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, A> ToProxy()`

### Request`6<RT, UOut, UIn, DIn, DOut, A> (class) : LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, A>

- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

- `public Request`6(UOut value, System.Func<UIn, LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, A>> next)`
- `public readonly System.Func<UIn, LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, A>> Next`
- `public readonly UOut Value`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, S> Action<S>(LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, S> r)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, S> Bind<S>(System.Func<A, LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, S>> f)`
- `public System.Void Deconstruct(out UOut& value, out System.Func<UIn, LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, A>>& fun)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, C1, C, A> For<C1, C>(System.Func<DOut, LanguageExt.Pipes.Proxy<RT, UOut, UIn, C1, C, DIn>> body)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, S> Map<S>(System.Func<A, S> f)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, A> Observe()`
- `public LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, DIn, DOut, A> PairEachRequestWithRespond<UOutA, AUInA>(System.Func<UOut, LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, UOut, UIn, A>> lhs)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DInC, DOutC, A> PairEachRespondWithRequest<DInC, DOutC>(System.Func<DOut, LanguageExt.Pipes.Proxy<RT, DIn, DOut, DInC, DOutC, A>> rhs)`
- `public LanguageExt.Pipes.Proxy<RT, DOut, DIn, UIn, UOut, A> Reflect()`
- `public LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, DIn, DOut, A> ReplaceRequest<UOutA, AUInA>(System.Func<UOut, LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, DIn, DOut, UIn>> lhs)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DInC, DOutC, A> ReplaceRespond<DInC, DOutC>(System.Func<DOut, LanguageExt.Pipes.Proxy<RT, UOut, UIn, DInC, DOutC, DIn>> rhs)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, A> ToProxy()`

### Respond`6<RT, UOut, UIn, DIn, DOut, A> (class) : LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, A>

- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

- `public Respond`6(DOut value, System.Func<DIn, LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, A>> next)`
- `public readonly System.Func<DIn, LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, A>> Next`
- `public readonly DOut Value`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, S> Action<S>(LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, S> r)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, S> Bind<S>(System.Func<A, LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, S>> f)`
- `public System.Void Deconstruct(out DOut& value, out System.Func<DIn, LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, A>>& fun)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, C1, C, A> For<C1, C>(System.Func<DOut, LanguageExt.Pipes.Proxy<RT, UOut, UIn, C1, C, DIn>> body)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, S> Map<S>(System.Func<A, S> f)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, A> Observe()`
- `public LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, DIn, DOut, A> PairEachRequestWithRespond<UOutA, AUInA>(System.Func<UOut, LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, UOut, UIn, A>> fb1)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DInC, DOutC, A> PairEachRespondWithRequest<DInC, DOutC>(System.Func<DOut, LanguageExt.Pipes.Proxy<RT, DIn, DOut, DInC, DOutC, A>> rhs)`
- `public LanguageExt.Pipes.Proxy<RT, DOut, DIn, UIn, UOut, A> Reflect()`
- `public LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, DIn, DOut, A> ReplaceRequest<UOutA, AUInA>(System.Func<UOut, LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, DIn, DOut, UIn>> lhs)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DInC, DOutC, A> ReplaceRespond<DInC, DOutC>(System.Func<DOut, LanguageExt.Pipes.Proxy<RT, UOut, UIn, DInC, DOutC, DIn>> rhs)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, A> ToProxy()`

### Server (class)

- `public Server()`
- `public static LanguageExt.Pipes.Server<RT, REQ, RES, R> Pure<RT, REQ, RES, R>(R value)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Server<RT, REQ, RES, R> lift<RT, REQ, RES, R>(LanguageExt.Aff<R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Server<RT, REQ, RES, R> lift<RT, REQ, RES, R>(LanguageExt.Eff<R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Server<RT, REQ, RES, R> lift<RT, REQ, RES, R>(LanguageExt.Aff<RT, R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Server<RT, REQ, RES, R> lift<RT, REQ, RES, R>(LanguageExt.Eff<RT, R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Server<RT, REQ, RES, LanguageExt.Unit> release<RT, REQ, RES, R>(R dispose)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Server<RT, REQ, RES, REQ> respond<RT, REQ, RES>(RES value)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Server<RT, REQ, RES, R> use<RT, REQ, RES, R>(LanguageExt.Aff<R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `where R : System.IDisposable`
- `public static LanguageExt.Pipes.Server<RT, REQ, RES, R> use<RT, REQ, RES, R>(LanguageExt.Eff<R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `where R : System.IDisposable`
- `public static LanguageExt.Pipes.Server<RT, REQ, RES, R> use<RT, REQ, RES, R>(LanguageExt.Aff<RT, R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `where R : System.IDisposable`
- `public static LanguageExt.Pipes.Server<RT, REQ, RES, R> use<RT, REQ, RES, R>(LanguageExt.Eff<RT, R> ma)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `where R : System.IDisposable`
- `public static LanguageExt.Pipes.Server<RT, REQ, RES, R> use<RT, REQ, RES, R>(LanguageExt.Aff<R> ma, System.Func<R, LanguageExt.Unit> dispose)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Server<RT, REQ, RES, R> use<RT, REQ, RES, R>(LanguageExt.Eff<R> ma, System.Func<R, LanguageExt.Unit> dispose)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Server<RT, REQ, RES, R> use<RT, REQ, RES, R>(LanguageExt.Aff<RT, R> ma, System.Func<R, LanguageExt.Unit> dispose)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public static LanguageExt.Pipes.Server<RT, REQ, RES, R> use<RT, REQ, RES, R>(LanguageExt.Eff<RT, R> ma, System.Func<R, LanguageExt.Unit> dispose)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

### Server`4<RT, REQ, RES, A> (class) : LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, REQ, RES, A>

- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

- `public Server`4(LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, REQ, RES, A> value)`
- `public readonly LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, REQ, RES, A> Value`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, REQ, RES, S> Action<S>(LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, REQ, RES, S> r)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, REQ, RES, S> Bind<S>(System.Func<A, LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, REQ, RES, S>> f)`
- `public LanguageExt.Pipes.Server<RT, REQ, RES, B> Bind<B>(System.Func<A, LanguageExt.Pipes.Server<RT, REQ, RES, B>> f)`
- `public System.Void Deconstruct(out LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, REQ, RES, A>& value)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, C1, C, A> For<C1, C>(System.Func<RES, LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, C1, C, REQ>> body)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, REQ, RES, S> Map<S>(System.Func<A, S> f)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, REQ, RES, A> Observe()`
- `public LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, REQ, RES, A> PairEachRequestWithRespond<UOutA, AUInA>(System.Func<LanguageExt.Pipes.Void, LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, LanguageExt.Pipes.Void, LanguageExt.Unit, A>> lhs)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, DInC, DOutC, A> PairEachRespondWithRequest<DInC, DOutC>(System.Func<RES, LanguageExt.Pipes.Proxy<RT, REQ, RES, DInC, DOutC, A>> rhs)`
- `public LanguageExt.Pipes.Proxy<RT, RES, REQ, LanguageExt.Unit, LanguageExt.Pipes.Void, A> Reflect()`
- `public LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, REQ, RES, A> ReplaceRequest<UOutA, AUInA>(System.Func<LanguageExt.Pipes.Void, LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, REQ, RES, LanguageExt.Unit>> lhs)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, DInC, DOutC, A> ReplaceRespond<DInC, DOutC>(System.Func<RES, LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, DInC, DOutC, REQ>> rhs)`
- `public LanguageExt.Pipes.Server<RT, REQ, RES, B> Select<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Server<RT, REQ, RES, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Server<RT, REQ, RES, B>> f)`
- `public LanguageExt.Pipes.Server<RT, REQ, RES, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Release<B>> bind)`
- `public LanguageExt.Pipes.Server<RT, REQ, RES, C> SelectMany<B, C>(System.Func<A, LanguageExt.Pipes.Server<RT, REQ, RES, B>> f, System.Func<A, B, C> project)`
- `public LanguageExt.Pipes.Server<RT, REQ, RES, C> SelectMany<B, C>(System.Func<A, LanguageExt.Pipes.Release<B>> bind, System.Func<A, B, C> project)`
- `public LanguageExt.Pipes.Proxy<RT, LanguageExt.Pipes.Void, LanguageExt.Unit, REQ, RES, A> ToProxy()`
- `public static LanguageExt.Pipes.Server<RT, REQ, RES, A> op_BitwiseAnd(LanguageExt.Pipes.Server<RT, REQ, RES, A> lhs, LanguageExt.Pipes.Server<RT, REQ, RES, A> rhs)`

### Use`6<RT, UOut, UIn, DIn, DOut, A> (class [abstract]) : LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, A>

- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`


### Use`7<RT, UOut, UIn, DIn, DOut, X, A> (class) : LanguageExt.Pipes.Use<RT, UOut, UIn, DIn, DOut, A>

- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

- `public Use`7(System.Func<LanguageExt.Aff<RT, X>> acquire, System.Func<X, LanguageExt.Unit> release, System.Func<X, LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, A>> next)`
- `public readonly System.Func<LanguageExt.Aff<RT, X>> Acquire`
- `public readonly System.Func<X, LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, A>> Next`
- `public readonly System.Func<X, LanguageExt.Unit> Release`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, B> Action<B>(LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, B> rhs)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, B> Bind<B>(System.Func<A, LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, B>> f)`
- `public System.Void Deconstruct(out System.Func<LanguageExt.Aff<RT, X>>& acquire, out System.Func<X, LanguageExt.Unit>& release, out System.Func<X, LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, A>>& next)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, C1, C, A> For<C1, C>(System.Func<DOut, LanguageExt.Pipes.Proxy<RT, UOut, UIn, C1, C, DIn>> body)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, B> Map<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, A> Observe()`
- `public LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, DIn, DOut, A> PairEachRequestWithRespond<UOutA, AUInA>(System.Func<UOut, LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, UOut, UIn, A>> lhs)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DInC, DOutC, A> PairEachRespondWithRequest<DInC, DOutC>(System.Func<DOut, LanguageExt.Pipes.Proxy<RT, DIn, DOut, DInC, DOutC, A>> rhs)`
- `public LanguageExt.Pipes.Proxy<RT, DOut, DIn, UIn, UOut, A> Reflect()`
- `public LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, DIn, DOut, A> ReplaceRequest<UOutA, AUInA>(System.Func<UOut, LanguageExt.Pipes.Proxy<RT, UOutA, AUInA, DIn, DOut, UIn>> lhs)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DInC, DOutC, A> ReplaceRespond<DInC, DOutC>(System.Func<DOut, LanguageExt.Pipes.Proxy<RT, UOut, UIn, DInC, DOutC, DIn>> rhs)`
- `public LanguageExt.Pipes.Proxy<RT, UOut, UIn, DIn, DOut, A> ToProxy()`

### Void (class)


### Yield<IN, OUT, A> (class) : LanguageExt.Pipes.Pipe<IN, OUT, A>

- `public Yield(OUT value, System.Func<LanguageExt.Unit, LanguageExt.Pipes.Pipe<IN, OUT, A>> next)`
- `public readonly System.Func<LanguageExt.Unit, LanguageExt.Pipes.Pipe<IN, OUT, A>> Next`
- `public readonly OUT Value`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, A> Interpret<RT>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<IN, OUT, B> Select<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Pipe<IN, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Pipe<IN, OUT, B>> f)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, B> SelectMany<RT, B>(System.Func<A, LanguageExt.Pipes.Pipe<RT, IN, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<IN, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Consumer<IN, B>> f)`
- `public LanguageExt.Pipes.Pipe<IN, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`

### Yield<OUT, A> (class) : LanguageExt.Pipes.Producer<OUT, A>

- `public Yield(OUT value, System.Func<LanguageExt.Unit, LanguageExt.Pipes.Producer<OUT, A>> next)`
- `public readonly System.Func<LanguageExt.Unit, LanguageExt.Pipes.Producer<OUT, A>> Next`
- `public readonly OUT Value`
- `public LanguageExt.Pipes.Producer<RT, OUT, A> Interpret<RT>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Producer<OUT, B> Select<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.Producer<OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`
- `public LanguageExt.Pipes.Producer<RT, OUT, B> SelectMany<RT, B>(System.Func<A, LanguageExt.Pipes.Producer<RT, OUT, B>> f)`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`
- `public LanguageExt.Pipes.Pipe<IN, OUT, A> ToPipe<IN>()`
- `public LanguageExt.Pipes.ProducerLift<RT, OUT, A> ToProducerLift<RT>()`
- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

### Yield<RT, OUT, A> (class) : LanguageExt.Pipes.ProducerLift<RT, OUT, A>

- `where RT : struct, LanguageExt.Effects.Traits.HasCancel<RT>`

- `public Yield(OUT value, System.Func<LanguageExt.Unit, LanguageExt.Pipes.ProducerLift<RT, OUT, A>> next)`
- `public readonly System.Func<LanguageExt.Unit, LanguageExt.Pipes.ProducerLift<RT, OUT, A>> Next`
- `public readonly OUT Value`
- `public LanguageExt.Pipes.Producer<RT, OUT, A> Interpret()`
- `public LanguageExt.Pipes.ProducerLift<RT, OUT, B> Select<B>(System.Func<A, B> f)`
- `public LanguageExt.Pipes.ProducerLift<RT, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Producer<OUT, B>> f)`
- `public LanguageExt.Pipes.ProducerLift<RT, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.ProducerLift<RT, OUT, B>> f)`
- `public LanguageExt.Pipes.Producer<RT, OUT, B> SelectMany<B>(System.Func<A, LanguageExt.Pipes.Producer<RT, OUT, B>> f)`
- `public LanguageExt.Pipes.Pipe<RT, IN, OUT, A> ToPipe<IN>()`

