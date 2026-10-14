namespace EFCoreAdvanced.Models;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? CategoryName { get; set; }
}

public class UpdateProductRequest
{
    public string? Name { get; set; }
    public decimal? Price { get; set; }
}
