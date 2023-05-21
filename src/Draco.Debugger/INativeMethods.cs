using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Debugger;

public interface INativeMethods
{
    public nint LoadLibrary(string path);
}
