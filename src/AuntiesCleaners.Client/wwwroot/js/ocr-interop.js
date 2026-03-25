window.ocrInterop = {
    _worker: null,

    async _initWorker() {
        if (this._worker) return this._worker;
        const { createWorker } = Tesseract;
        this._worker = await createWorker('eng');
        return this._worker;
    },

    async extractText(base64ImageData) {
        try {
            const worker = await this._initWorker();
            const imageBytes = Uint8Array.from(atob(base64ImageData), c => c.charCodeAt(0));
            const blob = new Blob([imageBytes]);
            const { data: { text } } = await worker.recognize(blob);

            const businessName = this._parseBusinessName(text);
            const amount = this._parseAmount(text);

            return { businessName, amount, rawText: text };
        } catch (e) {
            console.error('OCR extraction failed:', e);
            return { businessName: '', amount: 0, rawText: '' };
        }
    },

    _parseBusinessName(text) {
        const lines = text.split('\n').map(l => l.trim()).filter(l => l.length > 0);
        return lines.length > 0 ? lines[0] : '';
    },

    _parseAmount(text) {
        const matches = text.match(/\$\s?(\d+[.,]\d{2})/g);
        if (!matches || matches.length === 0) return 0;
        // Take the last dollar amount found (usually the total)
        const last = matches[matches.length - 1];
        const cleaned = last.replace(/[$\s,]/g, '');
        const parsed = parseFloat(cleaned);
        return isNaN(parsed) ? 0 : parsed;
    }
};
