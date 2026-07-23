type ConfirmableForm = HTMLFormElement & {
  dataset: DOMStringMap & { confirm?: string };
};

function initializeDeleteConfirmation(): void {
  const forms = document.querySelectorAll<ConfirmableForm>("form[data-confirm]");

  forms.forEach((form) => {
    form.addEventListener("submit", (event: SubmitEvent) => {
      const message = form.dataset.confirm ?? "Are you sure?";

      if (!window.confirm(message)) {
        event.preventDefault();
      }
    });
  });
}

function initializeMovieFilters(): void {
  const searchInput = document.querySelector<HTMLInputElement>("#movie-search");
  const filterForm = document.querySelector<HTMLFormElement>("#movie-filter-form");
  const clearButton = document.querySelector<HTMLButtonElement>("#clear-filters");
  const resultCount = document.querySelector<HTMLElement>("#movie-result-count");
  const movieRows = document.querySelectorAll<HTMLTableRowElement>("[data-movie-row]");

  if (resultCount) {
    const label = movieRows.length === 1 ? "movie" : "movies";
    resultCount.textContent = `${movieRows.length} ${label} shown`;
  }

  clearButton?.addEventListener("click", () => {
    if (!filterForm) return;

    filterForm.reset();
    window.location.assign(filterForm.action);
  });

  document.addEventListener("keydown", (event: KeyboardEvent) => {
    const target = event.target as HTMLElement | null;
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
