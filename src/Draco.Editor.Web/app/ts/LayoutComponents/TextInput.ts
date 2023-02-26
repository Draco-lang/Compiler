import * as monaco from 'monaco-editor/esm/vs/editor/editor.main.js';
import { ComponentContainer } from 'golden-layout';
import { inputCode } from '../app.js';

// function updateHash() {
//     const source = dracoEditor.getModel().createSnapshot().read();
//     // setting the URL Hash with the state of the editor.
//     // Doing this before invoking DotNet will allow sharing hard crash.
//     const content = outputTypeSelector.value + '\n' + source;
//     const encoded = new TextEncoder().encode(content);
//     const compressed = deflateRaw(encoded);
//     const buffer = new Uint8Array(compressed.length + 1);
//     buffer[0] = 1; // version, for future use.
//     buffer.set(compressed, 1);
//     history.replaceState(undefined, undefined, '#' + fromBase64ToBase64URL(toBase64(buffer)));
// }
export class TextInput {
    static editors = [];
    rootElement: HTMLElement;
    resizeWithContainerAutomatically = true;
    constructor(public container: ComponentContainer) {
        this.rootElement = container.element;
        const div = document.createElement('div');
        div.classList.add('editor-container');
        this.rootElement.appendChild(div);
        const editor = monaco.editor.create(div, {
            value: inputCode,
            language: 'draco',
            theme: 'dynamic-theme',
            scrollbar: {
                vertical: 'visible'
            },
            scrollBeyondLastLine: false,
            minimap: {
                enabled: false
            },
            renderLineHighlight: 'none',
            overviewRulerBorder: false,
            hideCursorInOverviewRuler: true,
            mouseWheelZoom: true
        });
        TextInput.editors.push(editor);
        container.on('resize', () => {
            editor.layout();
        });
    }
}
