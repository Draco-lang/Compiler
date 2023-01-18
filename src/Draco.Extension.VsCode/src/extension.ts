import * as path from 'path';
import * as vscode from 'vscode';
import * as lsp from 'vscode-languageclient/node';

let languageClient: lsp.LanguageClient;

export function activate(context: vscode.ExtensionContext) {
    // Path for the server
	let serverPath = context.asAbsolutePath(path.join('out', 'Draco.LanguageServer.exe'));
	context.subscriptions.push(vscode.commands.registerCommand("draco.run", runDraco));
    // Server options
	let serverOptions: lsp.ServerOptions = {
		command: serverPath,
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

function runDraco(){
	vscode.workspace.textDocuments.forEach(x => x.save());
	const terminal = typeof(vscode.window.activeTerminal) === "undefined" 
	? vscode.window.createTerminal()
	: vscode.window.activeTerminal;
	terminal.show(true);
	var project = "";
	if(typeof vscode.workspace.workspaceFolders !== "undefined" && vscode.workspace.workspaceFolders.length > 0){
		project = `--project ${vscode.workspace.workspaceFolders[0].uri.fsPath}`
	}
	terminal.sendText(`dotnet run ${project}`, true);
}