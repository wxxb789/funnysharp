module FunnySharp.Harness.Tests.DisposableReleaseDenialTests

open System
open Xunit

type DisposableReleaseDenialTests() =
    [<Fact>]
    member _.OrdinaryReleaseHasNoDenialContext() =
        let context = Environment.GetEnvironmentVariable "FUNNYSHARP_U15_DENIAL_CONTEXT"
        Assert.True(isNull context, sprintf "Deliberate U15 failed-test proof: %s" context)
