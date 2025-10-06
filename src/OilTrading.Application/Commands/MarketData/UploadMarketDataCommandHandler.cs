using MediatR;
using ClosedXML.Excel;
using OilTrading.Application.DTOs;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace OilTrading.Application.Commands.MarketData;

public class UploadMarketDataCommandHandler : IRequestHandler<UploadMarketDataCommand, MarketDataUploadResultDto>
{
    private readonly IMarketDataRepository _marketDataRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UploadMarketDataCommandHandler> _logger;
    
    // Product code mappings for daily prices - expanded to include all products
    private readonly Dictionary<string, (string Code, string Name)> _productMappings = new()
    {
        // Main products
        { "LCOc1", ("ICE_BRENT", "ICE Brent Crude Future") },
        { "LCOCALMc1", ("BRENT_1ST", "Brent 1st Line Future") },
        { "XDOA001", ("DME_OMAN", "DME Oman Crude") },
        { "LGOc1", ("IPE_GASOIL", "IPE Gasoil Futures") },
        { "LGOCALMc1", ("GASOIL_1ST", "Gasoil 1st Line Future") },
        { "PUADV00", ("MOPS_180", "MOPS FO 180cst FOB Sg") },
        { "PPXDK00", ("MOPS_380", "MOPS FO 380cst FOB Sg") },
        { "AMFSA00", ("MOPS_05", "MOPS Marine Fuel 0.5%") },
        { "PUABC00", ("MOPS_180_HI", "MOPS FO 180cst High") },
        { "PUMFD00", ("MOPS_500", "MOPS FO 500cst") },
        { "AAOVC00", ("SING_380", "Singapore 380cst") },
        { "PJABF00", ("SING_180", "Singapore 180cst") },
        { "PGAEY00", ("GO_92", "GO 92 RON") },
        { "PGAEZ00", ("GO_95", "GO 95 RON") },
        { "PGAMS00", ("GO_10PPM", "Gasoil 10ppm") },
        { "AAPPF00", ("JET_KERO", "Jet Kerosene") },
        { "AAFEX00", ("NAPHTHA", "Naphtha") },
        { "PAAAD00", ("DUBAI", "Dubai Crude") },
        { "PAAAP00", ("MURBAN", "Murban Crude") },
        { "PHALF00", ("FUEL_OIL", "Fuel Oil") },
        { "PUABE00", ("HSFO", "High Sulfur Fuel Oil") },
        { "AACUA00", ("LSFO", "Low Sulfur Fuel Oil") },
        { "AAIDC00", ("VLSFO", "Very Low Sulfur Fuel Oil") },
        { "AAGZF00", ("MGO", "Marine Gas Oil") },
        { "PPXDL00", ("MOPS_380_PREM", "MOPS 380cst Premium") },
        { "FOFSB00", ("FUEL_OIL_SPREAD", "Fuel Oil Spread") },
        { "AAOVD00", ("GO_10PPM_PREM", "Gasoil 10ppm Premium") }
    };
    
    // Contract month mapping
    private readonly Dictionary<int, string> _monthMappings = new()
    {
        { 1, "JAN" }, { 2, "FEB" }, { 3, "MAR" }, { 4, "APR" },
        { 5, "MAY" }, { 6, "JUN" }, { 7, "JUL" }, { 8, "AUG" },
        { 9, "SEP" }, { 10, "OCT" }, { 11, "NOV" }, { 12, "DEC" }
    };

    private const int BATCH_SIZE = 100; // Process in batches for performance
    private const int MAX_ERRORS = 50; // Stop processing if too many errors
    
