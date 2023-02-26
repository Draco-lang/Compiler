import { deflateRaw, inflateRaw } from 'pako';
import { GoldenLayout, LayoutConfig } from 'golden-layout';
import { fromBase64, fromBase64ToBase64URL, fromBase64URLToBase64, toBase64 } from './helpers.js';
import { initDotnetWorkers, setCode, subscribeOutputChange } from './dotnet.js';
import { TextDisplay } from './LayoutComponents/TextDisplay.js';
import { StdOut } from './LayoutComponents/StdOut.js';
import { TextInput } from './LayoutComponents/TextInput.js';
import { loadThemes } from './loadThemes.js';

function updateHash(code: string) {
    // setting the URL Hash with the state of the editor.
    // Doing this before invoking DotNet will allow sharing hard crash.
    const encoded = new TextEncoder().encode(code);
    const compressed = deflateRaw(encoded);
    const buffer = new Uint8Array(compressed.length + 1);
    buffer[0] = 2; // version, for future use.
    buffer.set(compressed, 1);
    history.replaceState(undefined, undefined, '#' + fromBase64ToBase64URL(toBase64(buffer)));
}
// This file is run on page load.
// This run before blazor load, and will tell blazor to start.

self.MonacoEnvironment = {
    // Web Workers need to start a new script, by url.
    // This is the path where the script of the webworker is served.
    getWorkerUrl: function () {
        return './editor.worker.js';
    }
};

const hash = window.location.hash.slice(1);
export let inputCode = `func main() {
    println("Hello!");
}
`;

if (hash != null && hash.trim().length > 0) {
    // We store data in the hash of the url, so we need to decode it on load.
    try {
        const b64 = fromBase64URLToBase64(hash);// our hash is encoded in base64 url: https://en.wikipedia.org/wiki/Base64#URL_applications
        let buffer = fromBase64(b64);
        const version = buffer[0];
        buffer = buffer.subarray(1); // Version byte, for future usage.
        const uncompressed = inflateRaw(buffer);
        let str = new TextDecoder().decode(uncompressed);
        if(version == 1) {
            const firstNewLine = str.indexOf('\n');
            str.slice(0, firstNewLine);
            str = str.slice(firstNewLine + 1);
        }
        inputCode = str;
    } catch (e) {
        inputCode = `Error while decoding the URL hash. ${e}`;
    }
}

// We can now lazy load these functions.
// They are asynchronous and will complete in background.
loadThemes();
initDotnetWorkers(inputCode);

const layoutElement = document.querySelector('#layoutContainer') as HTMLElement;

const config : LayoutConfig = {
    root: {
        type: 'row',
        content: [
            {
                title: 'Input',
                type: 'component',
                componentType: 'TextInput',
                width: 50,
                isClosable: false
            },
            {
                type: 'stack',
                content: [
                    {
                        title: 'IR',
                        type: 'component',
                        componentType: 'TextDisplay',
                        isClosable: false
                    },
                    {
                        title: 'IL',
                        type: 'component',
                        componentType: 'TextDisplay',
                        isClosable: false
                    },
                    {
                        title: 'Console',
                        type: 'component',
                        componentType: 'StdOut',
                        isClosable: false
                    }

                ]
            }
        ]
    }
};


const goldenLayout = new GoldenLayout(layoutElement);
goldenLayout.registerComponentConstructor('TextInput', TextInput);
goldenLayout.registerComponentConstructor('StdOut', StdOut);
goldenLayout.registerComponentConstructor('TextDisplay', TextDisplay);

goldenLayout.loadLayout(config);
const inputEditor = TextInput.editors[0];
inputEditor.getModel().onDidChangeContent(() => {
    const code = inputEditor.getModel().getValue();
    setCode(code);
    updateHash(code);
});
subscribeOutputChange((arg) => {
    console.log(arg);
    if(arg.outputType == 'stdout') {
        if(arg.clear) {
            StdOut.terminals[0].reset();
        }
        StdOut.terminals[0].write(arg.value);
    }
});
