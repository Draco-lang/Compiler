import * as vscode from "vscode";
import { DracoDebugAdapterCommandName } from "./settings";

export function activateDebugAdapter(context: vscode.ExtensionContext) {
    context.subscriptions.push(vscode.debug.registerDebugConfigurationProvider(
        'draco',
        new DracoDebugConfigurationProvider()));
    context.subscriptions.push(vscode.debug.registerDebugAdapterDescriptorFactory(
        'draco',
        new DebugAdapterDotnetToolFactory()));
}

/**
 * Provides debug config based on the project.
 */
class DracoDebugConfigurationProvider implements vscode.DebugConfigurationProvider {
    public async provideDebugConfigurations(folder: vscode.WorkspaceFolder | undefined, token?: vscode.CancellationToken): Promise<vscode.DebugConfiguration[]> {
        return [{
            name: 'Draco: Launch Console App',
            type: 'draco',
            request: 'launch',
            preLaunchTask: 'build',
            program: 'TODO',
            stopAtEntry: false
        }];
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
