// Silme gibi geri alınamaz işlemlerden önce onay iste.
document.addEventListener('submit', function (e) {
    const message = e.target.getAttribute('data-confirm');
    if (message && !window.confirm(message)) {
        e.preventDefault();
    }
});
