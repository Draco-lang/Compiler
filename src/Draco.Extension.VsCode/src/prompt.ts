import { MessageItem, window } from "vscode";

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
 * @param message The message to print in the prompt.
 * @param items The @see PromptItems to choose from.
 * @returns The promise with the chosen @see PromptResult.
 * If the user closes the prompt without choosing an item, @see PromptResult.cancel is returned.
 */
export async function prompt(message: string, ...items: PromptItem[]): Promise<PromptResult> {
    // TODO: Not necessarily an error
    // Show the prompt
    const result = await window.showErrorMessage(message, ...items);
    // Map value
    return result?.result ?? PromptResult.cancel;
}
