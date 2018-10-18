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
    }

    export interface FormDialogOptions {
        onSubmit: (modal: Modal, form: HTMLFormElement, event: Event) => void;
        onSubmitFormData: (modal: Modal, form: HTMLFormElement, responseData: Mox.Utils.Fetch.FormPostResponse) => void;
        onButtonClicked: (modal: Modal, button: Element, event: Event) => void;
        buttonSelector: string;
        dontEvaluateScripts: boolean;
        actualWindowHref: string;
    }
    
    export class Modal {
        private escapeHandle: CloseOnEscapeHandle;

        private root: HTMLElement;
        private shadow: HTMLElement;
        private contentWrapper: HTMLElement;
        private closeButton: HTMLAnchorElement;
        private onCloseCallbacks: (() => void)[];
        private onContentReplacedCallbacks: (() => void)[];

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
            this.shadow.className = 'mox-modal-shadow';
            this.shadow.appendChild(this.contentWrapper);

            this.root = document.createElement('div');
            this.root.className = _options.className || 'mox-modal';
            this.root.appendChild(this.shadow);

            document.body.appendChild(this.root);

            this.onCloseCallbacks = [];
            this.onContentReplacedCallbacks = [];
            this.escapeHandle = CloseOnEscapeQueue.enqueue(() => this.close());
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
            let response = await fetch(url, getInit)
                .then(Mox.Utils.Fetch.checkErrorCode)
                .then(Mox.Utils.Fetch.redirect(url => { window.location.href = url; }));
            let text = await response.text();
            modal.contentContainer.innerHTML = text;
            modal.contentWrapper.classList.remove('loading');

            document.body.style.overflow = 'hidden';

            if (!Modal.allOpenModals) Modal.allOpenModals = [];
            Modal.allOpenModals.push(modal);

            return modal;
        }

        static async createFormDialog(url: string, options: FormDialogOptions): Promise<Mox.UI.Modal> {
            const _options = options || {} as FormDialogOptions;
            _options.actualWindowHref = _options.actualWindowHref || window.location.href;

            const modal = await Modal.createDialog(url);

            function bindEvents() {
                const form = modal.contentContainer.querySelector('form');
                if (form) {
                    form.addEventListener('submit', e => submitForm(form, e));

                    const firstInput = form.querySelector('input[type="text"], textarea, select') as HTMLInputElement
                    if(firstInput) {
                        firstInput.focus();
                    }
                }

                if(!_options.dontEvaluateScripts) {
                    const scripts = modal.contentContainer.getElementsByTagName('script');
                    for (let i = 0; i < scripts.length; i++) {
                        eval(scripts.item(i).innerText);
                    }
                }

                console.log(modal.contentContainer.querySelectorAll(_options.buttonSelector || 'a'));

                const editButtons = Mox.Utils.DOM.nodeListOfToArray(modal.contentContainer.querySelectorAll(_options.buttonSelector || 'a'));
                editButtons.forEach(button => button.addEventListener('click', event => { 
                    event.preventDefault();
                    if(_options.onButtonClicked) {
                        _options.onButtonClicked(modal, button, event);
                    }
                }));
            }

            async function submitForm(form, event) {
                event.preventDefault();

                if(_options.onSubmit) {
                    _options.onSubmit(modal, form, event);
                }

                const response = await Mox.Utils.Fetch.submitAjaxForm(form, event);
                console.log(response);
                if(response.type === 'html') {
                    modal.replaceContentWithHtml(response.data as string);
                } else if(response.type === 'json') {
                    const responseData = response.data as Mox.Utils.Fetch.FormPostResponse;
                    
                    if(_options.onSubmitFormData) {
                        _options.onSubmitFormData(modal, form, responseData);
                    }

                    if(responseData.handleManually) {
                        return;
                    }

                    if (responseData.action === 'replaceWithContent') {
                        modal.replaceContentWithHtml(responseData.data);
                    } else if (responseData.action === 'redirect') {
                        const indexOfPath = _options.actualWindowHref.toLowerCase().indexOf((responseData.data as string).toLowerCase())
                        if(indexOfPath !== _options.actualWindowHref.length - (responseData.data as string).length) {
                            window.location.href = _options.actualWindowHref;
                        } else {
                            await modal.close();
                        }
                    }
                }
            }

            modal.onContentReplaced(() => {
                bindEvents();
            });

            bindEvents();

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
            this.onContentReplacedCallbacks.forEach(x => x());
        }

        replaceContentWithHtml(html: string) {
            this.contentContainer.innerHTML = html;
            this.onContentReplacedCallbacks.forEach(x => x());
        }

        onClose(callback: () => void) {
            this.onCloseCallbacks.push(callback);
        }

        onContentReplaced(callback: () => void) {
            this.onContentReplacedCallbacks.push(callback);
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

    export class DataTable {
        containerElement: HTMLElement;
        renderCompleteCallback?: () => Promise<void>;
        private addQueryCallback?: () => string;
        private baseUrl: string;

        static async create(containerElement: HTMLElement, baseUrl: string, renderCompleteCallback?: () => Promise<void>, addQueryCallback?: () => string) {
            const table = new DataTable(containerElement);
            table.renderCompleteCallback = renderCompleteCallback;
            table.addQueryCallback = addQueryCallback;
            table.baseUrl = baseUrl;

            const renderUrl = localStorage.getItem(table.containerElement.id) || table.baseUrl;
            await table.render(table.addWindowQueryTo(renderUrl));

            return table;
        }

        private addWindowQueryTo(url: string): string {
            const urlQuery = DataTable.getQuery(url);
            const windowQuery = DataTable.getQuery(window.location.href);
            const addedQuery = this.addQueryCallback ? this.addQueryCallback() : '';
            const newQuery = [urlQuery, addedQuery, windowQuery].filter(x => !!x).join('&')
            const baseUrl = url.split('?')[0];
            return baseUrl + '?' + newQuery;
        }

        private static getQuery(url: string): string {
            const s = url.split('?');
            if (s.length > 1) return s[1];
            return "";
        }

        private constructor(containerElement: HTMLElement) {
            this.containerElement = containerElement;
        }

        private async render(url: string) {
            const getInit: RequestInit = {
                credentials: 'same-origin',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest',
                },
            };
            this.containerElement.innerHTML = await fetch(url, getInit)
                .then(Mox.Utils.Fetch.checkErrorCode)
                .then(Mox.Utils.Fetch.parseText);

            localStorage.setItem(this.containerElement.id + '_fullurl', url);

            const sortLinks = Mox.Utils.DOM.nodeListOfToArray(this.containerElement.querySelectorAll('th.sortable a, .mox-pager a')) as HTMLAnchorElement[];
            sortLinks.forEach(x => x.addEventListener('click', e => {
                e.preventDefault();
                const fullUrl = this.addWindowQueryTo(x.href);
                this.render(fullUrl);
                localStorage.setItem(this.containerElement.id, x.href);
                localStorage.setItem(this.containerElement.id + '_fullurl', fullUrl);
            }));

            if (this.renderCompleteCallback)
                await this.renderCompleteCallback();
        }

        static getStoredParam(tableElementId: string, key: string, defaultValue: string): string {
            const renderUrl = localStorage.getItem(tableElementId + '_fullurl');
            if (!renderUrl)
                return defaultValue;
            const query = DataTable.getQuery(renderUrl);
            const queries = query.split(/&/g);
            const result = queries.map(x => x.split('=')).filter(x => x[0].toLowerCase() === key.toLowerCase()).map(x => x[1]);
            return result.length ? result[0] : defaultValue;
        }

        async refresh(addQueryCallback?: () => string) {
            this.addQueryCallback = addQueryCallback || this.addQueryCallback;
            const renderUrl = localStorage.getItem(this.containerElement.id) || this.baseUrl;
            await this.render(this.addWindowQueryTo(renderUrl));
        }
    }
}