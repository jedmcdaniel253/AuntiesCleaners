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
            if (e.name !== 'AbortError') {
                console.error('Share failed:', e);
            }
        }
    },

    async shareWithFile(title, text, fileBytes, fileName, mimeType) {
        if (!navigator.canShare) {
            // Fall back to text-only share
            return await this.shareText(title, text);
        }
        try {
            const file = new File([new Uint8Array(fileBytes)], fileName, { type: mimeType });
            const shareData = { title, text, files: [file] };
            if (navigator.canShare(shareData)) {
                await navigator.share(shareData);
            } else {
                // Browser can't share files, fall back to text
                await this.shareText(title, text);
            }
        } catch (e) {
            if (e.name !== 'AbortError') {
                console.error('Share with file failed:', e);
            }
        }
    }
};
