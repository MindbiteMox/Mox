
namespace Mox.Images {
    export async function uploadMulti(url: string, form: HTMLFormElement, container: HTMLElement, prefix: string) {
        container.classList.add('loading');
        const formData = getFormData(form, prefix);
        try {
            const response = await post(url, formData, { prefix });

            container.innerHTML = response.data;
        } finally {
            container.classList.remove('loading');
        }
    }

    export async function updateMulti(url: string, form: HTMLFormElement, container: HTMLElement, prefix: string) {
        const formData = getFormData(form, prefix);
        try {
            const response = await post(url, formData, { prefix });

            container.innerHTML = response.data;
        } finally {
        }
    }
}