using System.Diagnostics;
using System.Text;

namespace Draco.Fuzzer;

internal static class Helper
{
    private static List<(string input, Exception ex)> errors = new();

    public static void PrintError(Exception ex, string input) => errors.Add((input, ex));

    public static void PrintResult()
    {
        var color = Console.ForegroundColor;
        var errorCount = errors.GroupBy(x => x.ex.StackTrace);
        foreach (var error in errorCount)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"{error.Count()} error/s : ");
            Console.ForegroundColor = color;
            Console.WriteLine();
            Console.WriteLine(error.First().input);
            Console.WriteLine();
            Console.WriteLine(error.Key);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(new string('-', Console.WindowWidth));
            Console.ForegroundColor = color;
        }
    }
}
