# Public API inventory: language-ext v5-dev 2f0e362 LanguageExt.Traits

Assemblies: LanguageExt.Core 5.0.0.0

Type count: 150

## LanguageExt.Traits

### <G>$02C72181451E88A05CE6DF026154E386`2<$T0, $T1> (class [sealed])


### <G>$14835FBE69151A508C14E14D3EB12204`4<$T0, $T1, $T2, $T3> (class [sealed])

- `where $T2 : LanguageExt.Traits.Monad<$T2>, LanguageExt.Traits.Final<$T2>`


### <G>$18482B4AF23B2DBBCB1A68048F525A89`6<$T0, $T1, $T2, $T3, $T4, $T5> (class [sealed])

- `where $T1 : LanguageExt.Traits.Monoid<$T1>`
- `where $T4 : LanguageExt.Traits.Monad<$T4>, LanguageExt.Traits.Final<$T4>`


### <G>$25B48F39CDB93ED9CD54D748B258BF0D`4<$T0, $T1, $T2, $T3> (class [sealed])

- `where $T0 : LanguageExt.Traits.Monoid<$T0>`
- `where $T2 : LanguageExt.Traits.Monad<$T2>, LanguageExt.Traits.Final<$T2>`


### <G>$554D3B16D354AE4FC5F7F0506FA03236`4<$T0, $T1, $T2, $T3> (class [sealed])

- `where $T2 : LanguageExt.Traits.Monad<$T2>, LanguageExt.Traits.Final<$T2>`


### <G>$B9F14575F410FD0F3CB4176AAC6CE4ED`4<$T0, $T1, $T2, $T3> (class [sealed])

- `where $T2 : LanguageExt.Traits.Monad<$T2>, LanguageExt.Traits.Final<$T2>`


### <G>$C52EFD07A002A4ABD30B76A8E4DA8415`3<$T0, $T1, $T2> (class [sealed])

- `where $T1 : LanguageExt.Traits.Monad<$T1>, LanguageExt.Traits.Final<$T1>`


### <G>$C5BC5CC07178038AD76122115FA779ED`2<$T0, $T1> (class [sealed])


### <G>$C921804E826D43833DBF8DFBF5771BB2`4<$T0, $T1, $T2, $T3> (class [sealed])

- `where $T2 : LanguageExt.Traits.Monad<$T2>, LanguageExt.Traits.Final<$T2>`


### <G>$D88CC7F8CACC4380E76BCF400A682BA1`3<$T0, $T1, $T2> (class [sealed])

- `where $T1 : LanguageExt.Traits.Monad<$T1>, LanguageExt.Traits.Final<$T1>`


### <G>$DC906485226B733F6EC4FAE6B2391099`3<$T0, $T1, $T2> (class [sealed])


### <G>$EEDB5946A78DB62FEB55BC42D6A9FE90`2<$T0, $T1> (class [sealed])


### <M>$1DB3050022FE285F3172047E98B8CCB2<W, X, M, A> (class [static])

- `where W : LanguageExt.Traits.Monoid<W>`
- `where M : LanguageExt.Traits.Monad<M>, LanguageExt.Traits.Final<M>`


### <M>$21318AF91566E3EA6A137D1062550A7C<RT, X, A> (class [static])


### <M>$39349FEC06AD8BEE8405884E56F991A7<R, W, S, X, M, A> (class [static])

- `where W : LanguageExt.Traits.Monoid<W>`
- `where M : LanguageExt.Traits.Monad<M>, LanguageExt.Traits.Final<M>`


### <M>$68BE5FC74B25D179B80A0158FDCA5165<S, X, M, A> (class [static])

- `where M : LanguageExt.Traits.Monad<M>, LanguageExt.Traits.Final<M>`


### <M>$708FDA5E92264E8D4506A968533A5BA0<X, Ch, M, A> (class [static])

- `where M : LanguageExt.Traits.Monad<M>, LanguageExt.Traits.Final<M>`


### <M>$8B50CDDB8D62AD20CFF62BBD3B5F1B36<X, A> (class [static])


### <M>$A5E5A685E7EC91FDD09D2369D36ECC63<X, M, A> (class [static])

- `where M : LanguageExt.Traits.Monad<M>, LanguageExt.Traits.Final<M>`


### <M>$B611DEA96A15B957EE33F75AB7DF3F3E<X, L, M, A> (class [static])

- `where M : LanguageExt.Traits.Monad<M>, LanguageExt.Traits.Final<M>`


### <M>$B7DA4FE4EB549206F65F276FB5AA95ED<X, A> (class [static])


### <M>$C771499E3CDF8970112FA1BBE22B4CCA<X, M, A> (class [static])

- `where M : LanguageExt.Traits.Monad<M>, LanguageExt.Traits.Final<M>`


### <M>$D49D4D2EA92394924F12BF9334358D3E<X, Env, M, A> (class [static])

- `where M : LanguageExt.Traits.Monad<M>, LanguageExt.Traits.Final<M>`


### <M>$F665C6FDFAB6F406E1E9743767436468<X, A> (class [static])


### Act`2<A, B> (class [static])

- `public static System.Func<A, System.Func<B, B>> fun`

### Alternative (class [static])

- `public static LanguageExt.Traits.K<F, A> choice<F, A>(LanguageExt.Seq<LanguageExt.Traits.K<F, A>> ms)`
- `where F : LanguageExt.Traits.Alternative<F>`
- `public static LanguageExt.Traits.K<F, A> choice<F, A>(System.ReadOnlySpan<LanguageExt.Traits.K<F, A>> ms)`
- `where F : LanguageExt.Traits.Alternative<F>`
- `public static LanguageExt.Traits.K<F, LanguageExt.Either<A, B>> either<F, A, B>(LanguageExt.Traits.K<F, A> ma, LanguageExt.Traits.K<F, B> mb)`
- `where F : LanguageExt.Traits.Alternative<F>`
- `public static LanguageExt.Traits.K<F, A> empty<F, A>()`
- `where F : LanguageExt.Traits.Alternative<F>`
- `public static LanguageExt.Traits.K<F, LanguageExt.Seq<A>> endBy<F, A, SEP>(LanguageExt.Traits.K<F, A> p, LanguageExt.Traits.K<F, SEP> sep)`
- `where F : LanguageExt.Traits.Alternative<F>`
- `public static LanguageExt.Traits.K<F, LanguageExt.Seq<A>> endBy1<F, A, SEP>(LanguageExt.Traits.K<F, A> p, LanguageExt.Traits.K<F, SEP> sep)`
- `where F : LanguageExt.Traits.Alternative<F>`
- `public static LanguageExt.Traits.K<F, LanguageExt.Seq<A>> many<F, A>(LanguageExt.Traits.K<F, A> fa)`
- `where F : LanguageExt.Traits.Alternative<F>`
- `public static LanguageExt.Traits.K<F, LanguageExt.Seq<A>> manyUntil<F, A, END>(LanguageExt.Traits.K<F, A> fa, LanguageExt.Traits.K<F, END> fend)`
- `where F : LanguageExt.Traits.Alternative<F>`
- `public static LanguageExt.Traits.K<F, System.ValueTuple<LanguageExt.Seq<A>, END>> manyUntil2<F, A, END>(LanguageExt.Traits.K<F, A> fa, LanguageExt.Traits.K<F, END> fend)`
- `where F : LanguageExt.Traits.Alternative<F>`
- `public static LanguageExt.Traits.K<F, A> option<F, A>(A value, LanguageExt.Traits.K<F, A> fa)`
- `where F : LanguageExt.Traits.Alternative<F>`
- `public static LanguageExt.Traits.K<F, LanguageExt.Seq<A>> sepBy<F, A, SEP>(LanguageExt.Traits.K<F, A> fa, LanguageExt.Traits.K<F, SEP> sep)`
- `where F : LanguageExt.Traits.Alternative<F>`
- `public static LanguageExt.Traits.K<F, LanguageExt.Seq<A>> sepBy1<F, A, SEP>(LanguageExt.Traits.K<F, A> fa, LanguageExt.Traits.K<F, SEP> sep)`
- `where F : LanguageExt.Traits.Alternative<F>`
- `public static LanguageExt.Traits.K<F, LanguageExt.Seq<A>> sepByEnd<F, A, SEP>(LanguageExt.Traits.K<F, A> fa, LanguageExt.Traits.K<F, SEP> sep)`
- `where F : LanguageExt.Traits.Alternative<F>`
- `public static LanguageExt.Traits.K<F, LanguageExt.Seq<A>> sepByEnd1<F, A, SEP>(LanguageExt.Traits.K<F, A> fa, LanguageExt.Traits.K<F, SEP> sep)`
- `where F : LanguageExt.Traits.Alternative<F>`
- `public static LanguageExt.Traits.K<F, LanguageExt.Unit> skip<F, A>(System.Int32 n, LanguageExt.Traits.K<F, A> fa)`
- `where F : LanguageExt.Traits.Alternative<F>`
- `public static LanguageExt.Traits.K<F, LanguageExt.Unit> skipMany<F, A>(LanguageExt.Traits.K<F, A> fa)`
- `where F : LanguageExt.Traits.Alternative<F>`
- `public static LanguageExt.Traits.K<F, END> skipManyUntil<F, A, END>(LanguageExt.Traits.K<F, A> fa, LanguageExt.Traits.K<F, END> fend)`
- `where F : LanguageExt.Traits.Alternative<F>`
- `public static LanguageExt.Traits.K<F, LanguageExt.Unit> skipSome<F, A>(LanguageExt.Traits.K<F, A> fa)`
- `where F : LanguageExt.Traits.Alternative<F>`
- `public static LanguageExt.Traits.K<F, END> skipSomeUntil<F, A, END>(LanguageExt.Traits.K<F, A> fa, LanguageExt.Traits.K<F, END> fend)`
- `where F : LanguageExt.Traits.Alternative<F>`
- `public static LanguageExt.Traits.K<F, LanguageExt.Seq<A>> some<F, A>(LanguageExt.Traits.K<F, A> fa)`
- `where F : LanguageExt.Traits.Alternative<F>`
- `public static LanguageExt.Traits.K<F, LanguageExt.Seq<A>> someUntil<F, A, END>(LanguageExt.Traits.K<F, A> fa, LanguageExt.Traits.K<F, END> fend)`
- `where F : LanguageExt.Traits.Alternative<F>`
- `public static LanguageExt.Traits.K<F, System.ValueTuple<LanguageExt.Seq<A>, END>> someUntil2<F, A, END>(LanguageExt.Traits.K<F, A> fa, LanguageExt.Traits.K<F, END> fend)`
- `where F : LanguageExt.Traits.Alternative<F>`

### AlternativeLaw`1<F> (class [static])

- `where F : LanguageExt.Traits.Alternative<F>, LanguageExt.Traits.Applicative<F>`

- `public static LanguageExt.Unit assert(System.Func<LanguageExt.Traits.K<F, System.Int32>, LanguageExt.Traits.K<F, System.Int32>, System.Boolean> equals)`
- `public static LanguageExt.Validation<LanguageExt.Common.Error, LanguageExt.Unit> leftCatchLaw(System.Func<LanguageExt.Traits.K<F, System.Int32>, LanguageExt.Traits.K<F, System.Int32>, System.Boolean> equals)`
- `public static LanguageExt.Validation<LanguageExt.Common.Error, LanguageExt.Unit> leftZeroLaw(System.Func<LanguageExt.Traits.K<F, System.Int32>, LanguageExt.Traits.K<F, System.Int32>, System.Boolean> equals)`
- `public static LanguageExt.Validation<LanguageExt.Common.Error, LanguageExt.Unit> rightZeroLaw(System.Func<LanguageExt.Traits.K<F, System.Int32>, LanguageExt.Traits.K<F, System.Int32>, System.Boolean> equals)`
- `public static LanguageExt.Validation<LanguageExt.Common.Error, LanguageExt.Unit> validate(System.Func<LanguageExt.Traits.K<F, System.Int32>, LanguageExt.Traits.K<F, System.Int32>, System.Boolean> equals)`

### Alternative`1<F> (interface) : LanguageExt.Traits.Choice<F>, LanguageExt.Traits.Applicative<F>, LanguageExt.Traits.Functor<F>

- `where F : LanguageExt.Traits.Alternative<F>`

- `public static LanguageExt.Traits.K<F, A> Choice<A>(in LanguageExt.Seq<LanguageExt.Traits.K<F, A>>& ms)`
- `public static LanguageExt.Traits.K<F, A> Choice<A>(in System.ReadOnlySpan<LanguageExt.Traits.K<F, A>>& ms)`
- `public static LanguageExt.Traits.K<F, LanguageExt.Either<A, B>> Either<A, B>(LanguageExt.Traits.K<F, A> fa, LanguageExt.Traits.K<F, B> fb)`
- `public static LanguageExt.Traits.K<F, A> Empty<A>()`
- `public static LanguageExt.Traits.K<F, LanguageExt.Seq<A>> EndBy<A, SEP>(LanguageExt.Traits.K<F, A> p, LanguageExt.Traits.K<F, SEP> sep)`
- `public static LanguageExt.Traits.K<F, LanguageExt.Seq<A>> EndBy1<A, SEP>(LanguageExt.Traits.K<F, A> p, LanguageExt.Traits.K<F, SEP> sep)`
- `public static LanguageExt.Traits.K<F, LanguageExt.Seq<A>> Many<A>(LanguageExt.Traits.K<F, A> fa)`
- `public static LanguageExt.Traits.K<F, LanguageExt.Seq<A>> ManyUntil<A, END>(LanguageExt.Traits.K<F, A> fa, LanguageExt.Traits.K<F, END> fend)`
- `public static LanguageExt.Traits.K<F, System.ValueTuple<LanguageExt.Seq<A>, END>> ManyUntil2<A, END>(LanguageExt.Traits.K<F, A> fa, LanguageExt.Traits.K<F, END> fend)`
- `public static LanguageExt.Traits.K<F, A> Option<A>(A value, LanguageExt.Traits.K<F, A> fa)`
- `public static LanguageExt.Traits.K<F, LanguageExt.Seq<A>> SepBy<A, SEP>(LanguageExt.Traits.K<F, A> fa, LanguageExt.Traits.K<F, SEP> sep)`
- `public static LanguageExt.Traits.K<F, LanguageExt.Seq<A>> SepBy1<A, SEP>(LanguageExt.Traits.K<F, A> fa, LanguageExt.Traits.K<F, SEP> sep)`
- `public static LanguageExt.Traits.K<F, LanguageExt.Seq<A>> SepByEnd<A, SEP>(LanguageExt.Traits.K<F, A> fa, LanguageExt.Traits.K<F, SEP> sep)`
- `public static LanguageExt.Traits.K<F, LanguageExt.Seq<A>> SepByEnd1<A, SEP>(LanguageExt.Traits.K<F, A> fa, LanguageExt.Traits.K<F, SEP> sep)`
- `public static LanguageExt.Traits.K<F, LanguageExt.Unit> Skip<A>(System.Int32 n, LanguageExt.Traits.K<F, A> fa)`
- `public static LanguageExt.Traits.K<F, LanguageExt.Unit> SkipMany<A>(LanguageExt.Traits.K<F, A> fa)`
- `public static LanguageExt.Traits.K<F, END> SkipManyUntil<A, END>(LanguageExt.Traits.K<F, A> fa, LanguageExt.Traits.K<F, END> fend)`
- `public static LanguageExt.Traits.K<F, LanguageExt.Unit> SkipSome<A>(LanguageExt.Traits.K<F, A> fa)`
- `public static LanguageExt.Traits.K<F, END> SkipSomeUntil<A, END>(LanguageExt.Traits.K<F, A> fa, LanguageExt.Traits.K<F, END> fend)`
- `public static LanguageExt.Traits.K<F, LanguageExt.Seq<A>> Some<A>(LanguageExt.Traits.K<F, A> fa)`
- `public static LanguageExt.Traits.K<F, LanguageExt.Seq<A>> SomeUntil<A, END>(LanguageExt.Traits.K<F, A> fa, LanguageExt.Traits.K<F, END> fend)`
- `public static LanguageExt.Traits.K<F, System.ValueTuple<LanguageExt.Seq<A>, END>> SomeUntil2<A, END>(LanguageExt.Traits.K<F, A> fa, LanguageExt.Traits.K<F, END> fend)`

### Applicative (class [static])

