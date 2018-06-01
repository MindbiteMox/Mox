var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : new P(function (resolve) { resolve(result.value); }).then(fulfilled, rejected); }
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
            if (f = 1, y && (t = y[op[0] & 2 ? "return" : op[0] ? "throw" : "next"]) && !(t = t.call(y, op[1])).done) return t;
            if (y = 0, t) op = [0, t.value];
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
var Mox;
(function (Mox) {
    var Utils;
    (function (Utils) {
        var DOM = /** @class */ (function () {
            function DOM() {
            }
            DOM.collectionOfToArray = function (collection) {
                var out = [];
                for (var i = 0; i < collection.length; i++) {
                    out.push(collection.item(i));
                }
                return out;
            };
            DOM.nodeListOfToArray = function (collection) {
                return DOM.collectionOfToArray(collection);
            };
            DOM.closest = function (element, selector) {
                if (element === null)
                    return null;
                if (element.matches(selector))
                    return element;
                return DOM.closest(element.parentNode, selector);
            };
            return DOM;
        }());
        Utils.DOM = DOM;
        var Fetch = /** @class */ (function () {
            function Fetch() {
            }
            Fetch.postFormOptions = function (form) {
                return {
                    method: 'POST',
                    body: new FormData(form),
                    credentials: 'same-origin',
                    headers: {
                        'X-Requested-With': 'XMLHttpRequest'
                    },
                };
            };
            Fetch.redirect = function (onRedirect) {
                var _this = this;
                var func = function (response) { return __awaiter(_this, void 0, void 0, function () {
                    return __generator(this, function (_a) {
                        if (response.type === 'opaqueredirect' || response.status >= 300 && response.status < 400) {
                            onRedirect(response.url);
                            return [2 /*return*/, Promise.reject('Fetch: redirecting to ' + response.url)];
                        }
                        return [2 /*return*/, response];
                    });
                }); };
                return func;
            };
            Fetch.checkErrorCode = function (response) {
                return __awaiter(this, void 0, void 0, function () {
                    var error;
                    return __generator(this, function (_a) {
                        if (response.type === 'opaqueredirect' || response.status >= 200 && response.status < 300 || response.status >= 400 && response.status < 500) {
                            return [2 /*return*/, response];
                        }
                        error = new Error(response.statusText);
                        error.response = response;
                        throw error;
                    });
                });
            };
            Fetch.parseJson = function (response) {
                return response.json();
            };
            Fetch.parseText = function (response) {
                return response.text();
            };
            Fetch.submitForm = function (event, onRedirect) {
                var form = event.target;
                var url = form.action;
                var init = Mox.Utils.Fetch.postFormOptions(form);
                init.redirect = 'manual';
                var button = form.querySelector('input[type=submit]');
                button.classList.add('loading');
                return new Promise(function (resolve, reject) {
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
            };
            return Fetch;
        }());
        Utils.Fetch = Fetch;
        var Ajax = /** @class */ (function () {
            function Ajax() {
            }
            Ajax.getJSON = function (url) {
                return __awaiter(this, void 0, void 0, function () {
                    return __generator(this, function (_a) {
                        return [2 /*return*/, new Promise(function (resolve, reject) {
                                var request = new XMLHttpRequest();
                                request.open("GET", url, true);
                                request.setRequestHeader('Content-Type', 'application/json');
                                request.onreadystatechange = function (event) {
                                    if (request.readyState === 4) {
                                        if (request.status === 200) {
                                            resolve(JSON.parse(request.responseText));
                                        }
                                        else {
                                            reject(request.status);
                                        }
                                    }
                                };
                                request.send();
                            })];
                    });
                });
            };
            Ajax.postJSON = function (url, data) {
                return __awaiter(this, void 0, void 0, function () {
                    return __generator(this, function (_a) {
                        return [2 /*return*/, new Promise(function (resolve, reject) {
                                var request = new XMLHttpRequest();
                                request.open("POST", url, true);
                                request.setRequestHeader('Content-Type', 'application/json');
                                request.onreadystatechange = function (event) {
                                    if (request.readyState === 4) {
                                        if (request.status === 200) {
                                            resolve(JSON.parse(request.responseText));
                                        }
                                        else {
                                            reject(request.status);
                                        }
                                    }
                                };
                                request.send(JSON.stringify(data));
                            })];
                    });
                });
            };
            return Ajax;
        }());
        Utils.Ajax = Ajax;
    })(Utils = Mox.Utils || (Mox.Utils = {}));
})(Mox || (Mox = {}));
//# sourceMappingURL=utils.js.map