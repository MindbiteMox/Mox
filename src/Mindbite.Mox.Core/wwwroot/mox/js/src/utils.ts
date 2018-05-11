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
            return DOM.collectionOfToArray(collection as HTMLCollectionOf<T>);
        }

        static closest(element: HTMLElement, selector: string): HTMLElement {
            if (element === null)
                return null;

            if (element.matches(selector))
                return element;

            return DOM.closest(element.parentNode as HTMLElement, selector);
        }
    }

    export class Fetch {
        static postFormOptions(form: HTMLFormElement): RequestInit {
            return {
                method: 'POST',
                body: new FormData(form),
                credentials: 'same-origin',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                },
            }
        }

        static async checkErrorCode(response: Response): Promise<Response> {
            if (response.status >= 200 && response.status < 400) {
                return response;
            }

            let error = new Error(response.statusText) as any;
            error.response = response;
            throw error;
        }

        static async redirect(response: Response): Promise<Response> {
            if ((response as any).redirected) {
                window.location.href = response.url;
                return Promise.reject('Fetch: redirecting to ' + response.url);
            }
            return response;
        }

        static parseJson(response: Response): Promise<any> {
            return response.json();
        }

        static parseText(response: Response): Promise<string> {
            return response.text();
        }

        static submitForm(event: Event): Promise<string> {
            let form = (event.target as HTMLFormElement);
            let url = form.action;
            let init = Mox.Utils.Fetch.postFormOptions(form);

            let button = form.querySelector('input[type=submit]');
            button.classList.add('loading');

            return new Promise<string>((resolve, reject) => {
                fetch(url, init)
                    .then(Mox.Utils.Fetch.checkErrorCode)
                    .then(Mox.Utils.Fetch.redirect)
                    .then(Mox.Utils.Fetch.parseText)
                    .then(function (text) {
                        resolve(text);
                    }).catch(function (error) {
                        reject(error);
                    });
            });
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