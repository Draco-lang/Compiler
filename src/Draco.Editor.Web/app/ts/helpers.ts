// This file where every function you'll see will make you think: "wtf that's not in the standard lib ???"

export function fromBase64ToBase64URL(str: string) {
    return str
        .replace('+', '-')
        .replace('/', '_');
}

export function fromBase64URLToBase64(str: string) {
    return str
        .replace('_', '/')
        .replace('-', '+');
}

export function toBase64(u8) {
    return btoa(String.fromCharCode.apply(null, u8));
}

export function fromBase64(str) {
    return new Uint8Array(atob(str).split('').map(c => c.charCodeAt(0)));
}

export function blobToBase64(blob: Blob) : Promise<string> {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.readAsDataURL(blob);
        reader.onload = () => resolve((reader.result as string).split(',')[1]);
        reader.onerror = error => reject(error);
    });
}

export function isDarkMode() {
    // From: https://stackoverflow.com/questions/56393880/how-do-i-detect-dark-mode-using-javascript
    return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
}

