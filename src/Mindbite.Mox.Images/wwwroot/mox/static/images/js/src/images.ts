
namespace Mox.Images {
    export const onChangeCallbacks: (() => void)[] = [];

    export function onChange(callback: () => void) {
        onChangeCallbacks.push(callback);
    }

    export function triggerOnChange() {
        for (var callback of onChangeCallbacks) {
            callback();
        }
    }

    export async function uploadMulti(url: string, form: HTMLFormElement, container: HTMLElement, prefix: string) {
        container.classList.add('loading');
        const formData = getFormData(form, prefix);
        try {
            const response = await post(url, formData, { prefix });

            container.innerHTML = response.data;

        } finally {
            container.classList.remove('loading');
        }

        triggerOnChange();
    }

    export async function updateMulti(url: string, form: HTMLFormElement, container: HTMLElement, prefix: string) {
        const formData = getFormData(form, prefix);
        try {
            const response = await post(url, formData, { prefix });

            container.innerHTML = response.data;

        } finally {
        }

        triggerOnChange();
    }
}