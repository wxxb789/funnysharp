#pragma warning disable FS0017

using FunnySharp;
using FunnySharp.TreeTransformations;

namespace FunnySharp.TestSupport.TreeComparison;

/// <summary>The generic path-rewrite consumer: adapter, path edit per override, traversal validation.</summary>
internal sealed class GenericPathConfigEditor : IConfigEditor
{
    private readonly ConfigNode _base;
    private int _visited;
    private int _rebuilt;
    private int _shared;
    private int _applied;
    private int _errors;

    internal GenericPathConfigEditor(ConfigNode baseTree) => _base = baseTree;

    public string Name => "Generic path rewrite";

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

            var result = TreeRewrite.RewriteAt<ConfigNode, ConfigError>(
                tree,
                ConfigTreeAdapter.Instance,
                item.Path,
                resolved => Edit(resolved, item),
                (kind, location) => ConfigErrors.Path(item, kind, location));

            if (result.TryGetError(out var error))
            {
                _visited += error.Kind is ConfigErrorKind.PathNotFound or ConfigErrorKind.IndexOutOfRange
                    ? ConfigResolution.Visited(item.Path, error.Location)
                    : ConfigResolution.Visited(item.Path);
                errors.Add(error);
                _errors++;
                continue;
            }

            if (!result.TryGetValue(out var rewrite))
            {
                throw new InvalidOperationException("RewriteAt returned neither a result nor an error.");
            }

            _visited += rewrite.VisitedNodes;
            _rebuilt += rewrite.RebuiltNodes;
            _shared += rewrite.SharedNodes;

            if (rewrite.Changed)
            {
                _applied++;
                tree = rewrite.Root;
            }
        }

        ValidateTree(tree, errors);

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

            var result = TreeRewrite.RewriteAt<ConfigNode, ConfigError>(
                tree,
                ConfigTreeAdapter.Instance,
                item.Path,
                resolved => Edit(resolved, item),
                (kind, location) => ConfigErrors.Path(item, kind, location));

            if (result.TryGetError(out var error))
            {
                _visited += error.Kind is ConfigErrorKind.PathNotFound or ConfigErrorKind.IndexOutOfRange
                    ? ConfigResolution.Visited(item.Path, error.Location)
                    : ConfigResolution.Visited(item.Path);
                _errors++;
                return Result<ConfigNode, ConfigError>.Failure(error);
            }

            if (!result.TryGetValue(out var rewrite))
            {
                throw new InvalidOperationException("RewriteAt returned neither a result nor an error.");
            }

            _visited += rewrite.VisitedNodes;
            _rebuilt += rewrite.RebuiltNodes;
            _shared += rewrite.SharedNodes;

            if (rewrite.Changed)
            {
                _applied++;
                tree = rewrite.Root;
            }
        }

        ConfigError? firstError = null;

        var traversal = TreeTraversal.Traverse(
            tree,
            ConfigTreeAdapter.Instance,
            (node, depth, path) =>
            {
                if (node.IsSection || ConfigKernel.IsValid(node.Name, node.Value!))
                {
                    return TreeVisitDecision.Continue;
                }

                firstError = ConfigErrors.InvalidBase(node.Name, node.Value!, path.ToLocation());
                return TreeVisitDecision.Stop;
            });

        _visited += traversal.VisitedNodes;

        if (firstError is not null)
        {
            _errors++;
            return Result<ConfigNode, ConfigError>.Failure(firstError);
        }

        return Result<ConfigNode, ConfigError>.Success(tree);
    }

    public Validation<ConfigNode, ConfigError> Validate()
    {
        var errors = new List<ConfigError>();
        ValidateTree(_base, errors);

        return ConfigResults.From(_base, errors);
    }

    private static Result<ConfigNode, ConfigError> Edit(
        TreeResolvedNode<ConfigNode> resolved,
        ConfigOverride item)
    {
        if (resolved.Node.IsSection)
        {
            return Result<ConfigNode, ConfigError>.Failure(
                ConfigErrors.NotAValue(item, resolved.Location));
        }

        if (!ConfigKernel.IsValid(resolved.Node.Name, item.Value))
        {
            return Result<ConfigNode, ConfigError>.Failure(
                ConfigErrors.InvalidValue(item, resolved.Location));
        }

        return resolved.Node.Value == item.Value
            ? Result<ConfigNode, ConfigError>.Success(resolved.Node)
            : Result<ConfigNode, ConfigError>.Success(resolved.Node with { Value = item.Value });
    }

    private void ValidateTree(ConfigNode tree, List<ConfigError> errors)
    {
        var traversal = TreeTraversal.Traverse(
            tree,
            ConfigTreeAdapter.Instance,
            (node, depth, path) =>
            {
                if (!node.IsSection && !ConfigKernel.IsValid(node.Name, node.Value!))
                {
                    errors.Add(ConfigErrors.InvalidBase(node.Name, node.Value!, path.ToLocation()));
                    _errors++;
                }

                return TreeVisitDecision.Continue;
            });

        _visited += traversal.VisitedNodes;
    }
}
