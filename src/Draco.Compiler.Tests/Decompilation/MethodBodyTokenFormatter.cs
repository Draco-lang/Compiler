using System.Diagnostics;
using System.Text;
using Draco.Compiler.Api;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Tests.Decompilation;

internal sealed class MethodBodyTokenFormatter : SymbolVisitor
{
    private readonly StringBuilder _sb;
    private readonly Compilation _compilation;

    // TODO: generics
    private MethodBodyTokenFormatter(StringBuilder sb, Compilation compilation)
    {
        _sb = sb;
        _compilation = compilation;
    }

    public static void FormatTo(Symbol symbol, Compilation compilation, StringBuilder stringBuilder)
    {
        var formatter = new MethodBodyTokenFormatter(stringBuilder, compilation);
        symbol.Accept(formatter);
    }

    public override void VisitLocal(LocalSymbol localSymbol)
    {
        _sb.Append(localSymbol.Type);

        if (!string.IsNullOrEmpty(localSymbol.Name))
        {
            _sb.Append(' ');
            _sb.Append(EscapeName(localSymbol.Name));
        }
    }

    public override void VisitTypeParameter(TypeParameterSymbol typeParameterSymbol)
    {
        throw new NotImplementedException();
    }

    public override void VisitLabel(LabelSymbol labelSymbol)
    {
        throw new NotSupportedException();
    }

    public override void VisitParameter(ParameterSymbol parameterSymbol)
    {
        throw new NotSupportedException();
    }

    public override void VisitField(FieldSymbol fieldSymbol)
    {
        throw new NotImplementedException();
    }

    public override void VisitProperty(PropertySymbol fieldSymbol)
    {
        // can be used with ldtoken
        throw new NotImplementedException();
    }

    public override void VisitModule(ModuleSymbol namespaceSymbol)
    {
        throw new NotSupportedException();
    }

    public override void VisitType(TypeSymbol typeSymbol)
    {
        // when it's standalone type, it's fine to write short name
        FormatTypeCore(typeSymbol, true);
    }

    private void FormatTypeCore(TypeSymbol typeSymbol, bool allowPrimitives)
    {
        if (allowPrimitives)
            if (typeSymbol == _compilation.WellKnownTypes.SystemInt32)
            {
                _sb.Append("int32");
                return;
            }
            else if (typeSymbol == _compilation.WellKnownTypes.SystemString)
            {
                _sb.Append("string");
                return;
            }
            else if (typeSymbol == _compilation.WellKnownTypes.SystemVoid)
            {
                _sb.Append("void");
                return;
            }

        var ancestors = typeSymbol.AncestorChain.Skip(1).Reverse().Skip(2); // skip self then assembly and root namespace, which have empty name
        foreach (var ancestor in ancestors)
        {
            _sb.Append(ancestor.Name);
            if (ancestor is TypeSymbol)
                _sb.Append('/');
            else
            {
                Debug.Assert(ancestor is ModuleSymbol);
                _sb.Append('.');
            }
        }

        _sb.Append(typeSymbol.Name);
    }

    public override void VisitFunction(FunctionSymbol functionSymbol)
    {
        if (!functionSymbol.IsStatic)
            _sb.Append("instance ");

        FormatTypeCore(functionSymbol.ReturnType, true);

        _sb.Append(' ');

        if (functionSymbol.ContainingSymbol is { } container)
        {
            // cannot use short name with referencing members of type
            // e.g. 'string System.Int32::ToString()' is allowed
            // but 'string int32::ToString()' is not
            FormatTypeCore((TypeSymbol)container, false);
            _sb.Append("::");
        }

        _sb.Append(EscapeName(functionSymbol.Name));

        _sb.Append('(');

        for (var i = 0; i < functionSymbol.Parameters.Length; i++)
        {
            if (i > 0)
                _sb.Append(", ");

            var parameter = functionSymbol.Parameters[i];
            FormatTypeCore(parameter.Type, true);
        }

        _sb.Append(')');
    }

    private static string EscapeName(string name)
    {
        return name;
    }
}
