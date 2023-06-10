import * as vscode from "vscode";
import { registerLanguageServer, startLanguageServer, stopLanguageServer } from "./language_server";
import { registerDebugAdapter } from "./debug_adapter";
import { registerCommandHandlers } from "./commands";

export function activate(context: vscode.ExtensionContext): Promise<void> {
    // Subscribe commands
    registerCommandHandlers(context);

    // Subscribe everything the langserver needs
    registerLanguageServer(context);

    // Subscribe everything the debug adapter needs
    registerDebugAdapter(context);

    // Start the langserver
    return startLanguageServer();
}

export function deactivate(): Promise<void> {
    return stopLanguageServer();
}
