# FunnySharp Core Design

FunnySharp should make demanding .NET programs smaller, clearer, and more trustworthy. Business
decisions should be visible in the code. Important distinctions should survive composition.
Execution should do only the work the problem requires. The ambition is to make powerful
functional techniques feel natural in production C#.

This document sets the project's enduring design direction and the judgment behind its
abstractions. The [product contract](product-contract.md) defines the current product boundary;
the [grammar](grammar.md) defines current API meanings; each [goal](goals/README.md) defines its
own outcome and completion evidence. This blueprint guides choices within that work. It adds no
acceptance gate and does not silently change a shipped contract. The prospective capabilities
below illustrate the direction; their delivery and research status remain in the goals and
[stability inventory](stability-inventory.md).

## The experience we are building

A developer should be able to express an application's essential decisions once and carry them
through a complete workflow: interpret input, establish domain facts, transform data, decide what
happens next, and execute the necessary work. The resulting program should be easy to explain,
difficult to misuse, and economical to run.

Imagine an order service in which a supported description of an address drives decoding and useful
error explanations; independent field errors are reported together; downstream decisions receive
trusted values; related reads can be batched; and state changes can be simulated before commands
reach real services. The value is the business logic that becomes explicit and the repeated work
that disappears. Each capability should also be useful on its own in an existing C# application.

The comparison is excellent ordinary C#: a clear loop, a well-factored method, a switch, LINQ,
or a suitable .NET facility. Count the caller's whole solution, including models, adapters,
configuration, error handling, and debugging. A short fluent expression earns its place when it
reduces that total burden or provides a meaningful guarantee. Lines of code are a useful symptom;
clarity and fewer independent decisions are the underlying objective.

This permits substantial ambition inside the library. A sophisticated implementation can be an
excellent investment when many callers gain a simpler model, stronger guarantees, or less work at
runtime. A small public surface can contain deep engineering.

## Find the model that removes the problem

Begin with the distinction that makes a problem difficult. Missing input, an explicit null, and
an invalid value can require different decisions. An independent check and a check that needs a
previous result have different composition rules. A relationship expected to have one match is
different from an arbitrary join. A rejected transition is different from a failed attempt to
execute an accepted command.

A good abstraction gives such a distinction a precise representation and makes the remaining
code straightforward. Its smallness comes from finding the right model, with enough information
to express the whole problem. Few types or few methods alone do not establish simplicity.

Types should carry facts that consumers can rely on. Construction establishes an invariant;
composition preserves it; explicit boundaries explain where it can be lost. Where a guarantee
depends on mutable storage, time, a database, or caller-provided behavior, its scope belongs in the
model. A type name cannot turn a read-only view into immutable data or a previous validation into
a permanent fact.

The existing outcome vocabulary is the shared foundation. New capabilities should carry their
domain meaning through that vocabulary and preserve useful error information. Expected domain
failure, absence, cancellation, and unexpected exceptions remain distinguishable. Independent
validation collects what can actually be checked; dependent work proceeds only when its
prerequisites exist. Recovering, discarding information, or changing failure policy is a visible
decision.

The best reusable pieces have a few understandable composition laws. Combining them should
preserve meaning, ordering, and ownership without forcing the caller to learn a new set of
exceptions. Generality is earned when the same model explains several real uses. A useful pattern
can remain an ordinary function or a documented idiom until a library abstraction improves it.

## Preserve meaning to unlock more capability

Some of the largest gains come from keeping useful structure available before execution. A
structured constraint can support validation, explanation, and generated test inputs. A declared
set of read requirements can expose batching opportunities. A pure decision can support both
production execution and deterministic simulation. A composed reduction can express several
summaries while consuming its input once.

This is a promising direction for FunnySharp: descriptions that preserve enough meaning to
support several useful interpretations. The payoff can be greater correctness, better diagnostics,
less duplicated code, or less execution work. The representation should fit the particular
problem. An application should be able to use a decoder without adopting a universal schema, or
compose decisions without moving the application into a universal program representation.

