import * as vscode from "vscode";
import { window, workspace } from "vscode";
import * as lsp from "vscode-languageclient/node";
import { prompt, PromptKind, PromptResult } from "./prompt";
import * as settings from "./settings";

let languageClient: lsp.LanguageClient | undefined;

export function activateLangserver(context: vscode.ExtensionContext) {
    // Subscribe to setting changes, and prompt the user if the language server should be restarted
    context.subscriptions.push(workspace.onDidChangeConfiguration(async event => {
        if (event.affectsConfiguration('draco')) {
            if (languageClient !== undefined) {
                // Langserver is already running, ask if it should be restarted
                const promptResult = await prompt(
                    PromptKind.info,
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
    }));
}

export async function stopLanguageServer() {
    if (languageClient !== undefined) {
        await languageClient.stop();
        languageClient = undefined;
    }
}

export async function startLanguageServer(): Promise<void> {
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