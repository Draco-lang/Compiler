using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Lsp.Generation.TypeScript;

internal abstract record class ModelElement(string? Documentation);

internal sealed record class InterfaceModel(
    string? Documentation,
    string Name,
    ImmutableArray<Field> Fields) : ModelElement(Documentation);

internal abstract record class ModelType;

internal sealed record class NameType(
    string Name) : ModelType;

internal sealed record class ArrayType(
    ModelType ElementType) : ModelType;

internal sealed record class AnonymousType(
    ImmutableArray<Field> Fields) : ModelType;

internal sealed record class Field(
    string? Documentation,
    string Name,
    bool Nullable,
    ModelType Type);
