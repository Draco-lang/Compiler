import { ComponentContainer } from 'golden-layout';

export class Settings {
    static instance: Settings;

    rootElement: HTMLElement;
    resizeWithContainerAutomatically = true;

    constructor(public container: ComponentContainer) {
        if (Settings.instance != undefined) throw new Error('Settings panel should be instantied only once.');
        Settings.instance = this;
        this.rootElement = container.element;
        this.rootElement.innerHTML = `
<div class="settings monaco-editor">
    <div>
        <span class="monaco-editor">
            Editor Theme:
            <select id="theme-selector" class="monaco-editor-background monaco-editor">
            </select>
        </span>
    </div>
</div>
        `;
    }
}
