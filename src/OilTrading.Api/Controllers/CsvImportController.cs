using Microsoft.AspNetCore.Mvc;
using OilTrading.Api.Services;

namespace OilTrading.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CsvImportController : ControllerBase
{
    private readonly CsvImportService _csvImportService;
    private readonly ILogger<CsvImportController> _logger;

    public CsvImportController(CsvImportService csvImportService, ILogger<CsvImportController> logger)
    {
        _csvImportService = csvImportService;
        _logger = logger;
    }

    /// <summary>
    /// 上传现货价格CSV文件
    /// </summary>
    /// <param name="file">CSV文件</param>
    /// <returns>导入结果</returns>
    [HttpPost("spot-prices")]
    public async Task<ActionResult<ImportResult>> UploadSpotPrices(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "请选择要上传的CSV文件" });
        }

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "请上传CSV格式的文件" });
        }

        try
        {
            // 创建临时文件
            var tempFilePath = Path.GetTempFileName();
            
            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 导入数据
            var result = await _csvImportService.ImportSpotPricesAsync(tempFilePath);

            // 清理临时文件
            if (System.IO.File.Exists(tempFilePath))
            {
                System.IO.File.Delete(tempFilePath);
            }

            if (result.Success)
            {
                _logger.LogInformation("现货价格CSV上传成功：{FileName}, 导入{Imported}条记录", 
                    file.FileName, result.ImportedRecords);
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("现货价格CSV上传失败：{FileName}, 错误：{Error}", 
                    file.FileName, result.ErrorMessage);
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传现货价格CSV时发生异常：{FileName}", file.FileName);
            return StatusCode(500, new { message = "服务器内部错误", error = ex.Message });
        }
    }

    /// <summary>
    /// 上传期货价格CSV文件
    /// </summary>
    /// <param name="file">CSV文件</param>
    /// <returns>导入结果</returns>
    [HttpPost("futures-prices")]
    public async Task<ActionResult<ImportResult>> UploadFuturesPrices(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "请选择要上传的CSV文件" });
        }

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "请上传CSV格式的文件" });
        }

        try
        {
            // 创建临时文件
            var tempFilePath = Path.GetTempFileName();
            
            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 导入数据
            var result = await _csvImportService.ImportFuturesPricesAsync(tempFilePath);

            // 清理临时文件
            if (System.IO.File.Exists(tempFilePath))
            {
                System.IO.File.Delete(tempFilePath);
            }

            if (result.Success)
            {
                _logger.LogInformation("期货价格CSV上传成功：{FileName}, 导入{Imported}条记录", 
                    file.FileName, result.ImportedRecords);
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("期货价格CSV上传失败：{FileName}, 错误：{Error}", 
                    file.FileName, result.ErrorMessage);
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传期货价格CSV时发生异常：{FileName}", file.FileName);
            return StatusCode(500, new { message = "服务器内部错误", error = ex.Message });
        }
    }

    /// <summary>
    /// 直接导入本地期货价格CSV文件
    /// </summary>
    /// <param name="request">文件路径请求</param>
    /// <returns>导入结果</returns>
    [HttpPost("import-local-futures")]
    public async Task<ActionResult<ImportResult>> ImportLocalFuturesPrices([FromBody] LocalFileImportRequest request)
    {
        if (string.IsNullOrEmpty(request.FilePath))
        {
            return BadRequest(new { message = "请提供文件路径" });
        }

        if (!System.IO.File.Exists(request.FilePath))
        {
            return BadRequest(new { message = $"文件不存在：{request.FilePath}" });
        }

        try
        {
            var result = await _csvImportService.ImportFuturesPricesAsync(request.FilePath);

            if (result.Success)
            {
                _logger.LogInformation("本地期货价格CSV导入成功：{FilePath}, 导入{Imported}条记录", 
                    request.FilePath, result.ImportedRecords);
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("本地期货价格CSV导入失败：{FilePath}, 错误：{Error}", 
                    request.FilePath, result.ErrorMessage);
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入本地期货价格CSV时发生异常：{FilePath}", request.FilePath);
            return StatusCode(500, new { message = "服务器内部错误", error = ex.Message });
        }
    }

    /// <summary>
    /// 直接导入本地现货价格CSV文件
    /// </summary>
    /// <param name="request">文件路径请求</param>
    /// <returns>导入结果</returns>
    [HttpPost("import-local-spot")]
    public async Task<ActionResult<ImportResult>> ImportLocalSpotPrices([FromBody] LocalFileImportRequest request)
    {
        if (string.IsNullOrEmpty(request.FilePath))
        {
            return BadRequest(new { message = "请提供文件路径" });
        }

        if (!System.IO.File.Exists(request.FilePath))
        {
            return BadRequest(new { message = $"文件不存在：{request.FilePath}" });
        }

        try
        {
            var result = await _csvImportService.ImportSpotPricesAsync(request.FilePath);

            if (result.Success)
            {
                _logger.LogInformation("本地现货价格CSV导入成功：{FilePath}, 导入{Imported}条记录", 
                    request.FilePath, result.ImportedRecords);
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("本地现货价格CSV导入失败：{FilePath}, 错误：{Error}", 
                    request.FilePath, result.ErrorMessage);
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入本地现货价格CSV时发生异常：{FilePath}", request.FilePath);
            return StatusCode(500, new { message = "服务器内部错误", error = ex.Message });
        }
    }
}

public class LocalFileImportRequest
{
    public string FilePath { get; set; } = string.Empty;
}