using System.ComponentModel.DataAnnotations;
namespace Domain.Entities;

public sealed class Manufacturer
{
    public int Id { get; set; }
    [Required, StringLength(110)] public string Name { get; set; } = string.Empty;
    [StringLength(90)] public string? Country { get; set; }
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
