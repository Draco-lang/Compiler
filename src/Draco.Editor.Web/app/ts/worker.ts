importScripts('./dotnet.js');
declare global { // Blazor does not provide types, so we have our own to please typescript.
    interface Window {
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

    dotnet.moduleConfig.configSrc = null;
    const { setModuleImports, getAssemblyExports, } = await dotnet
        .withConfig(
            monoCfg
        ).withModuleConfig({
            print: (txt: string) => sendMessage('stdout', txt)
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
