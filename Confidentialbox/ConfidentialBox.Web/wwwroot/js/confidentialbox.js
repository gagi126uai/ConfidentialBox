const sessions = new Map();
const pdfFrames = new Map();

const globalGuards = {
    printStyleElement: null,
    originalPrint: null,
    printOverrideActive: false
};

function anySession(predicate) {
    for (const state of sessions.values()) {
        if (predicate(state)) {
            return true;
        }
    }

    return false;
}

function broadcastEventTo(predicate, callback) {
    for (const state of sessions.values()) {
        if (predicate(state)) {
            callback(state);
        }
    }
}

function ensurePrintBlockedStyles() {
    if (typeof document === 'undefined') {
        return;
    }

    if (!globalGuards.printStyleElement) {
        const style = document.createElement('style');
        style.id = 'secure-print-blocker';
        style.textContent = `@media print {
            body * {
                visibility: hidden !important;
            }

            body::before {
                visibility: visible !important;
                display: block !important;
                content: 'Impresión bloqueada por la política de ConfidentialBox';
                font-size: 22px !important;
                font-weight: 600 !important;
                text-align: center !important;
                margin-top: 30vh !important;
                color: #b91c1c !important;
                font-family: 'Inter', 'Segoe UI', sans-serif !important;
            }
        }`;

        document.head.appendChild(style);
        globalGuards.printStyleElement = style;
    }
}

function removePrintBlockedStyles() {
    if (globalGuards.printStyleElement && globalGuards.printStyleElement.parentElement) {
        globalGuards.printStyleElement.parentElement.removeChild(globalGuards.printStyleElement);
    }

    globalGuards.printStyleElement = null;
}

function overrideWindowPrint() {
    if (typeof window === 'undefined') {
        return;
    }

    if (globalGuards.printOverrideActive) {
        return;
    }

    const originalPrint = typeof window.print === 'function'
        ? window.print.bind(window)
        : null;

    window.print = function securePrintOverride() {
        let blocked = false;
        broadcastEventTo((state) => !state.permissions?.printAllowed, (state) => {
            blocked = true;
            reportPrintAttempt(state);
        });

        if (blocked) {
            return;
        }

        if (originalPrint) {
            return originalPrint();
        }

        return undefined;
    };

    globalGuards.originalPrint = originalPrint;
    globalGuards.printOverrideActive = true;
}

function restoreWindowPrint() {
    if (typeof window === 'undefined') {
        return;
    }

    if (!globalGuards.printOverrideActive) {
        return;
    }

    if (globalGuards.originalPrint) {
        window.print = globalGuards.originalPrint;
    } else {
        delete window.print;
    }

    globalGuards.originalPrint = null;
    globalGuards.printOverrideActive = false;
}

function updateGlobalPolicyGuards() {
    const requiresPrintBlock = anySession((state) => !state.permissions?.printAllowed);

    if (requiresPrintBlock) {
        ensurePrintBlockedStyles();
        overrideWindowPrint();
    } else {
        removePrintBlockedStyles();
        restoreWindowPrint();
    }
}

async function resolveGeoMetadata() {
    try {
        const response = await fetch('https://ipapi.co/json/', { cache: 'no-store' });
        if (!response.ok) {
            throw new Error('ipapi.co unavailable');
        }

        const data = await response.json();
        return {
            ip: data.ip || null,
            location: data.city && data.country_name
                ? `${data.city}, ${data.country_name}`
                : data.country_name || null,
            latitude: typeof data.latitude === 'number' ? data.latitude : null,
            longitude: typeof data.longitude === 'number' ? data.longitude : null
        };
    } catch (error) {
        console.warn('No se pudo obtener información geográfica', error);
    }

    try {
        const ipifyResponse = await fetch('https://api.ipify.org?format=json', { cache: 'no-store' });
        if (!ipifyResponse.ok) {
            return null;
        }

        const { ip } = await ipifyResponse.json();
        if (!ip) {
            return null;
        }

        let fallbackLocation = null;
        let latitude = null;
        let longitude = null;

        try {
            const ipWhoResponse = await fetch(`https://ipwho.is/${ip}`, { cache: 'no-store' });
            if (ipWhoResponse.ok) {
                const geo = await ipWhoResponse.json();
                fallbackLocation = geo.city && geo.country
                    ? `${geo.city}, ${geo.country}`
                    : geo.country || null;
                latitude = typeof geo.latitude === 'number' ? geo.latitude : null;
                longitude = typeof geo.longitude === 'number' ? geo.longitude : null;
            }
        } catch (lookupError) {
            console.warn('No se pudo resolver ubicación por IP pública', lookupError);
        }

        return {
            ip: ip,
            location: fallbackLocation,
            latitude: latitude,
            longitude: longitude
        };
    } catch (error) {
        console.warn('No se pudo obtener IP pública', error);
    }

    return null;
}

async function collectClientContext() {
    const [{
        userAgent,
        platform
    }, geo] = await Promise.all([
        Promise.resolve({
            userAgent: navigator.userAgent || null,
            platform: navigator.platform || null,
            deviceMemory: navigator.deviceMemory || null
        }),
        resolveGeoMetadata()
    ]);

    const timezone = (() => {
        try {
            return Intl.DateTimeFormat().resolvedOptions().timeZone || null;
        } catch {
            return null;
        }
    })();

    const deviceType = (() => {
        const ua = (navigator.userAgent || '').toLowerCase();
        if (/tablet|ipad/.test(ua)) {
            return 'Tablet';
        }
        if (/mobile|iphone|android/.test(ua)) {
            return 'Mobile';
        }
        return 'Desktop';
    })();

    const browserName = (() => {
        if (navigator.userAgentData?.brands?.length) {
            return navigator.userAgentData.brands.map(b => b.brand).join(' / ');
        }

        const ua = navigator.userAgent || '';
        if (ua.includes('Edg/')) return 'Microsoft Edge';
        if (ua.includes('OPR/') || ua.includes('Opera')) return 'Opera';
        if (ua.includes('Chrome/')) return 'Google Chrome';
        if (ua.includes('Firefox/')) return 'Mozilla Firefox';
        if (ua.includes('Safari/')) return 'Safari';
        return null;
    })();

    return {
        ipAddress: geo?.ip ?? null,
        userAgent: userAgent,
        deviceName: navigator.userAgentData?.platform || platform || null,
        deviceType: deviceType,
        operatingSystem: navigator.userAgentData?.platform || platform || null,
        browser: browserName,
        location: geo?.location ?? null,
        latitude: geo?.latitude ?? null,
        longitude: geo?.longitude ?? null,
        timeZone: timezone
    };
}

function forEachSessionByFrame(frameId, callback) {
    if (!frameId) {
        return;
    }

    for (const state of sessions.values()) {
        if (state.frameId === frameId) {
            callback(state);
        }
    }
}

function frameRequiresContextMenuBlock(frameId) {
    if (!frameId) {
        return false;
    }

    for (const state of sessions.values()) {
        if (state.frameId === frameId && state.disableContextMenu) {
            return true;
        }
    }

    return false;
}

function frameAllowsTextSelection(frameId) {
    if (!frameId) {
        return false;
    }

    let allow = false;
    forEachSessionByFrame(frameId, (state) => {
        if (!state?.permissions) {
            return;
        }

        if (state.permissions.copyAllowed && !state.permissions.textSelectionBlocked) {
            allow = true;
        }
    });

    return allow;
}

const PDF_JS_CDN_VERSION = '3.11.174';
const PDF_JS_MODULE_URL = `https://cdn.jsdelivr.net/npm/pdfjs-dist@${PDF_JS_CDN_VERSION}/build/pdf.mjs`;
const PDF_JS_WORKER_URL = `https://cdn.jsdelivr.net/npm/pdfjs-dist@${PDF_JS_CDN_VERSION}/build/pdf.worker.min.js`;

const defaultViewerSettings = {
    theme: 'dark',
    accentColor: '#f97316',
    backgroundColor: '#0f172a',
    toolbarBackgroundColor: '#111827',
    toolbarTextColor: '#f9fafb',
    fontFamily: "'Inter', 'Segoe UI', sans-serif",
    showToolbar: true,
    toolbarPosition: 'top',
    showFileDetails: true,
    showSearch: true,
    showPageControls: true,
    showPageIndicator: true,
    showDownloadButton: false,
    showPrintButton: false,
    showFullscreenButton: true,
    allowDownload: false,
    allowPrint: false,
    allowCopy: false,
    disableTextSelection: true,
    disableContextMenu: true,
    forceGlobalWatermark: false,
    globalWatermarkText: 'CONFIDENTIAL',
    watermarkOpacity: 0.12,
    watermarkFontSize: 48,
    watermarkColor: 'rgba(220,53,69,0.18)',
    maxViewTimeMinutes: 0,
    defaultZoomPercent: 110,
    zoomStepPercent: 15,
    viewerPadding: '1.5rem',
    customCss: ''
};

