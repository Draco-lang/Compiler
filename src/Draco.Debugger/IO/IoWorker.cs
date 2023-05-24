using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Draco.Debugger.IO;

/// <summary>
/// Represents a worker that handles IO for a remote process.
/// </summary>
/// <typeparam name="TProcess">The process type.</typeparam>
internal sealed class IoWorker<TProcess>
{
    private const int BufferSize = 4096;

    /// <summary>
    /// Fires, when the standard output of the process receives text.
    /// </summary>
    public EventHandler<string>? OnStandardOut;

    /// <summary>
    /// Fires, when the standard error of the process receives text.
    /// </summary>
    public EventHandler<string>? OnStandardError;

    /// <summary>
    /// The standard input writer of the process.
    /// </summary>
    public StreamWriter StandardInput { get; }

    private readonly TProcess process;
    private readonly RemoteIoHandles handles;

    public IoWorker(TProcess process, RemoteIoHandles handles)
    {
        this.handles = handles;
        this.StandardInput = new StreamWriter(handles.StandardInputWriter);
    }

    /// <summary>
    /// Runs the worker loop.
    /// </summary>
    /// <param name="cancellationToken">Can be used to cancel the worker.</param>
    /// <returns>The task of the worker loop.</returns>
    public Task Run(CancellationToken cancellationToken) => Task.Run(async () =>
    {
        var stdoutReader = new StreamReader(this.handles.StandardOutputReader);
        var stderrReader = new StreamReader(this.handles.StandardErrorReader);

        var stdoutBuffer = new char[BufferSize];
        var stderrBuffer = new char[BufferSize];

        while (!cancellationToken.IsCancellationRequested)
        {
            var stdoutTask = stdoutReader.ReadAsync(stdoutBuffer, cancellationToken).AsTask();
            var stderrTask = stderrReader.ReadAsync(stderrBuffer, cancellationToken).AsTask();

            await Task.WhenAny(stdoutTask, stderrTask);

            if (stdoutTask.IsCompleted)
            {
                var str = new string(stdoutBuffer, 0, stdoutTask.Result);
                this.OnStandardOut?.Invoke(this.process, str);
            }
            if (stderrTask.IsCompleted)
            {
                var str = new string(stderrBuffer, 0, stderrTask.Result);
                this.OnStandardError?.Invoke(this.process, str);
            }
        }
    }, cancellationToken);
}
