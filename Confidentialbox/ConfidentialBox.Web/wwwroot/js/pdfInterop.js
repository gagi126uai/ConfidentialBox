window.ConfidentialBox = window.ConfidentialBox || {};

window.ConfidentialBox.renderPdf = function (frameId, base64Content, fileName) {
    var iframe = document.getElementById(frameId);
    if (iframe) {
        iframe.src = "data:application/pdf;base64," + base64Content; // Usa base64 para mostrar el PDF
        iframe.onload = function () {
            console.log("PDF cargado correctamente: " + fileName);
        };
    } else {
        console.error("No se encontró el iframe con el id: " + frameId);
    }
};