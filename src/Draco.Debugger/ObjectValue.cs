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
        public GetFieldPropsResult FieldProps;
        public bool ValueInitialized;
        public object? Value;

        public Slot(mdFieldDef fieldDef, GetFieldPropsResult fieldProps)
        {
            this.FieldDef = fieldDef;
            this.FieldProps = fieldProps;
        }
    }

    public int Count => this.Slots.Length;
    public object? this[string key] => this.TryGetValue(key, out var result)
        ? result
        : throw new KeyNotFoundException();

    public IEnumerable<string> Keys => this.Slots.Select(s => s.FieldProps.szField);
    public IEnumerable<object?> Values
    {
        get
        {
            for (var i = 0; i < this.Slots.Length; ++i) yield return this.GetValue(i);
        }
    }

    private readonly ICorDebugObjectValue value;
    private readonly CorDebugClass @class;
    private readonly MetaDataImport metaData;

    private Slot[] Slots => this.slots ??= this.metaData
        .EnumFields(this.@class.Token)
        .Select(t => (Token: t, FieldProps: this.metaData.GetFieldProps(t)))
        .Where(p => !p.FieldProps.pdwAttr.HasFlag(CorFieldAttr.fdStatic))
        .Select(p => new Slot(p.Token, p.FieldProps))
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

    private object? GetValue(int slotIndex)
    {
        var slot = this.Slots[slotIndex];
        if (!slot.ValueInitialized)
        {
            slot.ValueInitialized = true;
            var result = this.value.GetFieldValue(this.@class.Raw, slot.FieldDef, out var value);
            if (result != HRESULT.S_OK) throw new InvalidOperationException("failed to retrieve field value");
            slot.Value = CorDebugValue.New(value).ToBrowsableObject();
            this.Slots[slotIndex] = slot;
        }
        return slot.Value;
    }

    public bool ContainsKey(string key) => this.Keys.Contains(key);
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out object? value)
    {
        for (var i = 0; i < this.Slots.Length; ++i)
        {
            var prop = this.Slots[i].FieldProps;
            if (prop.szField == key)
            {
                value = this.GetValue(i);
                return true;
            }
        }
        value = default;
        return false;
    }
    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => this.Keys
        .Zip(this.Values)
        .Select(p => new KeyValuePair<string, object?>(p.First, p.Second))
        .GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
