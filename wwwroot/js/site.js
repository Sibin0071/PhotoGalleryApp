document.addEventListener('DOMContentLoaded', function () {
    // 🔷 IMAGE MODAL LOGIC
    const imageModal = document.getElementById('imageModal');
    if (imageModal) {
        imageModal.addEventListener('show.bs.modal', function (event) {
            const trigger = event.relatedTarget;
            const imgUrl = trigger.getAttribute('data-img-url');
            const modalImage = imageModal.querySelector('#modalImage');
            if (modalImage) {
                modalImage.src = imgUrl;
            }
        });
    }

    // 🔷 VIDEO MODAL LOGIC
    const videoModal = document.getElementById('videoModal');
    const modalVideo = document.getElementById('modalVideo');
    const modalVideoSource = document.getElementById('modalVideoSource');
    const closeBtn = document.getElementById('closeVideoModalBtn');
    const prevBtn = document.getElementById('prevVideoBtn');
    const nextBtn = document.getElementById('nextVideoBtn');
    const videoThumbnails = Array.from(document.querySelectorAll('[data-video-url]'));
    let currentIndex = 0;

    if (videoModal && modalVideo && modalVideoSource) {
        const videoModalInstance = new bootstrap.Modal(videoModal);

        // Attach click events to thumbnails
        videoThumbnails.forEach((thumb, index) => {
            thumb.addEventListener('click', function (e) {
                e.preventDefault();
                const videoUrl = this.getAttribute('data-video-url');
                modalVideoSource.src = videoUrl;
                modalVideo.load();
                currentIndex = index;
                videoModalInstance.show();
            });
        });

        // Next Button
        if (nextBtn) {
            nextBtn.addEventListener('click', () => {
                if (currentIndex < videoThumbnails.length - 1) {
                    currentIndex++;
                    const newUrl = videoThumbnails[currentIndex].getAttribute('data-video-url');
                    modalVideoSource.src = newUrl;
                    modalVideo.load();
                }
            });
        }

        // Previous Button
        if (prevBtn) {
            prevBtn.addEventListener('click', () => {
                if (currentIndex > 0) {
                    currentIndex--;
                    const newUrl = videoThumbnails[currentIndex].getAttribute('data-video-url');
                    modalVideoSource.src = newUrl;
                    modalVideo.load();
                }
            });
        }

        // Close Button
        if (closeBtn) {
            closeBtn.addEventListener('click', () => {
                console.log("Close clicked");
                videoModalInstance.hide();
            });
        }

        // Pause and reset video on modal close
        videoModal.addEventListener('hidden.bs.modal', function () {
            modalVideo.pause();
            modalVideo.currentTime = 0;
        });
    }
});
