#pragma warning disable FS0017

using FunnySharp;
using FunnySharp.TreeTransformations;

namespace FunnySharp.TestSupport.TreeComparison;

/// <summary>The direct-recursion consumer: recursive resolve, rebuild, and validation.</summary>
internal sealed class RecursiveConfigEditor : IConfigEditor
{
    private readonly ConfigNode _base;
    private int _visited;
    private int _rebuilt;
    private int _shared;
    private int _applied;
    private int _errors;

    internal RecursiveConfigEditor(ConfigNode baseTree) => _base = baseTree;

    public string Name => "Direct recursion";

    public ConfigEditStats Stats => new(_visited, _rebuilt, _shared, _applied, _errors);

    public void ResetStats()
    {
        _visited = 0;
        _rebuilt = 0;
        _shared = 0;
        _applied = 0;
        _errors = 0;
    }

    public Validation<ConfigNode, ConfigError> Apply(IReadOnlyList<ConfigOverride> overrides)
    {
        var errors = new List<ConfigError>();
        var seen = new HashSet<TreePath>();
        var tree = _base;

        foreach (var item in overrides)
        {
            if (!seen.Add(item.Path))
            {
                errors.Add(ConfigErrors.Duplicate(item));
                _errors++;
                continue;
            }

            var resolved = Resolve(tree, item, 0, errors);

            if (resolved is null)
            {
                continue;
            }

            if (resolved.IsSection)
            {
                errors.Add(ConfigErrors.NotAValue(item, item.Path.ToLocation()));
                _errors++;
                continue;
            }

            if (!ConfigKernel.IsValid(resolved.Name, item.Value))
            {
                errors.Add(ConfigErrors.InvalidValue(item, item.Path.ToLocation()));
                _errors++;
                continue;
            }

            if (resolved.Value == item.Value)
            {
                continue;
            }

            var edited = resolved with { Value = item.Value };
            _rebuilt++;
            tree = Rebuild(tree, item, 0, edited);
            _applied++;
        }

        ValidateTree(tree, new List<TreePathSegment>(), errors);

        return ConfigResults.From(tree, errors);
    }

    public Result<ConfigNode, ConfigError> ApplyFirst(IReadOnlyList<ConfigOverride> overrides)
    {
        var seen = new HashSet<TreePath>();
        var tree = _base;

        foreach (var item in overrides)
        {
            if (!seen.Add(item.Path))
            {
                _errors++;
                return Result<ConfigNode, ConfigError>.Failure(ConfigErrors.Duplicate(item));
            }

            var resolved = ResolveFirst(tree, item, 0, out var error);

            if (error is not null)
            {
                _errors++;
                return Result<ConfigNode, ConfigError>.Failure(error);
            }

            if (resolved!.IsSection)
            {
                _errors++;
                return Result<ConfigNode, ConfigError>.Failure(
                    ConfigErrors.NotAValue(item, item.Path.ToLocation()));
            }

            if (!ConfigKernel.IsValid(resolved.Name, item.Value))
            {
                _errors++;
                return Result<ConfigNode, ConfigError>.Failure(
                    ConfigErrors.InvalidValue(item, item.Path.ToLocation()));
            }

            if (resolved.Value == item.Value)
            {
                continue;
            }

            var edited = resolved with { Value = item.Value };
            _rebuilt++;
            tree = Rebuild(tree, item, 0, edited);
            _applied++;
        }

        var validationError = ValidateFirst(tree, new List<TreePathSegment>());

        if (validationError is not null)
        {
            _errors++;
            return Result<ConfigNode, ConfigError>.Failure(validationError);
        }

        return Result<ConfigNode, ConfigError>.Success(tree);
    }

    public Validation<ConfigNode, ConfigError> Validate()
    {
        var errors = new List<ConfigError>();
        ValidateTree(_base, new List<TreePathSegment>(), errors);

        return ConfigResults.From(_base, errors);
    }

    private ConfigNode? Resolve(
        ConfigNode node,
        ConfigOverride item,
        int index,
        List<ConfigError> errors)
    {
        _visited++;

        if (index == item.Path.Count)
        {
            return node;
        }

        var segment = item.Path[index];
        var childIndex = ConfigTreeAdapter.Instance.ChildIndex(node, segment);

        if (!childIndex.TryGetValue(out var position))
        {
            errors.Add(ConfigErrors.Path(item, segment, LocationAt(item.Path, index)));
            _errors++;
            return null;
        }

        return Resolve(node.Children[position], item, index + 1, errors);
    }

    private ConfigNode? ResolveFirst(
        ConfigNode node,
        ConfigOverride item,
        int index,
        out ConfigError? error)
    {
        _visited++;

        if (index == item.Path.Count)
        {
            error = null;
            return node;
        }

        var segment = item.Path[index];
        var childIndex = ConfigTreeAdapter.Instance.ChildIndex(node, segment);

        if (!childIndex.TryGetValue(out var position))
        {
            error = ConfigErrors.Path(item, segment, LocationAt(item.Path, index));
            return null;
        }

        return ResolveFirst(node.Children[position], item, index + 1, out error);
    }

    private ConfigNode Rebuild(ConfigNode node, ConfigOverride item, int index, ConfigNode edited)
    {
        if (index == item.Path.Count)
        {
            return edited;
        }

        var segment = item.Path[index];
        var childIndex = ConfigTreeAdapter.Instance.ChildIndex(node, segment);

        if (!childIndex.TryGetValue(out var position))
        {
            throw new InvalidOperationException("The resolved path no longer selects a child.");
        }

        var children = node.Children;
        var newChildren = new ConfigNode[children.Count];

        for (var copyIndex = 0; copyIndex < children.Count; copyIndex++)
        {
            newChildren[copyIndex] = children[copyIndex];
        }

        newChildren[position] = Rebuild(children[position], item, index + 1, edited);
        _rebuilt++;
        _shared += children.Count - 1;

        return node with { Children = newChildren };
    }

    private void ValidateTree(ConfigNode node, List<TreePathSegment> path, List<ConfigError> errors)
    {
        _visited++;

        if (node.IsSection)
        {
            for (var index = 0; index < node.Children.Count; index++)
            {
                path.Add(ConfigTreeAdapter.SegmentFor(node, index));
                ValidateTree(node.Children[index], path, errors);
                path.RemoveAt(path.Count - 1);
            }

            return;
        }

        if (!ConfigKernel.IsValid(node.Name, node.Value!))
        {
            errors.Add(ConfigErrors.InvalidBase(node.Name, node.Value!, LocationFromPath(path)));
            _errors++;
        }
    }

    private ConfigError? ValidateFirst(ConfigNode node, List<TreePathSegment> path)
    {
        _visited++;

        if (node.IsSection)
        {
            for (var index = 0; index < node.Children.Count; index++)
            {
                path.Add(ConfigTreeAdapter.SegmentFor(node, index));
                var error = ValidateFirst(node.Children[index], path);
                path.RemoveAt(path.Count - 1);

                if (error is not null)
                {
                    return error;
                }
            }

            return null;
        }

        return ConfigKernel.IsValid(node.Name, node.Value!)
            ? null
            : ConfigErrors.InvalidBase(node.Name, node.Value!, LocationFromPath(path));
    }

    private static Location LocationAt(TreePath path, int index) =>
        TreePath.FromSegments(path.Segments.Take(index + 1)).ToLocation();

    private static Location LocationFromPath(List<TreePathSegment> path) =>
        TreePath.FromSegments(path).ToLocation();
}
