namespace Draco.Fuzzer.Testing.Generators;

internal sealed class RandomTextGenerator : IInputGenerator<string>
{
    private readonly Random random;
    private readonly int maxLength;
    private readonly string charset;

    public RandomTextGenerator(int maxLength = 5000, string charset = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~\r\n\t")
    {
        this.random = new Random();
        this.maxLength = maxLength;
        this.charset = charset;
    }

    public RandomTextGenerator(int seed, int maxLength = 5000, string charset = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~\r\n\t")
    {
        this.random = new Random(seed);
        this.maxLength = maxLength;
        this.charset = charset;
    }

    public string NextExpoch()
    {
        int length = this.random.Next(this.maxLength);
        var chars = new char[length];
        return new string(chars.Select(x => x = this.charset[this.random.Next(this.charset.Length)]).ToArray());
    }

    public string NextMutation() => throw new NotImplementedException();
}
