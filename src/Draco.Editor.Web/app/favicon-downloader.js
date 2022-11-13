function stripXMLHeader(xml) {
    const indexOf = xml.indexOf('\n');
    return xml.slice(indexOf);
}

export async function createThemeBasedLogo() {
    const bodyLight = await (await fetch('https://raw.githubusercontent.com/Draco-lang/Language-suggestions/main/Resources/Logo-Short.svg')).text();
    let logoLight = stripXMLHeader(bodyLight);
    // logoLight = optimize(logoLight).data;

    await (await fetch('https://raw.githubusercontent.com/Draco-lang/Language-suggestions/main/Resources/Logo-Short-Inverted.svg')).text();
    let logoDark = stripXMLHeader(bodyLight);
    // logoDark = optimize(logoDark).data;
    const logoSvg =
        `<?xml version="1.0" encoding="UTF-8" standalone="no"?>
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
${logoDark}
            </g>
            <g class = "light">
${logoLight}
            </g>
        </svg>
`;
    return logoSvg;
}
