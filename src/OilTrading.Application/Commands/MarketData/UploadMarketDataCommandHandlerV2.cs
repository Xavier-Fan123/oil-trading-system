using MediatR;
using ClosedXML.Excel;
using OilTrading.Application.DTOs;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace OilTrading.Application.Commands.MarketData;

/// <summary>
/// Improved version with batch processing and robust error handling
/// </summary>
public class UploadMarketDataCommandHandlerV2 : IRequestHandler<UploadMarketDataCommand, MarketDataUploadResultDto>
{
    private readonly IMarketDataRepository _marketDataRepository;
    private readonly IFuturesDealRepository _futuresDealRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UploadMarketDataCommandHandlerV2> _logger;
    
    private const int BATCH_SIZE = 100;
    private const int MAX_ERRORS = 50;
    private const int MAX_DAYS_TO_PROCESS = 30;
    
    // Enhanced product mappings with validation
    private readonly Dictionary<string, (string Code, string Name, string Type)> _productMappings = new()
    {
        // Crude Oil
        { "LCOc1", ("ICE_BRENT", "ICE Brent Crude Future", "CRUDE") },
        { "LCOCALMc1", ("BRENT_1ST", "Brent 1st Line Future", "CRUDE") },
        { "XDOA001", ("DME_OMAN", "DME Oman Crude", "CRUDE") },
        { "PAAAD00", ("DUBAI", "Dubai Crude", "CRUDE") },
        { "PAAAP00", ("MURBAN", "Murban Crude", "CRUDE") },
        
        // Fuel Oil
        { "PPXDK00", ("MOPS_380", "MOPS FO 380cst FOB Sg", "FUEL_OIL") },
        { "PUADV00", ("MOPS_180", "MOPS FO 180cst FOB Sg", "FUEL_OIL") },
        { "AMFSA00", ("MOPS_05", "MOPS Marine Fuel 0.5%", "FUEL_OIL") },
        { "AAOVC00", ("SING_380", "Singapore 380cst", "FUEL_OIL") },
        { "PJABF00", ("SING_180", "Singapore 180cst", "FUEL_OIL") },
        { "PUMFD00", ("MOPS_500", "MOPS FO 500cst", "FUEL_OIL") },
        { "PUABC00", ("MOPS_180_HI", "MOPS FO 180cst High", "FUEL_OIL") },
        { "PUABE00", ("HSFO", "High Sulfur Fuel Oil", "FUEL_OIL") },
        { "AACUA00", ("LSFO", "Low Sulfur Fuel Oil", "FUEL_OIL") },
        { "AAIDC00", ("VLSFO", "Very Low Sulfur Fuel Oil", "FUEL_OIL") },
        
        // Gasoil & Distillates
        { "LGOc1", ("IPE_GASOIL", "IPE Gasoil Futures", "GASOIL") },
        { "LGOCALMc1", ("GASOIL_1ST", "Gasoil 1st Line Future", "GASOIL") },
        { "PGAEY00", ("GO_92", "GO 92 RON", "GASOIL") },
        { "PGAEZ00", ("GO_95", "GO 95 RON", "GASOIL") },
        { "PGAMS00", ("GO_10PPM", "Gasoil 10ppm", "GASOIL") },
        { "AAGZF00", ("MGO", "Marine Gas Oil", "GASOIL") },
        { "AAOVD00", ("GO_10PPM_PREM", "Gasoil 10ppm Premium", "GASOIL") },
        
        // Other Products
        { "AAPPF00", ("JET_KERO", "Jet Kerosene", "JET") },
        { "AAFEX00", ("NAPHTHA", "Naphtha", "NAPHTHA") },
        { "PHALF00", ("FUEL_OIL", "Fuel Oil", "FUEL_OIL") },
        { "PPXDL00", ("MOPS_380_PREM", "MOPS 380cst Premium", "FUEL_OIL") },
        { "FOFSB00", ("FUEL_OIL_SPREAD", "Fuel Oil Spread", "SPREAD") }
    };

