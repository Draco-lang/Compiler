namespace Draco.Compiler.Tests.Utilities;

internal static class Disposable
{
    public static IDisposable Empty { get; } = new EmptyDisposable();

    public static IDisposable Create(Action action) => new ActionDisposable(action);

    private sealed class EmptyDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    private class ActionDisposable : IDisposable
    {
        private Action? _action;

        public ActionDisposable(Action action)
        {
            _action = action;
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref _action, null)?.Invoke();
        }
    }
}
