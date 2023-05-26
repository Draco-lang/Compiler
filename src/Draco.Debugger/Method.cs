using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClrDebug;

namespace Draco.Debugger;

/// <summary>
/// Represents a method.
/// </summary>
internal sealed class Method
{
    /// <summary>
    /// The internal handle.
    /// </summary>
    internal CorDebugFunction CorDebugFunction { get; }

    /// <summary>
    /// The name of the method.
    /// </summary>
    public string Name => this.name ??= this.BuildName();
    private string? name;

    internal Method(CorDebugFunction corDebugFunction)
    {
        this.CorDebugFunction = corDebugFunction;
    }

    private string BuildName()
    {
        var import = this.CorDebugFunction.Module.GetMetaDataInterface().MetaDataImport;
        var methodProps = import.GetMethodProps(this.CorDebugFunction.Token);
        return methodProps.szMethod;
    }
}