    public UploadMarketDataCommandHandlerV2(
        IMarketDataRepository marketDataRepository,
        IFuturesDealRepository futuresDealRepository,
        IUnitOfWork unitOfWork,
        ILogger<UploadMarketDataCommandHandlerV2> logger)
    {
        _marketDataRepository = marketDataRepository;
        _futuresDealRepository = futuresDealRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<MarketDataUploadResultDto> Handle(
        UploadMarketDataCommand request, 
        CancellationToken cancellationToken)
    {
        var result = new MarketDataUploadResultDto();
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Starting market data upload. File: {FileName}, Type: {FileType}, Size: {Size} bytes",
                request.FileName, request.FileType, request.FileContent.Length);
            
            // Validate file content
            if (request.FileContent.Length == 0)
            {
                result.Errors.Add("File is empty");
                return result;
            }
            
            if (request.FileContent.Length > 50 * 1024 * 1024) // 50MB limit
            {
                result.Errors.Add("File size exceeds 50MB limit");
                return result;
            }
            
            using var stream = new MemoryStream(request.FileContent);
            using var workbook = new XLWorkbook(stream);
            
            // Process based on file type
            switch (request.FileType)
            {
                case "DailyPrices":
                    result = await ProcessDailyPricesWithBatch(workbook, request.UploadedBy, cancellationToken);
                    break;
                    
                case "ICESettlement":
                    result = await ProcessICESettlementWithBatch(workbook, request.UploadedBy, cancellationToken);
                    break;
                    
                case "DealReport":
                    result = await ProcessDealReport(workbook, request.UploadedBy, request.FileName, cancellationToken);
                    break;
                    
                default:
                    result.Errors.Add($"Unsupported file type: {request.FileType}");
                    return result;
            }
            
            // Save changes with retry logic
            var retryCount = 0;
            while (retryCount < 3)
            {
                try
                {
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    result.Success = true;
                    break;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    _logger.LogWarning("Save attempt {Attempt} failed: {Error}", retryCount, ex.Message);
                    
                    if (retryCount >= 3)
                    {
                        throw;
                    }
                    
                    await Task.Delay(1000 * retryCount, cancellationToken); // Exponential backoff
                }
            }
            
            stopwatch.Stop();
            _logger.LogInformation(
                "Market data upload completed in {ElapsedMs}ms. Records: Created={Created}, Updated={Updated}, Skipped={Skipped}, Errors={ErrorCount}",
                stopwatch.ElapsedMilliseconds, result.RecordsCreated, result.RecordsUpdated, 
                result.RecordsSkipped, result.Errors.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error processing market data upload");
            result.Success = false;
            result.Errors.Add($"Critical error: {ex.Message}");
        }
        
        return result;
    }
    
