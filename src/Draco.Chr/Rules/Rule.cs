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
    /// A delegate that can be used to guard a rule application.
    /// </summary>
    /// <param name="head">The list of constraints matching the rule.</param>
    /// <returns>True, if the guard allows rule application.</returns>
    public delegate bool GuardDelegate(IReadOnlyList<IConstraint> head);

    /// <summary>
    /// A delegate that can be used to guard a simpagation rule application.
    /// </summary>
    /// <param name="headKeep">The list of constraints that matched and will be kept.</param>
    /// <param name="headRemove">The list of constraints that matched and will be discarded.</param>
    /// <returns>True, if the guard allows rule application.</returns>
    public delegate bool SimpagationGuardDelegate(
        IReadOnlyList<IConstraint> headKeep,
        IReadOnlyList<IConstraint> headRemove);

    /// <summary>
    /// A delegate that specifies the rule action.
    /// </summary>
    /// <param name="head">The list of constraints matching the rule.</param>
    /// <param name="store">The store that will be manipulated when executed.</param>
    public delegate void BodyDelegate(ImmutableArray<IConstraint> head, ConstraintStore store);

    /// <summary>
    /// A delegate that specifies the simpagation rule action.
    /// </summary>
    /// <param name="headKeep">The list of constraints that matched and will be kept.</param>
    /// <param name="headRemove">The list of constraints that matched and will be discarded.</param>
    /// <param name="store">The store that will be manipulated when executed.</param>
    public delegate void SimpagationBodyDelegate(
        ImmutableArray<IConstraint> headKeep,
        ImmutableArray<IConstraint> headRemove,
        ConstraintStore store);

    /// <summary>
    /// True, if the rule application should be saved in the propagation history, false otherwise.
    /// </summary>
    public virtual bool SaveHistory => false;

    /// <summary>
    /// The vareiable bindings of the rule.
    /// </summary>
    internal ImmutableDictionary<Var, ImmutableArray<int>> VariableBindings { get; } = ImmutableDictionary<Var, ImmutableArray<int>>.Empty;

    /// <summary>
    /// The name of this rule.
    /// </summary>
    public string Name { get; set; } = "unnamed";

    /// <summary>
    /// The number of head elements.
    /// </summary>
    public int HeadCount { get; }

    /// <summary>
    /// The types of the head elements.
    /// </summary>
    public ImmutableArray<Type> HeadTypes { get; }

    /// <summary>
    /// The head definitions of the rule.
    /// </summary>
    public ImmutableArray<Head> HeadDefinitions { get; }

    /// <summary>
    /// The way the heads were specified.
    /// </summary>
    internal HeadListType DefinitionType
    {
        get
        {
            if (this.HeadTypes.IsDefault && this.HeadDefinitions.IsDefault) return HeadListType.SizeSpecified;
            if (!this.HeadTypes.IsDefault && this.HeadDefinitions.IsDefault) return HeadListType.TypesSpecified;
            return HeadListType.ComplexDefinition;
        }
    }

    protected Rule(int headCount)
    {
        if (headCount < 1) throw new ArgumentOutOfRangeException(nameof(headCount), "at least one head must be present");

        this.HeadCount = headCount;
    }

    protected Rule(ImmutableArray<Type> headTypes)
        : this(headTypes.Length)
    {
        this.HeadTypes = headTypes;
    }

    protected Rule(ImmutableArray<Head> headDefinitions)
        : this(headDefinitions.Length)
    {
        this.HeadDefinitions = headDefinitions;

        var variableBindings = new Dictionary<Var, ImmutableArray<int>.Builder>();
        for (var i = 0; i < this.HeadDefinitions.Length; ++i)
        {
            var headDef = this.HeadDefinitions[i];
            if (headDef.BoundTo is null) continue;

            if (!variableBindings.TryGetValue(headDef.BoundTo, out var indexList))
            {
                indexList = ImmutableArray.CreateBuilder<int>();
                variableBindings.Add(headDef.BoundTo, indexList);
            }
            indexList.Add(i);
        }

        this.VariableBindings = variableBindings.ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value.ToImmutable());
    }

    /// <summary>
    /// Sets the name of this rule.
    /// </summary>
    /// <param name="name">The name of the rule.</param>
    /// <returns>The rule itself.</returns>
    public Rule WithName(string name)
    {
        this.Name = name;
        return this;
    }

    /// <summary>
    /// Checks, if the given constraints match the head of this rule.
    /// </summary>
    /// <param name="constraints">The constraints to check.</param>
    /// <returns>True, if the constraints match the head of this rule and can be applied, false otherwise.</returns>
    public abstract bool Accepts(IReadOnlyList<IConstraint> constraints);

    /// <summary>
    /// Applies the rule to the given constraints.
    /// </summary>
    /// <param name="constraints">The constraints that were matched with the rule head.</param>
    /// <returns>The sequence of constraints the rule produced.</returns>
    public abstract IEnumerable<IConstraint> Apply(ImmutableArray<IConstraint> constraints);
}