- `public static LanguageExt.Traits.K<F, B> action<F, A, B>(LanguageExt.Traits.K<F, A> ma, LanguageExt.Traits.K<F, B> mb)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, B> action<F, A, B>(LanguageExt.Traits.K<F, A> ma, LanguageExt.Memo<F, B> mb)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, B> action<F, A, B>(LanguageExt.Memo<F, A> ma, LanguageExt.Memo<F, B> mb)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, B> action<F, A, B>(LanguageExt.Memo<F, A> ma, LanguageExt.Traits.K<F, B> mb)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, A> actions<F, A>(LanguageExt.IterableNE<LanguageExt.Traits.K<F, A>> ma)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, A> add<NumA, F, A>(LanguageExt.Traits.K<F, A> fa, LanguageExt.Traits.K<F, A> fb)`
- `where NumA : LanguageExt.Traits.Num<A>`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, A> add<F, A>(LanguageExt.Traits.K<F, A> fa, LanguageExt.Traits.K<F, A> fb)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `where A : System.Numerics.IAdditionOperators<A, A, A>`
- `public static LanguageExt.Traits.K<F, A> add<NumA, F, A>(LanguageExt.Memo<F, A> fa, LanguageExt.Memo<F, A> fb)`
- `where NumA : LanguageExt.Traits.Num<A>`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, A> add<F, A>(LanguageExt.Memo<F, A> fa, LanguageExt.Memo<F, A> fb)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `where A : System.Numerics.IAdditionOperators<A, A, A>`
- `public static LanguageExt.Traits.K<AF, B> apply<AF, A, B>(LanguageExt.Traits.K<AF, System.Func<A, B>> mf, LanguageExt.Traits.K<AF, A> ma)`
- `where AF : LanguageExt.Traits.Applicative<AF>`
- `public static LanguageExt.Traits.K<AF, System.Func<B, C>> apply<AF, A, B, C>(LanguageExt.Traits.K<AF, System.Func<A, B, C>> mf, LanguageExt.Traits.K<AF, A> ma)`
- `where AF : LanguageExt.Traits.Applicative<AF>`
- `public static LanguageExt.Traits.K<AF, System.Func<B, System.Func<C, D>>> apply<AF, A, B, C, D>(LanguageExt.Traits.K<AF, System.Func<A, B, C, D>> mf, LanguageExt.Traits.K<AF, A> ma)`
- `where AF : LanguageExt.Traits.Applicative<AF>`
- `public static LanguageExt.Traits.K<AF, System.Func<B, System.Func<C, System.Func<D, E>>>> apply<AF, A, B, C, D, E>(LanguageExt.Traits.K<AF, System.Func<A, B, C, D, E>> mf, LanguageExt.Traits.K<AF, A> ma)`
- `where AF : LanguageExt.Traits.Applicative<AF>`
- `public static LanguageExt.Traits.K<AF, System.Func<B, System.Func<C, System.Func<D, System.Func<E, F>>>>> apply<AF, A, B, C, D, E, F>(LanguageExt.Traits.K<AF, System.Func<A, B, C, D, E, F>> mf, LanguageExt.Traits.K<AF, A> ma)`
- `where AF : LanguageExt.Traits.Applicative<AF>`
- `public static LanguageExt.Traits.K<AF, System.Func<B, System.Func<C, System.Func<D, System.Func<E, System.Func<F, G>>>>>> apply<AF, A, B, C, D, E, F, G>(LanguageExt.Traits.K<AF, System.Func<A, B, C, D, E, F, G>> mf, LanguageExt.Traits.K<AF, A> ma)`
- `where AF : LanguageExt.Traits.Applicative<AF>`
- `public static LanguageExt.Traits.K<AF, System.Func<B, System.Func<C, System.Func<D, System.Func<E, System.Func<F, System.Func<G, H>>>>>>> apply<AF, A, B, C, D, E, F, G, H>(LanguageExt.Traits.K<AF, System.Func<A, B, C, D, E, F, G, H>> mf, LanguageExt.Traits.K<AF, A> ma)`
- `where AF : LanguageExt.Traits.Applicative<AF>`
- `public static LanguageExt.Traits.K<AF, System.Func<B, System.Func<C, System.Func<D, System.Func<E, System.Func<F, System.Func<G, System.Func<H, I>>>>>>>> apply<AF, A, B, C, D, E, F, G, H, I>(LanguageExt.Traits.K<AF, System.Func<A, B, C, D, E, F, G, H, I>> mf, LanguageExt.Traits.K<AF, A> ma)`
- `where AF : LanguageExt.Traits.Applicative<AF>`
- `public static LanguageExt.Traits.K<AF, System.Func<B, System.Func<C, System.Func<D, System.Func<E, System.Func<F, System.Func<G, System.Func<H, System.Func<I, J>>>>>>>>> apply<AF, A, B, C, D, E, F, G, H, I, J>(LanguageExt.Traits.K<AF, System.Func<A, B, C, D, E, F, G, H, I, J>> mf, LanguageExt.Traits.K<AF, A> ma)`
- `where AF : LanguageExt.Traits.Applicative<AF>`
- `public static LanguageExt.Traits.K<AF, System.Func<B, System.Func<C, System.Func<D, System.Func<E, System.Func<F, System.Func<G, System.Func<H, System.Func<I, System.Func<J, K>>>>>>>>>> apply<AF, A, B, C, D, E, F, G, H, I, J, K>(LanguageExt.Traits.K<AF, System.Func<A, B, C, D, E, F, G, H, I, J, K>> mf, LanguageExt.Traits.K<AF, A> ma)`
- `where AF : LanguageExt.Traits.Applicative<AF>`
- `public static LanguageExt.Traits.K<AF, R> apply<AF, A, B, R>(System.ValueTuple<LanguageExt.Traits.K<AF, A>, LanguageExt.Traits.K<AF, B>> items, System.Func<A, B, R> f)`
- `where AF : LanguageExt.Traits.Applicative<AF>`
- `public static LanguageExt.Traits.K<AF, R> apply<AF, A, B, C, R>(System.ValueTuple<LanguageExt.Traits.K<AF, A>, LanguageExt.Traits.K<AF, B>, LanguageExt.Traits.K<AF, C>> items, System.Func<A, B, C, R> f)`
- `where AF : LanguageExt.Traits.Applicative<AF>`
- `public static LanguageExt.Traits.K<AF, R> apply<AF, A, B, C, D, R>(System.ValueTuple<LanguageExt.Traits.K<AF, A>, LanguageExt.Traits.K<AF, B>, LanguageExt.Traits.K<AF, C>, LanguageExt.Traits.K<AF, D>> items, System.Func<A, B, C, D, R> f)`
- `where AF : LanguageExt.Traits.Applicative<AF>`
- `public static LanguageExt.Traits.K<AF, R> apply<AF, A, B, C, D, E, R>(System.ValueTuple<LanguageExt.Traits.K<AF, A>, LanguageExt.Traits.K<AF, B>, LanguageExt.Traits.K<AF, C>, LanguageExt.Traits.K<AF, D>, LanguageExt.Traits.K<AF, E>> items, System.Func<A, B, C, D, E, R> f)`
- `where AF : LanguageExt.Traits.Applicative<AF>`
- `public static LanguageExt.Traits.K<AF, R> apply<AF, A, B, C, D, E, F, R>(System.ValueTuple<LanguageExt.Traits.K<AF, A>, LanguageExt.Traits.K<AF, B>, LanguageExt.Traits.K<AF, C>, LanguageExt.Traits.K<AF, D>, LanguageExt.Traits.K<AF, E>, LanguageExt.Traits.K<AF, F>> items, System.Func<A, B, C, D, E, F, R> f)`
- `where AF : LanguageExt.Traits.Applicative<AF>`
- `public static LanguageExt.Traits.K<AF, R> apply<AF, A, B, C, D, E, F, G, R>(System.ValueTuple<LanguageExt.Traits.K<AF, A>, LanguageExt.Traits.K<AF, B>, LanguageExt.Traits.K<AF, C>, LanguageExt.Traits.K<AF, D>, LanguageExt.Traits.K<AF, E>, LanguageExt.Traits.K<AF, F>, LanguageExt.Traits.K<AF, G>> items, System.Func<A, B, C, D, E, F, G, R> f)`
- `where AF : LanguageExt.Traits.Applicative<AF>`
- `public static LanguageExt.Traits.K<AF, R> apply<AF, A, B, C, D, E, F, G, H, R>(System.ValueTuple<LanguageExt.Traits.K<AF, A>, LanguageExt.Traits.K<AF, B>, LanguageExt.Traits.K<AF, C>, LanguageExt.Traits.K<AF, D>, LanguageExt.Traits.K<AF, E>, LanguageExt.Traits.K<AF, F>, LanguageExt.Traits.K<AF, G>, System.ValueTuple<LanguageExt.Traits.K<AF, H>>> items, System.Func<A, B, C, D, E, F, G, H, R> f)`
- `where AF : LanguageExt.Traits.Applicative<AF>`
- `public static LanguageExt.Traits.K<AF, R> apply<AF, A, B, C, D, E, F, G, H, I, R>(System.ValueTuple<LanguageExt.Traits.K<AF, A>, LanguageExt.Traits.K<AF, B>, LanguageExt.Traits.K<AF, C>, LanguageExt.Traits.K<AF, D>, LanguageExt.Traits.K<AF, E>, LanguageExt.Traits.K<AF, F>, LanguageExt.Traits.K<AF, G>, System.ValueTuple<LanguageExt.Traits.K<AF, H>, LanguageExt.Traits.K<AF, I>>> items, System.Func<A, B, C, D, E, F, G, H, I, R> f)`
- `where AF : LanguageExt.Traits.Applicative<AF>`
- `public static LanguageExt.Traits.K<AF, R> apply<AF, A, B, C, D, E, F, G, H, I, J, R>(System.ValueTuple<LanguageExt.Traits.K<AF, A>, LanguageExt.Traits.K<AF, B>, LanguageExt.Traits.K<AF, C>, LanguageExt.Traits.K<AF, D>, LanguageExt.Traits.K<AF, E>, LanguageExt.Traits.K<AF, F>, LanguageExt.Traits.K<AF, G>, System.ValueTuple<LanguageExt.Traits.K<AF, H>, LanguageExt.Traits.K<AF, I>, LanguageExt.Traits.K<AF, J>>> items, System.Func<A, B, C, D, E, F, G, H, I, J, R> f)`
- `where AF : LanguageExt.Traits.Applicative<AF>`
- `public static LanguageExt.Traits.K<AF, B> apply<AF, A, B>(LanguageExt.Traits.K<AF, System.Func<A, B>> mf, LanguageExt.Memo<AF, A> ma)`
- `where AF : LanguageExt.Traits.Applicative<AF>`
- `public static LanguageExt.Traits.K<AF, System.Func<B, C>> apply<AF, A, B, C>(LanguageExt.Traits.K<AF, System.Func<A, B, C>> mf, LanguageExt.Memo<AF, A> ma)`
- `where AF : LanguageExt.Traits.Applicative<AF>`
- `public static LanguageExt.Traits.K<AF, System.Func<B, System.Func<C, D>>> apply<AF, A, B, C, D>(LanguageExt.Traits.K<AF, System.Func<A, B, C, D>> mf, LanguageExt.Memo<AF, A> ma)`
- `where AF : LanguageExt.Traits.Applicative<AF>`
- `public static LanguageExt.Traits.K<AF, System.Func<B, System.Func<C, System.Func<D, E>>>> apply<AF, A, B, C, D, E>(LanguageExt.Traits.K<AF, System.Func<A, B, C, D, E>> mf, LanguageExt.Memo<AF, A> ma)`
- `where AF : LanguageExt.Traits.Applicative<AF>`
- `public static LanguageExt.Traits.K<AF, System.Func<B, System.Func<C, System.Func<D, System.Func<E, F>>>>> apply<AF, A, B, C, D, E, F>(LanguageExt.Traits.K<AF, System.Func<A, B, C, D, E, F>> mf, LanguageExt.Memo<AF, A> ma)`
- `where AF : LanguageExt.Traits.Applicative<AF>`
- `public static LanguageExt.Traits.K<AF, System.Func<B, System.Func<C, System.Func<D, System.Func<E, System.Func<F, G>>>>>> apply<AF, A, B, C, D, E, F, G>(LanguageExt.Traits.K<AF, System.Func<A, B, C, D, E, F, G>> mf, LanguageExt.Memo<AF, A> ma)`
- `where AF : LanguageExt.Traits.Applicative<AF>`
- `public static LanguageExt.Traits.K<AF, System.Func<B, System.Func<C, System.Func<D, System.Func<E, System.Func<F, System.Func<G, H>>>>>>> apply<AF, A, B, C, D, E, F, G, H>(LanguageExt.Traits.K<AF, System.Func<A, B, C, D, E, F, G, H>> mf, LanguageExt.Memo<AF, A> ma)`
- `where AF : LanguageExt.Traits.Applicative<AF>`
- `public static LanguageExt.Traits.K<AF, System.Func<B, System.Func<C, System.Func<D, System.Func<E, System.Func<F, System.Func<G, System.Func<H, I>>>>>>>> apply<AF, A, B, C, D, E, F, G, H, I>(LanguageExt.Traits.K<AF, System.Func<A, B, C, D, E, F, G, H, I>> mf, LanguageExt.Memo<AF, A> ma)`
- `where AF : LanguageExt.Traits.Applicative<AF>`
- `public static LanguageExt.Traits.K<AF, System.Func<B, System.Func<C, System.Func<D, System.Func<E, System.Func<F, System.Func<G, System.Func<H, System.Func<I, J>>>>>>>>> apply<AF, A, B, C, D, E, F, G, H, I, J>(LanguageExt.Traits.K<AF, System.Func<A, B, C, D, E, F, G, H, I, J>> mf, LanguageExt.Memo<AF, A> ma)`
- `where AF : LanguageExt.Traits.Applicative<AF>`
- `public static LanguageExt.Traits.K<AF, System.Func<B, System.Func<C, System.Func<D, System.Func<E, System.Func<F, System.Func<G, System.Func<H, System.Func<I, System.Func<J, K>>>>>>>>>> apply<AF, A, B, C, D, E, F, G, H, I, J, K>(LanguageExt.Traits.K<AF, System.Func<A, B, C, D, E, F, G, H, I, J, K>> mf, LanguageExt.Memo<AF, A> ma)`
- `where AF : LanguageExt.Traits.Applicative<AF>`
- `public static LanguageExt.Traits.K<AF, R> apply<AF, A, B, R>(System.ValueTuple<LanguageExt.Memo<AF, A>, LanguageExt.Memo<AF, B>> items, System.Func<A, B, R> f)`
- `where AF : LanguageExt.Traits.Applicative<AF>`
- `public static LanguageExt.Traits.K<AF, R> apply<AF, A, B, C, R>(System.ValueTuple<LanguageExt.Memo<AF, A>, LanguageExt.Memo<AF, B>, LanguageExt.Memo<AF, C>> items, System.Func<A, B, C, R> f)`
- `where AF : LanguageExt.Traits.Applicative<AF>`
- `public static LanguageExt.Traits.K<AF, R> apply<AF, A, B, C, D, R>(System.ValueTuple<LanguageExt.Memo<AF, A>, LanguageExt.Memo<AF, B>, LanguageExt.Memo<AF, C>, LanguageExt.Memo<AF, D>> items, System.Func<A, B, C, D, R> f)`
- `where AF : LanguageExt.Traits.Applicative<AF>`
- `public static LanguageExt.Traits.K<AF, R> apply<AF, A, B, C, D, E, R>(System.ValueTuple<LanguageExt.Memo<AF, A>, LanguageExt.Memo<AF, B>, LanguageExt.Memo<AF, C>, LanguageExt.Memo<AF, D>, LanguageExt.Memo<AF, E>> items, System.Func<A, B, C, D, E, R> f)`
- `where AF : LanguageExt.Traits.Applicative<AF>`
- `public static LanguageExt.Traits.K<AF, R> apply<AF, A, B, C, D, E, F, R>(System.ValueTuple<LanguageExt.Memo<AF, A>, LanguageExt.Memo<AF, B>, LanguageExt.Memo<AF, C>, LanguageExt.Memo<AF, D>, LanguageExt.Memo<AF, E>, LanguageExt.Memo<AF, F>> items, System.Func<A, B, C, D, E, F, R> f)`
- `where AF : LanguageExt.Traits.Applicative<AF>`
- `public static LanguageExt.Traits.K<AF, R> apply<AF, A, B, C, D, E, F, G, R>(System.ValueTuple<LanguageExt.Memo<AF, A>, LanguageExt.Memo<AF, B>, LanguageExt.Memo<AF, C>, LanguageExt.Memo<AF, D>, LanguageExt.Memo<AF, E>, LanguageExt.Memo<AF, F>, LanguageExt.Memo<AF, G>> items, System.Func<A, B, C, D, E, F, G, R> f)`
- `where AF : LanguageExt.Traits.Applicative<AF>`
- `public static LanguageExt.Traits.K<AF, R> apply<AF, A, B, C, D, E, F, G, H, R>(System.ValueTuple<LanguageExt.Memo<AF, A>, LanguageExt.Memo<AF, B>, LanguageExt.Memo<AF, C>, LanguageExt.Memo<AF, D>, LanguageExt.Memo<AF, E>, LanguageExt.Memo<AF, F>, LanguageExt.Memo<AF, G>, System.ValueTuple<LanguageExt.Memo<AF, H>>> items, System.Func<A, B, C, D, E, F, G, H, R> f)`
- `where AF : LanguageExt.Traits.Applicative<AF>`
- `public static LanguageExt.Traits.K<AF, R> apply<AF, A, B, C, D, E, F, G, H, I, R>(System.ValueTuple<LanguageExt.Memo<AF, A>, LanguageExt.Memo<AF, B>, LanguageExt.Memo<AF, C>, LanguageExt.Memo<AF, D>, LanguageExt.Memo<AF, E>, LanguageExt.Memo<AF, F>, LanguageExt.Memo<AF, G>, System.ValueTuple<LanguageExt.Memo<AF, H>, LanguageExt.Memo<AF, I>>> items, System.Func<A, B, C, D, E, F, G, H, I, R> f)`
- `where AF : LanguageExt.Traits.Applicative<AF>`
- `public static LanguageExt.Traits.K<AF, R> apply<AF, A, B, C, D, E, F, G, H, I, J, R>(System.ValueTuple<LanguageExt.Memo<AF, A>, LanguageExt.Memo<AF, B>, LanguageExt.Memo<AF, C>, LanguageExt.Memo<AF, D>, LanguageExt.Memo<AF, E>, LanguageExt.Memo<AF, F>, LanguageExt.Memo<AF, G>, System.ValueTuple<LanguageExt.Memo<AF, H>, LanguageExt.Memo<AF, I>, LanguageExt.Memo<AF, J>>> items, System.Func<A, B, C, D, E, F, G, H, I, J, R> f)`
- `where AF : LanguageExt.Traits.Applicative<AF>`
- `public static LanguageExt.Traits.K<M, R> applyM<M, A, B, R>(System.ValueTuple<LanguageExt.Traits.K<M, A>, LanguageExt.Traits.K<M, B>> items, System.Func<A, B, LanguageExt.Traits.K<M, R>> f)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, R> applyM<M, A, B, C, R>(System.ValueTuple<LanguageExt.Traits.K<M, A>, LanguageExt.Traits.K<M, B>, LanguageExt.Traits.K<M, C>> items, System.Func<A, B, C, LanguageExt.Traits.K<M, R>> f)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, R> applyM<M, A, B, C, D, R>(System.ValueTuple<LanguageExt.Traits.K<M, A>, LanguageExt.Traits.K<M, B>, LanguageExt.Traits.K<M, C>, LanguageExt.Traits.K<M, D>> items, System.Func<A, B, C, D, LanguageExt.Traits.K<M, R>> f)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, R> applyM<M, A, B, C, D, E, R>(System.ValueTuple<LanguageExt.Traits.K<M, A>, LanguageExt.Traits.K<M, B>, LanguageExt.Traits.K<M, C>, LanguageExt.Traits.K<M, D>, LanguageExt.Traits.K<M, E>> items, System.Func<A, B, C, D, E, LanguageExt.Traits.K<M, R>> f)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, R> applyM<M, A, B, C, D, E, F, R>(System.ValueTuple<LanguageExt.Traits.K<M, A>, LanguageExt.Traits.K<M, B>, LanguageExt.Traits.K<M, C>, LanguageExt.Traits.K<M, D>, LanguageExt.Traits.K<M, E>, LanguageExt.Traits.K<M, F>> items, System.Func<A, B, C, D, E, F, LanguageExt.Traits.K<M, R>> f)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, R> applyM<M, A, B, C, D, E, F, G, R>(System.ValueTuple<LanguageExt.Traits.K<M, A>, LanguageExt.Traits.K<M, B>, LanguageExt.Traits.K<M, C>, LanguageExt.Traits.K<M, D>, LanguageExt.Traits.K<M, E>, LanguageExt.Traits.K<M, F>, LanguageExt.Traits.K<M, G>> items, System.Func<A, B, C, D, E, F, G, LanguageExt.Traits.K<M, R>> f)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, R> applyM<M, A, B, C, D, E, F, G, H, R>(System.ValueTuple<LanguageExt.Traits.K<M, A>, LanguageExt.Traits.K<M, B>, LanguageExt.Traits.K<M, C>, LanguageExt.Traits.K<M, D>, LanguageExt.Traits.K<M, E>, LanguageExt.Traits.K<M, F>, LanguageExt.Traits.K<M, G>, System.ValueTuple<LanguageExt.Traits.K<M, H>>> items, System.Func<A, B, C, D, E, F, G, H, LanguageExt.Traits.K<M, R>> f)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, R> applyM<M, A, B, C, D, E, F, G, H, I, R>(System.ValueTuple<LanguageExt.Traits.K<M, A>, LanguageExt.Traits.K<M, B>, LanguageExt.Traits.K<M, C>, LanguageExt.Traits.K<M, D>, LanguageExt.Traits.K<M, E>, LanguageExt.Traits.K<M, F>, LanguageExt.Traits.K<M, G>, System.ValueTuple<LanguageExt.Traits.K<M, H>, LanguageExt.Traits.K<M, I>>> items, System.Func<A, B, C, D, E, F, G, H, I, LanguageExt.Traits.K<M, R>> f)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, R> applyM<M, A, B, C, D, E, F, G, H, I, J, R>(System.ValueTuple<LanguageExt.Traits.K<M, A>, LanguageExt.Traits.K<M, B>, LanguageExt.Traits.K<M, C>, LanguageExt.Traits.K<M, D>, LanguageExt.Traits.K<M, E>, LanguageExt.Traits.K<M, F>, LanguageExt.Traits.K<M, G>, System.ValueTuple<LanguageExt.Traits.K<M, H>, LanguageExt.Traits.K<M, I>, LanguageExt.Traits.K<M, J>>> items, System.Func<A, B, C, D, E, F, G, H, I, J, LanguageExt.Traits.K<M, R>> f)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, R> applyM<M, A, B, R>(System.ValueTuple<LanguageExt.Memo<M, A>, LanguageExt.Memo<M, B>> items, System.Func<A, B, LanguageExt.Traits.K<M, R>> f)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, R> applyM<M, A, B, C, R>(System.ValueTuple<LanguageExt.Memo<M, A>, LanguageExt.Memo<M, B>, LanguageExt.Memo<M, C>> items, System.Func<A, B, C, LanguageExt.Traits.K<M, R>> f)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, R> applyM<M, A, B, C, D, R>(System.ValueTuple<LanguageExt.Memo<M, A>, LanguageExt.Memo<M, B>, LanguageExt.Memo<M, C>, LanguageExt.Memo<M, D>> items, System.Func<A, B, C, D, LanguageExt.Traits.K<M, R>> f)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, R> applyM<M, A, B, C, D, E, R>(System.ValueTuple<LanguageExt.Memo<M, A>, LanguageExt.Memo<M, B>, LanguageExt.Memo<M, C>, LanguageExt.Memo<M, D>, LanguageExt.Memo<M, E>> items, System.Func<A, B, C, D, E, LanguageExt.Traits.K<M, R>> f)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, R> applyM<M, A, B, C, D, E, F, R>(System.ValueTuple<LanguageExt.Memo<M, A>, LanguageExt.Memo<M, B>, LanguageExt.Memo<M, C>, LanguageExt.Memo<M, D>, LanguageExt.Memo<M, E>, LanguageExt.Memo<M, F>> items, System.Func<A, B, C, D, E, F, LanguageExt.Traits.K<M, R>> f)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, R> applyM<M, A, B, C, D, E, F, G, R>(System.ValueTuple<LanguageExt.Memo<M, A>, LanguageExt.Memo<M, B>, LanguageExt.Memo<M, C>, LanguageExt.Memo<M, D>, LanguageExt.Memo<M, E>, LanguageExt.Memo<M, F>, LanguageExt.Memo<M, G>> items, System.Func<A, B, C, D, E, F, G, LanguageExt.Traits.K<M, R>> f)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, R> applyM<M, A, B, C, D, E, F, G, H, R>(System.ValueTuple<LanguageExt.Memo<M, A>, LanguageExt.Memo<M, B>, LanguageExt.Memo<M, C>, LanguageExt.Memo<M, D>, LanguageExt.Memo<M, E>, LanguageExt.Memo<M, F>, LanguageExt.Memo<M, G>, System.ValueTuple<LanguageExt.Memo<M, H>>> items, System.Func<A, B, C, D, E, F, G, H, LanguageExt.Traits.K<M, R>> f)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, R> applyM<M, A, B, C, D, E, F, G, H, I, R>(System.ValueTuple<LanguageExt.Memo<M, A>, LanguageExt.Memo<M, B>, LanguageExt.Memo<M, C>, LanguageExt.Memo<M, D>, LanguageExt.Memo<M, E>, LanguageExt.Memo<M, F>, LanguageExt.Memo<M, G>, System.ValueTuple<LanguageExt.Memo<M, H>, LanguageExt.Memo<M, I>>> items, System.Func<A, B, C, D, E, F, G, H, I, LanguageExt.Traits.K<M, R>> f)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, R> applyM<M, A, B, C, D, E, F, G, H, I, J, R>(System.ValueTuple<LanguageExt.Memo<M, A>, LanguageExt.Memo<M, B>, LanguageExt.Memo<M, C>, LanguageExt.Memo<M, D>, LanguageExt.Memo<M, E>, LanguageExt.Memo<M, F>, LanguageExt.Memo<M, G>, System.ValueTuple<LanguageExt.Memo<M, H>, LanguageExt.Memo<M, I>, LanguageExt.Memo<M, J>>> items, System.Func<A, B, C, D, E, F, G, H, I, J, LanguageExt.Traits.K<M, R>> f)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<F, A> between<F, A, OPEN, CLOSE>(LanguageExt.Traits.K<F, OPEN> open, LanguageExt.Traits.K<F, CLOSE> close, LanguageExt.Traits.K<F, A> p)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, A> divide<NumA, F, A>(LanguageExt.Traits.K<F, A> fa, LanguageExt.Traits.K<F, A> fb)`
- `where NumA : LanguageExt.Traits.Num<A>`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, A> divide<F, A>(LanguageExt.Traits.K<F, A> fa, LanguageExt.Traits.K<F, A> fb)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `where A : System.Numerics.IDivisionOperators<A, A, A>`
- `public static LanguageExt.Traits.K<F, A> divide<NumA, F, A>(LanguageExt.Memo<F, A> fa, LanguageExt.Memo<F, A> fb)`
- `where NumA : LanguageExt.Traits.Num<A>`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, A> divide<F, A>(LanguageExt.Memo<F, A> fa, LanguageExt.Memo<F, A> fb)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `where A : System.Numerics.IDivisionOperators<A, A, A>`
- `public static LanguageExt.Traits.K<F, B> lift<F, A, B>(System.Func<A, B> f, LanguageExt.Traits.K<F, A> fa)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, B> lift<F, A, B>(System.Func<A, B> f, LanguageExt.Memo<F, A> fa)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, C> lift<F, A, B, C>(System.Func<A, B, C> f, LanguageExt.Traits.K<F, A> fa, LanguageExt.Traits.K<F, B> fb)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, C> lift<F, A, B, C>(System.Func<A, System.Func<B, C>> f, LanguageExt.Traits.K<F, A> fa, LanguageExt.Traits.K<F, B> fb)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, C> lift<F, A, B, C>(System.Func<A, B, C> f, LanguageExt.Memo<F, A> fa, LanguageExt.Memo<F, B> fb)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, C> lift<F, A, B, C>(System.Func<A, B, C> f, LanguageExt.Traits.K<F, A> fa, LanguageExt.Memo<F, B> fb)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, C> lift<F, A, B, C>(System.Func<A, System.Func<B, C>> f, LanguageExt.Memo<F, A> fa, LanguageExt.Memo<F, B> fb)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, C> lift<F, A, B, C>(System.Func<A, System.Func<B, C>> f, LanguageExt.Traits.K<F, A> fa, LanguageExt.Memo<F, B> fb)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, D> lift<F, A, B, C, D>(System.Func<A, B, C, D> f, LanguageExt.Traits.K<F, A> fa, LanguageExt.Traits.K<F, B> fb, LanguageExt.Traits.K<F, C> fc)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, D> lift<F, A, B, C, D>(System.Func<A, System.Func<B, System.Func<C, D>>> f, LanguageExt.Traits.K<F, A> fa, LanguageExt.Traits.K<F, B> fb, LanguageExt.Traits.K<F, C> fc)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, D> lift<F, A, B, C, D>(System.Func<A, B, C, D> f, LanguageExt.Memo<F, A> fa, LanguageExt.Memo<F, B> fb, LanguageExt.Memo<F, C> fc)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, D> lift<F, A, B, C, D>(System.Func<A, B, C, D> f, LanguageExt.Traits.K<F, A> fa, LanguageExt.Memo<F, B> fb, LanguageExt.Memo<F, C> fc)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, D> lift<F, A, B, C, D>(System.Func<A, B, C, D> f, LanguageExt.Memo<F, A> fa, LanguageExt.Traits.K<F, B> fb, LanguageExt.Memo<F, C> fc)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, D> lift<F, A, B, C, D>(System.Func<A, B, C, D> f, LanguageExt.Memo<F, A> fa, LanguageExt.Memo<F, B> fb, LanguageExt.Traits.K<F, C> fc)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, D> lift<F, A, B, C, D>(System.Func<A, B, C, D> f, LanguageExt.Traits.K<F, A> fa, LanguageExt.Traits.K<F, B> fb, LanguageExt.Memo<F, C> fc)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, D> lift<F, A, B, C, D>(System.Func<A, B, C, D> f, LanguageExt.Memo<F, A> fa, LanguageExt.Traits.K<F, B> fb, LanguageExt.Traits.K<F, C> fc)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, D> lift<F, A, B, C, D>(System.Func<A, System.Func<B, System.Func<C, D>>> f, LanguageExt.Memo<F, A> fa, LanguageExt.Memo<F, B> fb, LanguageExt.Memo<F, C> fc)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, D> lift<F, A, B, C, D>(System.Func<A, System.Func<B, System.Func<C, D>>> f, LanguageExt.Traits.K<F, A> fa, LanguageExt.Memo<F, B> fb, LanguageExt.Memo<F, C> fc)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, D> lift<F, A, B, C, D>(System.Func<A, System.Func<B, System.Func<C, D>>> f, LanguageExt.Memo<F, A> fa, LanguageExt.Traits.K<F, B> fb, LanguageExt.Memo<F, C> fc)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, D> lift<F, A, B, C, D>(System.Func<A, System.Func<B, System.Func<C, D>>> f, LanguageExt.Memo<F, A> fa, LanguageExt.Memo<F, B> fb, LanguageExt.Traits.K<F, C> fc)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, D> lift<F, A, B, C, D>(System.Func<A, System.Func<B, System.Func<C, D>>> f, LanguageExt.Traits.K<F, A> fa, LanguageExt.Traits.K<F, B> fb, LanguageExt.Memo<F, C> fc)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, D> lift<F, A, B, C, D>(System.Func<A, System.Func<B, System.Func<C, D>>> f, LanguageExt.Memo<F, A> fa, LanguageExt.Traits.K<F, B> fb, LanguageExt.Traits.K<F, C> fc)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, A> multiply<NumA, F, A>(LanguageExt.Traits.K<F, A> fa, LanguageExt.Traits.K<F, A> fb)`
- `where NumA : LanguageExt.Traits.Arithmetic<A>`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, A> multiply<F, A>(LanguageExt.Traits.K<F, A> fa, LanguageExt.Traits.K<F, A> fb)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `where A : System.Numerics.IMultiplyOperators<A, A, A>`
- `public static LanguageExt.Traits.K<F, A> multiply<NumA, F, A>(LanguageExt.Memo<F, A> fa, LanguageExt.Memo<F, A> fb)`
- `where NumA : LanguageExt.Traits.Arithmetic<A>`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, A> multiply<F, A>(LanguageExt.Memo<F, A> fa, LanguageExt.Memo<F, A> fb)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `where A : System.Numerics.IMultiplyOperators<A, A, A>`
- `public static LanguageExt.Traits.K<F, A> pure<F, A>(A value)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, LanguageExt.Seq<A>> replicate<F, A>(System.Int32 count, LanguageExt.Traits.K<F, A> fa)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, A> subtract<NumA, F, A>(LanguageExt.Traits.K<F, A> fa, LanguageExt.Traits.K<F, A> fb)`
- `where NumA : LanguageExt.Traits.Arithmetic<A>`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, A> subtract<F, A>(LanguageExt.Traits.K<F, A> fa, LanguageExt.Traits.K<F, A> fb)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `where A : System.Numerics.ISubtractionOperators<A, A, A>`
- `public static LanguageExt.Traits.K<F, A> subtract<NumA, F, A>(LanguageExt.Memo<F, A> fa, LanguageExt.Memo<F, A> fb)`
- `where NumA : LanguageExt.Traits.Arithmetic<A>`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, A> subtract<F, A>(LanguageExt.Memo<F, A> fa, LanguageExt.Memo<F, A> fb)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `where A : System.Numerics.ISubtractionOperators<A, A, A>`
- `public static LanguageExt.Traits.K<F, LanguageExt.Unit> unless<F>(System.Boolean flag, LanguageExt.Traits.K<F, LanguageExt.Unit> fx)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, LanguageExt.Unit> when<F>(System.Boolean flag, LanguageExt.Traits.K<F, LanguageExt.Unit> fx)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, System.ValueTuple<A, B>> zip<F, A, B>(System.ValueTuple<LanguageExt.Traits.K<F, A>, LanguageExt.Traits.K<F, B>> tuple)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, System.ValueTuple<A, B, C>> zip<F, A, B, C>(System.ValueTuple<LanguageExt.Traits.K<F, A>, LanguageExt.Traits.K<F, B>, LanguageExt.Traits.K<F, C>> tuple)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, System.ValueTuple<A, B, C, D>> zip<F, A, B, C, D>(System.ValueTuple<LanguageExt.Traits.K<F, A>, LanguageExt.Traits.K<F, B>, LanguageExt.Traits.K<F, C>, LanguageExt.Traits.K<F, D>> tuple)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, System.ValueTuple<A, B, C, D, E>> zip<F, A, B, C, D, E>(System.ValueTuple<LanguageExt.Traits.K<F, A>, LanguageExt.Traits.K<F, B>, LanguageExt.Traits.K<F, C>, LanguageExt.Traits.K<F, D>, LanguageExt.Traits.K<F, E>> tuple)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, System.ValueTuple<A, B>> zip<F, A, B>(LanguageExt.Traits.K<F, A> First, LanguageExt.Traits.K<F, B> Second)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, System.ValueTuple<A, B, C>> zip<F, A, B, C>(LanguageExt.Traits.K<F, A> First, LanguageExt.Traits.K<F, B> Second, LanguageExt.Traits.K<F, C> Third)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, System.ValueTuple<A, B, C, D>> zip<F, A, B, C, D>(LanguageExt.Traits.K<F, A> First, LanguageExt.Traits.K<F, B> Second, LanguageExt.Traits.K<F, C> Third, LanguageExt.Traits.K<F, D> Fourth)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, System.ValueTuple<A, B, C, D, E>> zip<F, A, B, C, D, E>(LanguageExt.Traits.K<F, A> First, LanguageExt.Traits.K<F, B> Second, LanguageExt.Traits.K<F, C> Third, LanguageExt.Traits.K<F, D> Fourth, LanguageExt.Traits.K<F, E> Fifth)`
- `where F : LanguageExt.Traits.Applicative<F>`

