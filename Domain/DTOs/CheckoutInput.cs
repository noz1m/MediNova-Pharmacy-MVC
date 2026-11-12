using System.ComponentModel.DataAnnotations;
namespace Domain.DTOs;

public sealed class CheckoutInput
{
    [Required(ErrorMessage = "Укажите имя"), StringLength(100)] public string CustomerName { get; set; } = string.Empty;
    [Required(ErrorMessage = "Укажите телефон"), Phone, StringLength(30)] public string Phone { get; set; } = string.Empty;
    [EmailAddress, StringLength(160)] public string? Email { get; set; }
    [StringLength(500)] public string? Comment { get; set; }
}
public sealed record CartLine(int ProductId, int Quantity);
public sealed record CartDisplayLine(int ProductId, string Name, decimal Price, int Quantity, int Stock, string? ImageUrl)
{
    public decimal Subtotal => Price * Quantity;
}
