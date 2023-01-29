using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Api.Syntax;
using Draco.RedGreenTree.Attributes;

namespace Draco.Compiler.Internal.Syntax;

internal abstract partial record class ParseNode
{
    /// <summary>
    /// Represents single trivia.
    /// </summary>
    [Ignore(IgnoreFlags.SyntaxFactoryConstruct)]
    internal sealed partial record class Trivia : ParseNode
    {
        /// <summary>
        /// The <see cref="TriviaType"/> of this <see cref="Trivia"/>.
        /// </summary>
        public TriviaType Type { get; }

        /// <summary>
        /// The textual representation of this <see cref="Trivia"/>.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// The width of this <see cref="Trivia"/> in characters.
        /// </summary>
        public override int Width => this.Text.Length;

        public override IEnumerable<ParseNode> Children => Enumerable.Empty<ParseNode>();

        private Trivia(TriviaType type, string text)
        {
            this.Type = type;
            this.Text = text;
        }

        public static Trivia From(TriviaType type, string text) => new Trivia(type, text);
    }
}
