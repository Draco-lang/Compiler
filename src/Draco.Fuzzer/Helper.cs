namespace Draco.Fuzzer;

internal static class Helper
{
    public static void PrintError(Exception ex, string input)
    {
        Console.WriteLine(input);
        Console.WriteLine();
        Console.WriteLine(ex.Message);
        Console.WriteLine();
        Console.WriteLine(ex.StackTrace);
        var color = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(new string('-', 80));
        Console.ForegroundColor = color;
    }
}
