document.addEventListener('DOMContentLoaded', function () {
    // 🔷 IMAGE MODAL LOGIC
    const imageModal = document.getElementById('imageModal');
    const modalImage = document.getElementById('modalImage');
    const imageLinks = Array.from(document.querySelectorAll('[data-img-url]'));
    const prevImageBtn = document.getElementById('prevImageBtn');
    const nextImageBtn = document.getElementById('nextImageBtn');
    let currentImageIndex = -1;

    if (imageLinks.length && modalImage && prevImageBtn && nextImageBtn) {
        imageLinks.forEach((img, index) => {
            img.addEventListener('click', function () {
                modalImage.src = this.getAttribute('data-img-url');
                currentImageIndex = index;
            });
        });

        prevImageBtn.addEventListener('click', () => {
            if (currentImageIndex > 0) {
                currentImageIndex--;
                modalImage.src = imageLinks[currentImageIndex].getAttribute('data-img-url');
            }
        });

        nextImageBtn.addEventListener('click', () => {
            if (currentImageIndex < imageLinks.length - 1) {
                currentImageIndex++;
                modalImage.src = imageLinks[currentImageIndex].getAttribute('data-img-url');
            }
        });
    }

    // 🔷 VIDEO MODAL LOGIC
    const videoModal = document.getElementById('videoModal');
    const modalVideo = document.getElementById('modalVideo');
    const modalVideoSource = document.getElementById('modalVideoSource');
    const prevVideoBtn = document.getElementById('prevVideoBtn');
    const nextVideoBtn = document.getElementById('nextVideoBtn');
    const closeVideoBtn = document.getElementById('customCloseVideoBtn'); // updated id
    const videoThumbnails = Array.from(document.querySelectorAll('[data-video-url]'));
    let currentVideoIndex = 0;

    if (videoModal && modalVideo && modalVideoSource) {
        videoThumbnails.forEach((thumb, index) => {
            thumb.addEventListener('click', function () {
                const videoUrl = this.getAttribute('data-video-url');
                modalVideoSource.src = videoUrl;
                modalVideo.load();
                currentVideoIndex = index;
            });
        });

        if (nextVideoBtn) {
            nextVideoBtn.addEventListener('click', () => {
                if (currentVideoIndex < videoThumbnails.length - 1) {
                    currentVideoIndex++;
                    const newUrl = videoThumbnails[currentVideoIndex].getAttribute('data-video-url');
                    modalVideoSource.src = newUrl;
                    modalVideo.load();
                }
            });
        }

        if (prevVideoBtn) {
            prevVideoBtn.addEventListener('click', () => {
                if (currentVideoIndex > 0) {
                    currentVideoIndex--;
                    const newUrl = videoThumbnails[currentVideoIndex].getAttribute('data-video-url');
                    modalVideoSource.src = newUrl;
                    modalVideo.load();
                }
            });
        }

        // ✅ Manual modal close handler
        if (closeVideoBtn) {
            closeVideoBtn.addEventListener('click', () => {
                // Pause video and reset
                modalVideo.pause();
                modalVideo.currentTime = 0;
                modalVideoSource.src = '';

                // Force hide modal
                videoModal.classList.remove('show');
                videoModal.style.display = 'none';
                videoModal.setAttribute('aria-hidden', 'true');
                videoModal.removeAttribute('aria-modal');
                document.body.classList.remove('modal-open');
                document.body.style = '';
                const backdrop = document.querySelector('.modal-backdrop');
                if (backdrop) backdrop.remove();
            });
        }

        // Also reset on modal close (for any other method)
        videoModal.addEventListener('hidden.bs.modal', function () {
            modalVideo.pause();
            modalVideo.currentTime = 0;
            modalVideoSource.src = '';
        });
    }
});