const defaultViewerPermissions = {
    toolbarVisible: defaultViewerSettings.showToolbar,
    fileDetailsVisible: defaultViewerSettings.showFileDetails,
    searchEnabled: defaultViewerSettings.showSearch,
    zoomControlsEnabled: defaultViewerSettings.showPageControls,
    pageIndicatorEnabled: defaultViewerSettings.showPageIndicator,
    downloadButtonVisible: defaultViewerSettings.showDownloadButton && defaultViewerSettings.allowDownload,
    printButtonVisible: defaultViewerSettings.showPrintButton && defaultViewerSettings.allowPrint,
    fullscreenButtonVisible: defaultViewerSettings.showFullscreenButton,
    downloadAllowed: defaultViewerSettings.allowDownload,
    printAllowed: defaultViewerSettings.allowPrint,
    copyAllowed: defaultViewerSettings.allowCopy,
    contextMenuBlocked: defaultViewerSettings.disableContextMenu,
    textSelectionBlocked: defaultViewerSettings.disableTextSelection,
    watermarkForced: defaultViewerSettings.forceGlobalWatermark,
    defaultZoomPercent: defaultViewerSettings.defaultZoomPercent,
    zoomStepPercent: defaultViewerSettings.zoomStepPercent,
    maxViewTimeMinutes: defaultViewerSettings.maxViewTimeMinutes
};

let pdfjsLibPromise = null;

async function ensurePdfJsLibrary() {
    if (typeof window !== 'undefined' && window.pdfjsLib) {
        const lib = window.pdfjsLib;
        if (lib.GlobalWorkerOptions && !lib.GlobalWorkerOptions.workerSrc) {
            lib.GlobalWorkerOptions.workerSrc = window.pdfjsWorkerSrc || PDF_JS_WORKER_URL;
        }
        return lib;
    }

    if (!pdfjsLibPromise) {
        pdfjsLibPromise = import(PDF_JS_MODULE_URL)
            .then((module) => {
                const lib = module && module.getDocument ? module : module.default || module;
                if (!lib || typeof lib.getDocument !== 'function') {
                    throw new Error('La librería pdf.js no está disponible.');
                }

                if (lib.GlobalWorkerOptions) {
                    lib.GlobalWorkerOptions.workerSrc = PDF_JS_WORKER_URL;
                }

                if (typeof window !== 'undefined' && !window.pdfjsLib) {
                    window.pdfjsLib = lib;
                    window.pdfjsWorkerSrc = PDF_JS_WORKER_URL;
                }

                return lib;
            })
            .catch((err) => {
                console.error('No se pudo cargar pdf.js desde el CDN configurado', err);
                if (typeof window !== 'undefined' && window.pdfjsLib) {
                    return window.pdfjsLib;
                }
                throw err;
            });
    }

    return pdfjsLibPromise;
}

function base64ToUint8Array(base64) {
    const binary = atob(base64);
    const length = binary.length;
    const bytes = new Uint8Array(length);

    for (let i = 0; i < length; i++) {
        bytes[i] = binary.charCodeAt(i);
    }

    return bytes;
}

function sendEvent(state, type, pageNumber, data) {
    if (!state || !state.dotNetRef) {
        return;
    }

    try {
        state.dotNetRef.invokeMethodAsync('ReportViewerEvent', type, pageNumber ?? null, data ? JSON.stringify(data) : null);
    } catch (err) {
        console.error('Error enviando evento al servidor', err);
    }
}

function reportPrintAttempt(state) {
    if (!state) {
        return;
    }

    const now = Date.now();
    if (now - (state.lastPrintAttemptAt || 0) < 400) {
        return;
    }

    state.lastPrintAttemptAt = now;
    sendEvent(state, 'PrintAttempt');
}

function reportDownloadAttempt(state) {
    if (!state) {
        return;
    }

    const now = Date.now();
    if (now - (state.lastDownloadAttemptAt || 0) < 400) {
        return;
    }

    state.lastDownloadAttemptAt = now;
    sendEvent(state, 'DownloadAttempt');
}

function ensureDocumentReady(state, frameState) {
    if (!state || state.documentReadyReported) {
        return;
    }

    if (!frameState || !frameState.pdfDoc) {
        state.awaitingFrame = true;
        return;
    }

    const pageCount = typeof frameState.pdfDoc.numPages === 'number'
        ? frameState.pdfDoc.numPages
        : undefined;

    state.awaitingFrame = false;
    state.documentReadyReported = true;
    const payload = pageCount != null ? { pageCount } : undefined;
    sendEvent(state, 'DocumentReady', null, payload);
}

function registerHandler(state, target, eventName, handler, options) {
    target.addEventListener(eventName, handler, options);
    state.handlers.push({ target, eventName, handler, options });
}

function registerTrackingHandler(state, target, eventName, handler, options) {
    target.addEventListener(eventName, handler, options);
    const record = { target, eventName, handler, options };
    state.handlers.push(record);
    state.trackingHandlers.push(record);
}

function cleanupPageTracking(state) {
    if (!state.trackingHandlers) {
        return;
    }

    for (const entry of state.trackingHandlers) {
        try {
            if (entry.dispose) {
                entry.dispose();
                continue;
            }

            entry.target.removeEventListener(entry.eventName, entry.handler, entry.options);
        } catch (err) {
            console.warn('No se pudo eliminar un listener del visor seguro', err);
        }
    }

    state.trackingHandlers.length = 0;

    if (state.pageIndicator) {
        updatePageIndicator(state, null);
    }

    state.currentPage = null;
}

function buildWatermark(container, text, style) {
    if (!text) {
        return null;
    }

    const watermark = document.createElement('div');
    watermark.className = 'secure-pdf-watermark';
    watermark.setAttribute('aria-hidden', 'true');

    const fragment = document.createDocumentFragment();
    const rows = 5;
    const columns = 3;
    for (let row = 0; row < rows; row++) {
        for (let col = 0; col < columns; col++) {
            const tile = document.createElement('span');
            tile.className = 'secure-pdf-watermark__tile';
            tile.textContent = text;
            fragment.appendChild(tile);
        }
    }

    watermark.appendChild(fragment);

    if (style && typeof style === 'object') {
        if (style.color) {
            watermark.style.setProperty('--secure-watermark-color', style.color);
        }

        if (typeof style.opacity === 'number') {
            watermark.style.setProperty('--secure-watermark-opacity', `${Math.min(Math.max(style.opacity, 0), 1)}`);
        }

        if (style.fontSize) {
            const size = typeof style.fontSize === 'number' ? `${style.fontSize}px` : style.fontSize;
            watermark.style.setProperty('--secure-watermark-size', size);
        }
    }

    container.appendChild(watermark);
    return watermark;
}

function getMostVisiblePage(frameElement, pages) {
    if (!pages || pages.length === 0) {
        return null;
    }

    const frameRect = frameElement.getBoundingClientRect();
    let bestPage = null;
    let bestVisibility = 0;

    for (const page of pages) {
        const rect = page.element.getBoundingClientRect();
        const intersectionTop = Math.max(rect.top, frameRect.top);
        const intersectionBottom = Math.min(rect.bottom, frameRect.bottom);
        const visibleHeight = Math.max(0, intersectionBottom - intersectionTop);
        const ratio = visibleHeight / rect.height;

        if (ratio > bestVisibility) {
            bestVisibility = ratio;
            bestPage = page.pageNumber;
        }
    }

    return bestPage;
}

function ensurePageTracking(state) {
    if (!state.frameId) {
        return;
    }

    const frameState = pdfFrames.get(state.frameId);
    if (!frameState || !frameState.container) {
        state.awaitingFrame = true;
        return;
    }

    state.awaitingFrame = false;

    cleanupPageTracking(state);

    const frameElement = frameState.viewport || frameState.container;
    const scheduleQueue = { scheduled: false };

    const evaluateVisiblePage = () => {
        scheduleQueue.scheduled = false;
        const nextPage = getMostVisiblePage(frameElement, frameState.pages);

        if (!nextPage || nextPage === state.lastReportedPage) {
            return;
        }

        state.lastReportedPage = nextPage;
        state.currentPage = nextPage;
        updatePageIndicator(state, nextPage);

        if (state.hasInitialPageReport) {
            sendEvent(state, 'PageView', nextPage, { pageNumber: nextPage });
        }
    };

    const scheduleEvaluation = () => {
        if (scheduleQueue.scheduled) {
            return;
        }

        scheduleQueue.scheduled = true;
        requestAnimationFrame(evaluateVisiblePage);
    };

    const cleanupToken = { dispose: () => { scheduleQueue.scheduled = false; } };
    state.trackingHandlers.push(cleanupToken);
    state.handlers.push(cleanupToken);

    registerTrackingHandler(state, frameElement, 'scroll', scheduleEvaluation);
    registerTrackingHandler(state, frameElement, 'touchmove', scheduleEvaluation, { passive: true });
    registerTrackingHandler(state, window, 'resize', scheduleEvaluation);

    requestAnimationFrame(evaluateVisiblePage);
}

