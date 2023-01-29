import * as proc from "child_process";
import { PathLike } from "fs";
import * as fs from "fs/promises";
import * as lsp from "vscode-languageclient/node";
import { ConfigurationTarget, MessageItem, TextEditor, Uri, window, workspace, WorkspaceConfiguration } from "vscode";
import { FatalError } from "./errors";
import { prompt, PromptKind, PromptResult } from "./prompt";
import { logError } from "./logging";

/**
 * The language server package name.
 */
const DracoLangserverToolName = 'Draco.LanguageServer';

/**
 * The language server command that can be used to start it up.
 */
const DracoLangserverCommandName = 'draco-langserver';

/**
 * Reads options for the language server.
 * The settings are also validated accordingly, and prompted for editing if needed.
 * @returns The read out @see ServerOptions, or @constant undefined, if the language server
 * can not, or should not be started.
 */
export async function getLanguageServerOptions(): Promise<lsp.ServerOptions | undefined> {
    const config = workspace.getConfiguration('draco');

    // First off, check if the dotnet command is available by checking the version
    const dotnetCommand = config.get<string>('dotnetCommand');
    const dotnetVersionExec = await executeCommand(`${dotnetCommand} --version`);
    if (dotnetVersionExec.exitCode !== 0) {
        await askUserToOpenSettings(PromptKind.error, 'Could not locate the dotnet tool.');
        logError('Could not locate the dotnet tool.', dotnetVersionExec);
        return undefined;
    }

    // Dotnet is available, chech if the language server is installed
    let justInstalled = false;
    {
        // Try to get a list of the global tools
        const globalToolsResult = await executeCommand(`${dotnetCommand} tool list --global`);
        if (globalToolsResult.exitCode != 0) {
            await window.showErrorMessage('Failed to retrieve the list of dotnet tools.');
            logError('Failed to retrieve the list of dotnet tools.', globalToolsResult);
            return undefined;
        }
        // Check, if the output contains the tool name
        const regExp = new RegExp(`\\b${escapeRegExp(DracoLangserverToolName.toLowerCase())}\\b`);
        const langserverInstalled = regExp.test(globalToolsResult.stdout);
        if (!langserverInstalled) {
            // Language server is not installed
            if (!config.get<boolean>('promptInstallDracoLangserver')) {
                // We can't even prompt the user
                return undefined;
            }
            // Ask the user for installation
            if (!await askUserToInstallLangserver('Draco language server is not installed.')) {
                // Installation failed one way or another
                return undefined;
            }
            // Installation was successful
            justInstalled = true;
        }
    }

    // The language server is available locally, check for updates
    // We only check if we haven't just installed, otherwise why bother
    if (!justInstalled && config.get<boolean>('promptUpdateDracoLangserver')) {
        const checkForUpdateResult = await executeCommand(`${DracoLangserverCommandName} check-for-updates`);
        if (checkForUpdateResult.exitCode == 0) {
            // No updates found
        }
        else if (checkForUpdateResult.exitCode == 1) {
            // Updates found
            await askUserToUpdateLangserver();
        }
        else {
            // Error
            await window.showErrorMessage('Failed to check for updates.');
            logError('Failed to check for updates.', checkForUpdateResult);
        }
    }

    return {
        command: `${DracoLangserverCommandName} run`,
		transport: lsp.TransportKind.stdio,
        options: {
            shell: true,
        },
    };
}

/**
 * Asks user for updating the language server.
 */
async function askUserToUpdateLangserver(): Promise<void> {
    const config = workspace.getConfiguration('draco');
    const dotnetCommand = config.get<string>('dotnetCommand');

    // Prompt
    const promptResult = await promptUpdateLangserver('There is a new version of Draco language server available. Would you like to update?');
    if (promptResult != PromptResult.yes) {
        return;
    }

    // We want to update
    const execResult = await executeCommand(`${dotnetCommand} tool update ${DracoLangserverToolName} --global`);
    if (execResult.exitCode == 0) {
        await window.showInformationMessage('Draco language server updated successfully.');
    } else {
        await window.showErrorMessage('Failed to update Draco language server.');
        logError('Failed to update Draco language server.', execResult);
    }
}

