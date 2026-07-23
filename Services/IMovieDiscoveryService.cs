using MvcMovie.Models;

namespace MvcMovie.Services;

public interface IMovieDiscoveryService
{
    Task<DiscoveryPageViewModel> GetDiscoveryAsync(string? query, CancellationToken cancellationToken);
    Task<MovieDiscoveryDetailsViewModel?> GetMovieAsync(int id, CancellationToken cancellationToken);
}
