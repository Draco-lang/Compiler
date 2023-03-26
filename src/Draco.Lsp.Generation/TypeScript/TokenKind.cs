using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Lsp.Generation.TypeScript;

/// <summary>
/// The different kinds of TypeScript token.
/// </summary>
internal enum TokenKind
{
    EndOfInput,

    Dot,
    Comma,
    Colon,
    Semicolon,
    Pipe,
    QuestionMark,
    Assign,
    Minus,

    LessThan,
    GreaterThan,

    ParenOpen,
    ParenClose,
    CurlyOpen,
    CurlyClose,
    BracketOpen,
    BracketClose,

    Name,

    KeywordConst,
    KeywordEnum,
    KeywordExport,
    KeywordExtends,
    KeywordInterface,
    KeywordNamespace,
    KeywordNull,
    KeywordReadonly,
    KeywordType,

    LiteralString,
    LiteralInt,
}
