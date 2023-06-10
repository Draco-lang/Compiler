import * as vscode from "vscode";
import { registerLanguageServer, startLanguageServer, stopLanguageServer } from "./language_server";
import { registerDebugAdapter } from "./debug_adapter";
import { registerCommandHandlers } from "./commands";
import { interactivelyCheckForDotnet, promptUserToCreateLaunchAndTasksConfig } from "./user_flow";

export async function activate(context: vscode.ExtensionContext) {
    if (!await interactivelyCheckForDotnet()) {
        // We really need .NET
        return;
    }

    // Subscribe commands
    registerCommandHandlers(context);

    // Subscribe everything the langserver needs
    registerLanguageServer(context);

    // Subscribe everything the debug adapter needs
    registerDebugAdapter(context);

    // Offer to create settings
    await promptUserToCreateLaunchAndTasksConfig();

    // Start the langserver
    await startLanguageServer();
}

export function deactivate(): Promise<void> {
    return stopLanguageServer();
}
