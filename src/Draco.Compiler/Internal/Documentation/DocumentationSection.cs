using System;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;

namespace Draco.Compiler.Internal.Documentation;

/// <summary>
/// Represents a section of the documentation.
/// </summary>
/// <param name="Name">The name of the section</param>
/// <param name="Elements">The <see cref="DocumentationElement"/>s this section contains.</param>
internal record class DocumentationSection(string Name, ImmutableArray<DocumentationElement> Elements);

/// <summary>
/// Represents a general description of this <see cref="Symbols.Symbol"/>.
/// </summary>
/// <param name="Elements"></param>
internal record class SummaryDocumentationSection(ImmutableArray<DocumentationElement> Elements) : DocumentationSection("summary", Elements);

/// <summary>
/// Represents all parameters this <see cref="Symbols.Symbol"/> has.
/// </summary>
/// <param name="Parameters">The parameters references.</param>
internal record class ParametersDocumentationSection(ImmutableArray<ParameterDocumentationElement> Parameters) : DocumentationSection("Parameters", Parameters.Cast<DocumentationElement>().ToImmutableArray());

/// <summary>
/// Represents all type parameters this <see cref="Symbols.Symbol"/> has.
/// </summary>
/// <param name="TypeParameters">The type parameters references.</param>
internal record class TypeParametersDocumentationSection(ImmutableArray<TypeParameterDocumentationElement> TypeParameters) : DocumentationSection("TypeParameters", TypeParameters.Cast<DocumentationElement>().ToImmutableArray());

/// <summary>
/// Represents a section of code.
/// </summary>
/// <param name="Code">The code to display.</param>
internal record class CodeDocumentationSection(CodeDocumentationElement Code) : DocumentationSection("Code", ImmutableArray.Create<DocumentationElement>(Code));
