import { downloadAssemblies } from './cache.js';

const compilerWorker = new Worker('worker.js'); // first thing: we start the worker so it loads in parallel.
let runtimeWorker: Worker | undefined;
let listeners: ((arg: { outputType: string; value: string; clear: boolean }) => void)[] = [];
let jsModuleNativeName: string | undefined;
let jsModuleRuntimeName: string | undefined;
compilerWorker.onmessage = async (ev) => {
    const msg = ev.data as {
        type: string;
        message: string;
    };
    switch (msg.type) {
    case 'setOutputText': {
        const parsed = JSON.parse(msg.message);
        onOutputChange(parsed['OutputType'], parsed['Text'], true);
        break;
    }
    case 'runtimeAssembly': {
        if (runtimeWorker != undefined) {
            runtimeWorker.terminate();
        }
        onOutputChange('stdout', 'Loading script\'s .NET Runtime...', true);
        runtimeWorker = new Worker('worker.js');
        const cfg = JSON.parse(msg.message);
        console.log('Starting worker with boot config:');
        cfg['disableInterop'] = true;
        const assets = cfg['assets'];
        assets.unshift({
            name: jsModuleNativeName,
            behavior: 'js-module-native',
        });
        assets.unshift({
            name: jsModuleRuntimeName,
            behavior: 'js-module-runtime',
        });
        await downloadAssemblies(cfg);
        runtimeWorker.postMessage(cfg);
        let shouldClean = true;
        runtimeWorker.onmessage = (e) => {
            const runtimeMsg = e.data as {
                type: string;
                message: string;
            };
            switch (runtimeMsg.type) {
            case 'stdout':
                onOutputChange('stdout', runtimeMsg.message + '\n', shouldClean);
                shouldClean = false;
                break;
            default:
                console.error('Runtime sent unknown message', runtimeMsg);
                break;
            }
        };
        break;
    }
    default:
        console.log('Runtime sent unknown message', msg);
        break;
    }
};

export function setCode(code: string) {
    compilerWorker.postMessage({
        type: 'CodeChange',
        payload: code
    });
}

function onOutputChange(outputType: string, value: string, clear: boolean) {
    listeners.forEach(s => s({
        outputType: outputType,
        value: value,
        clear: clear
    }));
}

export function subscribeOutputChange(listener: (arg: { outputType: string; value: string; clear: boolean }) => void) {
    listeners.push(listener);
}

export function unsubscribeOutputChange(listener: (arg: { outputType: string; value: string }) => void) {
    listeners = listeners.filter(s => s != listener);
}

export async function initDotnetWorkers(initCode: string) {
    const cfg = await (await fetch('_framework/blazor.boot.json')).json();
    console.log(cfg);
    const assets: unknown[] = Object.keys(cfg.resources.assembly).map(
        s => {
            return {
                'behavior': 'assembly',
                'name': s
            };
        }
    );
    assets.unshift({
        'behavior': 'dotnetwasm',
        'name': 'dotnet.native.wasm'
    });
    jsModuleNativeName = Object.keys(cfg['resources']['jsModuleNative'])[0];
    assets.unshift({
        name: jsModuleNativeName,
        behavior: 'js-module-native',
    });
    jsModuleRuntimeName = Object.keys(cfg['resources']['jsModuleRuntime'])[0];
    assets.unshift({
        name: jsModuleRuntimeName,
        behavior: 'js-module-runtime',
    });

    const bootCfg = {
        mainAssemblyName: cfg.mainAssemblyName,
        assets: assets,
    };
    await downloadAssemblies(bootCfg);
    console.log(bootCfg);
    compilerWorker.postMessage(bootCfg);
    compilerWorker.postMessage({
        type: 'CodeChange',
        payload: initCode
    });
}