### ApplicativeLaw`1<F> (class [static])

- `where F : LanguageExt.Traits.Applicative<F>`

- `public static LanguageExt.Unit assert(System.Func<LanguageExt.Traits.K<F, System.Int32>, LanguageExt.Traits.K<F, System.Int32>, System.Boolean> equals)`
- `public static LanguageExt.Validation<LanguageExt.Common.Error, LanguageExt.Unit> compositionLaw(System.Func<LanguageExt.Traits.K<F, System.Int32>, LanguageExt.Traits.K<F, System.Int32>, System.Boolean> equals)`
- `public static LanguageExt.Validation<LanguageExt.Common.Error, LanguageExt.Unit> functorLaw(System.Func<LanguageExt.Traits.K<F, System.Int32>, LanguageExt.Traits.K<F, System.Int32>, System.Boolean> equals)`
- `public static LanguageExt.Validation<LanguageExt.Common.Error, LanguageExt.Unit> homomorphismLaw(System.Func<LanguageExt.Traits.K<F, System.Int32>, LanguageExt.Traits.K<F, System.Int32>, System.Boolean> equals)`
- `public static LanguageExt.Validation<LanguageExt.Common.Error, LanguageExt.Unit> identityLaw(System.Func<LanguageExt.Traits.K<F, System.Int32>, LanguageExt.Traits.K<F, System.Int32>, System.Boolean> equals)`
- `public static LanguageExt.Validation<LanguageExt.Common.Error, LanguageExt.Unit> interchangeLaw(System.Func<LanguageExt.Traits.K<F, System.Int32>, LanguageExt.Traits.K<F, System.Int32>, System.Boolean> equals)`
- `public static LanguageExt.Validation<LanguageExt.Common.Error, LanguageExt.Unit> validate(System.Func<LanguageExt.Traits.K<F, System.Int32>, LanguageExt.Traits.K<F, System.Int32>, System.Boolean> equals)`

### Applicative`1<F> (interface) : LanguageExt.Traits.Functor<F>

- `where F : LanguageExt.Traits.Applicative<F>`

- `public static LanguageExt.Traits.K<F, B> Action<A, B>(LanguageExt.Traits.K<F, A> ma, LanguageExt.Traits.K<F, B> mb)`
- `public static LanguageExt.Traits.K<F, B> Action<A, B>(LanguageExt.Traits.K<F, A> ma, LanguageExt.Memo<F, B> mb)`
- `public static LanguageExt.Traits.K<F, B> Action<A, B>(LanguageExt.Memo<F, A> ma, LanguageExt.Memo<F, B> mb)`
- `public static LanguageExt.Traits.K<F, B> Action<A, B>(LanguageExt.Memo<F, A> ma, LanguageExt.Traits.K<F, B> mb)`
- `public static LanguageExt.Traits.K<F, A> Actions<A>(LanguageExt.IterableNE<LanguageExt.Traits.K<F, A>> fas)`
- `public static LanguageExt.Traits.K<F, B> Apply<A, B>(LanguageExt.Traits.K<F, System.Func<A, B>> mf, LanguageExt.Traits.K<F, A> ma)`
- `public static LanguageExt.Traits.K<F, B> Apply<A, B>(LanguageExt.Traits.K<F, System.Func<A, B>> mf, LanguageExt.Memo<F, A> ma)`
- `public static LanguageExt.Traits.K<F, B> Apply<A, B>(LanguageExt.Memo<F, System.Func<A, B>> mf, LanguageExt.Memo<F, A> ma)`
- `public static LanguageExt.Traits.K<F, B> Apply<A, B>(LanguageExt.Memo<F, System.Func<A, B>> mf, LanguageExt.Traits.K<F, A> ma)`
- `public static LanguageExt.Traits.K<F, A> BackAction<A, B>(LanguageExt.Traits.K<F, A> ma, LanguageExt.Traits.K<F, B> mb)`
- `public static LanguageExt.Traits.K<F, A> BackAction<A, B>(LanguageExt.Traits.K<F, A> ma, LanguageExt.Memo<F, B> mb)`
- `public static LanguageExt.Traits.K<F, A> BackAction<A, B>(LanguageExt.Memo<F, A> ma, LanguageExt.Memo<F, B> mb)`
- `public static LanguageExt.Traits.K<F, A> BackAction<A, B>(LanguageExt.Memo<F, A> ma, LanguageExt.Traits.K<F, B> mb)`
- `public static LanguageExt.Traits.K<F, A> Between<A, OPEN, CLOSE>(LanguageExt.Traits.K<F, OPEN> open, LanguageExt.Traits.K<F, CLOSE> close, LanguageExt.Traits.K<F, A> p)`
- `public static LanguageExt.Traits.K<F, A> Pure<A>(A value)`
- `public static LanguageExt.Traits.K<F, LanguageExt.Seq<A>> Replicate<A>(System.Int32 count, LanguageExt.Traits.K<F, A> fa)`

### Arithmetic`1<A> (interface) : LanguageExt.Traits.Trait

- `public static A Add(A x, A y)`
- `public static A Multiply(A x, A y)`
- `public static A Negate(A x)`
- `public static A Subtract(A x, A y)`

### Biapplicative`1<FF> (interface) : LanguageExt.Traits.Bifunctor<FF>

- `where FF : LanguageExt.Traits.Biapplicative<FF>`

- `public static LanguageExt.Traits.K<FF, C, D> BiApply<A, B, C, D>(LanguageExt.Traits.K<FF, System.Func<A, C>, System.Func<B, D>> ff, LanguageExt.Traits.K<FF, A, B> fab)`
- `public static LanguageExt.Traits.K<FF, System.Func<C, E>, System.Func<D, F>> BiApply<A, B, C, D, E, F>(LanguageExt.Traits.K<FF, System.Func<A, C, E>, System.Func<B, D, F>> ff, LanguageExt.Traits.K<FF, A, B> fab)`
- `public static LanguageExt.Traits.K<FF, E, F> BiApply<A, B, C, D, E, F>(LanguageExt.Traits.K<FF, System.Func<A, C, E>, System.Func<B, D, F>> ff, LanguageExt.Traits.K<FF, A, B> fab, LanguageExt.Traits.K<FF, C, D> fcd)`

### Bifunctor (class [static])

- `public static LanguageExt.Traits.K<F, Q, B> bimap<F, P, A, Q, B>(System.Func<P, Q> first, System.Func<A, B> second, LanguageExt.Traits.K<F, P, A> fab)`
- `where F : LanguageExt.Traits.Bifunctor<F>`
- `public static LanguageExt.Traits.K<F, Q, A> first<F, P, A, Q>(System.Func<P, Q> first, LanguageExt.Traits.K<F, P, A> fab)`
- `where F : LanguageExt.Traits.Bifunctor<F>`
- `public static LanguageExt.Traits.K<F, P, B> second<F, P, A, B>(System.Func<A, B> second, LanguageExt.Traits.K<F, P, A> fab)`
- `where F : LanguageExt.Traits.Bifunctor<F>`

### BifunctorExtensions (class [static])

- `[ext] public static LanguageExt.Traits.K<F, M, B> BiMap<F, L, A, M, B>(this LanguageExt.Traits.K<F, L, A> fab, System.Func<L, M> First, System.Func<A, B> Second)`
- `where F : LanguageExt.Traits.Bifunctor<F>`
- `[ext] public static LanguageExt.Traits.K<F, System.Func<M, N>, System.Func<B, C>> BiMap<F, L, M, N, A, B, C>(this LanguageExt.Traits.K<F, L, A> ma, System.Func<L, M, N> First, System.Func<A, B, C> Second)`
- `where F : LanguageExt.Traits.Bifunctor<F>`
- `[ext] public static LanguageExt.Traits.K<F, System.Func<M, System.Func<N, O>>, System.Func<B, System.Func<C, D>>> BiMap<F, L, M, N, O, A, B, C, D>(this LanguageExt.Traits.K<F, L, A> ma, System.Func<L, M, N, O> First, System.Func<A, B, C, D> Second)`
- `where F : LanguageExt.Traits.Bifunctor<F>`
- `[ext] public static LanguageExt.Traits.K<F, System.Func<M, System.Func<N, System.Func<O, P>>>, System.Func<B, System.Func<C, System.Func<D, E>>>> BiMap<F, L, M, N, O, P, A, B, C, D, E>(this LanguageExt.Traits.K<F, L, A> ma, System.Func<L, M, N, O, P> First, System.Func<A, B, C, D, E> Second)`
- `where F : LanguageExt.Traits.Bifunctor<F>`
- `[ext] public static LanguageExt.Traits.K<Fnctr, System.Func<M, System.Func<N, System.Func<O, System.Func<P, Q>>>>, System.Func<B, System.Func<C, System.Func<D, System.Func<E, F>>>>> BiMap<Fnctr, L, M, N, O, P, Q, A, B, C, D, E, F>(this LanguageExt.Traits.K<Fnctr, L, A> ma, System.Func<L, M, N, O, P, Q> First, System.Func<A, B, C, D, E, F> Second)`
- `where Fnctr : LanguageExt.Traits.Bifunctor<Fnctr>`
- `[ext] public static LanguageExt.Traits.K<F, M, A> MapFirst<F, L, A, M>(this LanguageExt.Traits.K<F, L, A> fab, System.Func<L, M> first)`
- `where F : LanguageExt.Traits.Bifunctor<F>`
- `[ext] public static LanguageExt.Traits.K<F, System.Func<M, N>, A> MapFirst<F, L, A, M, N>(this LanguageExt.Traits.K<F, L, A> fab, System.Func<L, M, N> first)`
- `where F : LanguageExt.Traits.Bifunctor<F>`
- `[ext] public static LanguageExt.Traits.K<F, System.Func<M, System.Func<N, O>>, A> MapFirst<F, L, A, M, N, O>(this LanguageExt.Traits.K<F, L, A> fab, System.Func<L, M, N, O> first)`
- `where F : LanguageExt.Traits.Bifunctor<F>`
- `[ext] public static LanguageExt.Traits.K<F, System.Func<M, System.Func<N, System.Func<O, P>>>, A> MapFirst<F, L, A, M, N, O, P>(this LanguageExt.Traits.K<F, L, A> fab, System.Func<L, M, N, O, P> first)`
- `where F : LanguageExt.Traits.Bifunctor<F>`
- `[ext] public static LanguageExt.Traits.K<F, System.Func<M, System.Func<N, System.Func<O, System.Func<P, Q>>>>, A> MapFirst<F, L, A, M, N, O, P, Q>(this LanguageExt.Traits.K<F, L, A> fab, System.Func<L, M, N, O, P, Q> first)`
- `where F : LanguageExt.Traits.Bifunctor<F>`
- `[ext] public static LanguageExt.Traits.K<F, M, A> MapFirst<F, L, A, M>(this System.Func<L, M> first, LanguageExt.Traits.K<F, L, A> fab)`
- `where F : LanguageExt.Traits.Bifunctor<F>`
- `[ext] public static LanguageExt.Traits.K<F, System.Func<M, N>, A> MapFirst<F, L, A, M, N>(this System.Func<L, M, N> first, LanguageExt.Traits.K<F, L, A> fab)`
- `where F : LanguageExt.Traits.Bifunctor<F>`
- `[ext] public static LanguageExt.Traits.K<F, System.Func<M, System.Func<N, O>>, A> MapFirst<F, L, A, M, N, O>(this System.Func<L, M, N, O> first, LanguageExt.Traits.K<F, L, A> fab)`
- `where F : LanguageExt.Traits.Bifunctor<F>`
- `[ext] public static LanguageExt.Traits.K<F, System.Func<M, System.Func<N, System.Func<O, P>>>, A> MapFirst<F, L, A, M, N, O, P>(this System.Func<L, M, N, O, P> first, LanguageExt.Traits.K<F, L, A> fab)`
- `where F : LanguageExt.Traits.Bifunctor<F>`
- `[ext] public static LanguageExt.Traits.K<F, System.Func<M, System.Func<N, System.Func<O, System.Func<P, Q>>>>, A> MapFirst<F, L, A, M, N, O, P, Q>(this System.Func<L, M, N, O, P, Q> first, LanguageExt.Traits.K<F, L, A> fab)`
- `where F : LanguageExt.Traits.Bifunctor<F>`
- `[ext] public static LanguageExt.Traits.K<F, L, B> MapSecond<F, L, A, B>(this LanguageExt.Traits.K<F, L, A> fab, System.Func<A, B> second)`
- `where F : LanguageExt.Traits.Bifunctor<F>`
- `[ext] public static LanguageExt.Traits.K<F, L, System.Func<B, C>> MapSecond<F, L, A, B, C>(this LanguageExt.Traits.K<F, L, A> fab, System.Func<A, B, C> second)`
- `where F : LanguageExt.Traits.Bifunctor<F>`
- `[ext] public static LanguageExt.Traits.K<F, L, System.Func<B, System.Func<C, D>>> MapSecond<F, L, A, B, C, D>(this LanguageExt.Traits.K<F, L, A> fab, System.Func<A, B, C, D> second)`
- `where F : LanguageExt.Traits.Bifunctor<F>`
- `[ext] public static LanguageExt.Traits.K<F, L, System.Func<B, System.Func<C, System.Func<D, E>>>> MapSecond<F, L, A, B, C, D, E>(this LanguageExt.Traits.K<F, L, A> fab, System.Func<A, B, C, D, E> second)`
- `where F : LanguageExt.Traits.Bifunctor<F>`
- `[ext] public static LanguageExt.Traits.K<BF, L, System.Func<B, System.Func<C, System.Func<D, System.Func<E, F>>>>> MapSecond<BF, L, A, B, C, D, E, F>(this LanguageExt.Traits.K<BF, L, A> fab, System.Func<A, B, C, D, E, F> second)`
- `where BF : LanguageExt.Traits.Bifunctor<BF>`
- `[ext] public static LanguageExt.Traits.K<F, L, B> MapSecond<F, L, A, B>(this System.Func<A, B> second, LanguageExt.Traits.K<F, L, A> fab)`
- `where F : LanguageExt.Traits.Bifunctor<F>`
- `[ext] public static LanguageExt.Traits.K<F, L, System.Func<B, C>> MapSecond<F, L, A, B, C>(this System.Func<A, B, C> second, LanguageExt.Traits.K<F, L, A> fab)`
- `where F : LanguageExt.Traits.Bifunctor<F>`
- `[ext] public static LanguageExt.Traits.K<F, L, System.Func<B, System.Func<C, D>>> MapSecond<F, L, A, B, C, D>(this System.Func<A, B, C, D> second, LanguageExt.Traits.K<F, L, A> fab)`
- `where F : LanguageExt.Traits.Bifunctor<F>`
- `[ext] public static LanguageExt.Traits.K<F, L, System.Func<B, System.Func<C, System.Func<D, E>>>> MapSecond<F, L, A, B, C, D, E>(this System.Func<A, B, C, D, E> second, LanguageExt.Traits.K<F, L, A> fab)`
- `where F : LanguageExt.Traits.Bifunctor<F>`
- `[ext] public static LanguageExt.Traits.K<BF, L, System.Func<B, System.Func<C, System.Func<D, System.Func<E, F>>>>> MapSecond<BF, L, A, B, C, D, E, F>(this System.Func<A, B, C, D, E, F> second, LanguageExt.Traits.K<BF, L, A> fab)`
- `where BF : LanguageExt.Traits.Bifunctor<BF>`

