// Kim Long EcoStay - File Download Helper
window.downloadFileFromText = function (filename, content, mimeType) {
    var blob = new Blob([content], { type: mimeType || 'text/csv;charset=utf-8;' });
    var link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(link.href);
};