function ensureTrackingForFrame(frameId) {
    const frameState = pdfFrames.get(frameId);
    for (const state of sessions.values()) {
        if (state.frameId === frameId) {
            if (frameState && state.pendingZoom != null) {
                updateZoom(state, frameState, state.pendingZoom, { silent: true });
                state.pendingZoom = null;
            } else if (frameState) {
                applyZoomFromState(state, frameState);
            }
            ensurePageTracking(state);
        }
    }
}

function toCamelCaseKey(key) {
    if (!key || typeof key !== 'string') {
        return key;
    }

    return key.length === 1 ? key.toLowerCase() : key.charAt(0).toLowerCase() + key.slice(1);
}

function coerceBoolean(value, fallback) {
    if (typeof value === 'boolean') {
        return value;
    }

    if (typeof value === 'string') {
        const lowered = value.trim().toLowerCase();
        if (lowered === 'true') {
            return true;
        }
        if (lowered === 'false') {
            return false;
        }
    }

    return fallback;
}

function coerceNumber(value, fallback) {
    if (typeof value === 'number' && Number.isFinite(value)) {
        return value;
    }

    if (typeof value === 'string') {
        const parsed = Number(value);
        if (Number.isFinite(parsed)) {
            return parsed;
        }
    }

    return fallback;
}

function coerceString(value, fallback) {
    if (typeof value === 'string') {
        const trimmed = value.trim();
        if (trimmed.length > 0) {
            return trimmed;
        }
    }

    return fallback;
}

function normalizeViewerSettings(raw) {
    const normalized = { ...defaultViewerSettings };

    if (raw && typeof raw === 'object') {
        for (const [key, value] of Object.entries(raw)) {
            if (value === undefined || value === null) {
                continue;
            }

            const camelKey = toCamelCaseKey(key);
            normalized[camelKey] = value;
        }
    }

    normalized.theme = coerceString(normalized.theme, defaultViewerSettings.theme).toLowerCase();
    normalized.toolbarPosition = normalized.toolbarPosition === 'bottom' ? 'bottom' : 'top';
    normalized.viewerPadding = coerceString(normalized.viewerPadding, defaultViewerSettings.viewerPadding);
    normalized.fontFamily = coerceString(normalized.fontFamily, defaultViewerSettings.fontFamily);
    normalized.customCss = coerceString(normalized.customCss, defaultViewerSettings.customCss);
    normalized.accentColor = coerceString(normalized.accentColor, defaultViewerSettings.accentColor);
    normalized.backgroundColor = coerceString(normalized.backgroundColor, defaultViewerSettings.backgroundColor);
    normalized.toolbarBackgroundColor = coerceString(normalized.toolbarBackgroundColor, defaultViewerSettings.toolbarBackgroundColor);
    normalized.toolbarTextColor = coerceString(normalized.toolbarTextColor, defaultViewerSettings.toolbarTextColor);
    normalized.globalWatermarkText = coerceString(normalized.globalWatermarkText, defaultViewerSettings.globalWatermarkText);
    normalized.watermarkColor = coerceString(normalized.watermarkColor, defaultViewerSettings.watermarkColor);

    normalized.showToolbar = coerceBoolean(normalized.showToolbar, defaultViewerSettings.showToolbar);
    normalized.showSearch = coerceBoolean(normalized.showSearch, defaultViewerSettings.showSearch);
    normalized.showPageControls = coerceBoolean(normalized.showPageControls, defaultViewerSettings.showPageControls);
    normalized.showPageIndicator = coerceBoolean(normalized.showPageIndicator, defaultViewerSettings.showPageIndicator);
    normalized.showDownloadButton = coerceBoolean(normalized.showDownloadButton, defaultViewerSettings.showDownloadButton);
    normalized.showPrintButton = coerceBoolean(normalized.showPrintButton, defaultViewerSettings.showPrintButton);
    normalized.showFullscreenButton = coerceBoolean(normalized.showFullscreenButton, defaultViewerSettings.showFullscreenButton);
    normalized.allowDownload = coerceBoolean(normalized.allowDownload, defaultViewerSettings.allowDownload);
    normalized.allowPrint = coerceBoolean(normalized.allowPrint, defaultViewerSettings.allowPrint);
    normalized.allowCopy = coerceBoolean(normalized.allowCopy, defaultViewerSettings.allowCopy);
    normalized.disableTextSelection = coerceBoolean(normalized.disableTextSelection, defaultViewerSettings.disableTextSelection);
    normalized.disableContextMenu = coerceBoolean(normalized.disableContextMenu, defaultViewerSettings.disableContextMenu);
    normalized.forceGlobalWatermark = coerceBoolean(normalized.forceGlobalWatermark, defaultViewerSettings.forceGlobalWatermark);
    normalized.showFileDetails = coerceBoolean(normalized.showFileDetails, defaultViewerSettings.showFileDetails);

    const defaultZoom = coerceNumber(normalized.defaultZoomPercent, defaultViewerSettings.defaultZoomPercent);
    normalized.defaultZoomPercent = Math.min(Math.max(defaultZoom, 25), 400);

    const zoomStep = coerceNumber(normalized.zoomStepPercent, defaultViewerSettings.zoomStepPercent);
    normalized.zoomStepPercent = Math.min(Math.max(zoomStep, 5), 50);

    const watermarkFontSize = coerceNumber(normalized.watermarkFontSize, defaultViewerSettings.watermarkFontSize);
    normalized.watermarkFontSize = Math.min(Math.max(watermarkFontSize, 16), 120);

    const watermarkOpacity = coerceNumber(normalized.watermarkOpacity, defaultViewerSettings.watermarkOpacity);
    normalized.watermarkOpacity = Math.min(Math.max(watermarkOpacity, 0), 1);

    const maxViewTime = coerceNumber(normalized.maxViewTimeMinutes, defaultViewerSettings.maxViewTimeMinutes);
    normalized.maxViewTimeMinutes = Math.max(0, Math.round(maxViewTime));

    return normalized;
}

function normalizeViewerPermissions(raw, settings) {
    const normalized = { ...defaultViewerPermissions };

    if (settings && typeof settings === 'object') {
        normalized.toolbarVisible = coerceBoolean(settings.showToolbar, normalized.toolbarVisible);
        normalized.fileDetailsVisible = coerceBoolean(settings.showFileDetails, normalized.fileDetailsVisible);
        normalized.searchEnabled = normalized.toolbarVisible && coerceBoolean(settings.showSearch, normalized.searchEnabled);
        normalized.zoomControlsEnabled = normalized.toolbarVisible && coerceBoolean(settings.showPageControls, normalized.zoomControlsEnabled);
        normalized.pageIndicatorEnabled = normalized.toolbarVisible && coerceBoolean(settings.showPageIndicator, normalized.pageIndicatorEnabled);
        const downloadAllowed = coerceBoolean(settings.allowDownload, normalized.downloadAllowed);
        const printAllowed = coerceBoolean(settings.allowPrint, normalized.printAllowed);
        const copyAllowed = coerceBoolean(settings.allowCopy, normalized.copyAllowed);
        normalized.downloadAllowed = downloadAllowed;
        normalized.printAllowed = printAllowed;
        normalized.copyAllowed = copyAllowed;
        normalized.downloadButtonVisible = downloadAllowed && coerceBoolean(settings.showDownloadButton, normalized.downloadButtonVisible);
        normalized.printButtonVisible = printAllowed && coerceBoolean(settings.showPrintButton, normalized.printButtonVisible);
        normalized.fullscreenButtonVisible = coerceBoolean(settings.showFullscreenButton, normalized.fullscreenButtonVisible);
        normalized.contextMenuBlocked = coerceBoolean(settings.disableContextMenu, normalized.contextMenuBlocked);
        normalized.textSelectionBlocked = coerceBoolean(settings.disableTextSelection, normalized.textSelectionBlocked) || !copyAllowed;
        normalized.watermarkForced = coerceBoolean(settings.forceGlobalWatermark, normalized.watermarkForced);
        normalized.defaultZoomPercent = coerceNumber(settings.defaultZoomPercent, normalized.defaultZoomPercent);
        normalized.zoomStepPercent = coerceNumber(settings.zoomStepPercent, normalized.zoomStepPercent);
        normalized.maxViewTimeMinutes = coerceNumber(settings.maxViewTimeMinutes, normalized.maxViewTimeMinutes);
    }

    if (raw && typeof raw === 'object') {
        for (const [key, value] of Object.entries(raw)) {
            if (value === undefined || value === null) {
                continue;
            }

            const camelKey = toCamelCaseKey(key);
            switch (camelKey) {
                case 'toolbarVisible':
                case 'fileDetailsVisible':
                case 'searchEnabled':
                case 'zoomControlsEnabled':
                case 'pageIndicatorEnabled':
                case 'downloadButtonVisible':
                case 'printButtonVisible':
                case 'fullscreenButtonVisible':
                case 'downloadAllowed':
                case 'printAllowed':
                case 'copyAllowed':
                case 'contextMenuBlocked':
                case 'textSelectionBlocked':
                case 'watermarkForced':
                    normalized[camelKey] = coerceBoolean(value, normalized[camelKey]);
                    break;
                case 'defaultZoomPercent':
                case 'zoomStepPercent':
                case 'maxViewTimeMinutes':
                    normalized[camelKey] = coerceNumber(value, normalized[camelKey]);
                    break;
                default:
                    break;
            }
        }
    }

    if (!normalized.toolbarVisible) {
        normalized.searchEnabled = false;
        normalized.zoomControlsEnabled = false;
        normalized.pageIndicatorEnabled = false;
    }

    if (!normalized.downloadAllowed) {
        normalized.downloadButtonVisible = false;
    }

    if (!normalized.printAllowed) {
        normalized.printButtonVisible = false;
    }

    if (!normalized.copyAllowed) {
        normalized.textSelectionBlocked = true;
    }

    normalized.defaultZoomPercent = Math.min(Math.max(normalized.defaultZoomPercent, 25), 400);
    normalized.zoomStepPercent = Math.min(Math.max(normalized.zoomStepPercent, 5), 50);
    normalized.maxViewTimeMinutes = Math.max(0, Math.round(normalized.maxViewTimeMinutes || 0));

    return normalized;
}

