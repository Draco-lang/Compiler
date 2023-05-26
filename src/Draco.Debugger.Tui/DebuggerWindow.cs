using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;

namespace Draco.Debugger.Tui;

internal sealed class DebuggerWindow : Window
{
    private readonly TextView stdinText;
    private readonly TextView stdoutText;
    private readonly TextView stderrText;

    private readonly ListView callStackList;
    private readonly TableView localsTable;

    public DebuggerWindow()
    {
        this.Title = "Draco debugger (Ctrl+Q to quit)";

        this.stdinText = MakeTextView(readOnly: false);
        this.stdoutText = MakeTextView(readOnly: true);
        this.stderrText = MakeTextView(readOnly: true);

        var stdioTab = MakeTabView(
            new("stdout", this.stdoutText),
            new("stdin", this.stdinText),
            new("stderr", this.stderrText));
        stdioTab.Height = Dim.Percent(40);
        stdioTab.Width = Dim.Percent(50);

        this.localsTable = MakeTableView();
        this.callStackList = MakeListView();
        var localsTab = MakeTabView(
            new("locals", this.localsTable),
            new("call-stack", this.callStackList));
        localsTab.Height = Dim.Height(stdioTab);
        localsTab.Y = Pos.Top(stdioTab);
        localsTab.X = Pos.Right(stdioTab);

        this.Add(stdioTab, localsTab);
    }

    public void AppendStdout(string text) => AppendText(this.stdoutText, text);
    public void AppendStderr(string text) => AppendText(this.stderrText, text);
    public void SetCallStack(IReadOnlyList<string> elements) => this.callStackList.SetSource(elements.ToList());

    private static void AppendText(TextView textView, string text)
    {
        textView.Text += text;
        textView.MoveEnd();
    }

    private static TabView MakeTabView(params KeyValuePair<string, View>[] views)
    {
        var tabView = new TabView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };
        foreach (var (name, tab) in views) tabView.AddTab(new TabView.Tab(name, tab), false);
        return tabView;
    }

    private static ListView MakeListView() => new ListView()
    {
        X = 0,
        Y = 0,
        Width = Dim.Fill(),
        Height = Dim.Fill(),
    };

    private static TableView MakeTableView() => new TableView()
    {
        X = 0,
        Y = 0,
        Width = Dim.Fill(),
        Height = Dim.Fill(),
    };

    private static TextView MakeTextView(bool readOnly) => new TextView()
    {
        X = 0,
        Y = 0,
        Width = Dim.Fill(),
        Height = Dim.Fill(),
        ReadOnly = readOnly,
    };
}
