const sessions = new Map();
const pdfFrames = new Map();

async function resolveGeoMetadata() {
    try {
        const response = await fetch('https://ipapi.co/json/', { cache: 'no-store' });
        if (!response.ok) {
            return null;
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
        return null;
    }
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

function base64ToUint8Array(base64) {
    const binary = atob(base64);
    const length = binary.length;
    const bytes = new Uint8Array(length);

    for (let i = 0; i < length; i++) {
        bytes[i] = binary.charCodeAt(i);
    }

    return bytes;
}

function createPdfObjectUrl(base64) {
    const pdfBytes = base64ToUint8Array(base64);
    const blob = new Blob([pdfBytes], { type: 'application/pdf' });
    return URL.createObjectURL(blob);
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

function normalizeViewerSettings(raw) {
    const normalized = { ...defaultViewerSettings, ...(raw || {}) };
    normalized.theme = (normalized.theme || 'dark').toLowerCase();
    normalized.toolbarPosition = normalized.toolbarPosition === 'bottom' ? 'bottom' : 'top';
    normalized.viewerPadding = normalized.viewerPadding || defaultViewerSettings.viewerPadding;
    normalized.fontFamily = normalized.fontFamily || defaultViewerSettings.fontFamily;
    normalized.customCss = normalized.customCss || '';
    normalized.defaultZoomPercent = Math.min(Math.max(normalized.defaultZoomPercent || defaultViewerSettings.defaultZoomPercent, 25), 400);
    normalized.zoomStepPercent = Math.min(Math.max(normalized.zoomStepPercent || defaultViewerSettings.zoomStepPercent, 5), 50);
    normalized.watermarkFontSize = Math.min(Math.max(normalized.watermarkFontSize || defaultViewerSettings.watermarkFontSize, 16), 120);
    normalized.watermarkOpacity = Math.min(Math.max(typeof normalized.watermarkOpacity === 'number' ? normalized.watermarkOpacity : defaultViewerSettings.watermarkOpacity, 0), 1);
    normalized.maxViewTimeMinutes = Math.max(0, normalized.maxViewTimeMinutes || 0);
    normalized.showPageIndicator = normalized.showPageIndicator !== false;
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

function updateZoom(state, frameState, scale, options = {}) {
    const targetScale = Math.min(Math.max(scale, 0.25), 4);
    state.currentZoom = targetScale;

    const iframe = frameState?.iframe;
    if (iframe) {
        iframe.style.transform = `scale(${targetScale})`;
        iframe.style.width = `${(1 / targetScale) * 100}%`;
        iframe.style.height = `${(1 / targetScale) * 100}%`;
    }

    if (frameState?.viewport) {
        frameState.viewport.scrollTop = frameState.viewport.scrollTop;
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

function executeSearch(state, query) {
    if (!query) {
        return;
    }

    sendEvent(state, 'SearchRequested', null, { query });

    const frameState = pdfFrames.get(state.frameId);
    const iframe = frameState?.iframe;
    if (!iframe || !iframe.contentWindow) {
        return;
    }

    try {
        const win = iframe.contentWindow;
        const found = win.find(query, false, false, true, false, true, false);
        if (!found) {
            sendEvent(state, 'SearchNotFound', null, { query });
        }
    } catch (err) {
        console.warn('No se pudo ejecutar la búsqueda segura', err);
    }
}

function setupToolbar(state, container, toolbarElement, options) {
    if (!toolbarElement) {
        return;
    }

    const settings = state.settings;
    toolbarElement.innerHTML = '';
    toolbarElement.classList.add('secure-toolbar');
    toolbarElement.dataset.position = settings.toolbarPosition;
    state.pageIndicator = null;

    if (!settings.showToolbar) {
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

    if (settings.showPageIndicator) {
        const indicator = document.createElement('span');
        indicator.className = 'secure-toolbar-page-indicator';
        indicator.textContent = 'Pág. --';
        leftGroup.appendChild(indicator);
        state.pageIndicator = indicator;
    }

    if (settings.showPageControls) {
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

    if (settings.showSearch) {
        const searchForm = document.createElement('form');
        searchForm.className = 'secure-toolbar-search';
        const input = document.createElement('input');
        input.type = 'search';
        input.placeholder = 'Buscar en el documento';
        input.className = 'form-control';
        searchForm.appendChild(input);

        registerHandler(state, searchForm, 'submit', (event) => {
            event.preventDefault();
            executeSearch(state, input.value);
        });

        rightGroup.appendChild(searchForm);
    }

    if (settings.showDownloadButton && settings.allowDownload) {
        const downloadButton = createToolbarButton('fas fa-download', 'Descargar PDF', () => {
            const frame = pdfFrames.get(state.frameId);
            if (!frame?.base64) {
                return;
            }
            sendEvent(state, 'ToolbarDownload');
            downloadFile(frame.fileName, frame.base64);
        });
        rightGroup.appendChild(downloadButton);
    }

    if (settings.showPrintButton && settings.allowPrint) {
        const printButton = createToolbarButton('fas fa-print', 'Imprimir', () => {
            const frame = pdfFrames.get(state.frameId);
            const iframe = frame?.iframe;
            sendEvent(state, 'ToolbarPrint');
            if (iframe && iframe.contentWindow && typeof iframe.contentWindow.print === 'function') {
                try {
                    iframe.contentWindow.focus();
                    iframe.contentWindow.print();
                } catch (err) {
                    console.warn('No se pudo abrir el diálogo de impresión seguro', err);
                }
            }
        });
        rightGroup.appendChild(printButton);
    }

    if (settings.showFullscreenButton) {
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
    const defaultZoom = Math.max(settings.defaultZoomPercent / 100, 0.25);
    const zoomStep = Math.max(settings.zoomStepPercent / 100, 0.05);

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
        awaitingFrame: false,
        container,
        toolbarElement: options.toolbarId ? document.getElementById(options.toolbarId) : null,
        customStyleElement: null,
        settings,
        defaultZoom,
        zoomStep,
        currentZoom: defaultZoom,
        pendingZoom: null,
        zoomLabel: null,
        pageIndicator: null,
        currentPage: null,
        disableContextMenu: false,
        devToolsFlagged: false
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
        overlay.addEventListener('contextmenu', (event) => event.preventDefault(), true);
        overlay.style.pointerEvents = 'none';
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
    state.dotNetRef = options.dotNetRef;

    sessions.set(sessionId, state);

    container.setAttribute('data-secure-viewer', 'true');
    applyViewerTheme(container, settings);
    toggleSelection(container, settings.disableTextSelection);

    registerHandler(state, container, 'dragstart', (e) => e.preventDefault());

    if (settings.disableTextSelection) {
        const selectionHandler = () => clearSelectionWithin(container);
        registerHandler(state, document, 'selectionchange', selectionHandler, true);
        registerHandler(state, container, 'mouseup', selectionHandler, true);
    }

    state.watermarkOptions = {
        text: options.watermarkText,
        style: options.watermarkStyle
    };
    const frameState = pdfFrames.get(state.frameId);
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
    }

    state.disableContextMenu = options.disableContextMenu || settings.disableContextMenu;

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

    registerHandler(state, document, 'keydown', (e) => {
        const key = e.key.toLowerCase();
        const meta = e.metaKey || e.ctrlKey;
        if (key === 'printscreen') {
            notifyScreenshotAttempt(state, 'KeyDown');
        }

        if (meta && key === 'p' && !settings.allowPrint) {
            e.preventDefault();
            sendEvent(state, 'PrintAttempt');
        }

        if (meta && key === 's' && !settings.allowDownload) {
            e.preventDefault();
            sendEvent(state, 'DownloadAttempt');
        }

        if ((e.metaKey && e.shiftKey && (key === '3' || key === '4')) || (e.ctrlKey && e.altKey && key === 'printscreen')) {
            notifyScreenshotAttempt(state, 'SystemCaptureShortcut');
        }
    }, true);

    registerHandler(state, document, 'keyup', (e) => {
        const key = e.key.toLowerCase();
        if (key === 'printscreen') {
            notifyScreenshotAttempt(state, 'KeyUp');
        }

        if ((e.ctrlKey || e.metaKey) && e.shiftKey && key === 's') {
            notifyScreenshotAttempt(state, 'SnippingShortcut');
        }
    }, true);

    registerHandler(state, window, 'blur', () => {
        sendEvent(state, 'WindowBlur');
    });

    registerHandler(state, window, 'focus', () => {
        sendEvent(state, 'WindowFocus');
    });

    if (!settings.allowCopy) {
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

    ensurePageTracking(state);
}

async function disposePdfFrame(frameId) {
    const frameState = pdfFrames.get(frameId);
    pdfFrames.delete(frameId);

    if (frameState) {
        try {
            if (frameState.pdfDoc && typeof frameState.pdfDoc.destroy === 'function') {
                await frameState.pdfDoc.destroy();
            } else if (frameState.loadingTask && typeof frameState.loadingTask.destroy === 'function') {
                await frameState.loadingTask.destroy();
            }
            if (frameState.objectUrl) {
                URL.revokeObjectURL(frameState.objectUrl);
            }
        } catch (err) {
            console.warn('No se pudo liberar recursos del PDF seguro', err);
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
    try {
        objectUrl = createPdfObjectUrl(base64Data);
        frame.innerHTML = '';

        const viewport = document.createElement('div');
        viewport.className = 'secure-pdf-viewport';
        viewport.addEventListener('contextmenu', (evt) => evt.preventDefault(), true);

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

                    frameDoc.addEventListener('contextmenu', (evt) => {
                        forEachSessionByFrame(frameId, (relatedState) => {
                            if (relatedState.disableContextMenu) {
                                evt.preventDefault();
                                sendEvent(relatedState, 'ContextMenuBlocked');
                            } else {
                                sendEvent(relatedState, 'ContextMenuOpened');
                            }
                        });
                    }, true);

                    frameDoc.addEventListener('selectionchange', () => {
                        forEachSessionByFrame(frameId, (relatedState) => {
                            if (relatedState.settings?.disableTextSelection) {
                                clearSelectionWithin(frameDoc.body, frameDoc);
                                sendEvent(relatedState, 'SelectionCleared');
                            }
                        });
                    });

                    frameDoc.addEventListener('copy', (evt) => {
                        forEachSessionByFrame(frameId, (relatedState) => {
                            if (!relatedState.settings?.allowCopy) {
                                evt.preventDefault();
                                sendEvent(relatedState, 'CopyAttempt');
                            }
                        });
                    }, true);
                }

                if (frameWindow) {
                    frameWindow.addEventListener('keyup', (evt) => {
                        const key = evt.key.toLowerCase();
                        if (key === 'printscreen') {
                            forEachSessionByFrame(frameId, (relatedState) => notifyScreenshotAttempt(relatedState, 'FrameKeyUp'));
                        }
                    }, true);

                    frameWindow.addEventListener('keydown', (evt) => {
                        const key = evt.key.toLowerCase();
                        if (key === 'printscreen') {
                            forEachSessionByFrame(frameId, (relatedState) => notifyScreenshotAttempt(relatedState, 'FrameKeyDown'));
                        }
                        if ((evt.ctrlKey || evt.metaKey) && evt.shiftKey && key === 's') {
                            forEachSessionByFrame(frameId, (relatedState) => notifyScreenshotAttempt(relatedState, 'FrameSnippingShortcut'));
                        }
                    }, true);
                }
            } catch (err) {
                console.warn('No se pudo interceptar el menú contextual interno del visor seguro', err);
            }
        });

        viewport.appendChild(iframe);

        const overlay = document.createElement('div');
        overlay.className = 'secure-pdf-overlay';
        overlay.setAttribute('aria-hidden', 'true');
        overlay.addEventListener('contextmenu', (event) => event.preventDefault(), true);

        const watermarkLayer = document.createElement('div');
        watermarkLayer.className = 'secure-pdf-watermark-layer';
        overlay.appendChild(watermarkLayer);

        viewport.appendChild(overlay);
        frame.appendChild(viewport);

        pdfFrames.set(frameId, {
            container: frame,
            viewport,
            iframe,
            pages: [],
            objectUrl,
            base64: base64Data,
            fileName,
            overlay,
            watermarkLayer
        });

        forEachSessionByFrame(frameId, (state) => {
            const currentFrameState = pdfFrames.get(frameId);
            state.watermarkHost = ensureWatermarkHost(currentFrameState);
            if (state.watermarkElement) {
                state.watermarkElement.remove();
            }

            const desiredText = state.settings?.forceGlobalWatermark
                ? state.settings.globalWatermarkText
                : state.watermarkOptions?.text;

            const desiredStyle = state.watermarkOptions?.style || (state.settings ? {
                color: state.settings.watermarkColor,
                opacity: state.settings.watermarkOpacity,
                fontSize: state.settings.watermarkFontSize
            } : null);

            state.watermarkElement = buildWatermark(state.watermarkHost || overlay, desiredText, desiredStyle);
        });

        ensureTrackingForFrame(frameId);
    } catch (err) {
        console.error('Error renderizando el PDF seguro', err);
        frame.innerHTML = '';
        const errorMessage = document.createElement('div');
        errorMessage.className = 'secure-pdf-loading';
        errorMessage.textContent = 'No se pudo renderizar el documento.';
        frame.appendChild(errorMessage);
        if (objectUrl) {
            URL.revokeObjectURL(objectUrl);
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
    toggleSelection(state.container, false);

    sessions.delete(sessionId);
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

function downloadFile(fileName, base64Data) {
    const link = document.createElement('a');
    link.href = `data:application/octet-stream;base64,${base64Data}`;
    link.download = fileName || 'archivo';
    link.style.display = 'none';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
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
}
