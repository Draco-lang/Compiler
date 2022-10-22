using System.Linq;
using Microsoft.CodeAnalysis;

namespace Draco.RedGreenTree.SourceGeneration;

internal static class Diagnostics
{
    public static readonly DiagnosticDescriptor redTypeNameDoesNotMatchGreen = new(
        "RGT0001",
        "Red type name does not match green type name",
        "Red type name {0} should match green type name {1}",
        "naming",
        DiagnosticSeverity.Error,
        true);

    public static void ReportRedTypeNameDoesNotMatchGreen(this SourceProductionContext ctx, INamedTypeSymbol red, INamedTypeSymbol green)
    {
        var location = red.Locations.FirstOrDefault();
        var redName = red.Name;
        var greenName = green.Name;

        var diagnostic = Diagnostic.Create(
            redTypeNameDoesNotMatchGreen,
            location,
            redName,
            greenName);
        ctx.ReportDiagnostic(diagnostic);
    }
}
