(function () {
    const sessions = new Map();

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

        const state = {
            dotNetRef: options.dotNetRef,
            sessionId,
            handlers: [],
            watermarkElement: null
        };

        sessions.set(sessionId, state);

        container.setAttribute('data-secure-viewer', 'true');
        container.addEventListener('dragstart', (e) => e.preventDefault());

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
    }

    function disposePdfFrame(frameId) {
        const frame = document.getElementById(frameId);
        if (!frame) {
            return;
        }

        const objectUrl = frame.dataset?.objectUrl;
        if (objectUrl) {
            try {
                URL.revokeObjectURL(objectUrl);
            } catch (err) {
                console.warn('No se pudo revocar la URL del PDF', err);
            }
            delete frame.dataset.objectUrl;
        }

        frame.removeAttribute('src');
    }

    function renderPdf(frameId, base64Data) {
        const frame = document.getElementById(frameId);
        if (!frame) {
            throw new Error('No se encontró el iframe del PDF seguro');
        }

        disposePdfFrame(frameId);

        const binary = atob(base64Data);
        const sliceSize = 1024;
        const byteArrays = [];

        for (let offset = 0; offset < binary.length; offset += sliceSize) {
            const slice = binary.slice(offset, offset + sliceSize);
            const byteNumbers = new Array(slice.length);

            for (let i = 0; i < slice.length; i++) {
                byteNumbers[i] = slice.charCodeAt(i);
            }

            byteArrays.push(new Uint8Array(byteNumbers));
        }

        const blob = new Blob(byteArrays, { type: 'application/pdf' });
        const objectUrl = URL.createObjectURL(blob);
        frame.src = objectUrl;
        frame.dataset.objectUrl = objectUrl;
    }

    function disposeSecurePdfViewer(sessionId) {
        const state = sessions.get(sessionId);
        if (!state) {
            return;
        }

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

    function notifyPdfPage(sessionId, pageNumber) {
        const state = sessions.get(sessionId);
        if (!state) {
            return;
        }

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

    window.confidentialBox = window.confidentialBox || {};
    window.confidentialBox.downloadFile = downloadFile;
    window.confidentialBox.initSecurePdfViewer = initSecurePdfViewer;
    window.confidentialBox.disposeSecurePdfViewer = disposeSecurePdfViewer;
    window.confidentialBox.notifyPdfPage = notifyPdfPage;
    window.confidentialBox.renderPdf = renderPdf;
    window.confidentialBox.disposePdfFrame = disposePdfFrame;
})();
