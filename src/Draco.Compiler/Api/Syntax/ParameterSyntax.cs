using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Api.Syntax;

public partial class ParameterSyntax
{
    public new FunctionDeclarationSyntax Parent => (FunctionDeclarationSyntax)((SyntaxNode)this).Parent!;
    public int Index
    {
        get
        {
            foreach (var (parameter, i) in this.Parent.ParameterList.Values.Select((s, i) => (s, i)))
            {
                if (parameter == this) return i;
            }
            throw new InvalidOperationException();
        }
    }
}
