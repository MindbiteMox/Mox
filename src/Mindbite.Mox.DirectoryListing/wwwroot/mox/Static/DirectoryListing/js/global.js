var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
var __generator = (this && this.__generator) || function (thisArg, body) {
    var _ = { label: 0, sent: function() { if (t[0] & 1) throw t[1]; return t[1]; }, trys: [], ops: [] }, f, y, t, g;
    return g = { next: verb(0), "throw": verb(1), "return": verb(2) }, typeof Symbol === "function" && (g[Symbol.iterator] = function() { return this; }), g;
    function verb(n) { return function (v) { return step([n, v]); }; }
    function step(op) {
        if (f) throw new TypeError("Generator is already executing.");
        while (_) try {
            if (f = 1, y && (t = op[0] & 2 ? y["return"] : op[0] ? y["throw"] || ((t = y["return"]) && t.call(y), 0) : y.next) && !(t = t.call(y, op[1])).done) return t;
            if (y = 0, t) op = [op[0] & 2, t.value];
            switch (op[0]) {
                case 0: case 1: t = op; break;
                case 4: _.label++; return { value: op[1], done: false };
                case 5: _.label++; y = op[1]; op = [0]; continue;
                case 7: op = _.ops.pop(); _.trys.pop(); continue;
                default:
                    if (!(t = _.trys, t = t.length > 0 && t[t.length - 1]) && (op[0] === 6 || op[0] === 2)) { _ = 0; continue; }
                    if (op[0] === 3 && (!t || (op[1] > t[0] && op[1] < t[3]))) { _.label = op[1]; break; }
                    if (op[0] === 6 && _.label < t[1]) { _.label = t[1]; t = op; break; }
                    if (t && _.label < t[2]) { _.label = t[2]; _.ops.push(op); break; }
                    if (t[2]) _.ops.pop();
                    _.trys.pop(); continue;
            }
            op = body.call(thisArg, _);
        } catch (e) { op = [6, e]; y = 0; } finally { f = t = 0; }
        if (op[0] & 5) throw op[1]; return { value: op[0] ? op[1] : void 0, done: true };
    }
};
function documentsUploadFile(form) {
    return __awaiter(this, void 0, void 0, function () {
        var fileInput, oldFormParent, formHTML, dialog, content, heading;
        return __generator(this, function (_a) {
            switch (_a.label) {
                case 0:
                    fileInput = form.querySelector('input[type="file"]');
                    oldFormParent = form.parentElement;
                    formHTML = oldFormParent.innerHTML;
                    return [4 /*yield*/, Mox.UI.Modal.createDialogWithContent("\n        <h1>Ladda upp filer</h1>\n        <fieldset class=\"buttons\">\n            <p>\n                <button type=\"button\" class=\"mox-button save\">Ladda upp</button>\n            </p>\n        </fieldset>\n    ")];
                case 1:
                    dialog = _a.sent();
                    content = dialog.contentContainer;
                    heading = content.querySelector('h1');
                    content.insertBefore(form, heading.nextElementSibling);
                    form.classList.add('in-modal');
                    form.querySelector('.document-upload-uploaded-file-count').innerHTML = fileInput.files.length.toString();
                    oldFormParent.innerHTML = form.querySelector('.document-upload-list-only label').outerHTML;
                    dialog.onClose(function () {
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
                    dialog.contentContainer.querySelector('.mox-button.save').addEventListener('click', function (e) {
                        var loadingDialog = Mox.UI.Modal.createDialogWithContent("\n            <h1 style=\"text-align: center;\">Laddar upp...</h1>\n        ");
                        loadingDialog.contentContainer.parentElement.querySelector('.mox-modal-close').remove();
                        form.submit();
                    });
                    return [2 /*return*/];
            }
        });
    });
}
//# sourceMappingURL=global.js.map