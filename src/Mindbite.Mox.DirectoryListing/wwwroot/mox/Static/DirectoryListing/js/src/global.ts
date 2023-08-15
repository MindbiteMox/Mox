
async function documentsUploadFile(form: HTMLFormElement) {
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

    //const dialog = await Mox.UI.Modal.createDialogWithContent(`
    //    <h1>Ladda upp filer</h1>
    //    <div style="margin-bottom: 60px;">
    //        <p style="border-radius: 5px; padding: 10px; border: 1px solid #ccc;"><i class="far fa-file"></i> ${fileInput.files.length} fil(er) valda</p>
    //        <p>Välj vilken behörighet dina filer ska få</p>
            
    //    </div>        
    //    <fieldset class="buttons">
    //        <p>
    //            <button type="button" class="mox-button save">Ladda upp</button>
    //        </p>
    //    </fieldset>
    //`);

    dialog.contentContainer.querySelector('.mox-button.save').addEventListener('click', e => {
        const loadingDialog = Mox.UI.Modal.createDialogWithContent(`
            <h1 style="text-align: center;">Laddar upp...</h1>
        `);
        loadingDialog.contentContainer.parentElement.querySelector('.mox-modal-close').remove();
        form.submit();
    });
}