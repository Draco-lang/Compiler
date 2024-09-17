using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Draco.Coverage;

/// <summary>
/// Represents an instrumented assembly, that weaves in instrumentation code.
/// </summary>
public sealed class InstrumentedAssembly : IDisposable
{
    /// <summary>
    /// Weaves instrumentation code into the given assembly.
    /// </summary>
    /// <param name="sourceStream">The stream containing the assembly to weave.</param>
    /// <param name="targetStream">The stream to write the weaved assembly to.</param>
    /// <param name="settings">The settings for the weaver.</param>
    public static void Weave(Stream sourceStream, Stream targetStream, InstrumentationWeaverSettings? settings = null) =>
        InstrumentationWeaver.WeaveInstrumentationCode(sourceStream, targetStream, settings);

    /// <summary>
    /// Creates an instrumented assembly from an already weaved assembly.
    /// </summary>
    /// <param name="assembly">The weaved assembly.</param>
    /// <returns>The instrumented assembly.</returns>
    public static InstrumentedAssembly FromWeavedAssembly(Assembly assembly) => new(assembly);

    /// <summary>
    /// Creates an instrumented assembly from a stream containing an unweaved assembly.
    /// </summary>
    /// <param name="sourceStream">The stream containing the unweaved assembly.</param>
    /// <returns>The instrumented assembly.</returns>
    public static InstrumentedAssembly Create(Stream sourceStream) => new(sourceStream);

    /// <summary>
    /// The weaved assembly.
    /// </summary>
    public Assembly WeavedAssembly
    {
        get
        {
            if (this.weavedAssembly is not null) return this.weavedAssembly;

            using var ms = new MemoryStream();
            this.weavedAssemblyStream!.CopyTo(ms);
            ms.Position = 0;
            this.weavedAssembly = Assembly.Load(ms.ToArray());
            return this.weavedAssembly;
        }
    }

    private readonly Stream? weavedAssemblyStream;
    private Assembly? weavedAssembly;

    private InstrumentedAssembly(Stream weavedAssemblyStream)
    {
        this.weavedAssemblyStream = weavedAssemblyStream;
    }

    private InstrumentedAssembly(Assembly weavedAssembly)
    {
        this.weavedAssembly = weavedAssembly;
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
        this.weavedAssembly = null;
        this.weavedAssemblyStream?.Dispose();
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
