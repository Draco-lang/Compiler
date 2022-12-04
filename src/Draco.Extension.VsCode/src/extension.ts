import * as path from 'path';
import * as vscode from 'vscode';
import * as lsp from 'vscode-languageclient/node';
import { TextDecoder, TextEncoder } from 'util';

let languageClient: lsp.LanguageClient;

export function activate(context: vscode.ExtensionContext) {
	context.subscriptions.push(vscode.workspace.registerNotebookSerializer("draco-notebook", new DracoNotebookSerilizer()));
	context.subscriptions.push(vscode.commands.registerCommand("draco.notebook.switch", switchNotebookView));
    // Path for the server
	let serverPath = context.asAbsolutePath(path.join('out', 'Draco.LanguageServer.exe'));

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

function switchNotebookView(): any{
	if (vscode.window.visibleNotebookEditors.some(x=>x.notebook.notebookType === "draco-notebook") && vscode.window.activeTextEditor?.document.languageId === "draco"){
		vscode.window.activeNotebookEditor?.notebook.save();
		vscode.workspace.openTextDocument((vscode.window.activeNotebookEditor?.notebook.uri as vscode.Uri)).then(function (result) { vscode.window.showTextDocument(result)});
	}
	else if(vscode.window.visibleTextEditors.some(x=>x.document.languageId === "draco" && vscode.window.activeTextEditor?.document.languageId === "draco")){
		vscode.window.activeTextEditor?.document.save();
		vscode.workspace.openNotebookDocument((vscode.window.activeTextEditor?.document.uri as vscode.Uri)).then(function (result) { vscode.window.showNotebookDocument(result)});
		vscode.window.activeTextEditor?.hide();
	}
	else{
		vscode.window.showErrorMessage("Currently selected file is not draco file or notebook");
	}
}

class DracoNotebookSerilizer implements vscode.NotebookSerializer{
	async deserializeNotebook(content: Uint8Array, _token: vscode.CancellationToken): Promise<vscode.NotebookData> {
		var contents = new TextDecoder().decode(content);
		const cells: vscode.NotebookCellData[] = [new vscode.NotebookCellData(vscode.NotebookCellKind.Code, contents, "draco")];
		return new vscode.NotebookData(cells);
	  }
	
	  async serializeNotebook(data: vscode.NotebookData,_token: vscode.CancellationToken): Promise<Uint8Array> {
		return new TextEncoder().encode(data.cells.map(x => x.value).join('\n'));
	  }
}