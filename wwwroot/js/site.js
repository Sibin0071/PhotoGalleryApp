document.addEventListener('DOMContentLoaded', function () {

    /* ================= IMAGE MODAL ================= */

    const imageModal = document.getElementById('imageModal');
    const modalImage = document.getElementById('modalImage');
    const imageLinks = Array.from(document.querySelectorAll('[data-img-url]'));
    const prevImageBtn = document.getElementById('prevImageBtn');
    const nextImageBtn = document.getElementById('nextImageBtn');

    let currentImageIndex = -1;

    if (imageLinks.length && modalImage) {
        imageLinks.forEach((img, index) => {
            img.addEventListener('click', function () {
                modalImage.src = this.getAttribute('data-img-url');
                currentImageIndex = index;
            });
        });

        prevImageBtn?.addEventListener('click', () => {
            if (currentImageIndex > 0) {
                currentImageIndex--;
                modalImage.src = imageLinks[currentImageIndex].getAttribute('data-img-url');
            }
        });

        nextImageBtn?.addEventListener('click', () => {
            if (currentImageIndex < imageLinks.length - 1) {
                currentImageIndex++;
                modalImage.src = imageLinks[currentImageIndex].getAttribute('data-img-url');
            }
        });
    }

    /* ================= VIDEO MODAL ================= */

    const videoModal = document.getElementById('videoModal');
    const modalVideo = document.getElementById('modalVideo');
    const modalVideoSource = document.getElementById('modalVideoSource');
    const prevVideoBtn = document.getElementById('prevVideoBtn');
    const nextVideoBtn = document.getElementById('nextVideoBtn');
    const videoThumbnails = Array.from(document.querySelectorAll('[data-video-url]'));

    let currentVideoIndex = 0;

    if (videoModal && modalVideo && modalVideoSource) {

        const videoModalInstance = bootstrap.Modal.getOrCreateInstance(videoModal);

        videoThumbnails.forEach((thumb, index) => {
            thumb.addEventListener('click', function (e) {
                e.preventDefault();

                const videoUrl = this.getAttribute('data-video-url');

                modalVideo.pause();
                modalVideo.currentTime = 0;
                modalVideoSource.src = videoUrl;
                modalVideo.load();

                currentVideoIndex = index;
                videoModalInstance.show();
            });
        });

        nextVideoBtn?.addEventListener('click', () => {
            if (currentVideoIndex < videoThumbnails.length - 1) {
                currentVideoIndex++;

                modalVideo.pause();
                modalVideo.currentTime = 0;

                modalVideoSource.src =
                    videoThumbnails[currentVideoIndex].getAttribute('data-video-url');

                modalVideo.load();
            }
        });

        prevVideoBtn?.addEventListener('click', () => {
            if (currentVideoIndex > 0) {
                currentVideoIndex--;

                modalVideo.pause();
                modalVideo.currentTime = 0;

                modalVideoSource.src =
                    videoThumbnails[currentVideoIndex].getAttribute('data-video-url');

                modalVideo.load();
            }
        });

        videoModal.addEventListener('hidden.bs.modal', function () {
            modalVideo.pause();
            modalVideo.currentTime = 0;
            modalVideoSource.src = '';
            modalVideo.load();
        });
    }
});
