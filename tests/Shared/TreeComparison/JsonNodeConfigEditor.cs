#pragma warning disable FS0017

using System.Text.Json;
using System.Text.Json.Nodes;
using FunnySharp;
using FunnySharp.TreeTransformations;

namespace FunnySharp.TestSupport.TreeComparison;

/// <summary>
/// The existing .NET tree-API consumer: parse the uniform JSON form into a mutable
/// <see cref="JsonNode"/>, resolve and edit paths, validate JSON leaves, and convert back to the
/// typed tree. The fresh parse and the typed conversion are part of the alternative's cost.
/// </summary>
internal sealed class JsonNodeConfigEditor : IConfigEditor
{
    private readonly ConfigNode _baseTree;
    private readonly string _baseJson;
    private int _visited;
    private int _rebuilt;
    private int _applied;
    private int _errors;

    internal JsonNodeConfigEditor(ConfigNode baseTree)
    {
        _baseTree = baseTree;
        _baseJson = ConfigKernel.ToCanonicalJson(baseTree);
    }

    public string Name => "JsonNode";

    public ConfigEditStats Stats => new(_visited, _rebuilt, 0, _applied, _errors);

    public void ResetStats()
    {
        _visited = 0;
        _rebuilt = 0;
        _applied = 0;
        _errors = 0;
    }

    public Validation<ConfigNode, ConfigError> Apply(IReadOnlyList<ConfigOverride> overrides)
    {
        var root = JsonNode.Parse(_baseJson)!;
        var errors = new List<ConfigError>();
        var seen = new HashSet<TreePath>();

        foreach (var item in overrides)
        {
            if (!seen.Add(item.Path))
            {
                errors.Add(ConfigErrors.Duplicate(item));
                _errors++;
                continue;
            }

            if (!TryResolve(root, item, errors, out var target, out var location))
            {
                continue;
            }

            var targetObject = target!.AsObject();
            var valueNode = targetObject["value"];

            if (valueNode is null || valueNode.GetValueKind() == JsonValueKind.Null)
            {
                errors.Add(ConfigErrors.NotAValue(item, location));
                _errors++;
                continue;
            }

            var name = targetObject["name"]!.GetValue<string>();

            if (!ConfigKernel.IsValid(name, item.Value))
            {
                errors.Add(ConfigErrors.InvalidValue(item, location));
                _errors++;
                continue;
            }

            if (valueNode.GetValue<string>() == item.Value)
            {
                continue;
            }

            targetObject["value"] = JsonValue.Create(item.Value);
            _applied++;
        }

        ValidateJson(root, errors);
        var tree = FromJson(root);

        return ConfigResults.From(tree, errors);
    }

    public Result<ConfigNode, ConfigError> ApplyFirst(IReadOnlyList<ConfigOverride> overrides)
    {
        var root = JsonNode.Parse(_baseJson)!;
        var seen = new HashSet<TreePath>();

        foreach (var item in overrides)
        {
            if (!seen.Add(item.Path))
            {
                _errors++;
                return Result<ConfigNode, ConfigError>.Failure(ConfigErrors.Duplicate(item));
            }

            var error = ResolveFirst(root, item, out var target, out var location);

            if (error is not null)
            {
                _errors++;
                return Result<ConfigNode, ConfigError>.Failure(error);
            }

            var targetObject = target!.AsObject();
            var valueNode = targetObject["value"];

            if (valueNode is null || valueNode.GetValueKind() == JsonValueKind.Null)
            {
                _errors++;
                return Result<ConfigNode, ConfigError>.Failure(ConfigErrors.NotAValue(item, location));
            }

            var name = targetObject["name"]!.GetValue<string>();

            if (!ConfigKernel.IsValid(name, item.Value))
            {
                _errors++;
                return Result<ConfigNode, ConfigError>.Failure(ConfigErrors.InvalidValue(item, location));
            }

            if (valueNode.GetValue<string>() == item.Value)
            {
                continue;
            }

            targetObject["value"] = JsonValue.Create(item.Value);
            _applied++;
        }

        var validationError = ValidateJsonFirst(root);

        if (validationError is not null)
        {
            _errors++;
            return Result<ConfigNode, ConfigError>.Failure(validationError);
        }

        return Result<ConfigNode, ConfigError>.Success(FromJson(root));
    }

    public Validation<ConfigNode, ConfigError> Validate()
    {
        var root = JsonNode.Parse(_baseJson)!;
        var errors = new List<ConfigError>();
        ValidateJson(root, errors);

        return errors.Count == 0
            ? Validation<ConfigNode, ConfigError>.Valid(_baseTree)
            : Validation<ConfigNode, ConfigError>.InvalidMany(errors);
    }

    private bool TryResolve(
        JsonNode root,
        ConfigOverride item,
        List<ConfigError> errors,
        out JsonNode? target,
        out Location location)
    {
        target = root;
        location = Location.Root;

        for (var index = 0; index < item.Path.Count; index++)
        {
            _visited++;
            var segment = item.Path[index];
            var children = target!.AsObject()["children"]!.AsArray();
            JsonNode? next = null;

            if (segment.Kind == TreePathSegmentKind.Index)
            {
                if (segment.Index < children.Count)
                {
                    next = children[segment.Index];
                }
            }
            else
            {
                foreach (var child in children)
                {
                    if (string.Equals(
                            child!.AsObject()["name"]!.GetValue<string>(),
                            segment.Name,
                            StringComparison.Ordinal))
                    {
                        next = child;
                        break;
                    }
                }
            }

            if (next is null)
            {
                target = null;
                errors.Add(ConfigErrors.Path(item, segment, segment.AppendTo(location)));
                _errors++;
                return false;
            }

            location = segment.AppendTo(location);
            target = next;
        }

        _visited++;
        return true;
    }

