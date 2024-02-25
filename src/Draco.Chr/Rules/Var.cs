namespace Draco.Chr.Rules;

/// <summary>
/// A variable within a rule.
/// </summary>
public sealed class Var(string name)
{
    /// <summary>
    /// The name of the variable.
    /// </summary>
    public string Name { get; } = name;

    public override string ToString() => this.Name;
}
