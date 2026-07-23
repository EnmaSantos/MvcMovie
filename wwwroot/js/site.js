"use strict";
function initializeDeleteConfirmation() {
    const forms = document.querySelectorAll("form[data-confirm]");
    forms.forEach((form) => {
        form.addEventListener("submit", (event) => {
            const message = form.dataset.confirm ?? "Are you sure?";
            if (!window.confirm(message)) {
                event.preventDefault();
            }
        });
    });
}
function initializeMovieFilters() {
    const searchInput = document.querySelector("#movie-search");
    const filterForm = document.querySelector("#movie-filter-form");
    const clearButton = document.querySelector("#clear-filters");
    const resultCount = document.querySelector("#movie-result-count");
    const movieRows = document.querySelectorAll("[data-movie-row]");
    if (resultCount) {
        const label = movieRows.length === 1 ? "movie" : "movies";
        resultCount.textContent = `${movieRows.length} ${label} shown`;
    }
    clearButton?.addEventListener("click", () => {
        if (!filterForm)
            return;
        filterForm.reset();
        window.location.assign(filterForm.action);
    });
    document.addEventListener("keydown", (event) => {
        const target = event.target;
        const isTyping = target?.matches("input, textarea, select, [contenteditable='true']") ?? false;
        if (event.key === "/" && !isTyping && searchInput) {
            event.preventDefault();
            searchInput.focus();
            searchInput.select();
        }
    });
}
document.addEventListener("DOMContentLoaded", () => {
    initializeDeleteConfirmation();
    initializeMovieFilters();
});
