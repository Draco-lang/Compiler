import { defineConfig } from 'vite'
const prefix = `monaco-editor/esm/vs`;
/** @type {import('vite').UserConfig} */
export default defineConfig(({ mode }) => {
    console.log(process.env.INIT_CWD);
    const part = mode === 'development' ? 'Debug' : 'Release';
    const path = `../bin/${part}/net6.0/wwwroot`;
    return {
        build: {
            outDir: path,
            rollupOptions: {
                output: {
                    manualChunks: { // monaco fix, does it work ? I don't know.
                        jsonWorker: [`${prefix}/language/json/json.worker`],
                        cssWorker: [`${prefix}/language/css/css.worker`],
                        htmlWorker: [`${prefix}/language/html/html.worker`],
                        tsWorker: [`${prefix}/language/typescript/ts.worker`],
                        editorWorker: [`${prefix}/editor/editor.worker`],
                    },
                },
            }
        },
        publicDir: path
    }
}
);
