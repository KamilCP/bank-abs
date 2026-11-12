// Bank ABS — допоміжні функції інтерфейсу.
// Кнопки з атрибутом data-confirm запитують підтвердження перед відправленням форми.
document.addEventListener('click', function (e) {
    const button = e.target.closest('[data-confirm]');
    if (button && !window.confirm(button.getAttribute('data-confirm'))) {
        e.preventDefault();
        e.stopPropagation();
    }
}, true);

// Кнопки з атрибутом data-print відкривають діалог друку
document.addEventListener('click', function (e) {
    if (e.target.closest('[data-print]')) {
        e.preventDefault();
        window.print();
    }
});

// Списки у формах фільтрів з атрибутом data-autosubmit відправляють форму одразу після вибору
document.addEventListener('change', function (e) {
    const field = e.target.closest('[data-autosubmit]');
    if (field && field.form) {
        field.form.requestSubmit();
    }
});
