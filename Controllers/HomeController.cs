using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MvcMovie.Models;
using MvcMovie.Services;

namespace MvcMovie.Controllers;

public class HomeController(IMovieDiscoveryService movieDiscoveryService) : Controller
{
    public async Task<IActionResult> Index(string? query, CancellationToken cancellationToken)
    {
        var model = await movieDiscoveryService.GetDiscoveryAsync(query, cancellationToken);
        return View(model);
    }

    public async Task<IActionResult> Movie(int id, CancellationToken cancellationToken)
    {
        var movie = await movieDiscoveryService.GetMovieAsync(id, cancellationToken);
        return movie is null ? NotFound() : View(movie);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
