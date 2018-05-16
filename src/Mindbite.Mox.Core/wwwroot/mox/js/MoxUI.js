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
    var UI;
    (function (UI) {
        var MobileMenu = /** @class */ (function () {
            function MobileMenu(openButton, menu, shadowClass, windowWidth) {
                var _this = this;
                this.openButton = openButton;
                this.menu = menu;
                this.windowWidth = windowWidth;
                {
                    var selectedMenuSelector = '.mox-header-menu-container > .mox-menu > li > .selected';
                    var selectedMenuItem = document.querySelector(selectedMenuSelector);
                    var selectedMenuTitle = selectedMenuItem.textContent;
                    var titleElement = document.createElement('h2');
                    titleElement.className = 'mox-header-mobile-app-name';
                    titleElement.innerText = selectedMenuTitle;
                    Mox.Utils.DOM.closest(this.menu, '.mox-header').appendChild(titleElement);
                }
                var shadows = document.getElementsByClassName(shadowClass);
                if (shadows.length > 0) {
                    this.shadow = shadows[0];
                }
                else {
                    var newShadow = document.createElement('div');
                    newShadow.className = shadowClass;
                    this.menu.parentNode.insertBefore(newShadow, this.menu);
                    this.shadow = newShadow;
                }
                this.openButton.addEventListener('click', function (event) {
                    if (window.innerWidth < _this.windowWidth) {
                        _this.openClicked(event);
                        event.stopPropagation();
                    }
                });
                this.shadow.addEventListener('click', function (event) {
                    if (window.innerWidth < _this.windowWidth) {
                        _this.closeClicked(event);
                        event.preventDefault();
                        event.stopPropagation();
                    }
                });
            }
            MobileMenu.initDefault = function () {
                var openButton = document.getElementsByClassName('mox-header')[0];
                var menu = document.getElementsByClassName('mox-header-menu-container')[0];
                return new MobileMenu(openButton, menu, 'mox-menu-background', 960);
            };
            MobileMenu.prototype.openClicked = function (event) {
                this.menu.classList.add('open');
                this.shadow.classList.add('visible');
            };
            MobileMenu.prototype.closeClicked = function (event) {
                this.menu.classList.remove('open');
                this.shadow.classList.remove('visible');
            };
            return MobileMenu;
        }());
        UI.MobileMenu = MobileMenu;
        var Modal = /** @class */ (function () {
            function Modal(options) {
                var _this = this;
                var _options = options || {};
                this.contentContainer = document.createElement('div');
                this.contentContainer.className = _options.contentClassName || 'mox-modal-content';
                this.closeButton = document.createElement('a');
                this.closeButton.className = 'mox-modal-close';
                this.closeButton.href = '#!';
                this.closeButton.addEventListener('click', function (e) { e.preventDefault(); _this.close(); });
                this.contentWrapper = document.createElement('div');
                this.contentWrapper.className = 'mox-modal-wrapper loading';
                this.contentWrapper.appendChild(this.contentContainer);
                this.contentWrapper.appendChild(this.closeButton);
                this.shadow = document.createElement('div');
                this.shadow.className = 'mox-modal-shadow' + (options.noShadow ? 'hidden' : '');
                this.shadow.appendChild(this.contentWrapper);
                this.root = document.createElement('div');
                this.root.className = _options.className || 'mox-modal';
                this.root.appendChild(this.shadow);
                (options.containerElement || document.body).appendChild(this.root);
                this.onCloseCallbacks = [];
                if (!options.dontCloseOnEscape) {
                    this.escapeHandle = CloseOnEscapeQueue.enqueue(function () { return _this.close(); });
                }
            }
            Modal.createDialog = function (url) {
                return __awaiter(this, void 0, void 0, function () {
                    var modal, getInit, response, text;
                    return __generator(this, function (_a) {
                        switch (_a.label) {
                            case 0:
                                modal = new Modal({
                                    className: 'mox-modal mox-dialog',
                                    contentClassName: 'mox-modal-content mox-content'
                                });
                                getInit = {
                                    credentials: 'same-origin',
                                    headers: {
                                        'X-Requested-With': 'XMLHttpRequest'
                                    }
                                };
                                return [4 /*yield*/, fetch(url, getInit).then(Mox.Utils.Fetch.checkErrorCode).then(Mox.Utils.Fetch.redirect(function (url) { window.location.href = url; }))];
                            case 1:
                                response = _a.sent();
                                return [4 /*yield*/, response.text()];
                            case 2:
                                text = _a.sent();
                                modal.contentContainer.innerHTML = text;
                                modal.contentWrapper.classList.remove('loading');
                                document.body.style.overflow = 'hidden';
                                if (!Modal.allOpenModals)
                                    Modal.allOpenModals = [];
                                Modal.allOpenModals.push(modal);
                                return [2 /*return*/, modal];
                        }
                    });
                });
            };
            Modal.createDialogWithContent = function (htmlContent) {
                var modal = new Modal({
                    className: 'mox-modal mox-dialog',
                    contentClassName: 'mox-modal-content mox-content'
                });
                modal.contentContainer.innerHTML = htmlContent;
                setTimeout(function () {
                    modal.contentWrapper.classList.remove('loading');
                }, 10);
                document.body.style.overflow = 'hidden';
                if (!Modal.allOpenModals)
                    Modal.allOpenModals = [];
                Modal.allOpenModals.push(modal);
                return modal;
            };
            Modal.closeAll = function () {
                return __awaiter(this, void 0, void 0, function () {
                    return __generator(this, function (_a) {
                        if (Modal.allOpenModals) {
                            return [2 /*return*/, Promise.all(Modal.allOpenModals.map(function (x) { return x.close(); }))];
                        }
                        return [2 /*return*/, Promise.resolve()];
                    });
                });
            };
            Modal.prototype.close = function () {
                return __awaiter(this, void 0, void 0, function () {
                    var _this = this;
                    return __generator(this, function (_a) {
                        Modal.allOpenModals = Modal.allOpenModals.filter(function (m) { return m != _this; });
                        if (!Modal.allOpenModals.length) {
                            document.body.style.overflow = 'unset';
                        }
                        CloseOnEscapeQueue.remove(this.escapeHandle);
                        return [2 /*return*/, new Promise(function (resolve, reject) {
                                _this.onCloseCallbacks.forEach(function (x) { return x(); });
                                _this.root.classList.add('hidden');
                                setTimeout(function () {
                                    _this.root.remove();
                                    resolve();
                                }, 200);
                            })];
                    });
                });
            };
            Modal.prototype.replaceContent = function (url) {
                return __awaiter(this, void 0, void 0, function () {
                    var getInit, response, text;
                    return __generator(this, function (_a) {
                        switch (_a.label) {
                            case 0:
                                getInit = {
                                    credentials: 'same-origin',
                                    headers: {
                                        'X-Requested-With': 'XMLHttpRequest'
                                    }
                                };
                                this.contentWrapper.classList.add('loading');
                                return [4 /*yield*/, fetch(url, getInit).then(Mox.Utils.Fetch.checkErrorCode).then(Mox.Utils.Fetch.redirect(function (url) { window.location.href = url; }))];
                            case 1:
                                response = _a.sent();
                                return [4 /*yield*/, response.text()];
                            case 2:
                                text = _a.sent();
                                this.contentContainer.innerHTML = text;
                                this.contentWrapper.classList.remove('loading');
                                return [2 /*return*/];
                        }
                    });
                });
            };
            Modal.prototype.onClose = function (callback) {
                this.onCloseCallbacks.push(callback);
            };
            Modal.setupHistory = function (modal, url) {
                var state = { url: url };
                var closeClicked = false;
                var poped = false;
                var prevState = history.state;
                var pop = function (e) {
                    if (!(e.state && prevState && prevState.url === e.state.url) && !(prevState === null && e.state === null)) {
                        e.preventDefault();
                        return;
                    }
                    e.preventDefault();
                    window.removeEventListener('popstate', pop);
                    poped = true;
                    if (!closeClicked) {
                        modal.close();
                    }
                };
                history.pushState(state, 'open modal', url);
                window.addEventListener('popstate', pop);
                modal.onClose(function () {
                    if (!poped) {
                        closeClicked = true;
                        history.back();
                    }
                });
                return modal;
            };
            return Modal;
        }());
        UI.Modal = Modal;
        var ShortcutStackFrame = /** @class */ (function () {
            function ShortcutStackFrame() {
            }
            return ShortcutStackFrame;
        }());
        var CloseOnEscapeQueue = /** @class */ (function () {
            function CloseOnEscapeQueue() {
            }
            CloseOnEscapeQueue.setupEvents = function () {
                var currentlyProcessing = false;
                window.addEventListener('keydown', function (e) {
                    if (e.keyCode == 27 && !currentlyProcessing && CloseOnEscapeQueue.handles.length) {
                        currentlyProcessing = true;
                        var callback = CloseOnEscapeQueue.handles[CloseOnEscapeQueue.handles.length - 1].callback;
                        var promise = callback() || Promise.resolve();
                        promise.then(function () {
                            currentlyProcessing = false;
                        });
                    }
                });
                CloseOnEscapeQueue.isEventsSetup = true;
            };
            CloseOnEscapeQueue.enqueue = function (callback) {
                if (!CloseOnEscapeQueue.isEventsSetup)
                    CloseOnEscapeQueue.setupEvents();
                var handle = ++CloseOnEscapeQueue.lastHandle;
                CloseOnEscapeQueue.handles.push({ handle: handle, callback: callback });
                return handle;
            };
            CloseOnEscapeQueue.remove = function (handle) {
                CloseOnEscapeQueue.handles = CloseOnEscapeQueue.handles.filter(function (x) { return x.handle != handle; });
            };
            CloseOnEscapeQueue.isEventsSetup = false;
            CloseOnEscapeQueue.lastHandle = 0;
            CloseOnEscapeQueue.handles = [];
            return CloseOnEscapeQueue;
        }());
        UI.CloseOnEscapeQueue = CloseOnEscapeQueue;
        var GlobalInstances = /** @class */ (function () {
            function GlobalInstances() {
            }
            GlobalInstances.initDefault = function () {
                GlobalInstances.mobileMenu = MobileMenu.initDefault();
            };
            return GlobalInstances;
        }());
        UI.GlobalInstances = GlobalInstances;
    })(UI = Mox.UI || (Mox.UI = {}));
})(Mox || (Mox = {}));
//# sourceMappingURL=MoxUI.js.map