/**
 * Prompts the user for updating the language server. Does not actually do the installation.
 * @param message The message to display in the prompt.
 * @returns The chosen @see PromptResult.
 */
function promptUpdateLangserver(message: string): Promise<PromptResult> {
    return promptYesNoDisable('promptUpdateDracoLangserver', PromptKind.info, message);
}

/**
 * Asks user for installing the language server.
 * @param reason The reason the language needs to be installed.
 * @returns True, if the tool got installed, false otherwise.
 */
async function askUserToInstallLangserver(reason: string): Promise<boolean> {
    const config = workspace.getConfiguration('draco');
    const canPrompt = config.get<boolean>('promptOpenSettings');

    if (!canPrompt) {
        // We can't even prompt about installing, just error out
        await window.showErrorMessage(reason);
        return false;
    }

    // Prompt the user
    const promptResult = await promptInstallLangserver(`${reason} Install it now?`);
    if (promptResult === PromptResult.yes) {
        // Attempt to install
        const dotnetCommand = config.get<string>('dotnetCommand');
        const sdkVersion = config.get<string>('dracoSdkVersion');
        const execResult = await executeCommand(`${dotnetCommand} tool install ${DracoLangserverToolName} --version ${sdkVersion} --global`);
        if (execResult.exitCode != 0) {
            // Installation failed
            await window.showErrorMessage('Failed to install Draco language server.');
            logError('Failed to install Draco language server.', execResult);
            return false;
        }
        // Installation succeeded
        return true;
    }

    // User didn't want to install
    return false;
}

/**
 * Prompts the user for installing the language server. Does not actually do the installation.
 * @param message The message to display in the prompt.
 * @returns The chosen @see PromptResult.
 */
function promptInstallLangserver(message: string): Promise<PromptResult> {
    return promptYesNoDisable('promptInstallDracoLangserver', PromptKind.info, message);
}

/**
 * Asksthe user if they want to open the settings file. If so, the settings file is opened.
 * @param kind The @see PromptKind to display.
 * @param reason The reason the settings needs to be opened.
 */
async function askUserToOpenSettings(kind: PromptKind, reason: string): Promise<void> {
    const config = workspace.getConfiguration('draco');
    const settingsUri = await getVscodeFileUri('settings.json');
    const canPrompt = config.get<boolean>('promptOpenSettings');

    // NOTE: Is this how URIs should be compared?
    if (!canPrompt || window.activeTextEditor?.document.uri.fsPath == settingsUri.fsPath) {
        // If the settings panel is currently open, we prompt slightly differently
        // We do that if we can't prompt as well
        await prompt(kind, reason);
        return;
    }

    // The settings window is not open
    if (await exists(settingsUri.fsPath)) {
        // The settings path exists, just prompt to open it
        const result = await promptOpenSettings(kind, `${reason} Open settings?`);
        if (result === PromptResult.yes) {
            await openDocument(settingsUri);
        }
        return;
    }

    // The settings file does not even exist yet
    {
        const result = await promptOpenSettings(kind, `${reason} Create settings?`);
        if (result === PromptResult.yes) {
            await writeDefaultSettings(ConfigurationTarget.Workspace);
            await openDocument(settingsUri);
        }
    }
}

/**
 * Prompts the user for opening the settings. Does not actually open the settings.
 * @param kind The @see PromptKind to display.
 * @param message The message to display in the prompt.
 * @returns The chosen @see PromptResult.
 */
function promptOpenSettings(kind: PromptKind, message: string): Promise<PromptResult> {
    return promptYesNoDisable('promptOpenSettings', kind, message);
}

