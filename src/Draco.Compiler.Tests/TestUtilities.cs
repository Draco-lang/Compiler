using System.Text;
using Draco.Compiler.Api;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using RoslynMetadataReference = Microsoft.CodeAnalysis.MetadataReference;

namespace Draco.Compiler.Tests;

internal static class TestUtilities
{
    public static string ToPath(params string[] parts) => Path.GetFullPath(Path.Combine(parts));

    public static MetadataReference CompileCSharpToMetadataRef(string code, Stream? xmlStream = null)
    {
        var stream = CompileCSharpToStream("Test.dll", code, xmlStream);
        return MetadataReference.FromPeStream(stream);
    }

    public static Stream CompileCSharpToStream(string assemblyName, string code, Stream? xmlStream = null)
    {
        var sourceText = SourceText.From(code, Encoding.UTF8);
        var tree = SyntaxFactory.ParseSyntaxTree(sourceText);

        var defaultReferences = Basic.Reference.Assemblies.Net70.ReferenceInfos.All
            .Select(r => RoslynMetadataReference.CreateFromStream(new MemoryStream(r.ImageBytes)));

        var compilation = CSharpCompilation.Create(
            assemblyName: assemblyName,
            syntaxTrees: new[] { tree },
            references: defaultReferences,
            options: new CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary));

        var stream = new MemoryStream();
        var emitResult = compilation.Emit(stream, xmlDocumentationStream: xmlStream);
        Assert.True(emitResult.Success);

        stream.Position = 0;
        if (xmlStream is not null) xmlStream.Position = 0;
        return stream;
    }
}