    private async Task<MarketDataUploadResultDto> ProcessDailyPricesWithBatch(
        IXLWorkbook workbook, 
        string uploadedBy,
        CancellationToken cancellationToken)
    {
        var result = new MarketDataUploadResultDto();
        var worksheet = FindWorksheet(workbook, new[] { "Origin", "originfile", "Daily Prices", "Sheet1" });
        
        if (worksheet == null)
        {
            result.Errors.Add("Daily prices worksheet not found");
            return result;
        }
        
        // Map columns dynamically
        var columnMappings = MapProductColumns(worksheet);
        if (columnMappings.Count == 0)
        {
            result.Errors.Add("No valid product columns found");
            return result;
        }
        
        // Find data range
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        var startRow = 3; // Assuming headers in rows 1-2
        
        if (lastRow < startRow)
        {
            result.Messages.Add("No data rows found");
            return result;
        }
        
        // Process in batches
        var rowsToProcess = Math.Min(MAX_DAYS_TO_PROCESS, lastRow - startRow + 1);
        var firstRow = lastRow - rowsToProcess + 1;
        
        var pricesToCreate = new List<MarketPrice>();
        var pricesToUpdate = new List<MarketPrice>();
        var errorCount = 0;
        
        for (int row = firstRow; row <= lastRow; row++)
        {
            try
            {
                var dateCell = worksheet.Cell(row, 2); // Column B for dates
                if (!TryParseDate(dateCell.GetString(), out var priceDate))
                {
                    result.RecordsSkipped++;
                    continue;
                }
                
                foreach (var (colIndex, productCode) in columnMappings)
                {
                    try
                    {
                        var priceCell = worksheet.Cell(row, colIndex);
                        if (!TryParsePrice(priceCell, out var price) || price <= 0)
                            continue;
                        
                        if (!_productMappings.TryGetValue(productCode, out var productInfo))
                            continue;
                        
                        // Check existing price
                        var existingPrice = await _marketDataRepository.GetByProductAndDateAsync(
                            productInfo.Code, priceDate, cancellationToken);
                        
                        if (existingPrice != null)
                        {
                            if (Math.Abs(existingPrice.Price - price) > 0.0001m) // Price changed
                            {
                                existingPrice.Price = price;
                                existingPrice.SetUpdatedBy(uploadedBy);
                                pricesToUpdate.Add(existingPrice);
                            }
                        }
                        else
                        {
                            var marketPrice = CreateMarketPrice(
                                priceDate, productInfo, price, productCode, 
                                uploadedBy, MarketPriceType.Spot);
                            pricesToCreate.Add(marketPrice);
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        _logger.LogError(ex, "Error processing cell at row {Row}, column {Column}", row, colIndex);
                        
                        if (errorCount >= MAX_ERRORS)
                        {
                            result.Errors.Add($"Too many errors ({MAX_ERRORS}). Stopping processing.");
                            break;
                        }
                    }
                }
                
                // Batch save
                if (pricesToCreate.Count >= BATCH_SIZE)
                {
                    await SaveBatch(pricesToCreate, pricesToUpdate, cancellationToken);
                    result.RecordsCreated += pricesToCreate.Count;
                    result.RecordsUpdated += pricesToUpdate.Count;
                    pricesToCreate.Clear();
                    pricesToUpdate.Clear();
                }
            }
            catch (Exception ex)
            {
                errorCount++;
                _logger.LogError(ex, "Error processing row {Row}", row);
                
                if (errorCount >= MAX_ERRORS)
                {
                    result.Errors.Add($"Too many errors ({MAX_ERRORS}). Stopping processing.");
                    break;
                }
            }
        }
        
        // Save remaining items
        if (pricesToCreate.Count > 0 || pricesToUpdate.Count > 0)
        {
            await SaveBatch(pricesToCreate, pricesToUpdate, cancellationToken);
            result.RecordsCreated += pricesToCreate.Count;
            result.RecordsUpdated += pricesToUpdate.Count;
        }
        
        result.RecordsProcessed = result.RecordsCreated + result.RecordsUpdated + result.RecordsSkipped;
        result.Messages.Add($"Processed {rowsToProcess} days of daily prices");
        
        return result;
    }
    
    private async Task<MarketDataUploadResultDto> ProcessICESettlementWithBatch(
        IXLWorkbook workbook,
        string uploadedBy,
        CancellationToken cancellationToken)
    {
        var result = new MarketDataUploadResultDto();
        
        // Process futures sheets - ICE Singapore official codes
        // SG380 = HSFO 380 CST Futures, SG05 = VLSFO 0.5% Futures
        var sheets = new[]
        {
            ("380 Vol-OI", "SG380"),
            ("0.5 Vol-OI", "SG05")
        };
        
        foreach (var (sheetName, productBase) in sheets)
        {
            var worksheet = FindWorksheet(workbook, new[] { sheetName, sheetName.Replace(" ", ""), sheetName.ToUpper() });
            if (worksheet == null)
            {
                result.Messages.Add($"{sheetName} sheet not found");
                continue;
            }
            
            var sheetResult = await ProcessFuturesSheetWithBatch(
                worksheet, productBase, uploadedBy, cancellationToken);
            
            result.RecordsCreated += sheetResult.RecordsCreated;
            result.RecordsUpdated += sheetResult.RecordsUpdated;
            result.RecordsSkipped += sheetResult.RecordsSkipped;
            result.RecordsProcessed += sheetResult.RecordsProcessed;
            result.Messages.AddRange(sheetResult.Messages);
            result.Errors.AddRange(sheetResult.Errors);
        }
        
        return result;
    }
    
    private async Task<MarketDataUploadResultDto> ProcessFuturesSheetWithBatch(
        IXLWorksheet worksheet,
        string productBase,
        string uploadedBy,
        CancellationToken cancellationToken)
    {
        var result = new MarketDataUploadResultDto();
        
        // Settlement price columns (N, Q, T, W, Z, AC, AF)
        var settlementColumns = new[] { 14, 17, 20, 23, 26, 29, 32 };
        var dateColumnIndex = 13; // Column M
        
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        if (lastRow < 3) return result;
        
        var pricesToCreate = new List<MarketPrice>();
        var pricesToUpdate = new List<MarketPrice>();
        
        // Process last 10 days
        var rowsToProcess = Math.Min(10, lastRow - 2);
        
        for (int row = 2; row < 2 + rowsToProcess; row++)
        {
            var dateCell = worksheet.Cell(row, dateColumnIndex);
            if (!TryParseDate(dateCell.GetString(), out var priceDate))
                continue;
            
            foreach (var colIndex in settlementColumns)
            {
                var contractHeader = worksheet.Cell(1, colIndex).GetString();
                if (string.IsNullOrEmpty(contractHeader))
                    continue;

                // Use new parser that supports both Platts MOPS format ("SG380 2511") and ICE format
                var (productCode, contractMonth) = ParseProductCodeAndMonth(contractHeader);
                if (string.IsNullOrEmpty(contractMonth))
                {
                    _logger.LogWarning("Could not extract contract month from header: '{Header}', skipping column {ColIndex}",
                        contractHeader, colIndex);
                    continue;
                }

                var priceCell = worksheet.Cell(row, colIndex);
                if (!TryParsePrice(priceCell, out var price) || price <= 0)
                    continue;

                // ProductCode is now clean base code (e.g., "SG380" not "SG380_202511")
                // ContractMonth is separate field (e.g., "202511")
                _logger.LogDebug("Processing futures price: ProductCode='{ProductCode}', ContractMonth='{ContractMonth}', Price={Price}",
                    productCode, contractMonth, price);

                // Check existing
                var existingPrice = await _marketDataRepository.GetByProductAndDateAsync(
                    productCode, priceDate, cancellationToken);

                if (existingPrice != null)
                {
                    if (Math.Abs(existingPrice.Price - price) > 0.0001m)
                    {
                        existingPrice.Price = price;
                        existingPrice.SetUpdatedBy(uploadedBy);
                        pricesToUpdate.Add(existingPrice);
                    }
                }
                else
                {
                    var marketPrice = MarketPrice.Create(
                        priceDate,
                        productCode,                          // Clean base code: "SG380"
                        $"{productCode} {contractMonth}",     // Display name: "SG380 202511"
                        MarketPriceType.FuturesSettlement,
                        price,
                        "USD",
                        contractHeader,                       // Original column header
                        "ICE Settlement",
                        true,
                        DateTime.UtcNow,
                        uploadedBy,
                        contractMonth,                        // Separate contract month field
                        GetRegionFromProductCode(productCode));
                    pricesToCreate.Add(marketPrice);
                }
            }
            
            // Batch save
            if (pricesToCreate.Count >= BATCH_SIZE)
            {
                await SaveBatch(pricesToCreate, pricesToUpdate, cancellationToken);
                result.RecordsCreated += pricesToCreate.Count;
                result.RecordsUpdated += pricesToUpdate.Count;
                pricesToCreate.Clear();
                pricesToUpdate.Clear();
            }
        }
        
        // Save remaining
        if (pricesToCreate.Count > 0 || pricesToUpdate.Count > 0)
        {
            await SaveBatch(pricesToCreate, pricesToUpdate, cancellationToken);
            result.RecordsCreated += pricesToCreate.Count;
            result.RecordsUpdated += pricesToUpdate.Count;
        }
        
        result.RecordsProcessed = result.RecordsCreated + result.RecordsUpdated;
        result.Messages.Add($"Processed {productBase} futures prices");
        
        return result;
    }
    
    private async Task<MarketDataUploadResultDto> ProcessDealReport(
        IXLWorkbook workbook,
        string uploadedBy,
        string fileName,
        CancellationToken cancellationToken)
    {
        var result = new MarketDataUploadResultDto();
        
        // Deal Report processing for futures trades
        var worksheet = FindWorksheet(workbook, new[] { "Deals", "Deal Report", "Trades", "Sheet1" });
        if (worksheet == null)
        {
            result.Errors.Add("Deal report worksheet not found");
            return result;
        }
        
        var dealsToCreate = new List<FuturesDeal>();
        var dealsToUpdate = new List<FuturesDeal>();
        
        // TODO: Implement Deal Report parsing based on actual file structure
        // This is a placeholder - needs to be customized based on actual XLSB format
        
        result.Messages.Add("Deal Report processing not yet implemented");
        return result;
    }
    
    // Helper methods
    private IXLWorksheet? FindWorksheet(IXLWorkbook workbook, string[] possibleNames)
    {
        foreach (var name in possibleNames)
        {
            try
            {
                var worksheet = workbook.Worksheet(name);
                if (worksheet != null)
                {
                    _logger.LogDebug("Found worksheet: {Name}", name);
                    return worksheet;
                }
            }
            catch { }
        }
        return null;
    }
    
    private Dictionary<int, string> MapProductColumns(IXLWorksheet worksheet)
    {
        var mappings = new Dictionary<int, string>();
        var headerRow = 1;
        
        for (int col = 3; col <= 50; col++)
        {
            var header = worksheet.Cell(headerRow, col).GetString();
            if (!string.IsNullOrEmpty(header) && _productMappings.ContainsKey(header))
            {
                mappings[col] = header;
            }
        }
        
        return mappings;
    }
    
    private bool TryParseDate(string value, out DateTime date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;
        
        // Try various date formats
        var formats = new[]
        {
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd",
            "MM/dd/yyyy",
            "dd/MM/yyyy",
            "M/d/yyyy"
        };
        
        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(value, format, null, 
                System.Globalization.DateTimeStyles.None, out date))
            {
                return true;
            }
        }
        
