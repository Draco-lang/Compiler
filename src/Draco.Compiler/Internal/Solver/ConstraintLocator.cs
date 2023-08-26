using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Locator for a constraint.
/// </summary>
internal abstract class ConstraintLocator
{
    /// <summary>
    /// A constraint locator not providing any information.
    /// </summary>
    public static ConstraintLocator Null { get; } = new NullConstraintLocator();

    /// <summary>
    /// Creates a simple syntactic locator.
    /// </summary>
    /// <param name="syntax">The syntax node to connect the location to.</param>
    /// <returns>The locator that will point at the syntax.</returns>
    public static ConstraintLocator Syntax(SyntaxNode syntax) => new SyntaxConstraintLocator(syntax);

    /// <summary>
    /// Creates a constraint locator based on a constraint promise.
    /// </summary>
    /// <param name="promise">The promise to base the locator on.</param>
    /// <returns>The locator that will point point wherever the locator of the promises constraint would point to.</returns>
    public static ConstraintLocator Promise(IConstraintPromise promise) => new PromiseConstraintLocator(promise);

    /// <summary>
    /// Locates information for the constraint.
    /// </summary>
    /// <param name="diagnostic">The diagnostic builder to help the location for.</param>
    public abstract void Locate(Diagnostic.Builder diagnostic);

    private sealed class NullConstraintLocator : ConstraintLocator
    {
        public override void Locate(Diagnostic.Builder diagnostic) { }
    }

    private sealed class SyntaxConstraintLocator : ConstraintLocator
    {
        private readonly SyntaxNode syntax;

        public SyntaxConstraintLocator(SyntaxNode syntax)
        {
            this.syntax = syntax;
        }

        public override void Locate(Diagnostic.Builder diagnostic) =>
            diagnostic.WithLocation(this.syntax.Location);
    }

    private sealed class PromiseConstraintLocator : ConstraintLocator
    {
        private readonly IConstraintPromise promise;

        public PromiseConstraintLocator(IConstraintPromise promise)
        {
            this.promise = promise;
        }

        public override void Locate(Diagnostic.Builder diagnostic) =>
            this.promise.Constraint.Locator.Locate(diagnostic);
    }
}