function applyViewerTheme(container, settings) {
    if (!container) {
        return;
    }

    container.style.setProperty('--secure-viewer-accent', settings.accentColor);
    container.style.setProperty('--secure-viewer-background', settings.backgroundColor);
    container.style.setProperty('--secure-viewer-toolbar-bg', settings.toolbarBackgroundColor);
    container.style.setProperty('--secure-viewer-toolbar-text', settings.toolbarTextColor);
    container.style.setProperty('--secure-viewer-font-family', settings.fontFamily);
    container.style.setProperty('--secure-viewer-padding', settings.viewerPadding);
    container.dataset.secureTheme = settings.theme;
    container.dataset.toolbarPosition = settings.toolbarPosition;
}

function clearViewerTheme(container) {
    if (!container) {
        return;
    }

    container.removeAttribute('data-secure-theme');
    container.removeAttribute('data-toolbar-position');
    container.style.removeProperty('--secure-viewer-accent');
    container.style.removeProperty('--secure-viewer-background');
    container.style.removeProperty('--secure-viewer-toolbar-bg');
    container.style.removeProperty('--secure-viewer-toolbar-text');
    container.style.removeProperty('--secure-viewer-font-family');
    container.style.removeProperty('--secure-viewer-padding');
}

function toggleSelection(container, disabled) {
    if (!container) {
        return;
    }

    if (disabled) {
        container.classList.add('secure-pdf-no-select');
    } else {
        container.classList.remove('secure-pdf-no-select');
    }
}

function updateContainerPolicyAttributes(container, permissions) {
    if (!container || !permissions) {
        return;
    }

    container.dataset.allowDownload = permissions.downloadAllowed ? 'true' : 'false';
    container.dataset.allowPrint = permissions.printAllowed ? 'true' : 'false';
    container.dataset.allowCopy = permissions.copyAllowed ? 'true' : 'false';
    container.dataset.contextMenuBlocked = permissions.contextMenuBlocked ? 'true' : 'false';
    container.dataset.textSelectionBlocked = permissions.textSelectionBlocked ? 'true' : 'false';
    container.dataset.toolbarVisible = permissions.toolbarVisible ? 'true' : 'false';
}

function clearContainerPolicyAttributes(container) {
    if (!container) {
        return;
    }

    delete container.dataset.allowDownload;
    delete container.dataset.allowPrint;
    delete container.dataset.allowCopy;
    delete container.dataset.contextMenuBlocked;
    delete container.dataset.textSelectionBlocked;
    delete container.dataset.toolbarVisible;
}

function clearFrameTextLayers(frameState) {
    if (!frameState?.pages) {
        return;
    }

    for (const pageView of frameState.pages) {
        if (pageView.renderTask && typeof pageView.renderTask.cancel === 'function') {
            try { pageView.renderTask.cancel(); } catch { /* noop */ }
        }

        if (pageView.textLayerRender && typeof pageView.textLayerRender.cancel === 'function') {
            try { pageView.textLayerRender.cancel(); } catch { /* noop */ }
        }

        if (pageView.textLayerDiv) {
            try { pageView.textLayerDiv.remove(); } catch { /* noop */ }
            pageView.textLayerDiv = null;
        }

        pageView.textLayerRender = null;
        pageView.textDivs = null;
        pageView.textContentPromise = null;
        pageView.searchTextPromise = null;
    }
}

function applySelectionState(state) {
    if (!state) {
        return;
    }

    const frameState = pdfFrames.get(state.frameId);
    if (!frameState) {
        return;
    }

    const allowSelection = state.permissions?.copyAllowed && !state.permissions?.textSelectionBlocked;
    frameState.allowTextSelection = allowSelection;

    if (!allowSelection) {
        clearFrameTextLayers(frameState);
        clearSelectionWithin(state.container || frameState.container);
        return;
    }

    scheduleRenderAllPages(frameState);
}

function clearSelectionWithin(container, targetDocument = document) {
    if (!container || !targetDocument) {
        return;
    }

    const selection = typeof targetDocument.getSelection === 'function'
        ? targetDocument.getSelection()
        : window.getSelection();
    if (!selection || selection.isCollapsed) {
        return;
    }

    const anchorNode = selection.anchorNode;
    if (!anchorNode) {
        return;
    }

    if (container.contains(anchorNode)) {
        selection.removeAllRanges();
    }
}

function notifyScreenshotAttempt(state, trigger) {
    sendEvent(state, 'ScreenshotAttempt', null, { trigger });
}

function updatePageIndicator(state, pageNumber) {
    if (!state || !state.pageIndicator) {
        return;
    }

    if (typeof pageNumber === 'number' && pageNumber > 0) {
        state.pageIndicator.textContent = `Pág. ${pageNumber}`;
    } else {
        state.pageIndicator.textContent = 'Pág. --';
    }
}

function createToolbarButton(iconClass, title, onClick) {
    const button = document.createElement('button');
    button.type = 'button';
    button.className = 'secure-toolbar-button';
    button.title = title;

    const icon = document.createElement('i');
    icon.className = iconClass;
    button.appendChild(icon);

    button.addEventListener('click', (event) => {
        event.preventDefault();
        onClick();
    });

    return button;
}

function applyZoomFromState(state, frameState) {
    if (!state || !frameState) {
        return;
    }

    const zoom = state.currentZoom ?? state.defaultZoom ?? 1;
    updateZoom(state, frameState, zoom, { silent: true });
}

