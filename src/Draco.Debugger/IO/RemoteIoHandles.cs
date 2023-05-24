using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Debugger.IO;

/// <summary>
/// A triplet of remote STDIO pipes that can be used to read from output handles and write to the input handle
/// of another process.
/// </summary>
/// <param name="StandardInputWriter">The input writer for the remote STDIN.</param>
/// <param name="StandardOutputReader">The output reader for the remote STDOUT.</param>
/// <param name="StandardErrorReader">The output reader for the remote STDERR.</param>
internal readonly record struct RemoteIoHandles(
    PipeStream StandardInputWriter,
    PipeStream StandardOutputReader,
    PipeStream StandardErrorReader);
