using System;
using System.IO;
using System.Reflection;

namespace Draco.Coverage;

/// <summary>
/// Represents an instrumented assembly, that weaves in instrumentation code.
/// </summary>
public sealed class InstrumentedAssembly
{
    /// <summary>
    /// The environment variable set to collect to an existing shared memory.
    /// </summary>
    public const string SharedMemoryEnvironmentVariable = "DRACO_COLLECT_TO_SHARED_MEMORY";

    /// <summary>
    /// Creates a shared memory key from the given identifier.
    /// </summary>
    /// <param name="identifier">The identifier to create the key from.</param>
    /// <returns>A key that can be used to reference the shared memory.</returns>
    public static string SharedMemoryKey(string identifier) => $"Draco.Coverage.SharedMemory.{identifier}";

    /// <summary>
    /// Weaves instrumentation code into the given assembly.
    /// </summary>
    /// <param name="sourcePath">The path to the assembly to weave.</param>
    /// <param name="targetPath">The path to write the weaved assembly to.</param>
    /// <param name="settings">The settings for the weaver.</param>
    public static void Weave(string sourcePath, string targetPath, InstrumentationWeaverSettings? settings = null) =>
        InstrumentationWeaver.WeaveInstrumentationCode(sourcePath, targetPath, settings);

    /// <summary>
    /// Weaves instrumentation code into the given assembly.
    /// </summary>
    /// <param name="sourceStream">The stream containing the assembly to weave.</param>
    /// <param name="targetStream">The stream to write the weaved assembly to.</param>
    /// <param name="settings">The settings for the weaver.</param>
    public static void Weave(Stream sourceStream, Stream targetStream, InstrumentationWeaverSettings? settings = null) =>
        InstrumentationWeaver.WeaveInstrumentationCode(sourceStream, targetStream, settings);

    /// <summary>
    /// Creates an instrumented assembly from a stream containing an unweaved assembly.
    /// </summary>
    /// <param name="sourceStream">The stream containing the unweaved assembly.</param>
    /// <param name="settings">The settings for the weaver.</param>
    /// <returns>The instrumented assembly.</returns>
    public static InstrumentedAssembly Create(Stream sourceStream, InstrumentationWeaverSettings? settings = null)
    {
        var targetStream = new MemoryStream();
        Weave(sourceStream, targetStream, settings);
        var weavedAssembly = Assembly.Load(targetStream.ToArray());
        return new(weavedAssembly);
    }

    /// <summary>
    /// Creates an instrumented assembly from an already weaved assembly.
    /// </summary>
    /// <param name="assembly">The weaved assembly.</param>
    /// <returns>The instrumented assembly.</returns>
    public static InstrumentedAssembly FromWeavedAssembly(Assembly assembly) => new(assembly);

    /// <summary>
    /// The weaved assembly.
    /// </summary>
    public Assembly WeavedAssembly { get; }

    /// <summary>
    /// The sequence points of the weaved assembly.
    /// </summary>
    public SequencePoint[] SequencePoints =>
        this.sequencePoints ??= NotNullOrNotWeaved((SequencePoint[]?)this.SequencePointsField.GetValue(null));
    private SequencePoint[]? sequencePoints;

    /// <summary>
    /// Retrieves a copy of the coverage result.
    /// </summary>
    public CoverageResult CoverageResult => CoverageResult.FromSharedMemory(this.HitsInstance);

    /// <summary>
    /// The coverage collector type weaved into the assembly.
    /// </summary>
    internal Type CoverageCollectorType =>
        this.coverageCollectorType ??= NotNullOrNotWeaved(this.WeavedAssembly.GetType(typeof(CoverageCollector).FullName!));
    private Type? coverageCollectorType;

    /// <summary>
    /// The hits field of the coverage collector.
    /// </summary>
    internal FieldInfo HitsField =>
        this.hitsField ??= NotNullOrNotWeaved(this.CoverageCollectorType.GetField(nameof(CoverageCollector.Hits)));
    private FieldInfo? hitsField;

    /// <summary>
    /// The sequence points field of the coverage collector.
    /// </summary>
    internal FieldInfo SequencePointsField =>
        this.sequencePointsField ??= NotNullOrNotWeaved(this.CoverageCollectorType.GetField(nameof(CoverageCollector.SequencePoints)));
    private FieldInfo? sequencePointsField;

    /// <summary>
    /// The hits instance of the coverage collector.
    /// </summary>
    internal SharedMemory<int> HitsInstance =>
        this.hitsInstance ??= NotNullOrNotWeaved((SharedMemory<int>?)this.HitsField.GetValue(null));
    private SharedMemory<int>? hitsInstance;

    private InstrumentedAssembly(Assembly weavedAssembly)
    {
        CheckForWeaved(weavedAssembly);
        this.WeavedAssembly = weavedAssembly;
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
        return this.CoverageResult;
    }

    /// <summary>
    /// Clears the coverage data of the instrumented assembly.
    /// </summary>
    public void ClearCoverageData()
    {
        var coverageCollectorType = NotNullOrNotWeaved(this.WeavedAssembly.GetType(typeof(CoverageCollector).FullName!));
        var clearMethod = NotNullOrNotWeaved(coverageCollectorType.GetMethod("Clear"));

        clearMethod.Invoke(null, null);
    }

    private static void CheckForWeaved(Assembly assembly) =>
        NotNullOrNotWeaved(assembly.GetType(typeof(CoverageCollector).FullName!));

    private static T NotNullOrNotWeaved<T>(T? value)
        where T : class =>
        value ?? throw new InvalidOperationException("the assembly was not weaved");
}