async function renderPageView(frameState, pageView, scale) {
    if (!frameState?.pdfDoc || !pageView) {
        return;
    }

    if (pageView.renderTask && typeof pageView.renderTask.cancel === 'function') {
        try {
            pageView.renderTask.cancel();
        } catch { /* noop */ }
    }

    if (pageView.textLayerRender && typeof pageView.textLayerRender.cancel === 'function') {
        try {
            pageView.textLayerRender.cancel();
        } catch { /* noop */ }
    }

    try {
        const pdfPage = await frameState.pdfDoc.getPage(pageView.pageNumber);
        const viewport = pdfPage.getViewport({ scale });
        const outputScale = window.devicePixelRatio || 1;
        const renderViewport = pdfPage.getViewport({ scale: scale * outputScale });

        const canvas = pageView.canvas;
        const context = canvas.getContext('2d', { alpha: false });
        if (!context) {
            console.warn('No se pudo obtener el contexto del lienzo del visor seguro');
            return;
        }

        canvas.width = Math.floor(renderViewport.width);
        canvas.height = Math.floor(renderViewport.height);
        canvas.style.width = `${Math.floor(viewport.width)}px`;
        canvas.style.height = `${Math.floor(viewport.height)}px`;
        canvas.style.maxWidth = '100%';
        pageView.element.style.width = '100%';
        pageView.element.style.height = 'auto';

        const renderTask = pdfPage.render({
            canvasContext: context,
            viewport: renderViewport
        });

        pageView.renderTask = renderTask;
        await renderTask.promise;
        pageView.viewport = viewport;

        const allowTextSelection = frameState.allowTextSelection === true;

        if (allowTextSelection) {
            if (!pageView.textLayerDiv) {
                const layer = document.createElement('div');
                layer.className = 'secure-pdf-text-layer';
                pageView.element.appendChild(layer);
                pageView.textLayerDiv = layer;
            }

            const textContentPromise = pageView.textContentPromise
                ?? pdfPage.getTextContent({ normalizeWhitespace: true });
            pageView.textContentPromise = textContentPromise;

            const textContent = await textContentPromise.catch((err) => {
                console.warn('No se pudo obtener el contenido de texto seguro', err);
                return null;
            });

            if (textContent && frameState.pdfjsLib?.renderTextLayer) {
                pageView.textLayerDiv.innerHTML = '';
                const textDivs = [];
                const textLayerRender = frameState.pdfjsLib.renderTextLayer({
                    textContent,
                    container: pageView.textLayerDiv,
                    viewport,
                    textDivs,
                    enhanceTextSelection: true
                });

                pageView.textDivs = textDivs;
                pageView.textLayerRender = textLayerRender;

                try {
                    await textLayerRender.promise;
                } catch (err) {
                    if (err?.name !== 'RenderingCancelledException') {
                        console.warn('No se pudo renderizar la capa de texto segura', err);
                    }
                }
            }
        } else {
            if (pageView.textLayerDiv) {
                pageView.textLayerDiv.remove();
                pageView.textLayerDiv = null;
            }
            pageView.textDivs = null;
            pageView.textLayerRender = null;
        }

        if (!pageView.searchTextPromise) {
            const textContentPromise = pageView.textContentPromise
                ?? pdfPage.getTextContent({ normalizeWhitespace: true });
            pageView.textContentPromise = textContentPromise;

            pageView.searchTextPromise = textContentPromise
                .then((content) => content.items.map((item) => item.str).join(' ').toLowerCase())
                .catch((err) => {
                    console.warn('No se pudo obtener el texto de la página segura', err);
                    return '';
                });
        }
    } catch (err) {
        if (err?.name === 'RenderingCancelledException') {
            return;
        }
        console.error('Error renderizando página del visor seguro', err);
    } finally {
        pageView.renderTask = null;
    }
}

async function renderAllPages(frameState) {
    if (!frameState) {
        return;
    }

    const scale = frameState.scale ?? 1;
    if (!frameState.pages || frameState.pages.length === 0) {
        return;
    }

    for (const pageView of frameState.pages) {
        await renderPageView(frameState, pageView, scale);
    }
}

function scheduleRenderAllPages(frameState) {
    if (!frameState) {
        return;
    }

    if (frameState.renderScheduled) {
        return;
    }

    frameState.renderScheduled = true;
    Promise.resolve().then(async () => {
        frameState.renderScheduled = false;
        await renderAllPages(frameState);
    });
}

async function resolvePageText(frameState, pageView) {
    if (!frameState?.pdfDoc || !pageView) {
        return '';
    }

    if (!pageView.searchTextPromise) {
        const textContentPromise = pageView.textContentPromise
            ?? frameState.pdfDoc.getPage(pageView.pageNumber)
                .then((page) => page.getTextContent({ normalizeWhitespace: true }));

        pageView.textContentPromise = textContentPromise;

        pageView.searchTextPromise = textContentPromise
            .then((content) => content.items.map((item) => item.str).join(' ').toLowerCase())
            .catch((err) => {
                console.warn('No se pudo obtener el texto de la página segura', err);
                return '';
            });
    }

    try {
        return await pageView.searchTextPromise;
    } catch (err) {
        console.warn('Error recuperando texto de página segura', err);
        return '';
    }
}

function updateZoom(state, frameState, scale, options = {}) {
    const targetScale = Math.min(Math.max(scale, 0.25), 4);
    state.currentZoom = targetScale;

    if (frameState) {
        const previousScale = typeof frameState.scale === 'number' ? frameState.scale : null;
        frameState.scale = targetScale;
        if (previousScale == null || Math.abs(previousScale - targetScale) > 0.001) {
            scheduleRenderAllPages(frameState);
        }
    }

    if (state.zoomLabel) {
        state.zoomLabel.textContent = `${Math.round(targetScale * 100)}%`;
    }

    if (!options.silent) {
        sendEvent(state, 'ZoomChanged', null, { zoom: targetScale });
    }
}

function adjustZoom(state, frameState, direction) {
    if (!state) {
        return;
    }

    const baseZoom = state.defaultZoom ?? 1;
    const step = state.zoomStep ?? 0.1;

    if (!frameState) {
        if (direction === 0) {
            state.pendingZoom = baseZoom;
        } else {
            const current = state.currentZoom ?? baseZoom;
            state.pendingZoom = Math.min(Math.max(current + direction * step, 0.25), 4);
        }
        return;
    }

    if (direction === 0) {
        updateZoom(state, frameState, baseZoom);
        return;
    }

    const current = state.currentZoom ?? baseZoom;
    updateZoom(state, frameState, current + direction * step);
}

function enterFullscreen(element) {
    if (!element) {
        return;
    }

    if (element.requestFullscreen) {
        element.requestFullscreen();
    } else if (element.webkitRequestFullscreen) {
        element.webkitRequestFullscreen();
    } else if (element.msRequestFullscreen) {
        element.msRequestFullscreen();
    }
}

async function executeSearch(state, query) {
    if (!query) {
        return;
    }

    sendEvent(state, 'SearchRequested', null, { query });

    const frameState = pdfFrames.get(state.frameId);
    if (!frameState?.pdfDoc) {
        sendEvent(state, 'SearchNotFound', null, { query });
        return;
    }

    const normalizedQuery = query.trim().toLowerCase();
    if (!normalizedQuery) {
        return;
    }

    let matchView = null;
    for (const pageView of frameState.pages) {
        try {
            const text = await resolvePageText(frameState, pageView);
            if (text && text.includes(normalizedQuery)) {
                matchView = pageView;
                break;
            }
        } catch (err) {
            console.warn('No se pudo evaluar la búsqueda segura en una página', err);
        }
    }

    if (matchView) {
        sendEvent(state, 'SearchMatch', matchView.pageNumber, { query });
        if (frameState.viewport && matchView.element) {
            const offsetTop = matchView.element.offsetTop;
            frameState.viewport.scrollTo({ top: offsetTop - 32, behavior: 'smooth' });
        }
    } else {
        sendEvent(state, 'SearchNotFound', null, { query });
    }
}

