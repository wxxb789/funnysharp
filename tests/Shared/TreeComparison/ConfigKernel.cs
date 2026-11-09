using System.Globalization;
using System.Text;

namespace FunnySharp.TestSupport.TreeComparison;

/// <summary>The shared configuration rules, digest, and canonical JSON form.</summary>
internal static class ConfigKernel
{
    internal static bool IsValid(string name, string value) => name switch
    {
        "port" => IsInteger(value, 1, 65_535),
        "timeout" => IsInteger(value, 1, 3_600),
        "retries" => IsInteger(value, 0, 10),
        "mode" => value is "fast" or "balanced" or "safe",
        "host" => IsNonEmptyWithoutWhitespace(value),
        "path" => value.Length > 0 && value[0] == '/',
        "tls" => value is "true" or "false",
        "name" => value.Length > 0,
        "maxBody" => IsInteger(value, 1, int.MaxValue),
        _ => value.Length > 0,
    };

    internal static ulong Digest(ConfigNode root)
    {
        const ulong offset = 14_695_981_039_346_656_037UL;
        const ulong prime = 1_099_511_628_211UL;
        var hash = offset;
        var stack = new Stack<ConfigNode>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var node = stack.Pop();
            AddString(ref hash, node.Name);
            AddString(ref hash, node.Value ?? "\0");
            Add(ref hash, node.Indexed ? 1UL : 0UL);
            Add(ref hash, (ulong)node.Children.Count);

            for (var index = node.Children.Count - 1; index >= 0; index--)
            {
                stack.Push(node.Children[index]);
            }
        }

        return hash;

        static void Add(ref ulong current, ulong value)
        {
            current ^= value;
            current *= prime;
        }

        static void AddString(ref ulong current, string value)
        {
            foreach (var character in value)
            {
                current ^= character;
                current *= prime;
            }

            current ^= 0xFFFF;
            current *= prime;
        }
    }

    internal static string ToCanonicalJson(ConfigNode root)
    {
        var builder = new StringBuilder();
        var stack = new Stack<(ConfigNode Node, int ChildIndex)>();
        AppendOpen(builder, root);
        stack.Push((root, 0));

        while (stack.Count > 0)
        {
            var (node, childIndex) = stack.Pop();

            if (childIndex < node.Children.Count)
            {
                if (childIndex > 0)
                {
                    builder.Append(',');
                }

                var child = node.Children[childIndex];
                stack.Push((node, childIndex + 1));
                AppendOpen(builder, child);
                stack.Push((child, 0));
            }
            else
            {
                builder.Append("]}");
            }
        }

        return builder.ToString();
    }

    private static bool IsInteger(string value, int minimum, int maximum) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
        && parsed >= minimum
        && parsed <= maximum;

    private static bool IsNonEmptyWithoutWhitespace(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        foreach (var character in value)
        {
            if (char.IsWhiteSpace(character))
            {
                return false;
            }
        }

        return true;
    }

    private static void AppendOpen(StringBuilder builder, ConfigNode node)
    {
        builder.Append("{\"name\":");
        AppendString(builder, node.Name);
        builder.Append(",\"value\":");

        if (node.Value is null)
        {
            builder.Append("null");
        }
        else
        {
            AppendString(builder, node.Value);
        }

        builder.Append(",\"indexed\":").Append(node.Indexed ? "true" : "false");
        builder.Append(",\"children\":[");
    }

    private static void AppendString(StringBuilder builder, string value)
    {
        builder.Append('"');

        foreach (var character in value)
        {
            switch (character)
            {
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    if (character < ' ')
                    {
                        builder.Append("\\u")
                            .Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        builder.Append(character);
                    }

                    break;
            }
        }

        builder.Append('"');
    }
}
