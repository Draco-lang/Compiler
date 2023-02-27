import { ComponentContainer } from 'golden-layout';
import { Terminal } from 'xterm';
import { FitAddon } from 'xterm-addon-fit';
import { getDownloadViewElement } from '../cache.js';

export class StdOut {
    static terminals: Terminal[] = [];

    rootElement: HTMLElement;
    resizeWithContainerAutomatically = true;
    terminal: Terminal;

    constructor(public container: ComponentContainer) {
        this.rootElement = container.element;
        this.rootElement.appendChild(getDownloadViewElement());
        const div = document.createElement('div');
        this.rootElement.appendChild(div);
        div.classList.add('editor-container');
        div.classList.add('output-viewer');
        this.terminal = new Terminal({convertEol: true});
        const fitAddon = new FitAddon();
        this.terminal.loadAddon(fitAddon);
        this.terminal.open(div);
        StdOut.terminals.push(this.terminal);
        container.on('resize', () => {
            fitAddon.fit();
        });

        container.on('shown', ()=> {
            setTimeout(()=> { // without that, the terminal is not resized, WTF
                fitAddon.fit();
            }, 1);
        });
    }
}
