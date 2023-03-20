using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Api.Syntax;

/// <summary>
/// Represents a portion of source text.
/// </summary>
/// <param name="Start">The start index of the text.</param>
/// <param name="Length">The length of the text.</param>
public readonly record struct TextSpan(int Start, int Length);
