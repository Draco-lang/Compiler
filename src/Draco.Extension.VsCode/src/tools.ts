/**
 * Tooling integration to abstract away CLI interactions.
 * This mainly means CLI tooling, like the dotnet command, language server and debug adapter.
 */

import { spawn } from "child_process";
import { exitCode } from "process";
import { workspace } from "vscode";

/**
 * Checks, if a given .NET tool is installed globally.
 * @param toolName The tool name.
 * @returns @constant true, if a .NET tool with name @param toolName is installed globally,
 * @constant false otherwise.
 */
async function isDotnetToolAvailable(toolName: string): Promise<boolean> {
    // Retrieve the list of global tools
    const dotnet = getDotnetCommand();
    const globalToolsResult = await executeCommand(dotnet, 'tool', 'list', '--global')
        .then(result => result.exitCode === 0 ? result.stdout : undefined)
        .catch(_ => undefined);
    if (!globalToolsResult) return false;

    // Check, if the output contains the tool name
    const regExp = new RegExp(`\\b${escapeRegExp(toolName.toLowerCase())}\\b`);
    const toolInstalled = regExp.test(globalToolsResult);
    return toolInstalled;
}

/**
 * Installs a .NET tool globally.
 * @param toolName The name of the .NET tool.
 * @param version The version filter for the tool.
 * @returns @constant true, if the tool was installed successfully, @constant false otherwise.
 */
async function installDotnetTool(toolName: string, version: string): Promise<boolean> {
    const dotnet = getDotnetCommand();
    const installToolResult = await executeCommand(dotnet, 'tool', 'install', toolName, '--version', version, '--global')
        .then(result => result.exitCode === 0)
        .catch(_ => false);
    return installToolResult;
}

/**
 * Checks, if the dotnet CLI tool is available.
 * @returns @constant true, if the dotnet CLI is available, @constant false otherwise.
 */
async function isDotnetCommandAvailable(): Promise<boolean> {
    const dotnet = getDotnetCommand();
    const execResult = await executeCommand(dotnet, '--version')
        .then(_ => exitCode === 0)
        .catch(_ => false);
    return execResult;
}

/**
 * Retrieves the command to invoke the dotnet CLI.
 * @returns The command to the dotnet CLI.
 */
function getDotnetCommand(): string {
    const config = workspace.getConfiguration('draco');
    return config.get<string>('dotnetCommand') || 'dotnet';
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
 * @param command The command to run.
 * @param args The command arguments.
 * @returns The execution result of the command.
 */
function executeCommand(command: string, ...args: string[]): Promise<Execution> {
    return new Promise<Execution>((resolve, reject) => {
        const childProcess = spawn(command, args);

        let stdout = '';
        let stderr = '';

        childProcess.stdout.on('data', data =>stdout += data.toString());
        childProcess.stderr.on('data', data => stderr += data.toString());
        childProcess.on('error', error => reject(error));
        childProcess.on('close', exitCode => resolve({
            command,
            stdout,
            stderr,
            exitCode: exitCode || 0,
        }));
    });
}

/**
 * Source: https://stackoverflow.com/questions/3446170/escape-string-for-use-in-javascript-regex
 */
function escapeRegExp(s: string) {
    return s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'); // $& means the whole matched string
}
