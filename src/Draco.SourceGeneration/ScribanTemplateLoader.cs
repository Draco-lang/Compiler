using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;

namespace Draco.SourceGeneration;

public sealed class ScribanTemplateLoader : ITemplateLoader
{
    public static Template Load(string templateName)
    {
        var templateString = GetManifestResourceStreamReader(templateName).ReadToEnd();
        return Template.Parse(templateString);
    }

    public static ScribanTemplateLoader Instance { get; } = new();

    private static StreamReader GetManifestResourceStreamReader(string name)
    {
        name = $"Templates.{name}";
        var assembly = Assembly.GetExecutingAssembly();
        var stream = assembly.GetManifestResourceStream(name)
                  ?? throw new FileNotFoundException($"resource {name} was not embedded in the assembly");
        var reader = new StreamReader(stream);
        return reader;
    }

    private ScribanTemplateLoader()
    {
    }

    public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName) => templateName;
    public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath) =>
        GetManifestResourceStreamReader(templatePath).ReadToEnd();
    public async ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath) =>
        await GetManifestResourceStreamReader(templatePath).ReadToEndAsync();
}
