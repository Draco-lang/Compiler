using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.FlowAnalysis;

internal sealed class VariablesAreAssignedCorrectType : BoundTreeVisitor
{
    public static void Analyze(BoundNode node, DiagnosticBag diagnostics)
    {
        var pass = new VariablesAreAssignedCorrectType(diagnostics);
        node.Accept(pass);
    }

    private readonly DiagnosticBag diagnostics;

    public VariablesAreAssignedCorrectType(DiagnosticBag diagnostics)
    {
        this.diagnostics = diagnostics;
    }

    public override void VisitLiteralExpression(BoundLiteralExpression node)
    {
        if (node.Type is not BuiltinType builtin) return;
        foreach (var baseType in builtin.Bases)
        {
            if (baseType == IntrinsicTypes.IntegralType || baseType == IntrinsicTypes.FloatingPointType)
            {
                this.CheckIfValueIsCorrectType(node.Type, node.Value, node.Syntax);
                return;
            }
        }
    }

    private void CheckIfValueIsCorrectType(Type type, object? value, SyntaxNode? node)
    {
        try
        {
            if (ReferenceEquals(type, IntrinsicTypes.Int8)) _ = sbyte.MaxValue >= System.Convert.ToSByte(value) && System.Convert.ToSByte(value) >= sbyte.MinValue;
            else if (ReferenceEquals(type, IntrinsicTypes.Int16)) _ = short.MaxValue >= System.Convert.ToInt16(value) && System.Convert.ToInt16(value) >= short.MinValue;
            else if (ReferenceEquals(type, IntrinsicTypes.Int32)) _ = int.MaxValue >= System.Convert.ToInt32(value) && System.Convert.ToInt32(value) >= int.MinValue;
            else if (ReferenceEquals(type, IntrinsicTypes.Int64)) _ = long.MaxValue >= System.Convert.ToInt64(value) && System.Convert.ToInt64(value) >= long.MinValue;

            else if (ReferenceEquals(type, IntrinsicTypes.Uint8)) _ = byte.MaxValue >= System.Convert.ToByte(value) && System.Convert.ToByte(value) >= byte.MinValue;
            else if (ReferenceEquals(type, IntrinsicTypes.Uint16)) _ = ushort.MaxValue >= System.Convert.ToUInt16(value) && System.Convert.ToUInt16(value) >= ushort.MinValue;
            else if (ReferenceEquals(type, IntrinsicTypes.Uint32)) _ = uint.MaxValue >= System.Convert.ToUInt32(value) && System.Convert.ToUInt32(value) >= uint.MinValue;
            else if (ReferenceEquals(type, IntrinsicTypes.Uint64)) _ = ulong.MaxValue >= System.Convert.ToUInt64(value) && System.Convert.ToUInt64(value) >= ulong.MinValue;

            else if (ReferenceEquals(type, IntrinsicTypes.Float32)) _ = float.MaxValue >= System.Convert.ToSingle(value) && System.Convert.ToSingle(value) >= float.MinValue;
            else if (ReferenceEquals(type, IntrinsicTypes.Float64)) _ = double.MaxValue >= System.Convert.ToDouble(value) && System.Convert.ToDouble(value) >= double.MinValue;
        }
        catch
        {
            this.diagnostics.Add(Diagnostic.Create(
                template: FlowAnalysisErrors.ValueOutOfRangeOfType,
                location: node?.Location,
                formatArgs: type));
        }
    }
}
