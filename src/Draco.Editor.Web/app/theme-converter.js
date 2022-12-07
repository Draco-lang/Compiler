import * as fs from 'fs-extra';
import * as path from 'path';

// copied from https://github.com/Nishkalkashyap/monaco-vscode-textmate-theme-converter/tree/master

export function convertTheme(theme) {
    const monacoThemeRule = [];
    const returnTheme = {
        inherit: theme.include != undefined,
        base: 'vs-dark',
        colors: theme.colors ?? {},
        rules: monacoThemeRule,
        encodedTokensColors: [],
    };
    if (theme.tokenColors != undefined) {

        theme.tokenColors.map((color) => {

            if (typeof color.scope == 'string') {

                const split = color.scope.split(',');

                if (split.length > 1) {
                    color.scope = split;
                    evalAsArray();
                    return;
                }

                monacoThemeRule.push(Object.assign({}, color.settings, {
                    // token: color.scope.replace(/\s/g, '')
                    token: color.scope
                }));
                return;
            }

            evalAsArray();

            function evalAsArray() {
                if (color.scope) {
                    (color.scope).map((scope) => {
                        monacoThemeRule.push(Object.assign({}, color.settings, {
                            token: scope
                        }));
                    });
                }
            }
        });
    }
    return returnTheme;
}
