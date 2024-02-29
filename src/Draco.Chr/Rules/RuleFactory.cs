using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Chr.Constraints;

namespace Draco.Chr.Rules;

/// <summary>
/// High-level utilities for type-safe rule construction.
/// </summary>
public static partial class RuleFactory
{
    /// <summary>
    /// Separator for simpagation rules.
    /// </summary>
    public static readonly object Sep = new();

    // Construct propagation
    public static Rule Propagation(int headCount) => new Propagation(headCount);
    public static Rule Propagation(params object[] args)
    {
        var typeList = ExtractTypeList(args);
        if (typeList is not null)
        {
            // All types
            return new Propagation(typeList.ToImmutableArray());
        }
        else
        {
            // Complex definition
            return new Propagation(args.Select(ToHead).ToImmutableArray());
        }
    }

    // Construct simplification
    public static Rule Simplification(int headCount) => new Simplification(headCount);
    public static Rule Simplification(params object[] args)
    {
        var typeList = ExtractTypeList(args);
        if (typeList is not null)
        {
            // All types
            return new Simplification(typeList.ToImmutableArray());
        }
        else
        {
            // Complex definition
            return new Simplification(args.Select(ToHead).ToImmutableArray());
        }
    }

    // Construct simpagation
    public static Rule Simpagation(int headKeepCount, int headRemoveCount) =>
        new Simpagation(headKeepCount, headRemoveCount);
    public static Rule Simpagation(params object[] args)
    {
        // It must have a separator
        var sepIndex = Array.IndexOf(args, Sep);
        if (sepIndex == -1) throw new ArgumentException("simpagation rule must have a separator");

        // Extract the elements before and after the separator
        var keep = args.Take(sepIndex).ToArray();
        var remove = args.Skip(sepIndex + 1).Select(ToHead).ToArray();

        // Extract type lists for potential type-only rules
        var keepTypeList = ExtractTypeList(keep);
        var removeTypeList = ExtractTypeList(remove);

        if (keepTypeList is not null && removeTypeList is not null)
        {
            // All types
            return new Simpagation(sepIndex, keepTypeList.Concat(removeTypeList).ToImmutableArray());
        }
        else
        {
            // Complex definition
            return new Simpagation(sepIndex, keep.Concat(remove).Select(ToHead).ToImmutableArray());
        }
    }

    // Naming
    public static Rule Named(this Rule rule, string name) => rule.WithName(name);

    // Guards
    // TODO

    // Action
    // TODO

    // Internal

    private static IEnumerable<Type>? ExtractTypeList(object[] args) =>
        args.All(arg => arg is Type t && t.IsAssignableTo(typeof(Type)))
            ? args.Cast<Type>()
            : null;

    private static Head ToHead(object arg) => arg switch
    {
        Head h => h,
        Type t => Head.OfType(t),
        Var var => Head.Any().Bind(var),
        _ => Head.OfValue(arg),
    };
}
