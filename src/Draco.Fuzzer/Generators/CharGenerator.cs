namespace Draco.Fuzzer.Generators;

/// <summary>
/// Generates a random character.
/// </summary>
internal sealed class CharGenerator : IInputGenerator<char>
{
    /// <summary>
    /// The charset to use.
    /// </summary>
    public string Charset { get; set; } = Charsets.Ascii;

    private readonly Random random = new();

    public char NextExpoch() => this.Charset[this.random.Next(this.Charset.Length)];
    public char NextMutation() => this.NextExpoch();
}
