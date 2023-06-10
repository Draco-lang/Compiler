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
export const LanguageServerCommandName = 'draco-langserver';

/**
 * The debug adapter package name.
 */
const DebugAdapterToolName = 'Draco.DebugAdapter';

/**
 * The debug adapter command that can be used to start it up.
 */
export const DebugAdapterCommandName = 'draco-debugadapter';

/**
 * Checks, if the language server tool is installed.
 * @returns @constant true, if the language server tool is installed, @constant false if not,
 * an error result if an error happened.
 */
export function isLanguageServerInstalled(): Promise<Result<boolean>> {
    return isDotnetToolAvailable(LanguageServerToolName);
}

/**
 * Attempts to install the language server.
 * @returns Ok result, if the language server tool is installed successfully, error otherwise.
 */
export function installLanguageServer(): Promise<Result<void>> {
    const config = workspace.getConfiguration('draco');
    const sdkVersion = config.get<string>('dracoSdkVersion') || '*';
    return installDotnetTool(LanguageServerToolName, sdkVersion);
}

/**
 * Checks, if the debug adapter tool is installed.
 * @returns @constant true, if the debug adapter tool is installed, @constant false if not,
 * an error result if an error happened.
 */
export function isDebugAdapterInstalled(): Promise<Result<boolean>> {
    return isDotnetToolAvailable(DebugAdapterToolName);
}

/**
 * Attempts to install the debug adapter.
 * @returns Ok result, if the debug adapter tool is installed successfully, error otherwise.
 */
export function installDebugAdapter(): Promise<Result<void>> {
    const config = workspace.getConfiguration('draco');
    const sdkVersion = config.get<string>('dracoSdkVersion') || '*';
    return installDotnetTool(DebugAdapterToolName, sdkVersion);
}

/**
 * Checks, if a given .NET tool is installed globally.
 * @param toolName The tool name.
 * @returns @constant true, if a .NET tool with name @param toolName is installed globally,
 * @constant false if not, an error result if there was an error.
 */
async function isDotnetToolAvailable(toolName: string): Promise<Result<boolean>> {
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
async function installDotnetTool(toolName: string, version: string): Promise<Result<void>> {
    const dotnet = getDotnetCommand();
    return (await safeExecuteCommand(dotnet, 'tool', 'install', toolName, '--version', version, '--global'))
        .map(_ => {});
}

/**
 * Checks, if the dotnet CLI tool is available.
 * @returns Ok, if the dotnet CLI is available, an error result otherwise.
 */
async function isDotnetCommandAvailable(): Promise<Result<void>> {
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
