using System;
using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Internal.OptimizingIr.Model;
using Draco.Compiler.Internal.OptimizingIr.Passes;

namespace Draco.Compiler.Internal.OptimizingIr;

/// <summary>
/// Factory functions for creating passes over the IR code.
/// </summary>
internal static class PassFactory
{
    private sealed class DelegatePass : IPass
    {
        private readonly AssemblyPassDelegate passDelegate;

        public DelegatePass(AssemblyPassDelegate passDelegate)
        {
            this.passDelegate = passDelegate;
        }

        public bool Apply(Assembly assembly) => this.passDelegate(assembly);
    }

    /// <summary>
    /// Represents a delegate compatible with <see cref="IPass.Apply(Assembly)"/>.
    /// </summary>
    public delegate bool AssemblyPassDelegate(Assembly assembly);

    /// <summary>
    /// An optimization pass that runs on <see cref="DracoIr.Procedure"/> level for convenience.
    /// </summary>
    public delegate bool ProcedurePassDelegate(Procedure procedure);

    /// <summary>
    /// An optimization pass that runs on <see cref="BasicBlock"/> level for convenience.
    /// </summary>
    public delegate bool BasicBlockPassDelegate(BasicBlock basicBlock);

    /// <summary>
    /// An optimization plass that runs on <see cref="IInstruction"/> level for convenience.
    /// </summary>
    public delegate bool InstructionPassDelegate(IInstruction instruction);

    /// <summary>
    /// Constructs an <see cref="IPass"/> from the given delegate.
    /// </summary>
    /// <param name="passDelegate">The delegate to apply as the pass.</param>
    /// <returns>The constructed pass that applies <paramref name="passDelegate"/>.</returns>
    public static IPass Delegate(AssemblyPassDelegate passDelegate) => new DelegatePass(passDelegate);

    /// <summary>
    /// Constructs an <see cref="IPass"/> from the given delegate that works on
    /// individual <see cref="Model.Procedure"/>s.
    /// </summary>
    /// <param name="passDelegate">The delegate to apply as the pass.</param>
    /// <param name="filter">A filter predicate to only apply the pass on certain procedures that matches.</param>
    /// <returns>The constructed pass that applies <paramref name="passDelegate"/>.</returns>
    public static IPass Procedure(
        ProcedurePassDelegate passDelegate,
        Predicate<Procedure>? filter = null) => Delegate(assembly =>
        {
            filter ??= _ => true;
            var changed = false;
            foreach (var proc in assembly.GetAllProcedures().Cast<Procedure>())
            {
                if (!filter(proc)) continue;
                changed = passDelegate(proc) || changed;
            }
            return changed;
        });

    /// <summary>
    /// Constructs an <see cref="IPass"/> from the given delegate that works on
    /// individual <see cref="Model.BasicBlock"/>s.
    /// </summary>
    /// <param name="passDelegate">The delegate to apply as the pass.</param>
    /// <param name="filter">A filter predicate to only apply the pass on certain blocks that matches.</param>
    /// <returns>The constructed pass that applies <paramref name="passDelegate"/>.</returns>
    public static IPass BasicBlock(
        BasicBlockPassDelegate passDelegate,
        Predicate<BasicBlock>? filter = null) => Delegate(assembly =>
        {
            filter ??= _ => true;
            var changed = false;
            foreach (var proc in assembly.GetAllProcedures().Cast<Procedure>())
            {
                foreach (var bb in proc.BasicBlocks.Values.Cast<BasicBlock>())
                {
                    if (!filter(bb)) continue;
                    changed = passDelegate(bb) || changed;
                }
            }
            return changed;
        });

    /// <summary>
    /// Constructs an <see cref="IPass"/> from the given delegate that works on
    /// individual <see cref="IInstruction"/>s.
    /// </summary>
    /// <param name="passDelegate">The delegate to apply as the pass.</param>
    /// <param name="filter">A filter predicate to only apply the pass on certain instructions that matches.</param>
    /// <returns>The constructed pass that applies <paramref name="passDelegate"/>.</returns>
    public static IPass Instruction(
        InstructionPassDelegate passDelegate,
        Predicate<IInstruction>? filter = null) => Delegate(assembly =>
        {
            filter ??= _ => true;
            var changed = false;
            foreach (var proc in assembly.GetAllProcedures().Cast<Procedure>())
            {
                foreach (var bb in proc.BasicBlocks.Values.Cast<BasicBlock>())
                {
                    foreach (var instr in bb.Instructions)
                    {
                        if (!filter(instr)) continue;
                        changed = passDelegate(instr) || changed;
                    }
                }
            }
            return changed;
        });

    /// <summary>
    /// Constructs a fixpoint pass from the underlying <paramref name="pass"/>, that is repeated as
    /// long as it causes changes.
    /// </summary>
    /// <param name="pass">The pass to repeat.</param>
    /// <returns>The constructed fixpoint pass that repeats <paramref name="pass"/>.</returns>
    public static IPass Fixpoint(IPass pass) => Delegate(assembly =>
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
    public static IPass Sequence(params IPass[] passes) => Sequence(passes.AsEnumerable());

    /// <summary>
    /// Constructs a pass that applies passes in a sequence.
    /// </summary>
    /// <param name="passes">The passes to apply, in their order of application.</param>
    /// <returns>The constructed pass that applies <paramref name="passes"/> in order.</returns>
    public static IPass Sequence(IEnumerable<IPass> passes) => Delegate(assembly =>
    {
        var changed = false;
        foreach (var pass in passes) changed = pass.Apply(assembly) || changed;
        return changed;
    });
}
