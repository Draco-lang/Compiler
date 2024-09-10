using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Draco.SourceGeneration;

public abstract class XmlSourceGenerator : IIncrementalGenerator
{
#pragma warning disable RS2008 // Enable analyzer release tracking
    private static readonly DiagnosticDescriptor CouldNotReadXml = new(
        id: "DRC0001",
        title: "Could not read XML file",
        messageFormat: "{0} file could not be read",
        category: "XmlSourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor GenerationError = new(
        id: "DRC0002",
        title: "Generation error",
        messageFormat: "Error while generating code from {0}: {1}",
        category: "XmlSourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
#pragma warning restore RS2008 // Enable analyzer release tracking

    private sealed class SourceTextReader(SourceText sourceText) : TextReader
    {
        private int position = 0;

        public override int Peek() => this.position < sourceText.Length
            ? sourceText[this.position]
            : -1;

        public override int Read() => this.position < sourceText.Length
            ? sourceText[this.position++]
            : -1;

        public override int Read(char[] buffer, int index, int count)
        {
            var length = Math.Min(count, sourceText.Length - this.position);
            sourceText.CopyTo(this.position, buffer, index, length);
            this.position += length;
            return length;
        }
    }

    protected abstract string XmlFileName { get; }
    protected abstract Type XmlModelType { get; }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var xmlFiles = context.AdditionalTextsProvider
            .Where(at => Path.GetFileName(at.Path) == this.XmlFileName)
            .Collect();

        context.RegisterSourceOutput(xmlFiles, (context, xmlFiles) =>
        {
            var xmlFile = xmlFiles.SingleOrDefault();

            // We interpret it as the project not needing it
            if (xmlFile is null) return;

            var xmlSource = xmlFile.GetText();
            if (xmlSource is null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    descriptor: CouldNotReadXml,
                    location: null,
                    messageArgs: this.XmlFileName));
                return;
            }

            try
            {
                var serializer = new XmlSerializer(this.XmlModelType);
                var xmlModel = serializer.Deserialize(new SourceTextReader(xmlSource));

                var generatedSources = this.GenerateSources(xmlModel, context.CancellationToken);

                foreach (var kv in generatedSources)
                {
                    var hintName = kv.Key;
                    var text = kv.Value;

                    context.AddSource(hintName, SourceText.From(text, Encoding.UTF8));
                }
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    descriptor: GenerationError,
                    location: null,
                    messageArgs: [this.XmlFileName, ex]));
            }
        });
    }

    protected abstract IEnumerable<KeyValuePair<string, string>> GenerateSources(object xmlModel, CancellationToken cancellationToken);
}
