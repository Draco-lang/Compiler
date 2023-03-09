using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Types;

/// <summary>
/// Types known by the compiler.
/// </summary>
internal static class Intrinsics
{
    public static Type Never { get; } = NeverType.Instance;
    public static Type Unit { get; } = new BuiltinType(typeof(void), "unit");
    public static Type Int32 { get; } = new BuiltinType(typeof(int), "int32");
    public static Type Bool { get; } = new BuiltinType(typeof(bool), "bool");
}