function setupToolbar(state, container, toolbarElement, options) {
    if (!toolbarElement) {
        return;
    }

    const settings = state.settings;
    const permissions = state.permissions;
    toolbarElement.innerHTML = '';
    toolbarElement.classList.add('secure-toolbar');
    toolbarElement.dataset.position = settings.toolbarPosition;
    state.pageIndicator = null;
    state.zoomLabel = null;

    if (!permissions.toolbarVisible) {
        toolbarElement.style.display = 'none';
        return;
    }

    toolbarElement.style.display = '';

    const frameState = pdfFrames.get(state.frameId);

    const leftGroup = document.createElement('div');
    leftGroup.className = 'secure-toolbar-group';

    const title = document.createElement('div');
    title.className = 'secure-toolbar-title';
    title.textContent = options.fileName || 'Documento protegido';
    leftGroup.appendChild(title);

    if (permissions.pageIndicatorEnabled) {
        const indicator = document.createElement('span');
        indicator.className = 'secure-toolbar-page-indicator';
        indicator.textContent = 'Pág. --';
        leftGroup.appendChild(indicator);
        state.pageIndicator = indicator;
    }

    if (permissions.zoomControlsEnabled) {
        const zoomGroup = document.createElement('div');
        zoomGroup.className = 'secure-toolbar-group secure-toolbar-zoom-group';

        const zoomOut = createToolbarButton('fas fa-minus', 'Reducir zoom', () => {
            adjustZoom(state, pdfFrames.get(state.frameId), -1);
            sendEvent(state, 'ZoomOut');
        });

        const zoomReset = createToolbarButton('fas fa-compress', 'Restablecer zoom', () => {
            adjustZoom(state, pdfFrames.get(state.frameId), 0);
            sendEvent(state, 'ZoomReset');
        });

        const zoomIn = createToolbarButton('fas fa-plus', 'Aumentar zoom', () => {
            adjustZoom(state, pdfFrames.get(state.frameId), 1);
            sendEvent(state, 'ZoomIn');
        });

        const zoomLabel = document.createElement('span');
        zoomLabel.className = 'secure-toolbar-zoom';
        zoomLabel.textContent = `${Math.round((state.currentZoom ?? state.defaultZoom ?? 1) * 100)}%`;
        state.zoomLabel = zoomLabel;

        zoomGroup.appendChild(zoomOut);
        zoomGroup.appendChild(zoomLabel);
        zoomGroup.appendChild(zoomIn);
        zoomGroup.appendChild(zoomReset);
        leftGroup.appendChild(zoomGroup);
    }

    toolbarElement.appendChild(leftGroup);

    const rightGroup = document.createElement('div');
    rightGroup.className = 'secure-toolbar-group secure-toolbar-group-right';

    if (permissions.searchEnabled) {
        const searchForm = document.createElement('form');
        searchForm.className = 'secure-toolbar-search';
        const input = document.createElement('input');
        input.type = 'search';
        input.placeholder = 'Buscar en el documento';
        input.className = 'form-control';
        searchForm.appendChild(input);

        registerHandler(state, searchForm, 'submit', async (event) => {
            event.preventDefault();
            try {
                await executeSearch(state, input.value);
            } catch (err) {
                console.warn('La búsqueda segura encontró un error', err);
            }
        });

        rightGroup.appendChild(searchForm);
    }

    if (permissions.downloadButtonVisible && permissions.downloadAllowed) {
        const downloadButton = createToolbarButton('fas fa-download', 'Descargar PDF', () => {
            const frame = pdfFrames.get(state.frameId);
            if (!frame?.base64) {
                return;
            }
            sendEvent(state, 'ToolbarDownload');
            downloadFile(frame.fileName, frame.base64, { state });
        });
        rightGroup.appendChild(downloadButton);
    }

    if (permissions.printButtonVisible && permissions.printAllowed) {
        const printButton = createToolbarButton('fas fa-print', 'Imprimir', () => {
            const frame = pdfFrames.get(state.frameId);
            sendEvent(state, 'ToolbarPrint');
            if (!frame?.base64) {
                console.warn('No se encontró el contenido del PDF para imprimir');
                return;
            }

            try {
                const bytes = frame.bytes ?? base64ToUint8Array(frame.base64);
                const blobUrl = URL.createObjectURL(new Blob([bytes], { type: 'application/pdf' }));
                const printWindow = window.open(blobUrl);
                if (!printWindow) {
                    console.warn('El navegador bloqueó la ventana de impresión segura');
                    setTimeout(() => URL.revokeObjectURL(blobUrl), 30_000);
                    return;
                }

                const cleanup = () => {
                    try { URL.revokeObjectURL(blobUrl); } catch { /* noop */ }
                };

                const triggerPrint = () => {
                    try {
                        printWindow.focus();
                        printWindow.print();
                    } catch (err) {
                        console.warn('No se pudo iniciar la impresión segura', err);
                    } finally {
                        cleanup();
                    }
                };

                if (printWindow.document?.readyState === 'complete') {
                    triggerPrint();
                } else {
                    printWindow.addEventListener('load', triggerPrint, { once: true });
                    setTimeout(() => {
                        try { printWindow.removeEventListener('load', triggerPrint); } catch { /* noop */ }
                        triggerPrint();
                    }, 1500);
                }
            } catch (err) {
                console.warn('No se pudo preparar la impresión segura', err);
            }
        });
        rightGroup.appendChild(printButton);
    }

    if (permissions.fullscreenButtonVisible) {
        const fullscreenButton = createToolbarButton('fas fa-expand', 'Pantalla completa', () => {
            sendEvent(state, 'ToolbarFullscreen');
            enterFullscreen(container);
        });
        rightGroup.appendChild(fullscreenButton);
    }

    toolbarElement.appendChild(rightGroup);

    if (frameState) {
        updateZoom(state, frameState, state.currentZoom ?? state.defaultZoom ?? 1, { silent: true });
    }

    updatePageIndicator(state, state.lastReportedPage ?? state.currentPage);
}

function createState(sessionId, options, container) {
    const settings = normalizeViewerSettings(options.settings);
    const permissions = normalizeViewerPermissions(options.policy, settings);
    const defaultZoomPercent = permissions.defaultZoomPercent ?? settings.defaultZoomPercent;
    const zoomStepPercent = permissions.zoomStepPercent ?? settings.zoomStepPercent;
    const defaultZoom = Math.max(defaultZoomPercent / 100, 0.25);
    const zoomStep = Math.max(zoomStepPercent / 100, 0.05);

    return {
        dotNetRef: options.dotNetRef,
        sessionId,
        frameId: options.frameId,
        handlers: [],
        trackingHandlers: [],
        watermarkElement: null,
        watermarkHost: null,
        watermarkOptions: { text: null, style: null },
        lastReportedPage: null,
        hasInitialPageReport: false,
        documentReadyReported: false,
        awaitingFrame: false,
        container,
        toolbarElement: options.toolbarId ? document.getElementById(options.toolbarId) : null,
        customStyleElement: null,
        settings,
        permissions,
        defaultZoom,
        zoomStep,
        currentZoom: defaultZoom,
        pendingZoom: null,
        zoomLabel: null,
        pageIndicator: null,
        currentPage: null,
        disableContextMenu: permissions.contextMenuBlocked,
        devToolsFlagged: false,
        lastPrintAttemptAt: 0,
        lastDownloadAttemptAt: 0
    };
}

function ensureWatermarkHost(frameState) {
    if (!frameState || !frameState.viewport) {
        return null;
    }

    if (!frameState.overlay) {
        const overlay = document.createElement('div');
        overlay.className = 'secure-pdf-overlay';
        overlay.setAttribute('aria-hidden', 'true');
        overlay.style.pointerEvents = 'none';
        if (!Array.isArray(frameState.cleanupCallbacks)) {
            frameState.cleanupCallbacks = [];
        }

        const contextGuard = (event) => {
            const frameId = frameState.container?.id;
            if (frameRequiresContextMenuBlock(frameId)) {
                event.preventDefault();
            }
        };

        overlay.addEventListener('contextmenu', contextGuard, true);
        frameState.cleanupCallbacks.push(() => overlay.removeEventListener('contextmenu', contextGuard, true));
        frameState.viewport.appendChild(overlay);
        frameState.overlay = overlay;
    }

    if (!frameState.watermarkLayer) {
        const layer = document.createElement('div');
        layer.className = 'secure-pdf-watermark-layer';
        frameState.overlay.appendChild(layer);
        frameState.watermarkLayer = layer;
    }

    return frameState.watermarkLayer;
}

