module FunnySharp.Harness.Tests.ConcurrencyOracleTests

open System
open System.IO
open System.Text.RegularExpressions
open Xunit
open FunnySharp.Harness.Proc
open FunnySharp.Harness.Tests.Support

type ConcurrencyOracleTests() =
    [<Theory>]
    [<InlineData(2, false)>]
    [<InlineData(Int32.MaxValue, false)>]
    [<InlineData(2, true)>]
    [<InlineData(Int32.MaxValue, true)>]
    member _.LegalBoundedCoordinatorPassesCurrentOracle(maxWorkers: int, batches: bool) = task {
        use temp = new TempDirectory()
        let root = repositoryRoot ()
        let kit = Path.Combine(temp.Path, "oracle kit")
        Directory.CreateDirectory kit |> ignore
        File.Copy(Path.Combine(root, "global.json"), Path.Combine(kit, "global.json"))
        File.Copy(
            Path.Combine(root, "eng", "evaluation", "tasks", "concurrency", "template-idiomatic", "ConcurrencyKit.csproj"),
            Path.Combine(kit, "ConcurrencyKit.csproj")
        )
        for name in [ "Contract.cs"; "ConcurrencyTests.cs" ] do
            File.Copy(Path.Combine(root, "eng", "evaluation", "tasks", "concurrency", "tests", name), Path.Combine(kit, name))
        let solution = """
public static class AvailabilityCoordinator
{
    public static async Task<IReadOnlyList<ItemAvailability>> CheckAvailabilityAsync(
        IReadOnlyList<StockItem> items, WarehouseGateway gateway, int maxConcurrency, CancellationToken token)
    {
        var capacity = Math.Min(maxConcurrency, MAX_WORKERS);
        if (bool.Parse("USE_BATCHES"))
        {
            var results = new List<ItemAvailability>();
            foreach (var batch in items.Chunk(capacity))
                results.AddRange(await Task.WhenAll(batch.Select(CheckAsync)).WaitAsync(token).ConfigureAwait(false));
            return results;
        }

        var gate = new SemaphoreSlim(capacity);
        var pending = Task.WhenAll(items.Select(async item =>
        {
            await gate.WaitAsync(token).ConfigureAwait(false);
            try
            {
                return await CheckAsync(item).ConfigureAwait(false);
            }
            finally
            {
                gate.Release();
            }
        }));
        _ = pending.ContinueWith(_ => gate.Dispose(), CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        return await pending.WaitAsync(token).ConfigureAwait(false);

        async Task<ItemAvailability> CheckAsync(StockItem item)
        {
            var reply = await gateway.CheckAsync(item.Sku, token).ConfigureAwait(false);
            return new ItemAvailability(item.Sku, reply.OnHand, reply.OnHand >= item.Quantity);
        }
    }

    public static async Task<ReservationOutcome> ReserveFirstAsync(
        IReadOnlyList<string> suppliers, SupplierGateway gateway, CancellationToken token)
    {
        var winner = new TaskCompletionSource<(string Supplier, SupplierReply Reply)>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = Task.WhenAll(suppliers.Select(async supplier =>
        {
            var reply = await gateway.ProbeAsync(supplier, token).ConfigureAwait(false);
            if (reply.Accepts) winner.TrySetResult((supplier, reply));
        }));
        await pending.WaitAsync(token).ConfigureAwait(false);
        if (!winner.Task.IsCompletedSuccessfully)
            return new ReservationOutcome(false, null, null, suppliers);
        var accepted = await winner.Task.ConfigureAwait(false);
        return new ReservationOutcome(true, accepted.Supplier, accepted.Reply.ReservationId, []);
    }
}
"""
        File.WriteAllText(
            Path.Combine(kit, "AvailabilityCoordinator.cs"),
            solution.Replace("MAX_WORKERS", string maxWorkers).Replace("USE_BATCHES", string batches)
        )
        let! restore = runCaptureIn (Some kit) "dotnet" [ "restore"; "--use-lock-file" ] |> Async.StartAsTask
        Assert.True(restore.ExitCode = 0, restore.Stdout + restore.Stderr)
        let! result =
            runCaptureIn (Some kit) "dotnet"
                [ "run"; "--no-restore"; "--configuration"; "Release"; "--verbosity"; "quiet"
                  "--property:RestoreLockedMode=true" ]
            |> Async.StartAsTask
        Assert.True(result.ExitCode = 0, result.Stdout + result.Stderr)
        let summary = Regex.Match(result.Stdout, @"Total:\s*(\d+), Errors:\s*(\d+), Failed:\s*(\d+), Skipped:\s*(\d+), Not Run:\s*(\d+)")
        Assert.True(summary.Success, result.Stdout)
        Assert.Equal<string list>([ "9"; "0"; "0"; "0"; "0" ], [ for index in 1 .. 5 -> summary.Groups.[index].Value ])
    }
