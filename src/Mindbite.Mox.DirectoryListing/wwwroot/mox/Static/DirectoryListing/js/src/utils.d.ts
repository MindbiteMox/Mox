declare namespace Mox.Utils {
    class DOM {
        static collectionOfToArray<T extends Element>(collection: HTMLCollectionOf<T>): T[];
        static nodeListOfToArray<T extends Element>(collection: NodeListOf<T>): T[];
        static closest(element: HTMLElement, selector: string): HTMLElement;
    }
    namespace Fetch {
        interface FormPostResponse {
            action: 'redirect' | 'replaceWithContent';
            data: string;
            handleManually: boolean;
        }
        function postFormOptions(form: HTMLFormElement): RequestInit;
        function redirect(onRedirect: (url: string) => void): (response: Response) => Promise<Response>;
        function doRedirect(response: Response): Promise<Response>;
        function checkErrorCode(response: Response): Promise<Response>;
        function parseJson(response: Response): Promise<any>;
        function parseText(response: Response): Promise<string>;
        function submitForm(event: Event, onRedirect: (url: string) => void): Promise<string>;
        function submitAjaxForm(form: HTMLFormElement, event: Event): Promise<{
            type: 'html' | 'json';
            data: string | FormPostResponse;
        }>;
    }
    class Ajax {
        static getJSON(url: string): Promise<object>;
        static postJSON(url: string, data: object): Promise<object>;
    }
    namespace URL {
        function addWindowQueryTo(url: string, additionalQueries?: string[]): string;
        function splitUrl(url: string): {
            domainAndPath: string;
            query: string;
        };
        function queryStringFromObject(object: Object): string;
    }
}
declare function queryString(params: any): string;
declare function addQueryToUrl(url: string, queryString: string): string;
declare function get(url: string, queryParams?: any): Promise<string>;
declare function getJSON(url: string, queryParams?: any): Promise<any>;
declare function post(url: string, body: BodyInit, queryParams?: any, additionalHeaders?: any): Promise<{
    type: 'html' | 'json';
    data: string | Mox.Utils.Fetch.FormPostResponse | any;
}>;
declare function getFormData(form: HTMLFormElement, prefix: string): FormData;
