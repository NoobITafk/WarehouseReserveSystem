namespace WarehouseReserveSystem.Models;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ColorHex { get; set; } // for badge UI styling, e.g. #3b82f6

    public ICollection<Product> Products { get; set; } = [];
}
