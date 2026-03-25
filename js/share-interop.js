window.shareInterop = {
    canShare() {
        return !!navigator.share;
    },

    async shareText(title, text) {
        if (!navigator.share) {
            console.warn('Web Share API not supported');
            return;
        }
        try {
            await navigator.share({ title, text });
        } catch (e) {
            // User cancelled or share failed — not an error we need to surface
            if (e.name !== 'AbortError') {
                console.error('Share failed:', e);
            }
        }
    }
};
