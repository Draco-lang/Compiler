using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Lsp.Generation.TypeScript;

internal enum TokenType
{
    EndOfInput,

    Comment,

    Comma,
    Colon,
    Semicolon,
    Pipe,
    QuestionMark,
    Assign,

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
    KeywordExport,
    KeywordExtends,
    KeywordInterface,
    KeywordNamespace,

    LiteralString,
    LiteralInt,
}
