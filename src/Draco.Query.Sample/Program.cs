using System;
using System.Threading.Tasks;
using Draco.Query.Tasks;

namespace Draco.Query.Sample;

internal class Program
{
    private static QueryIdentifier xId;
    private static QueryIdentifier yId;

    private static async QueryValueTask<int> ParseVariable(string n)
    {
        var nStr = QueryDatabase.GetInput<string>(n == "x" ? xId : yId);
        Console.WriteLine($"ParseVariable({nStr})");
        return int.Parse(nStr);
    }

    private static async QueryValueTask<int> AddVariables(string v1, string v2)
    {
        Console.WriteLine($"AddVariables({v1}, {v2})");
        var n1 = await ParseVariable(v1);
        var n2 = await ParseVariable(v2);
        return n1 + n2;
    }

    internal static async Task Main(string[] args)
    {
        xId = QueryDatabase.CreateInput<string>();
        yId = QueryDatabase.CreateInput<string>();

        QueryDatabase.SetInput(xId, "1");
        QueryDatabase.SetInput(yId, "2");

        var res1 = await AddVariables("x", "y");
        Console.WriteLine(res1);

        QueryDatabase.SetInput(yId, "2 ");

        var res2 = await AddVariables("x", "y");
        Console.WriteLine(res2);
    }
}