function initSecurePdfViewer(elementId, options) {
    const container = document.getElementById(elementId);
    if (!container) {
        console.warn('No se encontró el contenedor del visor PDF seguro');
        return;
    }

    const sessionId = options.sessionId;
    if (sessions.has(sessionId)) {
        disposeSecurePdfViewer(sessionId);
    }

    const state = createState(sessionId, options, container);
    const settings = state.settings;
    const permissions = state.permissions;
    state.dotNetRef = options.dotNetRef;

    sessions.set(sessionId, state);
    updateGlobalPolicyGuards();

    container.setAttribute('data-secure-viewer', 'true');
    updateContainerPolicyAttributes(container, permissions);
    applyViewerTheme(container, settings);
    toggleSelection(container, permissions.textSelectionBlocked);
    applySelectionState(state);

    registerHandler(state, container, 'dragstart', (e) => e.preventDefault());

    if (permissions.textSelectionBlocked) {
        const selectionHandler = () => clearSelectionWithin(container);
        registerHandler(state, document, 'selectionchange', selectionHandler, true);
        registerHandler(state, container, 'mouseup', selectionHandler, true);
    }

    state.watermarkOptions = {
        text: options.watermarkText,
        style: options.watermarkStyle
    };
    const frameState = pdfFrames.get(state.frameId);
    if (frameState && !permissions.downloadAllowed) {
        frameState.base64 = null;
    }
    state.watermarkHost = ensureWatermarkHost(frameState);
    const watermarkTarget = state.watermarkHost || container;
    state.watermarkElement = buildWatermark(watermarkTarget, state.watermarkOptions.text, state.watermarkOptions.style);

    if (settings.customCss) {
        const style = document.createElement('style');
        style.textContent = settings.customCss;
        container.appendChild(style);
        state.customStyleElement = style;
    }

    if (!frameState) {
        state.awaitingFrame = true;
    } else {
        updateZoom(state, frameState, state.currentZoom, { silent: true });
        if (state.watermarkElement && state.watermarkHost) {
            state.watermarkHost.appendChild(state.watermarkElement);
        }
        ensureDocumentReady(state, frameState);
    }

    state.disableContextMenu = options.disableContextMenu || permissions.contextMenuBlocked;
    if (container) {
        container.dataset.contextMenuBlocked = state.disableContextMenu ? 'true' : 'false';
    }

    if (typeof window !== 'undefined' && !permissions.printAllowed) {
        registerHandler(state, window, 'beforeprint', () => {
            reportPrintAttempt(state);
        });
        registerHandler(state, window, 'afterprint', () => {
            // Reenfoca para evitar vistas persistentes tras intento bloqueado.
            try { window.focus(); } catch { /* noop */ }
        });
    }

    const contextMenuHandler = (event) => {
        if (!container) {
            return;
        }

        const rect = container.getBoundingClientRect();
        const inside = event.clientX >= rect.left && event.clientX <= rect.right &&
            event.clientY >= rect.top && event.clientY <= rect.bottom;

        if (!inside) {
            return;
        }

        if (state.disableContextMenu) {
            event.preventDefault();
            sendEvent(state, 'ContextMenuBlocked');
        } else {
            sendEvent(state, 'ContextMenuOpened');
        }
    };

    registerHandler(state, document, 'contextmenu', contextMenuHandler, true);
    if (state.frameId) {
        const frameElement = document.getElementById(state.frameId);
        if (frameElement) {
            registerHandler(state, frameElement, 'contextmenu', (event) => {
                if (!state.disableContextMenu) {
                    return;
                }
                event.preventDefault();
                sendEvent(state, 'ContextMenuBlocked');
            }, true);
        }
    }

    const keydownHandler = (e) => {
        const key = e.key.toLowerCase();
        const meta = e.metaKey || e.ctrlKey;
        if (key === 'printscreen') {
            notifyScreenshotAttempt(state, 'KeyDown');
        }

        if (meta && key === 'p' && !permissions.printAllowed) {
            e.preventDefault();
            reportPrintAttempt(state);
        }

        if (meta && key === 's' && !permissions.downloadAllowed) {
            e.preventDefault();
            reportDownloadAttempt(state);
        }

        if ((e.metaKey && e.shiftKey && (key === '3' || key === '4')) || (e.ctrlKey && e.altKey && key === 'printscreen')) {
            notifyScreenshotAttempt(state, 'SystemCaptureShortcut');
        }
    };

    const keyupHandler = (e) => {
        const key = e.key.toLowerCase();
        if (key === 'printscreen') {
            notifyScreenshotAttempt(state, 'KeyUp');
        }

        if ((e.ctrlKey || e.metaKey) && e.shiftKey && key === 's') {
            notifyScreenshotAttempt(state, 'SnippingShortcut');
        }
    };

    if (typeof window !== 'undefined') {
        registerHandler(state, window, 'keydown', keydownHandler, true);
        registerHandler(state, window, 'keyup', keyupHandler, true);
    } else {
        registerHandler(state, document, 'keydown', keydownHandler, true);
        registerHandler(state, document, 'keyup', keyupHandler, true);
    }

    registerHandler(state, window, 'blur', () => {
        sendEvent(state, 'WindowBlur');
    });

    registerHandler(state, window, 'focus', () => {
        sendEvent(state, 'WindowFocus');
    });

    if (!permissions.copyAllowed) {
        registerHandler(state, document, 'copy', (e) => {
            e.preventDefault();
            sendEvent(state, 'CopyAttempt');
        });
    }

    registerHandler(state, document, 'visibilitychange', () => {
        if (document.visibilityState === 'hidden') {
            sendEvent(state, 'WindowHidden');
        } else if (document.visibilityState === 'visible') {
            sendEvent(state, 'WindowVisible');
        }
    });

    const devToolsDetector = setInterval(() => {
        if (!state || !state.dotNetRef) {
            return;
        }

        const widthDelta = Math.abs(window.outerWidth - window.innerWidth);
        const heightDelta = Math.abs(window.outerHeight - window.innerHeight);
        if ((widthDelta > 160 || heightDelta > 160) && !state.devToolsFlagged) {
            state.devToolsFlagged = true;
            sendEvent(state, 'DeveloperToolsDetected');
        }
    }, 2000);

    state.handlers.push({
        dispose: () => clearInterval(devToolsDetector)
    });

    const effectiveMaxMinutes = options.maxViewMinutes && options.maxViewMinutes > 0
        ? options.maxViewMinutes
        : settings.maxViewTimeMinutes;

    if (effectiveMaxMinutes && effectiveMaxMinutes > 0) {
        const timeoutId = setTimeout(() => {
            sendEvent(state, 'ViewTimeExceeded');
        }, effectiveMaxMinutes * 60 * 1000);

        state.handlers.push({
            dispose: () => clearTimeout(timeoutId)
        });
    }

    if (state.toolbarElement) {
        setupToolbar(state, container, state.toolbarElement, options);
    }

    ensureTrackingForFrame(state.frameId);
    ensurePageTracking(state);
}

async function disposePdfFrame(frameId) {
    const frameState = pdfFrames.get(frameId);
    pdfFrames.delete(frameId);

    if (frameState) {
        clearFrameTextLayers(frameState);

        if (Array.isArray(frameState.cleanupCallbacks)) {
            for (const cleanup of frameState.cleanupCallbacks) {
                try {
                    cleanup();
                } catch (err) {
                    console.warn('No se pudo limpiar un manejador del marco seguro', err);
                }
            }
            frameState.cleanupCallbacks.length = 0;
        }

        try {
            if (frameState.pdfDoc && typeof frameState.pdfDoc.destroy === 'function') {
                await frameState.pdfDoc.destroy();
            } else if (frameState.loadingTask && typeof frameState.loadingTask.destroy === 'function') {
                await frameState.loadingTask.destroy();
            }
        } catch (err) {
            console.warn('No se pudo liberar recursos del PDF seguro', err);
        }

        if (frameState.objectUrl) {
            try {
                URL.revokeObjectURL(frameState.objectUrl);
            } catch (err) {
                console.warn('No se pudo liberar el blob del visor seguro', err);
            }
            frameState.objectUrl = null;
        }
    }

    const frameElement = document.getElementById(frameId);
    if (frameElement) {
        frameElement.innerHTML = '';
        frameElement.scrollTop = 0;
    }

    for (const state of sessions.values()) {
        if (state.frameId === frameId) {
            cleanupPageTracking(state);
            state.lastReportedPage = null;
            state.hasInitialPageReport = false;
            state.watermarkHost = null;
            if (state.watermarkElement) {
                state.watermarkElement.remove();
                state.watermarkElement = null;
            }
        }
    }
}

