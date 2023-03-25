using Draco.Lsp.Generation.TypeScript;

namespace Draco.Lsp.Generation;

internal class Program
{
    internal static void Main(string[] args)
    {
        var tokens = Lexer.Lex(new StringReader(testInput));
        var model = Parser.Parse(tokens);
        var csCode = Translator.Translate(model);
        Console.WriteLine(csCode);
    }

    private static readonly string testInput = """
        interface ClientCapabilities {
        	/**
        	 * Workspace specific client capabilities.
        	 */
        	workspace?: {
        		/**
        		 * The client supports applying batch edits
        		 * to the workspace by supporting the request
        		 * 'workspace/applyEdit'
        		 */
        		applyEdit?: boolean;

        		/**
        		 * Capabilities specific to `WorkspaceEdit`s
        		 */
        		workspaceEdit?: WorkspaceEditClientCapabilities;

        		/**
        		 * Capabilities specific to the `workspace/didChangeConfiguration`
        		 * notification.
        		 */
        		didChangeConfiguration?: DidChangeConfigurationClientCapabilities;

        		/**
        		 * Capabilities specific to the `workspace/didChangeWatchedFiles`
        		 * notification.
        		 */
        		didChangeWatchedFiles?: DidChangeWatchedFilesClientCapabilities;

        		/**
        		 * Capabilities specific to the `workspace/symbol` request.
        		 */
        		symbol?: WorkspaceSymbolClientCapabilities;

        		/**
        		 * Capabilities specific to the `workspace/executeCommand` request.
        		 */
        		executeCommand?: ExecuteCommandClientCapabilities;

        		/**
        		 * The client has support for workspace folders.
        		 *
        		 * @since 3.6.0
        		 */
        		workspaceFolders?: boolean;

        		/**
        		 * The client supports `workspace/configuration` requests.
        		 *
        		 * @since 3.6.0
        		 */
        		configuration?: boolean;

        		/**
        		 * Capabilities specific to the semantic token requests scoped to the
        		 * workspace.
        		 *
        		 * @since 3.16.0
        		 */
        		 semanticTokens?: SemanticTokensWorkspaceClientCapabilities;

        		/**
        		 * Capabilities specific to the code lens requests scoped to the
        		 * workspace.
        		 *
        		 * @since 3.16.0
        		 */
        		codeLens?: CodeLensWorkspaceClientCapabilities;

        		/**
        		 * The client has support for file requests/notifications.
        		 *
        		 * @since 3.16.0
        		 */
        		fileOperations?: {
        			/**
        			 * Whether the client supports dynamic registration for file
        			 * requests/notifications.
        			 */
        			dynamicRegistration?: boolean;

        			/**
        			 * The client has support for sending didCreateFiles notifications.
        			 */
        			didCreate?: boolean;

        			/**
        			 * The client has support for sending willCreateFiles requests.
        			 */
        			willCreate?: boolean;

        			/**
        			 * The client has support for sending didRenameFiles notifications.
        			 */
        			didRename?: boolean;

        			/**
        			 * The client has support for sending willRenameFiles requests.
        			 */
        			willRename?: boolean;

        			/**
        			 * The client has support for sending didDeleteFiles notifications.
        			 */
        			didDelete?: boolean;

        			/**
        			 * The client has support for sending willDeleteFiles requests.
        			 */
        			willDelete?: boolean;
        		};

        		/**
        		 * Client workspace capabilities specific to inline values.
        		 *
        		 * @since 3.17.0
        		 */
        		inlineValue?: InlineValueWorkspaceClientCapabilities;

        		/**
        		 * Client workspace capabilities specific to inlay hints.
        		 *
        		 * @since 3.17.0
        		 */
        		inlayHint?: InlayHintWorkspaceClientCapabilities;

        		/**
        		 * Client workspace capabilities specific to diagnostics.
        		 *
        		 * @since 3.17.0.
        		 */
        		diagnostics?: DiagnosticWorkspaceClientCapabilities;
        	};

        	/**
        	 * Text document specific client capabilities.
        	 */
        	textDocument?: TextDocumentClientCapabilities;

        	/**
        	 * Capabilities specific to the notebook document support.
        	 *
        	 * @since 3.17.0
        	 */
        	notebookDocument?: NotebookDocumentClientCapabilities;

        	/**
        	 * Window specific client capabilities.
        	 */
        	window?: {
        		/**
        		 * It indicates whether the client supports server initiated
        		 * progress using the `window/workDoneProgress/create` request.
        		 *
        		 * The capability also controls Whether client supports handling
        		 * of progress notifications. If set servers are allowed to report a
        		 * `workDoneProgress` property in the request specific server
        		 * capabilities.
        		 *
        		 * @since 3.15.0
        		 */
        		workDoneProgress?: boolean;

        		/**
        		 * Capabilities specific to the showMessage request
        		 *
        		 * @since 3.16.0
        		 */
        		showMessage?: ShowMessageRequestClientCapabilities;

        		/**
        		 * Client capabilities for the show document request.
        		 *
        		 * @since 3.16.0
        		 */
        		showDocument?: ShowDocumentClientCapabilities;
        	};

        	/**
        	 * General client capabilities.
        	 *
        	 * @since 3.16.0
        	 */
        	general?: {
        		/**
        		 * Client capability that signals how the client
        		 * handles stale requests (e.g. a request
        		 * for which the client will not process the response
        		 * anymore since the information is outdated).
        		 *
        		 * @since 3.17.0
        		 */
        		staleRequestSupport?: {
        			/**
        			 * The client will actively cancel the request.
        			 */
        			cancel: boolean;

        			/**
        			 * The list of requests for which the client
        			 * will retry the request if it receives a
        			 * response with error code `ContentModified``
        			 */
        			 retryOnContentModified: string[];
        		}

        		/**
        		 * Client capabilities specific to regular expressions.
        		 *
        		 * @since 3.16.0
        		 */
        		regularExpressions?: RegularExpressionsClientCapabilities;

        		/**
        		 * Client capabilities specific to the client's markdown parser.
        		 *
        		 * @since 3.16.0
        		 */
        		markdown?: MarkdownClientCapabilities;

        		/**
        		 * The position encodings supported by the client. Client and server
        		 * have to agree on the same position encoding to ensure that offsets
        		 * (e.g. character position in a line) are interpreted the same on both
        		 * side.
        		 *
        		 * To keep the protocol backwards compatible the following applies: if
        		 * the value 'utf-16' is missing from the array of position encodings
        		 * servers can assume that the client supports UTF-16. UTF-16 is
        		 * therefore a mandatory encoding.
        		 *
        		 * If omitted it defaults to ['utf-16'].
        		 *
        		 * Implementation considerations: since the conversion from one encoding
        		 * into another requires the content of the file / line the conversion
        		 * is best done where the file is read which is usually on the server
        		 * side.
        		 *
        		 * @since 3.17.0
        		 */
        		positionEncodings?: PositionEncodingKind[];
        	};

        	/**
        	 * Experimental client capabilities.
        	 */
        	experimental?: LSPAny;
        }
        """;
}
