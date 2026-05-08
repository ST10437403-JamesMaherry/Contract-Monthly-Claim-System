document.addEventListener('DOMContentLoaded', function () {
    initializeTooltips();
    initializeQuickActions();
    initializeFileList();
});

function initializeTooltips() {
    if (typeof bootstrap === 'undefined') {
        return;
    }

    var tooltipElements = document.querySelectorAll('[data-bs-toggle="tooltip"]');
    tooltipElements.forEach(function (element) {
        new bootstrap.Tooltip(element);
    });
}

function initializeQuickActions() {
    var quickActionCards = document.querySelectorAll('.quick-action-card');

    quickActionCards.forEach(function (card) {
        card.addEventListener('click', function () {
            var action = card.getAttribute('data-action');
            if (action) {
                window.location.href = action;
            }
        });
    });
}

function initializeFileList() {
    var fileInput = document.getElementById('fileUpload');
    var fileList = document.getElementById('fileList');

    if (!fileInput || !fileList) {
        return;
    }

    fileInput.addEventListener('change', function () {
        var files = Array.prototype.slice.call(fileInput.files || []);

        if (files.length === 0) {
            fileList.innerHTML = '';
            return;
        }

        fileList.innerHTML = files.map(function (file) {
            var size = (file.size / 1024 / 1024).toFixed(2);
            return '<div class="soft-panel mb-2"><strong>' + escapeHtml(file.name) + '</strong><span class="text-muted ms-2">' + size + ' MB</span></div>';
        }).join('');
    });
}

function escapeHtml(value) {
    return String(value)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');
}
