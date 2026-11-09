#pragma warning disable FS0017

using FunnySharp;
using FunnySharp.TreeTransformations;

namespace FunnySharp.TestSupport.TreeComparison;

/// <summary>The zipper consumer: navigate by segment, replace, reconstruct the root, validate.</summary>
internal sealed class ZipperConfigEditor : IConfigEditor
{
    private readonly ConfigNode _base;
    private int _visited;
    private int _rebuilt;
    private int _shared;
    private int _applied;
    private int _errors;

    internal ZipperConfigEditor(ConfigNode baseTree) => _base = baseTree;

    public string Name => "Zipper";

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

            var zipper = Zipper<ConfigNode>.FromRoot(tree, ConfigTreeAdapter.Instance);
            var failed = false;

            for (var index = 0; index < item.Path.Count; index++)
            {
                _visited++;
                var segment = item.Path[index];
                var next = zipper.Down(segment);

                if (!next.TryGetValue(out var child))
                {
                    errors.Add(ConfigErrors.Path(item, segment, segment.AppendTo(zipper.Location)));
                    _errors++;
                    failed = true;
                    break;
                }

                zipper = child;
            }

            if (failed)
            {
                continue;
            }

            _visited++;

            if (zipper.Current.IsSection)
            {
                errors.Add(ConfigErrors.NotAValue(item, zipper.Location));
                _errors++;
                continue;
            }

            if (!ConfigKernel.IsValid(zipper.Current.Name, item.Value))
            {
                errors.Add(ConfigErrors.InvalidValue(item, zipper.Location));
                _errors++;
                continue;
            }

            if (zipper.Current.Value == item.Value)
            {
                continue;
            }

            var edited = zipper.Replace(zipper.Current with { Value = item.Value });
            var root = edited.ToRoot();
            _rebuilt += 1 + root.RebuiltNodes;
            _shared += root.SharedNodes;
            _applied++;
            tree = root.Root;
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

            var zipper = Zipper<ConfigNode>.FromRoot(tree, ConfigTreeAdapter.Instance);
            ConfigError? pathError = null;

            for (var index = 0; index < item.Path.Count; index++)
            {
                _visited++;
                var segment = item.Path[index];
                var next = zipper.Down(segment);

                if (!next.TryGetValue(out var child))
                {
                    pathError = ConfigErrors.Path(item, segment, segment.AppendTo(zipper.Location));
                    break;
                }

                zipper = child;
            }

            if (pathError is not null)
            {
                _errors++;
                return Result<ConfigNode, ConfigError>.Failure(pathError);
            }

            _visited++;

            if (zipper.Current.IsSection)
            {
                _errors++;
                return Result<ConfigNode, ConfigError>.Failure(
                    ConfigErrors.NotAValue(item, zipper.Location));
            }

            if (!ConfigKernel.IsValid(zipper.Current.Name, item.Value))
            {
                _errors++;
                return Result<ConfigNode, ConfigError>.Failure(
                    ConfigErrors.InvalidValue(item, zipper.Location));
            }

            if (zipper.Current.Value == item.Value)
            {
                continue;
            }

            var edited = zipper.Replace(zipper.Current with { Value = item.Value });
            var root = edited.ToRoot();
            _rebuilt += 1 + root.RebuiltNodes;
            _shared += root.SharedNodes;
            _applied++;
            tree = root.Root;
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
