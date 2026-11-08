using System.Globalization;
using System.Text;
using FunnySharp.TreeTransformations;

namespace FunnySharp.TestSupport.TreeComparison;

/// <summary>Deterministic configuration fixtures shared by the tests and the measurement harness.</summary>
internal static class ConfigFixtures
{
    internal const int ServiceCount = 64;
    internal const int EndpointsPerService = 8;
    internal const int BaseNodeCount = 3_074;
    internal const int BaseLeafCount = 2_368;
    internal const int ValidOverrideCount = 32;

    internal static ConfigNode CreateBase()
    {
        var services = new ConfigNode[ServiceCount];

        for (var service = 0; service < ServiceCount; service++)
        {
            var endpoints = new ConfigNode[EndpointsPerService];

            for (var endpoint = 0; endpoint < EndpointsPerService; endpoint++)
            {
                endpoints[endpoint] = ConfigNode.Section(
                    $"endpoint-{endpoint}",
                    ConfigNode.Leaf("host", $"svc{service}.ep{endpoint}.example.test"),
                    ConfigNode.Leaf(
                        "port",
                        (8_000 + (service * 10) + endpoint).ToString(CultureInfo.InvariantCulture)),
                    ConfigNode.Leaf("path", $"/v1/svc{service}/ep{endpoint}"),
                    ConfigNode.Leaf("tls", "true"));
            }

            services[service] = ConfigNode.Section(
                $"service-{service}",
                ConfigNode.Leaf("name", $"service-{service}"),
                ConfigNode.Leaf(
                    "mode",
                    service % 3 == 0 ? "fast" : service % 3 == 1 ? "balanced" : "safe"),
                ConfigNode.IndexedSection("endpoints", endpoints),
                ConfigNode.Section(
                    "limits",
                    ConfigNode.Leaf(
                        "timeout",
                        (10 + (service % 20)).ToString(CultureInfo.InvariantCulture)),
                    ConfigNode.Leaf(
                        "retries",
                        (service % 4).ToString(CultureInfo.InvariantCulture)),
                    ConfigNode.Leaf(
                        "maxBody",
                        (1_024 * (1 + (service % 8))).ToString(CultureInfo.InvariantCulture))));
        }

        return ConfigNode.Section("config", ConfigNode.IndexedSection("services", services));
    }

    internal static IReadOnlyList<ConfigOverride> CreateValidOverrides()
    {
        var overrides = new List<ConfigOverride>(ValidOverrideCount);

        for (var index = 0; index < 24; index++)
        {
            overrides.Add(
                Override(
                    $"services[{index}].endpoints[{index % EndpointsPerService}].port",
                    (9_000 + index).ToString(CultureInfo.InvariantCulture)));
        }

        for (var index = 0; index < 4; index++)
        {
            overrides.Add(
                Override(
                    $"services[{index * 7}].limits.timeout",
                    (100 + index).ToString(CultureInfo.InvariantCulture)));
        }

        overrides.Add(Override("services[0].mode", "safe"));
        overrides.Add(Override("services[11].mode", "fast"));
        overrides.Add(Override("services[5].endpoints[3].host", "api.example.test"));
        overrides.Add(Override("services[9].endpoints[6].path", "/v2/orders"));

        return overrides;
    }

    internal static IReadOnlyList<ConfigOverride> CreateMixedOverrides()
    {
        var overrides = new List<ConfigOverride>(CreateValidOverrides())
        {
            Override("services[64].endpoints[0].port", "9000"),
            Override("services[0].endpoints", "unused"),
            Override("services[1].endpoints[0].port", "70000"),
            Override("services[2].limits.timeout", "200"),
            Override("services[2].limits.timeout", "201"),
        };

        return overrides;
    }

