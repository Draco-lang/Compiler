using System.CodeDom.Compiler;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Draco.Lsp.Model;

#region Generated

#pragma warning disable CS8618

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class InitializeParams : IWorkDoneProgressParams
{
    [GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
    public sealed class ClientInfoParams
    {
        /// <summary>
        /// The name of the client as defined by the client.
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public String Name { get; set; }

        /// <summary>
        /// The client's version as defined by the client.
        /// </summary>
        [JsonProperty(PropertyName = "version", NullValueHandling = NullValueHandling.Ignore)]
        public String? Version { get; set; }
    }

    /// <summary>
    /// The process Id of the parent process that started the server. Is null if
    /// the process has not been started by another process. If the parent
    /// process is not alive then the server should exit (see exit notification)
    /// its process.
    /// </summary>
    [JsonProperty(PropertyName = "processId")]
    public Int32? ProcessId { get; set; }

    /// <summary>
    /// Information about the client
    ///
    /// @since 3.15.0
    /// </summary>
    [JsonProperty(PropertyName = "clientInfo", NullValueHandling = NullValueHandling.Ignore)]
    public InitializeParams.ClientInfoParams? ClientInfo { get; set; }

    /// <summary>
    /// The locale the client is currently showing the user interface
    /// in. This must not necessarily be the locale of the operating
    /// system.
    ///
    /// Uses IETF language tags as the value's syntax
    /// (See https://en.wikipedia.org/wiki/IETF_language_tag)
    ///
    /// @since 3.16.0
    /// </summary>
    [JsonProperty(PropertyName = "locale", NullValueHandling = NullValueHandling.Ignore)]
    public String? Locale { get; set; }

    /// <summary>
    /// The rootPath of the workspace. Is null
    /// if no folder is open.
    ///
    /// @deprecated in favour of `rootUri`.
    /// </summary>
    [JsonProperty(PropertyName = "rootPath", NullValueHandling = NullValueHandling.Ignore)]
    public String? RootPath { get; set; }

    /// <summary>
    /// The rootUri of the workspace. Is null if no
    /// folder is open. If both `rootPath` and `rootUri` are set
    /// `rootUri` wins.
    ///
    /// @deprecated in favour of `workspaceFolders`
    /// </summary>
    [JsonProperty(PropertyName = "rootUri")]
    public String? RootUri { get; set; }

    /// <summary>
    /// User provided initialization options.
    /// </summary>
    [JsonProperty(PropertyName = "initializationOptions", NullValueHandling = NullValueHandling.Ignore)]
    public Object? InitializationOptions { get; set; }

    /// <summary>
    /// The capabilities provided by the client (editor or tool)
    /// </summary>
    [JsonProperty(PropertyName = "capabilities")]
    public ClientCapabilities Capabilities { get; set; }

    /// <summary>
    /// The initial trace setting. If omitted trace is disabled ('off').
    /// </summary>
    [JsonProperty(PropertyName = "trace", NullValueHandling = NullValueHandling.Ignore)]
    public TraceValue? Trace { get; set; }

    /// <summary>
    /// The workspace folders configured in the client when the server starts.
    /// This property is only available if the client supports workspace folders.
    /// It can be `null` if the client supports workspace folders but none are
    /// configured.
    ///
    /// @since 3.6.0
    /// </summary>
    [JsonProperty(PropertyName = "workspaceFolders", NullValueHandling = NullValueHandling.Ignore)]
    public IList<WorkspaceFolder>? WorkspaceFolders { get; set; }

    /// <summary>
    /// An optional token that a server can use to report work done progress.
    /// </summary>
    [JsonProperty(PropertyName = "workDoneToken", NullValueHandling = NullValueHandling.Ignore)]
    public OneOf<Int32, String>? WorkDoneToken { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class WorkDoneProgressParams
{
    /// <summary>
    /// An optional token that a server can use to report work done progress.
    /// </summary>
    [JsonProperty(PropertyName = "workDoneToken", NullValueHandling = NullValueHandling.Ignore)]
    public OneOf<Int32, String>? WorkDoneToken { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public interface IWorkDoneProgressParams
{
    /// <summary>
    /// An optional token that a server can use to report work done progress.
    /// </summary>
    [JsonProperty(PropertyName = "workDoneToken", NullValueHandling = NullValueHandling.Ignore)]
    public OneOf<Int32, String>? WorkDoneToken { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class ClientCapabilities
{
    [GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
    public sealed class FileOperationsCapabilities
    {
        /// <summary>
        /// Whether the client supports dynamic registration for file
        /// requests/notifications.
        /// </summary>
        [JsonProperty(PropertyName = "dynamicRegistration", NullValueHandling = NullValueHandling.Ignore)]
        public Boolean? DynamicRegistration { get; set; }

        /// <summary>
        /// The client has support for sending didCreateFiles notifications.
        /// </summary>
        [JsonProperty(PropertyName = "didCreate", NullValueHandling = NullValueHandling.Ignore)]
        public Boolean? DidCreate { get; set; }

        /// <summary>
        /// The client has support for sending willCreateFiles requests.
        /// </summary>
        [JsonProperty(PropertyName = "willCreate", NullValueHandling = NullValueHandling.Ignore)]
        public Boolean? WillCreate { get; set; }

        /// <summary>
        /// The client has support for sending didRenameFiles notifications.
        /// </summary>
        [JsonProperty(PropertyName = "didRename", NullValueHandling = NullValueHandling.Ignore)]
        public Boolean? DidRename { get; set; }

        /// <summary>
        /// The client has support for sending willRenameFiles requests.
        /// </summary>
        [JsonProperty(PropertyName = "willRename", NullValueHandling = NullValueHandling.Ignore)]
        public Boolean? WillRename { get; set; }

        /// <summary>
        /// The client has support for sending didDeleteFiles notifications.
        /// </summary>
        [JsonProperty(PropertyName = "didDelete", NullValueHandling = NullValueHandling.Ignore)]
        public Boolean? DidDelete { get; set; }

        /// <summary>
        /// The client has support for sending willDeleteFiles requests.
        /// </summary>
        [JsonProperty(PropertyName = "willDelete", NullValueHandling = NullValueHandling.Ignore)]
        public Boolean? WillDelete { get; set; }
    }

    [GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
    public sealed class WorkspaceCapabilities
    {
        /// <summary>
        /// The client supports applying batch edits
        /// to the workspace by supporting the request
        /// 'workspace/applyEdit'
        /// </summary>
        [JsonProperty(PropertyName = "applyEdit", NullValueHandling = NullValueHandling.Ignore)]
        public Boolean? ApplyEdit { get; set; }

        /// <summary>
        /// Capabilities specific to `WorkspaceEdit`s
        /// </summary>
        [JsonProperty(PropertyName = "workspaceEdit", NullValueHandling = NullValueHandling.Ignore)]
        public WorkspaceEditClientCapabilities? WorkspaceEdit { get; set; }

        /// <summary>
        /// Capabilities specific to the `workspace/didChangeConfiguration`
        /// notification.
        /// </summary>
        [JsonProperty(PropertyName = "didChangeConfiguration", NullValueHandling = NullValueHandling.Ignore)]
        public DidChangeConfigurationClientCapabilities? DidChangeConfiguration { get; set; }

        /// <summary>
        /// Capabilities specific to the `workspace/didChangeWatchedFiles`
        /// notification.
        /// </summary>
        [JsonProperty(PropertyName = "didChangeWatchedFiles", NullValueHandling = NullValueHandling.Ignore)]
        public DidChangeWatchedFilesClientCapabilities? DidChangeWatchedFiles { get; set; }

        /// <summary>
        /// Capabilities specific to the `workspace/symbol` request.
        /// </summary>
        [JsonProperty(PropertyName = "symbol", NullValueHandling = NullValueHandling.Ignore)]
        public WorkspaceSymbolClientCapabilities? Symbol { get; set; }

        /// <summary>
        /// Capabilities specific to the `workspace/executeCommand` request.
        /// </summary>
        [JsonProperty(PropertyName = "executeCommand", NullValueHandling = NullValueHandling.Ignore)]
        public ExecuteCommandClientCapabilities? ExecuteCommand { get; set; }

        /// <summary>
        /// The client has support for workspace folders.
        ///
        /// @since 3.6.0
        /// </summary>
        [JsonProperty(PropertyName = "workspaceFolders", NullValueHandling = NullValueHandling.Ignore)]
        public Boolean? WorkspaceFolders { get; set; }

        /// <summary>
        /// The client supports `workspace/configuration` requests.
        ///
        /// @since 3.6.0
        /// </summary>
        [JsonProperty(PropertyName = "configuration", NullValueHandling = NullValueHandling.Ignore)]
        public Boolean? Configuration { get; set; }

        /// <summary>
        /// Capabilities specific to the semantic token requests scoped to the
        /// workspace.
        ///
        /// @since 3.16.0
        /// </summary>
        [JsonProperty(PropertyName = "semanticTokens", NullValueHandling = NullValueHandling.Ignore)]
        public SemanticTokensWorkspaceClientCapabilities? SemanticTokens { get; set; }

        /// <summary>
        /// Capabilities specific to the code lens requests scoped to the
        /// workspace.
        ///
        /// @since 3.16.0
        /// </summary>
        [JsonProperty(PropertyName = "codeLens", NullValueHandling = NullValueHandling.Ignore)]
        public CodeLensWorkspaceClientCapabilities? CodeLens { get; set; }

        /// <summary>
        /// The client has support for file requests/notifications.
        ///
        /// @since 3.16.0
        /// </summary>
        [JsonProperty(PropertyName = "fileOperations", NullValueHandling = NullValueHandling.Ignore)]
        public ClientCapabilities.FileOperationsCapabilities? FileOperations { get; set; }

        /// <summary>
        /// Client workspace capabilities specific to inline values.
        ///
        /// @since 3.17.0
        /// </summary>
        [JsonProperty(PropertyName = "inlineValue", NullValueHandling = NullValueHandling.Ignore)]
        public InlineValueWorkspaceClientCapabilities? InlineValue { get; set; }

        /// <summary>
        /// Client workspace capabilities specific to inlay hints.
        ///
        /// @since 3.17.0
        /// </summary>
        [JsonProperty(PropertyName = "inlayHint", NullValueHandling = NullValueHandling.Ignore)]
        public InlayHintWorkspaceClientCapabilities? InlayHint { get; set; }

        /// <summary>
        /// Client workspace capabilities specific to diagnostics.
        ///
        /// @since 3.17.0.
        /// </summary>
        [JsonProperty(PropertyName = "diagnostics", NullValueHandling = NullValueHandling.Ignore)]
        public DiagnosticWorkspaceClientCapabilities? Diagnostics { get; set; }
    }

    [GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
    public sealed class WindowCapabilities
    {
        /// <summary>
        /// It indicates whether the client supports server initiated
        /// progress using the `window/workDoneProgress/create` request.
        ///
        /// The capability also controls Whether client supports handling
        /// of progress notifications. If set servers are allowed to report a
        /// `workDoneProgress` property in the request specific server
        /// capabilities.
        ///
        /// @since 3.15.0
        /// </summary>
        [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
        public Boolean? WorkDoneProgress { get; set; }

        /// <summary>
        /// Capabilities specific to the showMessage request
        ///
        /// @since 3.16.0
        /// </summary>
        [JsonProperty(PropertyName = "showMessage", NullValueHandling = NullValueHandling.Ignore)]
        public ShowMessageRequestClientCapabilities? ShowMessage { get; set; }

        /// <summary>
        /// Client capabilities for the show document request.
        ///
        /// @since 3.16.0
        /// </summary>
        [JsonProperty(PropertyName = "showDocument", NullValueHandling = NullValueHandling.Ignore)]
        public ShowDocumentClientCapabilities? ShowDocument { get; set; }
    }

    [GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
    public sealed class StaleRequestSupportCapabilities
    {
        /// <summary>
        /// The client will actively cancel the request.
        /// </summary>
        [JsonProperty(PropertyName = "cancel")]
        public Boolean Cancel { get; set; }

        /// <summary>
        /// The list of requests for which the client
        /// will retry the request if it receives a
        /// response with error code `ContentModified``
        /// </summary>
        [JsonProperty(PropertyName = "retryOnContentModified")]
        public IList<String> RetryOnContentModified { get; set; }
    }

    [GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
    public sealed class GeneralCapabilities
    {
        /// <summary>
        /// Client capability that signals how the client
        /// handles stale requests (e.g. a request
        /// for which the client will not process the response
        /// anymore since the information is outdated).
        ///
        /// @since 3.17.0
        /// </summary>
        [JsonProperty(PropertyName = "staleRequestSupport", NullValueHandling = NullValueHandling.Ignore)]
        public ClientCapabilities.StaleRequestSupportCapabilities? StaleRequestSupport { get; set; }

        /// <summary>
        /// Client capabilities specific to regular expressions.
        ///
        /// @since 3.16.0
        /// </summary>
        [JsonProperty(PropertyName = "regularExpressions", NullValueHandling = NullValueHandling.Ignore)]
        public RegularExpressionsClientCapabilities? RegularExpressions { get; set; }

        /// <summary>
        /// Client capabilities specific to the client's markdown parser.
        ///
        /// @since 3.16.0
        /// </summary>
        [JsonProperty(PropertyName = "markdown", NullValueHandling = NullValueHandling.Ignore)]
        public MarkdownClientCapabilities? Markdown { get; set; }

        /// <summary>
        /// The position encodings supported by the client. Client and server
        /// have to agree on the same position encoding to ensure that offsets
        /// (e.g. character position in a line) are interpreted the same on both
        /// side.
        ///
        /// To keep the protocol backwards compatible the following applies: if
        /// the value 'utf-16' is missing from the array of position encodings
        /// servers can assume that the client supports UTF-16. UTF-16 is
        /// therefore a mandatory encoding.
        ///
        /// If omitted it defaults to ['utf-16'].
        ///
        /// Implementation considerations: since the conversion from one encoding
        /// into another requires the content of the file / line the conversion
        /// is best done where the file is read which is usually on the server
        /// side.
        ///
        /// @since 3.17.0
        /// </summary>
        [JsonProperty(PropertyName = "positionEncodings", NullValueHandling = NullValueHandling.Ignore)]
        public IList<PositionEncodingKind>? PositionEncodings { get; set; }
    }

    /// <summary>
    /// Workspace specific client capabilities.
    /// </summary>
    [JsonProperty(PropertyName = "workspace", NullValueHandling = NullValueHandling.Ignore)]
    public ClientCapabilities.WorkspaceCapabilities? Workspace { get; set; }

    /// <summary>
    /// Text document specific client capabilities.
    /// </summary>
    [JsonProperty(PropertyName = "textDocument", NullValueHandling = NullValueHandling.Ignore)]
    public TextDocumentClientCapabilities? TextDocument { get; set; }

    /// <summary>
    /// Capabilities specific to the notebook document support.
    ///
    /// @since 3.17.0
    /// </summary>
    [JsonProperty(PropertyName = "notebookDocument", NullValueHandling = NullValueHandling.Ignore)]
    public NotebookDocumentClientCapabilities? NotebookDocument { get; set; }

    /// <summary>
    /// Window specific client capabilities.
    /// </summary>
    [JsonProperty(PropertyName = "window", NullValueHandling = NullValueHandling.Ignore)]
    public ClientCapabilities.WindowCapabilities? Window { get; set; }

    /// <summary>
    /// General client capabilities.
    ///
    /// @since 3.16.0
    /// </summary>
    [JsonProperty(PropertyName = "general", NullValueHandling = NullValueHandling.Ignore)]
    public ClientCapabilities.GeneralCapabilities? General { get; set; }

    /// <summary>
    /// Experimental client capabilities.
    /// </summary>
    [JsonProperty(PropertyName = "experimental", NullValueHandling = NullValueHandling.Ignore)]
    public Object? Experimental { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class WorkspaceEditClientCapabilities
{
    [GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
    public sealed class ChangeAnnotationSupportCapabilities
    {
        /// <summary>
        /// Whether the client groups edits with equal labels into tree nodes,
        /// for instance all edits labelled with "Changes in Strings" would
        /// be a tree node.
        /// </summary>
        [JsonProperty(PropertyName = "groupsOnLabel", NullValueHandling = NullValueHandling.Ignore)]
        public Boolean? GroupsOnLabel { get; set; }
    }

    /// <summary>
    /// The client supports versioned document changes in `WorkspaceEdit`s
    /// </summary>
    [JsonProperty(PropertyName = "documentChanges", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? DocumentChanges { get; set; }

    /// <summary>
    /// The resource operations the client supports. Clients should at least
    /// support 'create', 'rename' and 'delete' files and folders.
    ///
    /// @since 3.13.0
    /// </summary>
    [JsonProperty(PropertyName = "resourceOperations", NullValueHandling = NullValueHandling.Ignore)]
    public IList<ResourceOperationKind>? ResourceOperations { get; set; }

    /// <summary>
    /// The failure handling strategy of a client if applying the workspace edit
    /// fails.
    ///
    /// @since 3.13.0
    /// </summary>
    [JsonProperty(PropertyName = "failureHandling", NullValueHandling = NullValueHandling.Ignore)]
    public FailureHandlingKind? FailureHandling { get; set; }

    /// <summary>
    /// Whether the client normalizes line endings to the client specific
    /// setting.
    /// If set to `true` the client will normalize line ending characters
    /// in a workspace edit to the client specific new line character(s).
    ///
    /// @since 3.16.0
    /// </summary>
    [JsonProperty(PropertyName = "normalizesLineEndings", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? NormalizesLineEndings { get; set; }

    /// <summary>
    /// Whether the client in general supports change annotations on text edits,
    /// create file, rename file and delete file changes.
    ///
    /// @since 3.16.0
    /// </summary>
    [JsonProperty(PropertyName = "changeAnnotationSupport", NullValueHandling = NullValueHandling.Ignore)]
    public WorkspaceEditClientCapabilities.ChangeAnnotationSupportCapabilities? ChangeAnnotationSupport { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public enum ResourceOperationKind
{
    /// <summary>
    /// Supports creating new files and folders.
    /// </summary>
    [EnumMember(Value = "create")]
    Create,
    /// <summary>
    /// Supports renaming existing files and folders.
    /// </summary>
    [EnumMember(Value = "rename")]
    Rename,
    /// <summary>
    /// Supports deleting existing files and folders.
    /// </summary>
    [EnumMember(Value = "delete")]
    Delete,
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public enum FailureHandlingKind
{
    /// <summary>
    /// Applying the workspace change is simply aborted if one of the changes
    /// provided fails. All operations executed before the failing operation
    /// stay executed.
    /// </summary>
    [EnumMember(Value = "abort")]
    Abort,
    /// <summary>
    /// All operations are executed transactional. That means they either all
    /// succeed or no changes at all are applied to the workspace.
    /// </summary>
    [EnumMember(Value = "transactional")]
    Transactional,
    /// <summary>
    /// If the workspace edit contains only textual file changes they are
    /// executed transactional. If resource changes (create, rename or delete
    /// file) are part of the change the failure handling strategy is abort.
    /// </summary>
    [EnumMember(Value = "textOnlyTransactional")]
    TextOnlyTransactional,
    /// <summary>
    /// The client tries to undo the operations already executed. But there is no
    /// guarantee that this is succeeding.
    /// </summary>
    [EnumMember(Value = "undo")]
    Undo,
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class DidChangeConfigurationClientCapabilities
{
    /// <summary>
    /// Did change configuration notification supports dynamic registration.
    /// </summary>
    [JsonProperty(PropertyName = "dynamicRegistration", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? DynamicRegistration { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class DidChangeWatchedFilesClientCapabilities
{
    /// <summary>
    /// Did change watched files notification supports dynamic registration.
    /// Please note that the current protocol doesn't support static
    /// configuration for file changes from the server side.
    /// </summary>
    [JsonProperty(PropertyName = "dynamicRegistration", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? DynamicRegistration { get; set; }

    /// <summary>
    /// Whether the client has support for relative patterns
    /// or not.
    ///
    /// @since 3.17.0
    /// </summary>
    [JsonProperty(PropertyName = "relativePatternSupport", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? RelativePatternSupport { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class WorkspaceSymbolClientCapabilities
{
    [GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
    public sealed class SymbolKindCapabilities
    {
        /// <summary>
        /// The symbol kind values the client supports. When this
        /// property exists the client also guarantees that it will
        /// handle values outside its set gracefully and falls back
        /// to a default value when unknown.
        ///
        /// If this property is not present the client only supports
        /// the symbol kinds from `File` to `Array` as defined in
        /// the initial version of the protocol.
        /// </summary>
        [JsonProperty(PropertyName = "valueSet", NullValueHandling = NullValueHandling.Ignore)]
        public IList<SymbolKind>? ValueSet { get; set; }
    }

    [GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
    public sealed class TagSupportCapabilities
    {
        /// <summary>
        /// The tags supported by the client.
        /// </summary>
        [JsonProperty(PropertyName = "valueSet")]
        public IList<SymbolTag> ValueSet { get; set; }
    }

    [GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
    public sealed class ResolveSupportCapabilities
    {
        /// <summary>
        /// The properties that a client can resolve lazily. Usually
        /// `location.range`
        /// </summary>
        [JsonProperty(PropertyName = "properties")]
        public IList<String> Properties { get; set; }
    }

    /// <summary>
    /// Symbol request supports dynamic registration.
    /// </summary>
    [JsonProperty(PropertyName = "dynamicRegistration", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? DynamicRegistration { get; set; }

    /// <summary>
    /// Specific capabilities for the `SymbolKind` in the `workspace/symbol`
    /// request.
    /// </summary>
    [JsonProperty(PropertyName = "symbolKind", NullValueHandling = NullValueHandling.Ignore)]
    public WorkspaceSymbolClientCapabilities.SymbolKindCapabilities? SymbolKind { get; set; }

    /// <summary>
    /// The client supports tags on `SymbolInformation` and `WorkspaceSymbol`.
    /// Clients supporting tags have to handle unknown tags gracefully.
    ///
    /// @since 3.16.0
    /// </summary>
    [JsonProperty(PropertyName = "tagSupport", NullValueHandling = NullValueHandling.Ignore)]
    public WorkspaceSymbolClientCapabilities.TagSupportCapabilities? TagSupport { get; set; }

    /// <summary>
    /// The client support partial workspace symbols. The client will send the
    /// request `workspaceSymbol/resolve` to the server to resolve additional
    /// properties.
    ///
    /// @since 3.17.0 - proposedState
    /// </summary>
    [JsonProperty(PropertyName = "resolveSupport", NullValueHandling = NullValueHandling.Ignore)]
    public WorkspaceSymbolClientCapabilities.ResolveSupportCapabilities? ResolveSupport { get; set; }
}

/// <summary>
/// A symbol kind.
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public enum SymbolKind
{
    [EnumMember(Value = "1")]
    File,
    [EnumMember(Value = "2")]
    Module,
    [EnumMember(Value = "3")]
    Namespace,
    [EnumMember(Value = "4")]
    Package,
    [EnumMember(Value = "5")]
    Class,
    [EnumMember(Value = "6")]
    Method,
    [EnumMember(Value = "7")]
    Property,
    [EnumMember(Value = "8")]
    Field,
    [EnumMember(Value = "9")]
    Constructor,
    [EnumMember(Value = "10")]
    Enum,
    [EnumMember(Value = "11")]
    Interface,
    [EnumMember(Value = "12")]
    Function,
    [EnumMember(Value = "13")]
    Variable,
    [EnumMember(Value = "14")]
    Constant,
    [EnumMember(Value = "15")]
    String,
    [EnumMember(Value = "16")]
    Number,
    [EnumMember(Value = "17")]
    Boolean,
    [EnumMember(Value = "18")]
    Array,
    [EnumMember(Value = "19")]
    Object,
    [EnumMember(Value = "20")]
    Key,
    [EnumMember(Value = "21")]
    Null,
    [EnumMember(Value = "22")]
    EnumMember,
    [EnumMember(Value = "23")]
    Struct,
    [EnumMember(Value = "24")]
    Event,
    [EnumMember(Value = "25")]
    Operator,
    [EnumMember(Value = "26")]
    TypeParameter,
}

/// <summary>
/// Symbol tags are extra annotations that tweak the rendering of a symbol.
///
/// @since 3.16
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public enum SymbolTag
{
    /// <summary>
    /// Render a symbol as obsolete, usually using a strike-out.
    /// </summary>
    [EnumMember(Value = "1")]
    Deprecated,
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class ExecuteCommandClientCapabilities
{
    /// <summary>
    /// Execute command supports dynamic registration.
    /// </summary>
    [JsonProperty(PropertyName = "dynamicRegistration", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? DynamicRegistration { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class SemanticTokensWorkspaceClientCapabilities
{
    /// <summary>
    /// Whether the client implementation supports a refresh request sent from
    /// the server to the client.
    ///
    /// Note that this event is global and will force the client to refresh all
    /// semantic tokens currently shown. It should be used with absolute care
    /// and is useful for situation where a server for example detect a project
    /// wide change that requires such a calculation.
    /// </summary>
    [JsonProperty(PropertyName = "refreshSupport", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? RefreshSupport { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class CodeLensWorkspaceClientCapabilities
{
    /// <summary>
    /// Whether the client implementation supports a refresh request sent from the
    /// server to the client.
    ///
    /// Note that this event is global and will force the client to refresh all
    /// code lenses currently shown. It should be used with absolute care and is
    /// useful for situation where a server for example detect a project wide
    /// change that requires such a calculation.
    /// </summary>
    [JsonProperty(PropertyName = "refreshSupport", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? RefreshSupport { get; set; }
}

/// <summary>
/// Client workspace capabilities specific to inline values.
///
/// @since 3.17.0
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class InlineValueWorkspaceClientCapabilities
{
    /// <summary>
    /// Whether the client implementation supports a refresh request sent from
    /// the server to the client.
    ///
    /// Note that this event is global and will force the client to refresh all
    /// inline values currently shown. It should be used with absolute care and
    /// is useful for situation where a server for example detect a project wide
    /// change that requires such a calculation.
    /// </summary>
    [JsonProperty(PropertyName = "refreshSupport", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? RefreshSupport { get; set; }
}

/// <summary>
/// Client workspace capabilities specific to inlay hints.
///
/// @since 3.17.0
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class InlayHintWorkspaceClientCapabilities
{
    /// <summary>
    /// Whether the client implementation supports a refresh request sent from
    /// the server to the client.
    ///
    /// Note that this event is global and will force the client to refresh all
    /// inlay hints currently shown. It should be used with absolute care and
    /// is useful for situation where a server for example detects a project wide
    /// change that requires such a calculation.
    /// </summary>
    [JsonProperty(PropertyName = "refreshSupport", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? RefreshSupport { get; set; }
}

/// <summary>
/// Workspace client capabilities specific to diagnostic pull requests.
///
/// @since 3.17.0
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class DiagnosticWorkspaceClientCapabilities
{
    /// <summary>
    /// Whether the client implementation supports a refresh request sent from
    /// the server to the client.
    ///
    /// Note that this event is global and will force the client to refresh all
    /// pulled diagnostics currently shown. It should be used with absolute care
    /// and is useful for situation where a server for example detects a project
    /// wide change that requires such a calculation.
    /// </summary>
    [JsonProperty(PropertyName = "refreshSupport", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? RefreshSupport { get; set; }
}

/// <summary>
/// Text document specific client capabilities.
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class TextDocumentClientCapabilities
{
    [JsonProperty(PropertyName = "synchronization", NullValueHandling = NullValueHandling.Ignore)]
    public TextDocumentSyncClientCapabilities? Synchronization { get; set; }

    /// <summary>
    /// Capabilities specific to the `textDocument/completion` request.
    /// </summary>
    [JsonProperty(PropertyName = "completion", NullValueHandling = NullValueHandling.Ignore)]
    public CompletionClientCapabilities? Completion { get; set; }

    /// <summary>
    /// Capabilities specific to the `textDocument/hover` request.
    /// </summary>
    [JsonProperty(PropertyName = "hover", NullValueHandling = NullValueHandling.Ignore)]
    public HoverClientCapabilities? Hover { get; set; }

    /// <summary>
    /// Capabilities specific to the `textDocument/signatureHelp` request.
    /// </summary>
    [JsonProperty(PropertyName = "signatureHelp", NullValueHandling = NullValueHandling.Ignore)]
    public SignatureHelpClientCapabilities? SignatureHelp { get; set; }

    /// <summary>
    /// Capabilities specific to the `textDocument/declaration` request.
    ///
    /// @since 3.14.0
    /// </summary>
    [JsonProperty(PropertyName = "declaration", NullValueHandling = NullValueHandling.Ignore)]
    public DeclarationClientCapabilities? Declaration { get; set; }

    /// <summary>
    /// Capabilities specific to the `textDocument/definition` request.
    /// </summary>
    [JsonProperty(PropertyName = "definition", NullValueHandling = NullValueHandling.Ignore)]
    public DefinitionClientCapabilities? Definition { get; set; }

    /// <summary>
    /// Capabilities specific to the `textDocument/typeDefinition` request.
    ///
    /// @since 3.6.0
    /// </summary>
    [JsonProperty(PropertyName = "typeDefinition", NullValueHandling = NullValueHandling.Ignore)]
    public TypeDefinitionClientCapabilities? TypeDefinition { get; set; }

    /// <summary>
    /// Capabilities specific to the `textDocument/implementation` request.
    ///
    /// @since 3.6.0
    /// </summary>
    [JsonProperty(PropertyName = "implementation", NullValueHandling = NullValueHandling.Ignore)]
    public ImplementationClientCapabilities? Implementation { get; set; }

    /// <summary>
    /// Capabilities specific to the `textDocument/references` request.
    /// </summary>
    [JsonProperty(PropertyName = "references", NullValueHandling = NullValueHandling.Ignore)]
    public ReferenceClientCapabilities? References { get; set; }

    /// <summary>
    /// Capabilities specific to the `textDocument/documentHighlight` request.
    /// </summary>
    [JsonProperty(PropertyName = "documentHighlight", NullValueHandling = NullValueHandling.Ignore)]
    public DocumentHighlightClientCapabilities? DocumentHighlight { get; set; }

    /// <summary>
    /// Capabilities specific to the `textDocument/documentSymbol` request.
    /// </summary>
    [JsonProperty(PropertyName = "documentSymbol", NullValueHandling = NullValueHandling.Ignore)]
    public DocumentSymbolClientCapabilities? DocumentSymbol { get; set; }

    /// <summary>
    /// Capabilities specific to the `textDocument/codeAction` request.
    /// </summary>
    [JsonProperty(PropertyName = "codeAction", NullValueHandling = NullValueHandling.Ignore)]
    public CodeActionClientCapabilities? CodeAction { get; set; }

    /// <summary>
    /// Capabilities specific to the `textDocument/codeLens` request.
    /// </summary>
    [JsonProperty(PropertyName = "codeLens", NullValueHandling = NullValueHandling.Ignore)]
    public CodeLensClientCapabilities? CodeLens { get; set; }

    /// <summary>
    /// Capabilities specific to the `textDocument/documentLink` request.
    /// </summary>
    [JsonProperty(PropertyName = "documentLink", NullValueHandling = NullValueHandling.Ignore)]
    public DocumentLinkClientCapabilities? DocumentLink { get; set; }

    /// <summary>
    /// Capabilities specific to the `textDocument/documentColor` and the
    /// `textDocument/colorPresentation` request.
    ///
    /// @since 3.6.0
    /// </summary>
    [JsonProperty(PropertyName = "colorProvider", NullValueHandling = NullValueHandling.Ignore)]
    public DocumentColorClientCapabilities? ColorProvider { get; set; }

    /// <summary>
    /// Capabilities specific to the `textDocument/formatting` request.
    /// </summary>
    [JsonProperty(PropertyName = "formatting", NullValueHandling = NullValueHandling.Ignore)]
    public DocumentFormattingClientCapabilities? Formatting { get; set; }

    /// <summary>
    /// Capabilities specific to the `textDocument/rangeFormatting` request.
    /// </summary>
    [JsonProperty(PropertyName = "rangeFormatting", NullValueHandling = NullValueHandling.Ignore)]
    public DocumentRangeFormattingClientCapabilities? RangeFormatting { get; set; }

    [JsonProperty(PropertyName = "onTypeFormatting", NullValueHandling = NullValueHandling.Ignore)]
    public DocumentOnTypeFormattingClientCapabilities? OnTypeFormatting { get; set; }

    /// <summary>
    /// Capabilities specific to the `textDocument/rename` request.
    /// </summary>
    [JsonProperty(PropertyName = "rename", NullValueHandling = NullValueHandling.Ignore)]
    public RenameClientCapabilities? Rename { get; set; }

    /// <summary>
    /// Capabilities specific to the `textDocument/publishDiagnostics`
    /// notification.
    /// </summary>
    [JsonProperty(PropertyName = "publishDiagnostics", NullValueHandling = NullValueHandling.Ignore)]
    public PublishDiagnosticsClientCapabilities? PublishDiagnostics { get; set; }

    /// <summary>
    /// Capabilities specific to the `textDocument/foldingRange` request.
    ///
    /// @since 3.10.0
    /// </summary>
    [JsonProperty(PropertyName = "foldingRange", NullValueHandling = NullValueHandling.Ignore)]
    public FoldingRangeClientCapabilities? FoldingRange { get; set; }

    /// <summary>
    /// Capabilities specific to the `textDocument/selectionRange` request.
    ///
    /// @since 3.15.0
    /// </summary>
    [JsonProperty(PropertyName = "selectionRange", NullValueHandling = NullValueHandling.Ignore)]
    public SelectionRangeClientCapabilities? SelectionRange { get; set; }

    /// <summary>
    /// Capabilities specific to the `textDocument/linkedEditingRange` request.
    ///
    /// @since 3.16.0
    /// </summary>
    [JsonProperty(PropertyName = "linkedEditingRange", NullValueHandling = NullValueHandling.Ignore)]
    public LinkedEditingRangeClientCapabilities? LinkedEditingRange { get; set; }

    /// <summary>
    /// Capabilities specific to the various call hierarchy requests.
    ///
    /// @since 3.16.0
    /// </summary>
    [JsonProperty(PropertyName = "callHierarchy", NullValueHandling = NullValueHandling.Ignore)]
    public CallHierarchyClientCapabilities? CallHierarchy { get; set; }

    /// <summary>
    /// Capabilities specific to the various semantic token requests.
    ///
    /// @since 3.16.0
    /// </summary>
    [JsonProperty(PropertyName = "semanticTokens", NullValueHandling = NullValueHandling.Ignore)]
    public SemanticTokensClientCapabilities? SemanticTokens { get; set; }

    /// <summary>
    /// Capabilities specific to the `textDocument/moniker` request.
    ///
    /// @since 3.16.0
    /// </summary>
    [JsonProperty(PropertyName = "moniker", NullValueHandling = NullValueHandling.Ignore)]
    public MonikerClientCapabilities? Moniker { get; set; }

    /// <summary>
    /// Capabilities specific to the various type hierarchy requests.
    ///
    /// @since 3.17.0
    /// </summary>
    [JsonProperty(PropertyName = "typeHierarchy", NullValueHandling = NullValueHandling.Ignore)]
    public TypeHierarchyClientCapabilities? TypeHierarchy { get; set; }

    /// <summary>
    /// Capabilities specific to the `textDocument/inlineValue` request.
    ///
    /// @since 3.17.0
    /// </summary>
    [JsonProperty(PropertyName = "inlineValue", NullValueHandling = NullValueHandling.Ignore)]
    public InlineValueClientCapabilities? InlineValue { get; set; }

    /// <summary>
    /// Capabilities specific to the `textDocument/inlayHint` request.
    ///
    /// @since 3.17.0
    /// </summary>
    [JsonProperty(PropertyName = "inlayHint", NullValueHandling = NullValueHandling.Ignore)]
    public InlayHintClientCapabilities? InlayHint { get; set; }

    /// <summary>
    /// Capabilities specific to the diagnostic pull model.
    ///
    /// @since 3.17.0
    /// </summary>
    [JsonProperty(PropertyName = "diagnostic", NullValueHandling = NullValueHandling.Ignore)]
    public DiagnosticClientCapabilities? Diagnostic { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class TextDocumentSyncClientCapabilities
{
    /// <summary>
    /// Whether text document synchronization supports dynamic registration.
    /// </summary>
    [JsonProperty(PropertyName = "dynamicRegistration", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? DynamicRegistration { get; set; }

    /// <summary>
    /// The client supports sending will save notifications.
    /// </summary>
    [JsonProperty(PropertyName = "willSave", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WillSave { get; set; }

    /// <summary>
    /// The client supports sending a will save request and
    /// waits for a response providing text edits which will
    /// be applied to the document before it is saved.
    /// </summary>
    [JsonProperty(PropertyName = "willSaveWaitUntil", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WillSaveWaitUntil { get; set; }

    /// <summary>
    /// The client supports did save notifications.
    /// </summary>
    [JsonProperty(PropertyName = "didSave", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? DidSave { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class CompletionClientCapabilities
{
    [GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
    public sealed class TagSupportCapabilities
    {
        /// <summary>
        /// The tags supported by the client.
        /// </summary>
        [JsonProperty(PropertyName = "valueSet")]
        public IList<CompletionItemTag> ValueSet { get; set; }
    }

    [GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
    public sealed class ResolveSupportCapabilities
    {
        /// <summary>
        /// The properties that a client can resolve lazily.
        /// </summary>
        [JsonProperty(PropertyName = "properties")]
        public IList<String> Properties { get; set; }
    }

    [GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
    public sealed class InsertTextModeSupportCapabilities
    {
        [JsonProperty(PropertyName = "valueSet")]
        public IList<InsertTextMode> ValueSet { get; set; }
    }

    [GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
    public sealed class CompletionItemCapabilities
    {
        /// <summary>
        /// Client supports snippets as insert text.
        ///
        /// A snippet can define tab stops and placeholders with `$1`, `$2`
        /// and `${3:foo}`. `$0` defines the final tab stop, it defaults to
        /// the end of the snippet. Placeholders with equal identifiers are
        /// linked, that is typing in one will update others too.
        /// </summary>
        [JsonProperty(PropertyName = "snippetSupport", NullValueHandling = NullValueHandling.Ignore)]
        public Boolean? SnippetSupport { get; set; }

        /// <summary>
        /// Client supports commit characters on a completion item.
        /// </summary>
        [JsonProperty(PropertyName = "commitCharactersSupport", NullValueHandling = NullValueHandling.Ignore)]
        public Boolean? CommitCharactersSupport { get; set; }

        /// <summary>
        /// Client supports the follow content formats for the documentation
        /// property. The order describes the preferred format of the client.
        /// </summary>
        [JsonProperty(PropertyName = "documentationFormat", NullValueHandling = NullValueHandling.Ignore)]
        public IList<MarkupKind>? DocumentationFormat { get; set; }

        /// <summary>
        /// Client supports the deprecated property on a completion item.
        /// </summary>
        [JsonProperty(PropertyName = "deprecatedSupport", NullValueHandling = NullValueHandling.Ignore)]
        public Boolean? DeprecatedSupport { get; set; }

        /// <summary>
        /// Client supports the preselect property on a completion item.
        /// </summary>
        [JsonProperty(PropertyName = "preselectSupport", NullValueHandling = NullValueHandling.Ignore)]
        public Boolean? PreselectSupport { get; set; }

        /// <summary>
        /// Client supports the tag property on a completion item. Clients
        /// supporting tags have to handle unknown tags gracefully. Clients
        /// especially need to preserve unknown tags when sending a completion
        /// item back to the server in a resolve call.
        ///
        /// @since 3.15.0
        /// </summary>
        [JsonProperty(PropertyName = "tagSupport", NullValueHandling = NullValueHandling.Ignore)]
        public CompletionClientCapabilities.TagSupportCapabilities? TagSupport { get; set; }

        /// <summary>
        /// Client supports insert replace edit to control different behavior if
        /// a completion item is inserted in the text or should replace text.
        ///
        /// @since 3.16.0
        /// </summary>
        [JsonProperty(PropertyName = "insertReplaceSupport", NullValueHandling = NullValueHandling.Ignore)]
        public Boolean? InsertReplaceSupport { get; set; }

        /// <summary>
        /// Indicates which properties a client can resolve lazily on a
        /// completion item. Before version 3.16.0 only the predefined properties
        /// `documentation` and `detail` could be resolved lazily.
        ///
        /// @since 3.16.0
        /// </summary>
        [JsonProperty(PropertyName = "resolveSupport", NullValueHandling = NullValueHandling.Ignore)]
        public CompletionClientCapabilities.ResolveSupportCapabilities? ResolveSupport { get; set; }

        /// <summary>
        /// The client supports the `insertTextMode` property on
        /// a completion item to override the whitespace handling mode
        /// as defined by the client (see `insertTextMode`).
        ///
        /// @since 3.16.0
        /// </summary>
        [JsonProperty(PropertyName = "insertTextModeSupport", NullValueHandling = NullValueHandling.Ignore)]
        public CompletionClientCapabilities.InsertTextModeSupportCapabilities? InsertTextModeSupport { get; set; }

        /// <summary>
        /// The client has support for completion item label
        /// details (see also `CompletionItemLabelDetails`).
        ///
        /// @since 3.17.0
        /// </summary>
        [JsonProperty(PropertyName = "labelDetailsSupport", NullValueHandling = NullValueHandling.Ignore)]
        public Boolean? LabelDetailsSupport { get; set; }
    }

    [GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
    public sealed class CompletionItemKindCapabilities
    {
        /// <summary>
        /// The completion item kind values the client supports. When this
        /// property exists the client also guarantees that it will
        /// handle values outside its set gracefully and falls back
        /// to a default value when unknown.
        ///
        /// If this property is not present the client only supports
        /// the completion items kinds from `Text` to `Reference` as defined in
        /// the initial version of the protocol.
        /// </summary>
        [JsonProperty(PropertyName = "valueSet", NullValueHandling = NullValueHandling.Ignore)]
        public IList<CompletionItemKind>? ValueSet { get; set; }
    }

    [GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
    public sealed class CompletionListCapabilities
    {
        /// <summary>
        /// The client supports the following itemDefaults on
        /// a completion list.
        ///
        /// The value lists the supported property names of the
        /// `CompletionList.itemDefaults` object. If omitted
        /// no properties are supported.
        ///
        /// @since 3.17.0
        /// </summary>
        [JsonProperty(PropertyName = "itemDefaults", NullValueHandling = NullValueHandling.Ignore)]
        public IList<String>? ItemDefaults { get; set; }
    }

    /// <summary>
    /// Whether completion supports dynamic registration.
    /// </summary>
    [JsonProperty(PropertyName = "dynamicRegistration", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? DynamicRegistration { get; set; }

    /// <summary>
    /// The client supports the following `CompletionItem` specific
    /// capabilities.
    /// </summary>
    [JsonProperty(PropertyName = "completionItem", NullValueHandling = NullValueHandling.Ignore)]
    public CompletionClientCapabilities.CompletionItemCapabilities? CompletionItem { get; set; }

    [JsonProperty(PropertyName = "completionItemKind", NullValueHandling = NullValueHandling.Ignore)]
    public CompletionClientCapabilities.CompletionItemKindCapabilities? CompletionItemKind { get; set; }

    /// <summary>
    /// The client supports to send additional context information for a
    /// `textDocument/completion` request.
    /// </summary>
    [JsonProperty(PropertyName = "contextSupport", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? ContextSupport { get; set; }

    /// <summary>
    /// The client's default when the completion item doesn't provide a
    /// `insertTextMode` property.
    ///
    /// @since 3.17.0
    /// </summary>
    [JsonProperty(PropertyName = "insertTextMode", NullValueHandling = NullValueHandling.Ignore)]
    public InsertTextMode? InsertTextMode { get; set; }

    /// <summary>
    /// The client supports the following `CompletionList` specific
    /// capabilities.
    ///
    /// @since 3.17.0
    /// </summary>
    [JsonProperty(PropertyName = "completionList", NullValueHandling = NullValueHandling.Ignore)]
    public CompletionClientCapabilities.CompletionListCapabilities? CompletionList { get; set; }
}

/// <summary>
/// Describes the content type that a client supports in various
/// result literals like `Hover`, `ParameterInfo` or `CompletionItem`.
///
/// Please note that `MarkupKinds` must not start with a `$`. This kinds
/// are reserved for internal usage.
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public enum MarkupKind
{
    /// <summary>
    /// Plain text is supported as a content format
    /// </summary>
    [EnumMember(Value = "plaintext")]
    PlainText,
    /// <summary>
    /// Markdown is supported as a content format
    /// </summary>
    [EnumMember(Value = "markdown")]
    Markdown,
}

/// <summary>
/// Completion item tags are extra annotations that tweak the rendering of a
/// completion item.
///
/// @since 3.15.0
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public enum CompletionItemTag
{
    /// <summary>
    /// Render a completion as obsolete, usually using a strike-out.
    /// </summary>
    [EnumMember(Value = "1")]
    Deprecated,
}

/// <summary>
/// How whitespace and indentation is handled during completion
/// item insertion.
///
/// @since 3.16.0
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public enum InsertTextMode
{
    /// <summary>
    /// The insertion or replace strings is taken as it is. If the
    /// value is multi line the lines below the cursor will be
    /// inserted using the indentation defined in the string value.
    /// The client will not apply any kind of adjustments to the
    /// string.
    /// </summary>
    [EnumMember(Value = "1")]
    AsIs,
    /// <summary>
    /// The editor adjusts leading whitespace of new lines so that
    /// they match the indentation up to the cursor of the line for
    /// which the item is accepted.
    ///
    /// Consider a line like this: < 2 tabs><cursor>< 3 tabs>foo. Accepting a
    /// multi line completion item is indented using 2 tabs and all
    /// following lines inserted will be indented using 2 tabs as well.
    /// </summary>
    [EnumMember(Value = "2")]
    AdjustIndentation,
}

/// <summary>
/// The kind of a completion entry.
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public enum CompletionItemKind
{
    [EnumMember(Value = "1")]
    Text,
    [EnumMember(Value = "2")]
    Method,
    [EnumMember(Value = "3")]
    Function,
    [EnumMember(Value = "4")]
    Constructor,
    [EnumMember(Value = "5")]
    Field,
    [EnumMember(Value = "6")]
    Variable,
    [EnumMember(Value = "7")]
    Class,
    [EnumMember(Value = "8")]
    Interface,
    [EnumMember(Value = "9")]
    Module,
    [EnumMember(Value = "10")]
    Property,
    [EnumMember(Value = "11")]
    Unit,
    [EnumMember(Value = "12")]
    Value,
    [EnumMember(Value = "13")]
    Enum,
    [EnumMember(Value = "14")]
    Keyword,
    [EnumMember(Value = "15")]
    Snippet,
    [EnumMember(Value = "16")]
    Color,
    [EnumMember(Value = "17")]
    File,
    [EnumMember(Value = "18")]
    Reference,
    [EnumMember(Value = "19")]
    Folder,
    [EnumMember(Value = "20")]
    EnumMember,
    [EnumMember(Value = "21")]
    Constant,
    [EnumMember(Value = "22")]
    Struct,
    [EnumMember(Value = "23")]
    Event,
    [EnumMember(Value = "24")]
    Operator,
    [EnumMember(Value = "25")]
    TypeParameter,
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class HoverClientCapabilities
{
    /// <summary>
    /// Whether hover supports dynamic registration.
    /// </summary>
    [JsonProperty(PropertyName = "dynamicRegistration", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? DynamicRegistration { get; set; }

    /// <summary>
    /// Client supports the follow content formats if the content
    /// property refers to a `literal of type MarkupContent`.
    /// The order describes the preferred format of the client.
    /// </summary>
    [JsonProperty(PropertyName = "contentFormat", NullValueHandling = NullValueHandling.Ignore)]
    public IList<MarkupKind>? ContentFormat { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class SignatureHelpClientCapabilities
{
    [GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
    public sealed class ParameterInformationCapabilities
    {
        /// <summary>
        /// The client supports processing label offsets instead of a
        /// simple label string.
        ///
        /// @since 3.14.0
        /// </summary>
        [JsonProperty(PropertyName = "labelOffsetSupport", NullValueHandling = NullValueHandling.Ignore)]
        public Boolean? LabelOffsetSupport { get; set; }
    }

    [GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
    public sealed class SignatureInformationCapabilities
    {
        /// <summary>
        /// Client supports the follow content formats for the documentation
        /// property. The order describes the preferred format of the client.
        /// </summary>
        [JsonProperty(PropertyName = "documentationFormat", NullValueHandling = NullValueHandling.Ignore)]
        public IList<MarkupKind>? DocumentationFormat { get; set; }

        /// <summary>
        /// Client capabilities specific to parameter information.
        /// </summary>
        [JsonProperty(PropertyName = "parameterInformation", NullValueHandling = NullValueHandling.Ignore)]
        public SignatureHelpClientCapabilities.ParameterInformationCapabilities? ParameterInformation { get; set; }

        /// <summary>
        /// The client supports the `activeParameter` property on
        /// `SignatureInformation` literal.
        ///
        /// @since 3.16.0
        /// </summary>
        [JsonProperty(PropertyName = "activeParameterSupport", NullValueHandling = NullValueHandling.Ignore)]
        public Boolean? ActiveParameterSupport { get; set; }
    }

    /// <summary>
    /// Whether signature help supports dynamic registration.
    /// </summary>
    [JsonProperty(PropertyName = "dynamicRegistration", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? DynamicRegistration { get; set; }

    /// <summary>
    /// The client supports the following `SignatureInformation`
    /// specific properties.
    /// </summary>
    [JsonProperty(PropertyName = "signatureInformation", NullValueHandling = NullValueHandling.Ignore)]
    public SignatureHelpClientCapabilities.SignatureInformationCapabilities? SignatureInformation { get; set; }

    /// <summary>
    /// The client supports to send additional context information for a
    /// `textDocument/signatureHelp` request. A client that opts into
    /// contextSupport will also support the `retriggerCharacters` on
    /// `SignatureHelpOptions`.
    ///
    /// @since 3.15.0
    /// </summary>
    [JsonProperty(PropertyName = "contextSupport", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? ContextSupport { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class DeclarationClientCapabilities
{
    /// <summary>
    /// Whether declaration supports dynamic registration. If this is set to
    /// `true` the client supports the new `DeclarationRegistrationOptions`
    /// return value for the corresponding server capability as well.
    /// </summary>
    [JsonProperty(PropertyName = "dynamicRegistration", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? DynamicRegistration { get; set; }

    /// <summary>
    /// The client supports additional metadata in the form of declaration links.
    /// </summary>
    [JsonProperty(PropertyName = "linkSupport", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? LinkSupport { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class DefinitionClientCapabilities
{
    /// <summary>
    /// Whether definition supports dynamic registration.
    /// </summary>
    [JsonProperty(PropertyName = "dynamicRegistration", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? DynamicRegistration { get; set; }

    /// <summary>
    /// The client supports additional metadata in the form of definition links.
    ///
    /// @since 3.14.0
    /// </summary>
    [JsonProperty(PropertyName = "linkSupport", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? LinkSupport { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class TypeDefinitionClientCapabilities
{
    /// <summary>
    /// Whether implementation supports dynamic registration. If this is set to
    /// `true` the client supports the new `TypeDefinitionRegistrationOptions`
    /// return value for the corresponding server capability as well.
    /// </summary>
    [JsonProperty(PropertyName = "dynamicRegistration", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? DynamicRegistration { get; set; }

    /// <summary>
    /// The client supports additional metadata in the form of definition links.
    ///
    /// @since 3.14.0
    /// </summary>
    [JsonProperty(PropertyName = "linkSupport", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? LinkSupport { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class ImplementationClientCapabilities
{
    /// <summary>
    /// Whether implementation supports dynamic registration. If this is set to
    /// `true` the client supports the new `ImplementationRegistrationOptions`
    /// return value for the corresponding server capability as well.
    /// </summary>
    [JsonProperty(PropertyName = "dynamicRegistration", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? DynamicRegistration { get; set; }

    /// <summary>
    /// The client supports additional metadata in the form of definition links.
    ///
    /// @since 3.14.0
    /// </summary>
    [JsonProperty(PropertyName = "linkSupport", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? LinkSupport { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class ReferenceClientCapabilities
{
    /// <summary>
    /// Whether references supports dynamic registration.
    /// </summary>
    [JsonProperty(PropertyName = "dynamicRegistration", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? DynamicRegistration { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class DocumentHighlightClientCapabilities
{
    /// <summary>
    /// Whether document highlight supports dynamic registration.
    /// </summary>
    [JsonProperty(PropertyName = "dynamicRegistration", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? DynamicRegistration { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class DocumentSymbolClientCapabilities
{
    [GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
    public sealed class SymbolKindCapabilities
    {
        /// <summary>
        /// The symbol kind values the client supports. When this
        /// property exists the client also guarantees that it will
        /// handle values outside its set gracefully and falls back
        /// to a default value when unknown.
        ///
        /// If this property is not present the client only supports
        /// the symbol kinds from `File` to `Array` as defined in
        /// the initial version of the protocol.
        /// </summary>
        [JsonProperty(PropertyName = "valueSet", NullValueHandling = NullValueHandling.Ignore)]
        public IList<SymbolKind>? ValueSet { get; set; }
    }

    [GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
    public sealed class TagSupportCapabilities
    {
        /// <summary>
        /// The tags supported by the client.
        /// </summary>
        [JsonProperty(PropertyName = "valueSet")]
        public IList<SymbolTag> ValueSet { get; set; }
    }

    /// <summary>
    /// Whether document symbol supports dynamic registration.
    /// </summary>
    [JsonProperty(PropertyName = "dynamicRegistration", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? DynamicRegistration { get; set; }

    /// <summary>
    /// Specific capabilities for the `SymbolKind` in the
    /// `textDocument/documentSymbol` request.
    /// </summary>
    [JsonProperty(PropertyName = "symbolKind", NullValueHandling = NullValueHandling.Ignore)]
    public DocumentSymbolClientCapabilities.SymbolKindCapabilities? SymbolKind { get; set; }

    /// <summary>
    /// The client supports hierarchical document symbols.
    /// </summary>
    [JsonProperty(PropertyName = "hierarchicalDocumentSymbolSupport", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? HierarchicalDocumentSymbolSupport { get; set; }

    /// <summary>
    /// The client supports tags on `SymbolInformation`. Tags are supported on
    /// `DocumentSymbol` if `hierarchicalDocumentSymbolSupport` is set to true.
    /// Clients supporting tags have to handle unknown tags gracefully.
    ///
    /// @since 3.16.0
    /// </summary>
    [JsonProperty(PropertyName = "tagSupport", NullValueHandling = NullValueHandling.Ignore)]
    public DocumentSymbolClientCapabilities.TagSupportCapabilities? TagSupport { get; set; }

    /// <summary>
    /// The client supports an additional label presented in the UI when
    /// registering a document symbol provider.
    ///
    /// @since 3.16.0
    /// </summary>
    [JsonProperty(PropertyName = "labelSupport", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? LabelSupport { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class CodeActionClientCapabilities
{
    [GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
    public sealed class CodeActionKindCapabilities
    {
        /// <summary>
        /// The code action kind values the client supports. When this
        /// property exists the client also guarantees that it will
        /// handle values outside its set gracefully and falls back
        /// to a default value when unknown.
        /// </summary>
        [JsonProperty(PropertyName = "valueSet")]
        public IList<CodeActionKind> ValueSet { get; set; }
    }

    [GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
    public sealed class CodeActionLiteralSupportCapabilities
    {
        /// <summary>
        /// The code action kind is supported with the following value
        /// set.
        /// </summary>
        [JsonProperty(PropertyName = "codeActionKind")]
        public CodeActionClientCapabilities.CodeActionKindCapabilities CodeActionKind { get; set; }
    }

    [GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
    public sealed class ResolveSupportCapabilities
    {
        /// <summary>
        /// The properties that a client can resolve lazily.
        /// </summary>
        [JsonProperty(PropertyName = "properties")]
        public IList<String> Properties { get; set; }
    }

    /// <summary>
    /// Whether code action supports dynamic registration.
    /// </summary>
    [JsonProperty(PropertyName = "dynamicRegistration", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? DynamicRegistration { get; set; }

    /// <summary>
    /// The client supports code action literals as a valid
    /// response of the `textDocument/codeAction` request.
    ///
    /// @since 3.8.0
    /// </summary>
    [JsonProperty(PropertyName = "codeActionLiteralSupport", NullValueHandling = NullValueHandling.Ignore)]
    public CodeActionClientCapabilities.CodeActionLiteralSupportCapabilities? CodeActionLiteralSupport { get; set; }

    /// <summary>
    /// Whether code action supports the `isPreferred` property.
    ///
    /// @since 3.15.0
    /// </summary>
    [JsonProperty(PropertyName = "isPreferredSupport", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? IsPreferredSupport { get; set; }

    /// <summary>
    /// Whether code action supports the `disabled` property.
    ///
    /// @since 3.16.0
    /// </summary>
    [JsonProperty(PropertyName = "disabledSupport", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? DisabledSupport { get; set; }

    /// <summary>
    /// Whether code action supports the `data` property which is
    /// preserved between a `textDocument/codeAction` and a
    /// `codeAction/resolve` request.
    ///
    /// @since 3.16.0
    /// </summary>
    [JsonProperty(PropertyName = "dataSupport", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? DataSupport { get; set; }

    /// <summary>
    /// Whether the client supports resolving additional code action
    /// properties via a separate `codeAction/resolve` request.
    ///
    /// @since 3.16.0
    /// </summary>
    [JsonProperty(PropertyName = "resolveSupport", NullValueHandling = NullValueHandling.Ignore)]
    public CodeActionClientCapabilities.ResolveSupportCapabilities? ResolveSupport { get; set; }

    /// <summary>
    /// Whether the client honors the change annotations in
    /// text edits and resource operations returned via the
    /// `CodeAction#edit` property by for example presenting
    /// the workspace edit in the user interface and asking
    /// for confirmation.
    ///
    /// @since 3.16.0
    /// </summary>
    [JsonProperty(PropertyName = "honorsChangeAnnotations", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? HonorsChangeAnnotations { get; set; }
}

/// <summary>
/// A set of predefined code action kinds.
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public enum CodeActionKind
{
    /// <summary>
    /// Empty kind.
    /// </summary>
    [EnumMember(Value = "")]
    Empty,
    /// <summary>
    /// Base kind for quickfix actions: 'quickfix'.
    /// </summary>
    [EnumMember(Value = "quickfix")]
    QuickFix,
    /// <summary>
    /// Base kind for refactoring actions: 'refactor'.
    /// </summary>
    [EnumMember(Value = "refactor")]
    Refactor,
    /// <summary>
    /// Base kind for refactoring extraction actions: 'refactor.extract'.
    ///
    /// Example extract actions:
    ///
    /// - Extract method
    /// - Extract function
    /// - Extract variable
    /// - Extract interface from class
    /// - ...
    /// </summary>
    [EnumMember(Value = "refactor.extract")]
    RefactorExtract,
    /// <summary>
    /// Base kind for refactoring inline actions: 'refactor.inline'.
    ///
    /// Example inline actions:
    ///
    /// - Inline function
    /// - Inline variable
    /// - Inline constant
    /// - ...
    /// </summary>
    [EnumMember(Value = "refactor.inline")]
    RefactorInline,
    /// <summary>
    /// Base kind for refactoring rewrite actions: 'refactor.rewrite'.
    ///
    /// Example rewrite actions:
    ///
    /// - Convert JavaScript function to class
    /// - Add or remove parameter
    /// - Encapsulate field
    /// - Make method static
    /// - Move method to base class
    /// - ...
    /// </summary>
    [EnumMember(Value = "refactor.rewrite")]
    RefactorRewrite,
    /// <summary>
    /// Base kind for source actions: `source`.
    ///
    /// Source code actions apply to the entire file.
    /// </summary>
    [EnumMember(Value = "source")]
    Source,
    /// <summary>
    /// Base kind for an organize imports source action:
    /// `source.organizeImports`.
    /// </summary>
    [EnumMember(Value = "source.organizeImports")]
    SourceOrganizeImports,
    /// <summary>
    /// Base kind for a 'fix all' source action: `source.fixAll`.
    ///
    /// 'Fix all' actions automatically fix errors that have a clear fix that
    /// do not require user input. They should not suppress errors or perform
    /// unsafe fixes such as generating new types or classes.
    ///
    /// @since 3.17.0
    /// </summary>
    [EnumMember(Value = "source.fixAll")]
    SourceFixAll,
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class CodeLensClientCapabilities
{
    /// <summary>
    /// Whether code lens supports dynamic registration.
    /// </summary>
    [JsonProperty(PropertyName = "dynamicRegistration", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? DynamicRegistration { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class DocumentLinkClientCapabilities
{
    /// <summary>
    /// Whether document link supports dynamic registration.
    /// </summary>
    [JsonProperty(PropertyName = "dynamicRegistration", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? DynamicRegistration { get; set; }

    /// <summary>
    /// Whether the client supports the `tooltip` property on `DocumentLink`.
    ///
    /// @since 3.15.0
    /// </summary>
    [JsonProperty(PropertyName = "tooltipSupport", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? TooltipSupport { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class DocumentColorClientCapabilities
{
    /// <summary>
    /// Whether document color supports dynamic registration.
    /// </summary>
    [JsonProperty(PropertyName = "dynamicRegistration", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? DynamicRegistration { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class DocumentFormattingClientCapabilities
{
    /// <summary>
    /// Whether formatting supports dynamic registration.
    /// </summary>
    [JsonProperty(PropertyName = "dynamicRegistration", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? DynamicRegistration { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class DocumentRangeFormattingClientCapabilities
{
    /// <summary>
    /// Whether formatting supports dynamic registration.
    /// </summary>
    [JsonProperty(PropertyName = "dynamicRegistration", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? DynamicRegistration { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class DocumentOnTypeFormattingClientCapabilities
{
    /// <summary>
    /// Whether on type formatting supports dynamic registration.
    /// </summary>
    [JsonProperty(PropertyName = "dynamicRegistration", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? DynamicRegistration { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class RenameClientCapabilities
{
    /// <summary>
    /// Whether rename supports dynamic registration.
    /// </summary>
    [JsonProperty(PropertyName = "dynamicRegistration", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? DynamicRegistration { get; set; }

    /// <summary>
    /// Client supports testing for validity of rename operations
    /// before execution.
    ///
    /// @since version 3.12.0
    /// </summary>
    [JsonProperty(PropertyName = "prepareSupport", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? PrepareSupport { get; set; }

    /// <summary>
    /// Client supports the default behavior result
    /// (`{ defaultBehavior: boolean }`).
    ///
    /// The value indicates the default behavior used by the
    /// client.
    ///
    /// @since version 3.16.0
    /// </summary>
    [JsonProperty(PropertyName = "prepareSupportDefaultBehavior", NullValueHandling = NullValueHandling.Ignore)]
    public PrepareSupportDefaultBehavior? PrepareSupportDefaultBehavior { get; set; }

    /// <summary>
    /// Whether the client honors the change annotations in
    /// text edits and resource operations returned via the
    /// rename request's workspace edit by for example presenting
    /// the workspace edit in the user interface and asking
    /// for confirmation.
    ///
    /// @since 3.16.0
    /// </summary>
    [JsonProperty(PropertyName = "honorsChangeAnnotations", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? HonorsChangeAnnotations { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public enum PrepareSupportDefaultBehavior
{
    /// <summary>
    /// The client's default behavior is to select the identifier
    /// according to the language's syntax rule.
    /// </summary>
    [EnumMember(Value = "1")]
    Identifier,
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class PublishDiagnosticsClientCapabilities
{
    [GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
    public sealed class TagSupportCapabilities
    {
        /// <summary>
        /// The tags supported by the client.
        /// </summary>
        [JsonProperty(PropertyName = "valueSet")]
        public IList<DiagnosticTag> ValueSet { get; set; }
    }

    /// <summary>
    /// Whether the clients accepts diagnostics with related information.
    /// </summary>
    [JsonProperty(PropertyName = "relatedInformation", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? RelatedInformation { get; set; }

    /// <summary>
    /// Client supports the tag property to provide meta data about a diagnostic.
    /// Clients supporting tags have to handle unknown tags gracefully.
    ///
    /// @since 3.15.0
    /// </summary>
    [JsonProperty(PropertyName = "tagSupport", NullValueHandling = NullValueHandling.Ignore)]
    public PublishDiagnosticsClientCapabilities.TagSupportCapabilities? TagSupport { get; set; }

    /// <summary>
    /// Whether the client interprets the version property of the
    /// `textDocument/publishDiagnostics` notification's parameter.
    ///
    /// @since 3.15.0
    /// </summary>
    [JsonProperty(PropertyName = "versionSupport", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? VersionSupport { get; set; }

    /// <summary>
    /// Client supports a codeDescription property
    ///
    /// @since 3.16.0
    /// </summary>
    [JsonProperty(PropertyName = "codeDescriptionSupport", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? CodeDescriptionSupport { get; set; }

    /// <summary>
    /// Whether code action supports the `data` property which is
    /// preserved between a `textDocument/publishDiagnostics` and
    /// `textDocument/codeAction` request.
    ///
    /// @since 3.16.0
    /// </summary>
    [JsonProperty(PropertyName = "dataSupport", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? DataSupport { get; set; }
}

/// <summary>
/// The diagnostic tags.
///
/// @since 3.15.0
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public enum DiagnosticTag
{
    /// <summary>
    /// Unused or unnecessary code.
    ///
    /// Clients are allowed to render diagnostics with this tag faded out
    /// instead of having an error squiggle.
    /// </summary>
    [EnumMember(Value = "1")]
    Unnecessary,
    /// <summary>
    /// Deprecated or obsolete code.
    ///
    /// Clients are allowed to rendered diagnostics with this tag strike through.
    /// </summary>
    [EnumMember(Value = "2")]
    Deprecated,
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class FoldingRangeClientCapabilities
{
    [GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
    public sealed class FoldingRangeKindCapabilities
    {
        /// <summary>
        /// The folding range kind values the client supports. When this
        /// property exists the client also guarantees that it will
        /// handle values outside its set gracefully and falls back
        /// to a default value when unknown.
        /// </summary>
        [JsonProperty(PropertyName = "valueSet", NullValueHandling = NullValueHandling.Ignore)]
        public IList<FoldingRangeKind>? ValueSet { get; set; }
    }

    [GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
    public sealed class FoldingRangeCapabilities
    {
        /// <summary>
        /// If set, the client signals that it supports setting collapsedText on
        /// folding ranges to display custom labels instead of the default text.
        ///
        /// @since 3.17.0
        /// </summary>
        [JsonProperty(PropertyName = "collapsedText", NullValueHandling = NullValueHandling.Ignore)]
        public Boolean? CollapsedText { get; set; }
    }

    /// <summary>
    /// Whether implementation supports dynamic registration for folding range
    /// providers. If this is set to `true` the client supports the new
    /// `FoldingRangeRegistrationOptions` return value for the corresponding
    /// server capability as well.
    /// </summary>
    [JsonProperty(PropertyName = "dynamicRegistration", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? DynamicRegistration { get; set; }

    /// <summary>
    /// The maximum number of folding ranges that the client prefers to receive
    /// per document. The value serves as a hint, servers are free to follow the
    /// limit.
    /// </summary>
    [JsonProperty(PropertyName = "rangeLimit", NullValueHandling = NullValueHandling.Ignore)]
    public UInt32? RangeLimit { get; set; }

    /// <summary>
    /// If set, the client signals that it only supports folding complete lines.
    /// If set, client will ignore specified `startCharacter` and `endCharacter`
    /// properties in a FoldingRange.
    /// </summary>
    [JsonProperty(PropertyName = "lineFoldingOnly", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? LineFoldingOnly { get; set; }

    /// <summary>
    /// Specific options for the folding range kind.
    ///
    /// @since 3.17.0
    /// </summary>
    [JsonProperty(PropertyName = "foldingRangeKind", NullValueHandling = NullValueHandling.Ignore)]
    public FoldingRangeClientCapabilities.FoldingRangeKindCapabilities? FoldingRangeKind { get; set; }

    /// <summary>
    /// Specific options for the folding range.
    /// @since 3.17.0
    /// </summary>
    [JsonProperty(PropertyName = "foldingRange", NullValueHandling = NullValueHandling.Ignore)]
    public FoldingRangeClientCapabilities.FoldingRangeCapabilities? FoldingRange { get; set; }
}

/// <summary>
/// A set of predefined range kinds.
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public enum FoldingRangeKind
{
    /// <summary>
    /// Folding range for a comment
    /// </summary>
    [EnumMember(Value = "comment")]
    Comment,
    /// <summary>
    /// Folding range for imports or includes
    /// </summary>
    [EnumMember(Value = "imports")]
    Imports,
    /// <summary>
    /// Folding range for a region (e.g. `#region`)
    /// </summary>
    [EnumMember(Value = "region")]
    Region,
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class SelectionRangeClientCapabilities
{
    /// <summary>
    /// Whether implementation supports dynamic registration for selection range
    /// providers. If this is set to `true` the client supports the new
    /// `SelectionRangeRegistrationOptions` return value for the corresponding
    /// server capability as well.
    /// </summary>
    [JsonProperty(PropertyName = "dynamicRegistration", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? DynamicRegistration { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class LinkedEditingRangeClientCapabilities
{
    /// <summary>
    /// Whether the implementation supports dynamic registration.
    /// If this is set to `true` the client supports the new
    /// `(TextDocumentRegistrationOptions & StaticRegistrationOptions)`
    /// return value for the corresponding server capability as well.
    /// </summary>
    [JsonProperty(PropertyName = "dynamicRegistration", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? DynamicRegistration { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class CallHierarchyClientCapabilities
{
    /// <summary>
    /// Whether implementation supports dynamic registration. If this is set to
    /// `true` the client supports the new `(TextDocumentRegistrationOptions &
    /// StaticRegistrationOptions)` return value for the corresponding server
    /// capability as well.
    /// </summary>
    [JsonProperty(PropertyName = "dynamicRegistration", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? DynamicRegistration { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class SemanticTokensClientCapabilities
{
    [GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
    public sealed class RangeCapabilities
    {
    }

    [GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
    public sealed class FullCapabilities
    {
        /// <summary>
        /// The client will send the `textDocument/semanticTokens/full/delta`
        /// request if the server provides a corresponding handler.
        /// </summary>
        [JsonProperty(PropertyName = "delta", NullValueHandling = NullValueHandling.Ignore)]
        public Boolean? Delta { get; set; }
    }

    [GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
    public sealed class RequestsCapabilities
    {
        /// <summary>
        /// The client will send the `textDocument/semanticTokens/range` request
        /// if the server provides a corresponding handler.
        /// </summary>
        [JsonProperty(PropertyName = "range", NullValueHandling = NullValueHandling.Ignore)]
        public OneOf<Boolean, SemanticTokensClientCapabilities.RangeCapabilities>? Range { get; set; }

        /// <summary>
        /// The client will send the `textDocument/semanticTokens/full` request
        /// if the server provides a corresponding handler.
        /// </summary>
        [JsonProperty(PropertyName = "full", NullValueHandling = NullValueHandling.Ignore)]
        public OneOf<Boolean, SemanticTokensClientCapabilities.FullCapabilities>? Full { get; set; }
    }

    /// <summary>
    /// Whether implementation supports dynamic registration. If this is set to
    /// `true` the client supports the new `(TextDocumentRegistrationOptions &
    /// StaticRegistrationOptions)` return value for the corresponding server
    /// capability as well.
    /// </summary>
    [JsonProperty(PropertyName = "dynamicRegistration", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? DynamicRegistration { get; set; }

    /// <summary>
    /// Which requests the client supports and might send to the server
    /// depending on the server's capability. Please note that clients might not
    /// show semantic tokens or degrade some of the user experience if a range
    /// or full request is advertised by the client but not provided by the
    /// server. If for example the client capability `requests.full` and
    /// `request.range` are both set to true but the server only provides a
    /// range provider the client might not render a minimap correctly or might
    /// even decide to not show any semantic tokens at all.
    /// </summary>
    [JsonProperty(PropertyName = "requests")]
    public SemanticTokensClientCapabilities.RequestsCapabilities Requests { get; set; }

    /// <summary>
    /// The token types that the client supports.
    /// </summary>
    [JsonProperty(PropertyName = "tokenTypes")]
    public IList<String> TokenTypes { get; set; }

    /// <summary>
    /// The token modifiers that the client supports.
    /// </summary>
    [JsonProperty(PropertyName = "tokenModifiers")]
    public IList<String> TokenModifiers { get; set; }

    /// <summary>
    /// The formats the clients supports.
    /// </summary>
    [JsonProperty(PropertyName = "formats")]
    public IList<TokenFormat> Formats { get; set; }

    /// <summary>
    /// Whether the client supports tokens that can overlap each other.
    /// </summary>
    [JsonProperty(PropertyName = "overlappingTokenSupport", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? OverlappingTokenSupport { get; set; }

    /// <summary>
    /// Whether the client supports tokens that can span multiple lines.
    /// </summary>
    [JsonProperty(PropertyName = "multilineTokenSupport", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? MultilineTokenSupport { get; set; }

    /// <summary>
    /// Whether the client allows the server to actively cancel a
    /// semantic token request, e.g. supports returning
    /// ErrorCodes.ServerCancelled. If a server does the client
    /// needs to retrigger the request.
    ///
    /// @since 3.17.0
    /// </summary>
    [JsonProperty(PropertyName = "serverCancelSupport", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? ServerCancelSupport { get; set; }

    /// <summary>
    /// Whether the client uses semantic tokens to augment existing
    /// syntax tokens. If set to `true` client side created syntax
    /// tokens and semantic tokens are both used for colorization. If
    /// set to `false` the client only uses the returned semantic tokens
    /// for colorization.
    ///
    /// If the value is `undefined` then the client behavior is not
    /// specified.
    ///
    /// @since 3.17.0
    /// </summary>
    [JsonProperty(PropertyName = "augmentsSyntaxTokens", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? AugmentsSyntaxTokens { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public enum TokenFormat
{
    [EnumMember(Value = "relative")]
    Relative,
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class MonikerClientCapabilities
{
    /// <summary>
    /// Whether implementation supports dynamic registration. If this is set to
    /// `true` the client supports the new `(TextDocumentRegistrationOptions &
    /// StaticRegistrationOptions)` return value for the corresponding server
    /// capability as well.
    /// </summary>
    [JsonProperty(PropertyName = "dynamicRegistration", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? DynamicRegistration { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class TypeHierarchyClientCapabilities
{
    /// <summary>
    /// Whether implementation supports dynamic registration. If this is set to
    /// `true` the client supports the new `(TextDocumentRegistrationOptions &
    /// StaticRegistrationOptions)` return value for the corresponding server
    /// capability as well.
    /// </summary>
    [JsonProperty(PropertyName = "dynamicRegistration", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? DynamicRegistration { get; set; }
}

/// <summary>
/// Client capabilities specific to inline values.
///
/// @since 3.17.0
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class InlineValueClientCapabilities
{
    /// <summary>
    /// Whether implementation supports dynamic registration for inline
    /// value providers.
    /// </summary>
    [JsonProperty(PropertyName = "dynamicRegistration", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? DynamicRegistration { get; set; }
}

/// <summary>
/// Inlay hint client capabilities.
///
/// @since 3.17.0
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class InlayHintClientCapabilities
{
    [GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
    public sealed class ResolveSupportCapabilities
    {
        /// <summary>
        /// The properties that a client can resolve lazily.
        /// </summary>
        [JsonProperty(PropertyName = "properties")]
        public IList<String> Properties { get; set; }
    }

    /// <summary>
    /// Whether inlay hints support dynamic registration.
    /// </summary>
    [JsonProperty(PropertyName = "dynamicRegistration", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? DynamicRegistration { get; set; }

    /// <summary>
    /// Indicates which properties a client can resolve lazily on an inlay
    /// hint.
    /// </summary>
    [JsonProperty(PropertyName = "resolveSupport", NullValueHandling = NullValueHandling.Ignore)]
    public InlayHintClientCapabilities.ResolveSupportCapabilities? ResolveSupport { get; set; }
}

/// <summary>
/// Client capabilities specific to diagnostic pull requests.
///
/// @since 3.17.0
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class DiagnosticClientCapabilities
{
    /// <summary>
    /// Whether implementation supports dynamic registration. If this is set to
    /// `true` the client supports the new
    /// `(TextDocumentRegistrationOptions & StaticRegistrationOptions)`
    /// return value for the corresponding server capability as well.
    /// </summary>
    [JsonProperty(PropertyName = "dynamicRegistration", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? DynamicRegistration { get; set; }

    /// <summary>
    /// Whether the clients supports related documents for document diagnostic
    /// pulls.
    /// </summary>
    [JsonProperty(PropertyName = "relatedDocumentSupport", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? RelatedDocumentSupport { get; set; }
}

/// <summary>
/// Capabilities specific to the notebook document support.
///
/// @since 3.17.0
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class NotebookDocumentClientCapabilities
{
    /// <summary>
    /// Capabilities specific to notebook document synchronization
    ///
    /// @since 3.17.0
    /// </summary>
    [JsonProperty(PropertyName = "synchronization")]
    public NotebookDocumentSyncClientCapabilities Synchronization { get; set; }
}

/// <summary>
/// Notebook specific client capabilities.
///
/// @since 3.17.0
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class NotebookDocumentSyncClientCapabilities
{
    /// <summary>
    /// Whether implementation supports dynamic registration. If this is
    /// set to `true` the client supports the new
    /// `(TextDocumentRegistrationOptions & StaticRegistrationOptions)`
    /// return value for the corresponding server capability as well.
    /// </summary>
    [JsonProperty(PropertyName = "dynamicRegistration", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? DynamicRegistration { get; set; }

    /// <summary>
    /// The client supports sending execution summary data per cell.
    /// </summary>
    [JsonProperty(PropertyName = "executionSummarySupport", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? ExecutionSummarySupport { get; set; }
}

/// <summary>
/// Show message request client capabilities
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class ShowMessageRequestClientCapabilities
{
    [GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
    public sealed class MessageActionItemCapabilities
    {
        /// <summary>
        /// Whether the client supports additional attributes which
        /// are preserved and sent back to the server in the
        /// request's response.
        /// </summary>
        [JsonProperty(PropertyName = "additionalPropertiesSupport", NullValueHandling = NullValueHandling.Ignore)]
        public Boolean? AdditionalPropertiesSupport { get; set; }
    }

    /// <summary>
    /// Capabilities specific to the `MessageActionItem` type.
    /// </summary>
    [JsonProperty(PropertyName = "messageActionItem", NullValueHandling = NullValueHandling.Ignore)]
    public ShowMessageRequestClientCapabilities.MessageActionItemCapabilities? MessageActionItem { get; set; }
}

/// <summary>
/// Client capabilities for the show document request.
///
/// @since 3.16.0
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class ShowDocumentClientCapabilities
{
    /// <summary>
    /// The client has support for the show document
    /// request.
    /// </summary>
    [JsonProperty(PropertyName = "support")]
    public Boolean Support { get; set; }
}

/// <summary>
/// Client capabilities specific to regular expressions.
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class RegularExpressionsClientCapabilities
{
    /// <summary>
    /// The engine's name.
    /// </summary>
    [JsonProperty(PropertyName = "engine")]
    public String Engine { get; set; }

    /// <summary>
    /// The engine's version.
    /// </summary>
    [JsonProperty(PropertyName = "version", NullValueHandling = NullValueHandling.Ignore)]
    public String? Version { get; set; }
}

/// <summary>
/// Client capabilities specific to the used markdown parser.
///
/// @since 3.16.0
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class MarkdownClientCapabilities
{
    /// <summary>
    /// The name of the parser.
    /// </summary>
    [JsonProperty(PropertyName = "parser")]
    public String Parser { get; set; }

    /// <summary>
    /// The version of the parser.
    /// </summary>
    [JsonProperty(PropertyName = "version", NullValueHandling = NullValueHandling.Ignore)]
    public String? Version { get; set; }

    /// <summary>
    /// A list of HTML tags that the client allows / supports in
    /// Markdown.
    ///
    /// @since 3.17.0
    /// </summary>
    [JsonProperty(PropertyName = "allowedTags", NullValueHandling = NullValueHandling.Ignore)]
    public IList<String>? AllowedTags { get; set; }
}

/// <summary>
/// A set of predefined position encoding kinds.
///
/// @since 3.17.0
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public enum PositionEncodingKind
{
    /// <summary>
    /// Character offsets count UTF-8 code units (e.g bytes).
    /// </summary>
    [EnumMember(Value = "utf-8")]
    UTF8,
    /// <summary>
    /// Character offsets count UTF-16 code units.
    ///
    /// This is the default and must always be supported
    /// by servers
    /// </summary>
    [EnumMember(Value = "utf-16")]
    UTF16,
    /// <summary>
    /// Character offsets count UTF-32 code units.
    ///
    /// Implementation note: these are the same as Unicode code points,
    /// so this `PositionEncodingKind` may also be used for an
    /// encoding-agnostic representation of character offsets.
    /// </summary>
    [EnumMember(Value = "utf-32")]
    UTF32,
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public enum TraceValue
{
    [EnumMember(Value = "off")]
    Off,
    [EnumMember(Value = "messages")]
    Messages,
    [EnumMember(Value = "verbose")]
    Verbose,
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class WorkspaceFolder
{
    /// <summary>
    /// The associated URI for this workspace folder.
    /// </summary>
    [JsonProperty(PropertyName = "uri")]
    public String Uri { get; set; }

    /// <summary>
    /// The name of the workspace folder. Used to refer to this
    /// workspace folder in the user interface.
    /// </summary>
    [JsonProperty(PropertyName = "name")]
    public String Name { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class InitializeResult
{
    [GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
    public sealed class ServerInfoResult
    {
        /// <summary>
        /// The name of the server as defined by the server.
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public String Name { get; set; }

        /// <summary>
        /// The server's version as defined by the server.
        /// </summary>
        [JsonProperty(PropertyName = "version", NullValueHandling = NullValueHandling.Ignore)]
        public String? Version { get; set; }
    }

    /// <summary>
    /// The capabilities the language server provides.
    /// </summary>
    [JsonProperty(PropertyName = "capabilities")]
    public ServerCapabilities Capabilities { get; set; }

    /// <summary>
    /// Information about the server.
    ///
    /// @since 3.15.0
    /// </summary>
    [JsonProperty(PropertyName = "serverInfo", NullValueHandling = NullValueHandling.Ignore)]
    public InitializeResult.ServerInfoResult? ServerInfo { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class ServerCapabilities
{
    [GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
    public sealed class FileOperationsCapabilities
    {
        /// <summary>
        /// The server is interested in receiving didCreateFiles
        /// notifications.
        /// </summary>
        [JsonProperty(PropertyName = "didCreate", NullValueHandling = NullValueHandling.Ignore)]
        public FileOperationRegistrationOptions? DidCreate { get; set; }

        /// <summary>
        /// The server is interested in receiving willCreateFiles requests.
        /// </summary>
        [JsonProperty(PropertyName = "willCreate", NullValueHandling = NullValueHandling.Ignore)]
        public FileOperationRegistrationOptions? WillCreate { get; set; }

        /// <summary>
        /// The server is interested in receiving didRenameFiles
        /// notifications.
        /// </summary>
        [JsonProperty(PropertyName = "didRename", NullValueHandling = NullValueHandling.Ignore)]
        public FileOperationRegistrationOptions? DidRename { get; set; }

        /// <summary>
        /// The server is interested in receiving willRenameFiles requests.
        /// </summary>
        [JsonProperty(PropertyName = "willRename", NullValueHandling = NullValueHandling.Ignore)]
        public FileOperationRegistrationOptions? WillRename { get; set; }

        /// <summary>
        /// The server is interested in receiving didDeleteFiles file
        /// notifications.
        /// </summary>
        [JsonProperty(PropertyName = "didDelete", NullValueHandling = NullValueHandling.Ignore)]
        public FileOperationRegistrationOptions? DidDelete { get; set; }

        /// <summary>
        /// The server is interested in receiving willDeleteFiles file
        /// requests.
        /// </summary>
        [JsonProperty(PropertyName = "willDelete", NullValueHandling = NullValueHandling.Ignore)]
        public FileOperationRegistrationOptions? WillDelete { get; set; }
    }

    [GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
    public sealed class WorkspaceCapabilities
    {
        /// <summary>
        /// The server supports workspace folder.
        ///
        /// @since 3.6.0
        /// </summary>
        [JsonProperty(PropertyName = "workspaceFolders", NullValueHandling = NullValueHandling.Ignore)]
        public WorkspaceFoldersServerCapabilities? WorkspaceFolders { get; set; }

        /// <summary>
        /// The server is interested in file notifications/requests.
        ///
        /// @since 3.16.0
        /// </summary>
        [JsonProperty(PropertyName = "fileOperations", NullValueHandling = NullValueHandling.Ignore)]
        public ServerCapabilities.FileOperationsCapabilities? FileOperations { get; set; }
    }

    /// <summary>
    /// The position encoding the server picked from the encodings offered
    /// by the client via the client capability `general.positionEncodings`.
    ///
    /// If the client didn't provide any position encodings the only valid
    /// value that a server can return is 'utf-16'.
    ///
    /// If omitted it defaults to 'utf-16'.
    ///
    /// @since 3.17.0
    /// </summary>
    [JsonProperty(PropertyName = "positionEncoding", NullValueHandling = NullValueHandling.Ignore)]
    public PositionEncodingKind? PositionEncoding { get; set; }

    /// <summary>
    /// Defines how text documents are synced. Is either a detailed structure
    /// defining each notification or for backwards compatibility the
    /// TextDocumentSyncKind number. If omitted it defaults to
    /// `TextDocumentSyncKind.None`.
    /// </summary>
    [JsonProperty(PropertyName = "textDocumentSync", NullValueHandling = NullValueHandling.Ignore)]
    public OneOf<TextDocumentSyncOptions, TextDocumentSyncKind>? TextDocumentSync { get; set; }

    /// <summary>
    /// Defines how notebook documents are synced.
    ///
    /// @since 3.17.0
    /// </summary>
    [JsonProperty(PropertyName = "notebookDocumentSync", NullValueHandling = NullValueHandling.Ignore)]
    public OneOf<NotebookDocumentSyncOptions, NotebookDocumentSyncRegistrationOptions>? NotebookDocumentSync { get; set; }

    /// <summary>
    /// The server provides completion support.
    /// </summary>
    [JsonProperty(PropertyName = "completionProvider", NullValueHandling = NullValueHandling.Ignore)]
    public CompletionOptions? CompletionProvider { get; set; }

    /// <summary>
    /// The server provides hover support.
    /// </summary>
    [JsonProperty(PropertyName = "hoverProvider", NullValueHandling = NullValueHandling.Ignore)]
    public OneOf<Boolean, HoverOptions>? HoverProvider { get; set; }

    /// <summary>
    /// The server provides signature help support.
    /// </summary>
    [JsonProperty(PropertyName = "signatureHelpProvider", NullValueHandling = NullValueHandling.Ignore)]
    public SignatureHelpOptions? SignatureHelpProvider { get; set; }

    /// <summary>
    /// The server provides go to declaration support.
    ///
    /// @since 3.14.0
    /// </summary>
    [JsonProperty(PropertyName = "declarationProvider", NullValueHandling = NullValueHandling.Ignore)]
    public OneOf<Boolean, DeclarationOptions, DeclarationRegistrationOptions>? DeclarationProvider { get; set; }

    /// <summary>
    /// The server provides goto definition support.
    /// </summary>
    [JsonProperty(PropertyName = "definitionProvider", NullValueHandling = NullValueHandling.Ignore)]
    public OneOf<Boolean, DefinitionOptions>? DefinitionProvider { get; set; }

    /// <summary>
    /// The server provides goto type definition support.
    ///
    /// @since 3.6.0
    /// </summary>
    [JsonProperty(PropertyName = "typeDefinitionProvider", NullValueHandling = NullValueHandling.Ignore)]
    public OneOf<Boolean, TypeDefinitionOptions, TypeDefinitionRegistrationOptions>? TypeDefinitionProvider { get; set; }

    /// <summary>
    /// The server provides goto implementation support.
    ///
    /// @since 3.6.0
    /// </summary>
    [JsonProperty(PropertyName = "implementationProvider", NullValueHandling = NullValueHandling.Ignore)]
    public OneOf<Boolean, ImplementationOptions, ImplementationRegistrationOptions>? ImplementationProvider { get; set; }

    /// <summary>
    /// The server provides find references support.
    /// </summary>
    [JsonProperty(PropertyName = "referencesProvider", NullValueHandling = NullValueHandling.Ignore)]
    public OneOf<Boolean, ReferenceOptions>? ReferencesProvider { get; set; }

    /// <summary>
    /// The server provides document highlight support.
    /// </summary>
    [JsonProperty(PropertyName = "documentHighlightProvider", NullValueHandling = NullValueHandling.Ignore)]
    public OneOf<Boolean, DocumentHighlightOptions>? DocumentHighlightProvider { get; set; }

    /// <summary>
    /// The server provides document symbol support.
    /// </summary>
    [JsonProperty(PropertyName = "documentSymbolProvider", NullValueHandling = NullValueHandling.Ignore)]
    public OneOf<Boolean, DocumentSymbolOptions>? DocumentSymbolProvider { get; set; }

    /// <summary>
    /// The server provides code actions. The `CodeActionOptions` return type is
    /// only valid if the client signals code action literal support via the
    /// property `textDocument.codeAction.codeActionLiteralSupport`.
    /// </summary>
    [JsonProperty(PropertyName = "codeActionProvider", NullValueHandling = NullValueHandling.Ignore)]
    public OneOf<Boolean, CodeActionOptions>? CodeActionProvider { get; set; }

    /// <summary>
    /// The server provides code lens.
    /// </summary>
    [JsonProperty(PropertyName = "codeLensProvider", NullValueHandling = NullValueHandling.Ignore)]
    public CodeLensOptions? CodeLensProvider { get; set; }

    /// <summary>
    /// The server provides document link support.
    /// </summary>
    [JsonProperty(PropertyName = "documentLinkProvider", NullValueHandling = NullValueHandling.Ignore)]
    public DocumentLinkOptions? DocumentLinkProvider { get; set; }

    /// <summary>
    /// The server provides color provider support.
    ///
    /// @since 3.6.0
    /// </summary>
    [JsonProperty(PropertyName = "colorProvider", NullValueHandling = NullValueHandling.Ignore)]
    public OneOf<Boolean, DocumentColorOptions, DocumentColorRegistrationOptions>? ColorProvider { get; set; }

    /// <summary>
    /// The server provides document formatting.
    /// </summary>
    [JsonProperty(PropertyName = "documentFormattingProvider", NullValueHandling = NullValueHandling.Ignore)]
    public OneOf<Boolean, DocumentFormattingOptions>? DocumentFormattingProvider { get; set; }

    /// <summary>
    /// The server provides document range formatting.
    /// </summary>
    [JsonProperty(PropertyName = "documentRangeFormattingProvider", NullValueHandling = NullValueHandling.Ignore)]
    public OneOf<Boolean, DocumentRangeFormattingOptions>? DocumentRangeFormattingProvider { get; set; }

    /// <summary>
    /// The server provides document formatting on typing.
    /// </summary>
    [JsonProperty(PropertyName = "documentOnTypeFormattingProvider", NullValueHandling = NullValueHandling.Ignore)]
    public DocumentOnTypeFormattingOptions? DocumentOnTypeFormattingProvider { get; set; }

    /// <summary>
    /// The server provides rename support. RenameOptions may only be
    /// specified if the client states that it supports
    /// `prepareSupport` in its initial `initialize` request.
    /// </summary>
    [JsonProperty(PropertyName = "renameProvider", NullValueHandling = NullValueHandling.Ignore)]
    public OneOf<Boolean, RenameOptions>? RenameProvider { get; set; }

    /// <summary>
    /// The server provides folding provider support.
    ///
    /// @since 3.10.0
    /// </summary>
    [JsonProperty(PropertyName = "foldingRangeProvider", NullValueHandling = NullValueHandling.Ignore)]
    public OneOf<Boolean, FoldingRangeOptions, FoldingRangeRegistrationOptions>? FoldingRangeProvider { get; set; }

    /// <summary>
    /// The server provides execute command support.
    /// </summary>
    [JsonProperty(PropertyName = "executeCommandProvider", NullValueHandling = NullValueHandling.Ignore)]
    public ExecuteCommandOptions? ExecuteCommandProvider { get; set; }

    /// <summary>
    /// The server provides selection range support.
    ///
    /// @since 3.15.0
    /// </summary>
    [JsonProperty(PropertyName = "selectionRangeProvider", NullValueHandling = NullValueHandling.Ignore)]
    public OneOf<Boolean, SelectionRangeOptions, SelectionRangeRegistrationOptions>? SelectionRangeProvider { get; set; }

    /// <summary>
    /// The server provides linked editing range support.
    ///
    /// @since 3.16.0
    /// </summary>
    [JsonProperty(PropertyName = "linkedEditingRangeProvider", NullValueHandling = NullValueHandling.Ignore)]
    public OneOf<Boolean, LinkedEditingRangeOptions, LinkedEditingRangeRegistrationOptions>? LinkedEditingRangeProvider { get; set; }

    /// <summary>
    /// The server provides call hierarchy support.
    ///
    /// @since 3.16.0
    /// </summary>
    [JsonProperty(PropertyName = "callHierarchyProvider", NullValueHandling = NullValueHandling.Ignore)]
    public OneOf<Boolean, CallHierarchyOptions, CallHierarchyRegistrationOptions>? CallHierarchyProvider { get; set; }

    /// <summary>
    /// The server provides semantic tokens support.
    ///
    /// @since 3.16.0
    /// </summary>
    [JsonProperty(PropertyName = "semanticTokensProvider", NullValueHandling = NullValueHandling.Ignore)]
    public OneOf<SemanticTokensOptions, SemanticTokensRegistrationOptions>? SemanticTokensProvider { get; set; }

    /// <summary>
    /// Whether server provides moniker support.
    ///
    /// @since 3.16.0
    /// </summary>
    [JsonProperty(PropertyName = "monikerProvider", NullValueHandling = NullValueHandling.Ignore)]
    public OneOf<Boolean, MonikerOptions, MonikerRegistrationOptions>? MonikerProvider { get; set; }

    /// <summary>
    /// The server provides type hierarchy support.
    ///
    /// @since 3.17.0
    /// </summary>
    [JsonProperty(PropertyName = "typeHierarchyProvider", NullValueHandling = NullValueHandling.Ignore)]
    public OneOf<Boolean, TypeHierarchyOptions, TypeHierarchyRegistrationOptions>? TypeHierarchyProvider { get; set; }

    /// <summary>
    /// The server provides inline values.
    ///
    /// @since 3.17.0
    /// </summary>
    [JsonProperty(PropertyName = "inlineValueProvider", NullValueHandling = NullValueHandling.Ignore)]
    public OneOf<Boolean, InlineValueOptions, InlineValueRegistrationOptions>? InlineValueProvider { get; set; }

    /// <summary>
    /// The server provides inlay hints.
    ///
    /// @since 3.17.0
    /// </summary>
    [JsonProperty(PropertyName = "inlayHintProvider", NullValueHandling = NullValueHandling.Ignore)]
    public OneOf<Boolean, InlayHintOptions, InlayHintRegistrationOptions>? InlayHintProvider { get; set; }

    /// <summary>
    /// The server has support for pull model diagnostics.
    ///
    /// @since 3.17.0
    /// </summary>
    [JsonProperty(PropertyName = "diagnosticProvider", NullValueHandling = NullValueHandling.Ignore)]
    public OneOf<DiagnosticOptions, DiagnosticRegistrationOptions>? DiagnosticProvider { get; set; }

    /// <summary>
    /// The server provides workspace symbol support.
    /// </summary>
    [JsonProperty(PropertyName = "workspaceSymbolProvider", NullValueHandling = NullValueHandling.Ignore)]
    public OneOf<Boolean, WorkspaceSymbolOptions>? WorkspaceSymbolProvider { get; set; }

    /// <summary>
    /// Workspace specific server capabilities
    /// </summary>
    [JsonProperty(PropertyName = "workspace", NullValueHandling = NullValueHandling.Ignore)]
    public ServerCapabilities.WorkspaceCapabilities? Workspace { get; set; }

    /// <summary>
    /// Experimental server capabilities.
    /// </summary>
    [JsonProperty(PropertyName = "experimental", NullValueHandling = NullValueHandling.Ignore)]
    public Object? Experimental { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class TextDocumentSyncOptions
{
    /// <summary>
    /// Open and close notifications are sent to the server. If omitted open
    /// close notification should not be sent.
    /// </summary>
    [JsonProperty(PropertyName = "openClose", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? OpenClose { get; set; }

    /// <summary>
    /// Change notifications are sent to the server. See
    /// TextDocumentSyncKind.None, TextDocumentSyncKind.Full and
    /// TextDocumentSyncKind.Incremental. If omitted it defaults to
    /// TextDocumentSyncKind.None.
    /// </summary>
    [JsonProperty(PropertyName = "change", NullValueHandling = NullValueHandling.Ignore)]
    public TextDocumentSyncKind? Change { get; set; }

    /// <summary>
    /// If present will save notifications are sent to the server. If omitted
    /// the notification should not be sent.
    /// </summary>
    [JsonProperty(PropertyName = "willSave", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WillSave { get; set; }

    /// <summary>
    /// If present will save wait until requests are sent to the server. If
    /// omitted the request should not be sent.
    /// </summary>
    [JsonProperty(PropertyName = "willSaveWaitUntil", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WillSaveWaitUntil { get; set; }

    /// <summary>
    /// If present save notifications are sent to the server. If omitted the
    /// notification should not be sent.
    /// </summary>
    [JsonProperty(PropertyName = "save", NullValueHandling = NullValueHandling.Ignore)]
    public OneOf<Boolean, SaveOptions>? Save { get; set; }
}

/// <summary>
/// Defines how the host (editor) should sync document changes to the language
/// server.
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public enum TextDocumentSyncKind
{
    /// <summary>
    /// Documents should not be synced at all.
    /// </summary>
    [EnumMember(Value = "0")]
    None,
    /// <summary>
    /// Documents are synced by always sending the full content
    /// of the document.
    /// </summary>
    [EnumMember(Value = "1")]
    Full,
    /// <summary>
    /// Documents are synced by sending the full content on open.
    /// After that only incremental updates to the document are
    /// sent.
    /// </summary>
    [EnumMember(Value = "2")]
    Incremental,
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class SaveOptions
{
    /// <summary>
    /// The client is supposed to include the content on save.
    /// </summary>
    [JsonProperty(PropertyName = "includeText", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? IncludeText { get; set; }
}

/// <summary>
/// Options specific to a notebook plus its cells
/// to be synced to the server.
///
/// If a selector provides a notebook document
/// filter but no cell selector all cells of a
/// matching notebook document will be synced.
///
/// If a selector provides no notebook document
/// filter but only a cell selector all notebook
/// documents that contain at least one matching
/// cell will be synced.
///
/// @since 3.17.0
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class NotebookDocumentSyncOptions
{
    [GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
    public sealed class CellOptions
    {
        [JsonProperty(PropertyName = "language")]
        public String Language { get; set; }
    }

    [GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
    public sealed class NotebookSelectorOptions
    {
        /// <summary>
        /// The notebook to be synced. If a string
        /// value is provided it matches against the
        /// notebook type. '*' matches every notebook.
        /// </summary>
        [JsonProperty(PropertyName = "notebook", NullValueHandling = NullValueHandling.Ignore)]
        public OneOf<String, NotebookDocumentFilter>? Notebook { get; set; }

        /// <summary>
        /// The cells of the matching notebook to be synced.
        /// </summary>
        [JsonProperty(PropertyName = "cells", NullValueHandling = NullValueHandling.Ignore)]
        public IList<NotebookDocumentSyncOptions.CellOptions>? Cells { get; set; }
    }

    /// <summary>
    /// The notebooks to be synced
    /// </summary>
    [JsonProperty(PropertyName = "notebookSelector")]
    public IList<NotebookDocumentSyncOptions.NotebookSelectorOptions> NotebookSelector { get; set; }

    /// <summary>
    /// Whether save notification should be forwarded to
    /// the server. Will only be honored if mode === `notebook`.
    /// </summary>
    [JsonProperty(PropertyName = "save", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? Save { get; set; }
}

/// <summary>
/// Options specific to a notebook plus its cells
/// to be synced to the server.
///
/// If a selector provides a notebook document
/// filter but no cell selector all cells of a
/// matching notebook document will be synced.
///
/// If a selector provides no notebook document
/// filter but only a cell selector all notebook
/// documents that contain at least one matching
/// cell will be synced.
///
/// @since 3.17.0
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public interface INotebookDocumentSyncOptions
{
    /// <summary>
    /// The notebooks to be synced
    /// </summary>
    [JsonProperty(PropertyName = "notebookSelector")]
    public IList<NotebookDocumentSyncOptions.NotebookSelectorOptions> NotebookSelector { get; set; }

    /// <summary>
    /// Whether save notification should be forwarded to
    /// the server. Will only be honored if mode === `notebook`.
    /// </summary>
    [JsonProperty(PropertyName = "save", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? Save { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class NotebookDocumentFilter
{
    /// <summary>
    /// The type of the enclosing notebook.
    /// </summary>
    [JsonProperty(PropertyName = "notebookType", NullValueHandling = NullValueHandling.Ignore)]
    public String? NotebookType { get; set; }

    /// <summary>
    /// A Uri [scheme](#Uri.scheme), like `file` or `untitled`.
    /// </summary>
    [JsonProperty(PropertyName = "scheme", NullValueHandling = NullValueHandling.Ignore)]
    public String? Scheme { get; set; }

    /// <summary>
    /// A glob pattern.
    /// </summary>
    [JsonProperty(PropertyName = "pattern", NullValueHandling = NullValueHandling.Ignore)]
    public String? Pattern { get; set; }
}

/// <summary>
/// Registration options specific to a notebook.
///
/// @since 3.17.0
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class NotebookDocumentSyncRegistrationOptions : INotebookDocumentSyncOptions, IStaticRegistrationOptions
{
    /// <summary>
    /// The notebooks to be synced
    /// </summary>
    [JsonProperty(PropertyName = "notebookSelector")]
    public IList<NotebookDocumentSyncOptions.NotebookSelectorOptions> NotebookSelector { get; set; }

    /// <summary>
    /// Whether save notification should be forwarded to
    /// the server. Will only be honored if mode === `notebook`.
    /// </summary>
    [JsonProperty(PropertyName = "save", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? Save { get; set; }

    /// <summary>
    /// The id used to register the request. The id can be used to deregister
    /// the request again. See also Registration#id.
    /// </summary>
    [JsonProperty(PropertyName = "id", NullValueHandling = NullValueHandling.Ignore)]
    public String? Id { get; set; }
}

/// <summary>
/// Static registration options to be returned in the initialize request.
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class StaticRegistrationOptions
{
    /// <summary>
    /// The id used to register the request. The id can be used to deregister
    /// the request again. See also Registration#id.
    /// </summary>
    [JsonProperty(PropertyName = "id", NullValueHandling = NullValueHandling.Ignore)]
    public String? Id { get; set; }
}

/// <summary>
/// Static registration options to be returned in the initialize request.
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public interface IStaticRegistrationOptions
{
    /// <summary>
    /// The id used to register the request. The id can be used to deregister
    /// the request again. See also Registration#id.
    /// </summary>
    [JsonProperty(PropertyName = "id", NullValueHandling = NullValueHandling.Ignore)]
    public String? Id { get; set; }
}

/// <summary>
/// Completion options.
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class CompletionOptions : IWorkDoneProgressOptions
{
    [GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
    public sealed class CompletionItemOptions
    {
        /// <summary>
        /// The server has support for completion item label
        /// details (see also `CompletionItemLabelDetails`) when receiving
        /// a completion item in a resolve call.
        ///
        /// @since 3.17.0
        /// </summary>
        [JsonProperty(PropertyName = "labelDetailsSupport", NullValueHandling = NullValueHandling.Ignore)]
        public Boolean? LabelDetailsSupport { get; set; }
    }

    /// <summary>
    /// The additional characters, beyond the defaults provided by the client (typically
    /// [a-zA-Z]), that should automatically trigger a completion request. For example
    /// `.` in JavaScript represents the beginning of an object property or method and is
    /// thus a good candidate for triggering a completion request.
    ///
    /// Most tools trigger a completion request automatically without explicitly
    /// requesting it using a keyboard shortcut (e.g. Ctrl+Space). Typically they
    /// do so when the user starts to type an identifier. For example if the user
    /// types `c` in a JavaScript file code complete will automatically pop up
    /// present `console` besides others as a completion item. Characters that
    /// make up identifiers don't need to be listed here.
    /// </summary>
    [JsonProperty(PropertyName = "triggerCharacters", NullValueHandling = NullValueHandling.Ignore)]
    public IList<String>? TriggerCharacters { get; set; }

    /// <summary>
    /// The list of all possible characters that commit a completion. This field
    /// can be used if clients don't support individual commit characters per
    /// completion item. See client capability
    /// `completion.completionItem.commitCharactersSupport`.
    ///
    /// If a server provides both `allCommitCharacters` and commit characters on
    /// an individual completion item the ones on the completion item win.
    ///
    /// @since 3.2.0
    /// </summary>
    [JsonProperty(PropertyName = "allCommitCharacters", NullValueHandling = NullValueHandling.Ignore)]
    public IList<String>? AllCommitCharacters { get; set; }

    /// <summary>
    /// The server provides support to resolve additional
    /// information for a completion item.
    /// </summary>
    [JsonProperty(PropertyName = "resolveProvider", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? ResolveProvider { get; set; }

    /// <summary>
    /// The server supports the following `CompletionItem` specific
    /// capabilities.
    ///
    /// @since 3.17.0
    /// </summary>
    [JsonProperty(PropertyName = "completionItem", NullValueHandling = NullValueHandling.Ignore)]
    public CompletionOptions.CompletionItemOptions? CompletionItem { get; set; }

    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }
}

/// <summary>
/// Completion options.
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public interface ICompletionOptions : IWorkDoneProgressOptions
{
    /// <summary>
    /// The additional characters, beyond the defaults provided by the client (typically
    /// [a-zA-Z]), that should automatically trigger a completion request. For example
    /// `.` in JavaScript represents the beginning of an object property or method and is
    /// thus a good candidate for triggering a completion request.
    ///
    /// Most tools trigger a completion request automatically without explicitly
    /// requesting it using a keyboard shortcut (e.g. Ctrl+Space). Typically they
    /// do so when the user starts to type an identifier. For example if the user
    /// types `c` in a JavaScript file code complete will automatically pop up
    /// present `console` besides others as a completion item. Characters that
    /// make up identifiers don't need to be listed here.
    /// </summary>
    [JsonProperty(PropertyName = "triggerCharacters", NullValueHandling = NullValueHandling.Ignore)]
    public IList<String>? TriggerCharacters { get; set; }

    /// <summary>
    /// The list of all possible characters that commit a completion. This field
    /// can be used if clients don't support individual commit characters per
    /// completion item. See client capability
    /// `completion.completionItem.commitCharactersSupport`.
    ///
    /// If a server provides both `allCommitCharacters` and commit characters on
    /// an individual completion item the ones on the completion item win.
    ///
    /// @since 3.2.0
    /// </summary>
    [JsonProperty(PropertyName = "allCommitCharacters", NullValueHandling = NullValueHandling.Ignore)]
    public IList<String>? AllCommitCharacters { get; set; }

    /// <summary>
    /// The server provides support to resolve additional
    /// information for a completion item.
    /// </summary>
    [JsonProperty(PropertyName = "resolveProvider", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? ResolveProvider { get; set; }

    /// <summary>
    /// The server supports the following `CompletionItem` specific
    /// capabilities.
    ///
    /// @since 3.17.0
    /// </summary>
    [JsonProperty(PropertyName = "completionItem", NullValueHandling = NullValueHandling.Ignore)]
    public CompletionOptions.CompletionItemOptions? CompletionItem { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class WorkDoneProgressOptions
{
    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public interface IWorkDoneProgressOptions
{
    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class HoverOptions : IWorkDoneProgressOptions
{
    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public interface IHoverOptions : IWorkDoneProgressOptions
{
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class SignatureHelpOptions : IWorkDoneProgressOptions
{
    /// <summary>
    /// The characters that trigger signature help
    /// automatically.
    /// </summary>
    [JsonProperty(PropertyName = "triggerCharacters", NullValueHandling = NullValueHandling.Ignore)]
    public IList<String>? TriggerCharacters { get; set; }

    /// <summary>
    /// List of characters that re-trigger signature help.
    ///
    /// These trigger characters are only active when signature help is already
    /// showing. All trigger characters are also counted as re-trigger
    /// characters.
    ///
    /// @since 3.15.0
    /// </summary>
    [JsonProperty(PropertyName = "retriggerCharacters", NullValueHandling = NullValueHandling.Ignore)]
    public IList<String>? RetriggerCharacters { get; set; }

    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public interface ISignatureHelpOptions : IWorkDoneProgressOptions
{
    /// <summary>
    /// The characters that trigger signature help
    /// automatically.
    /// </summary>
    [JsonProperty(PropertyName = "triggerCharacters", NullValueHandling = NullValueHandling.Ignore)]
    public IList<String>? TriggerCharacters { get; set; }

    /// <summary>
    /// List of characters that re-trigger signature help.
    ///
    /// These trigger characters are only active when signature help is already
    /// showing. All trigger characters are also counted as re-trigger
    /// characters.
    ///
    /// @since 3.15.0
    /// </summary>
    [JsonProperty(PropertyName = "retriggerCharacters", NullValueHandling = NullValueHandling.Ignore)]
    public IList<String>? RetriggerCharacters { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class DeclarationOptions : IWorkDoneProgressOptions
{
    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public interface IDeclarationOptions : IWorkDoneProgressOptions
{
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class DeclarationRegistrationOptions : IDeclarationOptions, ITextDocumentRegistrationOptions, IStaticRegistrationOptions
{
    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }

    /// <summary>
    /// A document selector to identify the scope of the registration. If set to
    /// null the document selector provided on the client side will be used.
    /// </summary>
    [JsonProperty(PropertyName = "documentSelector")]
    public IList<DocumentFilter>? DocumentSelector { get; set; }

    /// <summary>
    /// The id used to register the request. The id can be used to deregister
    /// the request again. See also Registration#id.
    /// </summary>
    [JsonProperty(PropertyName = "id", NullValueHandling = NullValueHandling.Ignore)]
    public String? Id { get; set; }
}

/// <summary>
/// General text document registration options.
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class TextDocumentRegistrationOptions
{
    /// <summary>
    /// A document selector to identify the scope of the registration. If set to
    /// null the document selector provided on the client side will be used.
    /// </summary>
    [JsonProperty(PropertyName = "documentSelector")]
    public IList<DocumentFilter>? DocumentSelector { get; set; }
}

/// <summary>
/// General text document registration options.
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public interface ITextDocumentRegistrationOptions
{
    /// <summary>
    /// A document selector to identify the scope of the registration. If set to
    /// null the document selector provided on the client side will be used.
    /// </summary>
    [JsonProperty(PropertyName = "documentSelector")]
    public IList<DocumentFilter>? DocumentSelector { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class DocumentFilter
{
    /// <summary>
    /// A language id, like `typescript`.
    /// </summary>
    [JsonProperty(PropertyName = "language", NullValueHandling = NullValueHandling.Ignore)]
    public String? Language { get; set; }

    /// <summary>
    /// A Uri [scheme](#Uri.scheme), like `file` or `untitled`.
    /// </summary>
    [JsonProperty(PropertyName = "scheme", NullValueHandling = NullValueHandling.Ignore)]
    public String? Scheme { get; set; }

    /// <summary>
    /// A glob pattern, like `*.{ts,js}`.
    ///
    /// Glob patterns can have the following syntax:
    /// - `*` to match one or more characters in a path segment
    /// - `?` to match on one character in a path segment
    /// - `**` to match any number of path segments, including none
    /// - `{}` to group sub patterns into an OR expression. (e.g. `**?/*.{ts,js}`
    /// matches all TypeScript and JavaScript files)
    /// - `[]` to declare a range of characters to match in a path segment
    /// (e.g., `example.[0-9]` to match on `example.0`, `example.1`, .)
    /// - `[!...]` to negate a range of characters to match in a path segment
    /// (e.g., `example.[!0-9]` to match on `example.a`, `example.b`, but
    /// not `example.0`)
    /// </summary>
    [JsonProperty(PropertyName = "pattern", NullValueHandling = NullValueHandling.Ignore)]
    public String? Pattern { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class DefinitionOptions : IWorkDoneProgressOptions
{
    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public interface IDefinitionOptions : IWorkDoneProgressOptions
{
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class TypeDefinitionOptions : IWorkDoneProgressOptions
{
    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public interface ITypeDefinitionOptions : IWorkDoneProgressOptions
{
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class TypeDefinitionRegistrationOptions : ITextDocumentRegistrationOptions, ITypeDefinitionOptions, IStaticRegistrationOptions
{
    /// <summary>
    /// A document selector to identify the scope of the registration. If set to
    /// null the document selector provided on the client side will be used.
    /// </summary>
    [JsonProperty(PropertyName = "documentSelector")]
    public IList<DocumentFilter>? DocumentSelector { get; set; }

    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }

    /// <summary>
    /// The id used to register the request. The id can be used to deregister
    /// the request again. See also Registration#id.
    /// </summary>
    [JsonProperty(PropertyName = "id", NullValueHandling = NullValueHandling.Ignore)]
    public String? Id { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class ImplementationOptions : IWorkDoneProgressOptions
{
    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public interface IImplementationOptions : IWorkDoneProgressOptions
{
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class ImplementationRegistrationOptions : ITextDocumentRegistrationOptions, IImplementationOptions, IStaticRegistrationOptions
{
    /// <summary>
    /// A document selector to identify the scope of the registration. If set to
    /// null the document selector provided on the client side will be used.
    /// </summary>
    [JsonProperty(PropertyName = "documentSelector")]
    public IList<DocumentFilter>? DocumentSelector { get; set; }

    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }

    /// <summary>
    /// The id used to register the request. The id can be used to deregister
    /// the request again. See also Registration#id.
    /// </summary>
    [JsonProperty(PropertyName = "id", NullValueHandling = NullValueHandling.Ignore)]
    public String? Id { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class ReferenceOptions : IWorkDoneProgressOptions
{
    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public interface IReferenceOptions : IWorkDoneProgressOptions
{
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class DocumentHighlightOptions : IWorkDoneProgressOptions
{
    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public interface IDocumentHighlightOptions : IWorkDoneProgressOptions
{
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class DocumentSymbolOptions : IWorkDoneProgressOptions
{
    /// <summary>
    /// A human-readable string that is shown when multiple outlines trees
    /// are shown for the same document.
    ///
    /// @since 3.16.0
    /// </summary>
    [JsonProperty(PropertyName = "label", NullValueHandling = NullValueHandling.Ignore)]
    public String? Label { get; set; }

    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public interface IDocumentSymbolOptions : IWorkDoneProgressOptions
{
    /// <summary>
    /// A human-readable string that is shown when multiple outlines trees
    /// are shown for the same document.
    ///
    /// @since 3.16.0
    /// </summary>
    [JsonProperty(PropertyName = "label", NullValueHandling = NullValueHandling.Ignore)]
    public String? Label { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class CodeActionOptions : IWorkDoneProgressOptions
{
    /// <summary>
    /// CodeActionKinds that this server may return.
    ///
    /// The list of kinds may be generic, such as `CodeActionKind.Refactor`,
    /// or the server may list out every specific kind they provide.
    /// </summary>
    [JsonProperty(PropertyName = "codeActionKinds", NullValueHandling = NullValueHandling.Ignore)]
    public IList<CodeActionKind>? CodeActionKinds { get; set; }

    /// <summary>
    /// The server provides support to resolve additional
    /// information for a code action.
    ///
    /// @since 3.16.0
    /// </summary>
    [JsonProperty(PropertyName = "resolveProvider", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? ResolveProvider { get; set; }

    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public interface ICodeActionOptions : IWorkDoneProgressOptions
{
    /// <summary>
    /// CodeActionKinds that this server may return.
    ///
    /// The list of kinds may be generic, such as `CodeActionKind.Refactor`,
    /// or the server may list out every specific kind they provide.
    /// </summary>
    [JsonProperty(PropertyName = "codeActionKinds", NullValueHandling = NullValueHandling.Ignore)]
    public IList<CodeActionKind>? CodeActionKinds { get; set; }

    /// <summary>
    /// The server provides support to resolve additional
    /// information for a code action.
    ///
    /// @since 3.16.0
    /// </summary>
    [JsonProperty(PropertyName = "resolveProvider", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? ResolveProvider { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class CodeLensOptions : IWorkDoneProgressOptions
{
    /// <summary>
    /// Code lens has a resolve provider as well.
    /// </summary>
    [JsonProperty(PropertyName = "resolveProvider", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? ResolveProvider { get; set; }

    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public interface ICodeLensOptions : IWorkDoneProgressOptions
{
    /// <summary>
    /// Code lens has a resolve provider as well.
    /// </summary>
    [JsonProperty(PropertyName = "resolveProvider", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? ResolveProvider { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class DocumentLinkOptions : IWorkDoneProgressOptions
{
    /// <summary>
    /// Document links have a resolve provider as well.
    /// </summary>
    [JsonProperty(PropertyName = "resolveProvider", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? ResolveProvider { get; set; }

    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public interface IDocumentLinkOptions : IWorkDoneProgressOptions
{
    /// <summary>
    /// Document links have a resolve provider as well.
    /// </summary>
    [JsonProperty(PropertyName = "resolveProvider", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? ResolveProvider { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class DocumentColorOptions : IWorkDoneProgressOptions
{
    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public interface IDocumentColorOptions : IWorkDoneProgressOptions
{
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class DocumentColorRegistrationOptions : ITextDocumentRegistrationOptions, IStaticRegistrationOptions, IDocumentColorOptions
{
    /// <summary>
    /// A document selector to identify the scope of the registration. If set to
    /// null the document selector provided on the client side will be used.
    /// </summary>
    [JsonProperty(PropertyName = "documentSelector")]
    public IList<DocumentFilter>? DocumentSelector { get; set; }

    /// <summary>
    /// The id used to register the request. The id can be used to deregister
    /// the request again. See also Registration#id.
    /// </summary>
    [JsonProperty(PropertyName = "id", NullValueHandling = NullValueHandling.Ignore)]
    public String? Id { get; set; }

    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class DocumentFormattingOptions : IWorkDoneProgressOptions
{
    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public interface IDocumentFormattingOptions : IWorkDoneProgressOptions
{
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class DocumentRangeFormattingOptions : IWorkDoneProgressOptions
{
    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public interface IDocumentRangeFormattingOptions : IWorkDoneProgressOptions
{
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class DocumentOnTypeFormattingOptions
{
    /// <summary>
    /// A character on which formatting should be triggered, like `{`.
    /// </summary>
    [JsonProperty(PropertyName = "firstTriggerCharacter")]
    public String FirstTriggerCharacter { get; set; }

    /// <summary>
    /// More trigger characters.
    /// </summary>
    [JsonProperty(PropertyName = "moreTriggerCharacter", NullValueHandling = NullValueHandling.Ignore)]
    public IList<String>? MoreTriggerCharacter { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public interface IDocumentOnTypeFormattingOptions
{
    /// <summary>
    /// A character on which formatting should be triggered, like `{`.
    /// </summary>
    [JsonProperty(PropertyName = "firstTriggerCharacter")]
    public String FirstTriggerCharacter { get; set; }

    /// <summary>
    /// More trigger characters.
    /// </summary>
    [JsonProperty(PropertyName = "moreTriggerCharacter", NullValueHandling = NullValueHandling.Ignore)]
    public IList<String>? MoreTriggerCharacter { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class RenameOptions : IWorkDoneProgressOptions
{
    /// <summary>
    /// Renames should be checked and tested before being executed.
    /// </summary>
    [JsonProperty(PropertyName = "prepareProvider", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? PrepareProvider { get; set; }

    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public interface IRenameOptions : IWorkDoneProgressOptions
{
    /// <summary>
    /// Renames should be checked and tested before being executed.
    /// </summary>
    [JsonProperty(PropertyName = "prepareProvider", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? PrepareProvider { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class FoldingRangeOptions : IWorkDoneProgressOptions
{
    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public interface IFoldingRangeOptions : IWorkDoneProgressOptions
{
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class FoldingRangeRegistrationOptions : ITextDocumentRegistrationOptions, IFoldingRangeOptions, IStaticRegistrationOptions
{
    /// <summary>
    /// A document selector to identify the scope of the registration. If set to
    /// null the document selector provided on the client side will be used.
    /// </summary>
    [JsonProperty(PropertyName = "documentSelector")]
    public IList<DocumentFilter>? DocumentSelector { get; set; }

    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }

    /// <summary>
    /// The id used to register the request. The id can be used to deregister
    /// the request again. See also Registration#id.
    /// </summary>
    [JsonProperty(PropertyName = "id", NullValueHandling = NullValueHandling.Ignore)]
    public String? Id { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class ExecuteCommandOptions : IWorkDoneProgressOptions
{
    /// <summary>
    /// The commands to be executed on the server
    /// </summary>
    [JsonProperty(PropertyName = "commands")]
    public IList<String> Commands { get; set; }

    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public interface IExecuteCommandOptions : IWorkDoneProgressOptions
{
    /// <summary>
    /// The commands to be executed on the server
    /// </summary>
    [JsonProperty(PropertyName = "commands")]
    public IList<String> Commands { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class SelectionRangeOptions : IWorkDoneProgressOptions
{
    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public interface ISelectionRangeOptions : IWorkDoneProgressOptions
{
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class SelectionRangeRegistrationOptions : ISelectionRangeOptions, ITextDocumentRegistrationOptions, IStaticRegistrationOptions
{
    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }

    /// <summary>
    /// A document selector to identify the scope of the registration. If set to
    /// null the document selector provided on the client side will be used.
    /// </summary>
    [JsonProperty(PropertyName = "documentSelector")]
    public IList<DocumentFilter>? DocumentSelector { get; set; }

    /// <summary>
    /// The id used to register the request. The id can be used to deregister
    /// the request again. See also Registration#id.
    /// </summary>
    [JsonProperty(PropertyName = "id", NullValueHandling = NullValueHandling.Ignore)]
    public String? Id { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class LinkedEditingRangeOptions : IWorkDoneProgressOptions
{
    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public interface ILinkedEditingRangeOptions : IWorkDoneProgressOptions
{
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class LinkedEditingRangeRegistrationOptions : ITextDocumentRegistrationOptions, ILinkedEditingRangeOptions, IStaticRegistrationOptions
{
    /// <summary>
    /// A document selector to identify the scope of the registration. If set to
    /// null the document selector provided on the client side will be used.
    /// </summary>
    [JsonProperty(PropertyName = "documentSelector")]
    public IList<DocumentFilter>? DocumentSelector { get; set; }

    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }

    /// <summary>
    /// The id used to register the request. The id can be used to deregister
    /// the request again. See also Registration#id.
    /// </summary>
    [JsonProperty(PropertyName = "id", NullValueHandling = NullValueHandling.Ignore)]
    public String? Id { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class CallHierarchyOptions : IWorkDoneProgressOptions
{
    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public interface ICallHierarchyOptions : IWorkDoneProgressOptions
{
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class CallHierarchyRegistrationOptions : ITextDocumentRegistrationOptions, ICallHierarchyOptions, IStaticRegistrationOptions
{
    /// <summary>
    /// A document selector to identify the scope of the registration. If set to
    /// null the document selector provided on the client side will be used.
    /// </summary>
    [JsonProperty(PropertyName = "documentSelector")]
    public IList<DocumentFilter>? DocumentSelector { get; set; }

    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }

    /// <summary>
    /// The id used to register the request. The id can be used to deregister
    /// the request again. See also Registration#id.
    /// </summary>
    [JsonProperty(PropertyName = "id", NullValueHandling = NullValueHandling.Ignore)]
    public String? Id { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class SemanticTokensOptions : IWorkDoneProgressOptions
{
    [GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
    public sealed class RangeOptions
    {
    }

    [GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
    public sealed class FullOptions
    {
        /// <summary>
        /// The server supports deltas for full documents.
        /// </summary>
        [JsonProperty(PropertyName = "delta", NullValueHandling = NullValueHandling.Ignore)]
        public Boolean? Delta { get; set; }
    }

    /// <summary>
    /// The legend used by the server
    /// </summary>
    [JsonProperty(PropertyName = "legend")]
    public SemanticTokensLegend Legend { get; set; }

    /// <summary>
    /// Server supports providing semantic tokens for a specific range
    /// of a document.
    /// </summary>
    [JsonProperty(PropertyName = "range", NullValueHandling = NullValueHandling.Ignore)]
    public OneOf<Boolean, SemanticTokensOptions.RangeOptions>? Range { get; set; }

    /// <summary>
    /// Server supports providing semantic tokens for a full document.
    /// </summary>
    [JsonProperty(PropertyName = "full", NullValueHandling = NullValueHandling.Ignore)]
    public OneOf<Boolean, SemanticTokensOptions.FullOptions>? Full { get; set; }

    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public interface ISemanticTokensOptions : IWorkDoneProgressOptions
{
    /// <summary>
    /// The legend used by the server
    /// </summary>
    [JsonProperty(PropertyName = "legend")]
    public SemanticTokensLegend Legend { get; set; }

    /// <summary>
    /// Server supports providing semantic tokens for a specific range
    /// of a document.
    /// </summary>
    [JsonProperty(PropertyName = "range", NullValueHandling = NullValueHandling.Ignore)]
    public OneOf<Boolean, SemanticTokensOptions.RangeOptions>? Range { get; set; }

    /// <summary>
    /// Server supports providing semantic tokens for a full document.
    /// </summary>
    [JsonProperty(PropertyName = "full", NullValueHandling = NullValueHandling.Ignore)]
    public OneOf<Boolean, SemanticTokensOptions.FullOptions>? Full { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class SemanticTokensLegend
{
    /// <summary>
    /// The token types a server uses.
    /// </summary>
    [JsonProperty(PropertyName = "tokenTypes")]
    public IList<String> TokenTypes { get; set; }

    /// <summary>
    /// The token modifiers a server uses.
    /// </summary>
    [JsonProperty(PropertyName = "tokenModifiers")]
    public IList<String> TokenModifiers { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class SemanticTokensRegistrationOptions : ITextDocumentRegistrationOptions, ISemanticTokensOptions, IStaticRegistrationOptions
{
    /// <summary>
    /// A document selector to identify the scope of the registration. If set to
    /// null the document selector provided on the client side will be used.
    /// </summary>
    [JsonProperty(PropertyName = "documentSelector")]
    public IList<DocumentFilter>? DocumentSelector { get; set; }

    /// <summary>
    /// The legend used by the server
    /// </summary>
    [JsonProperty(PropertyName = "legend")]
    public SemanticTokensLegend Legend { get; set; }

    /// <summary>
    /// Server supports providing semantic tokens for a specific range
    /// of a document.
    /// </summary>
    [JsonProperty(PropertyName = "range", NullValueHandling = NullValueHandling.Ignore)]
    public OneOf<Boolean, SemanticTokensOptions.RangeOptions>? Range { get; set; }

    /// <summary>
    /// Server supports providing semantic tokens for a full document.
    /// </summary>
    [JsonProperty(PropertyName = "full", NullValueHandling = NullValueHandling.Ignore)]
    public OneOf<Boolean, SemanticTokensOptions.FullOptions>? Full { get; set; }

    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }

    /// <summary>
    /// The id used to register the request. The id can be used to deregister
    /// the request again. See also Registration#id.
    /// </summary>
    [JsonProperty(PropertyName = "id", NullValueHandling = NullValueHandling.Ignore)]
    public String? Id { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class MonikerOptions : IWorkDoneProgressOptions
{
    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public interface IMonikerOptions : IWorkDoneProgressOptions
{
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class MonikerRegistrationOptions : ITextDocumentRegistrationOptions, IMonikerOptions
{
    /// <summary>
    /// A document selector to identify the scope of the registration. If set to
    /// null the document selector provided on the client side will be used.
    /// </summary>
    [JsonProperty(PropertyName = "documentSelector")]
    public IList<DocumentFilter>? DocumentSelector { get; set; }

    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class TypeHierarchyOptions : IWorkDoneProgressOptions
{
    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public interface ITypeHierarchyOptions : IWorkDoneProgressOptions
{
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class TypeHierarchyRegistrationOptions : ITextDocumentRegistrationOptions, ITypeHierarchyOptions, IStaticRegistrationOptions
{
    /// <summary>
    /// A document selector to identify the scope of the registration. If set to
    /// null the document selector provided on the client side will be used.
    /// </summary>
    [JsonProperty(PropertyName = "documentSelector")]
    public IList<DocumentFilter>? DocumentSelector { get; set; }

    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }

    /// <summary>
    /// The id used to register the request. The id can be used to deregister
    /// the request again. See also Registration#id.
    /// </summary>
    [JsonProperty(PropertyName = "id", NullValueHandling = NullValueHandling.Ignore)]
    public String? Id { get; set; }
}

/// <summary>
/// Inline value options used during static registration.
///
/// @since 3.17.0
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class InlineValueOptions : IWorkDoneProgressOptions
{
    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }
}

/// <summary>
/// Inline value options used during static registration.
///
/// @since 3.17.0
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public interface IInlineValueOptions : IWorkDoneProgressOptions
{
}

/// <summary>
/// Inline value options used during static or dynamic registration.
///
/// @since 3.17.0
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class InlineValueRegistrationOptions : IInlineValueOptions, ITextDocumentRegistrationOptions, IStaticRegistrationOptions
{
    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }

    /// <summary>
    /// A document selector to identify the scope of the registration. If set to
    /// null the document selector provided on the client side will be used.
    /// </summary>
    [JsonProperty(PropertyName = "documentSelector")]
    public IList<DocumentFilter>? DocumentSelector { get; set; }

    /// <summary>
    /// The id used to register the request. The id can be used to deregister
    /// the request again. See also Registration#id.
    /// </summary>
    [JsonProperty(PropertyName = "id", NullValueHandling = NullValueHandling.Ignore)]
    public String? Id { get; set; }
}

/// <summary>
/// Inlay hint options used during static registration.
///
/// @since 3.17.0
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class InlayHintOptions : IWorkDoneProgressOptions
{
    /// <summary>
    /// The server provides support to resolve additional
    /// information for an inlay hint item.
    /// </summary>
    [JsonProperty(PropertyName = "resolveProvider", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? ResolveProvider { get; set; }

    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }
}

/// <summary>
/// Inlay hint options used during static registration.
///
/// @since 3.17.0
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public interface IInlayHintOptions : IWorkDoneProgressOptions
{
    /// <summary>
    /// The server provides support to resolve additional
    /// information for an inlay hint item.
    /// </summary>
    [JsonProperty(PropertyName = "resolveProvider", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? ResolveProvider { get; set; }
}

/// <summary>
/// Inlay hint options used during static or dynamic registration.
///
/// @since 3.17.0
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class InlayHintRegistrationOptions : IInlayHintOptions, ITextDocumentRegistrationOptions, IStaticRegistrationOptions
{
    /// <summary>
    /// The server provides support to resolve additional
    /// information for an inlay hint item.
    /// </summary>
    [JsonProperty(PropertyName = "resolveProvider", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? ResolveProvider { get; set; }

    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }

    /// <summary>
    /// A document selector to identify the scope of the registration. If set to
    /// null the document selector provided on the client side will be used.
    /// </summary>
    [JsonProperty(PropertyName = "documentSelector")]
    public IList<DocumentFilter>? DocumentSelector { get; set; }

    /// <summary>
    /// The id used to register the request. The id can be used to deregister
    /// the request again. See also Registration#id.
    /// </summary>
    [JsonProperty(PropertyName = "id", NullValueHandling = NullValueHandling.Ignore)]
    public String? Id { get; set; }
}

/// <summary>
/// Diagnostic options.
///
/// @since 3.17.0
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class DiagnosticOptions : IWorkDoneProgressOptions
{
    /// <summary>
    /// An optional identifier under which the diagnostics are
    /// managed by the client.
    /// </summary>
    [JsonProperty(PropertyName = "identifier", NullValueHandling = NullValueHandling.Ignore)]
    public String? Identifier { get; set; }

    /// <summary>
    /// Whether the language has inter file dependencies meaning that
    /// editing code in one file can result in a different diagnostic
    /// set in another file. Inter file dependencies are common for
    /// most programming languages and typically uncommon for linters.
    /// </summary>
    [JsonProperty(PropertyName = "interFileDependencies")]
    public Boolean InterFileDependencies { get; set; }

    /// <summary>
    /// The server provides support for workspace diagnostics as well.
    /// </summary>
    [JsonProperty(PropertyName = "workspaceDiagnostics")]
    public Boolean WorkspaceDiagnostics { get; set; }

    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }
}

/// <summary>
/// Diagnostic options.
///
/// @since 3.17.0
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public interface IDiagnosticOptions : IWorkDoneProgressOptions
{
    /// <summary>
    /// An optional identifier under which the diagnostics are
    /// managed by the client.
    /// </summary>
    [JsonProperty(PropertyName = "identifier", NullValueHandling = NullValueHandling.Ignore)]
    public String? Identifier { get; set; }

    /// <summary>
    /// Whether the language has inter file dependencies meaning that
    /// editing code in one file can result in a different diagnostic
    /// set in another file. Inter file dependencies are common for
    /// most programming languages and typically uncommon for linters.
    /// </summary>
    [JsonProperty(PropertyName = "interFileDependencies")]
    public Boolean InterFileDependencies { get; set; }

    /// <summary>
    /// The server provides support for workspace diagnostics as well.
    /// </summary>
    [JsonProperty(PropertyName = "workspaceDiagnostics")]
    public Boolean WorkspaceDiagnostics { get; set; }
}

/// <summary>
/// Diagnostic registration options.
///
/// @since 3.17.0
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class DiagnosticRegistrationOptions : ITextDocumentRegistrationOptions, IDiagnosticOptions, IStaticRegistrationOptions
{
    /// <summary>
    /// A document selector to identify the scope of the registration. If set to
    /// null the document selector provided on the client side will be used.
    /// </summary>
    [JsonProperty(PropertyName = "documentSelector")]
    public IList<DocumentFilter>? DocumentSelector { get; set; }

    /// <summary>
    /// An optional identifier under which the diagnostics are
    /// managed by the client.
    /// </summary>
    [JsonProperty(PropertyName = "identifier", NullValueHandling = NullValueHandling.Ignore)]
    public String? Identifier { get; set; }

    /// <summary>
    /// Whether the language has inter file dependencies meaning that
    /// editing code in one file can result in a different diagnostic
    /// set in another file. Inter file dependencies are common for
    /// most programming languages and typically uncommon for linters.
    /// </summary>
    [JsonProperty(PropertyName = "interFileDependencies")]
    public Boolean InterFileDependencies { get; set; }

    /// <summary>
    /// The server provides support for workspace diagnostics as well.
    /// </summary>
    [JsonProperty(PropertyName = "workspaceDiagnostics")]
    public Boolean WorkspaceDiagnostics { get; set; }

    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }

    /// <summary>
    /// The id used to register the request. The id can be used to deregister
    /// the request again. See also Registration#id.
    /// </summary>
    [JsonProperty(PropertyName = "id", NullValueHandling = NullValueHandling.Ignore)]
    public String? Id { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class WorkspaceSymbolOptions : IWorkDoneProgressOptions
{
    /// <summary>
    /// The server provides support to resolve additional
    /// information for a workspace symbol.
    ///
    /// @since 3.17.0
    /// </summary>
    [JsonProperty(PropertyName = "resolveProvider", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? ResolveProvider { get; set; }

    [JsonProperty(PropertyName = "workDoneProgress", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? WorkDoneProgress { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public interface IWorkspaceSymbolOptions : IWorkDoneProgressOptions
{
    /// <summary>
    /// The server provides support to resolve additional
    /// information for a workspace symbol.
    ///
    /// @since 3.17.0
    /// </summary>
    [JsonProperty(PropertyName = "resolveProvider", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? ResolveProvider { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class WorkspaceFoldersServerCapabilities
{
    /// <summary>
    /// The server has support for workspace folders
    /// </summary>
    [JsonProperty(PropertyName = "supported", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? Supported { get; set; }

    /// <summary>
    /// Whether the server wants to receive workspace folder
    /// change notifications.
    ///
    /// If a string is provided, the string is treated as an ID
    /// under which the notification is registered on the client
    /// side. The ID can be used to unregister for these events
    /// using the `client/unregisterCapability` request.
    /// </summary>
    [JsonProperty(PropertyName = "changeNotifications", NullValueHandling = NullValueHandling.Ignore)]
    public OneOf<String, Boolean>? ChangeNotifications { get; set; }
}

/// <summary>
/// The options to register for file operations.
///
/// @since 3.16.0
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class FileOperationRegistrationOptions
{
    /// <summary>
    /// The actual filters.
    /// </summary>
    [JsonProperty(PropertyName = "filters")]
    public IList<FileOperationFilter> Filters { get; set; }
}

/// <summary>
/// A filter to describe in which file operation requests or notifications
/// the server is interested in.
///
/// @since 3.16.0
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class FileOperationFilter
{
    /// <summary>
    /// A Uri like `file` or `untitled`.
    /// </summary>
    [JsonProperty(PropertyName = "scheme", NullValueHandling = NullValueHandling.Ignore)]
    public String? Scheme { get; set; }

    /// <summary>
    /// The actual file operation pattern.
    /// </summary>
    [JsonProperty(PropertyName = "pattern")]
    public FileOperationPattern Pattern { get; set; }
}

/// <summary>
/// A pattern to describe in which file operation requests or notifications
/// the server is interested in.
///
/// @since 3.16.0
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class FileOperationPattern
{
    /// <summary>
    /// The glob pattern to match. Glob patterns can have the following syntax:
    /// - `*` to match one or more characters in a path segment
    /// - `?` to match on one character in a path segment
    /// - `**` to match any number of path segments, including none
    /// - `{}` to group sub patterns into an OR expression. (e.g. `**?/*.{ts,js}`
    /// matches all TypeScript and JavaScript files)
    /// - `[]` to declare a range of characters to match in a path segment
    /// (e.g., `example.[0-9]` to match on `example.0`, `example.1`, .)
    /// - `[!...]` to negate a range of characters to match in a path segment
    /// (e.g., `example.[!0-9]` to match on `example.a`, `example.b`, but
    /// not `example.0`)
    /// </summary>
    [JsonProperty(PropertyName = "glob")]
    public String Glob { get; set; }

    /// <summary>
    /// Whether to match files or folders with this pattern.
    ///
    /// Matches both if undefined.
    /// </summary>
    [JsonProperty(PropertyName = "matches", NullValueHandling = NullValueHandling.Ignore)]
    public FileOperationPatternKind? Matches { get; set; }

    /// <summary>
    /// Additional options used during matching.
    /// </summary>
    [JsonProperty(PropertyName = "options", NullValueHandling = NullValueHandling.Ignore)]
    public FileOperationPatternOptions? Options { get; set; }
}

/// <summary>
/// A pattern kind describing if a glob pattern matches a file a folder or
/// both.
///
/// @since 3.16.0
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public enum FileOperationPatternKind
{
    /// <summary>
    /// The pattern matches a file only.
    /// </summary>
    [EnumMember(Value = "file")]
    File,
    /// <summary>
    /// The pattern matches a folder only.
    /// </summary>
    [EnumMember(Value = "folder")]
    Folder,
}

/// <summary>
/// Matching options for the file operation pattern.
///
/// @since 3.16.0
/// </summary>
[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class FileOperationPatternOptions
{
    /// <summary>
    /// The pattern should be matched ignoring casing.
    /// </summary>
    [JsonProperty(PropertyName = "ignoreCase", NullValueHandling = NullValueHandling.Ignore)]
    public Boolean? IgnoreCase { get; set; }
}

[GeneratedCodeAttribute("Draco.Lsp.Generation", "0.1.0")]
public sealed class InitializedParams
{
}

#pragma warning restore CS8618

#endregion
