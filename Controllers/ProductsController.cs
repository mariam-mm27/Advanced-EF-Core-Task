using EFCoreAdvanced.Data;
using EFCoreAdvanced.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EFCoreAdvanced.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(AppDbContext context, ILogger<ProductsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/products
    /// Retrieves products with pagination support.
    /// Uses .AsNoTracking() for read-only performance optimization
    /// and .Select() projection to avoid N+1 queries when fetching category names.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        // Validate pagination parameters
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        int skip = (page - 1) * pageSize;

        // AsNoTracking() improves performance for read-only queries
        // Select() projects only required fields and prevents N+1 queries by eagerly loading Category Name in the query
        var products = await _context.Products
            .AsNoTracking()
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                CategoryName = p.Category != null ? p.Category.Name : null
            })
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            page,
            pageSize,
            total = await _context.Products.CountAsync(),
            data = products
        });
    }

    /// <summary>
    /// PUT /api/products/{id}
    /// Updates allowed fields (Name, Price) of a product.
    /// Handles EF Core change tracking with proper entry updates.
    /// Includes concurrency token handling for preventing conflicts during concurrent updates.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
    {
        if (request == null)
        {
            return BadRequest("Request body cannot be empty.");
        }

        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound($"Product with id {id} not found.");
        }

        // Update only allowed fields
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            product.Name = request.Name;
        }

        if (request.Price.HasValue && request.Price.Value > 0)
        {
            product.Price = request.Price.Value;
        }

        try
        {
            // Explicitly mark entity as modified to ensure EF Core tracks changes
            _context.Entry(product).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Product updated successfully.", product });
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Handle concurrency conflicts when RowVersion (timestamp) doesn't match
            _logger.LogWarning("Concurrency conflict detected for product {ProductId}", id);

            // Retrieve the current database values
            var databaseValues = ex.Entries[0].GetDatabaseValues();
            if (databaseValues == null)
            {
                return NotFound("Product was deleted by another user.");
            }

            return Conflict(new
            {
                error = "Concurrency conflict: Product was modified by another user. Please refresh and try again.",
                currentValues = new
                {
                    name = product.Name,
                    price = product.Price
                },
                databaseValues = new
                {
                    name = databaseValues["Name"],
                    price = databaseValues["Price"]
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {ProductId}", id);
            return StatusCode(500, new { error = "An error occurred while updating the product." });
        }
    }

    /// <summary>
    /// Soft delete endpoint (bonus)
    /// Marks a product as deleted without removing it from the database.
    /// The global query filter ensures soft-deleted products are not returned in normal queries.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteProduct(int id)
    {
        var product = await _context.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
        {
            return NotFound($"Product with id {id} not found.");
        }

        product.IsDeleted = true;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Product soft deleted successfully." });
    }
}
