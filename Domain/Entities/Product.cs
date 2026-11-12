using System.ComponentModel.DataAnnotations;
namespace Domain.Entities;

public sealed class Product
{
    public int Id { get; set; }
    [Required, StringLength(160)] public string Name { get; set; } = string.Empty;
    [StringLength(800)] public string? Description { get; set; }
    [StringLength(100)] public string? Form { get; set; }
    [Range(typeof(decimal), "0", "999999999", ParseLimitsInInvariantCulture = true)] public decimal Price { get; set; }
    [Range(0, 1000000)] public int Stock { get; set; }
    public bool RequiresPrescription { get; set; }
    public bool IsActive { get; set; } = true;
    [Url, StringLength(600)] public string? ImageUrl { get; set; }
    [Range(1, int.MaxValue)] public int CategoryId { get; set; }
    public Category? Category { get; set; }
    [Range(1, int.MaxValue)] public int ManufacturerId { get; set; }
    public Manufacturer? Manufacturer { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
