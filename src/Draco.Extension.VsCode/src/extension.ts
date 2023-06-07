import * as vscode from "vscode";
import { createLogChannel } from "./logging";
import { activateLangserver, startLanguageServer, stopLanguageServer } from "./langserver";
import { activateDebugAdapter } from "./debugadapter";

export async function activate(context: vscode.ExtensionContext): Promise<void> {
    // Create a log channel
    const logChannel = createLogChannel();
    context.subscriptions.push(logChannel);

    // Subscribe everything the langserver needs
    activateLangserver(context);

    // Subscribe everything the debug adapter needs
    activateDebugAdapter(context);

    // Start the langserver
    await startLanguageServer();
}

export async function deactivate(): Promise<void> {
    await stopLanguageServer();
}
