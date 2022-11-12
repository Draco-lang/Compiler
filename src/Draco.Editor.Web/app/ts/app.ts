import * as monaco from 'monaco-editor/esm/vs/editor/editor.main.js'; // Monaco is VSCode core, but limited due to browser environement.
import onigasmWasm from 'onigasm/lib/onigasm.wasm'; // TextMates regex parser lib compiled in WASM.
import { loadWASM } from 'onigasm'; // Helper shipped with it to load it.
import { Registry } from 'monaco-textmate';
import { wireTmGrammars } from 'monaco-editor-textmate'; // Library that allow running Textmates grammar in monaco.
import vstheme from '../data/vscode.converted.theme.json';
import grammarDefinition from '../../../Draco.SyntaxHighlighting/draco.tmLanguage.json';
import base64url from 'base64url';
import { deflateRaw, inflateRaw } from 'pako';

// This file is run on page load.
// This run before blazor load, and will tell blazor to start.

declare global { // Blazor do not provide types, so we have our own to please typescript.
    class Blazor {
        static start(): Promise<void>;
    }
}

self.MonacoEnvironment = {
    // Web Workers need to start a new script, by url.
    // This is the path where the script of the webworker is served.
    getWorkerUrl: function () {
        return './editor.worker.js';
    }
};

function isDarkMode() {
    // From: https://stackoverflow.com/questions/56393880/how-do-i-detect-dark-mode-using-javascript
    return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
}

function getVSThemeToUse() {
    return isDarkMode() ? 'vs-dark' : 'vs';
}

// We export these group of method so the C# runtime can call them.

/**
 * Sets the text of the output monaco-editor.
 * @param text
 */
export function setOutputText(text: string) {
    outputEditor.getModel().setValue(text);
}

// this is the element that allow to select the output type.
const outputTypeSelector = document.getElementById('output-type-selector') as HTMLSelectElement;

const hash = window.location.hash;
let inputCode = `func main() {
    println("Hello!");
}
`;

if (hash != null && hash.trim().length > 0) {
    // We store data in the hash of the url, so we need to decode it on load.
    try {
        let buffer = base64url.default.toBuffer(hash); // our hash is encoded in base64 url: https://en.wikipedia.org/wiki/Base64#URL_applications
        buffer = buffer.subarray(1); // Version byte, for future usage.
        const uncompressed = inflateRaw(buffer);
        const str = new TextDecoder().decode(uncompressed);
        const firstNewLine = str.indexOf('\n');
        outputTypeSelector.value = str.slice(0, firstNewLine);
        inputCode = str.slice(firstNewLine + 1);
    } catch (e) {
        inputCode = `Error while decoding the URL hash. ${e}`;
    }
}

outputTypeSelector.onchange = () => {
    // We relay the output type change to C#.
    DotNet.invokeMethodAsync<void>('Draco.Editor.Web', 'OnOutputTypeChange', outputTypeSelector.value);
};

const dracoEditor = monaco.editor.create(document.getElementById('draco-editor'), {
    value: inputCode,
    language: 'draco',
    theme: getVSThemeToUse()
});

dracoEditor.onDidChangeModelContent(() => {
    const text = dracoEditor.getModel().createSnapshot().read();
    // setting the URL Hash with the state of the editor.
    // Doing this before invoking DotNet will allow sharing hard crash.
    const content = outputTypeSelector.value + '\n' + text;
    const encoded = new TextEncoder().encode(content);
    const buffer = new Uint8Array(encoded.length + 1);
    buffer[0] = 1; // version, for future use.
    buffer.set(buffer, 1);
    const compressed = deflateRaw(buffer);
    const b64 = base64url.default.encode(Buffer.from(compressed));
    window.location.hash = b64;
    DotNet.invokeMethodAsync<void>('Draco.Editor.Web', 'CodeChange', text);
});

const outputEditor = monaco.editor.create(document.getElementById('output-viewer'), {
    value: ['.NET Runtime loading...'].join('\n'),
    language: 'rust',
    theme: getVSThemeToUse()
});

async function main() {
    // We can't do async on top level statements.
    await Blazor.start();
    DotNet.invokeMethodAsync<void>('Draco.Editor.Web', 'OnInit', outputTypeSelector.value, dracoEditor.getModel().createSnapshot().read());
    // At this point, the Blazor code can run "concurrently".

    await loadWASM(onigasmWasm.buffer); // https://www.npmjs.com/package/onigasm

    const registry = new Registry({
        getGrammarDefinition: async () => {
            return {
                format: 'json',
                content: grammarDefinition
            };
        }
    });
    // map of monaco "language id's" to TextMate scopeNames
    const grammars = new Map();
    grammars.set('draco', 'source.draco');
    monaco.editor.defineTheme('vs-dark', vstheme as monaco.editor.IStandaloneThemeData);
    monaco.languages.register({ id: 'draco' });
    await wireTmGrammars(monaco, registry, grammars, dracoEditor);
}
main();
