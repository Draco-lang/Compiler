using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.JsonRpc;

/// <summary>
/// The interface all JSON-RPC clients must implement.
/// </summary>
public interface IJsonRpcClient
{
    /// <summary>
    /// The connection.
    /// </summary>
    public IJsonRpcConnection Connection { get; }
}
