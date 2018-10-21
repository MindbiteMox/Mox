namespace Mox.Utils {
    export class DOM {
        static collectionOfToArray<T extends Element>(collection: HTMLCollectionOf<T>): T[] {
            let out: T[] = [];
            for (let i = 0; i < collection.length; i++) {
                out.push(collection.item(i));
            }
            return out;
        }

        static nodeListOfToArray<T extends Element>(collection: NodeListOf<T>): T[] {
            return DOM.collectionOfToArray<T>(collection as any);
        }

        static closest(element: HTMLElement, selector: string): HTMLElement {
            if (element === null)
                return null;

            if (element.matches(selector))
                return element;

            return DOM.closest(element.parentNode as HTMLElement, selector);
        }
    }

    export namespace Fetch {

        export interface FormPostResponse {
            action: 'redirect' | 'replaceWithContent';
            data: string,
            handleManually: boolean
        }

        export function postFormOptions(form: HTMLFormElement): RequestInit {
            var inputs = form.querySelectorAll('input[type="file"]:not([disabled])') as NodeListOf<HTMLInputElement>;
            inputs.forEach(function(input) {
                if (input.files.length > 0) 
                    return;
                input.setAttribute('disabled', '');
            });

            var formBody = new FormData(form);

            inputs.forEach(function(input) {
                input.removeAttribute('disabled');
            });

            return {
                method: 'POST',
                body: formBody,
                credentials: 'same-origin',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                },
            }
        }

        export function redirect(onRedirect: (url: string) => void): (response: Response) => Promise<Response> {
            var func = async (response: Response) => {
                if (response.type === 'opaqueredirect' || response.status >= 300 && response.status < 400) {
                    onRedirect(response.url);
                    return Promise.reject('Fetch: redirecting to ' + response.url);
                }
                return response;
            }
            return func;
        }

        export async function doRedirect(response: Response): Promise<Response> {
            if(response.status >= 300 && response.status < 400) {
                window.location.href = response.url;
                throw new Error('Fetch: fullRedirect to ' + response.url);
            }
            return response;
        }

        export async function checkErrorCode(response: Response): Promise<Response> {
            if (response.type === 'opaqueredirect' || response.status >= 200 && response.status < 300 || response.status >= 400 && response.status < 500) {
                return response;
            }

            let error = new Error(response.statusText) as any;
            error.response = response;
            throw error;
        }

        export function parseJson(response: Response): Promise<any> {
            return response.json();
        }

        export function parseText(response: Response): Promise<string> {
            return response.text();
        }

        export function submitForm(event: Event, onRedirect: (url: string) => void): Promise<string> {
            let form = (event.target as HTMLFormElement);
            let url = form.action;
            let init = Mox.Utils.Fetch.postFormOptions(form);
            init.redirect = 'manual';

            let button = form.querySelector('input[type=submit]');
            button.classList.add('loading');

            return new Promise<string>((resolve, reject) => {
                fetch(url, init)
                    .then(Mox.Utils.Fetch.checkErrorCode)
                    .then(Mox.Utils.Fetch.redirect(onRedirect))
                    .then(Mox.Utils.Fetch.parseText)
                    .then(function (text) {
                        resolve(text);
                        button.classList.remove('loading');
                    }).catch(function (error) {
                        reject(error);
                        button.classList.remove('loading');
                    });
            });
        }

        export async function submitAjaxForm(form: HTMLFormElement, event: Event): Promise<{ type: 'html' | 'json', data: string | FormPostResponse }> {
            const url = form.action;
            const init = Mox.Utils.Fetch.postFormOptions(form);

            let button = form.querySelector('input[type=submit]');
            button.classList.add('loading');

            const response = await fetch(url, init).then(Mox.Utils.Fetch.checkErrorCode);
            const contentType = response.headers.get('Content-Type');
            
            if(contentType.indexOf('text/html') > -1) {
                return { type: 'html', data: await Mox.Utils.Fetch.parseText(response) };
            } else if (contentType.indexOf('application/json') > -1) {
                return { type: 'json', data: await Mox.Utils.Fetch.parseJson(response) };
            } else {
                throw new Error('Content-Type: "' + contentType + '" cannot be used when responing to a form post request.');
            }
        }
    }
    
    export class Ajax {
        static async getJSON(url: string): Promise<object> {
            return new Promise<any>((resolve, reject) => {
                var request = new XMLHttpRequest();
                request.open("GET", url, true);
                request.setRequestHeader('Content-Type', 'application/json');
                request.onreadystatechange = function (event) {
                    if (request.readyState === 4) {
                        if(request.status === 200) {
                            resolve(JSON.parse(request.responseText));
                        } else {
                            reject(request.status);
                        }
                    }
                };

                request.send();
            });
        }

        static async postJSON(url: string, data: object): Promise<object> {
            return new Promise<any>((resolve, reject) => {
                var request = new XMLHttpRequest();
                request.open("POST", url, true);
                request.setRequestHeader('Content-Type', 'application/json');
                request.onreadystatechange = function (event) {
                    if (request.readyState === 4) {
                        if (request.status === 200) {
                            resolve(JSON.parse(request.responseText));
                        } else {
                            reject(request.status);
                        }
                    }
                };

                request.send(JSON.stringify(data));
            });
        }
    }
}