function stripXMLHeader(xml) {
    const indexOf = xml.indexOf('\n');
    return xml.slice(indexOf);
}

function stripViewBox(xml) {
    const indexOfViewBox = xml.indexOf('viewBox');
    const nextNewline = xml.substring(indexOfViewBox).indexOf('\n') + indexOfViewBox;
    return xml.slice(0, indexOfViewBox) + xml.slice(nextNewline);
}

export async function createThemeBasedLogo() {
    const bodyLight = await (await fetch('https://raw.githubusercontent.com/Draco-lang/Language-suggestions/main/Resources/Logo-Short.svg')).text();
    let logoLight = stripXMLHeader(bodyLight);
    logoLight = stripViewBox(logoLight);
    // logoLight = optimize(logoLight).data;

    const bodyDark = await (await fetch('https://raw.githubusercontent.com/Draco-lang/Language-suggestions/main/Resources/Logo-Short-Inverted.svg')).text();
    let logoDark = stripXMLHeader(bodyDark);
    logoDark = stripViewBox(logoDark);
    // logoDark = optimize(logoDark).data;
    const logoSvg =
        `<?xml version="1.0" encoding="UTF-8" standalone="no"?>
        <svg viewBox="0 0 128 128" xmlns="http://www.w3.org/2000/svg">
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
            <g class="dark">
${logoDark}
            </g>
            <g class="light">
${logoLight}
            </g>
        </svg>
`;
    return logoSvg;
}