async function renderPdf(frameId, base64Data, fileName) {
    const frame = document.getElementById(frameId);
    if (!frame) {
        throw new Error('No se encontró el contenedor del PDF seguro');
    }

    await disposePdfFrame(frameId);

    const loadingIndicator = document.createElement('div');
    loadingIndicator.className = 'secure-pdf-loading';
    loadingIndicator.textContent = 'Cargando documento seguro…';
    frame.appendChild(loadingIndicator);

    let objectUrl = null;
    let frameStateCreated = false;
    const cleanupCallbacks = [];

    try {
        const pdfjsLib = await ensurePdfJsLibrary();
        const pdfBytes = base64ToUint8Array(base64Data);
        const pdfBlob = new Blob([pdfBytes], { type: 'application/pdf' });
        objectUrl = URL.createObjectURL(pdfBlob);
        const loadingTask = pdfjsLib.getDocument({ data: pdfBytes });
        const pdfDoc = await loadingTask.promise;

        frame.innerHTML = '';

        const viewport = document.createElement('div');
        viewport.className = 'secure-pdf-viewport';

        const iframe = document.createElement('iframe');
        let sandboxed = true;
        iframe.className = 'secure-pdf-iframe';
        iframe.title = 'Documento PDF seguro';
        iframe.src = `${objectUrl}#toolbar=0&navpanes=0&scrollbar=0`;
        iframe.setAttribute('frameborder', '0');
        iframe.setAttribute('allowfullscreen', 'true');
        iframe.setAttribute('allow', 'fullscreen');
        iframe.setAttribute('referrerpolicy', 'no-referrer');
        iframe.setAttribute('sandbox', 'allow-forms allow-modals allow-pointer-lock allow-popups allow-same-origin allow-scripts allow-top-navigation-by-user-activation');

        iframe.addEventListener('load', () => {
            if (sandboxed) {
                try {
                    const fallbackText = (iframe.contentDocument?.body?.innerText || '').toLowerCase();
                    if (fallbackText.includes('ha bloqueado esta página')
                        || fallbackText.includes('chrome bloqueó esta página')
                        || fallbackText.includes('chrome bloqueo esta pagina')
                        || fallbackText.includes('blocked this page')) {
                        sandboxed = false;
                        iframe.removeAttribute('sandbox');
                        iframe.src = `${objectUrl}#toolbar=0&navpanes=0&scrollbar=0`;
                        return;
                    }
                } catch (sandboxErr) {
                    console.warn('No se pudo verificar el estado del sandbox del visor seguro', sandboxErr);
                }
            }
            viewport.dispatchEvent(new Event('scroll'));
            try {
                const frameWindow = iframe.contentWindow;
                const frameDoc = iframe.contentDocument || frameWindow?.document;
                if (frameDoc) {
                    try {
                        const styleElement = frameDoc.createElement('style');
                        styleElement.textContent = `* { user-select: none !important; }
                        ::selection { background: transparent !important; color: inherit !important; }
                        html, body { cursor: not-allowed; }
                        body { -webkit-user-drag: none !important; }
                        a, button { pointer-events: none !important; }`;
                        if (frameDoc.head) {
                            forEachSessionByFrame(frameId, (relatedState) => {
                                if (relatedState.settings?.disableTextSelection) {
                                    frameDoc.head.appendChild(styleElement.cloneNode(true));
                                }
                            });
                        }
                    } catch (styleErr) {
                        console.warn('No se pudo inyectar estilos anti selección en el visor seguro', styleErr);
                    }

        const pagesHost = document.createElement('div');
        pagesHost.className = 'secure-pdf-pages';
        viewport.appendChild(pagesHost);

        const overlay = document.createElement('div');
        overlay.className = 'secure-pdf-overlay';
        overlay.setAttribute('aria-hidden', 'true');

        const contextGuard = (event) => {
            if (frameRequiresContextMenuBlock(frameId)) {
                event.preventDefault();
            }
        };

        viewport.addEventListener('contextmenu', contextGuard, true);
        overlay.addEventListener('contextmenu', contextGuard, true);
        cleanupCallbacks.push(() => viewport.removeEventListener('contextmenu', contextGuard, true));
        cleanupCallbacks.push(() => overlay.removeEventListener('contextmenu', contextGuard, true));

        const watermarkLayer = document.createElement('div');
        watermarkLayer.className = 'secure-pdf-watermark-layer';
        overlay.appendChild(watermarkLayer);

        viewport.appendChild(overlay);
        frame.appendChild(viewport);

        const frameState = {
            container: frame,
            viewport,
            pagesHost,
            pages: [],
            pdfDoc,
            pdfjsLib,
            loadingTask,
            scale: 1,
            base64: base64Data,
            bytes: pdfBytes,
            fileName,
            overlay,
            watermarkLayer,
            objectUrl,
            renderScheduled: false,
            cleanupCallbacks,
            allowTextSelection: frameAllowsTextSelection(frameId)
        };

        for (let pageNumber = 1; pageNumber <= pdfDoc.numPages; pageNumber++) {
            const pageWrapper = document.createElement('div');
            pageWrapper.className = 'secure-pdf-page';

            const canvas = document.createElement('canvas');
            canvas.className = 'secure-pdf-canvas';
            pageWrapper.appendChild(canvas);

            pagesHost.appendChild(pageWrapper);

            frameState.pages.push({
                pageNumber,
                element: pageWrapper,
                canvas,
                viewport: null,
                renderTask: null,
                textLayerDiv: null,
                textLayerRender: null,
                textDivs: null,
                textContentPromise: null,
                searchTextPromise: null
            });
        }

        pdfFrames.set(frameId, frameState);

        frameStateCreated = true;

        let initialScale = 1;
        forEachSessionByFrame(frameId, (state) => {
            const currentFrameState = pdfFrames.get(frameId);
            state.watermarkHost = ensureWatermarkHost(currentFrameState);
            if (state.watermarkElement) {
                state.watermarkElement.remove();
            }

            const desiredText = state.permissions?.watermarkForced
                ? (state.settings?.globalWatermarkText || state.watermarkOptions?.text)
                : state.watermarkOptions?.text;

            const desiredStyle = state.watermarkOptions?.style || (state.settings ? {
                color: state.settings.watermarkColor,
                opacity: state.settings.watermarkOpacity,
                fontSize: state.settings.watermarkFontSize
            } : null);

            state.watermarkElement = buildWatermark(state.watermarkHost || overlay, desiredText, desiredStyle);

            initialScale = state.currentZoom ?? state.defaultZoom ?? initialScale;
            ensureDocumentReady(state, currentFrameState);
        });

        frameState.scale = initialScale;
        await renderAllPages(frameState);
        ensureTrackingForFrame(frameId);
    } catch (err) {
        console.error('Error renderizando el PDF seguro', err);
        frame.innerHTML = '';
        const errorMessage = document.createElement('div');
        errorMessage.className = 'secure-pdf-loading';
        errorMessage.textContent = 'No se pudo renderizar el documento.';
        frame.appendChild(errorMessage);
        forEachSessionByFrame(frameId, (state) => {
            sendEvent(state, 'DocumentError', null, { message: err?.message || 'RenderFailed' });
        });
        if (cleanupCallbacks.length) {
            for (const cleanup of cleanupCallbacks) {
                try {
                    cleanup();
                } catch (cleanupErr) {
                    console.warn('No se pudo limpiar un recurso del visor seguro tras un error', cleanupErr);
                }
            }
            cleanupCallbacks.length = 0;
        }
        if (!frameStateCreated && objectUrl) {
            try {
                URL.revokeObjectURL(objectUrl);
            } catch (revokeErr) {
                console.warn('No se pudo liberar el blob del visor seguro', revokeErr);
            }
        }
        throw err;
    }
}

function disposeSecurePdfViewer(sessionId) {
    const state = sessions.get(sessionId);
    if (!state) {
        return;
    }

    cleanupPageTracking(state);

    for (const entry of state.handlers) {
        if (entry.dispose) {
            try { entry.dispose(); } catch (err) { console.error(err); }
            continue;
        }

        entry.target.removeEventListener(entry.eventName, entry.handler, entry.options);
    }

    if (state.toolbarElement) {
        state.toolbarElement.innerHTML = '';
        state.toolbarElement.style.display = '';
        state.toolbarElement.classList.remove('secure-toolbar');
    }

    state.pageIndicator = null;

    if (state.customStyleElement && state.customStyleElement.parentElement) {
        state.customStyleElement.parentElement.removeChild(state.customStyleElement);
    }

    if (state.watermarkElement && state.watermarkElement.parentElement) {
        state.watermarkElement.parentElement.removeChild(state.watermarkElement);
    }

    clearViewerTheme(state.container);
    clearContainerPolicyAttributes(state.container);
    toggleSelection(state.container, false);

    sessions.delete(sessionId);
    updateGlobalPolicyGuards();
}

function notifyPdfPage(sessionId, pageNumber) {
    const state = sessions.get(sessionId);
    if (!state) {
        return;
    }

    state.lastReportedPage = pageNumber;
    state.hasInitialPageReport = true;
    state.currentPage = pageNumber;
    updatePageIndicator(state, pageNumber);
    sendEvent(state, 'PageView', pageNumber, { pageNumber });
}

function downloadFile(fileName, base64Data, options) {
    const targetState = options && typeof options === 'object' ? options.state : null;

    if (targetState && (!targetState.permissions || !targetState.permissions.downloadAllowed)) {
        reportDownloadAttempt(targetState);
        return;
    }

    const link = document.createElement('a');
    link.href = `data:application/octet-stream;base64,${base64Data}`;
    link.download = fileName || 'archivo';
    link.style.display = 'none';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

function ensureDownloadInterop() {
    if (!globalSecureViewerScope) {
        return;
    }

    const namespace = globalSecureViewerScope.ConfidentialBox || (globalSecureViewerScope.ConfidentialBox = {});

    if (typeof namespace.downloadFile !== 'function') {
        namespace.downloadFile = downloadFile;
    }
}

function ensureSecureViewerReady() {
    return true;
}

const globalSecureViewerScope = typeof window !== 'undefined'
    ? window
    : (typeof self !== 'undefined'
        ? self
        : (typeof globalThis !== 'undefined' ? globalThis : {}));

if (globalSecureViewerScope) {
    const namespace = globalSecureViewerScope.ConfidentialBox || (globalSecureViewerScope.ConfidentialBox = {});

    namespace.collectClientContext = collectClientContext;
    namespace.initSecurePdfViewer = initSecurePdfViewer;
    namespace.disposePdfFrame = disposePdfFrame;
    namespace.renderPdf = renderPdf;
    namespace.disposeSecurePdfViewer = disposeSecurePdfViewer;
    namespace.notifyPdfPage = notifyPdfPage;
    namespace.downloadFile = downloadFile;
    namespace.ensureSecureViewerReady = ensureSecureViewerReady;
    namespace.ensureDownloadInterop = ensureDownloadInterop;
}
