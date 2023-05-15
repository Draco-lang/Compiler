using System;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Error;
using Draco.Compiler.Internal.Symbols.Generic;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.Binding;

internal partial class Binder
{
    /// <summary>
    /// Binds the given syntax node to a type symbol. If the referenced symbol is not a type,
    /// an error is reported.
    /// </summary>
    /// <param name="syntax">The type to bind.</param>
    /// <param name="diagnostics">The diagnostics produced during the process.</param>
    /// <returns>The looked up type symbol for <paramref name="syntax"/>.</returns>
    internal TypeSymbol BindTypeToTypeSymbol(TypeSyntax syntax, DiagnosticBag diagnostics)
    {
        var symbol = this.BindType(syntax, diagnostics);
        if (symbol is TypeSymbol type)
        {
            // Ok
            return type;
        }
        else if (symbol is ModuleSymbol)
        {
            // Module, report it
            diagnostics.Add(Diagnostic.Create(
                template: SymbolResolutionErrors.IllegalModuleType,
                location: syntax.Location,
                formatArgs: syntax));
            return IntrinsicSymbols.ErrorType;
        }
        else
        {
            // TODO
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Binds the given type syntax node to a symbol.
    /// </summary>
    /// <param name="syntax">The type to bind.</param>
    /// <param name="diagnostics">The diagnostics produced during the process.</param>
    /// <returns>The looked up symbol for <paramref name="syntax"/>.</returns>
    internal virtual Symbol BindType(TypeSyntax syntax, DiagnosticBag diagnostics) => syntax switch
    {
        // NOTE: The syntax error is already reported
        UnexpectedTypeSyntax => new UndefinedTypeSymbol("<error>"),
        NameTypeSyntax name => this.BindNameType(name, diagnostics),
        MemberTypeSyntax member => this.BindMemberType(member, diagnostics),
        GenericTypeSyntax generic => this.BindGenericType(generic, diagnostics),
        _ => throw new ArgumentOutOfRangeException(nameof(syntax)),
    };

    private Symbol BindNameType(NameTypeSyntax syntax, DiagnosticBag diagnostics) =>
        this.LookupTypeSymbol(syntax.Name.Text, syntax, diagnostics);

    private Symbol BindMemberType(MemberTypeSyntax syntax, DiagnosticBag diagnostics)
    {
        var left = this.BindType(syntax.Accessed, diagnostics);
        var memberName = syntax.Member.Text;
        if (left.IsError)
        {
            // Error, don't cascade
            return left;
        }
        else
        {
            // Module or type member access
            var members = left.Members
                .Where(m => m.Name == memberName)
                .Where(BinderFacts.IsTypeSymbol)
                .ToImmutableArray();
            // Reuse logic from LookupResult
            var result = LookupResult.FromResultSet(members);
            var symbol = result.GetType(memberName, syntax, diagnostics);
            return symbol;
        }
    }

    private Symbol BindGenericType(GenericTypeSyntax syntax, DiagnosticBag diagnostics)
    {
        var instantiated = this.BindType(syntax.Instantiated, diagnostics);
        var args = syntax.Arguments.Values
            .Select(arg => this.BindType(arg, diagnostics))
            // TODO: Why do we even need this cast?
            .Cast<TypeSymbol>()
            .ToImmutableArray();
        // TODO: Check if this is even a generic type?
        // TODO: Check for correch amount of args
        if (instantiated.IsGenericDefinition)
        {
            if (instantiated.GenericParameters.Length != args.Length)
            {
                // TODO
                throw new NotImplementedException();
            }

            return instantiated.GenericInstantiate(instantiated.ContainingSymbol, args);
        }
        else
        {
            // TODO
            throw new NotImplementedException();
        }
    }
}
