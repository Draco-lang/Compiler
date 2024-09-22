using System.Diagnostics.CodeAnalysis;

namespace Draco.Compiler.Internal;

[ExcludeFromCodeCoverage]
internal static class CompilerConstants
{
    /// <summary>
    /// The default name of the root module in IL.
    /// </summary>
    public const string DefaultModuleName = "FreeFunctions";

    /// <summary>
    /// Name of the entry point of the application.
    /// </summary>
    public const string EntryPointName = "main";

    /// <summary>
    /// The name of a scripts entry point.
    /// </summary>
    public const string ScriptEntryPointName = ".evaluate";

    /// <summary>
    /// The default member name.
    /// </summary>
    public const string DefaultMemberName = "Item";

    /// <summary>
    /// The enum tag field name.
    /// </summary>
    public const string EnumTagField = "value__";

    /// <summary>
    /// The metadata name of constructors.
    /// </summary>
    public const string ConstructorName = ".ctor";

    /// <summary>
    /// The prefix for operator methods.
    /// </summary>
    public const string OperatorPrefix = "op_";

    /// <summary>
    /// The name of the module that contains compile-time code.
    /// </summary>
    public const string CompileTimeModuleName = "__CompileTime";
}
