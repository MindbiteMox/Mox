
//declare function getFormData(form: HTMLFormElement, prefix: string): FormData;
//declare function post(url: string, body: BodyInit, queryParams?: any, additionalHeaders?: any): Promise<{ type: 'html' | 'json', data: string | Mox.Utils.Fetch.FormPostResponse | any }> ;

async function uploadMultiImage(url: string, form: HTMLFormElement, container: HTMLElement, prefix: string) {
    container.classList.add('loading');
    const formData = getFormData(form, prefix);
    try {
        const response = await post(url, formData, { prefix });

        container.innerHTML = response.data;
    } finally {
        container.classList.remove('loading');
    }
}

async function updateMultiImage(url: string, form: HTMLFormElement, container: HTMLElement, prefix: string) {
    const formData = getFormData(form, prefix);
    try {
        const response = await post(url, formData, { prefix });

        container.innerHTML = response.data;
    } finally {
    }
}