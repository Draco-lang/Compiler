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

onmessage = async (e: MessageEvent<unknown>) => {
    if (firstMessageResolve != undefined) {
        firstMessageResolve(e.data);
        firstMessageResolve = undefined;
        return;
    }
    if (initPromise != undefined) {
        await initPromise;
        initPromise = undefined;
        initResolve = undefined;
    }
    try {
        csOnMessage(e.data['type'], JSON.stringify(e.data['payload']));
    } catch (e) {
        console.log(e);
        throw e;
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
