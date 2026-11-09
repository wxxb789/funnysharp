using System.Collections.ObjectModel;

namespace FunnySharp.OperationPlans;

/// <summary>
/// Describes one named operation without exposing its executable delegate.
/// </summary>
public sealed class OperationInfo
{
    private readonly ReadOnlyCollection<string> _dependsOn;

    /// <summary>
    /// Creates an operation description.
    /// </summary>
    /// <param name="name">The unique, non-empty operation name.</param>
    /// <param name="kind">The operation's effect class.</param>
    /// <param name="dependsOn">Names of earlier operations whose results this operation can use.</param>
    /// <param name="requiresPriorResult">Whether the operation's arguments or decision come from a prior result.</param>
    /// <param name="dynamicWork">Whether the operation emits sub-work that the description does not enumerate.</param>
    /// <param name="conditional">Whether the operation can be skipped based on the current state.</param>
    /// <param name="note">A short human-readable explanation.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> or a dependency name is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException">A required reference is null.</exception>
    public OperationInfo(
        string name,
        OperationKind kind,
        IReadOnlyList<string> dependsOn,
        bool requiresPriorResult,
        bool dynamicWork,
        bool conditional,
        string note)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(dependsOn);
        ArgumentNullException.ThrowIfNull(note);

        var dependencies = dependsOn.ToArray();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var dependency in dependencies)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(dependency);

            if (!seen.Add(dependency))
            {
                throw new ArgumentException(
                    $"Operation '{name}' declares dependency '{dependency}' more than once.",
                    nameof(dependsOn));
            }
        }

        Name = name;
        Kind = kind;
        _dependsOn = Array.AsReadOnly(dependencies);
        RequiresPriorResult = requiresPriorResult;
        DynamicWork = dynamicWork;
        Conditional = conditional;
        Note = note;
    }

    /// <summary>The unique operation name.</summary>
    public string Name { get; }

    /// <summary>The operation's effect class.</summary>
    public OperationKind Kind { get; }

    /// <summary>Names of earlier operations whose results this operation can use.</summary>
    public IReadOnlyList<string> DependsOn => _dependsOn;

    /// <summary>Whether the operation's arguments or decision come from a prior result.</summary>
    public bool RequiresPriorResult { get; }

    /// <summary>Whether the operation emits sub-work that this description does not enumerate.</summary>
    public bool DynamicWork { get; }

    /// <summary>Whether the operation can be skipped based on the current state.</summary>
    public bool Conditional { get; }

    /// <summary>A short human-readable explanation.</summary>
    public string Note { get; }
}
