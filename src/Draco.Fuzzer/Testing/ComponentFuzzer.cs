using Draco.Fuzzer.Testing.Generators;

namespace Draco.Fuzzer.Testing;

internal abstract class ComponentFuzzer<T>
{
    private readonly List<(string input, Exception ex)> errors = new();
    private const string path = "lastinputlog.txt";
    private bool shouldStop = false;
    protected readonly IInputGenerator<T> generator;

    public ComponentFuzzer(IInputGenerator<T> generator)
    {
        this.generator = generator;
        Console.CancelKeyPress += new ConsoleCancelEventHandler(this.HandleCancel);
        if (File.Exists(path)) File.Delete(path);
        Console.WriteLine($"in case of stack overflow look at this log for the last input: {Path.GetFullPath(path)}");
    }

    private void AddError(Exception ex, string input) => this.errors.Add((input, ex));

    // Note: We are writing the input to the log from the start, because if we get stack overflow, we wouldn't be able to get to the input
    private void AddInput(string input) => File.WriteAllText(path, input);

    private void PrintResult()
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

    private void HandleCancel(object? sender, ConsoleCancelEventArgs args)
    {
        this.shouldStop = true;
        args.Cancel = true;
    }

    public void StartTesting(int numEpochs, int numMutations)
    {
        // If number of epochs is -1 we run forever
        for (var i = 0; (i < numEpochs || numEpochs == -1) && !this.shouldStop; i++)
        {
            var input = this.generator.NextExpoch()!;
            this.AddInput(input.ToString()!);
            try
            {
                this.RunEpoch(input);
            }
            catch (Exception ex)
            {
                this.AddError(ex, input.ToString()!);
            }
            for (var j = 0; j < numMutations; j++)
            {
                this.RunMutation();
            }
        }
        this.PrintResult();
    }

    public abstract void RunEpoch(T input);

    public abstract void RunMutation();
}
