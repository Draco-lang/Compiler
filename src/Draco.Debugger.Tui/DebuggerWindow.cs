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
    public TextView StdoutText { get; set; }

    public DebuggerWindow()
    {
        this.Title = "Draco debugger (Ctrl+Q to quit)";

        this.SourceText = new TextView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Percent(70),
            Text = string.Empty,
            CanFocus = false,
        };

        this.StdoutText = new TextView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Percent(50),
            Height = Dim.Fill(),
            Text = string.Empty,
            ReadOnly = true,
            AutoSize = true,
        };
        var stdoutWindow = new Window()
        {
            Title = "stdout",
            X = 0,
            Y = Pos.Bottom(this.SourceText),
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };
        stdoutWindow.Add(this.StdoutText);

        this.Add(this.SourceText, stdoutWindow);
    }
}
