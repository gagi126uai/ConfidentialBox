(function () {
    const ns = (window.ConfidentialBox = window.ConfidentialBox || {});

    // Si el visor avanzado ya está disponible (confidentialbox.js), no lo sobrescribimos.
    if (ns.initSecurePdfViewer && ns.renderPdf && ns.disposeSecurePdfViewer && ns.notifyPdfPage) {
        return;
    }

    ns._viewers = ns._viewers || {};

    function base64ToBlob(base64, contentType) {
        const byteChars = atob(base64);
        const byteArrays = [];
        const sliceSize = 8192;
        for (let offset = 0; offset < byteChars.length; offset += sliceSize) {
            const slice = byteChars.slice(offset, offset + sliceSize);
            const byteNumbers = new Array(slice.length);
            for (let i = 0; i < slice.length; i++) byteNumbers[i] = slice.charCodeAt(i);
            byteArrays.push(new Uint8Array(byteNumbers));
        }
        return new Blob(byteArrays, { type: contentType || "application/pdf" });
    }

    // === requerido por tu C# ===
    ns.ensureSecureViewerReady = function () {
        // no-op: existir es suficiente para que la invocación no falle
        return true;
    };

    ns.renderPdf = function (frameId, base64Content, fileName) {
        const host = document.getElementById(frameId);
        if (!host) { console.warn("renderPdf: no host", frameId); return; }

        // limpia anterior
        try {
            if (host.dataset && host.dataset.objectUrl) {
                URL.revokeObjectURL(host.dataset.objectUrl);
                host.dataset.objectUrl = "";
            }
        } catch { /* ignore */ }
        host.innerHTML = "";

        // crea iframe con blob (mejor que data: para archivos grandes)
        const blob = base64ToBlob(base64Content, "application/pdf");
        const url = URL.createObjectURL(blob);
        if (host.dataset) host.dataset.objectUrl = url;

        const iframe = document.createElement("iframe");
        iframe.src = url;
        iframe.title = fileName || "PDF";
        iframe.style.width = "100%";
        iframe.style.height = "100%";
        iframe.style.border = "0";
        host.appendChild(iframe);
    };

    ns.disposePdfFrame = function (frameId) {
        const host = document.getElementById(frameId);
        if (!host) return;
        try {
            if (host.dataset && host.dataset.objectUrl) {
                URL.revokeObjectURL(host.dataset.objectUrl);
                host.dataset.objectUrl = "";
            }
            host.innerHTML = "";
        } catch (e) {
            console.warn("disposePdfFrame:", e);
        }
    };

    ns.initSecurePdfViewer = function (containerId, opts) {
        // guarda mínima metadata de la sesión, por si querés limpiar después
        if (!opts || !opts.sessionId) return;

        const viewerState = {
            containerId,
            frameId: opts.frameId,
            toolbarId: opts.toolbarId,
            cleanup: []
            // acá podrías agregar listeners o UI del toolbar si querés
        };

        const container = document.getElementById(containerId);
        const settings = opts?.settings || {};
        const disableMenu = opts.disableContextMenu || settings.disableContextMenu;
        const disableSelection = settings.disableTextSelection;
        const padding = settings.viewerPadding || '1.5rem';
        const background = settings.backgroundColor || '#0f172a';
        const watermarkEnabled = !!opts.watermarkText && (settings.forceGlobalWatermark || opts.hasWatermark);

        if (container) {
            container.dataset.contextMenuBlocked = disableMenu ? 'true' : 'false';
            container.style.userSelect = disableSelection ? 'none' : 'auto';
            container.style.webkitUserSelect = disableSelection ? 'none' : 'auto';
            container.style.background = background;
            container.style.position = container.style.position || 'relative';
            container.style.overflow = 'hidden';

            const style = document.createElement('style');
            style.textContent = `#${containerId}{height:calc(100vh - 220px);}#${containerId} iframe{padding:${padding};background:${background};}`;
            document.head.appendChild(style);
            viewerState.cleanup.push(() => style.remove());

            if (disableMenu) {
                const handler = (event) => {
                    const inside = container.contains(event.target);
                    if (inside) {
                        event.preventDefault();
                    }
                };
                document.addEventListener('contextmenu', handler, true);
                viewerState.cleanup.push(() => document.removeEventListener('contextmenu', handler, true));
            }

            if (watermarkEnabled) {
                const wm = document.createElement('div');
                wm.className = 'secure-pdf-watermark-layer';
                wm.style.pointerEvents = 'none';
                wm.style.position = 'absolute';
                wm.style.inset = '0';
                wm.style.display = 'grid';
                wm.style.gridTemplateColumns = 'repeat(auto-fill, minmax(240px,1fr))';
                wm.style.gridAutoRows = 'minmax(160px, 1fr)';
                wm.style.gap = '2rem';
                wm.style.alignContent = 'space-evenly';
                wm.style.justifyContent = 'space-evenly';
                wm.style.padding = '2rem';
                wm.style.alignItems = 'center';
                wm.style.justifyItems = 'center';
                const rotation = typeof opts?.watermarkStyle?.rotation === 'number'
                    ? `${opts.watermarkStyle.rotation}deg`
                    : '-15deg';

                wm.style.transform = rotation;
                wm.style.color = opts?.watermarkStyle?.color || 'rgba(220,53,69,0.18)';
                wm.style.opacity = opts?.watermarkStyle?.opacity ?? 0.12;
                wm.style.fontSize = (opts?.watermarkStyle?.fontSize || 48) + 'px';
                wm.style.fontFamily = settings.fontFamily || "'Inter','Segoe UI',sans-serif";
                wm.style.zIndex = '5';
                wm.style.textAlign = 'center';
                wm.style.whiteSpace = 'pre-wrap';
                wm.style.wordBreak = 'break-word';

                const watermarkRepeats = Math.max(24, Math.ceil((container.clientHeight || 800) / 80));
                for (let i = 0; i < watermarkRepeats; i++) {
                    const span = document.createElement('span');
                    span.textContent = opts.watermarkText;
                    span.style.opacity = '0.9';
                    wm.appendChild(span);
                }

                container.appendChild(wm);
                viewerState.cleanup.push(() => wm.remove());
            }
        }

        ns._viewers[opts.sessionId] = viewerState;
    };

    ns.disposeSecurePdfViewer = function (sessionId) {
        const v = ns._viewers[sessionId];
        if (!v) return;
        // limpia toolbar si lo hubieras poblado
        const tb = document.getElementById(v.toolbarId);
        if (tb) tb.innerHTML = "";

        if (Array.isArray(v.cleanup)) {
            v.cleanup.forEach((fn) => {
                try { fn(); } catch { /* ignore */ }
            });
        }

        delete ns._viewers[sessionId];
    };

    ns.notifyPdfPage = function (_sessionId, _page) {
        // hook opcional; dejar como no-op evita errores de interop
    };
})();
