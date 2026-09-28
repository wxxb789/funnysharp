using System.Net;
using FunnySharp.VerticalSlice.Application;
using FunnySharp.VerticalSlice.Domain;
using FunnySharp.VerticalSlice.Http;
using Microsoft.Extensions.DependencyInjection;

namespace FunnySharp.VerticalSlice.Tests;

/// <summary>
/// Replay is only a clean check if it compares everything the history produced. A stored projection whose
/// payment reference does not follow from its events must be reported as diverging.
/// </summary>
public sealed class TimelineIntegrityTests
{
    [Fact]
    public async Task AStoredProjectionThatDivergesFromItsHistoryIsReportedAsNotMatching()
    {
        GateOrderStore? store = null;
        await using var host = await SliceHost.StartAsync(
            services => services.AddSingleton<IOrderStore>(store!),
            options => store = new GateOrderStore(options));
        var draft = Order.Draft(
            OrderId.New(),
            Refined.CustomerOf("customer-42"),
            Refined.LinesOf(9.95m, ("SKU-BOOK", 2)),
            Refined.Now);
        var corrupted = draft with
        {
            Status = OrderStatus.Placed,
            Revision = 1,
            PaymentReference = "PAY-NOT-IN-HISTORY",
        };
        store!.OnFind = (_, _) => ValueTask.FromResult(Option.Some(
            new OrderRecord(corrupted, [new OrderEvent.Place(Refined.Now)])));

        using var response = await host.Client.GetAsync($"/orders/{corrupted.Id.Value}/timeline");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var timeline = await response.ReadJsonAsync<TimelineResponse>();
        Assert.NotNull(timeline);
        Assert.False(timeline.MatchesStoredProjection);
    }
}
