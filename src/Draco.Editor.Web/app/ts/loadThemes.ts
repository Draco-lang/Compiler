import * as monaco from 'monaco-editor/esm/vs/editor/editor.main.js';
import onigasmWasm from 'onigasm/lib/onigasm.wasm';
import { loadWASM } from 'onigasm';
import { Registry } from 'monaco-textmate';
import { wireTmGrammars } from 'monaco-editor-textmate';
import grammarDefinition from '../../../Draco.SyntaxHighlighting/draco.tmLanguage.json';
import { isDarkMode } from './helpers.js';
export async function loadThemes() {
    const wasmPromise = loadWASM(onigasmWasm.buffer); // https://www.npmjs.com/package/onigasm;

    const choosenTheme = window.localStorage.getItem('theme'); // get previous user theme choice
    const themes: unknown = (await (await fetch('themes.json')).json());
    function setTheme(theme: string) {
        try {
            if (theme == 'Default' || theme == null) {
                window.localStorage.removeItem('theme');
            } else {
                window.localStorage.setItem('theme', theme);
            }
        } catch (e) {
            console.error(e);
        }
        let currentTheme = theme;
        if (themes[theme] == undefined) {
            currentTheme = isDarkMode() ? 'Dark+ (default dark)' : 'Light+ (default light)';
        }
        let selectedTheme = themes[currentTheme];
        if (selectedTheme == undefined) {
            selectedTheme = Object.values(themes)[0]; // defensive programming: dark_vs, and light_vs don't exists anymore.
        }
        monaco.editor.defineTheme('dynamic-theme', selectedTheme as monaco.editor.IStandaloneThemeData);
        monaco.editor.setTheme('dynamic-theme');
    }
    setTheme(choosenTheme);

    const themeSelector = document.getElementById('theme-selector') as HTMLSelectElement;
    const defaultOption = document.createElement('option');

    defaultOption.innerText = defaultOption.value = 'Default';
    themeSelector.appendChild(defaultOption);
    Object.keys(themes).forEach(s => {
        const option = document.createElement('option');
        option.innerText = option.value = s;
        themeSelector.appendChild(option);
    });
    themeSelector.value = choosenTheme ?? 'Default';
    themeSelector.onchange = () => {
        setTheme(themeSelector.value);
    };
    await wasmPromise;

    const registry = new Registry({
        getGrammarDefinition: async (scopeName) => {
            switch (scopeName) {
                case 'source.draco':
                    return {
                        format: 'json',
                        content: grammarDefinition
                    };
                case 'source.cs':
                    return {
                        format: 'json',
                        content: await (await fetch('csharp.tmLanguage.json')).text()
                    };
                case 'source.il':
                    return {
                        format: 'json',
                        content: await (await fetch('il.tmLanguage.json')).text()
                    };
                default:
                    return null;
            }

        }
    });

    // map of monaco "language id's" to TextMate scopeNames
    const grammars = new Map([
        ['draco', 'source.draco'],
        ['csharp', 'source.cs'],
        ['il', 'source.il']
    ]);
    for (const language of grammars.keys()) {
        monaco.languages.register({ id: language });
    }
    await wireTmGrammars(monaco, registry, grammars);
}
