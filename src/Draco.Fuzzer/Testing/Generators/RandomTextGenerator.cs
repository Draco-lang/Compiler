namespace Draco.Fuzzer.Testing.Generators;

internal sealed class RandomTextGenerator : IInputGenerator<string>
{
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
        var chars = new char[length];
        return new string(chars.Select(x => x = (char)this.random.Next(256)).ToArray());
    }

    public string NextMutation() => throw new NotImplementedException();
}