    private ConfigError? ResolveFirst(
        JsonNode root,
        ConfigOverride item,
        out JsonNode? target,
        out Location location)
    {
        target = root;
        location = Location.Root;

        for (var index = 0; index < item.Path.Count; index++)
        {
            _visited++;
            var segment = item.Path[index];
            var children = target!.AsObject()["children"]!.AsArray();
            JsonNode? next = null;

            if (segment.Kind == TreePathSegmentKind.Index)
            {
                if (segment.Index < children.Count)
                {
                    next = children[segment.Index];
                }
            }
            else
            {
                foreach (var child in children)
                {
                    if (string.Equals(
                            child!.AsObject()["name"]!.GetValue<string>(),
                            segment.Name,
                            StringComparison.Ordinal))
                    {
                        next = child;
                        break;
                    }
                }
            }

            if (next is null)
            {
                target = null;
                return ConfigErrors.Path(item, segment, segment.AppendTo(location));
            }

            location = segment.AppendTo(location);
            target = next;
        }

        _visited++;
        return null;
    }

    private void ValidateJson(JsonNode root, List<ConfigError> errors)
    {
        var stack = new Stack<(JsonNode Node, Location Location)>();
        stack.Push((root, Location.Root));

        while (stack.Count > 0)
        {
            var (node, location) = stack.Pop();
            _visited++;
            var jsonObject = node.AsObject();
            var valueNode = jsonObject["value"];

            if (valueNode is not null && valueNode.GetValueKind() != JsonValueKind.Null)
            {
                var name = jsonObject["name"]!.GetValue<string>();
                var value = valueNode.GetValue<string>();

                if (!ConfigKernel.IsValid(name, value))
                {
                    errors.Add(ConfigErrors.InvalidBase(name, value, location));
                    _errors++;
                }

                continue;
            }

            var children = jsonObject["children"]!.AsArray();
            var indexed = jsonObject["indexed"]!.GetValue<bool>();

            for (var index = children.Count - 1; index >= 0; index--)
            {
                var child = children[index]!;
                var segment = indexed
                    ? TreePathSegment.At(index)
                    : TreePathSegment.Named(child.AsObject()["name"]!.GetValue<string>());
                stack.Push((child, segment.AppendTo(location)));
            }
        }
    }

    private ConfigError? ValidateJsonFirst(JsonNode root)
    {
        var stack = new Stack<(JsonNode Node, Location Location)>();
        stack.Push((root, Location.Root));

        while (stack.Count > 0)
        {
            var (node, location) = stack.Pop();
            _visited++;
            var jsonObject = node.AsObject();
            var valueNode = jsonObject["value"];

            if (valueNode is not null && valueNode.GetValueKind() != JsonValueKind.Null)
            {
                var name = jsonObject["name"]!.GetValue<string>();
                var value = valueNode.GetValue<string>();

                if (!ConfigKernel.IsValid(name, value))
                {
                    return ConfigErrors.InvalidBase(name, value, location);
                }

                continue;
            }

            var children = jsonObject["children"]!.AsArray();
            var indexed = jsonObject["indexed"]!.GetValue<bool>();

            for (var index = children.Count - 1; index >= 0; index--)
            {
                var child = children[index]!;
                var segment = indexed
                    ? TreePathSegment.At(index)
                    : TreePathSegment.Named(child.AsObject()["name"]!.GetValue<string>());
                stack.Push((child, segment.AppendTo(location)));
            }
        }

        return null;
    }

    private ConfigNode FromJson(JsonNode root)
    {
        var stack = new List<ConversionFrame> { new(root) };
        ConfigNode? completed = null;
        ConfigNode? result = null;

        while (stack.Count > 0)
        {
            var frame = stack[^1];
            var jsonObject = frame.Node.AsObject();

            if (!frame.Initialized)
            {
                frame.Name = jsonObject["name"]!.GetValue<string>();
                var valueNode = jsonObject["value"];
                frame.Value = valueNode is not null && valueNode.GetValueKind() != JsonValueKind.Null
                    ? valueNode.GetValue<string>()
                    : null;
                frame.Indexed = jsonObject["indexed"]!.GetValue<bool>();
                frame.Children = new ConfigNode[jsonObject["children"]!.AsArray().Count];
                frame.Initialized = true;
                stack[^1] = frame;
            }

            if (frame.NextChild < frame.Children.Length)
            {
                var childIndex = frame.NextChild;
                var child = jsonObject["children"]!.AsArray()[childIndex]!;
                frame.NextChild = childIndex + 1;
                stack[^1] = frame;
                stack.Add(new ConversionFrame(child));
                continue;
            }

            completed = frame.Value is null
                ? new ConfigNode(frame.Name!, null, frame.Children, frame.Indexed)
                : ConfigNode.Leaf(frame.Name!, frame.Value);
            _rebuilt++;
            stack.RemoveAt(stack.Count - 1);

            if (stack.Count == 0)
            {
                result = completed;
                break;
            }

            var parent = stack[^1];
            parent.Children[parent.NextChild - 1] = completed;
            stack[^1] = parent;
        }

        return result!;
    }

    private struct ConversionFrame
    {
        public ConversionFrame(JsonNode node)
        {
            Node = node;
            Name = null;
            Value = null;
            Children = [];
            Indexed = false;
            NextChild = 0;
            Initialized = false;
        }

        public JsonNode Node { get; }

        public string? Name { get; set; }

        public string? Value { get; set; }

        public ConfigNode[] Children { get; set; }

        public bool Indexed { get; set; }

        public int NextChild { get; set; }

        public bool Initialized { get; set; }
    }
}
