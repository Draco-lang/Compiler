namespace Draco.Fuzzer.Testing.Generators;

internal sealed class RandomTextGenerator : IInputGenerator<string>
{
    private string? currentEpoch;
    private readonly Random random;
    private readonly int maxLength;

    public RandomTextGenerator(int maxLength = 5000)
    {
        this.random = new Random();
        this.maxLength = maxLength;
    }

    public RandomTextGenerator(int seed, int maxLength = 5000)
    {
        this.random = new Random(seed);
        this.maxLength = maxLength;
    }

    public string NextExpoch()
    {
        int length = this.random.Next(this.maxLength);
        var chars = new List<char>();

        for (int i = 0; i < length; i++)
        {
            chars.Add((char)this.random.Next(256));
        }
        this.currentEpoch = new string(chars.ToArray());
        return this.currentEpoch;
    }

    public string NextMutation()
    {
        if (this.currentEpoch is null) throw new InvalidOperationException();
        throw new NotImplementedException();
    }
}
