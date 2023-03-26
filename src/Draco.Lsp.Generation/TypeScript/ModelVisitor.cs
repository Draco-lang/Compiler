using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Lsp.Generation.TypeScript;

/// <summary>
/// A visitor for the TypeScript model.
/// </summary>
internal abstract class ModelVisitor
{
    public void VisitModel(Model model)
    {
        foreach (var decl in model.Declarations) this.VisitDeclaration(decl);
    }

    public object? VisitDeclaration(Declaration decl) => decl switch
    {
        TypeAlias a => this.VisitTypeAlias(a),
        Interface i => this.VisitInterface(i),
        Namespace n => this.VisitNamespace(n),
        Enum e => this.VisitEnum(e),
        Constant c => this.VisitConstant(c),
        _ => throw new ArgumentOutOfRangeException(nameof(decl)),
    };

    public object? VisitExpression(Expression expr) => expr switch
    {
        UnionTypeExpression union => this.VisitUnionTypeExpression(union),
        AnonymousTypeExpression anon => this.VisitAnonymousTypeExpression(anon),
        ArrayTypeExpression array => this.VisitArrayTypeExpression(array),
        NegateExpression neg => this.VisitNegateExpression(neg),
        ArrayExpression array => this.VisitArrayExpression(array),
        MemberExpression member => this.VisitMemberExpression(member),
        NameExpression name => this.VisitNameExpression(name),
        IntExpression i => this.VisitIntExpression(i),
        StringExpression s => this.VisitStringExpression(s),
        NullExpression n => this.VisitNullExpression(n),
        _ => throw new ArgumentOutOfRangeException(nameof(expr)),
    };

    public object? VisitField(Field field) => field switch
    {
        IndexSignature indexSign => this.VisitIndexSignature(indexSign),
        SimpleField f => this.VisitSimpleField(f),
        _ => throw new ArgumentOutOfRangeException(nameof(field)),
    };

    public virtual object? VisitTypeAlias(TypeAlias a) => this.VisitExpression(a.Type);

    public virtual object? VisitInterface(Interface i)
    {
        foreach (var b in i.Bases) this.VisitExpression(b);
        foreach (var f in i.Fields) this.VisitField(f);
        return null;
    }

    public virtual object? VisitNamespace(Namespace n)
    {
        foreach (var c in n.Constants) this.VisitConstant(c);
        return null;
    }

    public virtual object? VisitEnum(Enum e)
    {
        foreach (var (_, value) in e.Members) this.VisitExpression(value);
        return null;
    }

    public virtual object? VisitConstant(Constant c) => this.VisitExpression(c.Value);

    public virtual object? VisitNameExpression(NameExpression name) => null;

    public virtual object? VisitUnionTypeExpression(UnionTypeExpression union)
    {
        foreach (var alt in union.Alternatives) this.VisitExpression(alt);
        return null;
    }

    public virtual object? VisitAnonymousTypeExpression(AnonymousTypeExpression anon)
    {
        foreach (var field in anon.Fields) this.VisitField(field);
        return null;
    }

    public virtual object? VisitArrayTypeExpression(ArrayTypeExpression array) =>
        this.VisitExpression(array.ElementType);

    public virtual object? VisitNegateExpression(NegateExpression neg) =>
        this.VisitExpression(neg.Operand);

    public virtual object? VisitArrayExpression(ArrayExpression array)
    {
        foreach (var element in array.Elements) this.VisitExpression(element);
        return null;
    }

    public virtual object? VisitMemberExpression(MemberExpression member) =>
        this.VisitExpression(member.Object);

    public virtual object? VisitStringExpression(StringExpression s) => null;
    public virtual object? VisitIntExpression(IntExpression i) => null;
    public virtual object? VisitNullExpression(NullExpression n) => null;

    public virtual object? VisitIndexSignature(IndexSignature indexSign)
    {
        this.VisitExpression(indexSign.KeyType);
        this.VisitExpression(indexSign.ValueType);
        return null;
    }

    public virtual object? VisitSimpleField(SimpleField f) => this.VisitExpression(f.Type);
}
