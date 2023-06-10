/**
 * User interaction flows factored out from core logic.
 * Most logic in here should only be querying settings, showing prompts and calling out to other APIs.
 */

import { ConfigurationTarget, window, workspace } from "vscode";
import { checkForDotnetToolUpdates, installDotnetTool, isDotnetToolAvailable, updateDotnetTool } from "./tools";
import { PromptKind, PromptResult, prompt } from "./prompt";

async function interactivelyInitializeDotnetTool(toolName: string, toolDisplayName: string): Promise<boolean> {
    // Check, if the tool is installed
    const isInstalledResult = await isDotnetToolAvailable(toolName);
    if (isInstalledResult.isErr) {
        const errMessage = isInstalledResult.unwrapErr().message;
        await window.showErrorMessage(`Failed to check for ${toolDisplayName}.\n${errMessage}`);
        return false;
    }

    const isInstalled = isInstalledResult.unwrap();
    if (!isInstalled) {
        // Not installed yet, ask the user
        const shouldInstall = await promptYesNoDisable(
            PromptKind.info,
            `${toolDisplayName} is not installed. Would you like to install it?`,
            'TODO: Setting');
        if (shouldInstall != PromptResult.yes) {
            // Should not install, tool isn't installed
            return false;
        }

        // Try to install it
        const installResult = await installDotnetTool(toolName);
        if (installResult.isErr) {
            const errMessage = installResult.unwrapErr().message;
            await window.showErrorMessage(`Failed to install ${toolDisplayName}.\n${errMessage}`);
            return false;
        }
        // We installed successfully
        await window.showInformationMessage(`${toolDisplayName} installed successfully.`);
        return true;
    }

    // Tool is already installed, check for updates
    const checkForUpdateResult = await checkForDotnetToolUpdates(toolName);
    if (checkForUpdateResult.isErr) {
        const errMessage = checkForUpdateResult.unwrapErr().message;
        await window.showErrorMessage(`Could not check for updates for ${toolDisplayName}.\n${errMessage}`);
        // We could not check for updates, but we do have a local install, we can use that
        return true;
    }

    const areThereUpdates = checkForUpdateResult.unwrap();
    if (!areThereUpdates) {
        return true;
    }

    // There are updates, ask
    const doUpdateResponse = await promptYesNoDisable(
        PromptKind.info,
        `There are updates available for ${toolDisplayName}. Would you like to update?`,
        'TODO: SETTING');
    if (doUpdateResponse != PromptResult.yes) {
        // We don't want to update
        return true;
    }

    // We want to update
    const updateResult = await updateDotnetTool(toolName);
    if (updateResult.isErr) {
        const errMessage = updateResult.unwrapErr().message;
        await window.showErrorMessage(`Could not updates ${toolDisplayName}.\n${errMessage}`);
        // We could not update, but we do have a local install, we can use that
        return true;
    }

    // We updated successfully
    await window.showInformationMessage(`${toolDisplayName} updated successfully.`);
    return true;
}

/**
 * Prompts the user with 3 options, Yes, No and Disable.
 * If the user picks Disable, the setting will be flipped and the prompt won't appear again.
 * The disable option is only available, if a setting name is provided
 * @param kind The @see PromptKind to display.
 * @param message The message to display.
 * @param setting The setting to save the prompt result to.
 * @returns The promise to the @see PromptResult.
 */
async function promptYesNoDisable(kind: PromptKind, message: string, setting?: string): Promise<PromptResult> {
    const config = workspace.getConfiguration('draco');

    // If we don't allow prompting, don't bother
    if (setting && !config.get<boolean>(setting)) {
        return PromptResult.disable;
    }

    // Assemble items
    const promptItems = [
        { title: 'Yes', result: PromptResult.yes },
        { title: 'No', result: PromptResult.no },
    ];
    if (setting) {
        promptItems.push({ title: "Don't Ask Again", result: PromptResult.disable });
    }

    // Actually ask the user
    const result = await prompt(kind, message, ...promptItems);

    // In case the user wants to disable it, save that option
    if (setting && result === PromptResult.disable) {
        await config.update(setting, false, ConfigurationTarget.Workspace);
    }

    return result;
}
