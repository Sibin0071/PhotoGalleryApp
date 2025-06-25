// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
const imageModal = document.getElementById('imageModal');
if (imageModal) {
    imageModal.addEventListener('show.bs.modal', function (event) {
        const trigger = event.relatedTarget;
        const imgUrl = trigger.getAttribute('data-img-url');
        const modalImage = imageModal.querySelector('#modalImage');
        modalImage.src = imgUrl;
    });
}

