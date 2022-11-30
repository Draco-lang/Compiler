using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Utilities;
using Draco.Compiler.Api.Syntax;
using static Draco.Compiler.Api.Syntax.ParseTree;
using System.IO;
using Draco.Compiler.Internal.Semantics.Symbols;
using Type = Draco.Compiler.Internal.Semantics.Types.Type;
using Draco.Compiler.Internal.Query;
using Draco.Compiler.Internal.Semantics.Types;
using Draco.Compiler.Internal.Semantics.AbstractSyntax;

namespace Draco.Compiler.Internal.Codegen;

// NOTE: Currently this is only here to have something hacky but runnable for the compiler
// Eventually we'll translate to our own IR and then compile that to IL
/// <summary>
/// Generates low-level C# code from the Draco <see cref="ParseTree"/>.
/// </summary>
internal sealed class CSharpCodegen : AstVisitorBase<string>
{
    private readonly StreamWriter output;

    private readonly Dictionary<Symbol, string> symbolNames = new();
    private int registerCount = 0;
    private int labelCount = 0;

    public CSharpCodegen(Stream output)
    {
        this.output = new(output);
    }

    public void Generate(Ast root)
    {
        // TODO
        throw new NotImplementedException();
    }

    private string AllocateRegister() => $"reg_{this.registerCount++}";
    private string AllocateLabel() => $"label_{this.labelCount++}";
    private string AllocateName(Symbol symbol)
    {
        if (!this.symbolNames.TryGetValue(symbol, out var name))
        {
            // For now we reserve their proper names for globals
            // For the rest we allocate an enumerated ID
            name = symbol.IsGlobal ? symbol.Name : $"sym_{this.symbolNames.Count}";
            this.symbolNames.Add(symbol, name);
        }
        return name;
    }

    private static string TranslateBuiltinType(System.Type type)
    {
        if (type == typeof(void)) return "void";
        return type.FullName ?? type.Name;
    }

    private static string TranslateType(Type type) => type switch
    {
        Type.Builtin builtin => TranslateBuiltinType(builtin.Type),
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };
}