    public UploadMarketDataCommandHandler(
        IMarketDataRepository marketDataRepository,
        IUnitOfWork unitOfWork,
        ILogger<UploadMarketDataCommandHandler> logger)
    {
        _marketDataRepository = marketDataRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<MarketDataUploadResultDto> Handle(
        UploadMarketDataCommand request, 
        CancellationToken cancellationToken)
    {
        var result = new MarketDataUploadResultDto();
        
        try
        {
            // If overwrite mode is enabled, delete existing data first
            if (request.OverwriteExisting)
            {
                _logger.LogInformation("Overwrite mode enabled. Deleting existing market data before upload.");
                await _marketDataRepository.DeleteAllAsync(cancellationToken);
                result.Messages.Add("Existing market data deleted for overwrite");
            }
            
            // Check if it's a CSV file based on extension
            var isCSV = request.FileName.ToLower().EndsWith(".csv");
            
            if (isCSV)
            {
                // Process CSV file
                result = await ProcessCSVFile(request, cancellationToken);
            }
            else
            {
                // Process Excel file
                using var stream = new MemoryStream(request.FileContent);
                using var workbook = new XLWorkbook(stream);
                
                if (request.FileType == "Spot")
                {
                    result = await ProcessDailyPrices(workbook, request.UploadedBy, cancellationToken);
                }
                else if (request.FileType == "Futures")
                {
                    result = await ProcessICESettlement(workbook, request.UploadedBy, cancellationToken);
                }
            }
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            result.Success = true;
            
            _logger.LogInformation(
                "Market data upload completed. File: {FileName}, Type: {FileType}, Records: {RecordsProcessed}",
                request.FileName, request.FileType, result.RecordsProcessed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing market data upload");
            result.Success = false;
            result.Errors.Add($"Error: {ex.Message}");
        }
        
        return result;
    }
    
    private async Task<MarketDataUploadResultDto> ProcessDailyPrices(
        IXLWorkbook workbook, 
        string uploadedBy,
        CancellationToken cancellationToken)
    {
        var result = new MarketDataUploadResultDto();
        
        // Try to find the worksheet with different possible names - Origin is the primary sheet name
        var possibleNames = new[] { "Origin", "originfile", "Daily Prices", "Daily", "Prices", "MOPS", "Sheet1" };
        IXLWorksheet? worksheet = null;
        
        foreach (var name in possibleNames)
        {
            try
            {
                worksheet = workbook.Worksheet(name);
                if (worksheet != null)
                {
                    _logger.LogInformation("Found worksheet: {WorksheetName}", name);
                    break;
                }
            }
            catch
            {
                continue;
            }
        }
        
        if (worksheet == null)
        {
            var availableSheets = string.Join(", ", workbook.Worksheets.Select(w => w.Name));
            result.Errors.Add($"Daily Prices worksheet not found. Expected names: {string.Join(", ", possibleNames)}. Available worksheets: {availableSheets}");
            return result;
        }
        
        // Map column headers to product codes
        var columnMappings = new Dictionary<int, string>();
        var headerRow = 1; // Row 1 contains column headers
        
        // Scan the header row to find product columns (starting from column C/3)
        for (int col = 3; col <= 29; col++) // Up to column AC
        {
            var headerCell = worksheet.Cell(headerRow, col);
            var headerValue = headerCell.GetString();
            if (!string.IsNullOrEmpty(headerValue) && _productMappings.ContainsKey(headerValue))
            {
                columnMappings[col] = headerValue;
                _logger.LogDebug("Found product column: {Product} at column {Column}", headerValue, col);
            }
        }
        
        if (columnMappings.Count == 0)
        {
            result.Errors.Add("No valid product columns found in the worksheet");
            return result;
        }
        
        // Find the last row with data
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        var startRow = 3; // Data starts from row 3 (row 1 is headers, row 2 is "Trade Close")
        
        if (lastRow < startRow)
        {
            result.Messages.Add("No data rows found in the worksheet");
            return result;
        }
        
        // Process last 5 days of data (or all available data if less) - reduced for testing
        var rowsToProcess = Math.Min(5, lastRow - startRow + 1);
        var firstRowToProcess = lastRow - rowsToProcess + 1;
        
        _logger.LogInformation("Processing rows {FirstRow} to {LastRow} ({RowCount} rows)", 
            firstRowToProcess, lastRow, rowsToProcess);
        
        // Process each data row
        for (int row = firstRowToProcess; row <= lastRow; row++)
        {
            var dateCell = worksheet.Cell(row, 2); // Column B has timestamps
            if (!DateTime.TryParse(dateCell.GetString(), out var priceDate))
            {
                _logger.LogWarning("Skipping row {Row}: Invalid date format", row);
                continue;
            }
            
            // Process each product column for this date
            foreach (var (colIndex, originalCode) in columnMappings)
            {
                var priceCell = worksheet.Cell(row, colIndex);
                if (priceCell.TryGetValue<decimal>(out var price) && price > 0)
                {
                    if (_productMappings.TryGetValue(originalCode, out var productInfo))
                    {
                        // Check if price already exists for this date and product
                        var existingPrice = await _marketDataRepository.GetByProductAndDateAsync(
                            productInfo.Code, priceDate, cancellationToken);
                        
                        if (existingPrice != null)
                        {
                            // Update existing price if different
                            if (existingPrice.Price != price)
                            {
                                existingPrice.Price = price;
                                existingPrice.SetUpdatedBy(uploadedBy);
                                await _marketDataRepository.UpdateAsync(existingPrice, cancellationToken);
                                result.RecordsUpdated++;
                            }
                        }
                        else
                        {
                            // Create new price record
                            var marketPrice = new MarketPrice
                            {
                                PriceDate = priceDate,
                                ProductCode = productInfo.Code,
                                ProductName = productInfo.Name,
                                PriceType = MarketPriceType.Spot,
                                Price = price,
                                Currency = "USD",
                                Source = originalCode,
                                DataSource = "Daily Prices Upload",
                                IsSettlement = false,
                                ImportedAt = DateTime.UtcNow,
                                ImportedBy = uploadedBy
                            };
                            marketPrice.SetId(Guid.NewGuid());
                            marketPrice.SetCreated(uploadedBy, DateTime.UtcNow);
                            
                            await _marketDataRepository.AddAsync(marketPrice, cancellationToken);
                            result.RecordsCreated++;
                            
                            // Add to imported prices list (limit to first 100 for response)
                            if (result.ImportedPrices.Count < 100)
                            {
                                result.ImportedPrices.Add(new MarketPriceDto
                                {
                                    Id = marketPrice.Id,
                                    PriceDate = marketPrice.PriceDate,
                                    ProductCode = marketPrice.ProductCode,
                                    ProductName = marketPrice.ProductName,
                                    Price = marketPrice.Price,
                                    Currency = marketPrice.Currency,
                                    PriceType = "Spot"
                                });
                            }
                        }
                        
                        result.RecordsProcessed++;
                    }
                }
            }
        }
        
        result.Messages.Add($"Processed {rowsToProcess} days of daily prices");
        return result;
    }
    
    private async Task<MarketDataUploadResultDto> ProcessICESettlement(
        IXLWorkbook workbook,
        string uploadedBy,
        CancellationToken cancellationToken)
    {
        var result = new MarketDataUploadResultDto();
        
        // Process 380 Vol-OI sheet
        var sheet380 = TryGetWorksheet(workbook, new[] { "380 Vol-OI", "380 VOL-OI", "380VOL-OI" });
        if (sheet380 != null)
        {
            _logger.LogInformation("Processing 380 Vol-OI sheet");
            await ProcessFuturesSheet(sheet380, "380CST", uploadedBy, result, cancellationToken);
        }
        else
        {
            result.Messages.Add("380 Vol-OI sheet not found");
        }
        
        // Process 0.5 Vol-OI sheet
        var sheet05 = TryGetWorksheet(workbook, new[] { "0.5 Vol-OI", "0.5 VOL-OI", "0.5VOL-OI" });
        if (sheet05 != null)
        {
            _logger.LogInformation("Processing 0.5 Vol-OI sheet");
            await ProcessFuturesSheet(sheet05, "VLSFO", uploadedBy, result, cancellationToken);
        }
        else
        {
            result.Messages.Add("0.5 Vol-OI sheet not found");
        }
        
        if (sheet380 == null && sheet05 == null)
        {
            var availableSheets = string.Join(", ", workbook.Worksheets.Select(w => w.Name));
            result.Errors.Add($"No ICE futures sheets found. Available worksheets: {availableSheets}");
        }
        
        return result;
    }
    
    private IXLWorksheet? TryGetWorksheet(IXLWorkbook workbook, string[] possibleNames)
    {
        foreach (var name in possibleNames)
        {
            try
            {
                var worksheet = workbook.Worksheet(name);
                if (worksheet != null) return worksheet;
            }
            catch
            {
                continue;
            }
        }
        return null;
    }
    
    private async Task ProcessFuturesSheet(
        IXLWorksheet worksheet,
        string productBase,
        string uploadedBy,
        MarketDataUploadResultDto result,
        CancellationToken cancellationToken)
    {
        // The futures settlement prices are in columns N, Q, T, W, Z, AC, AF (every 3rd column starting from 13)
        var settlementColumns = new[] { 14, 17, 20, 23, 26, 29, 32 }; // Excel columns N, Q, T, W, Z, AC, AF (1-based to 0-based)
        
        // Get the date column (column M, index 12)
        var dateColumnIndex = 13; // Column M (1-based)
        
        // Find the last row with data
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        if (lastRow < 3) return; // No data
        
        // Process the most recent data (typically row 2 has the latest date)
        var startRow = 2; // Skip header row
        
        // Process last 3 days of data - reduced for testing
        var rowsToProcess = Math.Min(3, lastRow - startRow + 1);
        
        for (int row = startRow; row < startRow + rowsToProcess; row++)
        {
            // Get the date from column M
            var dateCell = worksheet.Cell(row, dateColumnIndex);
            if (!DateTime.TryParse(dateCell.GetString(), out var priceDate))
            {
                continue;
            }
            
            // Process each settlement column
            for (int i = 0; i < settlementColumns.Length; i++)
            {
                var colIndex = settlementColumns[i];
                
                // Get the contract month from row 1
                var contractHeaderCell = worksheet.Cell(1, colIndex);
                var contractHeader = contractHeaderCell.GetString();
                
                if (string.IsNullOrEmpty(contractHeader)) continue;
                
                // Extract contract month (e.g., "AUG25" from "FUEL OIL FUTURES - 380CST SING - AUG25")
                var contractMonth = ExtractContractMonthFromHeader(contractHeader);
                if (string.IsNullOrEmpty(contractMonth)) continue;
                
                // Get the settlement price
                var priceCell = worksheet.Cell(row, colIndex);
                if (priceCell.TryGetValue<decimal>(out var price) && price > 0)
                {
                    var productCode = $"ICE_{productBase}_{contractMonth}";
                    var productName = $"ICE {productBase} {contractMonth}";
                    
                    // Check if price already exists
                    var existingPrice = await _marketDataRepository.GetByProductAndDateAsync(
                        productCode, priceDate, cancellationToken);
                    
                    if (existingPrice != null)
                    {
                        if (existingPrice.Price != price)
                        {
                            existingPrice.Price = price;
                            existingPrice.SetUpdatedBy(uploadedBy);
                            await _marketDataRepository.UpdateAsync(existingPrice, cancellationToken);
                            result.RecordsUpdated++;
                        }
                    }
                    else
                    {
                        var marketPrice = new MarketPrice
                        {
                            PriceDate = priceDate,
                            ProductCode = productCode,
                            ProductName = productName,
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
                        
                        await _marketDataRepository.AddAsync(marketPrice, cancellationToken);
                        result.RecordsCreated++;
                        
                        // Add to imported prices list (limit to first 100)
                        if (result.ImportedPrices.Count < 100)
                        {
                            result.ImportedPrices.Add(new MarketPriceDto
                            {
                                Id = marketPrice.Id,
                                PriceDate = marketPrice.PriceDate,
                                ProductCode = marketPrice.ProductCode,
                                ProductName = marketPrice.ProductName,
                                Price = marketPrice.Price,
                                Currency = marketPrice.Currency,
                                ContractMonth = marketPrice.ContractMonth,
                                PriceType = "FuturesSettlement"
                            });
                        }
                    }
                    
                    result.RecordsProcessed++;
                }
            }
        }
        
        result.Messages.Add($"Processed {productBase} futures prices");
    }
    
    private string ExtractContractMonthFromHeader(string header)
    {
        // Extract contract month from headers like "FUEL OIL FUTURES - 380CST SING - AUG25"
        var parts = header.Split('-');
        if (parts.Length >= 3)
        {
            var lastPart = parts[parts.Length - 1].Trim();
            // Check if it matches pattern like "AUG25", "SEP25", etc.
            if (lastPart.Length >= 5 && lastPart.Length <= 6)
            {
                return lastPart;
            }
        }
        return string.Empty;
    }
    
    private async Task<MarketDataUploadResultDto> ProcessCSVFile(
        UploadMarketDataCommand request,
        CancellationToken cancellationToken)
    {
        var result = new MarketDataUploadResultDto();
        const int MAX_ERRORS = 50;
        
        try
        {
            // Convert byte array to string
            var csvContent = System.Text.Encoding.UTF8.GetString(request.FileContent);
            var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            if (lines.Length < 2)
            {
                result.Errors.Add("CSV file must have at least 2 rows (header and data)");
                return result;
            }
            
            // Parse unified CSV format:
            // Row 0: Date/产品名称1, 产品名称2, ...
            // Row 1+: 日期, 价格1, 价格2, ...
            
            var headerLine = lines[0].Trim();
            var headers = headerLine.Split(',');
            
            if (headers.Length < 2)
            {
                result.Errors.Add("CSV must have at least 2 columns (Date and one product)");
                return result;
            }
            
            _logger.LogInformation("Processing unified CSV format with {ColumnCount} columns", headers.Length);
            
            // Build column mappings starting from column 1 (skip Date column)
            var columnMappings = new Dictionary<int, (string ProductCode, string ContractMonth, MarketPriceType PriceType)>();
            
            for (int col = 1; col < headers.Length; col++)
            {
                var productHeader = headers[col]?.Trim();
                if (string.IsNullOrEmpty(productHeader)) continue;
                
                // Analyze product type and extract info
                var productInfo = AnalyzeProductHeader(productHeader);
                if (productInfo != null)
                {
                    columnMappings[col] = productInfo.Value;
                    _logger.LogDebug("Column {Col}: {Header} -> Code: {Code}, Month: {Month}, Type: {Type}",
                        col, productHeader, productInfo.Value.ProductCode, productInfo.Value.ContractMonth, productInfo.Value.PriceType);
                }
                else
                {
                    _logger.LogWarning("Could not parse product header: {Header}", productHeader);
                }
            }
            
            if (columnMappings.Count == 0)
            {
                result.Errors.Add("No valid product columns found in CSV header");
                return result;
            }
            
            // Process data rows (starting from row 1)
            for (int rowIndex = 1; rowIndex < lines.Length; rowIndex++)
            {
                var line = lines[rowIndex].Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                var columns = line.Split(',');
                if (columns.Length < 2) continue;
                
                // Parse date from first column
                var dateStr = columns[0]?.Trim();
                if (string.IsNullOrEmpty(dateStr) || !DateTime.TryParse(dateStr, out var priceDate))
                {
                    _logger.LogWarning("Skipping row {Row}: Invalid date '{Date}'", rowIndex + 1, dateStr);
                    continue;
                }
                
                // Process each product column
                foreach (var (colIndex, (productCode, contractMonth, priceType)) in columnMappings)
                {
                    if (colIndex >= columns.Length) continue;
                    
                    var priceStr = columns[colIndex]?.Trim();
                    if (string.IsNullOrEmpty(priceStr) || !decimal.TryParse(priceStr, out var price) || price <= 0)
                    {
                        continue; // Skip empty or invalid prices
                    }
                    
                    try
                    {
                        // Create unique key for product+contract combination
                        var fullProductCode = string.IsNullOrEmpty(contractMonth) ? productCode : $"{productCode}_{contractMonth}";
                        
                        // Check if price already exists
                        var existingPrice = await _marketDataRepository.GetByProductAndDateAsync(
                            fullProductCode, priceDate, cancellationToken);
                        
                        if (existingPrice != null)
                        {
                            if (existingPrice.Price != price)
                            {
                                existingPrice.Price = price;
                                existingPrice.SetUpdatedBy(request.UploadedBy);
                                await _marketDataRepository.UpdateAsync(existingPrice, cancellationToken);
                                result.RecordsUpdated++;
                            }
                        }
                        else
                        {
                            var marketPrice = new MarketPrice
                            {
                                ProductCode = fullProductCode,
                                ProductName = GenerateProductName(productCode, contractMonth, priceType),
                                PriceDate = priceDate,
                                Price = price,
                                Currency = "USD",
                                PriceType = priceType,
                                ContractMonth = contractMonth,
                                Source = headers[colIndex],
                                DataSource = "CSV Upload",
                                ImportedBy = request.UploadedBy,
                                ImportedAt = DateTime.UtcNow
                            };
                            marketPrice.SetId(Guid.NewGuid());
                            marketPrice.SetCreated(request.UploadedBy, DateTime.UtcNow);
                            
                            await _marketDataRepository.AddAsync(marketPrice, cancellationToken);
                            result.RecordsCreated++;
                            
                            // Add to result for preview (limit to first 100)
                            if (result.ImportedPrices.Count < 100)
                            {
                                result.ImportedPrices.Add(new MarketPriceDto
                                {
                                    Id = marketPrice.Id,
                                    PriceDate = marketPrice.PriceDate,
                                    ProductCode = marketPrice.ProductCode,
                                    ProductName = marketPrice.ProductName,
                                    Price = marketPrice.Price,
                                    Currency = marketPrice.Currency,
                                    PriceType = marketPrice.PriceType.ToString(),
                                    ContractMonth = marketPrice.ContractMonth
                                });
                            }
                        }
                        
                        result.RecordsProcessed++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing price for {Product} on {Date}", productCode, priceDate);
                        result.Errors.Add($"Error processing {productCode} on {priceDate:yyyy-MM-dd}: {ex.Message}");
                        
                        if (result.Errors.Count >= MAX_ERRORS)
                        {
                            result.Errors.Add("Too many errors, stopping processing");
                            break;
                        }
                    }
                }
                
                if (result.Errors.Count >= MAX_ERRORS) break;
            }
            
            result.Messages.Add($"Processed {result.RecordsProcessed} price records from unified CSV format");
            result.Messages.Add($"Found {columnMappings.Count} product columns");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CSV file");
            result.Errors.Add($"CSV processing error: {ex.Message}");
        }
        
        return result;
    }
    
    private (string ProductCode, string ContractMonth, MarketPriceType PriceType)? AnalyzeProductHeader(string header)
    {
        if (string.IsNullOrEmpty(header)) return null;
        
        // Check for futures: ends with 4 digits (YYMM format like 2508, 2509)
        var futuresMatch = Regex.Match(header, @"^(.+?)(\d{4})$");
        if (futuresMatch.Success)
        {
            var baseName = futuresMatch.Groups[1].Value.Trim();
            var contractCode = futuresMatch.Groups[2].Value; // e.g., "2508"
            
            // Convert YYMM to full date format
            if (contractCode.Length == 4 && int.TryParse(contractCode.Substring(0, 2), out var year) && 
                int.TryParse(contractCode.Substring(2, 2), out var month) && month >= 1 && month <= 12)
            {
                var fullYear = 2000 + year; // 25 -> 2025
                var contractMonth = $"{fullYear:0000}{month:00}"; // "202508"
                var futuresProductCode = MapFuturesProductCode(baseName);
                
                _logger.LogDebug("Futures detected: {Header} -> Code: {Code}, Month: {Month}", header, futuresProductCode, contractMonth);
                return (futuresProductCode, contractMonth, MarketPriceType.FuturesSettlement);
            }
        }
        
        // Check for futures: contains date pattern like "2025/8/25"
        var dateMatch = Regex.Match(header, @"(\d{4})/(\d{1,2})/(\d{1,2})");
        if (dateMatch.Success)
        {
            if (int.TryParse(dateMatch.Groups[1].Value, out var year) && 
                int.TryParse(dateMatch.Groups[2].Value, out var month) && 
                int.TryParse(dateMatch.Groups[3].Value, out var day))
            {
                var contractMonth = $"{year:0000}{month:00}";
                var baseName = header.Substring(0, dateMatch.Index).Trim();
                var dateBasedProductCode = MapFuturesProductCode(baseName);
                
                _logger.LogDebug("Futures with date detected: {Header} -> Code: {Code}, Month: {Month}", header, dateBasedProductCode, contractMonth);
                return (dateBasedProductCode, contractMonth, MarketPriceType.FuturesSettlement);
            }
        }
        
        // Otherwise, treat as spot
        var spotProductCode = MapSpotProductCode(header);
        _logger.LogDebug("Spot detected: {Header} -> Code: {Code}", header, spotProductCode);
        return (spotProductCode, string.Empty, MarketPriceType.Spot);
    }
    
    private string MapFuturesProductCode(string baseName)
    {
        // Map futures product names to codes
        if (baseName.Contains("FUEL OIL") && baseName.Contains("380CST"))
            return "FO_380";
        if (baseName.Contains("FUEL OIL") && baseName.Contains("MARINE"))
            return "FO_MARINE";
        if (baseName.Contains("BRENT") && baseName.Contains("CRUDE"))
            return "BRENT";
        if (baseName.Contains("WTI") || baseName.Contains("CRUDE"))
            return "WTI";
        if (baseName.Contains("GASOIL"))
            return "GASOIL";
        
        // Default: clean up the name
        return baseName.Replace(" FUTURES", "").Replace(" ", "_").ToUpper();
    }
    
    private string MapSpotProductCode(string productName)
    {
        // Normalize the product name for mapping
        var normalizedName = productName.ToUpper().Trim();
        
        // Map to standardized product codes with specific handling for similar products
        if (normalizedName.Contains("ICE BRENT") && normalizedName.Contains("FUTURE"))
            return "BRENT";
        if (normalizedName.Contains("BRENT 1ST LINE"))
            return "BRENT_1ST";
        if (normalizedName.Contains("DME OMAN"))
            return "OMAN";
        
        // Handle Gasoil variants
        if (normalizedName.Contains("IPE GASOIL") && normalizedName.Contains("FUTURES"))
            return "GASOIL_FUTURES";
        if (normalizedName.Contains("IPE GASOIL") && normalizedName.Contains("1ST LINE"))
            return "GASOIL_1ST";
        
        // Handle MOPS FO 180cst variants
        if (normalizedName.Contains("MOPS FO 180CST") && normalizedName.Contains("PREMIUM"))
            return "MOPS_180_PREMIUM";
        if (normalizedName.Contains("MOPS FO 180CST") && !normalizedName.Contains("PREMIUM"))
            return "MOPS_180";
        
        // Handle MOPS FO 380cst variants  
        if (normalizedName.Contains("MOPS FO 380CST") && normalizedName.Contains("PREMIUM"))
            return "MOPS_380_PREMIUM";
        if (normalizedName.Contains("MOPS FO 380CST") && !normalizedName.Contains("PREMIUM"))
            return "MOPS_380";
        
        // Handle Marine Fuel 0.5% variants
        if (normalizedName.Contains("MARINE FUEL 0.5%") && normalizedName.Contains("PREMIUM"))
            return "MARINE_05_PREMIUM";
        if (normalizedName.Contains("MARINE FUEL 0.5%") && normalizedName.Contains("RTDM"))
            return "MARINE_05_RTDM";
        if (normalizedName.Contains("MARINE FUEL 0.5%") && !normalizedName.Contains("PREMIUM") && !normalizedName.Contains("RTDM"))
            return "MARINE_05";
        
        // Handle other specific products
        if (normalizedName.Contains("FUEL OIL 3.5%") && normalizedName.Contains("RTDM"))
            return "FUEL_OIL_35_RTDM";
        if (normalizedName.Contains("GAS OIL 10PPM") && normalizedName.Contains("PREMIUM"))
            return "GASOIL_10PPM_PREMIUM";
        if (normalizedName.Contains("GAS OIL 10PPM") && !normalizedName.Contains("PREMIUM"))
            return "GAS_OIL_10PPM";
        if (normalizedName.Contains("GAS OIL 50PPM"))
            return "GAS_OIL_50PPM";
        if (normalizedName.Contains("GAS OIL 500PPM"))
            return "GAS_OIL_500PPM";
        if (normalizedName.Contains("JET KERO"))
            return "JET_KERO";
        if (normalizedName.Contains("GASOLINE 92"))
            return "GASOLINE_92_FOB_SG";
        if (normalizedName.Contains("GASOLINE 95"))
            return "GASOLINE_95_FOB_SG";
        if (normalizedName.Contains("GASOLINE 97"))
            return "GASOLINE_97_FOB_SG";
        if (normalizedName.Contains("NAPHTHA") && normalizedName.Contains("JAPAN"))
            return "NAPHTHA_CFR_JAPAN";
        if (normalizedName.Contains("NAPHTHA") && normalizedName.Contains("SING"))
            return "NAPHTHA_FOB_SING";
        if (normalizedName.Contains("MTBE") && normalizedName.Contains("SPORE"))
            return "MTBE_FOB_SPORE";
        if (normalizedName.Contains("MOP ARAB GULF 180CST"))
            return "MOP_ARAB_GULF_180CST";
        if (normalizedName.Contains("MOP ARAB GULF 380CST"))
            return "MOP_ARAB_GULF_380CST";
        if (normalizedName.Contains("MOPAG 2500PPM"))
            return "MOPAG_2500PPM";
        
        // Extract code from parentheses if available (USD/BBLS, USD/MT etc.)
        var codeMatch = Regex.Match(productName, @"^([^(]+)");
        if (codeMatch.Success)
        {
            return codeMatch.Groups[1].Value.Trim().Replace(" ", "_").Replace(".", "").Replace("%", "PCT").ToUpper();
        }
        
        // Default: clean up the name
        return productName.Replace(" ", "_").Replace(".", "").Replace("%", "PCT").ToUpper();
    }
    
    private string GenerateProductName(string productCode, string contractMonth, MarketPriceType priceType)
    {
        if (priceType == MarketPriceType.FuturesSettlement && !string.IsNullOrEmpty(contractMonth))
        {
            // Parse contract month to readable format
            if (contractMonth.Length == 6 && 
                int.TryParse(contractMonth.Substring(0, 4), out var year) && 
                int.TryParse(contractMonth.Substring(4, 2), out var month))
            {
                var monthName = new DateTime(year, month, 1).ToString("MMM");
                return $"{productCode} {monthName}{year.ToString().Substring(2)} Futures";
            }
            return $"{productCode} Futures {contractMonth}";
        }
        
        return $"{productCode} Spot";
    }
}