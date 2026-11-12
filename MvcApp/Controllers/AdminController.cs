using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MvcApp.Models;
namespace MvcApp.Controllers;

[Authorize]
public sealed class AdminController(IDbContextFactory<AppDbContext> factory, IConfiguration configuration) : Controller
{
    [AllowAnonymous, HttpGet] public IActionResult Login() => User.Identity?.IsAuthenticated == true ? RedirectToAction(nameof(Index)) : View(new AdminLoginVm());
    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("login")]
    public async Task<IActionResult> Login(AdminLoginVm vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var secret = configuration["Admin:Password"];
        if (string.IsNullOrWhiteSpace(secret) || secret.Length < 14) { ModelState.AddModelError(string.Empty, "Пароль администратора не настроен."); return View(vm); }
        var a = SHA256.HashData(Encoding.UTF8.GetBytes(vm.Password)); var b = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
        if (!CryptographicOperations.FixedTimeEquals(a, b)) { ModelState.AddModelError(string.Empty, "Неверный пароль"); return View(vm); }
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "Administrator"), new Claim(ClaimTypes.Role, "Admin")], CookieAuthenticationDefaults.AuthenticationScheme)));
        return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Logout() { await HttpContext.SignOutAsync(); return RedirectToAction("Index", "Home"); }
    public async Task<IActionResult> Index()
    {
        await using var db = await factory.CreateDbContextAsync();
        ViewBag.Products = await db.Products.CountAsync(); ViewBag.Orders = await db.Orders.CountAsync(); ViewBag.LowStock = await db.Products.CountAsync(x => x.Stock < 5); ViewBag.Total = await db.Orders.Where(x => x.Status != OrderStatus.Cancelled).SumAsync(x => (decimal?)x.Total) ?? 0;
        return View();
    }
    public async Task<IActionResult> Products()
    {
        await using var db = await factory.CreateDbContextAsync();
        return View(await db.Products.AsNoTracking().Include(x => x.Category).Include(x => x.Manufacturer).OrderBy(x => x.Name).ToListAsync());
    }
    [HttpGet]
    public async Task<IActionResult> EditProduct(int? id)
    {
        await using var db = await factory.CreateDbContextAsync();
        await LoadLookups(db);
        if (id is null) return View(new Product());
        var entity = await db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return entity is null ? NotFound() : View(entity);
    }
    private async Task LoadLookups(AppDbContext db)
    {
        ViewBag.Categories = await db.Categories.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
        ViewBag.Manufacturers = await db.Manufacturers.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProduct(Product input)
    {
        await using var db = await factory.CreateDbContextAsync();
        if (!await db.Categories.AnyAsync(x => x.Id == input.CategoryId) || !await db.Manufacturers.AnyAsync(x => x.Id == input.ManufacturerId)) ModelState.AddModelError(string.Empty, "Выберите существующую категорию и производителя");
        ModelState.Remove(nameof(Product.Category)); ModelState.Remove(nameof(Product.Manufacturer)); ModelState.Remove(nameof(Product.OrderItems));
        if (!ModelState.IsValid) { await LoadLookups(db); return View(input); }
        Product entity;
        if (input.Id == 0) { entity = new Product(); db.Products.Add(entity); }
        else { entity = await db.Products.FirstOrDefaultAsync(x => x.Id == input.Id) ?? new Product(); if (entity.Id == 0) return NotFound(); }
        entity.Name = input.Name.Trim(); entity.Description = input.Description; entity.Form = input.Form; entity.Price = input.Price; entity.Stock = input.Stock; entity.CategoryId = input.CategoryId; entity.ManufacturerId = input.ManufacturerId; entity.IsActive = input.IsActive; entity.RequiresPrescription = input.RequiresPrescription; entity.ImageUrl = input.ImageUrl;
        await db.SaveChangesAsync(); TempData["Success"] = "Товар сохранён"; return RedirectToAction(nameof(Products));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleProduct(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        var product = await db.Products.FindAsync(id);
        if (product is null) return NotFound();
        product.IsActive = !product.IsActive;
        await db.SaveChangesAsync();
        TempData["Success"] = product.IsActive ? "Товар снова отображается в каталоге" : "Товар скрыт из каталога";
        return RedirectToAction(nameof(Products));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        var product = await db.Products.FindAsync(id);
        if (product is null) return NotFound();
        product.IsActive = false; await db.SaveChangesAsync(); TempData["Success"] = "Товар снят с публикации"; return RedirectToAction(nameof(Products));
    }
    public async Task<IActionResult> Categories()
    {
        await using var db = await factory.CreateDbContextAsync(); return View(await db.Categories.AsNoTracking().OrderBy(x => x.Name).ToListAsync());
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(string name)
    {
        name = (name ?? string.Empty).Trim(); if (name.Length is < 2 or > 90) { TempData["Error"] = "Название категории должно быть от 2 до 90 символов"; return RedirectToAction(nameof(Categories)); }
        await using var db = await factory.CreateDbContextAsync(); if (await db.Categories.AnyAsync(x => x.Name == name)) { TempData["Error"] = "Категория уже существует"; return RedirectToAction(nameof(Categories)); }
        db.Categories.Add(new Category { Name = name }); await db.SaveChangesAsync(); TempData["Success"] = "Категория создана"; return RedirectToAction(nameof(Categories));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RenameCategory(int id, string name)
    {
        name = (name ?? string.Empty).Trim();
        if (name.Length is < 2 or > 90) { TempData["Error"] = "Недопустимое название"; return RedirectToAction(nameof(Categories)); }
        await using var db = await factory.CreateDbContextAsync(); var item = await db.Categories.FindAsync(id); if (item is null) return NotFound();
        if (await db.Categories.AnyAsync(x => x.Id != id && x.Name == name)) { TempData["Error"] = "Название занято"; return RedirectToAction(nameof(Categories)); }
        item.Name = name; await db.SaveChangesAsync(); TempData["Success"] = "Категория обновлена"; return RedirectToAction(nameof(Categories));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        await using var db = await factory.CreateDbContextAsync(); var item = await db.Categories.FindAsync(id); if (item is null) return NotFound();
        if (await db.Products.AnyAsync(x => x.CategoryId == id)) { TempData["Error"] = "Категория используется товарами"; return RedirectToAction(nameof(Categories)); }
        db.Categories.Remove(item); await db.SaveChangesAsync(); TempData["Success"] = "Категория удалена"; return RedirectToAction(nameof(Categories));
    }
    public async Task<IActionResult> Manufacturers()
    {
        await using var db = await factory.CreateDbContextAsync(); return View(await db.Manufacturers.AsNoTracking().OrderBy(x => x.Name).ToListAsync());
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddManufacturer(string name, string? country)
    {
        name = (name ?? string.Empty).Trim(); if (name.Length is < 2 or > 110 || (country?.Length ?? 0) > 90) { TempData["Error"] = "Проверьте название и страну"; return RedirectToAction(nameof(Manufacturers)); }
        await using var db = await factory.CreateDbContextAsync(); if (await db.Manufacturers.AnyAsync(x => x.Name == name)) { TempData["Error"] = "Производитель уже существует"; return RedirectToAction(nameof(Manufacturers)); }
        db.Manufacturers.Add(new Manufacturer { Name = name, Country = country }); await db.SaveChangesAsync(); TempData["Success"] = "Производитель создан"; return RedirectToAction(nameof(Manufacturers));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RenameManufacturer(int id, string name, string? country)
    {
        name = (name ?? string.Empty).Trim();
        if (name.Length is < 2 or > 110 || (country?.Length ?? 0) > 90) { TempData["Error"] = "Проверьте данные"; return RedirectToAction(nameof(Manufacturers)); }
        await using var db = await factory.CreateDbContextAsync(); var item = await db.Manufacturers.FindAsync(id); if (item is null) return NotFound();
        if (await db.Manufacturers.AnyAsync(x => x.Id != id && x.Name == name)) { TempData["Error"] = "Название занято"; return RedirectToAction(nameof(Manufacturers)); }
        item.Name = name; item.Country = country; await db.SaveChangesAsync(); TempData["Success"] = "Производитель обновлён"; return RedirectToAction(nameof(Manufacturers));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteManufacturer(int id)
    {
        await using var db = await factory.CreateDbContextAsync(); var item = await db.Manufacturers.FindAsync(id); if (item is null) return NotFound();
        if (await db.Products.AnyAsync(x => x.ManufacturerId == id)) { TempData["Error"] = "Производитель используется товарами"; return RedirectToAction(nameof(Manufacturers)); }
        db.Manufacturers.Remove(item); await db.SaveChangesAsync(); TempData["Success"] = "Производитель удалён"; return RedirectToAction(nameof(Manufacturers));
    }
    public async Task<IActionResult> Orders()
    {
        await using var db = await factory.CreateDbContextAsync(); return View(await db.Orders.AsNoTracking().Include(x => x.Items).OrderByDescending(x => x.CreatedAtUtc).Take(200).ToListAsync());
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Status(int id, OrderStatus status)
    {
        if (!Enum.IsDefined(status)) return BadRequest();
        await using var db = await factory.CreateDbContextAsync(); var entity = await db.Orders.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id); if (entity is null) return NotFound();
        if (entity.Status == OrderStatus.Cancelled || entity.Status == OrderStatus.Completed) { TempData["Error"] = "Завершённый заказ нельзя изменить"; return RedirectToAction(nameof(Orders)); }
        if (status == OrderStatus.Cancelled) { foreach (var item in entity.Items) { if (item.ProductId > 0) { var product = await db.Products.FindAsync(item.ProductId); if (product is not null) product.Stock += item.Quantity; } } }
        entity.Status = status; await db.SaveChangesAsync(); TempData["Success"] = "Статус заказа обновлён"; return RedirectToAction(nameof(Orders));
    }
}