An ordinary delegate remains the natural form of executable behavior. Introduce inspectable
structure when inspection or another interpretation creates real value. Supported structure can
carry strong guarantees; arbitrary callbacks remain useful, opaque extensions. A predicate does
not reveal its inverse, a generator, its dependencies, or proof of purity. Honest limits make
derivation dependable and leave room for explicit caller-supplied knowledge.

Specialization, incremental computation, and richer domain descriptions belong in the design
space. Their promise is substantial: make a useful program cheaper by exploiting facts already
present in its meaning. Their adoption depends on the actual benefit and the cost of representing,
maintaining, and interpreting those facts. Research can discover a better abstraction or show that
an ordinary implementation is the right product choice.

## Stay native to .NET at every boundary

FunnySharp should feel like a capability added to the application. Callers keep their domain
types, delegates, collections, dependency ownership, and normal debugging tools. BCL values and
protocols are the common ground between features. Adopting one useful piece should be a local
decision, and returning to ordinary C# should be straightforward.

Keep business decisions explicit and, where useful, pure. State and messages can produce a next
state and descriptions of work; the application owns persistence and execution. Time, randomness,
and external responses become explicit inputs when reproducibility depends on them. This creates
room for simulation, replay, and composition while keeping the execution boundary small. Actual
transaction, durability, and recovery guarantees belong to the systems that provide them.

Asynchronous work is a first-class use case with normal .NET lifetimes. `Task`, `ValueTask`,
`IAsyncEnumerable<T>`, cancellation, and disposal retain their meaning. Sync and async operations
share a concept where both are useful; their shapes reflect real work and lifetime differences.
Concurrency expresses ownership, bounded admission, delivery order, and what happens to started
work when a caller leaves. A thin coordinator can remove difficult repetition while the .NET
runtime continues to own execution.

Immutability is a useful capability that callers can select. Pure calculations and structural
sharing can coexist with explicit mutation, builders, and caller-owned buffers. What matters is
that ownership and observation are clear: who can change a value, which references share it, and
when a result becomes visible. Choosing an immutable interface does not settle those questions.

Integration should preserve these advantages at application boundaries. ASP.NET Core adapters
should make domain outcomes and useful response metadata easy to express while leaving HTTP
policy with the application. The core remains independent of HTTP and has no runtime package
dependencies. Framework-specific knowledge belongs in an appropriate optional integration.

## Bring pragmatic taste to data work

The taste to carry forward from tidyverse is a close fit between the shape of a problem and the
operations used to express it: coherent verbs, useful intermediate values, and explicit changes
of shape. In C#, that means working with ordinary typed records and BCL data while making the
meaning of a transformation easy to see.

A transformation should make clear what happens to cardinality, missing values, order, and
identity. If a caller expects one customer for each order, that expectation should remain visible
when the data disagrees. If nested collections are aligned, their relationship should survive the
flattening. If an input fails, a field, key, or element location can be more valuable than another
layer of generic exception text.

These are opportunities to remove recurring reasoning from application code. Favor a few
composable operations that explain both ordinary and difficult inputs. Good diagnostics are part
of the abstraction's output: they retain enough context for a person or an agent to understand
what happened and act on it. Data-dependent edge cases belong in the model from the beginning.

Learn this way from every influence: identify the leverage in an idea, then express it honestly
through C# and .NET. The [goal references](goals/README.md#design-references) record those sources.
The project's identity comes from how the ideas work together for its users.

## Make performance a property of the design

The strongest optimization often removes work: fewer reads, one traversal instead of several,
short-circuiting before unnecessary input is consumed, sharing unchanged structure, or recomputing
only what changed. An abstraction that exposes these opportunities can improve both expression and
performance. A wrapper that obscures them makes both harder.

