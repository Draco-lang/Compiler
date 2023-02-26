namespace Draco.Fuzzer.Testing;

internal abstract class ComponentFuzzer
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
    public void StartTesting(int numEpochs, int numMutations)
    {
        // If number of epochs is -1 we run forever
        for (var i = 0; i < numEpochs || numEpochs == -1; i++)
        {
            this.RunEpoch();
            for (var j = 0; j < numMutations; j++)
            {
                this.RunMutation();
            }
        }
        this.PrintResult();
    }

    public abstract void RunEpoch();

    public abstract void RunMutation();
}
