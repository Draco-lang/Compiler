using System;
using System.Threading.Tasks;

namespace Draco.Query.Sample;

internal class Program
{
    private static async QueryValueTask<int> ParseNum(string n)
    {
        Console.WriteLine($"ParseNum({n})");
        return int.Parse(n);
    }

    private static async QueryValueTask<int> AddNums(string v1, string v2)
    {
        Console.WriteLine($"AddNums({v1}, {v2})");
        var n1 = await ParseNum(v1);
        var n2 = await ParseNum(v2);
        return n1 + n2;
    }

    internal static async Task Main(string[] args)
    {
        var res1 = await AddNums("12", "23");
        var res2 = await AddNums("12", "23");
        var res3 = await AddNums("23", "34");
        var res4 = await ParseNum("23");
        Console.WriteLine(res1);
        Console.WriteLine(res2);
        Console.WriteLine(res3);
    }
}
