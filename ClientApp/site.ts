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
  const emptyClearButton = document.querySelector<HTMLButtonElement>("#clear-filters-empty");
  const resultCount = document.querySelector<HTMLElement>("#movie-result-count");
  const movieRows = document.querySelectorAll<HTMLTableRowElement>("[data-movie-row]");

  if (resultCount) {
    const label = movieRows.length === 1 ? "film" : "films";
    resultCount.textContent = `${movieRows.length} ${label} on your shelf`;
  }

  const clearFilters = (): void => {
    if (!filterForm) return;

    filterForm.reset();
    window.location.assign(filterForm.action);
  };

  clearButton?.addEventListener("click", clearFilters);
  emptyClearButton?.addEventListener("click", clearFilters);

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

function initializeNavigation(): void {
  const toggle = document.querySelector<HTMLButtonElement>("[data-nav-toggle]");
  const menu = document.querySelector<HTMLElement>("[data-nav-menu]");

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
