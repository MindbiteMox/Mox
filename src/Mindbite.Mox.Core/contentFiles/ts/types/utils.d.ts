declare namespace Mox.Utils {
    class DOM {
        static collectionOfToArray<T extends Element>(collection: HTMLCollectionOf<T>): T[];
        static nodeListOfToArray<T extends Element>(collection: NodeListOf<T>): T[];
        static closest(element: HTMLElement, selector: string): HTMLElement;
    }
    class Fetch {
        static postFormOptions(form: HTMLFormElement): RequestInit;
        static redirect(onRedirect: (url: string) => void): (response: Response) => Promise<Response>;
        static checkErrorCode(response: Response): Promise<Response>;
        static parseJson(response: Response): Promise<any>;
        static parseText(response: Response): Promise<string>;
        static submitForm(event: Event, onRedirect: (url: string) => void): Promise<string>;
    }
    class Ajax {
        static getJSON(url: string): Promise<object>;
        static postJSON(url: string, data: object): Promise<object>;
    }
}
