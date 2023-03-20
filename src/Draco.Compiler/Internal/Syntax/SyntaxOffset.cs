using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Syntax;

/// <summary>
/// Represents a syntactical offset in text.
/// </summary>
/// <param name="Lines">The number of lines offset.</param>
/// <param name="Columns">The number of columns offset.</param>
internal readonly record struct SyntaxOffset(int Lines, int Columns);
