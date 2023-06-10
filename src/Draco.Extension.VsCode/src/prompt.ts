/**
 * Prompt utilities.
 */

import { MessageItem, window } from "vscode";

/**
 * Different kinds of prompts.
 */
export enum PromptKind {
    /**
     * Informational message.
     */
    info,

    /**
     * Error message.
     */
    error,
}

/**
 * Represents user choices in a prompt.
 */
export enum PromptResult {
    /**
     * Positive answer.
     */
    yes,

    /**
     * Negative answer.
     */
    no,

    /**
     * Close without choosing.
     */
    cancel,

    /**
     * Negative answer, don't ask again.
     */
    disable,
}

/**
 * A single item in a prompt.
 */
export interface PromptItem extends MessageItem {
    /**
     * The result that is returned, when the item is chosen.
     */
    result: PromptResult;
}

/**
 * Prompts the user.
 * @param kind The @see PromptKind to display.
 * @param message The message to print in the prompt.
 * @param items The @see PromptItems to choose from.
 * @returns The promise with the chosen @see PromptResult.
 * If the user closes the prompt without choosing an item, @see PromptResult.cancel is returned.
 */
export async function prompt(kind: PromptKind, message: string, ...items: PromptItem[]): Promise<PromptResult> {
    // Get the correct prompt function
    const promptFunc = kind == PromptKind.info
        ? window.showInformationMessage
        : window.showErrorMessage;
    // Show the prompt
    const result = await promptFunc(message, ...items);
    // Map value
    return result?.result ?? PromptResult.cancel;
}
