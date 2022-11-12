import { build } from 'esbuild'
import * as path from 'path';
import * as fs from 'fs';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);


let wasmPlugin = {
    name: 'wasm',
    setup(build) {
        build.onLoad({ filter: /.wasm$/ }, async (args) => ({
            contents: await fs.promises.readFile(args.path),
            loader: 'binary'
        }))
    },
}

const workerEntryPoints = [
    'vs/editor/editor.worker.js'
];

const distDir = process.argv[2];
const outDir = path.join(__dirname, distDir);
build({
    entryPoints: workerEntryPoints.map((entry) => `node_modules/monaco-editor/esm/${entry}`),
    bundle: true,
    format: 'iife',
    outdir: outDir
});

build({
    entryPoints: ['ts/app.ts', 'css/app.css'],
    bundle: true,
    format: 'esm',
    outdir: outDir,
    loader: {
        '.ttf': 'file'
    },
    inject: ['ts/process.ts'],
    plugins: [wasmPlugin]
})

if (!fs.existsSync(outDir)) fs.mkdirSync(outDir);
fs.copyFileSync('index.html', path.join(outDir, 'index.html'),);
