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

    /// <summary>
    /// The task of the worker loop. Is always completed when the loop is not running.
    /// </summary>
    public Task WorkLoopTask { get; private set; } = Task.CompletedTask;
    private readonly TProcess process;
    private readonly RemoteIoHandles handles;

    public IoWorker(TProcess process, RemoteIoHandles handles)
    {
        this.process = process;
        this.handles = handles;
        this.StandardInput = new StreamWriter(handles.StandardInputWriter) { AutoFlush = true };
    }

    /// <summary>
    /// Runs the worker loop.
    /// </summary>
    /// <param name="cancellationToken">Can be used to cancel the worker.</param>
    /// <returns>The task of the worker loop.</returns>
    public void Start() =>
        this.WorkLoopTask = Task.WhenAll(
            this.ReadStdout(),
            this.ReadStderr()
        );

    private async Task ReadStdout()
    {
        try
        {
            var stdoutReader = new StreamReader(this.handles.StandardOutputReader);
            var stdoutBuffer = new char[BufferSize];
            while (true)
            {
                var result = await stdoutReader.ReadAsync(stdoutBuffer);
                if (result == 0) break;
                var str = new string(stdoutBuffer, 0, result);
                this.OnStandardOut?.Invoke(this.process, str);
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task ReadStderr()
    {
        try
        {
            var stderrReader = new StreamReader(this.handles.StandardErrorReader);
            var stderrBuffer = new char[BufferSize];

            while (true)
            {
                var result = await stderrReader.ReadAsync(stderrBuffer);
                if (result == 0) break;
                var str = new string(stderrBuffer, 0, result);
                this.OnStandardError?.Invoke(this.process, str);
            }
        }
        catch (OperationCanceledException) { }
    }
}
