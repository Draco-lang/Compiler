import { Disposable, OutputChannel, window } from "vscode";

let logChannel: OutputChannel;

/**
 * Constructs a log channel.
 * @returns The @see Disposable that needs to be disposed of at the end.
 */
export function createLogChannel(): Disposable {
    logChannel = window.createOutputChannel('Draco Extension Logs');
    return logChannel;
}

/**
 * Logs an error to the log channel.
 * @param message The message to log.
 * @param additionalData Any additional data to log with the message.
 */
export function logError(message: string, ...additionalData: any[]) {
    logChannel.appendLine(`[error]: ${message}`);
    additionalData.forEach(d => logChannel.appendLine(`  ${d}`));
    logChannel.show();
}
