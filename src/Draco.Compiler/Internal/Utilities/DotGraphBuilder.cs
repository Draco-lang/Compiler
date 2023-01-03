using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Draco.Compiler.Internal.Utilities;

/// <summary>
/// Builds DOT graphs with a safe API.
/// </summary>
/// <typeparam name="TVertex">The vertex type mapped.</typeparam>
internal sealed class DotGraphBuilder<TVertex>
{
    public sealed class VertexBuilder
    {
        private readonly VertexInfo info;

        internal VertexBuilder(DotGraphBuilder<TVertex>.VertexInfo info)
        {
            this.info = info;
        }

        public VertexBuilder WithAttribute(string name, object? value)
        {
            if (value is not null) this.info.Attributes[name] = value;
            return this;
        }

        public VertexBuilder WithLabel(string? value) => this.WithAttribute("label", value);
        public VertexBuilder WithXLabel(string? value) => this.WithAttribute("xlabel", value);
    }

    public sealed class EdgeBuilder
    {
        private readonly EdgeInfo info;

        internal EdgeBuilder(DotGraphBuilder<TVertex>.EdgeInfo info)
        {
            this.info = info;
        }

        public EdgeBuilder WithAttribute(string name, object? value)
        {
            if (value is not null) this.info.Attributes[name] = value;
            return this;
        }

        public EdgeBuilder WithLabel(string? value) => this.WithAttribute("label", value);
    }

    private const string indentation = "  ";

    internal sealed record class VertexInfo(int Id, Dictionary<string, object> Attributes);
    internal sealed record class EdgeInfo(Dictionary<string, object> Attributes);
    private sealed record class HtmlText(string Html);

    private readonly Dictionary<string, object> attributes = new();
    private readonly VertexInfo allVertices = new(-1, new());
    private readonly EdgeInfo allEdges = new(new());
    private readonly Dictionary<TVertex, VertexInfo> vertices;
    private readonly Dictionary<(int From, int To), List<EdgeInfo>> edges = new();
    private readonly bool isDirected;
    private string name = "G";

    public DotGraphBuilder(bool isDirected, IEqualityComparer<TVertex>? vertexComparer = null)
    {
        vertexComparer ??= EqualityComparer<TVertex>.Default;
        this.isDirected = isDirected;
        this.vertices = new(vertexComparer);
    }

    private VertexInfo GetVertexInfo(TVertex vertex)
    {
        if (!this.vertices.TryGetValue(vertex, out var info))
        {
            info = new(this.vertices.Count, new());
            this.vertices.Add(vertex, info);
        }
        return info;
    }

    private List<EdgeInfo> GetEdgeInfos(TVertex from, TVertex to)
    {
        var fromId = this.GetVertexInfo(from).Id;
        var toId = this.GetVertexInfo(to).Id;
        if (!this.isDirected)
        {
            // Order the IDs to get a symmetric access
            if (toId < fromId) (fromId, toId) = (toId, fromId);
        }
        if (!this.edges.TryGetValue((fromId, toId), out var infos))
        {
            infos = new();
            this.edges.Add((fromId, toId), infos);
        }
        return infos;
    }

    public object Html(string html) => new HtmlText(html);

    public DotGraphBuilder<TVertex> WithName(string name)
    {
        this.name = name;
        return this;
    }

    public DotGraphBuilder<TVertex> WithAttribute(string name, object? value)
    {
        if (value is not null) this.attributes[name] = value;
        return this;
    }

    public VertexBuilder AllVertices() => new(this.allVertices);
    public EdgeBuilder AllEdges() => new(this.allEdges);

    public VertexBuilder AddVertex(TVertex vertex) => new(this.GetVertexInfo(vertex));
    public EdgeBuilder AddEdge(TVertex from, TVertex to)
    {
        var infos = this.GetEdgeInfos(from, to);
        var info = new EdgeInfo(new());
        infos.Add(info);
        return new(info);
    }
    public IEnumerable<EdgeBuilder> GetEdges(TVertex from, TVertex to) => this
        .GetEdgeInfos(from, to)
        .Select(info => new EdgeBuilder(info));
    public EdgeBuilder GetEdge(TVertex from, TVertex to) => this.GetEdges(from, to).First();
    public EdgeBuilder GetOrAddEdge(TVertex from, TVertex to)
    {
        var edges = this.GetEdges(from, to);
        var first = edges.FirstOrDefault();
        if (first is null) return this.AddEdge(from, to);
        return first;
    }

    public string ToDot()
    {
        var stream = new MemoryStream();
        this.WriteTo(stream);
        stream.Position = 0;
        return new StreamReader(stream).ReadToEnd();
    }

    public void WriteTo(Stream stream) => this.WriteTo(new StreamWriter(stream));

    public void WriteTo(StreamWriter writer)
    {
        // Header
        writer.Write(this.isDirected ? "digraph" : "graph");
        writer.Write(' ');
        writer.Write(this.name);
        writer.WriteLine(" {");
        // Graph-level attributes
        WriteNamedAttributeList(writer, "graph", this.attributes);
        // Vertex-level attributes
        WriteNamedAttributeList(writer, "node", this.allVertices.Attributes);
        // Edge-level attributes
        WriteNamedAttributeList(writer, "edge", this.allEdges.Attributes);
        // Vertices
        foreach (var vertexInfo in this.vertices.Values)
        {
            writer.Write(indentation);
            writer.Write(vertexInfo.Id);
            WriteInlineAttributeList(writer, vertexInfo.Attributes);
            writer.WriteLine(';');
        }
        // Edges
        foreach (var ((fromId, toId), edgeInfos) in this.edges)
        {
            foreach (var edgeInfo in edgeInfos)
            {
                writer.Write(indentation);
                writer.Write(fromId);
                writer.Write(this.isDirected ? " -> " : " -- ");
                writer.Write(toId);
                WriteInlineAttributeList(writer, edgeInfo.Attributes);
                writer.WriteLine(';');
            }
        }
        // Footer
        writer.Write('}');
        writer.Flush();
    }

    private static void WriteNamedAttributeList(StreamWriter writer, string listName, Dictionary<string, object> attributes)
    {
        if (attributes.Count == 0) return;
        writer.Write(indentation);
        writer.Write(listName);
        writer.WriteLine(" [");
        foreach (var (name, value) in attributes)
        {
            writer.Write(indentation);
            writer.Write(indentation);
            WriteAttribute(writer, name, value);
            writer.WriteLine(';');
        }
        writer.Write(indentation);
        writer.WriteLine("];");
    }

    private static void WriteInlineAttributeList(StreamWriter writer, Dictionary<string, object> attributes)
    {
        if (attributes.Count == 0) return;
        writer.Write(" [");
        var first = true;
        foreach (var (name, value) in attributes)
        {
            if (!first) writer.Write(", ");
            WriteAttribute(writer, name, value);
            first = false;
        }
        writer.Write(']');
    }

    private static void WriteAttribute(StreamWriter writer, string name, object value)
    {
        writer.Write(name);
        writer.Write('=');
        writer.Write(StringifyAttributeValue(value));
    }

    private static string StringifyAttributeValue(object value) => value switch
    {
        bool b => b ? "true" : "false",
        string s => $"\"{StringUtils.Unescape(s)}\"",
        HtmlText t => $"<{t.Html}>",
        _ => value.ToString(),
    };
}
