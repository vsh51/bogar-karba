(function () {
    'use strict';

    function formatIsoDateToLocale(isoDate) {
        const [year, month, day] = isoDate.split('-').map(Number);
        if (!year || !month || !day) {
            return isoDate;
        }

        const date = new Date(year, month - 1, day);
        const formatter = new Intl.DateTimeFormat(navigator.language, {
            year: 'numeric',
            month: 'short',
            day: 'numeric'
        });
        return formatter.format(date);
    }

    document.addEventListener('DOMContentLoaded', () => {
        const nodes = document.querySelectorAll('[data-deadline]');
        nodes.forEach(node => {
            const iso = node.getAttribute('data-deadline');
            if (iso) {
                node.textContent = formatIsoDateToLocale(iso);
            }
        });
    });
})();
