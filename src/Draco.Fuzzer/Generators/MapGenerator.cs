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
internal sealed class MapGenerator<TOld, TNew> : IGenerator<TNew>
{
    private readonly IGenerator<TOld> underlying;
    private readonly Func<TOld, TNew> map;
    private readonly Func<TNew, string> toString;

    public MapGenerator(IGenerator<TOld> underlying, Func<TOld, TNew> map, Func<TNew, string> toString)
    {
        this.underlying = underlying;
        this.map = map;
        this.toString = toString;
    }

    public TNew NextEpoch() => this.map(this.underlying.NextEpoch());
    public TNew NextMutation() => this.map(this.underlying.NextMutation());
    public string ToString(TNew value) => this.toString(value);
}
