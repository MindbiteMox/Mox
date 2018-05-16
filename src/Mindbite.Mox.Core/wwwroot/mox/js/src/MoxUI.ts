namespace Mox.UI {
    export class MobileMenu {

        private openButton: HTMLElement;
        private menu: HTMLElement;
        private shadow: HTMLElement;
        private windowWidth: number;

        constructor(openButton: HTMLElement, menu: HTMLElement, shadowClass: string, windowWidth: number) {
            this.openButton = openButton;
            this.menu = menu;
            this.windowWidth = windowWidth;

            {
                let selectedMenuSelector = '.mox-header-menu-container > .mox-menu > li > .selected'
                let selectedMenuItem = document.querySelector(selectedMenuSelector);
                let selectedMenuTitle = selectedMenuItem.textContent;
                let titleElement = document.createElement('h2');
                titleElement.className = 'mox-header-mobile-app-name';
                titleElement.innerText = selectedMenuTitle;
                Mox.Utils.DOM.closest(this.menu, '.mox-header').appendChild(titleElement);
            }

            let shadows = document.getElementsByClassName(shadowClass);
            if (shadows.length > 0) {
                this.shadow = shadows[0] as HTMLElement;
            } else {
                let newShadow = document.createElement('div');
                newShadow.className = shadowClass;
                this.menu.parentNode.insertBefore(newShadow, this.menu);
                this.shadow = newShadow;
            }

            this.openButton.addEventListener('click', event => {
                if (window.innerWidth < this.windowWidth) {
                    this.openClicked(event);
                    event.stopPropagation();
                }
            });

            this.shadow.addEventListener('click', event => {
                if (window.innerWidth < this.windowWidth) {
                    this.closeClicked(event);
                    event.preventDefault();
                    event.stopPropagation();
                }
            });
        }

        static initDefault(): MobileMenu {
            let openButton = document.getElementsByClassName('mox-header')[0] as HTMLElement;
            let menu = document.getElementsByClassName('mox-header-menu-container')[0] as HTMLElement;
            return new MobileMenu(openButton, menu, 'mox-menu-background', 960);
        }

        private openClicked(event: Event) {
            this.menu.classList.add('open');
            this.shadow.classList.add('visible');
        }

        private closeClicked(event: Event) {
            this.menu.classList.remove('open');
            this.shadow.classList.remove('visible');
        }
    }

    export interface ModalOptions {
        className?: string;
        contentClassName?: string;
        containerElement?: HTMLElement;
        noShadow?: boolean;
        dontCloseOnEscape?: boolean;
    }
    
    export class Modal {
        private escapeHandle: CloseOnEscapeHandle;

        private root: HTMLElement;
        private shadow: HTMLElement;
        private contentWrapper: HTMLElement;
        private closeButton: HTMLAnchorElement;
        private onCloseCallbacks: (() => void)[];

        contentContainer: HTMLElement;

        private static allOpenModals: Modal[];

        constructor(options?: ModalOptions) {
            let _options = options || {} as ModalOptions;

            this.contentContainer = document.createElement('div');
            this.contentContainer.className = _options.contentClassName || 'mox-modal-content';

            this.closeButton = document.createElement('a');
            this.closeButton.className = 'mox-modal-close';
            this.closeButton.href = '#!';
            this.closeButton.addEventListener('click', (e) => { e.preventDefault(); this.close(); });
            
            this.contentWrapper = document.createElement('div');
            this.contentWrapper.className = 'mox-modal-wrapper loading';
            this.contentWrapper.appendChild(this.contentContainer);
            this.contentWrapper.appendChild(this.closeButton);

            this.shadow = document.createElement('div');
            this.shadow.className = 'mox-modal-shadow' + (options.noShadow ? 'hidden' : '');
            this.shadow.appendChild(this.contentWrapper);

            this.root = document.createElement('div');
            this.root.className = _options.className || 'mox-modal';
            this.root.appendChild(this.shadow);

            (options.containerElement || document.body).appendChild(this.root);

            this.onCloseCallbacks = [];
            if (!options.dontCloseOnEscape) {
                this.escapeHandle = CloseOnEscapeQueue.enqueue(() => this.close());
            }
        }

        static async createDialog(url: string): Promise<Mox.UI.Modal> {
            let modal = new Modal({
                className: 'mox-modal mox-dialog',
                contentClassName: 'mox-modal-content mox-content'
            });

            let getInit: RequestInit = {
                credentials: 'same-origin',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                }
            };
            let response = await fetch(url, getInit).then(Mox.Utils.Fetch.checkErrorCode).then(Mox.Utils.Fetch.redirect(url => { window.location.href = url; }));
            let text = await response.text();
            modal.contentContainer.innerHTML = text;
            modal.contentWrapper.classList.remove('loading');

            document.body.style.overflow = 'hidden';

            if (!Modal.allOpenModals) Modal.allOpenModals = [];
            Modal.allOpenModals.push(modal);

            return modal;
        }

        static createDialogWithContent(htmlContent: string): Mox.UI.Modal {
            const modal = new Modal({
                className: 'mox-modal mox-dialog',
                contentClassName: 'mox-modal-content mox-content'
            });

            modal.contentContainer.innerHTML = htmlContent;
            setTimeout(() => {
                modal.contentWrapper.classList.remove('loading');
            }, 10);

            document.body.style.overflow = 'hidden';

            if (!Modal.allOpenModals) Modal.allOpenModals = [];
            Modal.allOpenModals.push(modal);

            return modal;
        }

        static async closeAll(): Promise<void> {
            if (Modal.allOpenModals) {
                return Promise.all(Modal.allOpenModals.map(x => x.close())) as any;
            }

            return Promise.resolve();
        }

        async close(): Promise<void> {
            Modal.allOpenModals = Modal.allOpenModals.filter(m => m != this);

            if (!Modal.allOpenModals.length) {
                document.body.style.overflow = 'unset';
            }

            CloseOnEscapeQueue.remove(this.escapeHandle);

            return new Promise<void>((resolve, reject) => {
                this.onCloseCallbacks.forEach(x => x());
                this.root.classList.add('hidden');
                setTimeout(() => {
                    this.root.remove();
                    resolve();
                }, 200);
            });
        }

        async replaceContent(url: string) {
            let getInit: RequestInit = {
                credentials: 'same-origin',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                }
            };
            this.contentWrapper.classList.add('loading');
            let response = await fetch(url, getInit).then(Mox.Utils.Fetch.checkErrorCode).then(Mox.Utils.Fetch.redirect(url => { window.location.href = url; }));
            let text = await response.text();
            this.contentContainer.innerHTML = text;
            this.contentWrapper.classList.remove('loading');
        }

        onClose(callback: () => void) {
            this.onCloseCallbacks.push(callback);
        }

        static setupHistory(modal: Modal, url: string): Modal {
            var state = { url };
            var closeClicked = false;
            var poped = false;
            var prevState = history.state;

            var pop = function (e) {
                if (!(e.state && prevState && prevState.url === e.state.url) && !(prevState === null && e.state === null)) {
                    e.preventDefault();
                    return;
                }

                e.preventDefault();
                window.removeEventListener('popstate', pop);

                poped = true;
                if (!closeClicked) {
                    modal.close();
                }
            }

            history.pushState(state, 'open modal', url);
            window.addEventListener('popstate', pop);

            modal.onClose(function () {
                if (!poped) {
                    closeClicked = true;
                    history.back();
                }
            });

            return modal;
        }
    }

    class ShortcutStackFrame {
        keyCode: number;
        control: boolean;
        shift: boolean;
    }

    export type CloseOnEscapeHandle = number;
    export class CloseOnEscapeQueue {

        private static isEventsSetup = false;
        private static lastHandle: CloseOnEscapeHandle = 0;
        private static handles: { handle: CloseOnEscapeHandle, callback: () => void | Promise<void> }[] = [];

        private static setupEvents() {
            let currentlyProcessing = false;

            window.addEventListener('keydown', e => {
                if (e.keyCode == 27 && !currentlyProcessing && CloseOnEscapeQueue.handles.length) {
                    currentlyProcessing = true;
                    let callback = CloseOnEscapeQueue.handles[CloseOnEscapeQueue.handles.length - 1].callback;
                    let promise = callback() || Promise.resolve();
                    promise.then(() => {
                        currentlyProcessing = false;
                    })
                }
            });

            CloseOnEscapeQueue.isEventsSetup = true;
        }

        static enqueue(callback: () => void|Promise<void>): CloseOnEscapeHandle {
            if (!CloseOnEscapeQueue.isEventsSetup) CloseOnEscapeQueue.setupEvents();

            let handle = ++CloseOnEscapeQueue.lastHandle;
            CloseOnEscapeQueue.handles.push({ handle, callback });
            return handle;
        }

        static remove(handle: CloseOnEscapeHandle): void {
            CloseOnEscapeQueue.handles = CloseOnEscapeQueue.handles.filter(x => x.handle != handle);
        }
    }

    export class GlobalInstances {
        static mobileMenu: MobileMenu;

        static initDefault() {
            GlobalInstances.mobileMenu = MobileMenu.initDefault();
        }
    }
}