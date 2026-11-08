#pragma warning disable FS0017

using FunnySharp;
using FunnySharp.TreeTransformations;

namespace FunnySharp.TestSupport.TreeComparison;

/// <summary>The hand-written iterative consumer: explicit stacks for resolve, rebuild, and validation.</summary>
internal sealed class IterativeConfigEditor : IConfigEditor
{
    private readonly ConfigNode _base;
    private int _visited;
    private int _rebuilt;
    private int _shared;
    private int _applied;
    private int _errors;

    internal IterativeConfigEditor(ConfigNode baseTree) => _base = baseTree;

    public string Name => "Iterative explicit stack";

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

            if (!TryResolve(tree, item, errors, out var target, out var frames))
            {
                continue;
            }

            if (target.IsSection)
            {
                errors.Add(ConfigErrors.NotAValue(item, item.Path.ToLocation()));
                _errors++;
                continue;
            }

            if (!ConfigKernel.IsValid(target.Name, item.Value))
            {
                errors.Add(ConfigErrors.InvalidValue(item, item.Path.ToLocation()));
                _errors++;
                continue;
            }

            if (target.Value == item.Value)
            {
                continue;
            }

            var edited = target with { Value = item.Value };
            _rebuilt++;
            tree = Rebuild(frames, edited);
            _applied++;
        }

        ValidateCore(tree, errors, stopOnFirst: false);

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

            var error = ResolveFirst(tree, item, out var target, out var frames);

            if (error is not null)
            {
                _errors++;
                return Result<ConfigNode, ConfigError>.Failure(error);
            }

            if (target!.IsSection)
            {
                _errors++;
                return Result<ConfigNode, ConfigError>.Failure(
                    ConfigErrors.NotAValue(item, item.Path.ToLocation()));
            }

            if (!ConfigKernel.IsValid(target.Name, item.Value))
            {
                _errors++;
                return Result<ConfigNode, ConfigError>.Failure(
                    ConfigErrors.InvalidValue(item, item.Path.ToLocation()));
            }

            if (target.Value == item.Value)
            {
                continue;
            }

            var edited = target with { Value = item.Value };
            _rebuilt++;
            tree = Rebuild(frames, edited);
            _applied++;
        }

        var validationError = ValidateCore(tree, errors: null, stopOnFirst: true);

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
        ValidateCore(_base, errors, stopOnFirst: false);

        return ConfigResults.From(_base, errors);
    }

    private bool TryResolve(
        ConfigNode root,
        ConfigOverride item,
        List<ConfigError> errors,
        out ConfigNode target,
        out ResolveFrame[] frames)
    {
        target = root;
        frames = new ResolveFrame[item.Path.Count];

        for (var index = 0; index < item.Path.Count; index++)
        {
            _visited++;
            var segment = item.Path[index];
            var children = target.Children;
            var childIndex = ConfigTreeAdapter.Instance.ChildIndex(target, segment);

            if (!childIndex.TryGetValue(out var position))
            {
                errors.Add(ConfigErrors.Path(item, segment, LocationAt(item.Path, index)));
                _errors++;
                return false;
            }

            frames[index] = new ResolveFrame(target, children, position);
            target = children[position];
        }

        _visited++;
        return true;
    }

    private ConfigError? ResolveFirst(
        ConfigNode root,
        ConfigOverride item,
        out ConfigNode? target,
        out ResolveFrame[] frames)
    {
        target = root;
        frames = new ResolveFrame[item.Path.Count];

        for (var index = 0; index < item.Path.Count; index++)
        {
            _visited++;
            var segment = item.Path[index];
            var children = target.Children;
            var childIndex = ConfigTreeAdapter.Instance.ChildIndex(target, segment);

            if (!childIndex.TryGetValue(out var position))
            {
                target = null;
                return ConfigErrors.Path(item, segment, LocationAt(item.Path, index));
            }

            frames[index] = new ResolveFrame(target, children, position);
            target = children[position];
        }

        _visited++;
        return null;
    }

    private ConfigNode Rebuild(ResolveFrame[] frames, ConfigNode edited)
    {
        var newChild = edited;

        for (var index = frames.Length - 1; index >= 0; index--)
        {
            var frame = frames[index];
            var newChildren = new ConfigNode[frame.Children.Count];

            for (var childIndex = 0; childIndex < newChildren.Length; childIndex++)
            {
                newChildren[childIndex] = frame.Children[childIndex];
            }

            newChildren[frame.ChildIndex] = newChild;
            _rebuilt++;
            _shared += frame.Children.Count - 1;
            newChild = frame.Node with { Children = newChildren };
        }

        return newChild;
    }

    private ConfigError? ValidateCore(ConfigNode root, List<ConfigError>? errors, bool stopOnFirst)
    {
        var stack = new List<ValidationFrame> { new(root, depth: 0) };
        var path = new TreePathSegment[16];
        var pathLength = 0;
        _visited++;

        if (!root.IsSection)
        {
            var rootError = ValidateLeaf(root, path, pathLength: 0);

            if (rootError is not null)
            {
                errors?.Add(rootError);
                _errors++;
            }

            return stopOnFirst ? rootError : null;
        }

        while (stack.Count > 0)
        {
            var frame = stack[^1];

            if (frame.Node.IsSection && frame.NextChild < frame.Node.Children.Count)
            {
                var childIndex = frame.NextChild;
                var child = frame.Node.Children[childIndex];
                var segment = ConfigTreeAdapter.SegmentFor(frame.Node, childIndex);
                frame.NextChild = childIndex + 1;
                stack[^1] = frame;

                if (pathLength >= path.Length)
                {
                    Array.Resize(ref path, path.Length * 2);
                }

                path[pathLength++] = segment;
                _visited++;

                if (child.IsSection)
                {
                    stack.Add(new ValidationFrame(child, frame.Depth + 1));
                }
                else
                {
                    var error = ValidateLeaf(child, path, pathLength);

                    if (error is not null)
                    {
                        errors?.Add(error);
                        _errors++;

                        if (stopOnFirst)
                        {
                            return error;
                        }
                    }

                    pathLength--;
                }

                continue;
            }

            stack.RemoveAt(stack.Count - 1);
            pathLength = stack.Count == 0 ? 0 : stack[^1].Depth;
        }

        return null;
    }

    private static Location LocationAt(TreePath path, int index) =>
        TreePath.FromSegments(path.Segments.Take(index + 1)).ToLocation();

    private static ConfigError? ValidateLeaf(
        ConfigNode node,
        TreePathSegment[] path,
        int pathLength)
    {
        if (ConfigKernel.IsValid(node.Name, node.Value!))
        {
            return null;
        }

        var location = Location.Root;

        for (var index = 0; index < pathLength; index++)
        {
            location = path[index].AppendTo(location);
        }

        return ConfigErrors.InvalidBase(node.Name, node.Value!, location);
    }

    private readonly struct ResolveFrame
    {
        public ResolveFrame(ConfigNode node, IReadOnlyList<ConfigNode> children, int childIndex)
        {
            Node = node;
            Children = children;
            ChildIndex = childIndex;
        }

        public ConfigNode Node { get; }

        public IReadOnlyList<ConfigNode> Children { get; }

        public int ChildIndex { get; }
    }

    private struct ValidationFrame
    {
        public ValidationFrame(ConfigNode node, int depth)
        {
            Node = node;
            Depth = depth;
            NextChild = 0;
        }

        public ConfigNode Node { get; }

        public int Depth { get; }

        public int NextChild { get; set; }
    }
}
