import * as monaco from 'monaco-editor';
import editorWorker from 'monaco-editor/esm/vs/editor/editor.worker?worker';
import jsonWorker from 'monaco-editor/esm/vs/language/json/json.worker?worker';
import cssWorker from 'monaco-editor/esm/vs/language/css/css.worker?worker';
import htmlWorker from 'monaco-editor/esm/vs/language/html/html.worker?worker';
import tsWorker from 'monaco-editor/esm/vs/language/typescript/ts.worker?worker';

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

const namespace = "Draco.Editor.Web";

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

const runtype = document.getElementById("runtype") as HTMLSelectElement;
runtype.onchange = () => {
    DotNet.invokeMethodAsync<void>(namespace, "OnOutputTypeChange", runtype.value);
}

export function setRunType(newRuntype: string) {
    runtype.value = newRuntype;
}

var dracoEditor = monaco.editor.create(document.getElementById('draco-editor'), {
    value: ['func main() {', '\tprintln("Hello!");', '}'].join('\n'),
    language: 'rust',
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


