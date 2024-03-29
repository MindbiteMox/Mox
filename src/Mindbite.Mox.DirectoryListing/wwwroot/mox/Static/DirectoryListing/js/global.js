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
        while (g && (g = 0, op[0] && (_ = 0)), _) try {
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
function documentsUploadFile(form, loadingText) {
    return __awaiter(this, void 0, void 0, function () {
        var fileInput, preflightUrlInput, formData, i, response, dialog, formDialog, loadingDialog;
        return __generator(this, function (_a) {
            switch (_a.label) {
                case 0:
                    fileInput = form.querySelector('input[type="file"]');
                    preflightUrlInput = form.querySelector('input[name="PreflightUrl"]');
                    formData = new FormData(form);
                    formData.delete('UploadedFiles');
                    for (i = 0; i < fileInput.files.length; i++) {
                        formData.append('FileNames[]', fileInput.files.item(i).name);
                    }
                    return [4 /*yield*/, post(preflightUrlInput.value, formData)];
                case 1:
                    response = _a.sent();
                    if (!(response.type === 'html')) return [3 /*break*/, 4];
                    return [4 /*yield*/, Mox.UI.Modal.createDialogWithContent(response.data)];
                case 2:
                    dialog = _a.sent();
                    return [4 /*yield*/, Mox.UI.Modal.createFormDialog(dialog, {
                            onSubmitFormData: function (modal, modalForm, response) {
                                var preflightElements = Array.from(modal.contentContainer.querySelectorAll('[preflight-data]'));
                                for (var _i = 0, preflightElements_1 = preflightElements; _i < preflightElements_1.length; _i++) {
                                    var preflightElement = preflightElements_1[_i];
                                    form.insertBefore(preflightElement, null);
                                }
                                modal.contentContainer.innerHTML = '<h1 style="text-align: center;">' + loadingText + '</h1>';
                                form.submit();
                            }
                        })];
                case 3:
                    formDialog = _a.sent();
                    return [3 /*break*/, 5];
                case 4:
                    loadingDialog = Mox.UI.Modal.createDialogWithContent("\n            <h1 style=\"text-align: center;\">".concat(loadingText, "</h1>\n        "));
                    form.submit();
                    _a.label = 5;
                case 5: return [2 /*return*/];
            }
        });
    });
}
//# sourceMappingURL=global.js.map