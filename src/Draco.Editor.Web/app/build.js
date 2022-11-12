import { build } from 'esbuild';
import * as path from 'path';
import * as fs from 'fs';
import { fileURLToPath } from 'url';
import { createThemeBasedLogo as getFavicon } from './favicon-downloader.js';

// This file manage the build process of the webapp.

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);


// this is a plugin to handle wasm file. We handle it like it was a binary file.
let wasmPlugin = {
    name: 'wasm',
    setup(build) {
        build.onLoad({ filter: /.wasm$/ }, async (args) => ({
            contents: await fs.promises.readFile(args.path),
            loader: 'binary'
        }));
    },
};

const workerEntryPoints = [
    'vs/editor/editor.worker.js'
];

const distDir = process.argv[2];
const outDir = path.join(__dirname, distDir);

// Bundle monaco editor workers.
build({
    entryPoints: workerEntryPoints.map((entry) => `node_modules/monaco-editor/esm/${entry}`),
    bundle: true,
    format: 'iife',
    outdir: outDir
});

// Bundle our app.
build({
    entryPoints: ['ts/app.ts', 'css/app.css'],
    bundle: true,
    format: 'esm', // We want ESM to use import in Blazor.
    outdir: outDir,
    loader: {
        '.ttf': 'file'
    },
    inject: ['ts/process.ts'],
    plugins: [wasmPlugin]
});

if (!fs.existsSync(outDir)) fs.mkdirSync(outDir);
fs.copyFileSync('index.html', path.join(outDir, 'index.html'),); // Copy index.html to wwwroot.

const favicon = await getFavicon();
fs.writeFileSync(path.join(outDir, 'favicon.svg'), favicon); // Write favicon to wwwroot.
