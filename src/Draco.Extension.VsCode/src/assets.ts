/**
 * Asset handling.
 */

import * as vscode from "vscode";
import { RelativePattern, Uri, workspace } from "vscode";

/**
 * Aids in asset generation.
 */
export class AssetGenerator {
    private readonly workspaceRoot: Uri;

    /**
     * Initializes an asset generator.
     * @param workspaceRoot The workspace root.
     */
    public constructor(workspaceRoot: Uri) {
        this.workspaceRoot = workspaceRoot;
    }

    /**
     * The '.vscode' path of the workspace.
     */
    public get vscodePath(): Uri {
        return Uri.joinPath(this.workspaceRoot, '.vscode');
    }

    /**
     * The 'settings.json' file path of the workspace.
     */
    public get settingsJsonPath(): Uri {
        return Uri.joinPath(this.vscodePath, 'settings.json');
    }

    /**
     * The 'tasks.json' file path of the workspace.
     */
    public get tasksJsonPath(): Uri {
        return Uri.joinPath(this.vscodePath, 'tasks.json');
    }

    /**
     * The 'launch.json' file path of the workspace.
     */
    public get launchJsonPath(): Uri {
        return Uri.joinPath(this.vscodePath, 'launch.json');
    }

    /**
     * Checks, if the '.vscode' folder exists.
     * @returns @constant true, if it exists, @constant false otherwise.
     */
    public vscodeFolderExists(): Promise<boolean> {
        return exists(this.vscodePath);
    }

    /**
     * Ensures that the '.vscode' folder exists.
     */
    public async ensureVscodeFolderExists() {
        if (!await exists(this.vscodePath)) {
            await vscode.workspace.fs.createDirectory(this.vscodePath);
        }
    }

    /**
     * Retrieves all file paths with the '.dracoproj' extension,
     * relative to the workspace root.
     */
    public async getDracoprojFilePaths(): Promise<Uri[]> {
        return await workspace.findFiles(new RelativePattern(this.workspaceRoot, '**/*.dracoproj'));
    }

    /**
     * Constructs the build task for a given project.
     * @param project The projectfile path.
     * @returns The single task descriptor to be used within 'tasks.json'.
     */
    public getBuildTaskDescriptionForProject(project: Uri): any {
        return {
            label: 'build',
            command: 'dotnet',
            type: 'process',
            args: [
                'build',
                project,
            ],
            problemMatcher: '$msCompile',
        };
    }

    /**
     * Constructs the launch config for a given project.
     * @param project The projectfile path.
     * @returns The single launch configuration to be used within 'launch.json'.
     */
    public getLaunchDescriptionForProject(project: Uri): vscode.DebugConfiguration {
        const filename = project.path.split('/').at(-1)!;
        // care of multiple dot in filename
        const filenameWithoutExtension = filename.split('.').slice(0, -1).join('.');
        const dllName = `${filenameWithoutExtension}.dll`;
        return {
            name: 'Draco: Launch Console App',
            type: 'dracodbg',
            request: 'launch',
            preLaunchTask: 'build',
            // TODO: Hardcoded config and framework
            program: `'\${workspaceFolder}/bin/Debug/net7.0/${dllName})`,
            stopAtEntry: false,
        };
    }
}

/**
 * Checks if the given path exists and is available for writing.
 * @param path The path to check.
 * @returns True, if the path exists and can be written, false otherwise.
 */
async function exists(path: Uri): Promise<boolean> {
    try {
        await workspace.fs.stat(path);
        return true;
    } catch {
        return false;
    }
}
