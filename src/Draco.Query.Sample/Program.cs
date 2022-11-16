using System;
using System.Threading.Tasks;
using Draco.Query.Tasks;

namespace Draco.Query.Sample;

internal class Program
{
    private static async QueryValueTask<int> ParseVariable(QueryDatabase db, string n)
    {
        var nStr = await db.GetInput<string>(n);
        Console.WriteLine($"ParseVariable({n} = '{nStr}')");
        return int.Parse(nStr);
    }

    private static async QueryValueTask<int> AddVariables(QueryDatabase db, string v1, string v2)
    {
        Console.WriteLine($"AddVariables({v1}, {v2})");
        var n1 = await ParseVariable(db, v1);
        var n2 = await ParseVariable(db, v2);
        return n1 + n2;
    }

    internal static async Task Main(string[] args)
    {
        var db = new QueryDatabase();

        db.SetInput("x", "1");
        db.SetInput("y", "2");

        var res1 = await AddVariables(db, "x", "y");
        Console.WriteLine(res1);

        db.SetInput("y", "2 ");

        var res2 = await AddVariables(db, "x", "y");
        Console.WriteLine(res2);
    }
}
