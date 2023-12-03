using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

internal sealed class Class : IClass
{
    public TypeSymbol Symbol { get; }

    public string Name => this.Symbol.Name;

    public Class? DeclaringClass { get; }
    IClass? IClass.DeclaringClass => this.DeclaringClass;

    public Module DeclaringModule { get; }
    IModule IClass.DeclaringModule => this.DeclaringModule;

    public Assembly Assembly => this.DeclaringModule.Assembly;
    IAssembly IClass.Assembly => this.Assembly;

    public IReadOnlyList<TypeParameterSymbol> Generics => this.Symbol.GenericParameters;

    public IReadOnlyDictionary<FunctionSymbol, IProcedure> Procedures => this.procedures;

    private readonly Dictionary<FunctionSymbol, IProcedure> procedures = new();

    public Class(Module declaringModule, Class? declaringClass, TypeSymbol symbol)
    {
        this.DeclaringModule = declaringModule;
        this.DeclaringClass = declaringClass;
        this.Symbol = symbol;
    }

    public override string ToString()
    {
        var result = new StringBuilder();

        // TODO: Modifiers

        result.AppendLine($"class {this.Name}");
        result.AppendLine("{");

        // TODO: add members

        result.AppendLine("}");

        return result.ToString();
    }
}