### Bifunctor`1<F> (interface)

- `where F : LanguageExt.Traits.Bifunctor<F>`

- `public static LanguageExt.Traits.K<F, M, B> BiMap<L, A, M, B>(System.Func<L, M> first, System.Func<A, B> second, LanguageExt.Traits.K<F, L, A> fab)`
- `public static LanguageExt.Traits.K<F, M, A> MapFirst<L, A, M>(System.Func<L, M> first, LanguageExt.Traits.K<F, L, A> fab)`
- `public static LanguageExt.Traits.K<F, L, B> MapSecond<L, A, B>(System.Func<A, B> second, LanguageExt.Traits.K<F, L, A> fab)`

### Bimonad`1<M> (interface) : LanguageExt.Traits.Bifunctor<M>

- `where M : LanguageExt.Traits.Bimonad<M>`

- `public static LanguageExt.Traits.K<M, Y, A> BindFirst<X, Y, A>(LanguageExt.Traits.K<M, X, A> ma, System.Func<X, LanguageExt.Traits.K<M, Y, A>> f)`
- `public static LanguageExt.Traits.K<M, X, B> BindSecond<X, A, B>(LanguageExt.Traits.K<M, X, A> ma, System.Func<A, LanguageExt.Traits.K<M, X, B>> f)`
- `public static LanguageExt.Traits.K<M, X, A> FlattenFirst<X, A>(LanguageExt.Traits.K<M, LanguageExt.Traits.K<M, X, A>, A> mma)`
- `public static LanguageExt.Traits.K<M, X, A> FlattenSecond<X, A>(LanguageExt.Traits.K<M, X, LanguageExt.Traits.K<M, X, A>> mma)`

### Bool`1<A> (interface) : LanguageExt.Traits.Trait

- `public static A And(A a, A b)`
- `public static A BiCondition(A a, A b)`
- `public static A False()`
- `public static A Implies(A a, A b)`
- `public static A Not(A a)`
- `public static A Or(A a, A b)`
- `public static A True()`
- `public static A XOr(A a, A b)`

### Cached`1<F, A> (class [static])

- `where F : LanguageExt.Traits.Alternative<F>`

- `public static System.Func<A, System.Func<LanguageExt.Seq<A>, LanguageExt.Seq<A>>> cons`

### Choice (class [static])

- `public static LanguageExt.Traits.K<F, A> choose<F, A>(LanguageExt.Traits.K<F, A> fa, LanguageExt.Traits.K<F, A> fb)`
- `where F : LanguageExt.Traits.Choice<F>`
- `public static LanguageExt.Traits.K<F, LanguageExt.Seq<A>> many<F, A>(LanguageExt.Traits.K<F, A> fa)`
- `where F : LanguageExt.Traits.Choice<F>, LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, LanguageExt.Seq<A>> some<F, A>(LanguageExt.Traits.K<F, A> fa)`
- `where F : LanguageExt.Traits.Choice<F>, LanguageExt.Traits.Applicative<F>`

### ChoiceLaw`1<F> (class [static])

- `where F : LanguageExt.Traits.Choice<F>, LanguageExt.Traits.Applicative<F>`

- `public static LanguageExt.Unit assert(LanguageExt.Traits.K<F, System.Int32> failure, System.Func<LanguageExt.Traits.K<F, System.Int32>, LanguageExt.Traits.K<F, System.Int32>, System.Boolean> equals)`
- `public static LanguageExt.Validation<LanguageExt.Common.Error, LanguageExt.Unit> leftCatchLaw(System.Func<LanguageExt.Traits.K<F, System.Int32>, LanguageExt.Traits.K<F, System.Int32>, System.Boolean> equals)`
- `public static LanguageExt.Validation<LanguageExt.Common.Error, LanguageExt.Unit> leftZeroLaw(LanguageExt.Traits.K<F, System.Int32> failure, System.Func<LanguageExt.Traits.K<F, System.Int32>, LanguageExt.Traits.K<F, System.Int32>, System.Boolean> equals)`
- `public static LanguageExt.Validation<LanguageExt.Common.Error, LanguageExt.Unit> rightZeroLaw(LanguageExt.Traits.K<F, System.Int32> failure, System.Func<LanguageExt.Traits.K<F, System.Int32>, LanguageExt.Traits.K<F, System.Int32>, System.Boolean> equals)`
- `public static LanguageExt.Validation<LanguageExt.Common.Error, LanguageExt.Unit> validate(LanguageExt.Traits.K<F, System.Int32> failure, System.Func<LanguageExt.Traits.K<F, System.Int32>, LanguageExt.Traits.K<F, System.Int32>, System.Boolean> equals)`

### Choice`1<F> (interface)

- `where F : LanguageExt.Traits.Choice<F>`

- `public static LanguageExt.Traits.K<F, A> Choose<A>(LanguageExt.Traits.K<F, A> fa, LanguageExt.Traits.K<F, A> fb)`
- `public static LanguageExt.Traits.K<F, A> Choose<A>(LanguageExt.Traits.K<F, A> fa, LanguageExt.Memo<F, A> fb)`

### ChronicalerExtensions (class [static])

- `[ext] public static LanguageExt.Traits.K<M, A> Absolve<Ch, M, A>(this LanguageExt.Traits.K<M, A> ma, A defaultValue)`
- `where M : LanguageExt.Traits.Chronicaler<M, Ch>`
- `[ext] public static LanguageExt.Traits.K<M, A> Censor<Ch, M, A>(this LanguageExt.Traits.K<M, A> ma, System.Func<Ch, Ch> f)`
- `where M : LanguageExt.Traits.Chronicaler<M, Ch>`
- `[ext] public static LanguageExt.Traits.K<M, A> Chronicle<Ch, M, A>(this LanguageExt.These<Ch, A> ma)`
- `where M : LanguageExt.Traits.Chronicaler<M, Ch>`
- `[ext] public static LanguageExt.Traits.K<M, A> Condemn<Ch, M, A>(this LanguageExt.Traits.K<M, A> ma)`
- `where M : LanguageExt.Traits.Chronicaler<M, Ch>`
- `[ext] public static LanguageExt.Traits.K<M, LanguageExt.Either<Ch, A>> Memento<Ch, M, A>(this LanguageExt.Traits.K<M, A> ma)`
- `where M : LanguageExt.Traits.Chronicaler<M, Ch>`

### Chronicaler`1<Ch> (class [static])

- `public static LanguageExt.Traits.K<M, A> absolve<M, A>(A defaultValue, LanguageExt.Traits.K<M, A> ma)`
- `where M : LanguageExt.Traits.Chronicaler<M, Ch>`
- `public static LanguageExt.Traits.K<M, A> censor<M, A>(System.Func<Ch, Ch> f, LanguageExt.Traits.K<M, A> ma)`
- `where M : LanguageExt.Traits.Chronicaler<M, Ch>`
- `public static LanguageExt.Traits.K<M, A> chronicle<M, A>(LanguageExt.These<Ch, A> ma)`
- `where M : LanguageExt.Traits.Chronicaler<M, Ch>`
- `public static LanguageExt.Traits.K<M, A> condemn<M, A>(LanguageExt.Traits.K<M, A> ma)`
- `where M : LanguageExt.Traits.Chronicaler<M, Ch>`
- `public static LanguageExt.Traits.K<M, A> confess<M, A>(Ch confession)`
- `where M : LanguageExt.Traits.Chronicaler<M, Ch>`
- `public static LanguageExt.Traits.K<M, A> dictate<M, A>(A value)`
- `where M : LanguageExt.Traits.Chronicaler<M, Ch>`
- `public static LanguageExt.Traits.K<M, LanguageExt.Either<Ch, A>> memento<M, A>(LanguageExt.Traits.K<M, A> ma)`
- `where M : LanguageExt.Traits.Chronicaler<M, Ch>`

### Chronicaler`2<M, Ch> (interface)

- `where M : LanguageExt.Traits.Chronicaler<M, Ch>`

- `public static LanguageExt.Traits.K<M, A> Absolve<A>(A defaultValue, LanguageExt.Traits.K<M, A> ma)`
- `public static LanguageExt.Traits.K<M, A> Censor<A>(System.Func<Ch, Ch> f, LanguageExt.Traits.K<M, A> ma)`
- `public static LanguageExt.Traits.K<M, A> Chronicle<A>(LanguageExt.These<Ch, A> ma)`
- `public static LanguageExt.Traits.K<M, A> Condemn<A>(LanguageExt.Traits.K<M, A> ma)`
- `public static LanguageExt.Traits.K<M, A> Confess<A>(Ch confession)`
- `public static LanguageExt.Traits.K<M, A> Dictate<A>(A value)`
- `public static LanguageExt.Traits.K<M, LanguageExt.Either<Ch, A>> Memento<A>(LanguageExt.Traits.K<M, A> ma)`

### ChronicleTExtensions (class [static])

- `public static LanguageExt.ChronicleT<Ch, M, A> op_BitwiseOr<X, Ch, M, A>(LanguageExt.Traits.K<LanguageExt.ChronicleT<Ch, M>, A> lhs, LanguageExt.Finally<M, X> rhs)`
- `where M : LanguageExt.Traits.Monad<M>, LanguageExt.Traits.Final<M>`

### CoNatural (class [static])

- `public static LanguageExt.Traits.K<F, A> transform<F, G, A>(LanguageExt.Traits.K<G, A> fa)`
- `where F : LanguageExt.Traits.CoNatural<F, G>`

### CoNatural`2<F, G> (interface)

- `public static LanguageExt.Traits.K<F, A> CoTransform<A>(LanguageExt.Traits.K<G, A> fa)`

### Cofunctor (class [static])

- `public static LanguageExt.Traits.K<F, A> contraMap<F, A, B>(System.Func<A, B> f, LanguageExt.Traits.K<F, B> fb)`
- `where F : LanguageExt.Traits.Cofunctor<F>`

### CofunctorExtensions (class [static])

- `[ext] public static LanguageExt.Traits.K<F, A> Comap<F, A, B>(this LanguageExt.Traits.K<F, B> fb, System.Func<A, B> f)`
- `where F : LanguageExt.Traits.Cofunctor<F>`
- `[ext] public static LanguageExt.Traits.K<F, A> Comap<F, A, B>(this System.Func<A, B> f, LanguageExt.Traits.K<F, B> fb)`
- `where F : LanguageExt.Traits.Cofunctor<F>`

### Cofunctor`1<F> (interface)

- `public static LanguageExt.Traits.K<F, A> Comap<A, B>(System.Func<A, B> f, LanguageExt.Traits.K<F, B> fb)`

### Const`1<TYPE> (interface) : LanguageExt.Traits.Trait

- `public static TYPE Value { get; }`

### Coproduct (class [static])

- `public static LanguageExt.Traits.K<F, A, B> left<F, A, B>(A value)`
- `where F : LanguageExt.Traits.Coproduct<F>`
- `public static System.ValueTuple<LanguageExt.Seq<A>, LanguageExt.Seq<B>> partition<FF, F, A, B>(LanguageExt.Traits.K<FF, LanguageExt.Traits.K<F, A, B>> fabs)`
- `where FF : LanguageExt.Traits.Foldable<FF>`
- `where F : LanguageExt.Traits.Coproduct<F>`
- `public static System.ValueTuple<LanguageExt.Seq<A>, LanguageExt.Seq<B>> partitionSequence<F, A, B>(System.Collections.Generic.IEnumerable<LanguageExt.Traits.K<F, A, B>> fabs)`
- `where F : LanguageExt.Traits.Coproduct<F>`
- `public static LanguageExt.Traits.K<F, A, B> right<F, A, B>(B value)`
- `where F : LanguageExt.Traits.Coproduct<F>`

### CoproductCons (class [static])

- `public static LanguageExt.Traits.K<F, A, B> left<F, A, B>(A value)`
- `where F : LanguageExt.Traits.CoproductCons<F>`
- `public static LanguageExt.Traits.K<F, A, B> right<F, A, B>(B value)`
- `where F : LanguageExt.Traits.CoproductCons<F>`

### CoproductCons`1<F> (interface)

- `where F : LanguageExt.Traits.CoproductCons<F>`

- `public static LanguageExt.Traits.K<F, A, B> Left<A, B>(A value)`
- `public static LanguageExt.Traits.K<F, A, B> Right<A, B>(B value)`

### CoproductK (class [static])

- `public static LanguageExt.Traits.K<F, A, B> left<F, A, B>(A value)`
- `where F : LanguageExt.Traits.CoproductK<F>`
- `public static LanguageExt.Traits.K<F, A, System.ValueTuple<LanguageExt.Seq<A>, LanguageExt.Seq<B>>> partition<FF, F, A, B>(LanguageExt.Traits.K<FF, LanguageExt.Traits.K<F, A, B>> fabs)`
- `where FF : LanguageExt.Traits.Foldable<FF>`
- `where F : LanguageExt.Traits.CoproductK<F>, LanguageExt.Traits.Bimonad<F>`
- `public static LanguageExt.Traits.K<F, A, System.ValueTuple<LanguageExt.Seq<A>, LanguageExt.Seq<B>>> partitionSequence<F, A, B>(System.Collections.Generic.IEnumerable<LanguageExt.Traits.K<F, A, B>> fabs)`
- `where F : LanguageExt.Traits.CoproductK<F>, LanguageExt.Traits.Bimonad<F>`
- `public static LanguageExt.Traits.K<F, A, B> right<F, A, B>(B value)`
- `where F : LanguageExt.Traits.CoproductK<F>`

### CoproductK`1<F> (interface) : LanguageExt.Traits.CoproductCons<F>

- `where F : LanguageExt.Traits.CoproductK<F>`

- `public static LanguageExt.Traits.K<F, A, B> IfLeft<A, B>(System.Func<A, B> Left, LanguageExt.Traits.K<F, A, B> fab)`
- `public static LanguageExt.Traits.K<F, A, B> IfLeft<A, B>(B Left, LanguageExt.Traits.K<F, A, B> fab)`
- `public static LanguageExt.Traits.K<F, A, A> IfRight<A, B>(System.Func<B, A> Right, LanguageExt.Traits.K<F, A, B> fab)`
- `public static LanguageExt.Traits.K<F, A, A> IfRight<A, B>(A Right, LanguageExt.Traits.K<F, A, B> fab)`
- `public static LanguageExt.Traits.K<F, A, C> Match<A, B, C>(System.Func<A, C> Left, System.Func<B, C> Right, LanguageExt.Traits.K<F, A, B> fab)`
- `public static LanguageExt.Traits.K<F, A, C> Match<A, B, C>(C Left, System.Func<B, C> Right, LanguageExt.Traits.K<F, A, B> fab)`
- `public static LanguageExt.Traits.K<F, A, C> Match<A, B, C>(System.Func<A, C> Left, C Right, LanguageExt.Traits.K<F, A, B> fab)`

### Coproduct`1<F> (interface) : LanguageExt.Traits.CoproductCons<F>

- `where F : LanguageExt.Traits.Coproduct<F>`

- `public static B IfLeft<A, B>(System.Func<A, B> Left, LanguageExt.Traits.K<F, A, B> fab)`
- `public static B IfLeft<A, B>(B Left, LanguageExt.Traits.K<F, A, B> fab)`
- `public static A IfRight<A, B>(System.Func<B, A> Right, LanguageExt.Traits.K<F, A, B> fab)`
- `public static A IfRight<A, B>(A Right, LanguageExt.Traits.K<F, A, B> fab)`
- `public static LanguageExt.Seq<A> Lefts<G, A, B>(LanguageExt.Traits.K<G, LanguageExt.Traits.K<F, A, B>> fabs)`
- `where G : LanguageExt.Traits.Foldable<G>`
- `public static C Match<A, B, C>(System.Func<A, C> Left, System.Func<B, C> Right, LanguageExt.Traits.K<F, A, B> fab)`
- `public static C Match<A, B, C>(C Left, System.Func<B, C> Right, LanguageExt.Traits.K<F, A, B> fab)`
- `public static C Match<A, B, C>(System.Func<A, C> Left, C Right, LanguageExt.Traits.K<F, A, B> fab)`
- `public static System.ValueTuple<LanguageExt.Seq<A>, LanguageExt.Seq<B>> Partition<FF, A, B>(LanguageExt.Traits.K<FF, LanguageExt.Traits.K<F, A, B>> fabs)`
- `where FF : LanguageExt.Traits.Foldable<FF>`
- `public static LanguageExt.Seq<B> Rights<G, A, B>(LanguageExt.Traits.K<G, LanguageExt.Traits.K<F, A, B>> fabs)`
- `where G : LanguageExt.Traits.Foldable<G>`

### Coreadable (class [static])

- `public static LanguageExt.Traits.K<M, Env, Env> ask<M, Env>()`
- `where M : LanguageExt.Traits.Coreadable<M>`
- `public static LanguageExt.Traits.K<M, Env, A> asks<M, Env, A>(System.Func<Env, A> f)`
- `where M : LanguageExt.Traits.Coreadable<M>`
- `public static LanguageExt.Traits.K<M, Env, A> asksM<M, Env, A>(System.Func<Env, LanguageExt.Traits.K<M, Env, A>> f)`
- `where M : LanguageExt.Traits.Coreadable<M>, LanguageExt.Traits.Bimonad<M>`
- `public static LanguageExt.Traits.K<M, Env1, A> local<M, Env, Env1, A>(System.Func<Env, Env1> f, LanguageExt.Traits.K<M, Env, A> ma)`
- `where M : LanguageExt.Traits.Coreadable<M>`

### Coreadable`1<M> (interface)

- `where M : LanguageExt.Traits.Coreadable<M>`

- `public static LanguageExt.Traits.K<M, Env, Env> Ask<Env>()`
- `public static LanguageExt.Traits.K<M, Env, A> Asks<Env, A>(System.Func<Env, A> f)`
- `public static LanguageExt.Traits.K<M, Env1, A> Local<Env, Env1, A>(System.Func<Env, Env1> f, LanguageExt.Traits.K<M, Env, A> ma)`

### Decidable (class [static])

- `public static LanguageExt.Traits.K<F, A> lose<F, A>(System.Func<A, LanguageExt.Void> f)`
- `where F : LanguageExt.Traits.Decidable<F>`
- `public static LanguageExt.Traits.K<F, LanguageExt.Void> lost<F>()`
- `where F : LanguageExt.Traits.Decidable<F>`
- `public static LanguageExt.Traits.K<F, LanguageExt.Either<A, B>> route<F, A, B>(LanguageExt.Traits.K<F, A> fa, LanguageExt.Traits.K<F, B> fb)`
- `where F : LanguageExt.Traits.Decidable<F>`
- `public static LanguageExt.Traits.K<F, A> route<F, A, B, C>(System.Func<A, LanguageExt.Either<B, C>> f, LanguageExt.Traits.K<F, B> fb, LanguageExt.Traits.K<F, C> fc)`
- `where F : LanguageExt.Traits.Decidable<F>`

### Decidable`1<F> (interface) : LanguageExt.Traits.Divisible<F>, LanguageExt.Traits.Cofunctor<F>

- `public static LanguageExt.Traits.K<F, A> Lose<A>(System.Func<A, LanguageExt.Void> f)`
- `public static LanguageExt.Traits.K<F, A> Route<A, B, C>(System.Func<A, LanguageExt.Either<B, C>> f, LanguageExt.Traits.K<F, B> fb, LanguageExt.Traits.K<F, C> fc)`

### Deriving`2<Supertype, Subtype> (interface) : LanguageExt.Traits.NaturalIso<Supertype, Subtype>, LanguageExt.Traits.Natural<Supertype, Subtype>, LanguageExt.Traits.CoNatural<Supertype, Subtype>


### Divisible (class [static])

- `public static LanguageExt.Traits.K<F, A> conquer<F, A>()`
- `where F : LanguageExt.Traits.Divisible<F>`
- `public static LanguageExt.Traits.K<F, A> divide<F, A, B, C>(System.Func<A, System.ValueTuple<B, C>> f, LanguageExt.Traits.K<F, B> fb, LanguageExt.Traits.K<F, C> fc)`
- `where F : LanguageExt.Traits.Divisible<F>`

### Divisible`1<F> (interface) : LanguageExt.Traits.Cofunctor<F>

- `public static LanguageExt.Traits.K<F, A> Conquer<A>()`
- `public static LanguageExt.Traits.K<F, A> Divide<A, B, C>(System.Func<A, System.ValueTuple<B, C>> f, LanguageExt.Traits.K<F, B> fb, LanguageExt.Traits.K<F, C> fc)`

### EffExtensions (class [static])

