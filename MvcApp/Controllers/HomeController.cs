using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using MvcApp.Models;
namespace MvcApp.Controllers;

public sealed class HomeController(CatalogService catalog) : Controller
{
    public async Task<IActionResult> Index(string? q, int? categoryId, int page = 1)
    {
        page = Math.Clamp(page, 1, 10000);
        var total = await catalog.CountAsync(q, categoryId);
        var vm = new CatalogVm { Query = q, CategoryId = categoryId, Page = page, TotalPages = Math.Max(1, (int)Math.Ceiling(total / 12.0)), Categories = await catalog.CategoriesAsync(), Products = await catalog.SearchAsync(q, categoryId, page) };
        return View(vm);
    }
    public async Task<IActionResult> Product(int id)
    {
        var item = await catalog.GetAsync(id);
        return item is null ? NotFound() : View(item);
    }
    public IActionResult Error() => View();
}
