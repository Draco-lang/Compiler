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
    internal ImmutableDictionary<Var, ImmutableArray<int>> VariableBindings { get; } = ImmutableDictionary<Var, ImmutableArray<int>>.Empty;

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

    protected Rule(string name, int headCount)
    {
        if (headCount < 1) throw new ArgumentOutOfRangeException(nameof(headCount), "at least one head must be present");

        this.Name = name;
        this.HeadCount = headCount;
    }

    protected Rule(string name, ImmutableArray<Type> headTypes)
        : this(name, headTypes.Length)
    {
        this.HeadTypes = headTypes;
    }

    protected Rule(string name, ImmutableArray<Head> headDefinitions)
        : this(name, headDefinitions.Length)
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
}