    internal static ConfigNode CreateSmall() =>
        ConfigNode.Section(
            "root",
            ConfigNode.Leaf("a", "1"),
            ConfigNode.IndexedSection(
                "items",
                ConfigNode.Section("first", ConfigNode.Leaf("x", "2")),
                ConfigNode.Section("second", ConfigNode.Leaf("y", "3"))),
            ConfigNode.Leaf("z", "4"));

    internal static ConfigNode CreateInvalidBase() =>
        ConfigNode.Section(
            "config",
            ConfigNode.IndexedSection(
                "services",
                ConfigNode.Section(
                    "service-0",
                    ConfigNode.Leaf("name", "service-0"),
                    ConfigNode.Leaf("mode", "fast"),
                    ConfigNode.IndexedSection(
                        "endpoints",
                        ConfigNode.Section(
                            "endpoint-0",
                            ConfigNode.Leaf("host", "h"),
                            ConfigNode.Leaf("port", "0"),
                            ConfigNode.Leaf("path", "/"),
                            ConfigNode.Leaf("tls", "true"))),
                    ConfigNode.Section(
                        "limits",
                        ConfigNode.Leaf("timeout", "10"),
                        ConfigNode.Leaf("retries", "1"),
                        ConfigNode.Leaf("maxBody", "1024")))));

    internal static ConfigNode CreateDeep(int sectionCount)
    {
        var node = ConfigNode.Leaf("value", "1");

        for (var index = sectionCount - 1; index >= 0; index--)
        {
            node = ConfigNode.Section($"level{index}", node);
        }

        return node;
    }

    internal static string CreateDeepPathText(int sectionCount)
    {
        var builder = new StringBuilder();

        for (var index = 1; index < sectionCount; index++)
        {
            if (builder.Length > 0)
            {
                builder.Append('.');
            }

            builder.Append("level").Append(index);
        }

        if (builder.Length > 0)
        {
            builder.Append('.');
        }

        builder.Append("value");

        return builder.ToString();
    }

    internal static ConfigOverride CreateDeepOverride(int sectionCount)
    {
        if (sectionCount == 0)
        {
            return new ConfigOverride("<root>", TreePath.Root, "2");
        }

        var text = CreateDeepPathText(sectionCount);

        return new ConfigOverride(text, TreePath.Parse(text), "2");
    }

    internal static ConfigNode CreateDag()
    {
        var shared = ConfigNode.Section(
            "shared",
            ConfigNode.Leaf("value", "1"),
            ConfigNode.Leaf("other", "x"));

        return ConfigNode.Section("root", ConfigNode.IndexedSection("items", shared, shared));
    }

    internal static ConfigOverride CreateDagOverride() =>
        Override("items[0].value", "2");

    internal static ConfigNode CreateEqualDistinctDag()
    {
        var first = ConfigNode.Section("shared", ConfigNode.Leaf("value", "1"));
        var second = ConfigNode.Section("shared", ConfigNode.Leaf("value", "1"));

        return ConfigNode.Section("root", ConfigNode.IndexedSection("items", first, second));
    }

    internal static (ConfigNode Root, ConfigNode Leaf) CreateCycle()
    {
        var children = new List<ConfigNode>();
        var root = new ConfigNode("root", null, children, Indexed: false);
        var leaf = ConfigNode.Leaf("value", "1");
        children.Add(leaf);
        children.Add(root);

        return (root, leaf);
    }

    internal static ConfigOverride CreateCycleOverride() => Override("value", "2");

    internal static (ConfigNode Root, ConfigNode Shared, List<ConfigNode> MutableChildren) CreateMutableShared()
    {
        var mutableChildren = new List<ConfigNode> { ConfigNode.Leaf("value", "1") };
        var shared = new ConfigNode("shared", null, mutableChildren, Indexed: false);
        var root = ConfigNode.Section("root", ConfigNode.IndexedSection("items", shared, shared));

        return (root, shared, mutableChildren);
    }

    private static ConfigOverride Override(string text, string value) =>
        new(text, TreePath.Parse(text), value);
}
