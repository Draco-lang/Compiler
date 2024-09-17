using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Draco.Coverage;

/// <summary>
/// Represents an instrumented assembly, that weaves in instrumentation code.
/// </summary>
public sealed class InstrumentedAssembly : IDisposable
{
    /// <summary>
    /// Creates an instrumented assembly from the given assembly path.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly.</param>
    /// <param name="settings">The settings for the instrumentation weaver.</param>
    /// <returns>The instrumented assembly.</returns>
    public static InstrumentedAssembly Create(string assemblyPath, InstrumentationWeaverSettings? settings = null)
    {
        var tmpLocation = Path.ChangeExtension(Path.GetTempFileName(), ".dll");
        InstrumentationWeaver.WeaveInstrumentationCode(assemblyPath, tmpLocation, settings);
        return new(tmpLocation);
    }

    /// <summary>
    /// The weaved assembly.
    /// </summary>
    public Assembly WeavedAssembly =>
        this.loadedAssembly ??= this.assemblyLoadContext.LoadFromAssemblyPath(this.weavedAssemblyLocation);

    private readonly AssemblyLoadContext assemblyLoadContext;
    private readonly string weavedAssemblyLocation;
    private Assembly? loadedAssembly;

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
        // First we clear the hits
        this.ClearCoverageData();

        // Then run instrumented code
        action(this.WeavedAssembly);

        // Finally, return coverage data
        return this.GetCoverageResult();
    }

    /// <summary>
    /// Clears the coverage data of the instrumented assembly.
    /// </summary>
    public void ClearCoverageData()
    {
        var assembly = this.WeavedAssembly;
        var coverageCollectorType = NotNullOrNotWeaved(assembly.GetType(typeof(CoverageCollector).FullName!));
        var clearMethod = NotNullOrNotWeaved(coverageCollectorType.GetMethod("Clear"));

        clearMethod.Invoke(null, null);
    }

    /// <summary>
    /// Reads the coverage data of the instrumented assembly.
    /// </summary>
    /// <returns>The coverage result.</returns>
    public CoverageResult GetCoverageResult()
    {
        var assembly = this.WeavedAssembly;

        var coverageCollectorType = NotNullOrNotWeaved(assembly.GetType(typeof(CoverageCollector).FullName!));
        var hitsField = NotNullOrNotWeaved(coverageCollectorType.GetField("Hits"));
        var sequencePointsField = NotNullOrNotWeaved(coverageCollectorType.GetField("SequencePoints"));

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
        if (sequencePointObjs.Length == 0) return CoverageResult.Empty;

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
