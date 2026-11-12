using System.ComponentModel.DataAnnotations;
namespace Domain.Entities;

public sealed class Category
{
    public int Id { get; set; }
    [Required, StringLength(90)] public string Name { get; set; } = string.Empty;
    [StringLength(200)] public string? Description { get; set; }
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
