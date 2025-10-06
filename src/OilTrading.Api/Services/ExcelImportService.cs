using OfficeOpenXml;
using OilTrading.Core.Entities;
using OilTrading.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace OilTrading.Api.Services;

public class ExcelImportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ExcelImportService> _logger;

    public ExcelImportService(ApplicationDbContext context, ILogger<ExcelImportService> logger)
    {
        _context = context;
        _logger = logger;
        
        // Set EPPlus license for version 8+ (Non-commercial use)
        // This is required for EPPlus 8 and later versions
    }

    public async Task<ImportResult> ImportICESettlementPricesAsync(string filePath)
    {
        var result = new ImportResult();
        
        try
        {
            // Create ExcelPackage with non-commercial license context
            using var package = new ExcelPackage(new FileInfo(filePath));
            // For EPPlus 8, license should be set globally once
            var worksheet = package.Workbook.Worksheets[0]; // 假设数据在第一个工作表

            // 跳过标题行，从第2行开始
            var rowCount = worksheet.Dimension.Rows;
            
            _logger.LogInformation("开始导入ICE结算价格数据，共{RowCount}行", rowCount);

            for (int row = 2; row <= rowCount; row++)
            {
                try
                {
                    // 假设列结构：日期(A), 产品代码(B), 产品名称(C), 价格(D), 货币(E)
                    var dateCell = worksheet.Cells[row, 1].Value?.ToString();
                    var productCode = worksheet.Cells[row, 2].Value?.ToString();
                    var productName = worksheet.Cells[row, 3].Value?.ToString();
                    var priceText = worksheet.Cells[row, 4].Value?.ToString();
                    var currency = worksheet.Cells[row, 5].Value?.ToString() ?? "USD";

                    if (string.IsNullOrEmpty(dateCell) || string.IsNullOrEmpty(productCode) || string.IsNullOrEmpty(priceText))
                        continue;

                    if (!DateTime.TryParse(dateCell, out var priceDate))
                        continue;

                    if (!decimal.TryParse(priceText, out var price))
                        continue;

                    // 检查是否已存在相同的记录
                    var existingPrice = await _context.MarketPrices
                        .FirstOrDefaultAsync(mp => mp.ProductCode == productCode && 
                                                  mp.PriceDate.Date == priceDate.Date);

                    if (existingPrice == null)
                    {
                        var marketPrice = new MarketPrice
                        {
                            PriceDate = priceDate,
                            ProductCode = productCode,
                            ProductName = productName ?? productCode,
                            PriceType = MarketPriceType.FuturesSettlement,
                            Price = price,
                            Currency = currency,
                            Source = "ICE",
                            DataSource = "Excel Import",
                            IsSettlement = true,
                            ImportedAt = DateTime.UtcNow,
                            ImportedBy = "System"
                        };

                        _context.MarketPrices.Add(marketPrice);
                        result.ImportedRecords++;
                    }
                    else
                    {
                        result.SkippedRecords++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "导入第{Row}行数据时出错", row);
                    result.ErrorRecords++;
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("ICE价格数据导入完成：导入{Imported}条，跳过{Skipped}条，错误{Errors}条", 
                result.ImportedRecords, result.SkippedRecords, result.ErrorRecords);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入ICE结算价格时发生异常");
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    public async Task<ImportResult> ImportPaperTradingDataAsync(string filePath)
    {
        var result = new ImportResult();
        
        try
        {
            using var package = new ExcelPackage(new FileInfo(filePath));
            var worksheet = package.Workbook.Worksheets[0];

            var rowCount = worksheet.Dimension.Rows;
            _logger.LogInformation("开始导入纸货交易数据，共{RowCount}行", rowCount);

            for (int row = 2; row <= rowCount; row++)
            {
                try
                {
                    // 假设列结构：合约号(A), 产品(B), 合约月(C), 数量(D), 价格(E), 交易日期(F), 状态(G)
                    var contractNumber = worksheet.Cells[row, 1].Value?.ToString();
                    var productCode = worksheet.Cells[row, 2].Value?.ToString();
                    var contractMonth = worksheet.Cells[row, 3].Value?.ToString();
                    var quantityText = worksheet.Cells[row, 4].Value?.ToString();
                    var priceText = worksheet.Cells[row, 5].Value?.ToString();
                    var tradeDateText = worksheet.Cells[row, 6].Value?.ToString();
                    var status = worksheet.Cells[row, 7].Value?.ToString();

                    if (string.IsNullOrEmpty(contractNumber) || string.IsNullOrEmpty(productCode) || 
                        string.IsNullOrEmpty(quantityText) || string.IsNullOrEmpty(priceText))
                        continue;

                    if (!decimal.TryParse(quantityText, out var quantity) || 
                        !decimal.TryParse(priceText, out var price) ||
                        !DateTime.TryParse(tradeDateText, out var tradeDate))
                        continue;

                    // 检查是否已存在
                    var existingContract = await _context.PaperContracts
                        .FirstOrDefaultAsync(pc => pc.ContractNumber == contractNumber);

                    if (existingContract == null)
                    {
                        var paperContract = new PaperContract
                        {
                            ContractNumber = contractNumber,
                            ContractMonth = contractMonth ?? DateTime.Now.ToString("MMMYY").ToUpper(),
                            ProductType = productCode,
                            Position = quantity > 0 ? PositionType.Long : PositionType.Short,
                            Quantity = Math.Abs(quantity), // 数量总是正数
                            LotSize = 1000m, // 默认1000MT每手
                            EntryPrice = price,
                            CurrentPrice = price,
                            TradeDate = tradeDate,
                            Status = GetPaperContractStatus(status)
                        };

                        _context.PaperContracts.Add(paperContract);
                        result.ImportedRecords++;
                    }
                    else
                    {
                        result.SkippedRecords++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "导入纸货数据第{Row}行时出错", row);
                    result.ErrorRecords++;
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("纸货交易数据导入完成：导入{Imported}条，跳过{Skipped}条，错误{Errors}条", 
                result.ImportedRecords, result.SkippedRecords, result.ErrorRecords);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入纸货交易数据时发生异常");
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }


    private static string GetProductName(string productCode)
    {
        return productCode.ToUpper() switch
        {
            "BRENT" => "Brent Crude Oil",
            "WTI" => "West Texas Intermediate",
            "GASOIL" => "Gas Oil",
            "NAPHTHA" => "Naphtha",
            "JET" => "Jet Fuel",
            "FUEL_OIL" => "Fuel Oil",
            _ => productCode
        };
    }

    private static string GetProductType(string productCode)
    {
        return productCode.ToUpper() switch
        {
            "BRENT" or "WTI" => "Crude Oil",
            "GASOIL" or "NAPHTHA" or "JET" or "FUEL_OIL" => "Refined Products",
            _ => "Other"
        };
    }

    private static PaperContractStatus GetPaperContractStatus(string? statusText)
    {
        return statusText?.ToUpper() switch
        {
            "OPEN" or "ACTIVE" => PaperContractStatus.Open,
            "CLOSED" => PaperContractStatus.Closed,
            "SETTLED" => PaperContractStatus.Settled,
            "CANCELLED" => PaperContractStatus.Cancelled,
            _ => PaperContractStatus.Open
        };
    }
}

public class ImportResult
{
    public bool Success { get; set; } = true;
    public int ImportedRecords { get; set; }
    public int SkippedRecords { get; set; }
    public int ErrorRecords { get; set; }
    public string? ErrorMessage { get; set; }
    public int TotalRecords => ImportedRecords + SkippedRecords + ErrorRecords;
}