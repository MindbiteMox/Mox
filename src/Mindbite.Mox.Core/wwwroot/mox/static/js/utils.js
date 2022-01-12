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
        var Fetch;
        (function (Fetch) {
            function postFormOptions(form) {
                var inputs = form.querySelectorAll('input[type="file"]:not([disabled])');
                inputs.forEach(function (input) {
                    if (input.files.length > 0)
                        return;
                    input.setAttribute('disabled', '');
                });
                var formBody = new FormData(form);
                inputs.forEach(function (input) {
                    input.removeAttribute('disabled');
                });
                return {
                    method: 'POST',
                    body: formBody,
                    credentials: 'same-origin',
                    headers: {
                        'X-Requested-With': 'XMLHttpRequest'
                    },
                };
            }
            Fetch.postFormOptions = postFormOptions;
            function redirect(onRedirect) {
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
            }
            Fetch.redirect = redirect;
            function doRedirect(response) {
                return __awaiter(this, void 0, void 0, function () {
                    return __generator(this, function (_a) {
                        if (response.status >= 300 && response.status < 400) {
                            window.location.href = response.url;
                            throw new Error('Fetch: fullRedirect to ' + response.url);
                        }
                        return [2 /*return*/, response];
                    });
                });
            }
            Fetch.doRedirect = doRedirect;
            function checkErrorCode(response) {
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
            }
            Fetch.checkErrorCode = checkErrorCode;
            function parseJson(response) {
                return response.json();
            }
            Fetch.parseJson = parseJson;
            function parseText(response) {
                return response.text();
            }
            Fetch.parseText = parseText;
            function submitForm(event, onRedirect) {
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
            }
            Fetch.submitForm = submitForm;
            function submitAjaxForm(form, event) {
                return __awaiter(this, void 0, void 0, function () {
                    var url, init, button, response, contentType;
                    var _a, _b;
                    return __generator(this, function (_c) {
                        switch (_c.label) {
                            case 0:
                                url = form.action;
                                init = Mox.Utils.Fetch.postFormOptions(form);
                                button = form.querySelector('input[type=submit]');
                                button.classList.add('loading');
                                return [4 /*yield*/, fetch(url, init).then(Mox.Utils.Fetch.checkErrorCode)];
                            case 1:
                                response = _c.sent();
                                contentType = response.headers.get('Content-Type');
                                if (!(contentType.indexOf('text/html') > -1)) return [3 /*break*/, 3];
                                _a = { type: 'html' };
                                return [4 /*yield*/, Mox.Utils.Fetch.parseText(response)];
                            case 2: return [2 /*return*/, (_a.data = _c.sent(), _a)];
                            case 3:
                                if (!(contentType.indexOf('application/json') > -1)) return [3 /*break*/, 5];
                                _b = { type: 'json' };
                                return [4 /*yield*/, Mox.Utils.Fetch.parseJson(response)];
                            case 4: return [2 /*return*/, (_b.data = _c.sent(), _b)];
                            case 5: throw new Error('Content-Type: "' + contentType + '" cannot be used when responing to a form post request.');
                        }
                    });
                });
            }
            Fetch.submitAjaxForm = submitAjaxForm;
        })(Fetch = Utils.Fetch || (Utils.Fetch = {}));
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
        var URL;
        (function (URL) {
            function addWindowQueryTo(url, additionalQueries) {
                var urlQuery = URL.splitUrl(url).query;
                var windowQuery = URL.splitUrl(window.location.href).query;
                var newQuery = [urlQuery, windowQuery].concat(additionalQueries || []).filter(function (x) { return !!x; }).join('&');
                var baseUrl = url.split('?')[0];
                return baseUrl + '?' + newQuery;
            }
            URL.addWindowQueryTo = addWindowQueryTo;
            function splitUrl(url) {
                var s = url.split('?');
                if (s.length > 1)
                    return { domainAndPath: s[0], query: s[1] };
                return { domainAndPath: s[0], query: '' };
            }
            URL.splitUrl = splitUrl;
            function queryStringFromObject(object) {
                var params = [];
                for (var key in object) {
                    var value = object[key];
                    if (value === null) {
                        continue;
                    }
                    params.push(key + '=' + encodeURIComponent(value));
                }
                return params.join("&");
            }
            URL.queryStringFromObject = queryStringFromObject;
        })(URL = Utils.URL || (Utils.URL = {}));
    })(Utils = Mox.Utils || (Mox.Utils = {}));
})(Mox || (Mox = {}));
function queryString(params) {
    return Object.keys(params).filter(function (key) { return !(params[key] == null); }).map(function (key) { return encodeURIComponent(key) + '=' + encodeURIComponent(params[key]); }).join('&');
}
function addQueryToUrl(url, queryString) {
    var separator = url.indexOf('?') > -1 ? '&' : '?';
    return "" + url + separator + queryString;
}
function get(url, queryParams) {
    return new Promise(function (resolve, reject) {
        var request = new XMLHttpRequest();
        var _url = url;
        if (queryParams) {
            _url = addQueryToUrl(url, queryString(queryParams));
        }
        request.open("GET", _url, true);
        request.onreadystatechange = function (event) {
            if (request.readyState === 4) {
                if (request.status === 200) {
                    resolve(request.responseText);
                }
                else {
                    reject(request.status);
                }
            }
        };
        request.send();
    });
}
function getJSON(url, queryParams) {
    return __awaiter(this, void 0, void 0, function () {
        var _a, _b;
        return __generator(this, function (_c) {
            switch (_c.label) {
                case 0:
                    _b = (_a = JSON).parse;
                    return [4 /*yield*/, get(url, queryParams)];
                case 1: return [2 /*return*/, _b.apply(_a, [_c.sent()])];
            }
        });
    });
}
function post(url, body, queryParams, additionalHeaders) {
    return __awaiter(this, void 0, void 0, function () {
        var _url, headers, name, response, contentType;
        var _a, _b;
        return __generator(this, function (_c) {
            switch (_c.label) {
                case 0:
                    _url = url;
                    if (queryParams) {
                        _url = addQueryToUrl(url, queryString(queryParams));
                    }
                    headers = {
                        'X-Requested-With': 'XMLHttpRequest'
                    };
                    for (name in additionalHeaders || {}) {
                        headers[name] = additionalHeaders[name];
                    }
                    return [4 /*yield*/, fetch(_url, {
                            method: 'POST',
                            body: body,
                            credentials: 'same-origin',
                            headers: headers
                        }).then(Mox.Utils.Fetch.checkErrorCode)];
                case 1:
                    response = _c.sent();
                    contentType = response.headers.get('Content-Type');
                    if (!!contentType) return [3 /*break*/, 2];
                    return [2 /*return*/, { type: 'html', data: '' }];
                case 2:
                    if (!(contentType.indexOf('text/html') > -1 || contentType.indexOf("text/plain") > -1)) return [3 /*break*/, 4];
                    _a = { type: 'html' };
                    return [4 /*yield*/, Mox.Utils.Fetch.parseText(response)];
                case 3: return [2 /*return*/, (_a.data = _c.sent(), _a)];
                case 4:
                    if (!(contentType.indexOf('application/json') > -1)) return [3 /*break*/, 6];
                    _b = { type: 'json' };
                    return [4 /*yield*/, Mox.Utils.Fetch.parseJson(response)];
                case 5: return [2 /*return*/, (_b.data = _c.sent(), _b)];
                case 6: throw new Error('Content-Type: "' + contentType + '" cannot be used when responing to a form post request.');
            }
        });
    });
}
function getFormData(form, prefix) {
    var fields = Mox.Utils.DOM.nodeListOfToArray(form.querySelectorAll('*[name^="' + prefix + '."]'));
    var stripPrefix = function (name) {
        return name.substr(prefix.length + 1);
    };
    var formData = new FormData();
    for (var _i = 0, fields_1 = fields; _i < fields_1.length; _i++) {
        var field = fields_1[_i];
        var name_1 = stripPrefix(field.name);
        if (field.tagName === 'INPUT' && field.type === 'file') {
            for (var i = 0; i < field.files.length; i++) {
                formData.append(name_1, field.files.item(i));
            }
        }
        else if (field.tagName === 'INPUT' && (field.type === 'checkbox' || field.type === 'radio')) {
            if (field.checked) {
                formData.append(name_1, field.value);
            }
        }
        else {
            formData.append(name_1, field.value);
        }
    }
    return formData;
}
//# sourceMappingURL=utils.js.map