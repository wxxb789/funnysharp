using System.Globalization;
using Funcky.Extensions;
using Funcky.Monads;

namespace CallSites.Competitors;

// W1 competitor: Funcky 3.6.0 dictionary + parse bridges.
public static class W1Funcky
{
    public static int TimeoutSeconds(IReadOnlyDictionary<string, string> config) =>
        config.GetValueOrNone("request.timeoutSeconds")
            .SelectMany(raw => raw.ParseInt32OrNone(CultureInfo.InvariantCulture))
            .GetOrElse(30);
}
