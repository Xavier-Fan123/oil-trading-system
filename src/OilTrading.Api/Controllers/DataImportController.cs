using Microsoft.AspNetCore.Mvc;
using OilTrading.Api.Services;

namespace OilTrading.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Data Import")]
public class DataImportController : ControllerBase
{
    private readonly ExcelImportService _importService;
    private readonly ILogger<DataImportController> _logger;

    public DataImportController(ExcelImportService importService, ILogger<DataImportController> logger)
    {
        _importService = importService;
        _logger = logger;
    }

    /// <summary>
    /// 导入ICE结算价格数据
    /// </summary>
    [HttpPost("ice-settlement-prices")]
    public async Task<IActionResult> ImportICESettlementPrices([FromQuery] string filePath)
    {
        _logger.LogInformation("开始导入ICE结算价格数据：{FilePath}", filePath);

        if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
        {
            return BadRequest(new { message = "文件路径无效或文件不存在", filePath });
        }

        try
        {
            var result = await _importService.ImportICESettlementPricesAsync(filePath);
            
            if (result.Success)
            {
                return Ok(new { 
                    message = "ICE价格数据导入成功", 
                    result.ImportedRecords, 
                    result.SkippedRecords, 
                    result.ErrorRecords,
                    result.TotalRecords
                });
            }
            else
            {
                return BadRequest(new { 
                    message = "ICE价格数据导入失败", 
                    error = result.ErrorMessage 
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入ICE价格数据时发生异常");
            return BadRequest(new { message = "导入失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 导入纸货交易数据
    /// </summary>
    [HttpPost("paper-trading-data")]
    public async Task<IActionResult> ImportPaperTradingData([FromQuery] string filePath)
    {
        _logger.LogInformation("开始导入纸货交易数据：{FilePath}", filePath);

        if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
        {
            return BadRequest(new { message = "文件路径无效或文件不存在", filePath });
        }

        try
        {
            var result = await _importService.ImportPaperTradingDataAsync(filePath);
            
            if (result.Success)
            {
                return Ok(new { 
                    message = "纸货交易数据导入成功", 
                    result.ImportedRecords, 
                    result.SkippedRecords, 
                    result.ErrorRecords,
                    result.TotalRecords
                });
            }
            else
            {
                return BadRequest(new { 
                    message = "纸货交易数据导入失败", 
                    error = result.ErrorMessage 
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入纸货交易数据时发生异常");
            return BadRequest(new { message = "导入失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 批量导入历史数据
    /// </summary>
    [HttpPost("bulk-import")]
    public async Task<IActionResult> BulkImportHistoricalData()
    {
        _logger.LogInformation("开始批量导入历史数据");

        var results = new List<object>();

        // 导入ICE数据
        var iceFilePath = @"C:\Users\itg\Desktop\ICE Settlement Price(1).xlsx";
        if (System.IO.File.Exists(iceFilePath))
        {
            try
            {
                var iceResult = await _importService.ImportICESettlementPricesAsync(iceFilePath);
                results.Add(new { 
                    DataType = "ICE Settlement Prices",
                    Success = iceResult.Success,
                    iceResult.ImportedRecords,
                    iceResult.SkippedRecords,
                    iceResult.ErrorRecords,
                    ErrorMessage = iceResult.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入ICE数据时发生异常");
                results.Add(new { 
                    DataType = "ICE Settlement Prices",
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }
        else
        {
            results.Add(new { 
                DataType = "ICE Settlement Prices",
                Success = false,
                ErrorMessage = "文件不存在: " + iceFilePath
            });
        }

        // 导入纸货数据
        var paperFilePath = @"C:\Users\itg\Desktop\纸货计价合约模板2025_new(1).xlsx";
        if (System.IO.File.Exists(paperFilePath))
        {
            try
            {
                var paperResult = await _importService.ImportPaperTradingDataAsync(paperFilePath);
                results.Add(new { 
                    DataType = "Paper Trading Data",
                    Success = paperResult.Success,
                    paperResult.ImportedRecords,
                    paperResult.SkippedRecords,
                    paperResult.ErrorRecords,
                    ErrorMessage = paperResult.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入纸货数据时发生异常");
                results.Add(new { 
                    DataType = "Paper Trading Data",
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }
        else
        {
            results.Add(new { 
                DataType = "Paper Trading Data",
                Success = false,
                ErrorMessage = "文件不存在: " + paperFilePath
            });
        }

        return Ok(new { 
            message = "批量导入完成",
            results,
            totalDataTypes = results.Count,
            successfulImports = results.Count(r => (bool)(r.GetType().GetProperty("Success")?.GetValue(r) ?? false))
        });
    }

    /// <summary>
    /// 检查数据导入状态
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetImportStatus()
    {
        // 返回与前端期望格式匹配的数据
        return Ok(new { 
            isImporting = false,
            progress = 0,
            currentFile = (string?)null,
            totalFiles = 0,
            completedFiles = 0,
            errors = new string[0],
            lastImport = (string?)null
        });
    }
}