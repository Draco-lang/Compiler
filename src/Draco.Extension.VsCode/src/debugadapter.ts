import * as vscode from "vscode";
import * as fs from "fs/promises";
import * as path from "path";
import { DracoDebugAdapterCommandName } from "./settings";
import { PathLike } from "fs";
import { AssetGenerator } from "./assets";

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

        const generator = new AssetGenerator(folder.uri.fsPath);

        await generator.ensureVscodeFolderExists();
        const projectFiles = await generator.getDracoprojFilePaths();

        const tasksDescription = {
            version: '2.0.0',
            tasks: projectFiles.map(generator.getBuildTaskDescriptionForProject),
        };
        await fs.writeFile(generator.tasksJsonPath, JSON.stringify(tasksDescription, null, 4));

        return projectFiles.map(generator.getLaunchDescriptionForProject);
    }

    public async resolveDebugConfigurationWithSubstitutedVariables(folder: vscode.WorkspaceFolder | undefined, debugConfiguration: vscode.DebugConfiguration, token?: vscode.CancellationToken): Promise<vscode.DebugConfiguration> {
        return debugConfiguration;
    }
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
