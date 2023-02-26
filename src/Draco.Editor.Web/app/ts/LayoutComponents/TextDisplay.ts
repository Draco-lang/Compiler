import * as monaco from 'monaco-editor/esm/vs/editor/editor.main.js';
import { ComponentContainer } from 'golden-layout';
import { getDownloadViewElement } from '../cache.js';
import { subscribeOutputChange } from '../dotnet.js';
export class TextDisplay {
    static editors = {};
    rootElement: HTMLElement;
    resizeWithContainerAutomatically = true;
    constructor(public container: ComponentContainer) {
        this.rootElement = container.element;
        this.rootElement.appendChild(getDownloadViewElement());
        const div = document.createElement('div');
        div.classList.add('editor-container');
        div.classList.add('output-viewer');
        this.rootElement.appendChild(div);
        const editor = monaco.editor.create(div, {
            theme: 'dynamic-theme',
            language: container.title.toLowerCase(),
            readOnly: true,
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
            mouseWheelZoom: true,
            occurrencesHighlight: false,
        });
        TextDisplay.editors[container.title] = editor;
        container.on('resize', () => {
            editor.layout();
        });
        subscribeOutputChange((arg) => {
            if(arg.outputType == container.title) {
                editor.setValue(arg.value);
            }
        });
    }
}
