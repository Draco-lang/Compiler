/**
 * Settings handling.
 */

import { ConfigurationTarget, workspace } from "vscode";

/**
 * Writes a useful set of default settings.
 * @param target The @see ConfigurationTarget to write the defaults to.
 * @returns The promise that finishes, when all settings are updated.
 */
export function updateWithDefaultSettings(target: ConfigurationTarget): Promise<void[]> {
    const settingsToInclude = [
        'dotnetCommand',
        'sdkVersion',
    ];
    const config = workspace.getConfiguration('draco');
    return Promise.all(settingsToInclude.map(setting =>
        config.update(setting, config.get(setting), target)));
}
