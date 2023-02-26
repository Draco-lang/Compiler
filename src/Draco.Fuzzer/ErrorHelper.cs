using System.Diagnostics;
using System.Text;

namespace Draco.Fuzzer;

internal class ErrorHelper
{
    private List<(string input, Exception ex)> errors = new();

    public void AddError(Exception ex, string input) => this.errors.Add((input, ex));

    public void PrintResult()
    {
        var color = Console.ForegroundColor;
        var errorCount = this.errors.GroupBy(x => x.ex.StackTrace);
        foreach (var error in errorCount)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"{error.Count()} error/s : ");
            Console.ForegroundColor = color;
            Console.WriteLine();
            Console.WriteLine(error.MinBy(x => x.input.Length).input);
            Console.WriteLine();
            Console.WriteLine(error.Key);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(new string('-', Console.WindowWidth));
            Console.ForegroundColor = color;
        }
    }
}
