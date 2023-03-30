namespace Draco.Fuzzer.Generators;

internal sealed class RandomTextGenerator : IInputGenerator<string>
{
    private readonly Random random;
    private readonly int maxLength;
    private readonly string charset;
    private const string defaultCharset = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~\r\n\t";

    public RandomTextGenerator(int maxLength = 5000, string charset = defaultCharset)
    {
        this.random = new Random();
        this.maxLength = maxLength;
        this.charset = charset;
    }

    public RandomTextGenerator(int seed, int maxLength = 5000, string charset = defaultCharset) : this(maxLength, charset)
    {
        this.random = new Random(seed);
    }

    public string NextExpoch() => new(Enumerable.Range(0, this.random.Next(this.maxLength)).Select(x => this.charset[this.random.Next(this.charset.Length)]).ToArray());

    public string NextMutation() => throw new NotImplementedException();
}
