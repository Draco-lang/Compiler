using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Internal.Semantics.AbstractSyntax;

namespace Draco.Compiler.Internal.Semantics.DFA;

internal abstract record class CFG<TStmt, TCondition>
{
    /// <summary>
    /// Represents a single block in <see cref="CFG"/> consisting non-branching statements, the block ends branching.
    /// </summary>
    /// <param name="Statements">List of statements contained in the <see cref="Block"/>.</param>
    /// <param name="Branches">List of possible <see cref="Branch"/>es.</param>
    internal sealed record class Block(ImmutableArray<TStmt> Statements, ImmutableArray<Branch> Branches) : CFG<TStmt, TCondition>
    {
        internal sealed new record class Builder(List<TStmt> Statements) : CFG<TStmt, TCondition>
        {
            public Builder() : this(new List<TStmt>()) { }

            private List<Branch.Builder> branches = new List<Branch.Builder>();
            public void AddBranch(Branch.Builder branch)
            {
                this.branches.Add(branch);
            }

            public void AddStatement(TStmt statement)
            {
                this.Statements.Add(statement);
            }

            public Block Build()
            {
                if (this.branches.Count == 0) throw new InvalidOperationException("Block must have branches before build.");
                return new Block(this.Statements.ToImmutableArray(), this.branches.Select(x => x.Build()).ToImmutableArray());
            }
        }
    }

    /// <summary>
    /// Represents single <see cref="Branch"/>.
    /// </summary>
    /// <param name="Target">The <see cref="Block"/> targeted by this <see cref="Branch"/>.</param>
    /// <param name="Condition">The condition that must be met so the execution continues to the <paramref name="Target"/>.</param>
    internal sealed record class Branch(Block Target, TCondition? Condition) : CFG<TStmt, TCondition>
    {
        internal sealed new record class Builder(TCondition? Condition) : CFG<TStmt, TCondition>
        {
            private Block.Builder? block = null;
            public void AddBlock(Block.Builder block)
            {
                this.block = block;
            }

            public Branch Build()
            {
                if (this.block is null) throw new InvalidOperationException("Block must be declared before branch is build.");
                return new Branch(this.block.Build(), this.Condition);
            }
        }
    }

    internal sealed class Builder
    {
        public CFG<TStmt, TCondition> Current;
        private Stack<CFG<TStmt, TCondition>> stack = new Stack<CFG<TStmt, TCondition>>();

        public Builder(Block.Builder block) => this.Current = block;

        public void PushBlock(Block.Builder block)
        {
            if (this.Current is not Branch.Builder brch) throw new InvalidOperationException();
            brch.AddBlock(block);
            this.stack.Push(brch);
            this.Current = block;
        }

        public void PushStatement(TStmt statement)
        {
            if (this.Current is not Block.Builder blc) throw new InvalidOperationException();
            blc.AddStatement(statement);
        }

        public void PushBranch(Branch.Builder block)
        {
            if (this.Current is not Block.Builder blc) throw new InvalidOperationException();
            blc.AddBranch(block);
            this.stack.Push(blc);
            this.Current = block;
        }

        public void PopBlock()
        {
            if (this.Current is not Block.Builder) throw new InvalidOperationException();
            this.Current = this.stack.Pop();
        }

        public void PopBranch()
        {
            if (this.Current is not Branch.Builder) throw new InvalidOperationException();
            this.Current = this.stack.Pop();
        }

        public Block Build() => (this.stack.First() as Block.Builder)!.Build();
    }
}
