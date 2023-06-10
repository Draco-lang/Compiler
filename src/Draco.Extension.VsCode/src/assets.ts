/**
 * Asset handling.
 */

import * as fs from "fs/promises";
import * as path from "path";
import * as vscode from "vscode";
import { PathLike } from "fs";
import { glob } from "glob";

/**
 * Aids in asset generation.
 */
export class AssetGenerator {
    private readonly workspaceRoot: string;

    /**
     * Initializes an asset generator.
     * @param workspaceRoot The workspace root.
     */
    public constructor(workspaceRoot: string) {
        this.workspaceRoot = workspaceRoot;
    }

    /**
     * The '.vscode' path of the workspace.
     */
    public get vscodePath(): string {
        return path.join(this.workspaceRoot, '.vscode');
    }

    /**
     * The 'settings.json' file path of the workspace.
     */
    public get settingsJsonPath(): string {
        return path.join(this.vscodePath, 'settings.json');
    }

    /**
     * The 'tasks.json' file path of the workspace.
     */
    public get tasksJsonPath(): string {
        return path.join(this.vscodePath, 'tasks.json');
    }

    /**
     * The 'launch.json' file path of the workspace.
     */
    public get launchJsonPath(): string {
        return path.join(this.vscodePath, 'launch.json');
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
    public async ensureVscodeFolderExists(): Promise<void> {
        if (!await exists(this.vscodePath)) {
            await fs.mkdir(this.vscodePath);
        }
    }

    /**
     * Retrieves all file paths with the '.dracoproj' extension,
     * relative to the workspace root.
     */
    public async getDracoprojFilePaths(): Promise<string[]> {
        const pattern = path.join(this.workspaceRoot, '**', '*.dracoproj').replace(/\\/g, '/');
        const paths = await globAsync(pattern);
        return paths.map(p => path.relative(this.workspaceRoot, p));
    }

    /**
     * Constructs the build task for a given project.
     * @param project The projectfile path.
     * @returns The single task descriptor to be used within 'tasks.json'.
     */
    public getBuildTaskDescriptionForProject(project: string): any {
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
    public getLaunchDescriptionForProject(project: string): vscode.DebugConfiguration {
        const dllName = `${path.parse(project).name}.dll`;
        return {
            name: 'Draco: Launch Console App',
            type: 'dracodbg',
            request: 'launch',
            preLaunchTask: 'build',
            // TODO: Hardcoded config and framework
            program: path.join('${workspaceFolder}', 'bin', 'Debug', 'net7.0', dllName),
            stopAtEntry: false,
        };
    }
}

/**
 * Searches the filesystem using a glob pattern.
 * @param pattern The pattern to search.
 * @returns The matching file paths.
 */
async function globAsync(pattern: string): Promise<string[]> {
    return new Promise<string[]>((resolve, reject) => {
        glob(pattern, (err, files) => {
            if (err) {
                reject(err);
            } else {
                resolve(files);
            }
        });
    });
}

/**
 * Checks if the given path exists and is available for writing.
 * @param path The path to check.
 * @returns True, if the path exists and can be written, false otherwise.
 */
async function exists(path: PathLike): Promise<boolean> {
    try {
        await fs.access(path, fs.constants.W_OK);
        return true;
    } catch {
        return false;
    }
}
