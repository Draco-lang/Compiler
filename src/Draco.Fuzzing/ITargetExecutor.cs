namespace Draco.Fuzzing;

/// <summary>
/// Executes the fuzzed target.
/// </summary>
public interface ITargetExecutor
{
    /// <summary>
    /// Called once at the start of the fuzzing process.
    /// </summary>
    public void GlobalInitializer();

    /// <summary>
    /// Initializes the target for execution.
    /// Called before each execution.
    /// </summary>
    /// <returns>The target information.</returns>
    public TargetInfo Initialize();

    /// <summary>
    /// Executes the target.
    /// </summary>
    /// <param name="targetInfo">The target information.</param>
    public void Execute(TargetInfo targetInfo);
}
