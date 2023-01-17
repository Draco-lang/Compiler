import * as proc from "child_process";
import { ServerOptions } from "https";
import { ConfigurationTarget, window, workspace, WorkspaceConfiguration } from "vscode";

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
