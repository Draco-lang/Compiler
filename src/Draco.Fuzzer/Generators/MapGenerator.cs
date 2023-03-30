using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Fuzzer.Generators;

/// <summary>
/// Maps the generated input to another type.
/// </summary>
/// <typeparam name="TOld">The type to map from.</typeparam>
/// <typeparam name="TNew">The type to map to.</typeparam>
internal sealed class MapGenerator<TOld, TNew> : IInputGenerator<TNew>
{
    private readonly IInputGenerator<TOld> underlying;
    private readonly Func<TOld, TNew> map;

    public MapGenerator(IInputGenerator<TOld> underlying, Func<TOld, TNew> map)
    {
        this.underlying = underlying;
        this.map = map;
    }

    public TNew NextExpoch() => this.map(this.underlying.NextExpoch());
    public TNew NextMutation() => this.map(this.underlying.NextMutation());
}
