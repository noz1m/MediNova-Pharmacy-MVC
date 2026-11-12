using System.ComponentModel.DataAnnotations;
using Domain.Entities;
using Domain.DTOs;
namespace MvcApp.Models;
public sealed class CatalogVm
{
 public List<Product> Products { get; set; } = [];
 public List<Category> Categories { get; set; } = [];
 public string? Query { get; set; }
 public int? CategoryId { get; set; }
 public int Page { get; set; } = 1;
 public int TotalPages { get; set; } = 1;
}
public sealed class CheckoutVm
{
 public CheckoutInput Input { get; set; } = new();
 public List<CartDisplayLine> Lines { get; set; } = [];
 public decimal Total => Lines.Sum(x=>x.Subtotal);
}
public sealed class AdminLoginVm
{
 [Required] public string Password { get; set; } = string.Empty;
}
