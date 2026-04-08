namespace VTCStockManagementCase.Application.Contracts.Products;

public class CreateProductRequest
{
    public string Sku { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}
