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
    class Modal {
        private escapeHandle;
        private root;
        private shadow;
        private contentWrapper;
        private closeButton;
        private onCloseCallbacks;
        contentContainer: HTMLElement;
        private static allOpenModals;
        constructor(options?: ModalOptions);
        static createDialog(url: string): Promise<Mox.UI.Modal>;
        static createDialogWithContent(htmlContent: string): Mox.UI.Modal;
        static closeAll(): Promise<void>;
        close(): Promise<void>;
        replaceContent(url: string): Promise<void>;
        onClose(callback: () => void): void;
        static setupHistory(modal: Modal, url: string): Modal;
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
    class DataTable {
        containerElement: HTMLElement;
        renderCompleteCallback?: () => Promise<void>;
        private addQueryCallback?;
        private baseUrl;
        static create(containerElement: HTMLElement, baseUrl: string, renderCompleteCallback?: () => Promise<void>, addQueryCallback?: () => string): Promise<DataTable>;
        private addWindowQueryTo;
        private static getQuery;
        private constructor();
        private render;
        static getStoredParam(tableElementId: string, key: string, defaultValue: string): string;
        refresh(addQueryCallback?: () => string): Promise<void>;
    }
}
