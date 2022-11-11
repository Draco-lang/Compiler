import * as monaco from 'monaco-editor/esm/vs/editor/editor.main.js'; // Monaco is VSCode core, but limited due to browser environement.
import onigasmWasm from 'onigasm/lib/onigasm.wasm'; // TextMates regex parser lib compiled in WASM.
import { loadWASM } from 'onigasm'; // Helper shipped with it to load it.
import { Registry } from 'monaco-textmate';
import { wireTmGrammars } from 'monaco-editor-textmate'; // Library that allow running Textmates grammar in monaco.
import vstheme from '../data/vscode.converted.theme.json';
import grammarDefinition from '../../../Draco.SyntaxHighlighting/draco.tmLanguage.json';

declare global {
    class DotNet {
        static invokeMethodAsync<T>(assemblyName: string, methodIdentifier: string, ...args: any[]): Promise<T>
    }
    class Blazor {
        static start(): Promise<void>;
    }
}

self.MonacoEnvironment = {
    getWorkerUrl: function (moduleId, label) {
        return './editor/editor.worker.js';
    }
};

function isDarkMode() {
    return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches
}

function getTheme() {
    return isDarkMode() ? "vs-dark" : "vs";
}

export function setHash(hash) {
    window.location.hash = hash;
}

export function setOutputText(text: string) {
    outputEditor.getModel().setValue(text);
}

export function setEditorText(text: string) {
    dracoEditor.getModel().setValue(text);
}

export function emitCodeChange() {
    const text = dracoEditor.getModel().createSnapshot().read();
    DotNet.invokeMethodAsync<void>(namespace, "CodeChange", text);
}

export function setRunType(newRuntype: string) {
    runtype.value = newRuntype;
}

const namespace = "Draco.Editor.Web";

const runtype = document.getElementById("runtype") as HTMLSelectElement;
runtype.onchange = () => {
    DotNet.invokeMethodAsync<void>(namespace, "OnOutputTypeChange", runtype.value);
}

//TODO: block blazor load until this function run.
var dracoEditor = monaco.editor.create(document.getElementById('draco-editor'), {
    value: ['func main() {', '\tprintln("Hello!");', '}'].join('\n'),
    language: 'draco',
    theme: getTheme()
});

dracoEditor.onDidChangeModelContent(() => {
    emitCodeChange();
});

var outputEditor = monaco.editor.create(document.getElementById('output-viewer'), {
    value: ['.NET Runtime loading...'].join('\n'),
    language: 'rust',
    theme: getTheme()
});

async function main() {
    await Blazor.start();
    await loadWASM(onigasmWasm.buffer);

    const registry = new Registry({
        getGrammarDefinition: async (scopeName) => {
            return {
                format: 'json',
                content: grammarDefinition
            }
        }
    });
    // map of monaco "language id's" to TextMate scopeNames
    const grammars = new Map();
    grammars.set('draco', 'source.draco');
    monaco.editor.defineTheme("vs-dark", vstheme as monaco.editor.IStandaloneThemeData);
    monaco.languages.register({ id: "draco" });
    await wireTmGrammars(monaco, registry, grammars, dracoEditor);
}
main();
