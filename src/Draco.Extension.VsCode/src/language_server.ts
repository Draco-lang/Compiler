/**
 * Handling the language server lifecycle.
 */

import * as vscode from "vscode";
import * as lsp from "vscode-languageclient/node";
import { LanguageServerCommandName, LanguageServerToolName } from "./tools";
import { interactivelyInitializeLanguageServer } from "./user_flow";
import { workspace } from "vscode";
import { PromptKind, PromptResult, prompt } from "./prompt";

let languageClient: lsp.LanguageClient | undefined;

/**
 * Registers functionality related to the language server lifecycle, like rebooting on setting changes.
 * @param context
 */
export function registerLanguageServer(context: vscode.ExtensionContext) {
    // Subscribe to setting changes, and prompt the user if the language server should be restarted
    context.subscriptions.push(workspace.onDidChangeConfiguration(async event => {
        if (event.affectsConfiguration('draco')) {
            if (languageClient) {
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

/**
 * Stops the language server.
 */
export async function stopLanguageServer() {
    if (!languageClient) {
        return;
    }
    await languageClient.stop();
    languageClient.dispose();
    languageClient = undefined;
}

/**
 * Starts the language server.
 */
export async function startLanguageServer() {
    // If there's a client running already, stop it
    await stopLanguageServer();

    // Check, if we can even run the langserver
    if (!await interactivelyInitializeLanguageServer()) {
        return;
    }

    // Server options
    let serverOptions: lsp.ServerOptions = {
        command: `${LanguageServerCommandName} run`,
        transport: lsp.TransportKind.stdio,
        options: {
            shell: true,
        },
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
    await languageClient.start();
}
