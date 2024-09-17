using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Coverage;

public sealed class InstrumentedAssembly : IDisposable
{
    public static InstrumentedAssembly Create(string assemblyPath, InstrumentationWeaverSettings? settings = null)
    {
        var tmpLocation = Path.GetTempFileName();
        return new(tmpLocation);
    }

    private readonly AssemblyLoadContext assemblyLoadContext;
    private readonly string weavedAssemblyLocation;
    private Assembly? loadedAssembly;

    private Assembly LoadedAssembly =>
        this.loadedAssembly ??= this.assemblyLoadContext.LoadFromAssemblyPath(this.weavedAssemblyLocation);

    private InstrumentedAssembly(string weavedAssemblyLocation)
    {
        this.assemblyLoadContext = new AssemblyLoadContext("Draco.Coverage.LoadContext", isCollectible: true);
        this.weavedAssemblyLocation = weavedAssemblyLocation;
    }

    public void Instrument(Action<Assembly> action)
    {
        var assembly = this.LoadedAssembly;

        var coverageCollectorType = NotNullOrNotWeaved(assembly.GetType(typeof(CoverageCollector).FullName!));
        var clearMethod = NotNullOrNotWeaved(coverageCollectorType.GetMethod("Clear"));
        var hitsField = NotNullOrNotWeaved(coverageCollectorType.GetField("Hits"));
        var sequencePointsField = NotNullOrNotWeaved(coverageCollectorType.GetField("SequencePoints"));

        // First we clear the hits
        clearMethod.Invoke(null, null);

        // Then run instrumented code
        action(assembly);

        // Finally, return coverage data
        var hits = (int[])hitsField.GetValue(null)!;
        var sequencePoints = (CoverageCollector.SequencePoint[])sequencePointsField.GetValue(null)!;
    }

    ~InstrumentedAssembly()
    {
        this.Dispose();
    }

    public void Dispose()
    {
        this.loadedAssembly = null;
        this.assemblyLoadContext.Unload();
        File.Delete(this.weavedAssemblyLocation);
    }

    private static T NotNullOrNotWeaved<T>(T? value)
        where T : class =>
        value ?? throw new InvalidOperationException("the assembly was not weaved");
}
