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
            await csOnMessage(currMessage['type'], JSON.stringify(currMessage['payload']));
        } catch (err) {
            console.log(err);
            throw err;
        }
        messages.shift();
    }
};

function sendMessage(type: string, message: string) {
    console.log(type);
    console.log(message);
    postMessage({
        type: type,
        message: message
    });
}

async function main() {
    console.log('Worker starting...');
    const monoCfg = await firstMessagePromise;
    console.log('Received boot config.');
    console.log(monoCfg);
    firstMessagePromise = undefined;
    const dotnet = self.dotnet.dotnet;
    dotnet.moduleConfig.configSrc = null;
    const { setModuleImports, getAssemblyExports, } = await dotnet
        .withDiagnosticTracing(true)
        .withConfig(
            monoCfg
        )
        .create();
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
    initResolve();
    await dotnet.run();
}
main();
