using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Draco.SourceGeneration.SyntaxTree;

[Generator]
public sealed class SyntaxTreeSourceGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor SyntaxXmlNotFound = new(
        id: "DRC0001",
        title: "Syntax.xml not found",
        messageFormat: "The Syntax.xml file was not found in the project.",
        category: "SyntaxGenerator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor CouldNotReadSyntaxXml = new(
        id: "DRC0002",
        title: "Could not real Syntax.xml",
        messageFormat: "The Syntax.xml file could not be read.",
        category: "SyntaxGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor GenerationError = new(
        id: "DRC0002",
        title: "Generation error",
        messageFormat: "Error while generating code from Syntax.xml: {0}",
        category: "SyntaxGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private sealed class SourceTextReader : TextReader
    {
        private readonly SourceText sourceText;
        private int position;

        public SourceTextReader(SourceText sourceText)
        {
            this.sourceText = sourceText;
            this.position = 0;
        }

        public override int Peek() => this.position < this.sourceText.Length
            ? this.sourceText[this.position]
            : -1;

        public override int Read() => this.position < this.sourceText.Length
            ? this.sourceText[this.position++]
            : -1;

        public override int Read(char[] buffer, int index, int count)
        {
            var length = Math.Min(count, this.sourceText.Length - this.position);
            this.sourceText.CopyTo(this.position, buffer, index, length);
            this.position += length;
            return length;
        }
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var syntaxXmlFiles = context.AdditionalTextsProvider
            .Where(at => Path.GetFileName(at.Path) == "Syntax.xml")
            .Collect();

        context.RegisterSourceOutput(syntaxXmlFiles, static (context, syntaxXmlFiles) =>
        {
            var syntaxXmlFile = syntaxXmlFiles.SingleOrDefault();

            if (syntaxXmlFile is null)
            {
                context.ReportDiagnostic(Diagnostic.Create(SyntaxXmlNotFound, location: null));
                return;
            }

            var syntaxXmlSource = syntaxXmlFile.GetText();
            if (syntaxXmlSource is null)
            {
                context.ReportDiagnostic(Diagnostic.Create(CouldNotReadSyntaxXml, location: null));
                return;
            }

            try
            {
                var serializer = new XmlSerializer(typeof(XmlTree));
                var xmlModel = (XmlTree)serializer.Deserialize(new SourceTextReader(syntaxXmlSource));
                var domainModel = Tree.FromXml(xmlModel);

                var greenTreeCode = CodeGenerator.GenerateGreenTree(domainModel, context.CancellationToken);
                var redTreeCode = CodeGenerator.GenerateRedTree(domainModel, context.CancellationToken);

                context.AddSource("GreenTree.Generated.cs", SourceText.From(greenTreeCode, Encoding.UTF8));
                context.AddSource("RedTree.Generated.cs", SourceText.From(redTreeCode, Encoding.UTF8));
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(GenerationError, location: null, messageArgs: ex));
            }
        });
    }
}
