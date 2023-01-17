import * as path from 'path';
import * as vscode from 'vscode';
import * as lsp from 'vscode-languageclient/node';

let languageClient: lsp.LanguageClient;

export function activate(context: vscode.ExtensionContext) {
    // Server options
	let serverOptions: lsp.ServerOptions = {
		command: 'draco-langserver',
		transport: lsp.TransportKind.stdio,
	};

	// Client options
	let clientOptions: lsp.LanguageClientOptions = {
		documentSelector: [{ scheme: 'file', language: 'draco' }],
	};

	languageClient = new lsp.LanguageClient(
		"dracoLanguageServer",
		'Draco Language Server',
		serverOptions,
		clientOptions,
	);

	// Start the client, which also starts the server
	languageClient.start();
}

export function deactivate(): Thenable<void> | undefined {
	if (!languageClient) return undefined;
	return languageClient.stop();
}
