/**
 * Tooling integration to abstract away CLI interactions.
 * This mainly means CLI tooling, like the dotnet command, language server and debug adapter.
 */

import { spawn } from "child_process";
import { exitCode } from "process";
import { workspace } from "vscode";
import { Result } from "./result";

/**
 * The language server package name.
 */
const LanguageServerToolName = 'Draco.LanguageServer';

/**
 * The language server command that can be used to start it up.
 */
const LanguageServerCommandName = 'draco-langserver';

/**
 * The debug adapter package name.
 */
const DebugAdapterToolName = 'Draco.DebugAdapter';

/**
 * The debug adapter command that can be used to start it up.
 */
const DebugAdapterCommandName = 'draco-debugadapter';

/**
 * Checks, if a given .NET tool has updates. It works by passing in a 'check-for-updates' flag for the tool,
 * the tool does the checking by itself. Expects exit code 0 to mean no updates and 1 to mean updates available.
 * @param toolName The tool name.
 * @returns @constant true, if a .NET tool with name @param toolName has updates,
 * @constant false if not, an error result if there was an error.
 */
export async function checkForDotnetToolUpdates(toolName: string): Promise<Result<boolean>> {
    const resultExecute = Result.wrapAsync(executeCommand);
    return (await resultExecute(toolName, 'check-for-updates'))
        .bind(ok => ok.exitCode === 0 || ok.exitCode === 1
            ? Result.ok(ok.exitCode === 1)
            : Result.err(new Error(`command ${ok.command} returned with nonzero (${ok.exitCode}) exit-code`)));
}

/**
 * Checks, if a given .NET tool is installed globally.
 * @param toolName The tool name.
 * @returns @constant true, if a .NET tool with name @param toolName is installed globally,
 * @constant false if not, an error result if there was an error.
 */
export async function isDotnetToolAvailable(toolName: string): Promise<Result<boolean>> {
    const dotnet = getDotnetCommand();
    const regExp = new RegExp(`\\b${escapeRegExp(toolName.toLowerCase())}\\b`);

    return (await safeExecuteCommand(dotnet, 'tool', 'list', '--global'))
        .map(ok => ok.stdout)
        .map(regExp.test);
}

/**
 * Installs a .NET tool globally.
 * @param toolName The name of the .NET tool.
 * @param version The version filter for the tool.
 * @returns Ok, if the dotnet CLI is available, an error result otherwise.
 */
export async function installDotnetTool(toolName: string, version: string): Promise<Result<void>> {
    const dotnet = getDotnetCommand();
    return (await safeExecuteCommand(dotnet, 'tool', 'install', toolName, '--version', version, '--global'))
        .map(_ => {});
}

/**
 * Updates a .NET tool globally.
 * @param toolName The name of the .NET tool.
 * @returns Ok, if the dotnet CLI is available, an error result otherwise.
 */
export async function updateDotnetTool(toolName: string): Promise<Result<void>> {
    const dotnet = getDotnetCommand();
    return (await safeExecuteCommand(dotnet, 'tool', 'update', toolName, '--global'))
        .map(_ => {});
}

/**
 * Checks, if the dotnet CLI tool is available.
 * @returns Ok, if the dotnet CLI is available, an error result otherwise.
 */
export async function isDotnetCommandAvailable(): Promise<Result<void>> {
    const dotnet = getDotnetCommand();
    return (await safeExecuteCommand(dotnet, '--version')).map(_ => {});
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
 * A wrapper around @see executeCommand that returns a result and only succeeds on 0 exit code.
 * @param command The command to run.
 * @param args The command arguments.
 * @returns The execution result of the command.
 */
async function safeExecuteCommand(command: string, ...args: string[]): Promise<Result<Execution>> {
    const resultExecute = Result.wrapAsync(executeCommand);
    const result = await resultExecute(command, ...args);
    return result.bind(ok => ok.exitCode === 0
        ? Result.ok(ok)
        : Result.err(new Error(`command ${ok.command} returned with nonzero (${ok.exitCode}) exit-code`)));
}

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
            command: `${command} ${args.join(' ')}`,
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
