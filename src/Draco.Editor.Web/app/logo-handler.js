import {get} from 'https';

export function CreateThemeBasedLogo() {
    var logoLight = "";
    get("https://raw.githubusercontent.com/Draco-lang/Language-suggestions/main/Resources/Logo-Short.svg", (response) => logoLight = response);
    var lightLines = logoLight.split("\n");
    lightLines.splice(0, 1);
    logoLight = lightLines.join("\n");
    
    var logoDark = "";
    get("https://raw.githubusercontent.com/Draco-lang/Language-suggestions/main/Resources/Logo-Short-Inverted.svg", (response) => logoDark = response);
    var darkLines = logoDark.split("\n");
    darkLines.splice(0, 1);
    logoDark = darkLines.join("\n");
    
    var finalLogo =
        `
        <?xml version="1.0" encoding="UTF-8" standalone="no"?>
        <style>
            @media (prefers-color-scheme: dark) {
                .light{
                    visibility: hidden;
                }
            }
    
            @media (prefers-color-scheme: light) {
                .dark{
                    visibility: hidden;
                }
            }
        </style>
        <svg>
            <g class = "dark">
        `;
    finalLogo += logoDark;
    finalLogo +=
        `
            </g>
            <g class = "light">
        `;
    finalLogo += logoLight;
    finalLogo +=
        `
            </g>
        </svg>
        `;
    // TODO: write to Icon.svg and reference it in index.html
}