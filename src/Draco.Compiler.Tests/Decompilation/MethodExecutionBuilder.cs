namespace Draco.Compiler.Tests.Decompilation;

internal sealed class MethodExecutionBuilder
{
    public object?[] Arguments { get; private set; } = Array.Empty<object?>();

    public object? Instance { get; private set; }

    public Action<object?>? CheckReturnAction { get; private set; }

    public Action<string>? CheckStdOutActon { get; private set; }

    public MethodExecutionBuilder Return(Action<object?> action)
    {
        CheckReturnAction = action;
        return this;
    }

    public MethodExecutionBuilder StdOut(Action<string> action)
    {
        CheckStdOutActon = action;
        return this;
    }

    public MethodExecutionBuilder WithArguments(params object?[] arguments)
    {
        Arguments = arguments;
        return this;
    }

    public MethodExecutionBuilder WithInstance(object instance)
    {
        Instance = instance;
        return this;
    }
}
