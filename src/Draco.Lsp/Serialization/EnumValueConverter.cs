using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Draco.Lsp.Serialization;

/// <summary>
/// Converts enum values according to the attribute.
/// </summary>
public class EnumValueConverter : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum;

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var type = typeof(EnumValueConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter?)Activator.CreateInstance(type);
    }
}

public class EnumValueConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
{
    private readonly Dictionary<TEnum, string> enumToString = new();
    private readonly Dictionary<string, TEnum> stringToEnum = new();

    public EnumValueConverter()
    {
        var type = typeof(TEnum);
        foreach (var value in Enum.GetValues<TEnum>())
        {
            var enumMember = type.GetMember(value.ToString())[0];
            var attr = enumMember.GetCustomAttribute<EnumMemberAttribute>();
            var name = attr?.Value ?? value.ToString();

            this.enumToString.Add(value, name);
            this.stringToEnum.Add(name, value);
        }
    }

    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => this.stringToEnum[reader.GetString()!];

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
        => writer.WriteStringValue(this.enumToString[value]);
}
