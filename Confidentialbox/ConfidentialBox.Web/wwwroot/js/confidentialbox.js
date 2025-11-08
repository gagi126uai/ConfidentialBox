const sessions = new Map();
const pdfFrames = new Map();

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
}

function buildWatermark(container, text) {
    if (!text) {
        return null;
    }

    const watermark = document.createElement('div');
    watermark.className = 'secure-pdf-watermark';
    watermark.textContent = text;
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

    const frameElement = frameState.container;
    const scheduleQueue = { scheduled: false };

    const evaluateVisiblePage = () => {
        scheduleQueue.scheduled = false;
        const nextPage = getMostVisiblePage(frameElement, frameState.pages);

        if (!nextPage || nextPage === state.lastReportedPage) {
            return;
        }

        state.lastReportedPage = nextPage;

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
    for (const state of sessions.values()) {
        if (state.frameId === frameId) {
            ensurePageTracking(state);
        }
    }
}

export function initSecurePdfViewer(elementId, options) {
    const container = document.getElementById(elementId);
    if (!container) {
        console.warn('No se encontró el contenedor del visor PDF seguro');
        return;
    }

    const sessionId = options.sessionId;
    if (sessions.has(sessionId)) {
        disposeSecurePdfViewer(sessionId);
    }

    const state = {
        dotNetRef: options.dotNetRef,
        sessionId,
        frameId: options.frameId,
        handlers: [],
        trackingHandlers: [],
        watermarkElement: null,
        lastReportedPage: null,
        hasInitialPageReport: false,
        awaitingFrame: false
    };

    sessions.set(sessionId, state);

    container.setAttribute('data-secure-viewer', 'true');
    registerHandler(state, container, 'dragstart', (e) => e.preventDefault());

    state.watermarkElement = buildWatermark(container, options.watermarkText);

    if (options.disableContextMenu) {
        registerHandler(state, container, 'contextmenu', (e) => {
            e.preventDefault();
            sendEvent(state, 'ContextMenuBlocked');
        });
    }

    registerHandler(state, document, 'keydown', (e) => {
        if (e.key === 'PrintScreen') {
            e.preventDefault();
            sendEvent(state, 'ScreenshotAttempt');
        }

        if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'p') {
            e.preventDefault();
            sendEvent(state, 'PrintAttempt');
        }

        if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 's') {
            e.preventDefault();
            sendEvent(state, 'DownloadAttempt');
        }
    }, true);

    registerHandler(state, document, 'copy', (e) => {
        e.preventDefault();
        sendEvent(state, 'CopyAttempt');
    });

    registerHandler(state, document, 'visibilitychange', () => {
        if (document.visibilityState === 'hidden') {
            sendEvent(state, 'WindowHidden');
        }
    });

    if (options.maxViewMinutes && options.maxViewMinutes > 0) {
        const timeoutId = setTimeout(() => {
            sendEvent(state, 'ViewTimeExceeded');
        }, options.maxViewMinutes * 60 * 1000);

        state.handlers.push({
            dispose: () => clearTimeout(timeoutId)
        });
    }

    ensurePageTracking(state);
}

export async function disposePdfFrame(frameId) {
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
        }
    }
}

export async function renderPdf(frameId, base64Data) {
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

        const iframe = document.createElement('iframe');
        iframe.className = 'secure-pdf-iframe';
        iframe.title = 'Documento PDF seguro';
        iframe.src = objectUrl;
        iframe.setAttribute('frameborder', '0');
        iframe.setAttribute('allowfullscreen', 'true');

        iframe.addEventListener('load', () => {
            frame.dispatchEvent(new Event('scroll'));
        });

        frame.appendChild(iframe);

        pdfFrames.set(frameId, {
            container: frame,
            pages: [],
            objectUrl
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

export function disposeSecurePdfViewer(sessionId) {
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

    if (state.watermarkElement && state.watermarkElement.parentElement) {
        state.watermarkElement.parentElement.removeChild(state.watermarkElement);
    }

    sessions.delete(sessionId);
}

export function notifyPdfPage(sessionId, pageNumber) {
    const state = sessions.get(sessionId);
    if (!state) {
        return;
    }

    state.lastReportedPage = pageNumber;
    state.hasInitialPageReport = true;
    sendEvent(state, 'PageView', pageNumber, { pageNumber });
}

export function downloadFile(fileName, base64Data) {
    const link = document.createElement('a');
    link.href = `data:application/octet-stream;base64,${base64Data}`;
    link.download = fileName || 'archivo';
    link.style.display = 'none';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}
