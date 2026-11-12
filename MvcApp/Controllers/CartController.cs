using System.Text.Json;
using Domain.DTOs;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcApp.Models;
namespace MvcApp.Controllers;

public sealed class CartController(IDbContextFactory<AppDbContext> factory, OrderService orders) : Controller
{
    private const string SessionKey = "medinova-cart-v1";
    private List<CartLine> ReadCart() { try { return JsonSerializer.Deserialize<List<CartLine>>(HttpContext.Session.GetString(SessionKey) ?? "[]") ?? []; } catch (JsonException) { return []; } }
    private void SaveCart(List<CartLine> lines) => HttpContext.Session.SetString(SessionKey, JsonSerializer.Serialize(lines));
    private async Task<List<CartDisplayLine>> DetailsAsync(List<CartLine> cart)
    {
        await using var db = await factory.CreateDbContextAsync();
        var ids = cart.Select(x => x.ProductId).ToList();
        var products = await db.Products.AsNoTracking().Where(x => ids.Contains(x.Id) && x.IsActive).ToDictionaryAsync(x => x.Id);
        return cart.Where(x => products.ContainsKey(x.ProductId)).Select(x => new CartDisplayLine(x.ProductId, products[x.ProductId].Name, products[x.ProductId].Price, x.Quantity, products[x.ProductId].Stock, products[x.ProductId].ImageUrl)).ToList();
    }
    public async Task<IActionResult> Index() => View(new CheckoutVm { Lines = await DetailsAsync(ReadCart()) });
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int id, int quantity = 1)
    {
        await using var db = await factory.CreateDbContextAsync();
        var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
        if (product is null) return NotFound();
        if (product.RequiresPrescription) { TempData["Error"] = "Для рецептурных товаров требуется проверка рецепта фармацевтом."; return RedirectToAction("Product", "Home", new { id }); }
        var cart = ReadCart(); var current = cart.FirstOrDefault(x => x.ProductId == id);
        var next = (current?.Quantity ?? 0) + Math.Clamp(quantity, 1, 99);
        if (next > product.Stock || next > 99) { TempData["Error"] = "Недостаточно товара на складе"; return RedirectToAction("Product", "Home", new { id }); }
        if (current is null) cart.Add(new CartLine(id, next)); else cart[cart.IndexOf(current)] = current with { Quantity = next };
        SaveCart(cart); TempData["Success"] = "Товар добавлен в корзину"; return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Remove(int id) { SaveCart(ReadCart().Where(x => x.ProductId != id).ToList()); return RedirectToAction(nameof(Index)); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutVm vm)
    {
        var cart = ReadCart();
        if (!ModelState.IsValid) { vm.Lines = await DetailsAsync(cart); return View("Index", vm); }
        var result = await orders.PlaceAsync(vm.Input, cart);
        if (!result.Success) { ModelState.AddModelError(string.Empty, result.Error); vm.Lines = await DetailsAsync(cart); return View("Index", vm); }
        HttpContext.Session.Remove(SessionKey);
        return RedirectToAction(nameof(Success), new { id = result.Id });
    }
    public IActionResult Success(int id) { if (id <= 0) return NotFound(); ViewBag.OrderId = id; return View(); }
}
