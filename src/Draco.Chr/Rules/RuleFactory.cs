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
    public static Rule Guard(this Rule rule, Delegate guard)
    {
        if (rule is Propagation p)
        {
            p.WithGuard(CreateGuardDelegate(rule, guard));
        }
        else if (rule is Simplification s)
        {
            s.WithGuard(CreateGuardDelegate(rule, guard));
        }
        else if (rule is Simpagation sp)
        {
            sp.WithGuard(CreateSimpagationGuardDelegate(rule, guard));
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(rule), "unknown rule type");
        }

        return rule;
    }

    // Body
    public static Rule Body(this Rule rule, Delegate body)
    {
        if (rule is Propagation p)
        {
            p.WithBody(CreateBodyDelegate(rule, body));
        }
        else if (rule is Simplification s)
        {
            s.WithBody(CreateBodyDelegate(rule, body));
        }
        else if (rule is Simpagation sp)
        {
            sp.WithBody(CreateSimpagationBodyDelegate(rule, body));
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(rule), "unknown rule type");
        }

        return rule;
    }

    // Internal

    private static Rule.GuardDelegate CreateGuardDelegate(Rule rule, Delegate guard)
    {
        var parameters = guard.Method.GetParameters();
        if (parameters.Length != rule.HeadCount)
        {
            throw new ArgumentException("guard must have the same number of parameters as the rule has heads");
        }

        if (guard.Method.ReturnType != typeof(bool))
        {
            throw new ArgumentException("guard must return a boolean");
        }

        var argExtractors = parameters
            .Select(p => GetArgumentExtractor(p.ParameterType))
            .ToList();

        return heads =>
        {
            var args = heads
                .Zip(argExtractors, (head, extractor) => extractor(head))
                .ToArray();
            return (bool)guard.Method.Invoke(guard.Target, args)!;
        };
    }

    private static Rule.SimpagationGuardDelegate CreateSimpagationGuardDelegate(Rule rule, Delegate guard)
    {
        var parameters = guard.Method.GetParameters();
        if (parameters.Length != rule.HeadCount)
        {
            throw new ArgumentException("guard must have the same number of parameters as the rule has heads");
        }

        if (guard.Method.ReturnType != typeof(bool))
        {
            throw new ArgumentException("guard must return a boolean");
        }

        var argExtractors = parameters
            .Select(p => GetArgumentExtractor(p.ParameterType))
            .ToList();

        return (headsKeep, headsRemove) =>
        {
            var args = headsKeep
                .Concat(headsRemove)
                .Zip(argExtractors, (head, extractor) => extractor(head))
                .ToArray();
            return (bool)guard.Method.Invoke(guard.Target, args)!;
        };
    }

    private static Rule.BodyDelegate CreateBodyDelegate(Rule rule, Delegate body)
    {
        var parameters = body.Method.GetParameters();
        if (parameters.Length != rule.HeadCount + 1)
        {
            throw new ArgumentException("body must have the same number of parameters as the rule has heads plus one");
        }

        if (!parameters[0].ParameterType.IsAssignableTo(typeof(ConstraintStore)))
        {
            throw new ArgumentException("body must have the store as the first parameter");
        }

        if (body.Method.ReturnType != typeof(void))
        {
            throw new ArgumentException("body must return void");
        }

        var argExtractors = parameters
            .Skip(1)
            .Select(p => GetArgumentExtractor(p.ParameterType))
            .ToList();

        return (heads, store) =>
        {
            var args = heads
                .Zip(argExtractors, (head, extractor) => extractor(head))
                .Prepend(store)
                .ToArray();
            body.Method.Invoke(body.Target, args);
        };
    }

    private static Rule.SimpagationBodyDelegate CreateSimpagationBodyDelegate(Rule rule, Delegate body)
    {
        var parameters = body.Method.GetParameters();
        if (parameters.Length != rule.HeadCount + 2)
        {
            throw new ArgumentException("body must have the same number of parameters as the rule has heads plus two");
        }

        if (!parameters[0].ParameterType.IsAssignableTo(typeof(ConstraintStore)))
        {
            throw new ArgumentException("body must have the store as the first parameter");
        }

        if (body.Method.ReturnType != typeof(void))
        {
            throw new ArgumentException("body must return void");
        }

        var argExtractors = parameters
            .Skip(1)
            .Select(p => GetArgumentExtractor(p.ParameterType))
            .ToList();

        return (headsKeep, headsRemove, store) =>
        {
            var args = headsKeep
                .Concat(headsRemove)
                .Zip(argExtractors, (head, extractor) => extractor(head))
                .Prepend(store)
                .ToArray();
            body.Method.Invoke(body.Target, args);
        };
    }

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

    private static Func<object, object> GetArgumentExtractor(Type type)
    {
        if (type.IsAssignableTo(typeof(IConstraint))) return x => x;
        return x => ((IConstraint)x).Value;
    }
}
