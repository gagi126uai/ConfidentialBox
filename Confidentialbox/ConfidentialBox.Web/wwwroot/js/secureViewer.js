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
        iframe.style.height = "80vh";
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
        ns._viewers[opts.sessionId] = {
            containerId,
            frameId: opts.frameId,
            toolbarId: opts.toolbarId
            // acá podrías agregar listeners o UI del toolbar si querés
        };
    };

    ns.disposeSecurePdfViewer = function (sessionId) {
        const v = ns._viewers[sessionId];
        if (!v) return;
        // limpia toolbar si lo hubieras poblado
        const tb = document.getElementById(v.toolbarId);
        if (tb) tb.innerHTML = "";
        delete ns._viewers[sessionId];
    };

    ns.notifyPdfPage = function (_sessionId, _page) {
        // hook opcional; dejar como no-op evita errores de interop
    };
})();
