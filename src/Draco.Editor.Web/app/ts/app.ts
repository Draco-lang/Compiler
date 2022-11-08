import * as monaco from 'monaco-editor';
// @ts-ignore
import editorWorker from 'monaco-editor/esm/vs/editor/editor.worker?worker';
// @ts-ignore
import jsonWorker from 'monaco-editor/esm/vs/language/json/json.worker?worker';
// @ts-ignore
import cssWorker from 'monaco-editor/esm/vs/language/css/css.worker?worker';
// @ts-ignore
import htmlWorker from 'monaco-editor/esm/vs/language/html/html.worker?worker';
// @ts-ignore
import tsWorker from 'monaco-editor/esm/vs/language/typescript/ts.worker?worker';
import { loadWASM } from 'onigasm'; // peer dependency of 'monaco-textmate'
import { Registry } from 'monaco-textmate'; // peer dependency
import { wireTmGrammars } from 'monaco-editor-textmate';
// @ts-ignore
import onigasm from 'onigasm/lib/onigasm.wasm?url';
// @ts-ignore
import vsthemeRaw from '../data/vscode.converted.theme.json?raw';
import JSON5 from 'json5';
import tslib from 'tslib';

declare global {
    class DotNet {
        static invokeMethodAsync<T>(assemblyName: string, methodIdentifier: string, ...args: any[]): Promise<T>
    }
}

// hack for vite bundler...
self.MonacoEnvironment = {
    getWorker(_, label) {
        if (label === 'json') {
            return new jsonWorker()
        }
        if (label === 'css' || label === 'scss' || label === 'less') {
            return new cssWorker()
        }
        if (label === 'html' || label === 'handlebars' || label === 'razor') {
            return new htmlWorker()
        }
        if (label === 'typescript' || label === 'javascript') {
            return new tsWorker()
        }
        return new editorWorker()
    }
}

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

    await loadWASM(onigasm);

    const registry = new Registry({
        getGrammarDefinition: async (scopeName) => {
            return {
                format: 'json',
                content: await (await fetch(`syntaxes/draco.tmLanguage.json`)).text()
            }
        }
    });
    // map of monaco "language id's" to TextMate scopeNames
    const grammars = new Map();
    grammars.set('draco', 'source.draco');
    const newTheme = JSON5.parse(vsthemeRaw);
    monaco.editor.defineTheme("vs-dark", newTheme);
    monaco.languages.register({id: "draco"});
    await wireTmGrammars(monaco, registry, grammars, dracoEditor);
}
main();
