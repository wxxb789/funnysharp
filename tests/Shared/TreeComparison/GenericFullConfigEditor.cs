#pragma warning disable FS0017

using FunnySharp;
using FunnySharp.TreeTransformations;

namespace FunnySharp.TestSupport.TreeComparison;

/// <summary>The generic full-rewrite consumer: adapter, pre-validation, bottom-up rewrite, traversal.</summary>
internal sealed class GenericFullConfigEditor : IConfigEditor
{
    private readonly ConfigNode _base;
    private int _visited;
    private int _rebuilt;
    private int _shared;
    private int _applied;
    private int _errors;

    internal GenericFullConfigEditor(ConfigNode baseTree) => _base = baseTree;

    public string Name => "Generic full rewrite";

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
        var valid = new Dictionary<TreePath, ConfigOverride>();
        var seen = new HashSet<TreePath>();

        foreach (var item in overrides)
        {
            if (!seen.Add(item.Path))
            {
                errors.Add(ConfigErrors.Duplicate(item));
                _errors++;
                continue;
            }

            var resolved = TreeRewrite.Resolve(_base, ConfigTreeAdapter.Instance, item.Path);

            if (resolved.TryGetError(out var editError))
            {
                _visited += ConfigResolution.Visited(item.Path, editError.Location);
                errors.Add(ConfigErrors.Path(item, editError.Kind, editError.Location));
                _errors++;
                continue;
            }

            if (!resolved.TryGetValue(out var resolvedNode))
            {
                throw new InvalidOperationException("Resolve returned neither a node nor an error.");
            }

            _visited += ConfigResolution.Visited(item.Path);

            if (resolvedNode.Node.IsSection)
            {
                errors.Add(ConfigErrors.NotAValue(item, resolvedNode.Location));
                _errors++;
                continue;
            }

            if (!ConfigKernel.IsValid(resolvedNode.Node.Name, item.Value))
            {
                errors.Add(ConfigErrors.InvalidValue(item, resolvedNode.Location));
                _errors++;
                continue;
            }

            valid[item.Path] = item;
        }

        var rewrite = TreeRewrite.RewriteBottomUp(
            _base,
            ConfigTreeAdapter.Instance,
            (node, originalChildren, rewrittenChildren, path) =>
                RewriteNode(node, originalChildren, rewrittenChildren, path, valid));

        _visited += rewrite.VisitedNodes;
        var tree = rewrite.Root;
        ValidateTree(tree, errors);

        return ConfigResults.From(tree, errors);
    }

    public Result<ConfigNode, ConfigError> ApplyFirst(IReadOnlyList<ConfigOverride> overrides)
    {
        var valid = new Dictionary<TreePath, ConfigOverride>();
        var seen = new HashSet<TreePath>();

        foreach (var item in overrides)
        {
            if (!seen.Add(item.Path))
            {
                _errors++;
                return Result<ConfigNode, ConfigError>.Failure(ConfigErrors.Duplicate(item));
            }

            var resolved = TreeRewrite.Resolve(_base, ConfigTreeAdapter.Instance, item.Path);

            if (resolved.TryGetError(out var editError))
            {
                _visited += ConfigResolution.Visited(item.Path, editError.Location);
                _errors++;
                return Result<ConfigNode, ConfigError>.Failure(
                    ConfigErrors.Path(item, editError.Kind, editError.Location));
            }

            if (!resolved.TryGetValue(out var resolvedNode))
            {
                throw new InvalidOperationException("Resolve returned neither a node nor an error.");
            }

            _visited += ConfigResolution.Visited(item.Path);

            if (resolvedNode.Node.IsSection)
            {
                _errors++;
                return Result<ConfigNode, ConfigError>.Failure(
                    ConfigErrors.NotAValue(item, resolvedNode.Location));
            }

            if (!ConfigKernel.IsValid(resolvedNode.Node.Name, item.Value))
            {
                _errors++;
                return Result<ConfigNode, ConfigError>.Failure(
                    ConfigErrors.InvalidValue(item, resolvedNode.Location));
            }

            valid[item.Path] = item;
        }

        var rewrite = TreeRewrite.RewriteBottomUp(
            _base,
            ConfigTreeAdapter.Instance,
            (node, originalChildren, rewrittenChildren, path) =>
                RewriteNode(node, originalChildren, rewrittenChildren, path, valid));

        _visited += rewrite.VisitedNodes;
        var tree = rewrite.Root;
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

    private ConfigNode RewriteNode(
        ConfigNode node,
        IReadOnlyList<ConfigNode> originalChildren,
        IReadOnlyList<ConfigNode> rewrittenChildren,
        TreePathView path,
        Dictionary<TreePath, ConfigOverride> valid)
    {
        if (valid.TryGetValue(path.ToPath(), out var item))
        {
            if (node.Value == item.Value)
            {
                return node;
            }

            _rebuilt++;
            _applied++;
            return node with { Value = item.Value };
        }

        var changed = 0;

        for (var index = 0; index < rewrittenChildren.Count; index++)
        {
            if (!ReferenceEquals(originalChildren[index], rewrittenChildren[index]))
            {
                changed++;
            }
        }

        if (changed == 0)
        {
            return node;
        }

        _rebuilt++;
        _shared += originalChildren.Count - changed;

        return ConfigTreeAdapter.Instance.Rebuild(node, rewrittenChildren);
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
