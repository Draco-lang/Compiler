/**
 * Commands added by the extension.
 */

import { ExtensionContext, commands, window } from "vscode";
import { DebugAdapterToolName, LanguageServerToolName, checkForDotnetToolUpdates, installDotnetTool, updateDotnetTool } from "./tools";

/**
 * Registers the command handlers supported by this extension.
 * @param context The extension context.
 */
export function registerCommands(context: ExtensionContext) {
    context.subscriptions.push(commands.registerCommand(
        'draco.installLanguageServer',
        () => installDotnetToolCommandHandler({ toolName: LanguageServerToolName, toolDisplayName: 'Language Server' })));
    context.subscriptions.push(commands.registerCommand(
        'draco.updateLanguageServer',
        () => updateDotnetToolCommandHandler({ toolName: LanguageServerToolName, toolDisplayName: 'Language Server' })));

    context.subscriptions.push(commands.registerCommand(
        'draco.installDebugAdapter',
        () => installDotnetToolCommandHandler({ toolName: DebugAdapterToolName, toolDisplayName: 'Debug Adapter' })));
    context.subscriptions.push(commands.registerCommand(
        'draco.updateDebugAdapter',
        () => updateDotnetToolCommandHandler({ toolName: DebugAdapterToolName, toolDisplayName: 'Debug Adapter' })));
}

/**
 * Attempts to install a .NET tool.
 * @param config The tool configuration.
 */
async function installDotnetToolCommandHandler(config: {
    toolName: string;
    toolDisplayName: string;
}): Promise<void> {
    // Try to install it
    const installResult = await installDotnetTool(config.toolName);
    if (installResult.isErr) {
        const errMessage = installResult.unwrapErr().message;
        await window.showErrorMessage(`Failed to install ${config.toolDisplayName}.\n${errMessage}`);
        return;
    }
    // We installed successfully
    await window.showInformationMessage(`${config.toolDisplayName} installed successfully.`);
}

/**
 * Attempts to update a .NET tool.
 * @param config The tool configuration.
 */
async function updateDotnetToolCommandHandler(config: {
    toolName: string;
    toolDisplayName: string;
}): Promise<void> {
    // Check for updates
    const checkForUpdateResult = await checkForDotnetToolUpdates(config.toolName);
    if (checkForUpdateResult.isErr) {
        const errMessage = checkForUpdateResult.unwrapErr().message;
        await window.showErrorMessage(`Could not check for updates for ${config.toolDisplayName}.\n${errMessage}`);
        return;
    }

    const areThereUpdates = checkForUpdateResult.unwrap();
    if (!areThereUpdates) {
        // Already up to date
        await window.showInformationMessage(`${config.toolDisplayName} is already up to date.`);
        return;
    }

    // Try to update
    const updateResult = await updateDotnetTool(config.toolName);
    if (updateResult.isErr) {
        const errMessage = updateResult.unwrapErr().message;
        await window.showErrorMessage(`Could not updates ${config.toolDisplayName}.\n${errMessage}`);
        return;
    }

    // We updated successfully
    await window.showInformationMessage(`${config.toolDisplayName} updated successfully.`);
}
