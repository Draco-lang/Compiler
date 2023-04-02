using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.OptimizingIr.Model;

namespace Draco.Compiler.Internal.Codegen;

/// <summary>
/// Generates PDB from IR.
/// </summary>
internal sealed class PdbCodegen
{
    /// <summary>
    /// GUID for identifying a Draco document.
    /// </summary>
    public static readonly Guid DracoLanguageGuid = new("7ef7b804-0709-43bc-b1b5-998bb801477b");

    private readonly MetadataCodegen metadataCodegen;
    private readonly MetadataBuilder metadataBuilder = new();
    private readonly Dictionary<SourceText, DocumentHandle> documentHandles = new();
    private readonly ImmutableArray<SequencePoint>.Builder sequencePoints = ImmutableArray.CreateBuilder<SequencePoint>();

    public PdbCodegen(MetadataCodegen metadataCodegen)
    {
        this.metadataCodegen = metadataCodegen;
    }

    public void EncodeProcedure(IProcedure procedure)
    {
        var sequencePointsForMethod = this.sequencePoints.ToImmutable();

        this.EncodeSequencePoints(default, sequencePointsForMethod);

        // Clear for next
        this.sequencePoints.Clear();
    }

    public void AddSequencePoint(InstructionEncoder encoder, OptimizingIr.Model.SequencePoint sequencePoint)
    {
        var sp = this.MakeSequencePoint(encoder, sequencePoint.Syntax);
        this.sequencePoints.Add(sp);
    }

    private SequencePoint MakeSequencePoint(InstructionEncoder encoder, SyntaxNode? syntax)
    {
        if (syntax is null) return SequencePoint.Hidden(default, encoder.Offset);
        var document = this.GetOrAddDocument(syntax);
        var range = syntax.Range;
        return new(
            Document: document,
            IlOffset: encoder.Offset,
            StartLine: range.Start.Line,
            StartColumn: range.Start.Column,
            EndLine: range.End.Line,
            EndColumn: range.End.Column);
    }

    private DocumentHandle GetOrAddDocument(SyntaxNode? syntax) => this.GetOrAddDocument(syntax?.Tree.SourceText);

    private DocumentHandle GetOrAddDocument(SourceText? sourceText)
    {
        // No source text
        if (sourceText is null) return default;
        // No path
        if (sourceText.Path is null) return default;
        if (!this.documentHandles.TryGetValue(sourceText, out var handle))
        {
            var documentName = this.metadataBuilder.GetOrAddDocumentName(sourceText.Path.AbsolutePath);
            handle = this.metadataBuilder.AddDocument(
                name: documentName,
                hashAlgorithm: default,
                hash: default,
                language: this.metadataBuilder.GetOrAddGuid(DracoLanguageGuid));
            this.documentHandles.Add(sourceText, handle);
        }
        return handle;
    }

    // From: https://github.com/dotnet/roslyn/blob/723b5ef7fc8146c65993814f1dba94f55f1c59a6/src/Compilers/Core/Portable/PEWriter/MetadataWriter.PortablePdb.cs#L618
    private BlobHandle EncodeSequencePoints(
        StandaloneSignatureHandle localSignatureHandleOpt,
        ImmutableArray<SequencePoint> sequencePoints)
    {
        // No-op
        if (sequencePoints.Length == 0) return default;

        var writer = new BlobBuilder();

        var previousNonHiddenStartLine = -1;
        var previousNonHiddenStartColumn = -1;

        // Header
        writer.WriteCompressedInteger(MetadataTokens.GetRowNumber(localSignatureHandleOpt));

        // Go through sequence points
        for (var i = 0; i < sequencePoints.Length; i++)
        {
            // delta IL offset:
            if (i > 0)
            {
                writer.WriteCompressedInteger(sequencePoints[i].IlOffset - sequencePoints[i - 1].IlOffset);
            }
            else
            {
                writer.WriteCompressedInteger(sequencePoints[i].IlOffset);
            }

            if (sequencePoints[i].IsHidden)
            {
                writer.WriteInt16(0);
                continue;
            }

            // Delta Lines & Columns:
            EncodeDeltaLinesAndColumns(writer, sequencePoints[i]);

            // delta Start Lines & Columns:
            if (previousNonHiddenStartLine < 0)
            {
                Debug.Assert(previousNonHiddenStartColumn < 0);
                writer.WriteCompressedInteger(sequencePoints[i].StartLine);
                writer.WriteCompressedInteger(sequencePoints[i].StartColumn);
            }
            else
            {
                writer.WriteCompressedSignedInteger(sequencePoints[i].StartLine - previousNonHiddenStartLine);
                writer.WriteCompressedSignedInteger(sequencePoints[i].StartColumn - previousNonHiddenStartColumn);
            }

            previousNonHiddenStartLine = sequencePoints[i].StartLine;
            previousNonHiddenStartColumn = sequencePoints[i].StartColumn;
        }

        return this.metadataBuilder.GetOrAddBlob(writer);
    }

    private static void EncodeDeltaLinesAndColumns(BlobBuilder writer, SequencePoint sequencePoint)
    {
        var deltaLines = sequencePoint.EndLine - sequencePoint.StartLine;
        var deltaColumns = sequencePoint.EndColumn - sequencePoint.StartColumn;

        // only hidden sequence points have zero width
        Debug.Assert(deltaLines != 0 || deltaColumns != 0 || sequencePoint.IsHidden);

        writer.WriteCompressedInteger(deltaLines);

        if (deltaLines == 0)
        {
            writer.WriteCompressedInteger(deltaColumns);
        }
        else
        {
            writer.WriteCompressedSignedInteger(deltaColumns);
        }
    }

    public void WritePdb(Stream pdbStream)
    {
        var pdbBuilder = new PortablePdbBuilder(
            tablesAndHeaps: this.metadataBuilder,
            // TODO: Type-system stuff, likely for local scope and such
            typeSystemRowCounts: new int[MetadataTokens.TableCount].ToImmutableArray(),
            entryPoint: this.metadataCodegen.EntryPointHandle,
            // TODO: For deterministic builds
            idProvider: null);

        var pdbBlob = new BlobBuilder();
        pdbBuilder.Serialize(pdbBlob);
        pdbBlob.WriteContentTo(pdbStream);
    }
}