- `public static LanguageExt.Eff<A> op_BitwiseOr<X, A>(LanguageExt.Traits.K<LanguageExt.Eff, A> lhs, LanguageExt.Finally<LanguageExt.Eff, X> rhs)`
- `public static LanguageExt.Eff<RT, A> op_BitwiseOr<RT, X, A>(LanguageExt.Traits.K<LanguageExt.Eff<RT>, A> lhs, LanguageExt.Finally<LanguageExt.Eff<RT>, X> rhs)`

### EitherTExtensions (class [static])

- `public static LanguageExt.EitherT<L, M, A> op_BitwiseOr<X, L, M, A>(LanguageExt.Traits.K<LanguageExt.EitherT<L, M>, A> lhs, LanguageExt.Finally<M, X> rhs)`
- `where M : LanguageExt.Traits.Monad<M>, LanguageExt.Traits.Final<M>`

### Eq`1<A> (interface) : LanguageExt.Hashable<A>, LanguageExt.Traits.Trait

- `public static System.Boolean Equals(A x, A y)`

### Fallible (class [static])

- `public static LanguageExt.Traits.K<F, A> error<F, A>(LanguageExt.Common.Error error)`
- `where F : LanguageExt.Traits.Fallible<LanguageExt.Common.Error, F>`
- `public static LanguageExt.Traits.K<F, LanguageExt.Unit> error<F>(LanguageExt.Common.Error error)`
- `where F : LanguageExt.Traits.Fallible<LanguageExt.Common.Error, F>`
- `public static LanguageExt.Traits.K<F, A> fail<E, F, A>(E error)`
- `where F : LanguageExt.Traits.Fallible<E, F>`
- `public static LanguageExt.Traits.K<F, LanguageExt.Unit> fail<E, F>(E error)`
- `where F : LanguageExt.Traits.Fallible<E, F>`

### FallibleExtensions (class [static])

- `public static LanguageExt.Traits.K<M, LanguageExt.Seq<E>> fails<E, F, M, A>(LanguageExt.Traits.K<F, LanguageExt.Traits.K<M, A>> fma)`
- `where F : LanguageExt.Traits.Foldable<F>`
- `where M : LanguageExt.Traits.Monad<M>, LanguageExt.Traits.Fallible<E, M>`
- `public static LanguageExt.Traits.K<M, LanguageExt.Seq<E>> fails<E, M, A>(LanguageExt.Seq<LanguageExt.Traits.K<M, A>> fma)`
- `where M : LanguageExt.Traits.Monad<M>, LanguageExt.Traits.Fallible<E, M>`
- `public static LanguageExt.Traits.K<M, LanguageExt.Seq<E>> fails<E, M, A>(LanguageExt.Iterable<LanguageExt.Traits.K<M, A>> fma)`
- `where M : LanguageExt.Traits.Monad<M>, LanguageExt.Traits.Fallible<E, M>`
- `public static LanguageExt.Traits.K<M, LanguageExt.Seq<E>> fails<E, M, A>(LanguageExt.Lst<LanguageExt.Traits.K<M, A>> fma)`
- `where M : LanguageExt.Traits.Monad<M>, LanguageExt.Traits.Fallible<E, M>`
- `public static LanguageExt.Traits.K<M, LanguageExt.Seq<E>> fails<E, M, A>(System.Collections.Generic.IEnumerable<LanguageExt.Traits.K<M, A>> fma)`
- `where M : LanguageExt.Traits.Monad<M>, LanguageExt.Traits.Fallible<E, M>`
- `public static LanguageExt.Traits.K<M, LanguageExt.Seq<E>> fails<E, M, A>(LanguageExt.HashSet<LanguageExt.Traits.K<M, A>> fma)`
- `where M : LanguageExt.Traits.Monad<M>, LanguageExt.Traits.Fallible<E, M>`
- `public static LanguageExt.Traits.K<M, LanguageExt.Seq<E>> fails<E, M, A>(LanguageExt.Set<LanguageExt.Traits.K<M, A>> fma)`
- `where M : LanguageExt.Traits.Monad<M>, LanguageExt.Traits.Fallible<E, M>`

### FallibleExtensionsE (class [static])

- `public static LanguageExt.Traits.K<M, LanguageExt.Seq<A>> succs<E, F, M, A>(LanguageExt.Traits.K<F, LanguageExt.Traits.K<M, A>> fma)`
- `where F : LanguageExt.Traits.Foldable<F>`
- `where M : LanguageExt.Traits.Monad<M>, LanguageExt.Traits.Fallible<E, M>`
- `public static LanguageExt.Traits.K<M, LanguageExt.Seq<A>> succs<E, M, A>(LanguageExt.Seq<LanguageExt.Traits.K<M, A>> fma)`
- `where M : LanguageExt.Traits.Monad<M>, LanguageExt.Traits.Fallible<E, M>`
- `public static LanguageExt.Traits.K<M, LanguageExt.Seq<A>> succs<E, M, A>(LanguageExt.Iterable<LanguageExt.Traits.K<M, A>> fma)`
- `where M : LanguageExt.Traits.Monad<M>, LanguageExt.Traits.Fallible<E, M>`
- `public static LanguageExt.Traits.K<M, LanguageExt.Seq<A>> succs<E, M, A>(LanguageExt.Lst<LanguageExt.Traits.K<M, A>> fma)`
- `where M : LanguageExt.Traits.Monad<M>, LanguageExt.Traits.Fallible<E, M>`
- `public static LanguageExt.Traits.K<M, LanguageExt.Seq<A>> succs<E, M, A>(System.Collections.Generic.IEnumerable<LanguageExt.Traits.K<M, A>> fma)`
- `where M : LanguageExt.Traits.Monad<M>, LanguageExt.Traits.Fallible<E, M>`
- `public static LanguageExt.Traits.K<M, LanguageExt.Seq<A>> succs<E, M, A>(LanguageExt.HashSet<LanguageExt.Traits.K<M, A>> fma)`
- `where M : LanguageExt.Traits.Monad<M>, LanguageExt.Traits.Fallible<E, M>`
- `public static LanguageExt.Traits.K<M, LanguageExt.Seq<A>> succs<E, M, A>(LanguageExt.Set<LanguageExt.Traits.K<M, A>> fma)`
- `where M : LanguageExt.Traits.Monad<M>, LanguageExt.Traits.Fallible<E, M>`

### Fallible`1<F> (interface) : LanguageExt.Traits.Fallible<LanguageExt.Common.Error, F>


### Fallible`2<E, F> (interface)

- `public static LanguageExt.Traits.K<F, A> Catch<A>(LanguageExt.Traits.K<F, A> fa, System.Func<E, System.Boolean> Predicate, System.Func<E, LanguageExt.Traits.K<F, A>> Fail)`
- `public static LanguageExt.Traits.K<F, A> Fail<A>(E error)`

### FinTExtensions (class [static])

- `public static LanguageExt.FinT<M, A> op_BitwiseOr<X, M, A>(LanguageExt.Traits.K<LanguageExt.FinT<M>, A> lhs, LanguageExt.Finally<M, X> rhs)`
- `where M : LanguageExt.Traits.Monad<M>, LanguageExt.Traits.Final<M>`

### Final (class [static])

- `public static LanguageExt.Finally<F, X> final<F, X>(LanguageExt.Traits.K<F, X> finally)`
- `where F : LanguageExt.Traits.Final<F>`

### FinalExtensions (class [static])

- `[ext] public static LanguageExt.Traits.K<F, A> Finally<X, F, A>(this LanguageExt.Traits.K<F, A> ma, LanguageExt.Traits.K<F, X> finally)`
- `where F : LanguageExt.Traits.Final<F>`

### Final`1<F> (interface)

- `where F : LanguageExt.Traits.Final<F>`

- `public static LanguageExt.Traits.K<F, A> Finally<X, A>(LanguageExt.Traits.K<F, A> fa, LanguageExt.Traits.K<F, X> finally)`

### Floating`1<A> (interface) : LanguageExt.Traits.Fraction<A>, LanguageExt.Traits.Num<A>, LanguageExt.Traits.Ord<A>, LanguageExt.Traits.Eq<A>, LanguageExt.Hashable<A>, LanguageExt.Traits.Trait, LanguageExt.Traits.Arithmetic<A>

- `public static A Acos(A x)`
- `public static A Acosh(A x)`
- `public static A Asin(A x)`
- `public static A Asinh(A x)`
- `public static A Atan(A x)`
- `public static A Atanh(A x)`
- `public static A Cos(A x)`
- `public static A Cosh(A x)`
- `public static A Exp(A x)`
- `public static A Log(A x)`
- `public static A LogBase(A x, A y)`
- `public static A Pi()`
- `public static A Pow(A x, A y)`
- `public static A Sin(A x)`
- `public static A Sinh(A x)`
- `public static A Sqrt(A x)`
- `public static A Tan(A x)`
- `public static A Tanh(A x)`

### Foldable (class [static])

