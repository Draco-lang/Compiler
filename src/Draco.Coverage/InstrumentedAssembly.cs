using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Coverage;

public sealed class InstrumentedAssembly : IDisposable
{
    public static InstrumentedAssembly Create(string assemblyPath, InstrumentationWeaverSettings? settings = null)
    {
        var tmpLocation = Path.ChangeExtension(Path.GetTempFileName(), ".dll");
        InstrumentationWeaver.WeaveInstrumentationCode(assemblyPath, tmpLocation, settings);
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

    /// <summary>
    /// Runs the given action with the instrumented assembly.
    /// </summary>
    /// <param name="action">The action to run.</param>
    /// <returns>The coverage result.</returns>
    public CoverageResult Instrument(Action<Assembly> action)
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
        var sequencePointObjs = (Array)sequencePointsField.GetValue(null)!;
        return ToCoverageResult(hits, sequencePointObjs);
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

    private static CoverageResult ToCoverageResult(int[] hits, Array sequencePointObjs)
    {
        if (sequencePointObjs.Length == 0) return new CoverageResult([]);

        var objType = sequencePointObjs.GetValue(0)!.GetType();
        var objFields = objType.GetFields();

        var sequencePoints = ImmutableArray.CreateBuilder<CoverageEntry>(hits.Length);
        for (var i = 0; i < hits.Length; ++i)
        {
            var fields = objFields.Select(f => f.GetValue(sequencePointObjs.GetValue(i))).ToArray();
            var sequencePoint = (SequencePoint)Activator.CreateInstance(typeof(SequencePoint), fields)!;
            sequencePoints.Add(new CoverageEntry(sequencePoint, hits[i]));
        }
        return new CoverageResult(sequencePoints.ToImmutable());
    }
}
