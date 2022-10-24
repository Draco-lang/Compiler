using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Api.Syntax;

public sealed class Token : ParseTree
{
    internal Token(ParseTree? parent, Internal.Syntax.ParseTree green)
        : base(parent, green)
    {
    }
}
