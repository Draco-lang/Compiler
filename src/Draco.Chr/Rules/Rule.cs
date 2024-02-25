using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Draco.Chr.Constraints;

namespace Draco.Chr.Rules;

/// <summary>
/// Represents a CHR rule.
/// </summary>
public abstract class Rule
{
    /// <summary>
    /// True, if the rule application should be saved in the propagation history, false otherwise.
    /// </summary>
    public virtual bool SaveHistory => false;

    /// <summary>
    /// The vareiable bindings of the rule.
    /// </summary>
    internal IReadOnlyDictionary<Var, List<IConstraint>> VariableBindings => this.variableBindings;

    /// <summary>
    /// The name of this rule.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The number of head elements.
    /// </summary>
    public int HeadCount { get; }

    /// <summary>
    /// The types of the head elements.
    /// </summary>
    public ImmutableArray<Type> HeadTyőes { get; }

    private readonly Dictionary<Var, List<IConstraint>> variableBindings = [];
}
