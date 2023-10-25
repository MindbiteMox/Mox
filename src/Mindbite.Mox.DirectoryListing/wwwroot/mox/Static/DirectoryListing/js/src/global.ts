
async function documentsUploadFileX(form: HTMLFormElement) {
    const fileInput = form.querySelector('input[type="file"]') as HTMLInputElement;

    var oldFormParent = form.parentElement;
    var formHTML = oldFormParent.innerHTML;

    const dialog = await Mox.UI.Modal.createDialogWithContent(`
        <h1>Ladda upp filer</h1>
        <fieldset class="buttons">
            <p>
                <button type="button" class="mox-button save">Ladda upp</button>
            </p>
        </fieldset>
    `);

    var content = dialog.contentContainer;
    var heading = content.querySelector('h1');
    content.insertBefore(form, heading.nextElementSibling);
    form.classList.add('in-modal');
    form.querySelector('.document-upload-uploaded-file-count').innerHTML = fileInput.files.length.toString();

    oldFormParent.innerHTML = form.querySelector('.document-upload-list-only label').outerHTML;
    dialog.onClose(() => {
        oldFormParent.innerHTML = formHTML;
    });

    dialog.contentContainer.querySelector('.mox-button.save').addEventListener('click', e => {
        const loadingDialog = Mox.UI.Modal.createDialogWithContent(`
            <h1 style="text-align: center;">Laddar upp...</h1>
        `);
        loadingDialog.contentContainer.parentElement.querySelector('.mox-modal-close').remove();
        form.submit();
    });
}

async function documentsUploadFile(form: HTMLFormElement) {
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
                modal.contentContainer.innerHTML = '<h1 style="text-align: center;">Laddar upp...</h1>';
                form.submit();
            }
        });
    } else {
        const loadingDialog = Mox.UI.Modal.createDialogWithContent(`
            <h1 style="text-align: center;">Laddar upp...</h1>
        `);
        form.submit();
    }
}