using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;

namespace Draco.Debugger.Tui;

internal sealed class DebuggerWindow : Window
{
    public TextView SourceText { get; set; }

    public DebuggerWindow()
    {
        this.Title = "Draco debugger (Ctrl+Q to quit)";

        this.SourceText = new TextView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Text = string.Empty,
        };
        this.Add(this.SourceText);
    }
}
