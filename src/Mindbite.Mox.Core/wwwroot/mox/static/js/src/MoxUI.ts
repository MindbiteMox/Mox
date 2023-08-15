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
        onSubmit?: (modal: Modal, form: HTMLFormElement, event: Event) => void;
        onSubmitFormData?: (modal: Modal, form: HTMLFormElement, responseData: Mox.Utils.Fetch.FormPostResponse) => void;
        onButtonClicked?: (modal: Modal, button: Element, event: Event) => void;
        buttonSelector?: string;
        dontEvaluateScripts?: boolean;
        actualWindowHref?: string;
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

        static allOpenModals: Modal[];

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
            this.shadow.className = 'mox-modal-shadow loading';
            this.shadow.appendChild(this.contentWrapper);

            this.root = document.createElement('div');
            this.root.className = _options.className || 'mox-modal';
            this.root.appendChild(this.shadow);

            document.body.appendChild(this.root);

            this.onCloseCallbacks = [];
            this.onContentReplacedCallbacks = [];
            this.escapeHandle = CloseOnEscapeQueue.enqueue(() => this.close());
        }

        static async createDialog(url: string, configureRequestInit?: (init: RequestInit) => void): Promise<Mox.UI.Modal> {
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

            configureRequestInit?.call(this, getInit);

            let response = await fetch(url, getInit)
                .then(Mox.Utils.Fetch.checkErrorCode)
                .then(Mox.Utils.Fetch.redirect(url => { window.location.href = url; }));
            let text = await response.text();
            modal.contentContainer.innerHTML = text;
            modal.shadow.classList.remove('loading');
            modal.contentWrapper.classList.remove('loading');

            document.body.style.overflow = 'hidden';

            if (!Modal.allOpenModals) Modal.allOpenModals = [];
            Modal.allOpenModals.push(modal);

            return modal;
        }

        static async createFormDialog(url: string, options: FormDialogOptions, configureRequestInit?: (init: RequestInit) => void): Promise<Mox.UI.Modal> {
            const _options = options || {} as FormDialogOptions;
            _options.actualWindowHref = _options.actualWindowHref || window.location.href;

            const modal = await Modal.createDialog(url, configureRequestInit);

            function bindEvents() {
                const form = modal.contentContainer.querySelector('form');
                if (form) {
                    form.addEventListener('submit', e => submitForm(form, e));

                    const firstInput = form.querySelector('input:not([type="hidden"]), textarea, select') as HTMLInputElement
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

                const response = await post(form.action, new FormData(form), null, null, configureRequestInit)
                console.log(response);
                if (response.type === 'html') {
                    modal.replaceContentWithHtml(response.data as string);

                    if (_options.onSubmit) {
                        _options.onSubmit(modal, form, event);
                    }
                } else if (response.type === 'json') {
                    const responseData = response.data as Mox.Utils.Fetch.FormPostResponse;

                    if (_options.onSubmitFormData) {
                        _options.onSubmitFormData(modal, form, responseData);
                    }

                    if (_options.onSubmit) {
                        _options.onSubmit(modal, form, event);
                    }

                    if (responseData.handleManually) {
                        return;
                    }

                    if (responseData.action === 'replaceWithContent') {
                        modal.replaceContentWithHtml(responseData.data);
                    } else if (responseData.action === 'redirect') {
                        const indexOfPath = _options.actualWindowHref.toLowerCase().indexOf((responseData.data as string).toLowerCase())
                        if (indexOfPath !== _options.actualWindowHref.length - (responseData.data as string).length) {
                            window.location.href = _options.actualWindowHref;
                        } else {
                            await modal.close();
                        }
                    } else if (responseData.action === 'close') {
                        await modal.close();
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
                modal.shadow.classList.remove('loading');
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

        async replaceContent(url: string, configureRequestInit?: (init: RequestInit) => void) {
            let getInit: RequestInit = {
                credentials: 'same-origin',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                }
            };
            configureRequestInit?.call(this, getInit);

            this.shadow.classList.add('loading');
            this.contentWrapper.classList.add('loading');
            let response = await fetch(url, getInit).then(Mox.Utils.Fetch.checkErrorCode).then(Mox.Utils.Fetch.redirect(url => { window.location.href = url; }));
            let text = await response.text();
            this.contentContainer.innerHTML = text;
            this.shadow.classList.remove('loading');
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

    export interface DataTableOptions {
        container: HTMLElement;
        url: string;
        tableId?: string;
        onRenderComplete?: (dataTable: DataTable) => Promise<void>;
        addQuery?: (dataTable: DataTable) => string;
        filters: (HTMLInputElement | HTMLSelectElement | string)[];
        rememberFilters?: boolean;
        skipRenderOnCreate?: boolean;
        configureRequestInit?: (init: RequestInit) => void;
        onSelectedIdsChanged?: (dataTable: DataTable) => void;
    }

    export class DataTable {
        options: DataTableOptions;
        filters: (HTMLInputElement | HTMLSelectElement)[];
        selectedIds: number[] = [];
        selectionEnabled: boolean;

        get tableId() {
            return Utils.URL.splitUrl(window.location.href).domainAndPath + (this.options.tableId || this.options.container.id);
        }

        get containerElement() {
            return this.options.container;
        }

        get filterQueryString(): string {
            const result = {};

            this.filters.forEach(x => {
                switch (x.nodeName) {
                    case 'INPUT':
                        switch (x.type) {
                            case 'checkbox':
                                result[x.name] = (x as HTMLInputElement).checked;
                                break;
                            case 'radio':
                                if ((x as HTMLInputElement).checked) {
                                    result[x.name] = x.value;
                                }
                                break;
                            default:
                                result[x.name] = x.value;
                                break;
                        }
                        break;
                    case 'SELECT':
                        result[x.name] = x.value;
                        break;
                    default:
                        throw new TypeError('Filter "' + x.name + '" is not an input or select');
                }
            });

            return Utils.URL.queryStringFromObject(result);
        }

        static async create(options: DataTableOptions): Promise<DataTable> {
            const table = new DataTable(options);

            table.filters = table.options.filters.map(x => typeof (x) === 'string' ? document.getElementById(x) as (HTMLInputElement | HTMLSelectElement) : x);
            table.filters.forEach(x => {
                switch (x.nodeName) {
                    case 'INPUT':
                        switch (x.type) {
                            case 'text':
                            case 'search':
                                {
                                    let refreshTimeout = null;
                                    x.addEventListener('keyup', () => {
                                        if (refreshTimeout) {
                                            clearTimeout(refreshTimeout);
                                        }

                                        refreshTimeout = setTimeout(() => {
                                            table.refresh();
                                            refreshTimeout = null;
                                        }, 300);
                                    });
                                }
                                break;
                            default:
                                x.addEventListener('change', () => table.refresh());
                                break;
                        }
                        break;
                    case 'SELECT':
                        x.addEventListener('change', () => table.refresh());
                        break;
                    default:
                        throw new TypeError('Filter "' + x.name + '" is not an input or select');
                }
            });

            const savedFiltersQuery = localStorage.getItem(table.tableId + '_filtersquery');
            if (savedFiltersQuery && URLSearchParams) {
                const queryParams = new URLSearchParams(savedFiltersQuery);

                table.filters.forEach(x => {

                    if (queryParams.get(x.name) === null) {
                        return;
                    }

                    switch (x.nodeName) {
                        case 'INPUT':
                            switch (x.type) {
                                case 'checkbox':
                                    (x as HTMLInputElement).checked = queryParams.get(x.name).toLowerCase() === 'true';
                                    break;
                                case 'radio':
                                    (x as HTMLInputElement).checked = queryParams.get(x.name) === x.value;
                                    break;
                                default:
                                    x.value = queryParams.get(x.name);
                                    break;
                            }
                            break;
                        case 'SELECT':
                            x.value = queryParams.get(x.name);
                            break;
                        default:
                            throw new TypeError('Filter "' + x.name + '" is not an input or select');
                    }
                });
            }

            const renderUrl = localStorage.getItem(table.tableId) || table.options.url;
            const addedQuery = table.options.addQuery ? table.options.addQuery(table) : '';
            const url = Utils.URL.addWindowQueryTo(renderUrl, [addedQuery, table.filterQueryString, 'r=' + Math.random()]);

            if (!table.options.skipRenderOnCreate) {
                await table.render(url);
            }

            return table;
        }

        private constructor(options: DataTableOptions) {
            this.options = options || {} as DataTableOptions;
            this.options.filters = this.options.filters || [];
            this.options.rememberFilters = this.options.rememberFilters === undefined ? true : !!this.options.rememberFilters;
            this.options.skipRenderOnCreate = this.options.skipRenderOnCreate || !!this.options.skipRenderOnCreate;
        }

        private async render(url: string) {
            
            if(!this.options.container.children.length) {
                this.options.container.classList.add('mox-datatable-loader');
            }

            const getInit: RequestInit = {
                credentials: 'same-origin',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest',
                },
            };

            this.options.configureRequestInit?.call(this, getInit);
            var response = fetch(url, getInit).then(Mox.Utils.Fetch.checkErrorCode);
            var headers = await response.then(x => x.headers);
            console.log(headers, this.selectionEnabled, this);
            if (headers.get('X-Mox-DataTable-Selection') == '1') {
                this.selectionEnabled = true;
            }
            this.options.container.innerHTML = await response.then(Mox.Utils.Fetch.parseText);

            if (this.options.rememberFilters) {
                localStorage.setItem(this.tableId + '_fullurl', url);
                localStorage.setItem(this.tableId + '_filtersquery', this.filterQueryString);
            }

            const sortLinks = Mox.Utils.DOM.nodeListOfToArray(this.options.container.querySelectorAll('th.sortable a, .mox-pager a')) as HTMLAnchorElement[];
            sortLinks.forEach(x => x.addEventListener('click', e => {
                e.preventDefault();
                const addedQuery = this.options.addQuery ? this.options.addQuery(this) : '';
                const fullUrl = Utils.URL.addWindowQueryTo(x.href, [addedQuery, this.filterQueryString, 'r=' + Math.random()]);
                this.render(fullUrl);

                if (this.options.rememberFilters) {
                    localStorage.setItem(this.tableId, x.href);
                    localStorage.setItem(this.tableId + '_fullurl', fullUrl);
                    localStorage.setItem(this.tableId + '_filtersquery', this.filterQueryString);
                }
            }));

            this.options.container.classList.remove('mox-datatable-loader');

            if (this.selectionEnabled) {
                this.checkSelectedRows(headers);
                this.options.container.addEventListener('change', e => {
                    const target = e.target as HTMLElement;
                    if (target.tagName === 'INPUT' && (target as HTMLInputElement).type.toLowerCase() === 'checkbox') {
                        const checkbox = target as HTMLInputElement;
                        if (checkbox.name === 'rowId') {
                            if (checkbox.checked) {
                                this.selectedIds.push(parseInt(checkbox.value));
                            } else {
                                this.selectedIds.splice(this.selectedIds.indexOf(parseInt(checkbox.value)), 1);
                            }

                            if (this.options.onSelectedIdsChanged) {
                                this.options.onSelectedIdsChanged(this);
                            }
                        } else if (checkbox.name === 'selectAll') {
                            // TODO: this
                        }
                    }
                });
            }

            if (this.options.onRenderComplete) {
                await this.options.onRenderComplete(this);
            }
        }

        async refresh() {
            const renderUrl = localStorage.getItem(this.tableId) || this.options.url;
            const addedQuery = this.options.addQuery ? this.options.addQuery(this) : '';
            const url = Utils.URL.addWindowQueryTo(renderUrl, [addedQuery, this.filterQueryString, 'r=' + Math.random()]);
            await this.render(url);
        }

        checkSelectedRows(headers) {
            // TODO: this
        }
    }

    interface ShortcutStackFrame {
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
                    e.preventDefault();
                    e.stopPropagation();

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

    export class CheckboxTree {
        static onClick(container: HTMLElement, input: HTMLInputElement, groupName: string) {
            this.updateParent(container, input, groupName);
            this.updateChildren(container, input, groupName);
        }

        private static updateParent(container: HTMLElement, input: HTMLInputElement, groupName: string) {
            const nameParts = groupName.split('/');
            const parentName = nameParts.slice(0, nameParts.length - 1).join('/');
            const parentInput = container.querySelector('input[data-id="' + parentName + '"]') as HTMLInputElement;
            if (!parentInput) {
                return;
            }

            const siblingsAndChildren = container.querySelectorAll('input[data-id^="' + parentName + '/"]');

            let allChecked = true;

            for (let i = 0; i < siblingsAndChildren.length; i++) {
                const element = siblingsAndChildren.item(i) as HTMLInputElement;
                if (!element.checked) {
                    allChecked = false;
                    break;
                }
            }

            parentInput.checked = allChecked;
        }

        private static updateChildren(container: HTMLElement, input: HTMLInputElement, groupName: string) {
            const children = container.querySelectorAll('input[data-id^="' + groupName + '/"]');

            for (let i = 0; i < children.length; i++) {
                const element = children.item(i) as HTMLInputElement;
                element.checked = input.checked;
            }
        }
    }
}