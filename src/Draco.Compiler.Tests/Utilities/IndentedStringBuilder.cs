using System.Text;

namespace Draco.Compiler.Tests.Utilities;

internal sealed class IndentedStringBuilder
{
    private int _indent;
    private int _indentSize = 4;
    private bool _doIndent = false;
    private readonly StringBuilder _sb;

    public IndentedStringBuilder(StringBuilder sb)
    {
        _sb = sb;
    }

    private void WriteIndent()
    {
        if (_doIndent)
        {
            Span<char> span = stackalloc char[_indent * _indentSize];
            span.Fill(' ');
            _sb.Append(span);
        }
    }

    public void Append(string text)
    {
        WriteIndent();
        _sb.Append(text);
        _doIndent = false;
    }

    public void Append(object o)
    {
        WriteIndent();
        _sb.Append(o);
        _doIndent = false;
    }

    public void Append(int i)
    {
        WriteIndent();
        _sb.Append(i);
        _doIndent = false;
    }

    public void Append(char c)
    {
        WriteIndent();
        _sb.Append(c);
        _doIndent = false;
    }

    public void AppendLine(string text)
    {
        WriteIndent();
        _sb.AppendLine(text);
        _doIndent = true;
    }

    public void AppendLine()
    {
        WriteIndent();
        _sb.AppendLine();
        _doIndent = true;
    }

    public void PushIndent() => _indent++;
    public void PopIndent() => _indent--;

    public Indenter WithIndent()
    {
        PushIndent();
        return new Indenter(this, true);
    }

    public Indenter WithDedent()
    {
        PopIndent();
        return new Indenter(this, false);
    }

    public readonly struct Indenter : IDisposable
    {
        private readonly IndentedStringBuilder _sb;
        private readonly bool _doPop;

        public Indenter(IndentedStringBuilder sb, bool doPop)
        {
            _sb = sb;
            _doPop = doPop;
        }

        public void Dispose()
        {
            if (_doPop)
                _sb.PopIndent();
            else
                _sb.PushIndent();
        }
    }
}
