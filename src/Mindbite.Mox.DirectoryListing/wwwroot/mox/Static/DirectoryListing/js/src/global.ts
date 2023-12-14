async function documentsUploadFile(form: HTMLFormElement, loadingText: string) {
    const fileInput = form.querySelector('input[type="file"]') as HTMLInputElement;
    const preflightUrlInput = form.querySelector('input[name="PreflightUrl"]') as HTMLInputElement;
    var formData = new FormData(form);

    formData.delete('UploadedFiles');

    for (var i = 0; i < fileInput.files.length; i++) {
        formData.append('FileNames[]', fileInput.files.item(i).name);
    }

    var response = await post(preflightUrlInput.value, formData);
    if (response.type === 'html') {
        const dialog = await Mox.UI.Modal.createDialogWithContent(response.data);
        const formDialog = await Mox.UI.Modal.createFormDialog(dialog, {
            onSubmitFormData: (modal, modalForm, response) => {
                let preflightElements = Array.from(modal.contentContainer.querySelectorAll('[preflight-data]'));
                for (let preflightElement of preflightElements) {
                    form.insertBefore(preflightElement, null);
                }
                modal.contentContainer.innerHTML = '<h1 style="text-align: center;">' + loadingText + '</h1>';
                form.submit();
            }
        });
    } else {
        const loadingDialog = Mox.UI.Modal.createDialogWithContent(`
            <h1 style="text-align: center;">${loadingText}</h1>
        `);
        form.submit();
    }
}