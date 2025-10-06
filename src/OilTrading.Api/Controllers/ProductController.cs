using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OilTrading.Core.Entities;
using OilTrading.Infrastructure.Data;

namespace OilTrading.Api.Controllers;

[ApiController]
[Route("api/[controller]s")]
public class ProductController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProductController> _logger;

    public ProductController(ApplicationDbContext context, ILogger<ProductController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new string[] { "page", "pageSize" })]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
    {
        return await _context.Products
            .Where(p => p.IsActive)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    [ResponseCache(Duration = 600, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new string[] { "id" })]
    public async Task<ActionResult<Product>> GetProduct(Guid id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null || !product.IsActive)
        {
            return NotFound();
        }

        return product;
    }

    [HttpGet("by-code/{code}")]
    [ResponseCache(Duration = 600, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new string[] { "code" })]
    public async Task<ActionResult<Product>> GetProductByCode(string code)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Code == code && p.IsActive);

        if (product == null)
        {
            return NotFound();
        }

        return product;
    }

    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct(CreateProductDto dto)
    {
        var product = new Product
        {
            Code = dto.Code,
            Name = dto.Name,
            ProductName = dto.Name,
            ProductCode = dto.Code,
            Type = dto.Type,
            ProductType = dto.Type,
            Grade = dto.Grade,
            Specification = dto.Specification,
            UnitOfMeasure = dto.UnitOfMeasure,
            Density = dto.Density ?? 0,
            Origin = string.IsNullOrWhiteSpace(dto.Origin) ? "Unknown" : dto.Origin,
            IsActive = true
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(Guid id, UpdateProductDto dto)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound();
        }

        product.Name = dto.Name;
        product.ProductName = dto.Name;
        product.Grade = dto.Grade;
        product.Specification = dto.Specification;
        product.Density = dto.Density ?? product.Density;
        product.Origin = string.IsNullOrWhiteSpace(dto.Origin) ? product.Origin : dto.Origin;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound();
        }

        product.IsActive = false; // Soft delete
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public class CreateProductDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ProductType Type { get; set; }
    public string? Grade { get; set; }
    public string? Specification { get; set; }
    public string UnitOfMeasure { get; set; } = "BBL";
    public decimal? Density { get; set; }
    public string Origin { get; set; } = string.Empty;
}

public class UpdateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string? Grade { get; set; }
    public string? Specification { get; set; }
    public decimal? Density { get; set; }
    public string Origin { get; set; } = string.Empty;
}