/**
 * Handling debug adapter integration.
 */

import * as vscode from "vscode";
import { AssetGenerator } from "./assets";
import { DebugAdapterCommandName } from "./tools";
import { interactivelyInitializeDebugAdapter } from "./user_flow";

/**
 * Registers the debug adapter handling functionality.
 * @param context The extension context.
 */
export function registerDebugAdapter(context: vscode.ExtensionContext) {
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
        const projectFiles = await generator.getDracoprojFilePaths();
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
    async createDebugAdapterDescriptor(session: vscode.DebugSession, executable: vscode.DebugAdapterExecutable | undefined): Promise<vscode.ProviderResult<vscode.DebugAdapterDescriptor>> {
        // Check, if we can even run the debug adapter
        if (!await interactivelyInitializeDebugAdapter()) {
            return null;
        }

        return new vscode.DebugAdapterExecutable(
            DebugAdapterCommandName,
            ['run', '--stdio'],
            undefined);
    }
}
