namespace MvcMovie.Models;

public sealed class DiscoveryMovieViewModel
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Overview { get; init; } = string.Empty;
    public string? PosterPath { get; init; }
    public string? BackdropPath { get; init; }
    public DateOnly? ReleaseDate { get; init; }
    public double VoteAverage { get; init; }
    public int VoteCount { get; init; }

    public string ReleaseYear => ReleaseDate?.Year.ToString() ?? "Coming soon";
    public string? PosterUrl => string.IsNullOrWhiteSpace(PosterPath)
        ? null
        : $"https://image.tmdb.org/t/p/w500{PosterPath}";
    public string? BackdropUrl => string.IsNullOrWhiteSpace(BackdropPath)
        ? null
        : $"https://image.tmdb.org/t/p/original{BackdropPath}";
    public string Score => VoteAverage > 0 ? VoteAverage.ToString("0.0") : "NR";
}

public sealed class DiscoveryPageViewModel
{
    public string? SearchQuery { get; init; }
    public DiscoveryMovieViewModel? Featured { get; init; }
    public IReadOnlyList<DiscoveryMovieViewModel> Trending { get; init; } = [];
    public IReadOnlyList<DiscoveryMovieViewModel> Popular { get; init; } = [];
    public IReadOnlyList<DiscoveryMovieViewModel> SearchResults { get; init; } = [];
    public string? Notice { get; init; }
    public bool IsSearch => !string.IsNullOrWhiteSpace(SearchQuery);
}

public sealed class MovieDiscoveryDetailsViewModel
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Overview { get; init; } = string.Empty;
    public string? Tagline { get; init; }
    public string? PosterPath { get; init; }
    public string? BackdropPath { get; init; }
    public DateOnly? ReleaseDate { get; init; }
    public double VoteAverage { get; init; }
    public int VoteCount { get; init; }
    public int? RuntimeMinutes { get; init; }
    public IReadOnlyList<string> Genres { get; init; } = [];
    public string? Certification { get; init; }
    public string? Director { get; init; }
    public string? Actors { get; init; }
    public string? Awards { get; init; }
    public string? BoxOffice { get; init; }
    public string? Metascore { get; init; }

    public string ReleaseYear => ReleaseDate?.Year.ToString() ?? "TBA";
    public string Runtime => RuntimeMinutes is > 0
        ? $"{RuntimeMinutes / 60}h {RuntimeMinutes % 60}m"
        : "Runtime unavailable";
    public string? PosterUrl => string.IsNullOrWhiteSpace(PosterPath)
        ? null
        : $"https://image.tmdb.org/t/p/w500{PosterPath}";
    public string? BackdropUrl => string.IsNullOrWhiteSpace(BackdropPath)
        ? null
        : $"https://image.tmdb.org/t/p/original{BackdropPath}";
}
