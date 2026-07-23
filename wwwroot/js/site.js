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
    const emptyClearButton = document.querySelector("#clear-filters-empty");
    const resultCount = document.querySelector("#movie-result-count");
    const movieRows = document.querySelectorAll("[data-movie-row]");
    if (resultCount) {
        const label = movieRows.length === 1 ? "film" : "films";
        resultCount.textContent = `${movieRows.length} ${label} on your shelf`;
    }
    const clearFilters = () => {
        if (!filterForm)
            return;
        filterForm.reset();
        window.location.assign(filterForm.action);
    };
    clearButton?.addEventListener("click", clearFilters);
    emptyClearButton?.addEventListener("click", clearFilters);
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
function initializeNavigation() {
    const toggle = document.querySelector("[data-nav-toggle]");
    const menu = document.querySelector("[data-nav-menu]");
    toggle?.addEventListener("click", () => {
        const isOpen = menu?.classList.toggle("is-open") ?? false;
        toggle.setAttribute("aria-expanded", isOpen.toString());
    });
}
document.addEventListener("DOMContentLoaded", () => {
    initializeNavigation();
    initializeDeleteConfirmation();
    initializeMovieFilters();
});
