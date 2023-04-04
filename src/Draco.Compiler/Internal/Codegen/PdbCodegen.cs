using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.OptimizingIr.Model;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Codegen;

/// <summary>
/// Generates PDB from IR.
/// </summary>
internal sealed class PdbCodegen : MetadataWriter
{
    private readonly record struct LocalScopeStart(
        LocalVariableHandle VariableList,
        int StartOffset);

    private readonly record struct LocalScope(
        LocalVariableHandle VariableList,
        int StartOffset,
        int Length);

    /// <summary>
    /// GUID for identifying a Draco document.
    /// </summary>
    public static readonly Guid DracoLanguageGuid = new("7ef7b804-0709-43bc-b1b5-998bb801477b");

    public Compilation Compilation => this.metadataCodegen.Compilation;
    public Guid PdbId { get; } = Guid.NewGuid();
    public uint PdbStamp => 123456;

    private readonly MetadataCodegen metadataCodegen;
    private readonly ImmutableArray<SequencePoint>.Builder sequencePoints = ImmutableArray.CreateBuilder<SequencePoint>();
    private readonly Stack<LocalScopeStart> scopeStartStack = new();
    private readonly ImmutableArray<LocalScope>.Builder localScopes = ImmutableArray.CreateBuilder<LocalScope>();

    public PdbCodegen(MetadataCodegen metadataCodegen)
    {
        this.metadataCodegen = metadataCodegen;
    }

    public DebugDirectoryBuilder EncodeDebugDirectory()
    {
        var debugDirectoryBuilder = new DebugDirectoryBuilder();
        var pdbPath = Path.Join(this.Compilation.OutputPath, this.Compilation.AssemblyName);
        pdbPath = Path.ChangeExtension(pdbPath, ".pdb");
        pdbPath = Path.GetFullPath(pdbPath);
        debugDirectoryBuilder.AddCodeViewEntry(
            pdbPath: pdbPath,
            pdbContentId: new(this.PdbId, this.PdbStamp),
            portablePdbVersion: 0x01000);
        return debugDirectoryBuilder;
    }

    public void EncodeProcedureDebugInfo(IProcedure procedure, MethodDefinitionHandle handle)
    {
        // Sequence points
        var sequencePointsForMethod = this.sequencePoints.ToImmutable();
        var sequencePoints = this.AddSequencePoints(default, sequencePointsForMethod);
        this.MetadataBuilder.AddMethodDebugInformation(
            document: this.GetOrAddDocument(procedure.Symbol.DeclarationSyntax),
            sequencePoints: sequencePoints);

        // Local scopes
        Debug.Assert(this.scopeStartStack.Count == 0);
        var localScopesForMethod = this.localScopes.ToImmutable();
        foreach (var scope in localScopesForMethod)
        {
            this.MetadataBuilder.AddLocalScope(
                method: handle,
                importScope: default,
                variableList: scope.VariableList,
                constantList: default,
                startOffset: scope.StartOffset,
                length: scope.Length);
        }

        // Clear for next
        this.sequencePoints.Clear();
        this.localScopes.Clear();
    }

    public void StartScope(int ilOffset, IEnumerable<AllocatedLocal> locals)
    {
        var variableList = this.NextLocalVariableHandle;
        foreach (var local in locals)
        {
            this.AddLocalVariable(
                attributes: LocalVariableAttributes.None,
                index: local.Index,
                name: local.Symbol!.Name);
        }
        this.scopeStartStack.Push(new(
            VariableList: variableList,
            StartOffset: ilOffset));
    }

    public void EndScope(int ilOffset)
    {
        var start = this.scopeStartStack.Pop();
        this.localScopes.Add(new(
            VariableList: start.VariableList,
            StartOffset: start.StartOffset,
            Length: ilOffset - start.StartOffset));
    }

    public void AddSequencePoint(InstructionEncoder encoder, OptimizingIr.Model.SequencePoint sequencePoint)
    {
        var sp = MakeSequencePoint(encoder, sequencePoint.Range);
        this.sequencePoints.Add(sp);
    }

    private static SequencePoint MakeSequencePoint(InstructionEncoder encoder, SyntaxRange? range)
    {
        if (range is null) return SequencePoint.Hidden(encoder.Offset);
        var r = range.Value;
        return new(
            ilOffset: encoder.Offset,
            startLine: r.Start.Line + 1,
            startColumn: r.Start.Column + 1,
            endLine: r.End.Line + 1,
            endColumn: r.End.Column + 1);
    }

    private DocumentHandle GetOrAddDocument(SyntaxNode? syntax) => this.GetOrAddDocument(syntax?.Tree.SourceText);

    private DocumentHandle GetOrAddDocument(SourceText? sourceText)
    {
        var path = sourceText?.Path;
        if (path is null) return default;

        return this.GetOrAddDocument(path, DracoLanguageGuid);
    }

    public void WritePdb(Stream pdbStream)
    {
        var pdbBuilder = new PortablePdbBuilder(
            tablesAndHeaps: this.MetadataBuilder,
            // TODO: Type-system stuff, likely for local scope and such
            typeSystemRowCounts: new int[MetadataTokens.TableCount].ToImmutableArray(),
            entryPoint: this.metadataCodegen.EntryPointHandle,
            // TODO: For deterministic builds
            idProvider: _ => new(this.PdbId, this.PdbStamp));

        var pdbBlob = new BlobBuilder();
        pdbBuilder.Serialize(pdbBlob);
        pdbBlob.WriteContentTo(pdbStream);
    }
}
