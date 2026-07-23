using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using MvcMovie.Models;

namespace MvcMovie.Services;

public sealed class MovieDiscoveryService(
    HttpClient tmdbClient,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    IMemoryCache cache,
    ILogger<MovieDiscoveryService> logger) : IMovieDiscoveryService
{
    private readonly string? _readToken = configuration["TMDB_API_READ_KEY"];
    private readonly string? _apiKey = configuration["TMDB_API_KEY"];
    private readonly string? _omdbApiKey = configuration["OMDB_API_KEY"];

    public async Task<DiscoveryPageViewModel> GetDiscoveryAsync(
        string? query,
        CancellationToken cancellationToken)
    {
        query = string.IsNullOrWhiteSpace(query) ? null : query.Trim();

        if (!HasTmdbCredentials())
        {
            return new DiscoveryPageViewModel
            {
                SearchQuery = query,
                Notice = "Add a TMDB key to .env to load live movie discovery."
            };
        }

        try
        {
            if (query is not null)
            {
                var search = await GetListAsync(
                    $"search/movie?query={Uri.EscapeDataString(query)}&include_adult=false&language=en-US&page=1",
                    cancellationToken);

                return new DiscoveryPageViewModel
                {
                    SearchQuery = query,
                    SearchResults = search
                };
            }

            var trendingTask = GetCachedListAsync(
                "tmdb:trending:week",
                "trending/movie/week?language=en-US",
                TimeSpan.FromMinutes(20),
                cancellationToken);
            var popularTask = GetCachedListAsync(
                "tmdb:popular",
                "movie/popular?language=en-US&page=1",
                TimeSpan.FromMinutes(20),
                cancellationToken);

            await Task.WhenAll(trendingTask, popularTask);

            var trending = await trendingTask;
            var popular = await popularTask;

            return new DiscoveryPageViewModel
            {
                Featured = trending.FirstOrDefault(movie => movie.BackdropUrl is not null),
                Trending = trending.Take(10).ToList(),
                Popular = popular.Take(10).ToList()
            };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Unavailable(query);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "TMDB discovery request failed.");
            return Unavailable(query);
        }
    }

    public async Task<MovieDiscoveryDetailsViewModel?> GetMovieAsync(
        int id,
        CancellationToken cancellationToken)
    {
        if (!HasTmdbCredentials())
        {
            return null;
        }

        try
        {
            var details = await GetTmdbAsync<TmdbMovieDetails>($"movie/{id}?language=en-US", cancellationToken);
            if (details is null)
            {
                return null;
            }

            var omdb = await GetOmdbAsync(details.ImdbId, cancellationToken);

            return new MovieDiscoveryDetailsViewModel
            {
                Id = details.Id,
                Title = details.Title,
                Overview = !string.IsNullOrWhiteSpace(omdb?.Plot) ? omdb.Plot : details.Overview,
                Tagline = details.Tagline,
                PosterPath = details.PosterPath,
                BackdropPath = details.BackdropPath,
                ReleaseDate = ParseDate(details.ReleaseDate),
                VoteAverage = details.VoteAverage,
                VoteCount = details.VoteCount,
                RuntimeMinutes = details.Runtime,
                Genres = details.Genres.Select(genre => genre.Name).ToList(),
                Certification = ValueOrNull(omdb?.Rated),
                Director = ValueOrNull(omdb?.Director),
                Actors = ValueOrNull(omdb?.Actors),
                Awards = ValueOrNull(omdb?.Awards),
                BoxOffice = ValueOrNull(omdb?.BoxOffice),
                Metascore = ValueOrNull(omdb?.Metascore)
            };
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Movie detail request failed for TMDB movie {MovieId}.", id);
            return null;
        }
    }

    private async Task<IReadOnlyList<DiscoveryMovieViewModel>> GetCachedListAsync(
        string cacheKey,
        string path,
        TimeSpan duration,
        CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(cacheKey, out IReadOnlyList<DiscoveryMovieViewModel>? cached) && cached is not null)
        {
            return cached;
        }

        var movies = await GetListAsync(path, cancellationToken);
        cache.Set(cacheKey, movies, duration);
        return movies;
    }

    private async Task<IReadOnlyList<DiscoveryMovieViewModel>> GetListAsync(
        string path,
        CancellationToken cancellationToken)
    {
        var response = await GetTmdbAsync<TmdbListResponse>(path, cancellationToken);
        return response?.Results
            .Where(movie => !string.IsNullOrWhiteSpace(movie.Title))
            .Select(movie => new DiscoveryMovieViewModel
            {
                Id = movie.Id,
                Title = movie.Title,
                Overview = movie.Overview,
                PosterPath = movie.PosterPath,
                BackdropPath = movie.BackdropPath,
                ReleaseDate = ParseDate(movie.ReleaseDate),
                VoteAverage = movie.VoteAverage,
                VoteCount = movie.VoteCount
            })
            .ToList() ?? [];
    }

    private async Task<T?> GetTmdbAsync<T>(string path, CancellationToken cancellationToken)
    {
        var separator = path.Contains('?') ? '&' : '?';
        var requestPath = string.IsNullOrWhiteSpace(_readToken) && !string.IsNullOrWhiteSpace(_apiKey)
            ? $"{path}{separator}api_key={Uri.EscapeDataString(_apiKey)}"
            : path;

        using var request = new HttpRequestMessage(HttpMethod.Get, requestPath);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrWhiteSpace(_readToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _readToken);
        }

        using var response = await tmdbClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
    }

    private async Task<OmdbMovie?> GetOmdbAsync(string? imdbId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(imdbId) || string.IsNullOrWhiteSpace(_omdbApiKey))
        {
            return null;
        }

        try
        {
            var client = httpClientFactory.CreateClient("omdb");
            var path = $"?apikey={Uri.EscapeDataString(_omdbApiKey)}&i={Uri.EscapeDataString(imdbId)}&plot=full&r=json";
            var result = await client.GetFromJsonAsync<OmdbMovie>(path, cancellationToken);
            return string.Equals(result?.Response, "True", StringComparison.OrdinalIgnoreCase) ? result : null;
        }
        catch (HttpRequestException exception)
        {
            logger.LogInformation(exception, "OMDb enrichment was unavailable for {ImdbId}.", imdbId);
            return null;
        }
    }

    private bool HasTmdbCredentials() =>
        !string.IsNullOrWhiteSpace(_readToken) || !string.IsNullOrWhiteSpace(_apiKey);

    private static DiscoveryPageViewModel Unavailable(string? query) => new()
    {
        SearchQuery = query,
        Notice = "Live movie data is taking an intermission. Your personal catalog is still available."
    };

    private static DateOnly? ParseDate(string? value) =>
        DateOnly.TryParse(value, out var date) ? date : null;

    private static string? ValueOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) || string.Equals(value, "N/A", StringComparison.OrdinalIgnoreCase)
            ? null
            : value;

    private sealed class TmdbListResponse
    {
        [JsonPropertyName("results")]
        public List<TmdbMovie> Results { get; init; } = [];
    }

    private class TmdbMovie
    {
        [JsonPropertyName("id")]
        public int Id { get; init; }

        [JsonPropertyName("title")]
        public string Title { get; init; } = string.Empty;

        [JsonPropertyName("overview")]
        public string Overview { get; init; } = string.Empty;

        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; init; }

        [JsonPropertyName("backdrop_path")]
        public string? BackdropPath { get; init; }

        [JsonPropertyName("release_date")]
        public string? ReleaseDate { get; init; }

        [JsonPropertyName("vote_average")]
        public double VoteAverage { get; init; }

        [JsonPropertyName("vote_count")]
        public int VoteCount { get; init; }
    }

    private sealed class TmdbMovieDetails : TmdbMovie
    {
        [JsonPropertyName("imdb_id")]
        public string? ImdbId { get; init; }

        [JsonPropertyName("tagline")]
        public string? Tagline { get; init; }

        [JsonPropertyName("runtime")]
        public int? Runtime { get; init; }

        [JsonPropertyName("genres")]
        public List<TmdbGenre> Genres { get; init; } = [];
    }

    private sealed class TmdbGenre
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;
    }

    private sealed class OmdbMovie
    {
        public string? Rated { get; init; }
        public string? Director { get; init; }
        public string? Actors { get; init; }
        public string? Awards { get; init; }
        public string? BoxOffice { get; init; }
        public string? Metascore { get; init; }
        public string? Plot { get; init; }
        public string? Response { get; init; }
    }
}
