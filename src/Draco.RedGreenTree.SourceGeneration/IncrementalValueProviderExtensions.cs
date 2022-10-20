using Microsoft.CodeAnalysis;

namespace Draco.RedGreenTree.SourceGeneration;

internal static class IncrementalValueProviderExtensions
{
    public static IncrementalValuesProvider<T> NotNull<T>(this IncrementalValuesProvider<T?> source)
        where T : struct => source
            .Where(x => x.HasValue)
            .Select((x, _) => x!.Value);
}
