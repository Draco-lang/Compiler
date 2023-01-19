import { start } from "repl";
import * as vscode from "vscode";
import { window, workspace } from "vscode";
import * as lsp from "vscode-languageclient/node";
import { prompt, PromptResult } from "./prompt";
import * as settings from "./settings";

let languageClient: lsp.LanguageClient | undefined;

export async function activate(context: vscode.ExtensionContext): Promise<void> {
    context.subscriptions.push(vscode.commands.registerCommand("draco.run", runDraco));
    // Subscribe to setting changes, and prompt the user if the language server should be restarted
    const listener = workspace.onDidChangeConfiguration(async event => {
        if (event.affectsConfiguration('draco')) {
            if (languageClient !== undefined) {
                // Langserver is already running, ask if it should be restarted
                const promptResult = await prompt(
                    'Settings changed. Restart Draco language server?',
                    { title: 'Yes', result: PromptResult.yes },
                    { title: 'No', result: PromptResult.no });
                if (promptResult == PromptResult.yes) {
                    await startLanguageServer();
                }
            } else {
                // Just start the language server, it was not running
                // NOTE: Is this a good idea?
                await startLanguageServer();
            }
        }
    });
    context.subscriptions.push(listener);

    await startLanguageServer();
}

export async function deactivate(): Promise<void> {
	await stopLanguageServer();
}

async function stopLanguageServer() {
    if (languageClient) {
        await languageClient.stop();
        languageClient = undefined;
    }
}

async function startLanguageServer(): Promise<void> {
    // If there's a client running already, stop it
    await stopLanguageServer();

    // Server options
	let serverOptions = await settings.getLanguageServerOptions();
    if (serverOptions === undefined) {
        await window.showErrorMessage('Could not start Draco language server.');
        return;
    }

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
	await languageClient.start();
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