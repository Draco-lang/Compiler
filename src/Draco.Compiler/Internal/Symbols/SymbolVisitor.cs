namespace Draco.Compiler.Internal.Symbols;

internal abstract class SymbolVisitor
{
    public virtual void VisitModule(ModuleSymbol moduleSymbol)
    {
        foreach (var member in moduleSymbol.Members) member.Accept(this);
    }

    public virtual void VisitFunction(FunctionSymbol functionSymbol)
    {
        foreach (var param in functionSymbol.Parameters) param.Accept(this);
    }

    public virtual void VisitType(TypeSymbol typeSymbol)
    {
        foreach (var member in typeSymbol.Members) member.Accept(this);
    }

    public virtual void VisitParameter(ParameterSymbol parameterSymbol)
    {
    }

    public virtual void VisitField(FieldSymbol fieldSymbol)
    {
    }

    public virtual void VisitGlobal(GlobalSymbol globalSymbol)
    {
    }

    public virtual void VisitLocal(LocalSymbol localSymbol)
    {
    }

    public virtual void VisitLabel(LabelSymbol labelSymbol)
    {
    }
}

internal abstract class SymbolVisitor<TResult>
{
    public virtual TResult VisitModule(ModuleSymbol moduleSymbol)
    {
        foreach (var member in moduleSymbol.Members) member.Accept(this);
        return default!;
    }

    public virtual TResult VisitFunction(FunctionSymbol functionSymbol)
    {
        foreach (var param in functionSymbol.Parameters) param.Accept(this);
        return default!;
    }

    public virtual TResult VisitType(TypeSymbol typeSymbol)
    {
        foreach (var member in typeSymbol.Members) member.Accept(this);
        return default!;
    }

    public virtual TResult VisitParameter(ParameterSymbol parameterSymbol) => default!;

    public virtual TResult VisitField(FieldSymbol fieldSymbol) => default!;

    public virtual TResult VisitGlobal(GlobalSymbol globalSymbol) => default!;

    public virtual TResult VisitLocal(LocalSymbol localSymbol) => default!;

    public virtual TResult VisitLabel(LabelSymbol labelSymbol) => default!;
}
