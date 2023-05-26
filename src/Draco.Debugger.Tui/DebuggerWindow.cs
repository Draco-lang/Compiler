using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;

namespace Draco.Debugger.Tui;

internal sealed class DebuggerWindow : Window
{
    private readonly FrameView sourceTextFrame;
    private readonly SourceTextView sourceText;

    private readonly ListView sourceBrowserList;

    private readonly TextView stdinText;
    private readonly TextView stdoutText;
    private readonly TextView stderrText;

    private readonly ListView callStackList;
    private readonly TableView localsTable;

    public DebuggerWindow()
    {
        this.Y = 1; // menu
        this.Height = Dim.Fill(1); // status bar

        this.Border.BorderStyle = BorderStyle.None;
        this.Border.DrawMarginFrame = false;

        var menu = new MenuBar(new[]
        {
            new MenuBarItem("_File", new[]
            {
                new MenuItem("_TODO", "", () => { }),
            }),
        });

        this.sourceText = new SourceTextView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
        };
        this.sourceTextFrame = MakeFrameView(string.Empty, this.sourceText);
        this.sourceTextFrame.Height = Dim.Percent(65);
        this.sourceTextFrame.Width = Dim.Percent(75);

        this.sourceBrowserList = MakeListView();
        var sourceBrowserFrame = MakeFrameView("sources", this.sourceBrowserList);
        sourceBrowserFrame.X = Pos.Right(this.sourceTextFrame);
        sourceBrowserFrame.Height = Dim.Height(this.sourceTextFrame);
        this.sourceBrowserList.SelectedItemChanged += this.OnSelectedSourceFileChanged;

        this.stdinText = MakeTextView(readOnly: false);
        this.stdoutText = MakeTextView(readOnly: true);
        this.stderrText = MakeTextView(readOnly: true);

        var stdioTab = MakeTabView(
            new("stdout", this.stdoutText),
            new("stdin", this.stdinText),
            new("stderr", this.stderrText));
        stdioTab.Y = Pos.Bottom(this.sourceTextFrame);
        stdioTab.Height = Dim.Fill();
        stdioTab.Width = Dim.Percent(50);

        this.localsTable = MakeTableView();
        this.callStackList = MakeListView();
        var localsTab = MakeTabView(
            new("locals", this.localsTable),
            new("call-stack", this.callStackList));
        localsTab.Height = Dim.Height(stdioTab);
        localsTab.Y = Pos.Top(stdioTab);
        localsTab.X = Pos.Right(stdioTab);

        var statusBar = new StatusBar(new[]
        {
            new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", () => Application.RequestStop()),
        });

        this.Add(
            menu,
            this.sourceTextFrame,
            sourceBrowserFrame,
            stdioTab,
            localsTab,
            statusBar);
        Application.Top.Add(menu, statusBar, this);
    }

    public void AppendStdout(string text) => AppendText(this.stdoutText, text);
    public void AppendStderr(string text) => AppendText(this.stderrText, text);
    public void SetCallStack(IReadOnlyList<string> elements) => this.callStackList.SetSource(elements.ToList());
    public void SetSourceFile(SourceFile sourceFile, SourceRange? rangeToHighlight)
    {
        if (this.sourceBrowserList.Source is SourceFileListDataSource ds)
        {
            this.sourceBrowserList.SelectedItem = ds.IndexOf(sourceFile);
        }
        this.sourceText.SetHighlightedRange(rangeToHighlight);
    }
    public void SetSourceFileList(IReadOnlyList<SourceFile> sourceFiles) =>
        this.sourceBrowserList.Source = new SourceFileListDataSource(sourceFiles);

    private void OnSelectedSourceFileChanged(ListViewItemEventArgs args)
    {
        var sourceFile = (SourceFile)args.Value;
        this.sourceTextFrame.Title = sourceFile.Uri.LocalPath;
        this.sourceText.Text = sourceFile.Text;
    }

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

    private static FrameView MakeFrameView(string title, View subview)
    {
        var frameView = new FrameView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Title = title,
        };
        frameView.Add(subview);
        return frameView;
    }
}
