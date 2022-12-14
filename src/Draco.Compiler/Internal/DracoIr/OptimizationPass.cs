using System;
using System.Collections.Generic;
using System.Linq;

namespace Draco.Compiler.Internal.DracoIr;

/// <summary>
/// Represents some kind of optimization pass.
/// </summary>
internal interface IOptimizationPass
{
    /// <summary>
    /// Applies the pass to the given <see cref="Assembly"/>.
    /// </summary>
    /// <param name="assembly">The <see cref="Assembly"/> to apply the pass to.</param>
    /// <returns>True, if the pass changed something.</returns>
    public bool Apply(Assembly assembly);
}

/// <summary>
/// Utilities for <see cref="IOptimizationPass"/>es.
/// </summary>
internal static class OptimizationPass
{
    /// <summary>
    /// Represents a delegate compatible with <see cref="IOptimizationPass.Apply(Assembly)"/>.
    /// </summary>
    public delegate bool AssemblyPassDelegate(Assembly assembly);

    /// <summary>
    /// An optimization pass that runs on <see cref="DracoIr.Procedure"/> level for convenience.
    /// </summary>
    public delegate bool ProcedurePassDelegate(Procedure procedure);

    /// <summary>
    /// An optimization pass that runs on <see cref="DracoIr.BasicBlock"/> level for convenience.
    /// </summary>
    public delegate bool BasicBlockPassDelegate(BasicBlock basicBlock);

    /// <summary>
    /// An optimization plass that runs on <see cref="DracoIr.Instruction"/> level for convenience.
    /// To be able to mutate the instruction, it is passed as a ref.
    /// </summary>
    public delegate bool InstructionPassDelegate(ref Instruction instruction);

    /// <summary>
    /// Constructs an <see cref="IOptimizationPass"/> from the given delegate.
    /// </summary>
    /// <param name="passDelegate">The delegate to apply as the pass.</param>
    /// <returns>The constructed pass that applies <paramref name="passDelegate"/>.</returns>
    public static IOptimizationPass Delegate(AssemblyPassDelegate passDelegate) => new DelegatePass(passDelegate);

    /// <summary>
    /// Constructs an <see cref="IOptimizationPass"/> from the given delegate that works on
    /// individual <see cref="DracoIr.Procedure"/>s.
    /// </summary>
    /// <param name="passDelegate">The delegate to apply as the pass.</param>
    /// <param name="filter">A filter predicate to only apply the pass on certain procedures that matches.</param>
    /// <returns>The constructed pass that applies <paramref name="passDelegate"/>.</returns>
    public static IOptimizationPass Procedure(
        ProcedurePassDelegate passDelegate,
        Predicate<Procedure>? filter = null) => Delegate(assembly =>
    {
        filter ??= _ => true;
        var changed = false;
        foreach (var proc in assembly.Procedures)
        {
            if (!filter(proc)) continue;
            changed = passDelegate(proc) || changed;
        }
        return changed;
    });

    /// <summary>
    /// Constructs an <see cref="IOptimizationPass"/> from the given delegate that works on
    /// individual <see cref="DracoIr.BasicBlock"/>s.
    /// </summary>
    /// <param name="passDelegate">The delegate to apply as the pass.</param>
    /// <param name="filter">A filter predicate to only apply the pass on certain blocks that matches.</param>
    /// <returns>The constructed pass that applies <paramref name="passDelegate"/>.</returns>
    public static IOptimizationPass BasicBlock(
        BasicBlockPassDelegate passDelegate,
        Predicate<BasicBlock>? filter = null) => Delegate(assembly =>
    {
        filter ??= _ => true;
        var changed = false;
        foreach (var proc in assembly.Procedures)
        {
            foreach (var bb in proc.BasicBlocks)
            {
                if (!filter(bb)) continue;
                changed = passDelegate(bb) || changed;
            }
        }
        return changed;
    });

    /// <summary>
    /// Constructs an <see cref="IOptimizationPass"/> from the given delegate that works on
    /// individual <see cref="DracoIr.Instruction"/>s.
    /// </summary>
    /// <param name="passDelegate">The delegate to apply as the pass.</param>
    /// <param name="filter">A filter predicate to only apply the pass on certain instructions that matches.</param>
    /// <returns>The constructed pass that applies <paramref name="passDelegate"/>.</returns>
    public static IOptimizationPass Instruction(
        InstructionPassDelegate passDelegate,
        Predicate<Instruction>? filter = null) => Delegate(assembly =>
    {
        filter ??= _ => true;
        var changed = false;
        foreach (var proc in assembly.Procedures)
        {
            foreach (var bb in proc.BasicBlocks)
            {
                for (var i = 0; i < bb.Instructions.Count; ++i)
                {
                    var instr = bb.Instructions[i];
                    if (!filter(instr)) continue;
                    changed = passDelegate(ref instr) || changed;
                    bb.Instructions[i] = instr;
                }
            }
        }
        return changed;
    });

    /// <summary>
    /// Constructs a fixpoint pass from the underlying <paramref name="pass"/>, that is repeated as long as it causes changes.
    /// </summary>
    /// <param name="pass">The pass to repeat.</param>
    /// <returns>The constructed fixpoint pass that repeats <paramref name="pass"/>.</returns>
    public static IOptimizationPass Fixpoint(IOptimizationPass pass) => Delegate(assembly =>
    {
        var changed = false;
        while (pass.Apply(assembly)) changed = true;
        return changed;
    });

    /// <summary>
    /// Constructs a pass that applies passes in a sequence.
    /// </summary>
    /// <param name="passes">The passes to apply, in their order of application.</param>
    /// <returns>The constructed pass that applies <paramref name="passes"/> in order.</returns>
    public static IOptimizationPass Sequence(params IOptimizationPass[] passes) => Sequence(passes.AsEnumerable());

    /// <summary>
    /// Constructs a pass that applies passes in a sequence.
    /// </summary>
    /// <param name="passes">The passes to apply, in their order of application.</param>
    /// <returns>The constructed pass that applies <paramref name="passes"/> in order.</returns>
    public static IOptimizationPass Sequence(IEnumerable<IOptimizationPass> passes) => Delegate(assembly =>
    {
        var changed = false;
        foreach (var pass in passes) changed = pass.Apply(assembly) || changed;
        return changed;
    });

    private sealed class DelegatePass : IOptimizationPass
    {
        private readonly AssemblyPassDelegate passDelegate;

        public DelegatePass(AssemblyPassDelegate passDelegate)
        {
            this.passDelegate = passDelegate;
        }

        public bool Apply(Assembly assembly) => this.passDelegate(assembly);
    }
}
