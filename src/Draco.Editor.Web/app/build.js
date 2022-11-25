import { createActionAuth } from '@octokit/auth-action';
import { build } from 'esbuild';
import * as path from 'path';
import * as fs from 'fs';
import { fileURLToPath } from 'url';
import { createThemeBasedLogo as getFavicon } from './favicon-downloader.js';
import { Octokit } from '@octokit/rest';
import { convertTheme } from 'monaco-vscode-textmate-theme-converter';
import JSON5 from 'json5';
import YAML from 'yaml';

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

console.log('Downloading logo svgs...');
const favicon = await getFavicon();
fs.writeFileSync(path.join(outDir, 'favicon.svg'), favicon); // Write favicon to wwwroot.

console.log('Downloading vs themes...');

let octokit;
if(process.env.GITHUB_TOKEN != undefined && process.env.GITHUB_TOKEN.length > 0) {
    const auth = createActionAuth();
    const authentication = await auth();
    octokit = new Octokit({
        auth: authentication
    });
} else {
    octokit = new Octokit();
}

const response = await octokit.repos.getContent({
    owner: 'microsoft',
    repo: 'vscode',
    path: 'extensions/theme-defaults/themes'
});

const themes = await Promise.all(response.data.map(async s => {
    const resp = await fetch(s.download_url);
    const txt = await resp.text();
    const parsed = JSON5.parse(txt);
    const converted = convertTheme(parsed);
    return {
        name: parsed.name,
        filename: s.name,
        theme: converted
    };
}));
const themeObj = {};
const themePackageJson = await (await fetch('https://raw.githubusercontent.com/microsoft/vscode/main/extensions/theme-defaults/package.json')).json();
const themesMetadata = themePackageJson.contributes.themes;

themes.forEach(s => {
    themeObj[s.name] = s.theme;
    themeObj[s.name].base = themesMetadata.find(t => path.basename(t.path) == s.filename).uiTheme;
    themeObj[s.name].inherit = true;
    if (themeObj[s.name].colors === undefined) {
        themeObj[s.name].colors = {}; // workaround monaco bug: if not set, will throw "Cannot read properties of undefined (reading 'editor.foreground')"
    }
});

const themeListJson = JSON.stringify(themeObj);
fs.writeFileSync(path.join(outDir, 'themes.json'), themeListJson);
const csharpTextmateYml = await (await fetch('https://raw.githubusercontent.com/dotnet/csharp-tmLanguage/main/src/csharp.tmLanguage.yml')).text();
const csharpTextmate = JSON.stringify(YAML.parse(csharpTextmateYml));
fs.writeFileSync(path.join(outDir, 'csharp.tmLanguage.json'), csharpTextmate);
const ilTextmate = await (await fetch('https://raw.githubusercontent.com/soltys/vscode-il/master/syntaxes/il.json')).text();
fs.writeFileSync(path.join(outDir, 'il.tmLanguage.json'), ilTextmate);
