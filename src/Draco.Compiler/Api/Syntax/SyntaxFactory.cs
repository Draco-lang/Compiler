using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.RedGreenTree.Attributes;

namespace Draco.Compiler.Api.Syntax;

/// <summary>
/// Factory functions for constructing a <see cref="ParseTree"/>.
/// </summary>
[SyntaxFactory(typeof(Internal.Syntax.ParseTree), typeof(ParseTree))]
public static partial class SyntaxFactory
{
}