Reason about the complete lifecycle. Construction, execution, materialization, retained memory,
copying, and cleanup can dominate different workloads. Streaming can bound memory; batching can
reduce round trips; specialization can reduce dispatch. Each also introduces costs and trade-offs.
Zero allocation is one useful property, not proof of zero copying or efficient execution.

Representation and ownership should let the implementation choose an economical algorithm.
Internal mutation, pooled scratch space, or generated code can serve a simple public model when
their lifetime and semantics are sound. Conversely, an optimization is not free to change callback
order, failure behavior, aliasing, or observable effects. Reordering requires the relevant facts;
an arbitrary callback does not supply them.

Compare equivalent complete operations against strong baselines and disclose where a technique
pays for itself. A small, understood cost may buy a valuable guarantee. A performance claim needs
appropriate evidence, and a better algorithm deserves attention even before a microbenchmark can
resolve small timing differences. The [performance guidance](performance.md) owns the concrete
measurement policy; this document adds no numeric targets or measurement machinery.

## Make coherence amplify human and AI capability

AI increases how much design space we can explore and how much implementation detail we can
handle. That gives FunnySharp room to pursue demanding abstractions when their benefit is real.
The lasting constraint is whether people can understand, trust, and maintain the resulting system.

A coherent vocabulary makes new combinations predictable. Names and signatures should expose
the relevant semantics; diagnostics should lead to a correction; examples should reveal why a
design is useful in a real task. A caller should learn one concept and be able to reuse that
understanding elsewhere. Mechanical symmetry is less valuable than semantic consistency.

Compiler assistance, generated implementations, and executable properties can move repeated
reasoning out of every application. Their claims should match what they can establish. Agreement
between artifacts derived from the same mistaken description is not independent evidence of
business correctness. Boundary examples, laws where applicable, and a credible reference behavior
give humans and agents something concrete to reason against.

The design is successful when an agent can infer a good next move from a small body of coherent
ideas, and a maintainer can understand the result without recovering the agent's entire history.
Invest in that shared understanding. The goal is greater capability with less incidental
complexity for everyone who uses or changes the library.

## Let successive goals build one product

Each goal is an opportunity to improve the same design. Look at how a proposed capability meets
the existing vocabulary, error model, data shapes, and execution boundaries. A locally convenient
wrapper can impose conversion and learning costs on every neighboring feature. A carefully chosen
shared concept can make several later goals easier without requiring a speculative foundation.

Start from a convincing consumer experience and work back to the necessary abstraction. Explore
alternative models, simplify an earlier implementation, or make a substantial internal change
when that better serves the agreed outcome. Agent autonomy includes those design choices. The
goal's full completion conditions and the current shipped contracts remain intact; a deliberate
change to a product promise is made explicit through the accepted goal and affected contracts.

Design judgment concerns the total trade-off. Does the caller express the business more directly?
Which mistakes become impossible or easier to diagnose? Does composition preserve useful meaning?
What work and lifetime costs appear at realistic scale? What can now be reused without exporting
implementation complexity? These questions guide reasoning; they are not a required report format
or a new checklist attached to every goal.

Keep the long-term ambition while allowing evidence to improve the route. Research goals may
resolve uncertainty with an adopt, adapt, defer, or reject decision; product goals still deliver
their stated outcomes. Historical evidence explains earlier choices. Current contracts explain
current promises. This blueprint keeps the purpose legible as both evolve.

## Deliberate boundaries

FunnySharp's role is to put difficult, repeatable reasoning into precise, reusable abstractions.
Applications retain control of their data, effects, infrastructure, and policy. This division
leaves room for ambitious libraries, compilers, and tooling within the project's accepted scope
while preserving local adoption, ordinary .NET interoperability, and understandable execution.

A strong design earns its abstraction: the consumer gains a clearer program, a useful guarantee,
or a meaningful reduction in work, and the cost of that gain is visible.
