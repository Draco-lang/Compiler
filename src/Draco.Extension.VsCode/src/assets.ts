import { PathLike } from "fs";
import * as fs from "fs/promises";
import { glob } from "glob";
import * as path from "path";

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
        let paths = await globAsync(`${this.workspaceRoot}/**/*.dracoproj`);
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
     * Retrieves the descriptor for the 'tasks.json' file.
     */
    public async getTasksDescription(): Promise<any> {
        let dracoprojPaths = await this.getDracoprojFilePaths();
        return {
            version: '2.0.0',
            tasks: dracoprojPaths.map(this.getBuildTaskDescriptionForProject),
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
