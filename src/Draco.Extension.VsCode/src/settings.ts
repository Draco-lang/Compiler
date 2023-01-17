import * as proc from "child_process";
import { ServerOptions } from "https";
import { ConfigurationTarget, MessageItem, window, workspace, WorkspaceConfiguration } from "vscode";

/**
 * Reads options for the language server.
 * The settings are also validated accordingly, and prompted for editing if needed.
 * @returns The read out @see ServerOptions, or @constant undefined, if the language server
 * can not, or should not be started.
 */
export async function getLanguageServerOptions(): Promise<ServerOptions | undefined> {
    const config = workspace.getConfiguration('draco');

    // First off, check if the dotnet command is available by checking the version
    const dotnetCommand = config.get<string>('dotnetCommand');
    const foundDotnet = (await executeCommand(`${dotnetCommand} --version`)).exitCode === 0;
    if (!foundDotnet) {
        window.showErrorMessage('Could not locate the dotnet tool');
        // TODO
        return undefined;
    }

    return { };
}

async function askUserToOpenSettings(reason: string): Promise<void> {

}

/**
 * Prompts the user for opening the settings. Does not actually open the settings.
 * @param message The message to display in the prompt.
 * @returns The chosen @see PromptResult.
 */
async function promptOpenSettings(message: string): Promise<PromptResult> {
    const config = workspace.getConfiguration('draco');

    // If we don't allow the settings to be opened, don't bother
    if (!config.get<boolean>('promptOpenSettings')) {
        return PromptResult.disable;
    }

    // Actually ask the user
    const result = await prompt(
        message,
        { title: 'Yes', result: PromptResult.yes },
        { title: 'No', result: PromptResult.no },
        { title: "Don't Ask Again", result: PromptResult.disable });

    // In case the user wants to disable it, save that option
    if (result === PromptResult.disable) {
        await config.update('promptOpenSettings', false, ConfigurationTarget.Workspace);
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
 * Represents user choices in a prompt.
 */
enum PromptResult {
    /**
     * Positive answer.
     */
    yes,

    /**
     * Negative answer.
     */
    no,

    /**
     * Close without choosing.
     */
    cancel,

    /**
     * Negative answer, don't ask again.
     */
    disable,
}

/**
 * A single item in a prompt.
 */
interface PromptItem extends MessageItem {
    /**
     * The result that is returned, when the item is chosen.
     */
    result: PromptResult;
}

/**
 * Prompts the user.
 * @param message The message to print in the prompt.
 * @param items The @see PromptItems to choose from.
 * @returns The promise with the chosen @see PromptResult.
 * If the user closes the prompt without choosing an item, @see PromptResult.cancel is returned.
 */
async function prompt(message: string, ...items: PromptItem[]): Promise<PromptResult> {
    // Show the prompt
    const result = await window.showErrorMessage(message, ...items);
    // Map value
    return result?.result ?? PromptResult.cancel;
}

/**
 * Describes the execution of a command.
 */
type Execution = {
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
            stdout: stdout,
            stderr: stderr,
            exitCode: err?.code || 0,
        }));
    });
}
