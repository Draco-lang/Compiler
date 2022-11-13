using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using InternalFormater = Draco.Compiler.Internal.Syntax.Formater;

namespace Draco.Compiler.Api;

public class CodeFormater
{
    public ParseTree Format(ParseTree input)
    {
        return ParseTree.ToRed(null, new InternalFormater().Format(input.Green));
    }
}
