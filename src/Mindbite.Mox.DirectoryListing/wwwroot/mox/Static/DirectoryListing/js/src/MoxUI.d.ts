declare namespace Mox.UI {
    class MobileMenu {
        private openButton;
        private menu;
        private shadow;
        private windowWidth;
        constructor(openButton: HTMLElement, menu: HTMLElement, shadowClass: string, windowWidth: number);
        static initDefault(): MobileMenu;
        private openClicked;
        private closeClicked;
    }
    interface ModalOptions {
        className?: string;
        contentClassName?: string;
    }
    interface FormDialogOptions {
        onSubmit?: (modal: Modal, form: HTMLFormElement, event: Event) => void;
        onSubmitFormData?: (modal: Modal, form: HTMLFormElement, responseData: Mox.Utils.Fetch.FormPostResponse) => void;
        onButtonClicked?: (modal: Modal, button: Element, event: Event) => void;
        buttonSelector?: string;
        dontEvaluateScripts?: boolean;
        actualWindowHref?: string;
    }
    class Modal {
        private escapeHandle;
        private root;
        private shadow;
        private contentWrapper;
        private closeButton;
        private onCloseCallbacks;
        private onContentReplacedCallbacks;
        contentContainer: HTMLElement;
        static allOpenModals: Modal[];
        constructor(options?: ModalOptions);
        static createDialog(url: string, configureRequestInit?: (init: RequestInit) => void): Promise<Mox.UI.Modal>;
        static createFormDialog(url: string, options: FormDialogOptions, configureRequestInit?: (init: RequestInit) => void): Promise<Mox.UI.Modal>;
        static createFormDialog(modal: Modal, options: FormDialogOptions, configureRequestInit?: (init: RequestInit) => void): Promise<Mox.UI.Modal>;
        static createDialogWithContent(htmlContent: string): Mox.UI.Modal;
        static closeAll(): Promise<void>;
        close(): Promise<void>;
        replaceContent(url: string, configureRequestInit?: (init: RequestInit) => void): Promise<void>;
        replaceContentWithHtml(html: string): void;
        onClose(callback: () => void): void;
        onContentReplaced(callback: () => void): void;
        static setupHistory(modal: Modal, url: string): Modal;
    }
    interface DataTableOptions {
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
    class DataTable {
        options: DataTableOptions;
        filters: (HTMLInputElement | HTMLSelectElement)[];
        selectedIds: number[];
        selectionEnabled: boolean;
        get tableId(): string;
        get containerElement(): HTMLElement;
        get filterQueryString(): string;
        static create(options: DataTableOptions): Promise<DataTable>;
        private constructor();
        private render;
        refresh(): Promise<void>;
        checkSelectedRows(headers: any): void;
    }
    type CloseOnEscapeHandle = number;
    class CloseOnEscapeQueue {
        private static isEventsSetup;
        private static lastHandle;
        private static handles;
        private static setupEvents;
        static enqueue(callback: () => void | Promise<void>): CloseOnEscapeHandle;
        static remove(handle: CloseOnEscapeHandle): void;
    }
    class GlobalInstances {
        static mobileMenu: MobileMenu;
        static initDefault(): void;
    }
    class CheckboxTree {
        static onClick(container: HTMLElement, input: HTMLInputElement, groupName: string): void;
        private static updateParent;
        private static updateChildren;
    }
}
