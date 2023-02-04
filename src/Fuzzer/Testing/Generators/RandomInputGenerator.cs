namespace Fuzzer.Testing.Generators;

internal class RandomInputGenerator : IInputGenerator
{
    private string? currentEpoch;
    private Random random;
    private int maxLength;

    public RandomInputGenerator(int maxLength = 5000)
    {
        this.random = new Random();
        this.maxLength = maxLength;
    }

    public RandomInputGenerator(int seed, int maxLength = 5000)
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
        return new string(chars.ToArray());
    }

    public string NextMutation()
    {
        if (this.currentEpoch is null) throw new InvalidOperationException();
        throw new NotImplementedException();
    }
}
