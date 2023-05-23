using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;

namespace Draco.Debugger.Tui;

internal sealed class DebuggerWindow : Window
{
    public DebuggerWindow()
    {
        this.Title = "Draco debugger (Ctrl+Q to quit)";
    }
}
