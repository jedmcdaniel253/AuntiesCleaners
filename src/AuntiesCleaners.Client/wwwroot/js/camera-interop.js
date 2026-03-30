window.cameraInterop = {
    clickElement(element) {
        element.click();
    },

    async readFileAsBytes(inputElement) {
        const file = inputElement.files[0];
        if (!file) return null;
        const buffer = await file.arrayBuffer();
        return new Uint8Array(buffer);
    },

    getFileName(inputElement) {
        const file = inputElement.files[0];
        return file ? file.name : '';
    }
};
