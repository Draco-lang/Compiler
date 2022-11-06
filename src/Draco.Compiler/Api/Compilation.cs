using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
using Draco.Query;

namespace Draco.Compiler.Api;

/// <summary>
/// Represents a single compilation session.
/// </summary>
public sealed class Compilation
{
    private readonly QueryDatabase db = new();

    /// <summary>
    /// Retrieves the <see cref="SemanticModel"/> for a tree.
    /// </summary>
    /// <param name="tree">The <see cref="ParseTree"/> root to retrieve the model for.</param>
    /// <returns>The <see cref="SemanticModel"/> with <paramref name="tree"/> as the root.</returns>
    public SemanticModel GetSemanticModel(ParseTree tree) =>
        new(this.db, tree);
}
