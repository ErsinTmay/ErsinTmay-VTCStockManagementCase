using Microsoft.AspNetCore.Mvc;
using VTCStockManagementCase.Application.Abstractions;
using VTCStockManagementCase.Application.Contracts.Products;

namespace VTCStockManagementCase.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _products;

    public ProductsController(IProductService products)
    {
        _products = products;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var dto = await _products.CreateAsync(request, cancellationToken);
        return Ok(dto);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> List(CancellationToken cancellationToken)
    {
        return Ok(await _products.ListAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var p = await _products.GetByIdAsync(id, cancellationToken);
        if (p == null) return NotFound();
        return Ok(p);
    }
}
