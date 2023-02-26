import { ComponentContainer } from 'golden-layout';
import { getDownloadViewElement } from '../cache.js';

export class StdOut {
    rootElement: HTMLElement;
    resizeWithContainerAutomatically = true;
    constructor(public container: ComponentContainer) {
        this.rootElement = container.element;
        this.rootElement.appendChild(getDownloadViewElement());
    }
}
