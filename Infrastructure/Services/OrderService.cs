using System.Data;
using Domain.DTOs;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace Infrastructure.Services;
public sealed class OrderService(IDbContextFactory<AppDbContext> factory)
{
 public async Task<(bool Success, string Error, int Id)> PlaceAsync(CheckoutInput input, IReadOnlyList<CartLine> cart)
 {
  if (cart.Count == 0) return (false,"Корзина пуста",0);
  await using var db = await factory.CreateDbContextAsync();
  await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
  var amounts = cart.GroupBy(x=>x.ProductId).Select(g=>new CartLine(g.Key,g.Sum(x=>x.Quantity))).ToList();
  if (amounts.Any(x=>x.Quantity <= 0 || x.Quantity > 99)) return (false,"Недопустимое количество",0);
  var ids = amounts.Select(x=>x.ProductId).ToList();
  var products = await db.Products.Where(x=>ids.Contains(x.Id)).ToDictionaryAsync(x=>x.Id);
  if (products.Count != amounts.Count) return (false,"Один из товаров больше недоступен",0);
  var order = new Order { CustomerName=input.CustomerName.Trim(), Phone=input.Phone.Trim(), Email=input.Email?.Trim(), Comment=input.Comment?.Trim() };
  foreach(var line in amounts)
  {
   var product = products[line.ProductId];
   if (!product.IsActive || product.RequiresPrescription) return (false,"Рецептурный товар требует проверки фармацевтом и не оформляется онлайн",0);
   if (product.Stock < line.Quantity) return (false,$"Недостаточно товара: {product.Name}",0);
   product.Stock -= line.Quantity;
   order.Items.Add(new OrderItem { ProductId=product.Id, ProductName=product.Name, Quantity=line.Quantity, UnitPrice=product.Price });
   order.Total += product.Price * line.Quantity;
  }
  db.Orders.Add(order);
  try { await db.SaveChangesAsync(); await tx.CommitAsync(); return (true,string.Empty,order.Id); }
  catch(DbUpdateException) { await tx.RollbackAsync(); return (false,"Не удалось сохранить заявку. Повторите попытку.",0); }
 }
}