/**
 * Prompts the user with 3 options, Yes, No and Disable.
 * If the user picks Disable, the setting will be flipped and the prompt won't appear again.
 * @param setting The setting to save the prompt result to.
 * @param kind The @see PromptKind to display.
 * @param message The message to display.
 * @returns The promise to the @see PromptResult.
 */
async function promptYesNoDisable(setting: string, kind: PromptKind, message: string): Promise<PromptResult> {
    const config = workspace.getConfiguration('draco');

    // If we don't allow prompting, don't bother
    if (!config.get<boolean>(setting)) {
        return PromptResult.disable;
    }

    // Actually ask the user
    const result = await prompt(
        kind,
        message,
        { title: 'Yes', result: PromptResult.yes },
        { title: 'No', result: PromptResult.no },
        { title: "Don't Ask Again", result: PromptResult.disable });

    // In case the user wants to disable it, save that option
    if (result === PromptResult.disable) {
        await config.update(setting, false, ConfigurationTarget.Workspace);
    }

    return result;
}

/**
 * Writes a useful set of default settings.
 * @param target The @see ConfigurationTarget to write the defaults to.
 * @returns The promise that finishes, when all settings are updated.
 */
function writeDefaultSettings(target: ConfigurationTarget): Promise<void[]> {
    const settingsToInclude = [
        'dotnetCommand',
        'dracoSdkVersion',
    ];
    const config = workspace.getConfiguration('draco');
    return Promise.all(settingsToInclude.map(setting =>
        config.update(setting, config.get(setting), target)));
}

/**
 * Opens a document in the editor.
 * @param uri The @see Uri of the document to open.
 * @returns The promise of the opened editor.
 */
async function openDocument(uri: Uri): Promise<TextEditor> {
    const textDocument = await workspace.openTextDocument(uri);
    return window.showTextDocument(textDocument);
}

/**
 * Retrieves the URI for a file in the '.vscode' folder.
 * @param fileName The name of the file to get the path of.
 * @return The path to the @see Uri of the file in the '.vscode' folder.
 */
async function getVscodeFileUri(fileName: string): Promise<Uri> {
    const workspaceUri = getWorkspaceUri();
    return Uri.joinPath(workspaceUri, '.vscode', fileName);
}

/**
 * Retrieves the relevant workspace URI.
 * @returns The @see Uri of the current workspace.
 */
function getWorkspaceUri(): Uri {
    if (workspace.workspaceFolders === undefined || workspace.workspaceFolders.length === 0) {
        throw new FatalError('No workspace is open.');
    }
    // NOTE: We are resolving to only the first one, might not be the best to do
    return workspace.workspaceFolders[0].uri;
}

/**
 * Describes the execution of a command.
 */
type Execution = {
    /**
     * The invoking command.
     */
    command: string;

    /**
     * The standard output of the command.
     */
    stdout: string;

    /**
     * The standard error output of the command.
     */
    stderr: string;

    /**
     * The exit code of the command.
     */
    exitCode: number;
};

/**
 * Executes the given command.
 * @param command
 * @returns The execution result of the command.
 */
function executeCommand(command: string): Promise<Execution> {
    return new Promise((resolve, reject) => {
        proc.exec(command, (err, stdout, stderr) => resolve({
            command: command,
            stdout: stdout,
            stderr: stderr,
            exitCode: err?.code || 0,
        }));
    });
}

/**
 * Checks if the given path exists and is available for writing.
 * @param path The path to check.
 * @returns True, if the path exists and can be written, false otherwise.
 */
async function exists(path: PathLike): Promise<boolean> {
    try {
        await fs.access(path, fs.constants.W_OK);
        return true;
    } catch {
        return false;
    }
}

/**
 * Source: https://stackoverflow.com/questions/3446170/escape-string-for-use-in-javascript-regex
 */
function escapeRegExp(s: string) {
    return s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'); // $& means the whole matched string
}
