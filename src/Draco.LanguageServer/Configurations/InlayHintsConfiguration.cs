using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.LanguageServer.Configurations;

internal sealed class InlayHintsConfiguration
{
    public bool ParameterNames { get; set; }
    public bool VariableTypes { get; set; }
}
