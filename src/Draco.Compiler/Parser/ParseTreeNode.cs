using Draco.Compiler.Syntax;

namespace Draco.Compiler.Parser;

internal abstract record class ParseTree {
    public sealed record class ParamList(
        ) : ParseTree;

    public sealed record class Param(
        IToken Identifier,
        TypeAnno? _Type) : ParseTree;

    public sealed record class TypeAnno(
        IToken ColonToken,
        Type _Type) : ParseTree;

    public abstract record class Type : ParseTree {

    }

    public abstract record class Decl : ParseTree {
        public abstract record class Func(
            IToken FuncKeyword,
            IToken Identifier,
            ParamList Params,
            IToken OpenParenToken,
            TypeAnno? _Type,
            IToken CloseParenToken,
            FuncBody Body) : Decl;
    }
}
