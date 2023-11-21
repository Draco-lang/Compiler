importScripts('./dotnet.js');
declare global { // Blazor does not provide types, so we have our own to please typescript.
    interface Window {
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        dotnet: any;
    }
}

let firstMessageResolve;
let firstMessagePromise = new Promise(
    resolve => firstMessageResolve = resolve
);

let initResolve;
let initPromise = new Promise(
    resolve => initResolve = resolve
);

let csOnMessage;
const messages = [];
onmessage = async (e: MessageEvent<unknown>) => {
    // allow to await the first message in the init part.
    if (firstMessageResolve != undefined) {
        firstMessageResolve(e.data);
        firstMessageResolve = undefined;
        return;
    }
    // Await that the init code completed.
    if (initPromise != undefined) {
        await initPromise;
        initPromise = undefined;
        initResolve = undefined;
    }
    const isWorkerLoop = messages.length == 0; // we start an async message loop if there is no messages stored.
    messages.push(e.data);
    if (!isWorkerLoop) {
        return;
    }
    // Messages loop.
    while (messages.length > 0) {
        const currMessage = messages[0];
        try {
            csOnMessage(currMessage['type'], JSON.stringify(currMessage['payload']));
        } catch (err) {
            console.log(err);
            throw err;
        }
        messages.shift();
    }
};

function sendMessage(type: string, message: string) {
    postMessage({
        type: type,
        message: message
    });
}

async function main() {
    console.log('Worker starting...');
    const monoCfg = await firstMessagePromise;
    console.log(monoCfg);
    console.log('Received boot config.');
    monoCfg['assets'].forEach(s => {
        if (s['buffer'] != undefined) {
            s['buffer'] = Uint8Array.from(atob(s['buffer']), c => c.charCodeAt(0));
        }
    });
    firstMessagePromise = undefined;
    const dotnet = self.dotnet.dotnet;

    const { setModuleImports, getAssemblyExports, } = await dotnet
        .withConfig(
            monoCfg
        ).withModuleConfig({
            print: (txt: string) => sendMessage('stdout', txt)
        })
        .withResourceLoader((type: WebAssemblyBootResourceType, name: string, defaultUri: string, integrity: string, behavior: AssetBehaviors) => {
            // inject "_framework" behind the name. in defaultUri.
            return defaultUri.replace(name, '_framework/' + name);
        })
        .create();

    if (monoCfg['disableInterop'] !== true) {
        console.log('Enabling interop');
        setModuleImports(
            'worker.js',
            {
                Interop: {
                    sendMessage
                }
            }
        );
        const exports = await getAssemblyExports(monoCfg['mainAssemblyName']);
        csOnMessage = exports.Draco.Editor.Web.Interop.OnMessage;
    }

    initResolve();
    await dotnet.run();
}
main();

//https://github.com/dotnet/runtime/blob/784537a9cdc97bc5f45f6606f3f9837a7f755236/src/mono/wasm/runtime/types/index.ts#L183
type WebAssemblyBootResourceType = 'assembly' | 'pdb' | 'dotnetjs' | 'dotnetwasm' | 'globalization' | 'manifest' | 'configuration';
type SingleAssetBehaviors =
    /**
     * The binary of the dotnet runtime.
     */
    | 'dotnetwasm'
    /**
     * The javascript module for loader.
     */
    | 'js-module-dotnet'
    /**
     * The javascript module for threads.
     */
    | 'js-module-threads'
    /**
     * The javascript module for runtime.
     */
    | 'js-module-runtime'
    /**
     * The javascript module for emscripten.
     */
    | 'js-module-native'
    /**
     * Typically blazor.boot.json
     */
    | 'manifest';

type AssetBehaviors = SingleAssetBehaviors |
    /**
     * Load asset as a managed resource assembly.
     */
    'resource'
    /**
     * Load asset as a managed assembly.
     */
    | 'assembly'
    /**
     * Load asset as a managed debugging information.
     */
    | 'pdb'
    /**
     * Store asset into the native heap.
     */
    | 'heap'
    /**
     * Load asset as an ICU data archive.
     */
    | 'icu'
    /**
     * Load asset into the virtual filesystem (for fopen, File.Open, etc).
     */
    | 'vfs'
    /**
     * The javascript module that came from nuget package .
     */
    | 'js-module-library-initializer'
    /**
     * The javascript module for threads.
     */
    | 'symbols'

