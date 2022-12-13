using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.RedGreenTree.Attributes;
using static Draco.Compiler.Api.Syntax.ParseTree;

namespace Draco.Compiler.Api.Syntax;

/// <summary>
/// Factory functions for constructing a <see cref="ParseTree"/>.
/// </summary>
[SyntaxFactory(typeof(Internal.Syntax.ParseTree), typeof(ParseTree))]
public static partial class SyntaxFactory
{
    // Plumbing methods
    private static Internal.Syntax.ParseTree ToGreen(ParseTree tree) => tree.Green;
    private static Internal.Syntax.ParseTree.Token ToGreen(Token token) => token.Green;
    private static ImmutableArray<Internal.Syntax.ParseTree.Decl> ToGreen(ImmutableArray<Decl> decls) =>
        decls.Select(d => d.Green).ToImmutableArray();
}
