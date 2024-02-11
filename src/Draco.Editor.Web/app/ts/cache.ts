import { blobToBase64 } from './helpers.js';
import { buildDate } from './metadata.js';

const elements = [];

export function getDownloadViewElement() {
    const downloadViewElement = document.createElement('div');
    downloadViewElement.classList.add('monaco-editor');
    downloadViewElement.classList.add('download-hidden');
    const text = document.createElement('h3');
    text.className = 'tab-title';
    text.innerText = 'Downloading ressources:';
    downloadViewElement.appendChild(text);
    elements.push(downloadViewElement);
    return downloadViewElement;
}

export async function downloadAssemblies(cfg: unknown) {
    await ensureCacheUpToDate();
    const assets = cfg['assets'];
    if (assets != null) {
        const promises = assets.map(async (asset) => {
            if (asset['buffer'] == null) {
                await downloadAssembly('_framework', asset);
            }
        });
        await Promise.all(promises);
    }
    setDownloadViewVisible(false);
}

async function ensureCacheUpToDate() {
    try {
        const cache = await caches.open('assembly-cache');
        const result = await cache.match('appBuildDate');
        const cachedDate = await result?.text();
        console.log(`Current build: ${buildDate} cache build: ${cachedDate ?? 'none'}`);

        if (result == null || cachedDate != buildDate) {
            console.log('Cache nuked.');
            await caches.delete('assembly-cache');
            const newCache = await caches.open('assembly-cache');
            await newCache.put('appBuildDate', new Response(buildDate));
        }
    } catch (e) {
        console.log('Could not open cache: ', e);
    }
}

async function downloadAssembly(dlPath: string, asset: unknown): Promise<void> {
    let cache = null;
    try {
        cache = await caches.open('assembly-cache');
        const cached = await cache.match(asset['name']);
        if (cached) {
            const assemblyB64 = await cached.text();
            asset['buffer'] = assemblyB64;
            return;
        }
    } catch (e) {
        console.log('Could not open cache: ', e);
    }
    console.log(`Cache miss: ${dlPath}`);
    setDownloadViewVisible(true);
    const progresses = elements.map((element) => {
        const progressContainer = document.createElement('div');
        const progressText = document.createElement('span');
        progressText.classList.add('monaco-editor');
        progressText.innerText = asset['name'];
        const progress = document.createElement('progress');
        progressContainer.appendChild(progressText);
        progressContainer.appendChild(progress);
        progress.classList.add('downloadProgress');
        element.appendChild(progressContainer);
        return progress;
    });
    const assemblyBlob = await progressFetch(dlPath + '/' + asset['name'], (loaded, total) => {
        progresses.forEach(progress => {
            progress.max = total;
            progress.value = loaded;
        });
    });
    const assemblyB64 = await (blobToBase64(assemblyBlob));
    asset['buffer'] = assemblyB64;
    const response = new Response(assemblyB64);
    if (cache != null) {
        await cache.put(asset['name'], response);
    }
    progresses.forEach(progress => {
        progress.parentElement.remove();
    });
}

function wait(milliseconds) {
    return new Promise(resolve => setTimeout(resolve, milliseconds));
}

async function progressFetch(url: string, onProgress: (loaded: number, total: number) => void): Promise<Blob> {
    const response = await fetch(url);
    const contentLength = response.headers.get('content-length');
    const total = parseInt(contentLength, 10);
    let loaded = 0;

    const res = new Response(new ReadableStream({
        async start(controller) {
            const reader = response.body.getReader();
            while (true) {
                const { done, value } = await reader.read();
                if (done) break;
                loaded += value.byteLength;
                onProgress(loaded, total);
                controller.enqueue(value);
            }
            controller.close();
        },
    }));
    return await res.blob();
}

function setDownloadViewVisible(enable: boolean) {
    const outputs = Array.from(document.getElementsByClassName('output-viewer'));
    if (enable) {
        outputs.forEach(s => s.classList.add('download-hidden'));
        elements.forEach(s => s.classList.remove('download-hidden'));
    } else {
        outputs.forEach(s => s.classList.remove('download-hidden'));
        elements.forEach(s => s.classList.add('download-hidden'));
    }
}
