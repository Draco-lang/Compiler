import * as vscode from "vscode";
import * as fs from "fs/promises";
import * as path from "path";
import { DracoDebugAdapterCommandName } from "./settings";

export function activateDebugAdapter(context: vscode.ExtensionContext) {
    context.subscriptions.push(vscode.debug.registerDebugConfigurationProvider(
        'dracodbg',
        new DracoDebugConfigurationProvider()));
    context.subscriptions.push(vscode.debug.registerDebugAdapterDescriptorFactory(
        'dracodbg',
        new DebugAdapterDotnetToolFactory()));
}

/**
 * Provides debug config based on the project.
 */
class DracoDebugConfigurationProvider implements vscode.DebugConfigurationProvider {
    public async provideDebugConfigurations(folder: vscode.WorkspaceFolder | undefined, token?: vscode.CancellationToken): Promise<vscode.DebugConfiguration[]> {
        if (!folder) {
            return [];
        }

        await addTasksJsonIfNecessary(folder.uri.fsPath);

        let filesInWorkspaceRoot = await fs.readdir(folder.uri.fsPath);
        let projectFiles = filesInWorkspaceRoot.filter(f => f.endsWith('.dracoproj'));
        return projectFiles.map(f => ({
            name: 'Draco: Launch Console App',
            type: 'dracodbg',
            request: 'launch',
            preLaunchTask: 'build',
            // TODO: Hardcoded config and framework
            program: path.join('${workspaceFolder}', 'bin', 'Debug', `${path.parse(f).name}.dll`),
            stopAtEntry: false
        }));
    }

    public async resolveDebugConfigurationWithSubstitutedVariables(folder: vscode.WorkspaceFolder | undefined, debugConfiguration: vscode.DebugConfiguration, token?: vscode.CancellationToken): Promise<vscode.DebugConfiguration> {
        return debugConfiguration;
    }
}

async function addTasksJsonIfNecessary(workspaceRoot: string): Promise<void> {

}

function generateTasksJson(projectPath: string): any {
    return {
        version: '2.0.0',
        tasks: [
            {
                label: 'build',
                command: 'dotnet',
                type: 'process',
                args: [
                    'build',
                    path.join('${workspaceFolder}', projectPath),
                ],
                problemMatcher: '$msCompile',
            }
        ]
    };
}

/**
 * Descriptor factory for using the debugger as a dotnet tool.
 */
class DebugAdapterDotnetToolFactory implements vscode.DebugAdapterDescriptorFactory {
    createDebugAdapterDescriptor(session: vscode.DebugSession, executable: vscode.DebugAdapterExecutable | undefined): vscode.ProviderResult<vscode.DebugAdapterDescriptor> {
        return new vscode.DebugAdapterExecutable(
            DracoDebugAdapterCommandName,
            ['run', '--stdio'],
            undefined);
    }
}
