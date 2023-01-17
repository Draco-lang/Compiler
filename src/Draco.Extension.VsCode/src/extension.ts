import { start } from "repl";
import * as vscode from "vscode";
import { workspace } from "vscode";
import * as lsp from "vscode-languageclient/node";
import { prompt, PromptResult } from "./prompt";
import * as settings from "./settings";

let languageClient: lsp.LanguageClient | undefined;

export async function activate(context: vscode.ExtensionContext): Promise<void> {
    // Subscribe to setting changes, and prompt the user if the language server should be restarted
    const listener = workspace.onDidChangeConfiguration(async event => {
        if (event.affectsConfiguration('draco') && languageClient !== undefined) {
            const promptResult = await prompt(
                'Settings changed. Restart Draco language server?',
                { title: 'Yes', result: PromptResult.yes },
                { title: 'No', result: PromptResult.no });
            if (promptResult == PromptResult.yes) {
                startLanguageServer();
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
    // Server options
	let serverOptions = await settings.getLanguageServerOptions();
    if (serverOptions === undefined) {
        return;
    }

    // If there's a client running already, stop it
    await stopLanguageServer();

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