- `public static LanguageExt.Option<A> at<T, A>(LanguageExt.Traits.K<T, A> ta, System.Index index)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `public static A average<T, A>(LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `where A : System.Numerics.INumber<A>`
- `public static B average<T, A, B>(System.Func<A, B> f, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `where B : System.Numerics.INumber<B>`
- `public static System.Boolean contains<EqA, T, A>(A value, LanguageExt.Traits.K<T, A> ta)`
- `where EqA : LanguageExt.Traits.Eq<A>`
- `where T : LanguageExt.Traits.Foldable<T>`
- `public static System.Boolean contains<T, A>(A value, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `public static System.Int32 count<T, A>(LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `public static System.Boolean exists<T, A>(System.Func<A, System.Boolean> predicate, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `public static LanguageExt.Option<A> find<T, A>(System.Func<A, System.Boolean> predicate, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `public static LanguageExt.Iterable<A> findAll<T, A>(System.Func<A, System.Boolean> predicate, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `public static LanguageExt.Iterable<A> findAllBack<T, A>(System.Func<A, System.Boolean> predicate, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `public static LanguageExt.Option<A> findBack<T, A>(System.Func<A, System.Boolean> predicate, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `public static A fold<T, A>(LanguageExt.Traits.K<T, A> tm)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `where A : LanguageExt.Traits.Monoid<A>`
- `public static S fold<T, A, S>(System.Func<A, System.Func<S, S>> f, S initialState, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `public static S fold<T, A, S>(System.Func<S, A, S> f, S initialState, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `public static S foldBack<T, A, S>(System.Func<S, System.Func<A, S>> f, S initialState, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `public static S foldBack<T, A, S>(System.Func<S, A, S> f, S initialState, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `public static LanguageExt.Traits.K<M, S> foldBackM<T, A, M, S>(System.Func<S, System.Func<A, LanguageExt.Traits.K<M, S>>> f, S initialState, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, S> foldBackM<T, A, M, S>(System.Func<S, A, LanguageExt.Traits.K<M, S>> f, S initialState, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static S foldBackUntil<T, A, S>(System.Func<S, System.Func<A, S>> f, System.Func<System.ValueTuple<S, A>, System.Boolean> predicate, S initialState, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `public static S foldBackUntil<T, A, S>(System.Func<S, A, S> f, System.Func<System.ValueTuple<S, A>, System.Boolean> predicate, S initialState, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `public static LanguageExt.Traits.K<M, S> foldBackUntilM<T, A, M, S>(System.Func<S, System.Func<A, LanguageExt.Traits.K<M, S>>> f, System.Func<System.ValueTuple<S, A>, System.Boolean> predicate, S initialState, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, S> foldBackUntilM<T, A, M, S>(System.Func<S, A, LanguageExt.Traits.K<M, S>> f, System.Func<System.ValueTuple<S, A>, System.Boolean> predicate, S initialState, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static S foldBackWhile<T, A, S>(System.Func<S, System.Func<A, S>> f, System.Func<System.ValueTuple<S, A>, System.Boolean> predicate, S initialState, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `public static S foldBackWhile<T, A, S>(System.Func<S, A, S> f, System.Func<System.ValueTuple<S, A>, System.Boolean> predicate, S initialState, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `public static LanguageExt.Traits.K<M, S> foldBackWhileM<T, A, M, S>(System.Func<S, System.Func<A, LanguageExt.Traits.K<M, S>>> f, System.Func<System.ValueTuple<S, A>, System.Boolean> predicate, S initialState, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, S> foldBackWhileM<T, A, M, S>(System.Func<S, A, LanguageExt.Traits.K<M, S>> f, System.Func<System.ValueTuple<S, A>, System.Boolean> predicate, S initialState, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, S> foldM<T, A, M, S>(System.Func<A, System.Func<S, LanguageExt.Traits.K<M, S>>> f, S initialState, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, S> foldM<T, A, M, S>(System.Func<S, A, LanguageExt.Traits.K<M, S>> f, S initialState, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static B foldMap<T, A, B>(System.Func<A, B> f, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `where B : LanguageExt.Traits.Monoid<B>`
- `public static B foldMapBack<T, A, B>(System.Func<A, B> f, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `where B : LanguageExt.Traits.Monoid<B>`
- `public static B foldMapBackUntil<T, A, B>(System.Func<A, B> f, System.Func<System.ValueTuple<B, A>, System.Boolean> predicate, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `where B : LanguageExt.Traits.Monoid<B>`
- `public static B foldMapBackWhile<T, A, B>(System.Func<A, B> f, System.Func<System.ValueTuple<B, A>, System.Boolean> predicate, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `where B : LanguageExt.Traits.Monoid<B>`
- `public static B foldMapUntil<T, A, B>(System.Func<A, B> f, System.Func<System.ValueTuple<B, A>, System.Boolean> predicate, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `where B : LanguageExt.Traits.Monoid<B>`
- `public static B foldMapWhile<T, A, B>(System.Func<A, B> f, System.Func<System.ValueTuple<B, A>, System.Boolean> predicate, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `where B : LanguageExt.Traits.Monoid<B>`
- `public static A foldUntil<T, A>(System.Func<System.ValueTuple<A, A>, System.Boolean> predicate, LanguageExt.Traits.K<T, A> tm)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `where A : LanguageExt.Traits.Monoid<A>`
- `public static S foldUntil<T, A, S>(System.Func<A, System.Func<S, S>> f, System.Func<System.ValueTuple<S, A>, System.Boolean> predicate, S initialState, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `public static S foldUntil<T, A, S>(System.Func<S, A, S> f, System.Func<System.ValueTuple<S, A>, System.Boolean> predicate, S initialState, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `public static LanguageExt.Traits.K<M, S> foldUntilM<T, A, M, S>(System.Func<A, System.Func<S, LanguageExt.Traits.K<M, S>>> f, System.Func<System.ValueTuple<S, A>, System.Boolean> predicate, S initialState, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, S> foldUntilM<T, A, M, S>(System.Func<S, A, LanguageExt.Traits.K<M, S>> f, System.Func<System.ValueTuple<S, A>, System.Boolean> predicate, S initialState, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static A foldWhile<T, A>(System.Func<System.ValueTuple<A, A>, System.Boolean> predicate, LanguageExt.Traits.K<T, A> tm)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `where A : LanguageExt.Traits.Monoid<A>`
- `public static S foldWhile<T, A, S>(System.Func<A, System.Func<S, S>> f, System.Func<System.ValueTuple<S, A>, System.Boolean> predicate, S initialState, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `public static S foldWhile<T, A, S>(System.Func<S, A, S> f, System.Func<System.ValueTuple<S, A>, System.Boolean> predicate, S initialState, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `public static LanguageExt.Traits.K<M, S> foldWhileM<T, A, M, S>(System.Func<A, System.Func<S, LanguageExt.Traits.K<M, S>>> f, System.Func<System.ValueTuple<S, A>, System.Boolean> predicate, S initialState, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, S> foldWhileM<T, A, M, S>(System.Func<S, A, LanguageExt.Traits.K<M, S>> f, System.Func<System.ValueTuple<S, A>, System.Boolean> predicate, S initialState, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static System.Boolean forAll<T, A>(System.Func<A, System.Boolean> predicate, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `public static LanguageExt.Traits.K<F, LanguageExt.Unit> forM<T, F, A, B>(LanguageExt.Traits.K<T, A> ta, System.Func<A, LanguageExt.Traits.K<F, B>> f)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Option<A> head<T, A>(LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `public static System.Boolean isEmpty<T, A>(LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `public static LanguageExt.Traits.K<F, LanguageExt.Unit> iter<T, A, F, B>(System.Func<A, LanguageExt.Traits.K<F, B>> f, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `where F : LanguageExt.Traits.Monad<F>`
- `public static LanguageExt.Unit iter<T, A>(System.Action<System.Int32, A> f, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `public static LanguageExt.Unit iter<T, A>(System.Action<A> f, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `public static LanguageExt.Option<A> last<T, A>(LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `public static LanguageExt.Option<A> max<OrdA, T, A>(LanguageExt.Traits.K<T, A> ta)`
- `where OrdA : LanguageExt.Traits.Ord<A>`
- `where T : LanguageExt.Traits.Foldable<T>`
- `public static LanguageExt.Option<A> max<T, A>(LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `where A : System.IComparable<A>`
- `public static A max<OrdA, T, A>(LanguageExt.Traits.K<T, A> ta, A initialMax)`
- `where OrdA : LanguageExt.Traits.Ord<A>`
- `where T : LanguageExt.Traits.Foldable<T>`
- `public static A max<T, A>(LanguageExt.Traits.K<T, A> ta, A initialMax)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `where A : System.IComparable<A>`
- `public static LanguageExt.Option<A> min<OrdA, T, A>(LanguageExt.Traits.K<T, A> ta)`
- `where OrdA : LanguageExt.Traits.Ord<A>`
- `where T : LanguageExt.Traits.Foldable<T>`
- `public static LanguageExt.Option<A> min<T, A>(LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `where A : System.IComparable<A>`
- `public static A min<OrdA, T, A>(LanguageExt.Traits.K<T, A> ta, A initialMin)`
- `where OrdA : LanguageExt.Traits.Ord<A>`
- `where T : LanguageExt.Traits.Foldable<T>`
- `public static A min<T, A>(LanguageExt.Traits.K<T, A> ta, A initialMin)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `where A : System.IComparable<A>`
- `public static System.ValueTuple<LanguageExt.Seq<A>, LanguageExt.Seq<A>> partition<T, A>(System.Func<A, System.Boolean> f, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `public static A product<T, A>(LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `where A : System.Numerics.IMultiplyOperators<A, A, A>, System.Numerics.IMultiplicativeIdentity<A, A>`
- `public static A sum<T, A>(LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `where A : System.Numerics.IAdditionOperators<A, A, A>, System.Numerics.IAdditiveIdentity<A, A>`
- `public static LanguageExt.Arr<A> toArr<T, A>(LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `public static LanguageExt.Iterable<A> toIterable<T, A>(LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `public static LanguageExt.Lst<A> toLst<T, A>(LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`
- `public static LanguageExt.Seq<A> toSeq<T, A>(LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Foldable<T>`

### Foldable`1<T> (interface)

- `where T : LanguageExt.Traits.Foldable<T>`

- `public static LanguageExt.Option<A> At<A>(LanguageExt.Traits.K<T, A> ta, System.Index index)`
- `public static A Average<A>(LanguageExt.Traits.K<T, A> ta)`
- `where A : System.Numerics.INumber<A>`
- `public static B Average<A, B>(System.Func<A, B> f, LanguageExt.Traits.K<T, A> ta)`
- `where B : System.Numerics.INumber<B>`
- `public static System.Boolean Contains<EqA, A>(A value, LanguageExt.Traits.K<T, A> ta)`
- `where EqA : LanguageExt.Traits.Eq<A>`
- `public static System.Boolean Contains<A>(A value, LanguageExt.Traits.K<T, A> ta)`
- `public static System.Int32 Count<A>(LanguageExt.Traits.K<T, A> ta)`
- `public static System.Boolean Exists<A>(System.Func<A, System.Boolean> predicate, LanguageExt.Traits.K<T, A> ta)`
- `public static LanguageExt.Option<A> Find<A>(System.Func<A, System.Boolean> predicate, LanguageExt.Traits.K<T, A> ta)`
- `public static LanguageExt.Iterable<A> FindAll<A>(System.Func<A, System.Boolean> predicate, LanguageExt.Traits.K<T, A> ta)`
- `public static LanguageExt.Iterable<A> FindAllBack<A>(System.Func<A, System.Boolean> predicate, LanguageExt.Traits.K<T, A> ta)`
- `public static LanguageExt.Option<A> FindBack<A>(System.Func<A, System.Boolean> predicate, LanguageExt.Traits.K<T, A> ta)`
- `public static A Fold<A>(LanguageExt.Traits.K<T, A> ta)`
- `where A : LanguageExt.Traits.Monoid<A>`
- `public static S Fold<A, S>(System.Func<A, System.Func<S, S>> f, S initialState, LanguageExt.Traits.K<T, A> ta)`
- `public static S FoldBack<A, S>(System.Func<S, System.Func<A, S>> f, S initialState, LanguageExt.Traits.K<T, A> ta)`
- `public static LanguageExt.Traits.K<M, S> FoldBackM<A, M, S>(System.Func<S, System.Func<A, LanguageExt.Traits.K<M, S>>> f, S initialState, LanguageExt.Traits.K<T, A> ta)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static S FoldBackMaybe<A, S>(System.Func<A, System.Func<S, LanguageExt.Option<S>>> f, S initialState, LanguageExt.Traits.K<T, A> ta)`
- `public static S FoldBackUntil<A, S>(System.Func<S, System.Func<A, S>> f, System.Func<System.ValueTuple<S, A>, System.Boolean> predicate, S initialState, LanguageExt.Traits.K<T, A> ta)`
- `public static LanguageExt.Traits.K<M, S> FoldBackUntilM<A, M, S>(System.Func<S, System.Func<A, LanguageExt.Traits.K<M, S>>> f, System.Func<System.ValueTuple<S, A>, System.Boolean> predicate, S initialState, LanguageExt.Traits.K<T, A> ta)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static S FoldBackWhile<A, S>(System.Func<S, System.Func<A, S>> f, System.Func<System.ValueTuple<S, A>, System.Boolean> predicate, S initialState, LanguageExt.Traits.K<T, A> ta)`
- `public static LanguageExt.Traits.K<M, S> FoldBackWhileM<A, M, S>(System.Func<S, System.Func<A, LanguageExt.Traits.K<M, S>>> f, System.Func<System.ValueTuple<S, A>, System.Boolean> predicate, S initialState, LanguageExt.Traits.K<T, A> ta)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, S> FoldM<A, M, S>(System.Func<A, System.Func<S, LanguageExt.Traits.K<M, S>>> f, S initialState, LanguageExt.Traits.K<T, A> ta)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static B FoldMap<A, B>(System.Func<A, B> f, LanguageExt.Traits.K<T, A> ta)`
- `where B : LanguageExt.Traits.Monoid<B>`
- `public static B FoldMapBack<A, B>(System.Func<A, B> f, LanguageExt.Traits.K<T, A> ta)`
- `where B : LanguageExt.Traits.Monoid<B>`
- `public static B FoldMapUntil<A, B>(System.Func<A, B> f, System.Func<System.ValueTuple<B, A>, System.Boolean> predicate, LanguageExt.Traits.K<T, A> ta)`
- `where B : LanguageExt.Traits.Monoid<B>`
- `public static B FoldMapUntilBack<A, B>(System.Func<A, B> f, System.Func<System.ValueTuple<B, A>, System.Boolean> predicate, LanguageExt.Traits.K<T, A> ta)`
- `where B : LanguageExt.Traits.Monoid<B>`
- `public static B FoldMapWhile<A, B>(System.Func<A, B> f, System.Func<System.ValueTuple<B, A>, System.Boolean> predicate, LanguageExt.Traits.K<T, A> ta)`
- `where B : LanguageExt.Traits.Monoid<B>`
- `public static B FoldMapWhileBack<A, B>(System.Func<A, B> f, System.Func<System.ValueTuple<B, A>, System.Boolean> predicate, LanguageExt.Traits.K<T, A> ta)`
- `where B : LanguageExt.Traits.Monoid<B>`
- `public static S FoldMaybe<A, S>(System.Func<S, System.Func<A, LanguageExt.Option<S>>> f, S initialState, LanguageExt.Traits.K<T, A> ta)`
- `public static LanguageExt.Fold<A, S> FoldStep<A, S>(LanguageExt.Traits.K<T, A> ta, S initialState)`
- `public static LanguageExt.Fold<A, S> FoldStepBack<A, S>(LanguageExt.Traits.K<T, A> ta, S initialState)`
- `public static A FoldUntil<A>(System.Func<System.ValueTuple<A, A>, System.Boolean> predicate, LanguageExt.Traits.K<T, A> ta)`
- `where A : LanguageExt.Traits.Monoid<A>`
- `public static S FoldUntil<A, S>(System.Func<A, System.Func<S, S>> f, System.Func<System.ValueTuple<S, A>, System.Boolean> predicate, S initialState, LanguageExt.Traits.K<T, A> ta)`
- `public static LanguageExt.Traits.K<M, S> FoldUntilM<A, M, S>(System.Func<A, System.Func<S, LanguageExt.Traits.K<M, S>>> f, System.Func<System.ValueTuple<S, A>, System.Boolean> predicate, S initialState, LanguageExt.Traits.K<T, A> ta)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static A FoldWhile<A>(System.Func<System.ValueTuple<A, A>, System.Boolean> predicate, LanguageExt.Traits.K<T, A> ta)`
- `where A : LanguageExt.Traits.Monoid<A>`
- `public static S FoldWhile<A, S>(System.Func<A, System.Func<S, S>> f, System.Func<System.ValueTuple<S, A>, System.Boolean> predicate, S initialState, LanguageExt.Traits.K<T, A> ta)`
- `public static LanguageExt.Traits.K<M, S> FoldWhileM<A, M, S>(System.Func<A, System.Func<S, LanguageExt.Traits.K<M, S>>> f, System.Func<System.ValueTuple<S, A>, System.Boolean> predicate, S initialState, LanguageExt.Traits.K<T, A> ta)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static System.Boolean ForAll<A>(System.Func<A, System.Boolean> predicate, LanguageExt.Traits.K<T, A> ta)`
- `public static LanguageExt.Option<A> Head<A>(LanguageExt.Traits.K<T, A> ta)`
- `public static System.Boolean IsEmpty<A>(LanguageExt.Traits.K<T, A> ta)`
- `public static LanguageExt.Traits.K<M, LanguageExt.Unit> Iter<M, A, B>(System.Func<A, LanguageExt.Traits.K<M, B>> f, LanguageExt.Traits.K<T, A> ta)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Unit Iter<A>(System.Action<A> f, LanguageExt.Traits.K<T, A> ta)`
- `public static LanguageExt.Unit Iter<A>(System.Action<System.Int32, A> f, LanguageExt.Traits.K<T, A> ta)`
- `public static LanguageExt.Option<A> Last<A>(LanguageExt.Traits.K<T, A> ta)`
- `public static LanguageExt.Option<A> Max<OrdA, A>(LanguageExt.Traits.K<T, A> ta)`
- `where OrdA : LanguageExt.Traits.Ord<A>`
- `public static LanguageExt.Option<A> Max<A>(LanguageExt.Traits.K<T, A> ta)`
- `where A : System.IComparable<A>`
- `public static A Max<OrdA, A>(LanguageExt.Traits.K<T, A> ta, A initialMax)`
- `where OrdA : LanguageExt.Traits.Ord<A>`
- `public static A Max<A>(LanguageExt.Traits.K<T, A> ta, A initialMax)`
- `where A : System.IComparable<A>`
- `public static LanguageExt.Option<A> Min<OrdA, A>(LanguageExt.Traits.K<T, A> ta)`
- `where OrdA : LanguageExt.Traits.Ord<A>`
- `public static LanguageExt.Option<A> Min<A>(LanguageExt.Traits.K<T, A> ta)`
- `where A : System.IComparable<A>`
- `public static A Min<OrdA, A>(LanguageExt.Traits.K<T, A> ta, A initialMin)`
- `where OrdA : LanguageExt.Traits.Ord<A>`
- `public static A Min<A>(LanguageExt.Traits.K<T, A> ta, A initialMin)`
- `where A : System.IComparable<A>`
- `public static System.ValueTuple<LanguageExt.Seq<A>, LanguageExt.Seq<A>> Partition<A>(System.Func<A, System.Boolean> f, LanguageExt.Traits.K<T, A> ta)`
- `public static A Product<A>(LanguageExt.Traits.K<T, A> ta)`
- `where A : System.Numerics.IMultiplyOperators<A, A, A>, System.Numerics.IMultiplicativeIdentity<A, A>`
- `public static A Sum<A>(LanguageExt.Traits.K<T, A> ta)`
- `where A : System.Numerics.IAdditionOperators<A, A, A>, System.Numerics.IAdditiveIdentity<A, A>`
- `public static LanguageExt.Arr<A> ToArr<A>(LanguageExt.Traits.K<T, A> ta)`
- `public static LanguageExt.Iterable<A> ToIterable<A>(LanguageExt.Traits.K<T, A> ta)`
- `public static LanguageExt.Lst<A> ToLst<A>(LanguageExt.Traits.K<T, A> ta)`
- `public static LanguageExt.Seq<A> ToSeq<A>(LanguageExt.Traits.K<T, A> ta)`

### Fraction`1<A> (interface) : LanguageExt.Traits.Num<A>, LanguageExt.Traits.Ord<A>, LanguageExt.Traits.Eq<A>, LanguageExt.Hashable<A>, LanguageExt.Traits.Trait, LanguageExt.Traits.Arithmetic<A>

- `public static A FromRational(LanguageExt.Ratio<System.Int32> x)`

### Functor (class [static])

- `public static LanguageExt.Traits.K<F, LanguageExt.Unit> ignore<F, A>(LanguageExt.Traits.K<F, A> fa)`
- `where F : LanguageExt.Traits.Functor<F>`
- `public static LanguageExt.Traits.K<F, B> map<F, A, B>(System.Func<A, B> f, LanguageExt.Traits.K<F, A> fa)`
- `where F : LanguageExt.Traits.Functor<F>`

### FunctorLaw`1<F> (class [static])

- `where F : LanguageExt.Traits.Functor<F>`

- `public static LanguageExt.Unit assert(LanguageExt.Traits.K<F, System.Int32> fa, System.Func<LanguageExt.Traits.K<F, System.Int32>, LanguageExt.Traits.K<F, System.Int32>, System.Boolean> equals)`
- `public static LanguageExt.Validation<LanguageExt.Common.Error, LanguageExt.Unit> compositionLaw(LanguageExt.Traits.K<F, System.Int32> fa, System.Func<System.Int32, System.Int32> f, System.Func<System.Int32, System.Int32> g, System.Func<LanguageExt.Traits.K<F, System.Int32>, LanguageExt.Traits.K<F, System.Int32>, System.Boolean> equals)`
- `public static LanguageExt.Validation<LanguageExt.Common.Error, LanguageExt.Unit> identityLaw(LanguageExt.Traits.K<F, System.Int32> lhs, System.Func<LanguageExt.Traits.K<F, System.Int32>, LanguageExt.Traits.K<F, System.Int32>, System.Boolean> equals)`
- `public static LanguageExt.Validation<LanguageExt.Common.Error, LanguageExt.Unit> validate(LanguageExt.Traits.K<F, System.Int32> fa, System.Func<LanguageExt.Traits.K<F, System.Int32>, LanguageExt.Traits.K<F, System.Int32>, System.Boolean> equals)`

### Functor`1<F> (interface)

- `where F : LanguageExt.Traits.Functor<F>`

- `public static LanguageExt.Traits.K<F, A> ConstMap<A, B>(A constantValue, LanguageExt.Traits.K<F, B> ma)`
- `public static LanguageExt.Traits.K<F, B> Map<A, B>(System.Func<A, B> f, LanguageExt.Traits.K<F, A> ma)`
- `public static LanguageExt.Traits.K<F, B> Map<A, B>(System.Func<A, B> f, LanguageExt.Memo<F, A> ma)`

### Has`2<M, VALUE> (interface)

- `public static LanguageExt.Traits.K<M, VALUE> Ask { get; }`

### Has`3<M, Env, VALUE> (class [static])

- `where Env : LanguageExt.Traits.Has<M, VALUE>`

- `public static LanguageExt.Traits.K<M, VALUE> ask`

### Hashable (class [static])

- `public static System.Int32 code<A>(A x)`
- `where A : LanguageExt.Hashable<A>`

### IOExtensions (class [static])

- `public static LanguageExt.IO<A> op_BitwiseOr<X, A>(LanguageExt.Traits.K<LanguageExt.IO, A> lhs, LanguageExt.Finally<LanguageExt.IO, X> rhs)`

### Identifiable (class [static])

- `public static LanguageExt.Traits.K<F, A> identify<F, L, A>(LanguageExt.Traits.K<F, A> fa, L label)`
- `where F : LanguageExt.Traits.Identifiable<F, L>`
- `public static LanguageExt.Traits.K<F, A> identify<F, L, A>(LanguageExt.Traits.K<F, A> fa, LanguageExt.Label<L> label)`
- `where F : LanguageExt.Traits.Identifiable<F, L>`

### Identifiable`2<F, L> (interface)

- `where F : LanguageExt.Traits.Identifiable<F, L>`

- `public static LanguageExt.Traits.K<F, A> Identify<A>(LanguageExt.Traits.K<F, A> fa, LanguageExt.Label<L> label)`

### Indexable`3<A, KEY, VALUE> (interface)

- `public static VALUE Get(A ma, KEY key)`
- `public static LanguageExt.Option<VALUE> TryGet(A ma, KEY key)`

### K`2<F, A> (interface)


### K`3<F, A, B> (interface)


### Local (class [static])

- `public static LanguageExt.Traits.K<M, A> with<M, Env, InnerEnv, A>(System.Func<InnerEnv, InnerEnv> f, LanguageExt.Traits.K<M, A> ma)`
- `where Env : LanguageExt.Traits.Local<M, InnerEnv>`

### Local`2<M, InnerEnv> (interface) : LanguageExt.Traits.Has<M, InnerEnv>

- `public static LanguageExt.Traits.K<M, A> With<A>(System.Func<InnerEnv, InnerEnv> f, LanguageExt.Traits.K<M, A> ma)`

### Maybe (class [static])


### Monad (class [static])

- `public static LanguageExt.Traits.K<M, LanguageExt.Iterable<A>> accumUntil<M, A>(LanguageExt.Traits.K<M, A> ma, System.Func<A, System.Boolean> f)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, LanguageExt.Iterable<A>> accumUntilM<M, A>(LanguageExt.Traits.K<M, A> ma, System.Func<A, LanguageExt.Traits.K<M, System.Boolean>> f)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, LanguageExt.Iterable<A>> accumWhile<M, A>(LanguageExt.Traits.K<M, A> ma, System.Func<A, System.Boolean> f)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, LanguageExt.Iterable<A>> accumWhileM<M, A>(LanguageExt.Traits.K<M, A> ma, System.Func<A, LanguageExt.Traits.K<M, System.Boolean>> f)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, B> bind<M, A, B>(LanguageExt.Traits.K<M, A> ma, System.Func<A, LanguageExt.Traits.K<M, B>> f)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static MB bind<M, MB, A, B>(LanguageExt.Traits.K<M, A> ma, System.Func<A, MB> f)`
- `where M : LanguageExt.Traits.Monad<M>`
- `where MB : LanguageExt.Traits.K<M, B>`
- `public static LanguageExt.Traits.K<M, LanguageExt.Next<A, B>> done<M, A, B>(B value)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static System.Collections.Generic.IEnumerable<B> enumerableRecur<A, B>(A value, System.Func<A, System.Collections.Generic.IEnumerable<LanguageExt.Next<A, B>>> f)`
- `public static LanguageExt.Traits.K<M, A> flatten<M, A>(LanguageExt.Traits.K<M, LanguageExt.Traits.K<M, A>> mma)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, A> forever<M, A>(LanguageExt.Traits.K<M, A> ma)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, B> forever<M, A, B>(LanguageExt.Traits.K<M, A> ma)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, A> iff<M, A>(LanguageExt.Traits.K<M, System.Boolean> Pred, LanguageExt.Traits.K<M, A> Then, LanguageExt.Traits.K<M, A> Else)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, A> iff<M, A>(LanguageExt.Traits.K<M, System.Boolean> Pred, LanguageExt.Traits.K<M, A> Then, LanguageExt.Traits.K<LanguageExt.IO, A> Else)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, A> iff<M, A>(LanguageExt.Traits.K<M, System.Boolean> Pred, LanguageExt.Traits.K<LanguageExt.IO, A> Then, LanguageExt.Traits.K<M, A> Else)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, A> iff<M, A>(LanguageExt.Traits.K<M, System.Boolean> Pred, LanguageExt.Traits.K<LanguageExt.IO, A> Then, LanguageExt.Traits.K<LanguageExt.IO, A> Else)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, A> iff<M, A>(LanguageExt.Traits.K<M, System.Boolean> Pred, LanguageExt.Traits.K<M, A> Then, LanguageExt.Pure<A> Else)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, A> iff<M, A>(LanguageExt.Traits.K<M, System.Boolean> Pred, LanguageExt.Pure<A> Then, LanguageExt.Traits.K<M, A> Else)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, A> iff<M, A>(LanguageExt.Traits.K<M, System.Boolean> Pred, LanguageExt.Pure<A> Then, LanguageExt.Pure<A> Else)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, A> iff<M, A>(LanguageExt.Traits.K<M, System.Boolean> Pred, LanguageExt.Pure<A> Then, LanguageExt.Traits.K<LanguageExt.IO, A> Else)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, A> iff<M, A>(LanguageExt.Traits.K<M, System.Boolean> Pred, LanguageExt.Traits.K<LanguageExt.IO, A> Then, LanguageExt.Pure<A> Else)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, B> iterableRecur<M, A, B>(A value, System.Func<A, LanguageExt.Traits.K<M, LanguageExt.Next<A, B>>> f)`
- `where M : LanguageExt.Traits.Natural<M, LanguageExt.Iterable>, LanguageExt.Traits.CoNatural<M, LanguageExt.Iterable>`
- `public static LanguageExt.Traits.K<M, LanguageExt.Next<A, B>> loop<M, A, B>(A value)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, A> pure<M, A>(A value)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, B> recur<M, A, B>(A value, System.Func<A, LanguageExt.Traits.K<M, LanguageExt.Next<A, B>>> f)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, LanguageExt.Iterable<A>> replicate<M, A>(LanguageExt.Traits.K<M, A> ma, System.Int32 count)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, LanguageExt.Unit> unless<M>(LanguageExt.Traits.K<M, System.Boolean> Pred, LanguageExt.Traits.K<M, LanguageExt.Unit> Then)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, LanguageExt.Unit> unless<M>(LanguageExt.Traits.K<M, System.Boolean> Pred, LanguageExt.Pure<LanguageExt.Unit> Then)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, B> unsafeRecur<M, A, B>(A value, System.Func<A, LanguageExt.Traits.K<M, LanguageExt.Next<A, B>>> f)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, LanguageExt.Unit> when<M>(LanguageExt.Traits.K<M, System.Boolean> Pred, LanguageExt.Traits.K<M, LanguageExt.Unit> Then)`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, LanguageExt.Unit> when<M>(LanguageExt.Traits.K<M, System.Boolean> Pred, LanguageExt.Pure<LanguageExt.Unit> Then)`
- `where M : LanguageExt.Traits.Monad<M>`

### MonadIO (class [static])

- `public static LanguageExt.Traits.K<M, LanguageExt.EnvIO> envIO<M>()`
- `where M : LanguageExt.Traits.MonadIO<M>, LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, A> liftIO<M, A>(LanguageExt.IO<A> ma)`
- `where M : LanguageExt.Traits.Maybe+MonadIO<M>, LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, A> liftIO<M, A>(LanguageExt.Traits.K<LanguageExt.IO, A> ma)`
- `where M : LanguageExt.Traits.Maybe+MonadIO<M>, LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, LanguageExt.Option<System.Threading.SynchronizationContext>> syncContext<M>()`
- `where M : LanguageExt.Traits.MonadIO<M>, LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, System.Threading.CancellationToken> token<M>()`
- `where M : LanguageExt.Traits.MonadIO<M>, LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, System.Threading.CancellationTokenSource> tokenSource<M>()`
- `where M : LanguageExt.Traits.MonadIO<M>, LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, LanguageExt.Unit> unless<M>(LanguageExt.Traits.K<M, System.Boolean> Pred, LanguageExt.Traits.K<LanguageExt.IO, LanguageExt.Unit> Then)`
- `where M : LanguageExt.Traits.MonadIO<M>`
- `public static LanguageExt.Traits.K<M, LanguageExt.Unit> when<M>(LanguageExt.Traits.K<M, System.Boolean> Pred, LanguageExt.Traits.K<LanguageExt.IO, LanguageExt.Unit> Then)`
- `where M : LanguageExt.Traits.MonadIO<M>`

### MonadIO`1<M> (interface) : LanguageExt.Traits.Monad<M>, LanguageExt.Traits.Applicative<M>, LanguageExt.Traits.Functor<M>, LanguageExt.Traits.Maybe+MonadUnliftIO<M>, LanguageExt.Traits.Maybe+MonadIO<M>

- `where M : LanguageExt.Traits.MonadIO<M>`

- `public static LanguageExt.Traits.K<M, LanguageExt.EnvIO> EnvIO { get; }`
- `public static LanguageExt.Traits.K<M, LanguageExt.Option<System.Threading.SynchronizationContext>> SyncContext { get; }`
- `public static LanguageExt.Traits.K<M, System.Threading.CancellationToken> Token { get; }`
- `public static LanguageExt.Traits.K<M, System.Threading.CancellationTokenSource> TokenSource { get; }`
- `public static LanguageExt.Traits.K<M, A> LiftIO<A>(LanguageExt.Traits.K<LanguageExt.IO, A> ma)`
- `public static LanguageExt.Traits.K<M, A> LiftIO<A>(LanguageExt.IO<A> ma)`

### MonadIO`1<M> (interface)

- `where M : LanguageExt.Traits.Maybe+MonadIO<M>`

- `public static LanguageExt.Traits.K<M, A> LiftIOMaybe<A>(LanguageExt.Traits.K<LanguageExt.IO, A> ma)`
- `public static LanguageExt.Traits.K<M, A> LiftIOMaybe<A>(LanguageExt.IO<A> ma)`

### MonadLaw`1<F> (class [static])

- `where F : LanguageExt.Traits.Monad<F>`

- `public static LanguageExt.Unit assert(System.Func<LanguageExt.Traits.K<F, System.Int32>, LanguageExt.Traits.K<F, System.Int32>, System.Boolean> equals)`
- `public static LanguageExt.Validation<LanguageExt.Common.Error, LanguageExt.Unit> associativityLaw(System.Func<LanguageExt.Traits.K<F, System.Int32>, LanguageExt.Traits.K<F, System.Int32>, System.Boolean> equals)`
- `public static LanguageExt.Validation<LanguageExt.Common.Error, LanguageExt.Unit> leftIdentityLaw(System.Func<LanguageExt.Traits.K<F, System.Int32>, LanguageExt.Traits.K<F, System.Int32>, System.Boolean> equals)`
- `public static LanguageExt.Validation<LanguageExt.Common.Error, LanguageExt.Unit> recurIsSameAsBind(System.Func<LanguageExt.Traits.K<F, System.Int32>, LanguageExt.Traits.K<F, System.Int32>, System.Boolean> equals)`
- `public static LanguageExt.Validation<LanguageExt.Common.Error, LanguageExt.Unit> rightIdentityLaw(System.Func<LanguageExt.Traits.K<F, System.Int32>, LanguageExt.Traits.K<F, System.Int32>, System.Boolean> equals)`
- `public static LanguageExt.Validation<LanguageExt.Common.Error, LanguageExt.Unit> validate(System.Func<LanguageExt.Traits.K<F, System.Int32>, LanguageExt.Traits.K<F, System.Int32>, System.Boolean> equals)`

### MonadT (class [static])

- `public static LanguageExt.Traits.K<MTran, A> lift<MTran, M, A>(LanguageExt.Traits.K<M, A> ma)`
- `where MTran : LanguageExt.Traits.MonadT<MTran, M>`
- `where M : LanguageExt.Traits.Monad<M>`

### MonadT`2<T, M> (interface) : LanguageExt.Traits.Monad<T>, LanguageExt.Traits.Applicative<T>, LanguageExt.Traits.Functor<T>, LanguageExt.Traits.Maybe+MonadUnliftIO<T>, LanguageExt.Traits.Maybe+MonadIO<T>

- `where T : LanguageExt.Traits.MonadT<T, M>`
- `where M : LanguageExt.Traits.Monad<M>`

- `public static LanguageExt.Traits.K<T, A> Lift<A>(LanguageExt.Traits.K<M, A> ma)`

### MonadUnliftIO (class [static])

- `public static LanguageExt.Traits.K<M, B> mapIO<M, A, B>(System.Func<LanguageExt.IO<A>, LanguageExt.IO<B>> f, LanguageExt.Traits.K<M, A> ma)`
- `where M : LanguageExt.Traits.MonadUnliftIO<M>`
- `public static LanguageExt.Traits.K<M, LanguageExt.IO<A>> toIO<M, A>(LanguageExt.Traits.K<M, A> ma)`
- `where M : LanguageExt.Traits.MonadUnliftIO<M>`

### MonadUnliftIO`1<M> (interface) : LanguageExt.Traits.MonadIO<M>, LanguageExt.Traits.Monad<M>, LanguageExt.Traits.Applicative<M>, LanguageExt.Traits.Functor<M>, LanguageExt.Traits.Maybe+MonadUnliftIO<M>, LanguageExt.Traits.Maybe+MonadIO<M>

- `where M : LanguageExt.Traits.MonadUnliftIO<M>`

- `public static LanguageExt.Traits.K<M, A> Await<A>(LanguageExt.Traits.K<M, LanguageExt.ForkIO<A>> ma)`
- `public static LanguageExt.Traits.K<M, A> BracketIO<A>(LanguageExt.Traits.K<M, A> ma)`
- `public static LanguageExt.Traits.K<M, C> BracketIO<A, B, C>(LanguageExt.Traits.K<M, A> Acq, System.Func<A, LanguageExt.IO<C>> Use, System.Func<A, LanguageExt.IO<B>> Fin)`
- `public static LanguageExt.Traits.K<M, C> BracketIO<A, B, C>(LanguageExt.Traits.K<M, A> Acq, System.Func<A, LanguageExt.IO<C>> Use, System.Func<LanguageExt.Common.Error, LanguageExt.IO<C>> Catch, System.Func<A, LanguageExt.IO<B>> Fin)`
- `public static LanguageExt.Traits.K<M, S> FoldIO<S, A>(LanguageExt.Traits.K<M, A> ma, S initialState, System.Func<S, A, S> folder)`
- `public static LanguageExt.Traits.K<M, S> FoldIO<S, A>(LanguageExt.Traits.K<M, A> ma, LanguageExt.Schedule schedule, S initialState, System.Func<S, A, S> folder)`
- `public static LanguageExt.Traits.K<M, S> FoldUntilIO<S, A>(LanguageExt.Traits.K<M, A> ma, S initialState, System.Func<S, A, S> folder, System.Func<S, System.Boolean> stateIs)`
- `public static LanguageExt.Traits.K<M, S> FoldUntilIO<S, A>(LanguageExt.Traits.K<M, A> ma, S initialState, System.Func<S, A, S> folder, System.Func<A, System.Boolean> valueIs)`
- `public static LanguageExt.Traits.K<M, S> FoldUntilIO<S, A>(LanguageExt.Traits.K<M, A> ma, S initialState, System.Func<S, A, S> folder, System.Func<System.ValueTuple<S, A>, System.Boolean> predicate)`
- `public static LanguageExt.Traits.K<M, S> FoldUntilIO<S, A>(LanguageExt.Traits.K<M, A> ma, LanguageExt.Schedule schedule, S initialState, System.Func<S, A, S> folder, System.Func<S, System.Boolean> stateIs)`
- `public static LanguageExt.Traits.K<M, S> FoldUntilIO<S, A>(LanguageExt.Traits.K<M, A> ma, LanguageExt.Schedule schedule, S initialState, System.Func<S, A, S> folder, System.Func<A, System.Boolean> valueIs)`
- `public static LanguageExt.Traits.K<M, S> FoldUntilIO<S, A>(LanguageExt.Traits.K<M, A> ma, LanguageExt.Schedule schedule, S initialState, System.Func<S, A, S> folder, System.Func<System.ValueTuple<S, A>, System.Boolean> predicate)`
- `public static LanguageExt.Traits.K<M, S> FoldWhileIO<S, A>(LanguageExt.Traits.K<M, A> ma, S initialState, System.Func<S, A, S> folder, System.Func<S, System.Boolean> stateIs)`
- `public static LanguageExt.Traits.K<M, S> FoldWhileIO<S, A>(LanguageExt.Traits.K<M, A> ma, S initialState, System.Func<S, A, S> folder, System.Func<A, System.Boolean> valueIs)`
- `public static LanguageExt.Traits.K<M, S> FoldWhileIO<S, A>(LanguageExt.Traits.K<M, A> ma, S initialState, System.Func<S, A, S> folder, System.Func<System.ValueTuple<S, A>, System.Boolean> predicate)`
- `public static LanguageExt.Traits.K<M, S> FoldWhileIO<S, A>(LanguageExt.Traits.K<M, A> ma, LanguageExt.Schedule schedule, S initialState, System.Func<S, A, S> folder, System.Func<S, System.Boolean> stateIs)`
- `public static LanguageExt.Traits.K<M, S> FoldWhileIO<S, A>(LanguageExt.Traits.K<M, A> ma, LanguageExt.Schedule schedule, S initialState, System.Func<S, A, S> folder, System.Func<A, System.Boolean> valueIs)`
- `public static LanguageExt.Traits.K<M, S> FoldWhileIO<S, A>(LanguageExt.Traits.K<M, A> ma, LanguageExt.Schedule schedule, S initialState, System.Func<S, A, S> folder, System.Func<System.ValueTuple<S, A>, System.Boolean> predicate)`
- `public static LanguageExt.Traits.K<M, LanguageExt.ForkIO<A>> ForkIO<A>(LanguageExt.Traits.K<M, A> ma, LanguageExt.Option<System.TimeSpan> timeout)`
- `public static LanguageExt.Traits.K<M, A> LocalIO<A>(LanguageExt.Traits.K<M, A> ma)`
- `public static LanguageExt.Traits.K<M, B> MapIO<A, B>(LanguageExt.Traits.K<M, A> ma, System.Func<LanguageExt.IO<A>, LanguageExt.IO<B>> f)`
- `public static LanguageExt.Traits.K<M, A> PostIO<A>(LanguageExt.Traits.K<M, A> ma)`
- `public static LanguageExt.Traits.K<M, A> RepeatIO<A>(LanguageExt.Traits.K<M, A> ma)`
- `public static LanguageExt.Traits.K<M, A> RepeatIO<A>(LanguageExt.Traits.K<M, A> ma, LanguageExt.Schedule schedule)`
- `public static LanguageExt.Traits.K<M, A> RepeatUntilIO<A>(LanguageExt.Traits.K<M, A> ma, System.Func<A, System.Boolean> predicate)`
- `public static LanguageExt.Traits.K<M, A> RepeatUntilIO<A>(LanguageExt.Traits.K<M, A> ma, LanguageExt.Schedule schedule, System.Func<A, System.Boolean> predicate)`
- `public static LanguageExt.Traits.K<M, A> RepeatWhileIO<A>(LanguageExt.Traits.K<M, A> ma, System.Func<A, System.Boolean> predicate)`
- `public static LanguageExt.Traits.K<M, A> RepeatWhileIO<A>(LanguageExt.Traits.K<M, A> ma, LanguageExt.Schedule schedule, System.Func<A, System.Boolean> predicate)`
- `public static LanguageExt.Traits.K<M, A> RetryIO<A>(LanguageExt.Traits.K<M, A> ma)`
- `public static LanguageExt.Traits.K<M, A> RetryIO<A>(LanguageExt.Traits.K<M, A> ma, LanguageExt.Schedule schedule)`
- `public static LanguageExt.Traits.K<M, A> RetryUntilIO<A>(LanguageExt.Traits.K<M, A> ma, System.Func<LanguageExt.Common.Error, System.Boolean> predicate)`
- `public static LanguageExt.Traits.K<M, A> RetryUntilIO<A>(LanguageExt.Traits.K<M, A> ma, LanguageExt.Schedule schedule, System.Func<LanguageExt.Common.Error, System.Boolean> predicate)`
- `public static LanguageExt.Traits.K<M, A> RetryWhileIO<A>(LanguageExt.Traits.K<M, A> ma, System.Func<LanguageExt.Common.Error, System.Boolean> predicate)`
- `public static LanguageExt.Traits.K<M, A> RetryWhileIO<A>(LanguageExt.Traits.K<M, A> ma, LanguageExt.Schedule schedule, System.Func<LanguageExt.Common.Error, System.Boolean> predicate)`
- `public static LanguageExt.Traits.K<M, A> TimeoutIO<A>(LanguageExt.Traits.K<M, A> ma, System.TimeSpan timeout)`
- `public static LanguageExt.Traits.K<M, LanguageExt.IO<A>> ToIO<A>(LanguageExt.Traits.K<M, A> ma)`
- `public static LanguageExt.Traits.K<M, A> UninterruptibleIO<A>(LanguageExt.Traits.K<M, A> ma)`

### MonadUnliftIO`1<M> (interface) : LanguageExt.Traits.Maybe+MonadIO<M>

- `where M : LanguageExt.Traits.Maybe+MonadUnliftIO<M>, LanguageExt.Traits.Monad<M>`

- `public static LanguageExt.Traits.K<M, A> AwaitMaybe<A>(LanguageExt.Traits.K<M, LanguageExt.ForkIO<A>> ma)`
- `public static LanguageExt.Traits.K<M, A> BracketIOMaybe<A>(LanguageExt.Traits.K<M, A> ma)`
- `public static LanguageExt.Traits.K<M, C> BracketIOMaybe<A, B, C>(LanguageExt.Traits.K<M, A> Acq, System.Func<A, LanguageExt.IO<C>> Use, System.Func<A, LanguageExt.IO<B>> Fin)`
- `public static LanguageExt.Traits.K<M, C> BracketIOMaybe<A, B, C>(LanguageExt.Traits.K<M, A> Acq, System.Func<A, LanguageExt.IO<C>> Use, System.Func<LanguageExt.Common.Error, LanguageExt.IO<C>> Catch, System.Func<A, LanguageExt.IO<B>> Fin)`
- `public static LanguageExt.Traits.K<M, S> FoldIOMaybe<S, A>(LanguageExt.Traits.K<M, A> ma, S initialState, System.Func<S, A, S> folder)`
- `public static LanguageExt.Traits.K<M, S> FoldIOMaybe<S, A>(LanguageExt.Traits.K<M, A> ma, LanguageExt.Schedule schedule, S initialState, System.Func<S, A, S> folder)`
- `public static LanguageExt.Traits.K<M, S> FoldUntilIOMaybe<S, A>(LanguageExt.Traits.K<M, A> ma, S initialState, System.Func<S, A, S> folder, System.Func<S, System.Boolean> stateIs)`
- `public static LanguageExt.Traits.K<M, S> FoldUntilIOMaybe<S, A>(LanguageExt.Traits.K<M, A> ma, S initialState, System.Func<S, A, S> folder, System.Func<A, System.Boolean> valueIs)`
- `public static LanguageExt.Traits.K<M, S> FoldUntilIOMaybe<S, A>(LanguageExt.Traits.K<M, A> ma, S initialState, System.Func<S, A, S> folder, System.Func<System.ValueTuple<S, A>, System.Boolean> predicate)`
- `public static LanguageExt.Traits.K<M, S> FoldUntilIOMaybe<S, A>(LanguageExt.Traits.K<M, A> ma, LanguageExt.Schedule schedule, S initialState, System.Func<S, A, S> folder, System.Func<S, System.Boolean> stateIs)`
- `public static LanguageExt.Traits.K<M, S> FoldUntilIOMaybe<S, A>(LanguageExt.Traits.K<M, A> ma, LanguageExt.Schedule schedule, S initialState, System.Func<S, A, S> folder, System.Func<A, System.Boolean> valueIs)`
- `public static LanguageExt.Traits.K<M, S> FoldUntilIOMaybe<S, A>(LanguageExt.Traits.K<M, A> ma, LanguageExt.Schedule schedule, S initialState, System.Func<S, A, S> folder, System.Func<System.ValueTuple<S, A>, System.Boolean> predicate)`
- `public static LanguageExt.Traits.K<M, S> FoldWhileIOMaybe<S, A>(LanguageExt.Traits.K<M, A> ma, S initialState, System.Func<S, A, S> folder, System.Func<S, System.Boolean> stateIs)`
- `public static LanguageExt.Traits.K<M, S> FoldWhileIOMaybe<S, A>(LanguageExt.Traits.K<M, A> ma, S initialState, System.Func<S, A, S> folder, System.Func<A, System.Boolean> valueIs)`
- `public static LanguageExt.Traits.K<M, S> FoldWhileIOMaybe<S, A>(LanguageExt.Traits.K<M, A> ma, S initialState, System.Func<S, A, S> folder, System.Func<System.ValueTuple<S, A>, System.Boolean> predicate)`
- `public static LanguageExt.Traits.K<M, S> FoldWhileIOMaybe<S, A>(LanguageExt.Traits.K<M, A> ma, LanguageExt.Schedule schedule, S initialState, System.Func<S, A, S> folder, System.Func<S, System.Boolean> stateIs)`
- `public static LanguageExt.Traits.K<M, S> FoldWhileIOMaybe<S, A>(LanguageExt.Traits.K<M, A> ma, LanguageExt.Schedule schedule, S initialState, System.Func<S, A, S> folder, System.Func<A, System.Boolean> valueIs)`
- `public static LanguageExt.Traits.K<M, S> FoldWhileIOMaybe<S, A>(LanguageExt.Traits.K<M, A> ma, LanguageExt.Schedule schedule, S initialState, System.Func<S, A, S> folder, System.Func<System.ValueTuple<S, A>, System.Boolean> predicate)`
- `public static LanguageExt.Traits.K<M, LanguageExt.ForkIO<A>> ForkIOMaybe<A>(LanguageExt.Traits.K<M, A> ma, LanguageExt.Option<System.TimeSpan> timeout)`
- `public static LanguageExt.Traits.K<M, A> LocalIOMaybe<A>(LanguageExt.Traits.K<M, A> ma)`
- `public static LanguageExt.Traits.K<M, B> MapIOMaybe<A, B>(LanguageExt.Traits.K<M, A> ma, System.Func<LanguageExt.IO<A>, LanguageExt.IO<B>> f)`
- `public static LanguageExt.Traits.K<M, A> PostIOMaybe<A>(LanguageExt.Traits.K<M, A> ma)`
- `public static LanguageExt.Traits.K<M, A> RepeatIOMaybe<A>(LanguageExt.Traits.K<M, A> ma)`
- `public static LanguageExt.Traits.K<M, A> RepeatIOMaybe<A>(LanguageExt.Traits.K<M, A> ma, LanguageExt.Schedule schedule)`
- `public static LanguageExt.Traits.K<M, A> RepeatUntilIOMaybe<A>(LanguageExt.Traits.K<M, A> ma, System.Func<A, System.Boolean> predicate)`
- `public static LanguageExt.Traits.K<M, A> RepeatUntilIOMaybe<A>(LanguageExt.Traits.K<M, A> ma, LanguageExt.Schedule schedule, System.Func<A, System.Boolean> predicate)`
- `public static LanguageExt.Traits.K<M, A> RepeatWhileIOMaybe<A>(LanguageExt.Traits.K<M, A> ma, System.Func<A, System.Boolean> predicate)`
- `public static LanguageExt.Traits.K<M, A> RepeatWhileIOMaybe<A>(LanguageExt.Traits.K<M, A> ma, LanguageExt.Schedule schedule, System.Func<A, System.Boolean> predicate)`
- `public static LanguageExt.Traits.K<M, A> RetryIOMaybe<A>(LanguageExt.Traits.K<M, A> ma)`
- `public static LanguageExt.Traits.K<M, A> RetryIOMaybe<A>(LanguageExt.Traits.K<M, A> ma, LanguageExt.Schedule schedule)`
- `public static LanguageExt.Traits.K<M, A> RetryUntilIOMaybe<A>(LanguageExt.Traits.K<M, A> ma, System.Func<LanguageExt.Common.Error, System.Boolean> predicate)`
- `public static LanguageExt.Traits.K<M, A> RetryUntilIOMaybe<A>(LanguageExt.Traits.K<M, A> ma, LanguageExt.Schedule schedule, System.Func<LanguageExt.Common.Error, System.Boolean> predicate)`
- `public static LanguageExt.Traits.K<M, A> RetryWhileIOMaybe<A>(LanguageExt.Traits.K<M, A> ma, System.Func<LanguageExt.Common.Error, System.Boolean> predicate)`
- `public static LanguageExt.Traits.K<M, A> RetryWhileIOMaybe<A>(LanguageExt.Traits.K<M, A> ma, LanguageExt.Schedule schedule, System.Func<LanguageExt.Common.Error, System.Boolean> predicate)`
- `public static LanguageExt.Traits.K<M, A> TimeoutIOMaybe<A>(LanguageExt.Traits.K<M, A> ma, System.TimeSpan timeout)`
- `public static LanguageExt.Traits.K<M, LanguageExt.IO<A>> ToIOMaybe<A>(LanguageExt.Traits.K<M, A> ma)`

### Monad`1<M> (interface) : LanguageExt.Traits.Applicative<M>, LanguageExt.Traits.Functor<M>, LanguageExt.Traits.Maybe+MonadUnliftIO<M>, LanguageExt.Traits.Maybe+MonadIO<M>

- `where M : LanguageExt.Traits.Monad<M>`

- `public static LanguageExt.Traits.K<M, B> Bind<A, B>(LanguageExt.Traits.K<M, A> ma, System.Func<A, LanguageExt.Traits.K<M, B>> f)`
- `public static LanguageExt.Traits.K<M, LanguageExt.Next<A, B>> Done<A, B>(B value)`
- `public static LanguageExt.Traits.K<M, A> Flatten<A>(LanguageExt.Traits.K<M, LanguageExt.Traits.K<M, A>> mma)`
- `public static LanguageExt.Traits.K<M, LanguageExt.Next<A, B>> Loop<A, B>(A value)`
- `public static LanguageExt.Traits.K<M, B> Recur<A, B>(A value, System.Func<A, LanguageExt.Traits.K<M, LanguageExt.Next<A, B>>> f)`
- `public static LanguageExt.Traits.K<M, C> SelectMany<A, B, C>(LanguageExt.Traits.K<M, A> ma, System.Func<A, LanguageExt.Traits.K<M, B>> bind, System.Func<A, B, C> project)`
- `public static LanguageExt.Traits.K<M, C> SelectMany<A, B, C>(LanguageExt.Traits.K<M, A> ma, System.Func<A, LanguageExt.Pure<B>> bind, System.Func<A, B, C> project)`

### MonoidInstance`1<A> (class) : LanguageExt.Traits.SemigroupInstance<A>, System.IEquatable<LanguageExt.Traits.SemigroupInstance<A>>, System.IEquatable<LanguageExt.Traits.MonoidInstance<A>>

- `public MonoidInstance`1(A Empty, System.Func<A, A, A> Combine)`
- `public A Empty { get; init; }`
- `public static LanguageExt.Option<LanguageExt.Traits.MonoidInstance<A>> Instance { get; }`
- `public LanguageExt.Traits.MonoidInstance<A> <Clone>$()`
- `public System.Void Deconstruct(out A& Empty, out System.Func<A, A, A>& Combine)`
- `public System.Boolean Equals(System.Object obj)`
- `public System.Boolean Equals(LanguageExt.Traits.SemigroupInstance<A> other)`
- `public System.Boolean Equals(LanguageExt.Traits.MonoidInstance<A> other)`
- `public System.Int32 GetHashCode()`
- `public System.String ToString()`

### MonoidK (class [static])

- `public static LanguageExt.Traits.K<M, B> choose<M, A, B>(LanguageExt.Traits.K<M, A> ma, System.Func<A, LanguageExt.Option<B>> selector)`
- `where M : LanguageExt.Traits.MonoidK<M>, LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, A> combine<M, A>(System.Collections.Generic.IEnumerable<LanguageExt.Traits.K<M, A>> xs)`
- `where M : LanguageExt.Traits.MonoidK<M>`
- `public static LanguageExt.Traits.K<M, A> combine<M, A>(LanguageExt.Seq<LanguageExt.Traits.K<M, A>> xs)`
- `where M : LanguageExt.Traits.MonoidK<M>`
- `public static LanguageExt.Traits.K<F, A> combine<F, A>(LanguageExt.Traits.K<F, A> ma, LanguageExt.Traits.K<F, A> mb)`
- `where F : LanguageExt.Traits.MonoidK<F>`
- `public static LanguageExt.Traits.K<M, A> combine<M, A>(LanguageExt.Traits.K<M, A> mx, LanguageExt.Traits.K<M, A> my, LanguageExt.Traits.K<M, A> mz, params LanguageExt.Traits.K<M, A>[] xs)`
- `where M : LanguageExt.Traits.MonoidK<M>`
- `public static LanguageExt.Traits.K<F, A> empty<F, A>()`
- `where F : LanguageExt.Traits.MonoidK<F>`
- `public static LanguageExt.Traits.K<M, A> filter<M, A>(LanguageExt.Traits.K<M, A> ma, System.Func<A, System.Boolean> predicate)`
- `where M : LanguageExt.Traits.MonoidK<M>, LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<F, LanguageExt.Unit> guard<F>(System.Boolean flag)`
- `where F : LanguageExt.Traits.MonoidK<F>, LanguageExt.Traits.Applicative<F>`

### MonoidK`1<M> (interface) : LanguageExt.Traits.SemigroupK<M>

- `where M : LanguageExt.Traits.MonoidK<M>`

- `public static LanguageExt.Traits.K<M, A> Empty<A>()`

### Monoid`1<A> (interface) : LanguageExt.Traits.Semigroup<A>

- `where A : LanguageExt.Traits.Monoid<A>`

- `public static A Empty { get; }`
- `public static LanguageExt.Traits.MonoidInstance<A> Instance { get; }`

### Mutates (class [static])

- `public static LanguageExt.Traits.K<F, A> mutate<F, Env, A>(System.Func<A, A> f)`
- `where F : LanguageExt.Traits.Functor<F>`
- `where Env : LanguageExt.Traits.Mutates<F, A>`

### Mutates`2<M, InnerEnv> (interface) : LanguageExt.Traits.Has<M, InnerEnv>

- `where M : LanguageExt.Traits.Functor<M>`

- `public static LanguageExt.Traits.K<M, LanguageExt.Atom<InnerEnv>> Mutable { get; }`

### Natural (class [static])

- `public static LanguageExt.Traits.K<G, A> transform<F, G, A>(LanguageExt.Traits.K<F, A> fa)`
- `where F : LanguageExt.Traits.Natural<F, G>`

### NaturalEpi`2<F, G> (interface) : LanguageExt.Traits.CoNatural<F, G>, LanguageExt.Traits.Natural<G, F>

- `where F : LanguageExt.Traits.CoNatural<F, G>`


### NaturalIso`2<F, G> (interface) : LanguageExt.Traits.Natural<F, G>, LanguageExt.Traits.CoNatural<F, G>


### NaturalMono`2<F, G> (interface) : LanguageExt.Traits.Natural<F, G>, LanguageExt.Traits.CoNatural<G, F>

- `where F : LanguageExt.Traits.Natural<F, G>`


### Natural`2<F, G> (interface)

- `public static LanguageExt.Traits.K<G, A> Transform<A>(LanguageExt.Traits.K<F, A> fa)`

### Num`1<A> (interface) : LanguageExt.Traits.Ord<A>, LanguageExt.Traits.Eq<A>, LanguageExt.Hashable<A>, LanguageExt.Traits.Trait, LanguageExt.Traits.Arithmetic<A>

- `public static A Abs(A x)`
- `public static A Divide(A x, A y)`
- `public static A FromDecimal(System.Decimal x)`
- `public static A FromDouble(System.Double x)`
- `public static A FromFloat(System.Single x)`
- `public static A FromInteger(System.Int32 x)`
- `public static A Signum(A x)`

### OptionTExtensions (class [static])

- `public static LanguageExt.OptionT<M, A> op_BitwiseOr<X, M, A>(LanguageExt.Traits.K<LanguageExt.OptionT<M>, A> lhs, LanguageExt.Finally<M, X> rhs)`
- `where M : LanguageExt.Traits.Monad<M>, LanguageExt.Traits.Final<M>`

### Ord`1<A> (interface) : LanguageExt.Traits.Eq<A>, LanguageExt.Hashable<A>, LanguageExt.Traits.Trait

- `public static System.Int32 Compare(A x, A y)`

### Pred`1<A> (interface) : LanguageExt.Traits.Trait

- `public static System.Boolean True(A value)`

### RWSTExtensions (class [static])

- `public static LanguageExt.RWST<R, W, S, M, A> op_BitwiseOr<R, W, S, X, M, A>(LanguageExt.Traits.K<LanguageExt.RWST<R, W, S, M>, A> lhs, LanguageExt.Finally<M, X> rhs)`
- `where W : LanguageExt.Traits.Monoid<W>`
- `where M : LanguageExt.Traits.Monad<M>, LanguageExt.Traits.Final<M>`

### Readable (class [static])

- `public static LanguageExt.Traits.K<M, Env> ask<M, Env>()`
- `where M : LanguageExt.Traits.Readable<M, Env>`
- `public static LanguageExt.Traits.K<M, A> asks<M, Env, A>(System.Func<Env, A> f)`
- `where M : LanguageExt.Traits.Readable<M, Env>`
- `public static LanguageExt.Traits.K<M, A> asksM<M, Env, A>(System.Func<Env, LanguageExt.Traits.K<M, A>> f)`
- `where M : LanguageExt.Traits.Readable<M, Env>, LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, A> local<M, Env, A>(System.Func<Env, Env> f, LanguageExt.Traits.K<M, A> ma)`
- `where M : LanguageExt.Traits.Readable<M, Env>`

### Readable`2<M, Env> (interface)

- `where M : LanguageExt.Traits.Readable<M, Env>`

- `public static LanguageExt.Traits.K<M, Env> Ask { get; }`
- `public static LanguageExt.Traits.K<M, A> Asks<A>(System.Func<Env, A> f)`
- `public static LanguageExt.Traits.K<M, A> Local<A>(System.Func<Env, Env> f, LanguageExt.Traits.K<M, A> ma)`

### ReaderTExtensions (class [static])

- `public static LanguageExt.ReaderT<Env, M, A> op_BitwiseOr<X, Env, M, A>(LanguageExt.Traits.K<LanguageExt.ReaderT<Env, M>, A> lhs, LanguageExt.Finally<M, X> rhs)`
- `where M : LanguageExt.Traits.Monad<M>, LanguageExt.Traits.Final<M>`

### SemigroupInstance`1<A> (class) : System.IEquatable<LanguageExt.Traits.SemigroupInstance<A>>

- `public SemigroupInstance`1(System.Func<A, A, A> Combine)`
- `public System.Func<A, A, A> Combine { get; init; }`
- `public static LanguageExt.Option<LanguageExt.Traits.SemigroupInstance<A>> Instance { get; }`
- `public LanguageExt.Traits.SemigroupInstance<A> <Clone>$()`
- `public System.Void Deconstruct(out System.Func<A, A, A>& Combine)`
- `public System.Boolean Equals(System.Object obj)`
- `public System.Boolean Equals(LanguageExt.Traits.SemigroupInstance<A> other)`
- `public System.Int32 GetHashCode()`
- `public System.String ToString()`

### SemigroupK`1<M> (interface)

- `where M : LanguageExt.Traits.SemigroupK<M>`

- `public static LanguageExt.Traits.K<M, A> Combine<A>(LanguageExt.Traits.K<M, A> lhs, LanguageExt.Traits.K<M, A> rhs)`

### Semigroup`1<A> (interface)

- `where A : LanguageExt.Traits.Semigroup<A>`

- `public static LanguageExt.Traits.SemigroupInstance<A> Instance { get; }`
- `public A Combine(A rhs)`

### StateTExtensions (class [static])

- `public static LanguageExt.StateT<S, M, A> op_BitwiseOr<S, X, M, A>(LanguageExt.Traits.K<LanguageExt.StateT<S, M>, A> lhs, LanguageExt.Finally<M, X> rhs)`
- `where M : LanguageExt.Traits.Monad<M>, LanguageExt.Traits.Final<M>`

### Stateful (class [static])

- `public static LanguageExt.Traits.K<M, S> get<M, S>()`
- `where M : LanguageExt.Traits.Stateful<M, S>`
- `public static LanguageExt.Traits.K<M, A> gets<M, S, A>(System.Func<S, A> f)`
- `where M : LanguageExt.Traits.Stateful<M, S>`
- `public static LanguageExt.Traits.K<M, A> getsM<M, S, A>(System.Func<S, LanguageExt.Traits.K<M, A>> f)`
- `where M : LanguageExt.Traits.Stateful<M, S>, LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, A> local<M, S, A>(LanguageExt.Traits.K<M, LanguageExt.Unit> stateSetter, LanguageExt.Traits.K<M, A> operation)`
- `where M : LanguageExt.Traits.Stateful<M, S>, LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, A> local<M, S, A>(System.Func<S, S> stateSetter, LanguageExt.Traits.K<M, A> operation)`
- `where M : LanguageExt.Traits.Stateful<M, S>, LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, LanguageExt.Unit> modify<M, S>(System.Func<S, S> modify)`
- `where M : LanguageExt.Traits.Stateful<M, S>`
- `public static LanguageExt.Traits.K<M, LanguageExt.Unit> modifyM<M, S>(System.Func<S, LanguageExt.Traits.K<M, S>> modify)`
- `where M : LanguageExt.Traits.Stateful<M, S>, LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, LanguageExt.Unit> put<M, S>(S value)`
- `where M : LanguageExt.Traits.Stateful<M, S>`
- `public static LanguageExt.Traits.K<M, A> state<M, S, A>(System.Func<S, System.ValueTuple<A, S>> f)`
- `where M : LanguageExt.Traits.Stateful<M, S>, LanguageExt.Traits.Monad<M>`

### Stateful`2<M, S> (interface)

- `where M : LanguageExt.Traits.Stateful<M, S>`

- `public static LanguageExt.Traits.K<M, S> Get { get; }`
- `public static LanguageExt.Traits.K<M, A> Gets<A>(System.Func<S, A> f)`
- `public static LanguageExt.Traits.K<M, LanguageExt.Unit> Modify(System.Func<S, S> modify)`
- `public static LanguageExt.Traits.K<M, LanguageExt.Unit> Put(S value)`

### TokenStream`2<TOKENS, TOKEN> (interface)

- `where TOKENS : LanguageExt.Traits.TokenStream<TOKENS, TOKEN>`

- `public static System.Int32 ChunkLength(in TOKENS& tokens)`
- `public static System.ReadOnlySpan<TOKEN> ChunkToTokens(in TOKENS& tokens)`
- `public static System.Boolean IsNewline(TOKEN token)`
- `public static System.Boolean IsTab(TOKEN token)`
- `public static System.Boolean Take(System.Int32 amount, in TOKENS& stream, out TOKENS& head, out TOKENS& tail)`
- `public static System.Boolean Take1(in TOKENS& stream, out TOKEN& head, out TOKENS& tail)`
- `public static System.Void TakeWhile(System.Func<TOKEN, System.Boolean> predicate, in TOKENS& stream, out TOKENS& head, out TOKENS& tail)`
- `public static TOKENS TokenToChunk(in TOKEN& token)`
- `public static System.ReadOnlySpan<System.Char> TokenToString(TOKEN token)`
- `public static TOKENS TokensToChunk(in System.ReadOnlySpan<TOKEN>& token)`

### Trait (interface)


### Traversable (class [static])

- `public static LanguageExt.Traits.K<F, LanguageExt.Traits.K<T, A>> sequence<T, F, A>(LanguageExt.Traits.K<T, LanguageExt.Traits.K<F, A>> ta)`
- `where T : LanguageExt.Traits.Traversable<T>`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<M, LanguageExt.Traits.K<T, A>> sequenceM<T, M, A>(LanguageExt.Traits.K<T, LanguageExt.Traits.K<M, A>> ta)`
- `where T : LanguageExt.Traits.Traversable<T>`
- `where M : LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<F, LanguageExt.Traits.K<T, B>> traverse<T, F, A, B>(System.Func<A, LanguageExt.Traits.K<F, B>> f, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Traversable<T>`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<M, LanguageExt.Traits.K<T, B>> traverseM<T, M, A, B>(System.Func<A, LanguageExt.Traits.K<M, B>> f, LanguageExt.Traits.K<T, A> ta)`
- `where T : LanguageExt.Traits.Traversable<T>`
- `where M : LanguageExt.Traits.Monad<M>`

### Traversable`1<T> (interface) : LanguageExt.Traits.Functor<T>, LanguageExt.Traits.Foldable<T>

- `where T : LanguageExt.Traits.Traversable<T>, LanguageExt.Traits.Functor<T>, LanguageExt.Traits.Foldable<T>`

- `public static LanguageExt.Traits.K<F, LanguageExt.Traits.K<T, A>> Sequence<F, A>(LanguageExt.Traits.K<T, LanguageExt.Traits.K<F, A>> ta)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, LanguageExt.Traits.K<T, A>> SequenceM<F, A>(LanguageExt.Traits.K<T, LanguageExt.Traits.K<F, A>> ta)`
- `where F : LanguageExt.Traits.Monad<F>`
- `public static LanguageExt.Traits.K<F, LanguageExt.Traits.K<T, B>> Traverse<F, A, B>(System.Func<A, LanguageExt.Traits.K<F, B>> f, LanguageExt.Traits.K<T, A> ta)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<F, LanguageExt.Traits.K<T, B>> TraverseDefault<F, A, B>(System.Func<A, LanguageExt.Traits.K<F, B>> f, LanguageExt.Traits.K<T, A> ta)`
- `where F : LanguageExt.Traits.Applicative<F>`
- `public static LanguageExt.Traits.K<M, LanguageExt.Traits.K<T, B>> TraverseM<M, A, B>(System.Func<A, LanguageExt.Traits.K<M, B>> f, LanguageExt.Traits.K<T, A> ta)`
- `where M : LanguageExt.Traits.Monad<M>`

### TryExtensions (class [static])

- `public static LanguageExt.Try<A> op_BitwiseOr<X, A>(LanguageExt.Traits.K<LanguageExt.Try, A> lhs, LanguageExt.Finally<LanguageExt.Try, X> rhs)`

### Writable (class [static])

- `public static LanguageExt.Traits.K<M, A> censor<W, M, A>(System.Func<W, W> f, LanguageExt.Traits.K<M, A> ma)`
- `where W : LanguageExt.Traits.Monoid<W>`
- `where M : LanguageExt.Traits.Writable<M, W>, LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, System.ValueTuple<A, W>> listen<W, M, A>(LanguageExt.Traits.K<M, A> ma)`
- `where W : LanguageExt.Traits.Monoid<W>`
- `where M : LanguageExt.Traits.Writable<M, W>`
- `public static LanguageExt.Traits.K<M, System.ValueTuple<A, B>> listens<W, M, A, B>(System.Func<W, B> f, LanguageExt.Traits.K<M, A> ma)`
- `where W : LanguageExt.Traits.Monoid<W>`
- `where M : LanguageExt.Traits.Writable<M, W>, LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, A> pass<W, M, A>(LanguageExt.Traits.K<M, System.ValueTuple<A, System.Func<W, W>>> action)`
- `where W : LanguageExt.Traits.Monoid<W>`
- `where M : LanguageExt.Traits.Writable<M, W>`
- `public static LanguageExt.Traits.K<M, LanguageExt.Unit> tell<M, W>(W item)`
- `where M : LanguageExt.Traits.Writable<M, W>`
- `where W : LanguageExt.Traits.Monoid<W>`
- `public static LanguageExt.Traits.K<M, A> write<W, M, A>(System.ValueTuple<A, W> item)`
- `where W : LanguageExt.Traits.Monoid<W>`
- `where M : LanguageExt.Traits.Writable<M, W>, LanguageExt.Traits.Monad<M>`
- `public static LanguageExt.Traits.K<M, A> write<W, M, A>(A value, W item)`
- `where W : LanguageExt.Traits.Monoid<W>`
- `where M : LanguageExt.Traits.Writable<M, W>, LanguageExt.Traits.Monad<M>`

### Writable`2<M, W> (interface)

- `where M : LanguageExt.Traits.Writable<M, W>`
- `where W : LanguageExt.Traits.Monoid<W>`

- `public static LanguageExt.Traits.K<M, System.ValueTuple<A, W>> Listen<A>(LanguageExt.Traits.K<M, A> ma)`
- `public static LanguageExt.Traits.K<M, A> Pass<A>(LanguageExt.Traits.K<M, System.ValueTuple<A, System.Func<W, W>>> action)`
- `public static LanguageExt.Traits.K<M, LanguageExt.Unit> Tell(W item)`

### WriterTExtensions (class [static])

- `public static LanguageExt.WriterT<W, M, A> op_BitwiseOr<W, X, M, A>(LanguageExt.Traits.K<LanguageExt.WriterT<W, M>, A> lhs, LanguageExt.Finally<M, X> rhs)`
- `where W : LanguageExt.Traits.Monoid<W>`
- `where M : LanguageExt.Traits.Monad<M>, LanguageExt.Traits.Final<M>`

## LanguageExt.Traits.Domain

### Amount`2<SELF, SCALAR> (interface) : LanguageExt.Traits.Domain.VectorSpace<SELF, SCALAR>, LanguageExt.Traits.Domain.Identifier<SELF>, LanguageExt.Traits.Domain.DomainType<SELF>, System.IEquatable<SELF>, System.Numerics.IEqualityOperators<SELF, SELF, System.Boolean>, System.Numerics.IUnaryNegationOperators<SELF, SELF>, System.Numerics.IAdditionOperators<SELF, SELF, SELF>, System.Numerics.ISubtractionOperators<SELF, SELF, SELF>, System.Numerics.IMultiplyOperators<SELF, SCALAR, SELF>, System.Numerics.IDivisionOperators<SELF, SCALAR, SELF>, System.IComparable<SELF>, System.Numerics.IComparisonOperators<SELF, SELF, System.Boolean>

- `where SELF : LanguageExt.Traits.Domain.Amount<SELF, SCALAR>`


### DomainType`1<SELF> (interface)

- `where SELF : LanguageExt.Traits.Domain.DomainType<SELF>`


### DomainType`2<SELF, REPR> (interface) : LanguageExt.Traits.Domain.DomainType<SELF>

- `where SELF : LanguageExt.Traits.Domain.DomainType<SELF, REPR>`

- `public static LanguageExt.Fin<SELF> From(REPR repr)`
- `public static SELF FromUnsafe(REPR repr)`
- `public REPR To()`

### Identifier`1<SELF> (interface) : LanguageExt.Traits.Domain.DomainType<SELF>, System.IEquatable<SELF>, System.Numerics.IEqualityOperators<SELF, SELF, System.Boolean>

- `where SELF : LanguageExt.Traits.Domain.Identifier<SELF>`


### Locus`3<SELF, DISTANCE, DISTANCE_SCALAR> (interface) : LanguageExt.Traits.Domain.Identifier<SELF>, LanguageExt.Traits.Domain.DomainType<SELF>, System.IEquatable<SELF>, System.Numerics.IEqualityOperators<SELF, SELF, System.Boolean>, System.IComparable<SELF>, System.Numerics.IComparisonOperators<SELF, SELF, System.Boolean>, System.Numerics.IUnaryNegationOperators<SELF, SELF>, System.Numerics.IAdditiveIdentity<SELF, SELF>, System.Numerics.IAdditionOperators<SELF, DISTANCE, SELF>, System.Numerics.ISubtractionOperators<SELF, SELF, DISTANCE>

- `where SELF : LanguageExt.Traits.Domain.Locus<SELF, DISTANCE, DISTANCE_SCALAR>`
- `where DISTANCE : LanguageExt.Traits.Domain.Amount<DISTANCE, DISTANCE_SCALAR>`


### VectorSpace`2<SELF, SCALAR> (interface) : LanguageExt.Traits.Domain.Identifier<SELF>, LanguageExt.Traits.Domain.DomainType<SELF>, System.IEquatable<SELF>, System.Numerics.IEqualityOperators<SELF, SELF, System.Boolean>, System.Numerics.IUnaryNegationOperators<SELF, SELF>, System.Numerics.IAdditionOperators<SELF, SELF, SELF>, System.Numerics.ISubtractionOperators<SELF, SELF, SELF>, System.Numerics.IMultiplyOperators<SELF, SCALAR, SELF>, System.Numerics.IDivisionOperators<SELF, SCALAR, SELF>

- `where SELF : LanguageExt.Traits.Domain.VectorSpace<SELF, SCALAR>`


## LanguageExt.Traits.Range

### Range`3<SELF, NumOrdA, A> (interface) : System.Collections.Generic.IEnumerable<A>, System.Collections.IEnumerable, LanguageExt.Traits.K<SELF, A>

- `where SELF : LanguageExt.Traits.Range.Range<SELF, NumOrdA, A>`
- `where NumOrdA : LanguageExt.Traits.Ord<A>, LanguageExt.Traits.Num<A>`

- `public static SELF Zero`
- `public System.Object Case { get; }`
- `public A From { get; }`
- `public A Step { get; }`
- `public System.Boolean StepIsAscending { get; }`
- `public A To { get; }`
- `public LanguageExt.Iterable<A> AsIterable()`
- `public S Fold<S>(S state, System.Func<S, A, S> f)`
- `public static SELF FromCount(A min, A count, A step)`
- `public static SELF FromMinMax(A min, A max, A step)`
- `public System.Boolean InRange(A value)`
- `public static SELF New(A from, A to, A step)`
- `public System.Boolean Overlaps(SELF other)`
- `public LanguageExt.Seq<A> ToSeq()`

## LanguageExt.Traits.Resolve

### EqResolve`1<A> (class [static])

- `public static System.Func<A, A, System.Boolean> EqualsFunc`
- `public static System.Reflection.MethodInfo EqualsMethod`
- `public static System.IntPtr EqualsMethodPtr`
- `public static System.Func<A, System.Int32> GetHashCodeFunc`
- `public static System.Reflection.MethodInfo GetHashCodeMethod`
- `public static System.IntPtr GetHashCodeMethodPtr`
- `public static System.String ResolutionError`
- `public static System.Boolean Exists { get; }`
- `public static System.Boolean Equals(A lhs, A rhs)`
- `public static System.Int32 GetHashCode(A value)`

### HashableResolve`1<A> (class [static])

- `public static System.Func<A, System.Int32> GetHashCodeFunc`
- `public static System.Reflection.MethodInfo GetHashCodeMethod`
- `public static System.IntPtr GetHashCodeMethodPtr`
- `public static System.String ResolutionError`
- `public static System.Boolean Exists { get; }`
- `public static System.Int32 GetHashCode(A value)`

### OrdResolve`1<A> (class [static])

- `public static System.Func<A, A, System.Int32> CompareFunc`
- `public static System.Reflection.MethodInfo CompareMethod`
- `public static System.IntPtr CompareMethodPtr`
- `public static System.Func<A, A, System.Boolean> EqualsFunc`
- `public static System.Reflection.MethodInfo EqualsMethod`
- `public static System.IntPtr EqualsMethodPtr`
- `public static System.Func<A, System.Int32> GetHashCodeFunc`
- `public static System.Reflection.MethodInfo GetHashCodeMethod`
- `public static System.IntPtr GetHashCodeMethodPtr`
- `public static System.String ResolutionError`
- `public static System.Boolean Exists { get; }`
- `public static System.Int32 Compare(A lhs, A rhs)`
- `public static System.Boolean Equals(A lhs, A rhs)`
- `public static System.Int32 GetHashCode(A value)`

