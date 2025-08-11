// Culture management for Blazor WebAssembly
window.cultureInfo = {
    get: function () {
        return localStorage.getItem('selectedCulture') || 'en';
    },
    set: function (culture) {
        localStorage.setItem('selectedCulture', culture);
    }
}; 