        return DateTime.TryParse(value, out date);
    }
    
    private bool TryParsePrice(IXLCell cell, out decimal price)
    {
        price = 0;
        
        try
        {
            if (cell.TryGetValue<decimal>(out price))
                return true;
            
            var strValue = cell.GetString();
            if (decimal.TryParse(strValue, out price))
                return true;
        }
        catch { }
        
        return false;
    }
    
    private MarketPrice CreateMarketPrice(
        DateTime priceDate,
        (string Code, string Name, string Type) productInfo,
        decimal price,
        string source,
        string uploadedBy,
        MarketPriceType priceType)
    {
        return MarketPrice.Create(
            priceDate,
            productInfo.Code,
            productInfo.Name,
            priceType,
            price,
            "USD",
            source,
            "Upload",
            priceType == MarketPriceType.FuturesSettlement,
            DateTime.UtcNow,
            uploadedBy,
            null,  // contractMonth (will be set if futures)
            GetRegionFromProductCode(productInfo.Code));  // region based on product code
    }
    
    private string ExtractContractMonth(string header)
    {
        // Extract from headers like "FUEL OIL FUTURES - 380CST SING - AUG25"
        var parts = header.Split('-');
        if (parts.Length >= 3)
        {
            var lastPart = parts[^1].Trim();
            if (lastPart.Length >= 5 && lastPart.Length <= 6)
            {
                return lastPart;
            }
        }
        return string.Empty;
    }

    /// <summary>
    /// Parses product code and contract month from CSV column headers
    /// Supports multiple formats:
    /// 1. Platts MOPS format: "SG380 2511" -> ProductCode="SG380", ContractMonth="202511"
    /// 2. ICE format: "FUEL OIL - 380CST - AUG25" -> ProductCode="FUEL OIL - 380CST", ContractMonth="AUG25"
    /// 3. Plain format: "BRENT" -> ProductCode="BRENT", ContractMonth=null
    /// </summary>
    private (string ProductCode, string? ContractMonth) ParseProductCodeAndMonth(string header)
    {
        if (string.IsNullOrWhiteSpace(header))
            return (header ?? string.Empty, null);

        var trimmedHeader = header.Trim();

        // Strategy 1: Platts MOPS format "PRODUCT YYMM" (e.g., "SG380 2511", "GO 10ppm 2601")
        // Pattern: Product name followed by space and exactly 4 digits (YYMM)
        var plattsMatch = Regex.Match(trimmedHeader, @"^(.+?)\s+(\d{4})$");
        if (plattsMatch.Success)
        {
            var baseProduct = plattsMatch.Groups[1].Value.Trim();
            var yymm = plattsMatch.Groups[2].Value;

            // Validate month (01-12)
            if (int.TryParse(yymm.Substring(0, 2), out var year) &&
                int.TryParse(yymm.Substring(2, 2), out var month) &&
                month >= 1 && month <= 12)
            {
                // Convert YYMM to YYYYMM format
                var fullYear = 2000 + year;
                var contractMonth = $"{fullYear:0000}{month:00}";

                _logger.LogInformation("Parsed Platts MOPS format: '{Header}' -> ProductCode='{ProductCode}', ContractMonth='{ContractMonth}'",
                    trimmedHeader, baseProduct, contractMonth);

                return (baseProduct, contractMonth);
            }
        }

        // Strategy 2: ICE format "PRODUCT - MONTH" (existing ExtractContractMonth logic)
        var extractedMonth = ExtractContractMonth(trimmedHeader);
        if (!string.IsNullOrEmpty(extractedMonth))
        {
            // Extract base product by removing the contract month from the header
            var baseProduct = trimmedHeader.Replace(extractedMonth, "").Trim(' ', '-');

            _logger.LogInformation("Parsed ICE format: '{Header}' -> ProductCode='{ProductCode}', ContractMonth='{ContractMonth}'",
                trimmedHeader, baseProduct, extractedMonth);

            return (baseProduct, extractedMonth);
        }

        // Strategy 3: No month detected - return header as-is with no contract month
        _logger.LogDebug("No contract month pattern found in header: '{Header}'", trimmedHeader);
        return (trimmedHeader, null);
    }

    /// <summary>
    /// Extracts region from product code for spot prices
    /// Singapore: MOPS_* and SING_* prefixes
    /// Dubai: DUBAI prefix
    /// Futures: null (exchange-traded, no physical region)
    /// </summary>
    private static string? GetRegionFromProductCode(string productCode)
    {
        if (string.IsNullOrEmpty(productCode))
            return null;

        // Singapore region - MOPS and SING prefixes
        if (productCode.StartsWith("MOPS_", StringComparison.OrdinalIgnoreCase) ||
            productCode.StartsWith("SING_", StringComparison.OrdinalIgnoreCase))
        {
            return "Singapore";
        }

        // Dubai region - DUBAI prefix
        if (productCode.StartsWith("DUBAI", StringComparison.OrdinalIgnoreCase))
        {
            return "Dubai";
        }

        // Futures products - ICE Singapore codes (exchange-traded, no physical region)
        // Legacy: ICE_, IPE_, DME_ prefixes
        // Current: SG380, SG05, etc.
        if (productCode.StartsWith("ICE_", StringComparison.OrdinalIgnoreCase) ||
            productCode.StartsWith("IPE_", StringComparison.OrdinalIgnoreCase) ||
            productCode.StartsWith("DME_", StringComparison.OrdinalIgnoreCase) ||
            productCode.StartsWith("SG", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // Default: null (unknown or futures)
        return null;
    }
    
    private async Task SaveBatch(
        List<MarketPrice> toCreate,
        List<MarketPrice> toUpdate,
        CancellationToken cancellationToken)
    {
        if (toCreate.Count > 0)
        {
            foreach (var price in toCreate)
            {
                await _marketDataRepository.AddAsync(price, cancellationToken);
            }
        }
        
        if (toUpdate.Count > 0)
        {
            foreach (var price in toUpdate)
            {
                await _marketDataRepository.UpdateAsync(price, cancellationToken);
            }
        }
    }
}