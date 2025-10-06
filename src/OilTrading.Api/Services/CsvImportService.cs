using System.Globalization;
using Microsoft.EntityFrameworkCore;
using OilTrading.Core.Entities;
using OilTrading.Infrastructure.Data;

namespace OilTrading.Api.Services;

public class CsvImportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CsvImportService> _logger;

    public CsvImportService(ApplicationDbContext context, ILogger<CsvImportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ImportResult> ImportSpotPricesAsync(string filePath)
    {
        var result = new ImportResult();
        
        try
        {
            // Ensure database is created
            await _context.Database.EnsureCreatedAsync();
            
            _logger.LogInformation("开始导入现货价格CSV文件: {FilePath}", filePath);
            
            var lines = await File.ReadAllLinesAsync(filePath);
            if (lines.Length < 3)
            {
                result.Success = false;
                result.ErrorMessage = "CSV文件格式不正确，至少需要3行（头部信息行、产品名称行、数据行）";
                return result;
            }

            // 解析头部行 - 产品代码
            var productCodes = ParseCsvLine(lines[0]).Skip(1).ToArray(); // 跳过第一列"Timestamp"
            
            // 解析第二行 - 产品名称
            var productNames = ParseCsvLine(lines[1]).Skip(1).ToArray();
            
            // 跳过第三行（价格类型标识），从第四行开始处理数据
            _logger.LogInformation("找到{ProductCount}个产品，准备导入{DataRows}行数据", 
                productCodes.Length, lines.Length - 3);

            var importedCount = 0;
            var skippedCount = 0;
            var errorCount = 0;

            // 处理数据行
            for (int i = 3; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                try
                {
                    var values = ParseCsvLine(line);
                    if (values.Length == 0) continue;

                    // 解析日期
                    var dateString = values[0];
                    if (string.IsNullOrEmpty(dateString)) continue;

                    if (!DateTime.TryParse(dateString, out var priceDate))
                    {
                        _logger.LogWarning("第{Row}行日期格式不正确: {Date}", i + 1, dateString);
                        errorCount++;
                        continue;
                    }

                    // 处理每个产品的价格
                    for (int j = 0; j < productCodes.Length && j + 1 < values.Length; j++)
                    {
                        var productCode = productCodes[j];
                        var productName = j < productNames.Length ? productNames[j] : productCode;
                        var priceString = values[j + 1]; // +1 因为第一列是日期

                        if (string.IsNullOrEmpty(priceString) || string.IsNullOrEmpty(productCode))
                            continue;

                        if (!decimal.TryParse(priceString, NumberStyles.Float, CultureInfo.InvariantCulture, out var price))
                            continue;

                        // 直接添加记录，不检查重复（简化首次导入）
                        var marketPrice = new MarketPrice
                        {
                            PriceDate = priceDate,
                            ProductCode = productCode,
                            ProductName = productName,
                            PriceType = MarketPriceType.Spot,
                            Price = price,
                            Currency = GetCurrencyFromProductCode(productCode),
                            Source = "CSV Import",
                            DataSource = "Spot Prices CSV",
                            IsSettlement = false,
                            ImportedAt = DateTime.UtcNow,
                            ImportedBy = "System"
                        };

                        _context.MarketPrices.Add(marketPrice);
                        importedCount++;
                    }

                    // 每100行保存一次，提高性能
                    if ((i - 3) % 100 == 0)
                    {
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("已处理{ProcessedRows}行数据", i - 3 + 1);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "处理第{Row}行数据时出错", i + 1);
                    errorCount++;
                }
            }

            // 最终保存
            await _context.SaveChangesAsync();
            
            result.ImportedRecords = importedCount;
            result.SkippedRecords = skippedCount;
            result.ErrorRecords = errorCount;
            
            _logger.LogInformation("现货价格数据导入完成：导入{Imported}条，跳过{Skipped}条，错误{Errors}条", 
                importedCount, skippedCount, errorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入现货价格CSV文件时发生异常");
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    public async Task<ImportResult> ImportFuturesPricesAsync(string filePath)
    {
        var result = new ImportResult();
        
        try
        {
            // Ensure database is created
            await _context.Database.EnsureCreatedAsync();
            
            _logger.LogInformation("开始导入期货价格CSV文件: {FilePath}", filePath);
            
            var lines = await File.ReadAllLinesAsync(filePath);
            if (lines.Length < 4)
            {
                result.Success = false;
                result.ErrorMessage = "期货CSV文件格式不正确，至少需要4行";
                return result;
            }

            // 解析期货CSV结构
            // 第1行: 产品名称 (FUEL OIL FUTURES - 380CST SING,,,,,FUEL OIL FUTURES - MARINE 0.5%,,,,,BRENT CRUDE OIL)
            // 第3行: 合约月份 (Date,2025-08,2025-09,...)
            // 第4行开始: 数据行

            var productHeaderLine = lines[0];
            var contractMonthsLine = lines[2];

            // 解析产品信息和合约月份
            var productSections = ParseFuturesProductSections(productHeaderLine, contractMonthsLine);
            
            _logger.LogInformation("找到{ProductCount}个期货产品，准备导入{DataRows}行数据", 
                productSections.Count, lines.Length - 3);

            var importedCount = 0;
            var skippedCount = 0;
            var errorCount = 0;

            // 处理数据行 (从第4行开始)
            for (int i = 3; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                try
                {
                    var values = ParseCsvLine(line);
                    if (values.Length == 0) continue;

                    // 解析日期
                    var dateString = values[0];
                    if (string.IsNullOrEmpty(dateString)) continue;

                    if (!DateTime.TryParse(dateString, out var priceDate))
                    {
                        _logger.LogWarning("第{Row}行日期格式不正确: {Date}", i + 1, dateString);
                        errorCount++;
                        continue;
                    }

                    // 处理每个产品的期货价格
                    foreach (var section in productSections)
                    {
                        for (int j = 0; j < section.ContractMonths.Count; j++)
                        {
                            var columnIndex = section.StartColumn + j;
                            if (columnIndex >= values.Length) continue;

                            var priceString = values[columnIndex];
                            if (string.IsNullOrEmpty(priceString)) continue;

                            if (!decimal.TryParse(priceString, NumberStyles.Float, CultureInfo.InvariantCulture, out var price))
                                continue;

                            var contractMonth = section.ContractMonths[j];
                            // 为期货合约创建唯一的产品代码（包含合约月份）
                            var uniqueProductCode = $"{section.ProductCode}_{contractMonth}";

                            var marketPrice = new MarketPrice
                            {
                                PriceDate = priceDate,
                                ProductCode = uniqueProductCode,
                                ProductName = $"{section.ProductName} {contractMonth}",
                                PriceType = MarketPriceType.FuturesClose,
                                Price = price,
                                Currency = "USD",
                                ContractMonth = contractMonth,
                                Source = "CSV Import",
                                DataSource = "Futures Prices CSV",
                                IsSettlement = false,
                                ImportedAt = DateTime.UtcNow,
                                ImportedBy = "System"
                            };

                            _context.MarketPrices.Add(marketPrice);
                            importedCount++;
                        }
                    }

                    // 每50行保存一次，提高性能
                    if ((i - 3) % 50 == 0)
                    {
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("已处理{ProcessedRows}行期货数据", i - 3 + 1);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "处理期货数据第{Row}行时出错", i + 1);
                    errorCount++;
                }
            }

            // 最终保存
            await _context.SaveChangesAsync();
            
            result.ImportedRecords = importedCount;
            result.SkippedRecords = skippedCount;
            result.ErrorRecords = errorCount;
            
            _logger.LogInformation("期货价格数据导入完成：导入{Imported}条，跳过{Skipped}条，错误{Errors}条", 
                importedCount, skippedCount, errorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入期货价格CSV文件时发生异常");
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    private static string[] ParseCsvLine(string line)
    {
        // 简单CSV解析，处理逗号分隔
        return line.Split(',', StringSplitOptions.None)
                   .Select(s => s.Trim())
                   .ToArray();
    }

    private static string GetCurrencyFromProductCode(string productCode)
    {
        // 根据产品代码推断货币，大多数是USD
        return productCode.ToUpper() switch
        {
            _ => "USD" // 默认USD
        };
    }

    private static List<FuturesProductSection> ParseFuturesProductSections(string productHeaderLine, string contractMonthsLine)
    {
        var sections = new List<FuturesProductSection>();
        var productHeaders = ParseCsvLine(productHeaderLine);
        var monthHeaders = ParseCsvLine(contractMonthsLine);

        // 定义三个产品的固定位置
        var productDefinitions = new[]
        {
            new { Name = "FUEL OIL FUTURES - 380CST SING", StartCol = 1, EndCol = 13 },
            new { Name = "FUEL OIL FUTURES - MARINE 0.5% FOB SING", StartCol = 15, EndCol = 27 }, // Skip Date column at 14
            new { Name = "BRENT CRUDE OIL", StartCol = 29, EndCol = 41 } // Skip Date column at 28
        };

        foreach (var productDef in productDefinitions)
        {
            if (productDef.EndCol < monthHeaders.Length)
            {
                var contractMonths = new List<string>();
                for (int i = productDef.StartCol; i <= productDef.EndCol && i < monthHeaders.Length; i++)
                {
                    var month = monthHeaders[i];
                    if (!string.IsNullOrEmpty(month) && month != "Date")
                    {
                        contractMonths.Add(month);
                    }
                }

                if (contractMonths.Count > 0)
                {
                    sections.Add(new FuturesProductSection
                    {
                        ProductName = productDef.Name,
                        ProductCode = GetFuturesProductCode(productDef.Name),
                        StartColumn = productDef.StartCol,
                        ContractMonths = contractMonths
                    });
                }
            }
        }

        return sections;
    }

    private static string GetFuturesProductCode(string productName)
    {
        return productName.ToUpper() switch
        {
            var name when name.Contains("FUEL OIL") && name.Contains("380CST") => "FUEL_OIL_380",
            var name when name.Contains("FUEL OIL") && name.Contains("MARINE") && name.Contains("0.5%") => "MARINE_FUEL_05",
            var name when name.Contains("BRENT") && name.Contains("CRUDE") => "BRENT_FUTURES",
            _ => productName.Replace(" ", "_").Replace("-", "_").ToUpper()
        };
    }
}

public class FuturesProductSection
{
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public int StartColumn { get; set; }
    public List<string> ContractMonths { get; set; } = new List<string>();
}