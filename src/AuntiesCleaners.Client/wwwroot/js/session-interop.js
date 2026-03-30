window.sessionInterop = {
    save(key, value) {
        localStorage.setItem(key, value);
    },
    load(key) {
        return localStorage.getItem(key);
    },
    destroy(key) {
        localStorage.removeItem(key);
    }
};
