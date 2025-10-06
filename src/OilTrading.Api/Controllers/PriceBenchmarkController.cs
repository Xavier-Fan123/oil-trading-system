using Microsoft.AspNetCore.Mvc;
using OilTrading.Core.Repositories;

namespace OilTrading.Api.Controllers;

/// <summary>
/// Price Benchmark Controller - 价格基准物控制器
/// Purpose: 提供价格基准物的查询接口，用于合同创建时的基准物选择
/// 这是油品交易中的关键组件，为前端提供可用的定价基准选项
/// </summary>
[ApiController]
[Route("api/price-benchmarks")]
public class PriceBenchmarkController : ControllerBase
{
    private readonly IPriceBenchmarkRepository _benchmarkRepository;
    private readonly ILogger<PriceBenchmarkController> _logger;

    public PriceBenchmarkController(
        IPriceBenchmarkRepository benchmarkRepository,
        ILogger<PriceBenchmarkController> logger)
    {
        _benchmarkRepository = benchmarkRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get all active price benchmarks
    /// 获取所有活跃的价格基准物
    /// </summary>
    [HttpGet]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)] // Cache for 5 minutes
    [ProducesResponseType(typeof(IEnumerable<PriceBenchmarkDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveBenchmarks()
    {
        try
        {
            var benchmarks = await _benchmarkRepository.GetActiveAsync();
            
            var benchmarkDtos = benchmarks.Select(b => new PriceBenchmarkDto
            {
                Id = b.Id,
                BenchmarkName = b.BenchmarkName,
                BenchmarkType = b.BenchmarkType.ToString(),
                ProductCategory = b.ProductCategory,
                Currency = b.Currency,
                Unit = b.Unit,
                Description = b.Description,
                DataSource = b.DataSource,
                IsActive = b.IsActive
            }).ToList();

            _logger.LogInformation("Retrieved {Count} active price benchmarks", benchmarkDtos.Count);
            return Ok(benchmarkDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving price benchmarks");
            return StatusCode(500, "Internal server error while retrieving price benchmarks");
        }
    }

    /// <summary>
    /// Get benchmark by ID
    /// 根据ID获取基准物详情
    /// </summary>
    [HttpGet("{id:guid}")]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(typeof(PriceBenchmarkDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBenchmarkById(Guid id)
    {
        try
        {
            var benchmark = await _benchmarkRepository.GetByIdAsync(id);
            if (benchmark == null)
            {
                return NotFound($"Price benchmark with ID {id} not found");
            }

            var benchmarkDto = new PriceBenchmarkDto
            {
                Id = benchmark.Id,
                BenchmarkName = benchmark.BenchmarkName,
                BenchmarkType = benchmark.BenchmarkType.ToString(),
                ProductCategory = benchmark.ProductCategory,
                Currency = benchmark.Currency,
                Unit = benchmark.Unit,
                Description = benchmark.Description,
                DataSource = benchmark.DataSource,
                IsActive = benchmark.IsActive
            };

            return Ok(benchmarkDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving price benchmark {BenchmarkId}", id);
            return StatusCode(500, "Internal server error while retrieving price benchmark");
        }
    }

    /// <summary>
    /// Get benchmarks by category
    /// 按产品类别获取基准物
    /// </summary>
    [HttpGet("by-category/{category}")]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(typeof(IEnumerable<PriceBenchmarkDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBenchmarksByCategory(string category)
    {
        try
        {
            var benchmarks = await _benchmarkRepository.GetActiveAsync();
            var filteredBenchmarks = benchmarks.Where(b => 
                b.ProductCategory.Equals(category, StringComparison.OrdinalIgnoreCase));

            var benchmarkDtos = filteredBenchmarks.Select(b => new PriceBenchmarkDto
            {
                Id = b.Id,
                BenchmarkName = b.BenchmarkName,
                BenchmarkType = b.BenchmarkType.ToString(),
                ProductCategory = b.ProductCategory,
                Currency = b.Currency,
                Unit = b.Unit,
                Description = b.Description,
                DataSource = b.DataSource,
                IsActive = b.IsActive
            }).ToList();

            _logger.LogInformation("Retrieved {Count} benchmarks for category {Category}", 
                benchmarkDtos.Count, category);
            return Ok(benchmarkDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving benchmarks for category {Category}", category);
            return StatusCode(500, "Internal server error while retrieving benchmarks by category");
        }
    }
}

/// <summary>
/// Price Benchmark DTO
/// 价格基准物数据传输对象
/// </summary>
public class PriceBenchmarkDto
{
    public Guid Id { get; set; }
    public string BenchmarkName { get; set; } = string.Empty;
    public string BenchmarkType { get; set; } = string.Empty;
    public string ProductCategory { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DataSource { get; set; }
    public bool IsActive { get; set; }
}