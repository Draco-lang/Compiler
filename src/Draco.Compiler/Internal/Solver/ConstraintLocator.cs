using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;

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
    /// Creates a constraint locator based on anonter constraint.
    /// </summary>
    /// <param name="constraint">The constraint to base the locator on.</param>
    /// <returns>The locator that will point point wherever the locator of the constraint would point to.</returns>
    public static ConstraintLocator Constraint(IConstraint constraint) => new ReferenceConstraintLocator(constraint);

    /// <summary>
    /// Creates a constraint locator based on a constraint promise.
    /// </summary>
    /// <param name="promise">The promise to base the locator on.</param>
    /// <returns>The locator that will point point wherever the locator of the promises constraint would point to.</returns>
    public static ConstraintLocator Promise(IConstraintPromise promise) => Constraint(promise.Constraint);

    /// <summary>
    /// Locates information for the constraint.
    /// </summary>
    /// <param name="diagnostic">The diagnostic builder to help the location for.</param>
    public abstract void Locate(Diagnostic.Builder diagnostic);

    /// <summary>
    /// Wraps the constraint locator to provide additional information.
    /// </summary>
    /// <param name="relatedInformation">The related information to append.</param>
    /// <returns>The wrapped locator.</returns>
    public ConstraintLocator WithRelatedInformation(DiagnosticRelatedInformation relatedInformation) =>
        new WithRelatedInfoConstraintLocator(this, relatedInformation);

    /// <summary>
    /// Wraps the constraint locator to provide additional information.
    /// </summary>
    /// <param name="location">The location of the related information.</param>
    /// <param name="format">The format message.</param>
    /// <param name="formatArgs">The format arguments.</param>
    /// <returns>The wrapped locator.</returns>
    public ConstraintLocator WithRelatedInformation(
        Location? location,
        string format,
        params object?[] formatArgs) => this.WithRelatedInformation(DiagnosticRelatedInformation.Create(
        location: location,
        format: format,
        formatArgs: formatArgs));

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

    private sealed class ReferenceConstraintLocator : ConstraintLocator
    {
        private readonly IConstraint constraint;

        public ReferenceConstraintLocator(IConstraint constraint)
        {
            this.constraint = constraint;
        }

        public override void Locate(Diagnostic.Builder diagnostic) =>
            this.constraint.Locator.Locate(diagnostic);
    }

    private sealed class WithRelatedInfoConstraintLocator : ConstraintLocator
    {
        private readonly ConstraintLocator underlying;
        private DiagnosticRelatedInformation relatedInfo;

        public WithRelatedInfoConstraintLocator(
            ConstraintLocator underlying,
            DiagnosticRelatedInformation relatedInfo)
        {
            this.underlying = underlying;
            this.relatedInfo = relatedInfo;
        }

        public override void Locate(Diagnostic.Builder diagnostic)
        {
            this.underlying.Locate(diagnostic);
            diagnostic.WithRelatedInformation(this.relatedInfo);
        }
    }
}
