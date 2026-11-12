using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace Infrastructure.Services;
public sealed class CatalogService(IDbContextFactory<AppDbContext> factory)
{
 public async Task<List<Product>> SearchAsync(string? term, int? categoryId, int page, int pageSize = 12)
 {
  await using var db = await factory.CreateDbContextAsync();
  var query = db.Products.AsNoTracking().Include(x => x.Category).Include(x => x.Manufacturer).Where(x => x.IsActive);
  if (!string.IsNullOrWhiteSpace(term)) { var keyword = term.Trim(); query = query.Where(x => EF.Functions.ILike(x.Name, $"%{keyword}%")); }
  if (categoryId is > 0) query = query.Where(x => x.CategoryId == categoryId);
  return await query.OrderBy(x => x.Name).Skip((Math.Max(page, 1)-1)*pageSize).Take(pageSize).ToListAsync();
 }
 public async Task<int> CountAsync(string? term, int? categoryId)
 {
  await using var db = await factory.CreateDbContextAsync();
  var q = db.Products.AsNoTracking().Where(x => x.IsActive);
  if (!string.IsNullOrWhiteSpace(term)) { var keyword=term.Trim(); q=q.Where(x => EF.Functions.ILike(x.Name, $"%{keyword}%")); }
  if(categoryId is > 0) q=q.Where(x => x.CategoryId == categoryId);
  return await q.CountAsync();
 }
 public async Task<Product?> GetAsync(int id)
 {
  await using var db = await factory.CreateDbContextAsync();
  return await db.Products.AsNoTracking().Include(x => x.Category).Include(x => x.Manufacturer).FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
 }
 public async Task<List<Category>> CategoriesAsync()
 {
  await using var db = await factory.CreateDbContextAsync();
  return await db.Categories.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
 }
}
