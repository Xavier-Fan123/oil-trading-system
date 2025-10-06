using MediatR;
using ClosedXML.Excel;
using OilTrading.Application.DTOs;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

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
        
        // Process both 380 and 0.5 sheets
        var sheets = new[]
        {
            ("380 Vol-OI", "380CST"),
            ("0.5 Vol-OI", "VLSFO")
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
                
                var contractMonth = ExtractContractMonth(contractHeader);
                if (string.IsNullOrEmpty(contractMonth))
                    continue;
                
                var priceCell = worksheet.Cell(row, colIndex);
                if (!TryParsePrice(priceCell, out var price) || price <= 0)
                    continue;
                
                var productCode = $"ICE_{productBase}_{contractMonth}";
                
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
                    var marketPrice = new MarketPrice
                    {
                        PriceDate = priceDate,
                        ProductCode = productCode,
                        ProductName = $"ICE {productBase} {contractMonth}",
                        PriceType = MarketPriceType.FuturesSettlement,
                        Price = price,
                        Currency = "USD",
                        ContractMonth = contractMonth,
                        Source = contractHeader,
                        DataSource = "ICE Settlement",
                        IsSettlement = true,
                        ImportedAt = DateTime.UtcNow,
                        ImportedBy = uploadedBy
                    };
                    marketPrice.SetId(Guid.NewGuid());
                    marketPrice.SetCreated(uploadedBy, DateTime.UtcNow);
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
        var marketPrice = new MarketPrice
        {
            PriceDate = priceDate,
            ProductCode = productInfo.Code,
            ProductName = productInfo.Name,
            PriceType = priceType,
            Price = price,
            Currency = "USD",
            Source = source,
            DataSource = "Upload",
            IsSettlement = priceType == MarketPriceType.FuturesSettlement,
            ImportedAt = DateTime.UtcNow,
            ImportedBy = uploadedBy
        };
        marketPrice.SetId(Guid.NewGuid());
        marketPrice.SetCreated(uploadedBy, DateTime.UtcNow);
        return marketPrice;
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