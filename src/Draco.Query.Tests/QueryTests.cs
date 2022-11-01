using Draco.Query.Tasks;

namespace Draco.Query.Tests;

public class QueryTests
{
    [Fact]
    public async Task DependencyTest()
    {
        var res = await this.QueryAB();
        Console.WriteLine(res);
        Assert.Equal("AB", res);
    }
    public async QueryValueTask<string> QueryAB()
    {
        var a = await this.QueryA();
        var b = await this.QueryB();
        return a + b;
    }

    public async QueryValueTask<string> QueryA() => "A";

    public async QueryValueTask<string> QueryB() => "B";
}
