using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace Draco.Debugger.Tui;

internal sealed class DebuggerWindow : Window
{
    public event EventHandler? OnStepOver;
    public event EventHandler? OnStepInto;
    public event EventHandler? OnStepOut;

    private readonly FrameView sourceTextFrame;
    private readonly SourceTextView sourceText;

    private readonly ListView sourceBrowserList;
    private readonly FrameView sourceBrowserFrame;

    private readonly TextView stdinText;
    private readonly TextView stdoutText;
    private readonly TextView stderrText;

    private readonly ListView callStackList;
    private readonly TableView localsTable;
    private readonly TextView logText;

    private readonly MenuBar menu;
    private readonly StatusBar statusBar;

    private readonly Dictionary<string, ColorScheme> themes = new();

    public DebuggerWindow()
    {
        this.themes["Light"] = Colors.Base;
        this.themes["Dark"] = new ColorScheme
        {
            Disabled = Attribute.Make(Color.BrightYellow, Color.DarkGray),
            Normal = Attribute.Make(Color.BrightYellow, Color.DarkGray),
            HotNormal = Attribute.Make(Color.Black, Color.BrightYellow),
            Focus = Attribute.Make(Color.White, Color.Black),
            HotFocus = Attribute.Make(Color.Black, Color.White),
        };
        this.themes["Contrast"] = new ColorScheme
        {
            Disabled = Attribute.Make(Color.White, Color.Black),
            Normal = Attribute.Make(Color.Brown, Color.Black),
            HotNormal = Attribute.Make(Color.White, Color.Black),
            Focus = Attribute.Make(Color.White, Color.Black),
            HotFocus = Attribute.Make(Color.White, Color.Black),
        };
        this.themes["Neon Cyan"] = new ColorScheme
        {
            Disabled = Attribute.Make(Color.White, Color.Black),
            Normal = Attribute.Make(Color.Cyan, Color.Black),
            HotNormal = Attribute.Make(Color.White, Color.Black),
            Focus = Attribute.Make(Color.White, Color.Black),
            HotFocus = Attribute.Make(Color.White, Color.Black),
        };
        this.themes["Neon Green"] = new ColorScheme
        {
            Disabled = Attribute.Make(Color.White, Color.Black),
            Normal = Attribute.Make(Color.BrightGreen, Color.Black),
            HotNormal = Attribute.Make(Color.White, Color.Black),
            Focus = Attribute.Make(Color.White, Color.Black),
            HotFocus = Attribute.Make(Color.White, Color.Black),
        };
        this.themes["Neon Magenta"] = new ColorScheme
        {
            Disabled = Attribute.Make(Color.White, Color.Black),
            Normal = Attribute.Make(Color.BrightMagenta, Color.Black),
            HotNormal = Attribute.Make(Color.White, Color.Black),
            Focus = Attribute.Make(Color.White, Color.Black),
            HotFocus = Attribute.Make(Color.White, Color.Black),
        };

        this.Y = 1; // menu
        this.Height = Dim.Fill(1); // status bar

        this.Border.BorderStyle = BorderStyle.None;
        this.Border.DrawMarginFrame = false;

        this.menu = new MenuBar(new[]
        {
            new MenuBarItem("File", new[]
            {
                new MenuItem("TODO", "", () => { }),
            }),

            new MenuBarItem("Theme", this.themes.Select(x => new MenuItem(x.Key, "", () => this.ChangeTheme(x.Value))).ToArray())
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
        this.sourceBrowserFrame = MakeFrameView("sources", this.sourceBrowserList);
        this.sourceBrowserFrame.X = Pos.Right(this.sourceTextFrame);
        this.sourceBrowserFrame.Height = Dim.Height(this.sourceTextFrame);
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
        this.logText = MakeTextView(readOnly: true);
        var localsTab = MakeTabView(
            new("locals", this.localsTable),
            new("call-stack", this.callStackList),
            new("logs", this.logText));
        localsTab.Height = Dim.Height(stdioTab);
        localsTab.Y = Pos.Top(stdioTab);
        localsTab.X = Pos.Right(stdioTab);

        this.statusBar = new StatusBar(new[]
        {
            new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", () => Application.RequestStop()),
            new StatusItem(Key.F5, "~F5~ Step Over", () => this.OnStepOver?.Invoke(this, null!)),
            new StatusItem(Key.F6, "~F6~ Step Into", () => this.OnStepInto?.Invoke(this, null!)),
            new StatusItem(Key.F7, "~F7~ Step Out", () => this.OnStepOut?.Invoke(this, null !)),
        });

        // NOTE: We need this for the menu and the status bar
        this.ChangeTheme(this.themes["Light"]);

        this.Add(
            this.menu,
            this.sourceTextFrame,
            this.sourceBrowserFrame,
            stdioTab,
            localsTab,
            this.statusBar);
        Application.Top.Add(this.menu, this.statusBar, this);
    }

    public void AppendStdout(string text) => AppendText(this.stdoutText, text);
    public void AppendStderr(string text) => AppendText(this.stderrText, text);
    public void Log(string line) => AppendText(this.logText, $"{line}{Environment.NewLine}");
    public void SetCallStack(IReadOnlyList<string> elements) => this.callStackList.SetSource(elements.ToList());
    public void SetLocals(IReadOnlyList<string> elements)
    {
        this.localsTable.Table = new();
        this.localsTable.Table.Columns.Add("name");
        foreach (var name in elements) this.localsTable.Table.Rows.Add(name);
        this.localsTable.SetNeedsDisplay();
    }
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

    private void ChangeTheme(ColorScheme scheme)
    {
        this.ColorScheme = scheme;
        this.menu!.ColorScheme = scheme;
        this.statusBar!.ColorScheme = scheme;
        this.sourceTextFrame!.Border.Background = scheme.Normal.Background;
        this.sourceTextFrame!.Border.BorderBrush = scheme.Normal.Foreground;

        this.sourceBrowserFrame!.Border.Background = scheme.Normal.Background;
        this.sourceBrowserFrame!.Border.BorderBrush = scheme.Normal.Foreground;
    }
}
