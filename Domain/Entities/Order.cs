using System.ComponentModel.DataAnnotations;
namespace Domain.Entities;

public enum OrderStatus { New, Confirmed, Ready, Completed, Cancelled }
public sealed class Order
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string CustomerName { get; set; } = string.Empty;
    [Required, Phone, StringLength(30)] public string Phone { get; set; } = string.Empty;
    [EmailAddress, StringLength(160)] public string? Email { get; set; }
    [StringLength(500)] public string? Comment { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public OrderStatus Status { get; set; } = OrderStatus.New;
    public decimal Total { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
public sealed class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int? ProductId { get; set; }
    public Product? Product { get; set; }
    [Required, StringLength(160)] public string ProductName { get; set; } = string.Empty;
    [Range(1, 99)] public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
