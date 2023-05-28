using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClrDebug;

namespace Draco.Debugger;

/// <summary>
/// Represents a lazily discovered object in the debugged program.
/// </summary>
public sealed class ObjectValue : IReadOnlyDictionary<string, object?>
{
    private struct Slot
    {
        public mdFieldDef FieldDef;
        public GetFieldPropsResult? FieldProps;
        public bool ValueInitialized;
        public object? Value;

        public Slot(mdFieldDef fieldDef)
        {
            this.FieldDef = fieldDef;
        }
    }

    public int Count => throw new NotImplementedException();
    public object? this[string key] => throw new NotImplementedException();

    public IEnumerable<string> Keys => throw new NotImplementedException();
    public IEnumerable<object?> Values => throw new NotImplementedException();

    private readonly ICorDebugObjectValue value;
    private readonly CorDebugClass @class;
    private readonly MetaDataImport metaData;

    private Slot[] Slots => this.slots ??= this.metaData
        .EnumFields(this.@class.Token)
        .Select(t => new Slot(t))
        .ToArray();
    private Slot[]? slots;

    public ObjectValue(ICorDebugObjectValue value)
    {
        this.value = value;
        if (value.GetClass(out var ppClass) != HRESULT.S_OK) throw new InvalidOperationException("could not retrieve class");
        this.@class = new CorDebugClass(ppClass);
        this.metaData = this.@class.Module.GetMetaDataInterface().MetaDataImport;
    }

    public override string ToString() => this.metaData.GetTypeDefProps(this.@class.Token).szTypeDef;

    public bool ContainsKey(string key) => throw new NotImplementedException();
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out object? value) => throw new NotImplementedException();
    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => throw new NotImplementedException();
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
