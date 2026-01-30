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

            // Log inner exception details for better debugging
            var errorMsg = $"Error: {ex.Message}";
            if (ex.InnerException != null)
            {
                errorMsg += $" | Inner: {ex.InnerException.Message}";
                _logger.LogError(ex.InnerException, "Inner exception details");
            }

            result.Errors.Add(errorMsg);
        }
        
        return result;
    }
    
    private async Task<MarketDataUploadResultDto> ProcessDailyPrices(
        IXLWorkbook workbook,
        string uploadedBy,
        CancellationToken cancellationToken)
    {
        var result = new MarketDataUploadResultDto();

        // STEP 1: Find the Daily Prices worksheet (supports both Origin format and Daily Prices format)
        var possibleNames = new[]
        {
            "2025 Daily MOPS_new",  // Latest Daily Prices format
            "Origin", "originfile", "Daily Prices", "Daily", "Prices", "MOPS", "Sheet1"
        };
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

        // STEP 2: Extract and map product headers from Row 1
        // Build intelligent product mapping from actual Excel headers
        var columnMappings = BuildSpotPriceColumnMappings(worksheet);

        if (columnMappings.Count == 0)
        {
            result.Errors.Add("No valid product columns found in the worksheet. Column headers should contain product names like 'MOPS FO 380cst FOB Sg', 'Naphtha FOB Sing Cargo', etc.");
            return result;
        }

        _logger.LogInformation("Identified {ProductCount} product columns for spot price import", columnMappings.Count);

        // STEP 3: Find the last row with data
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        var startRow = 3; // Data starts from row 3 (row 1 is headers, row 2 is "Average"/metadata)

        if (lastRow < startRow)
        {
            result.Messages.Add("No data rows found in the worksheet");
            return result;
        }

        // STEP 4: Process all available data (or configurable limit)
        var rowsToProcess = Math.Min(500, lastRow - startRow + 1); // Process up to 500 days
        var firstRowToProcess = lastRow - rowsToProcess + 1;

        _logger.LogInformation("Processing {RowCount} rows of daily spot prices (rows {FirstRow} to {LastRow})",
            rowsToProcess, firstRowToProcess, lastRow);

        // STEP 5: Process each data row with batch commit logic
        var recordsToAdd = new List<MarketPrice>();
        const int BATCH_COMMIT_SIZE = 50; // Commit every 50 product-date combinations

        for (int row = firstRowToProcess; row <= lastRow; row++)
        {
            // Get date from Column B
            var dateCell = worksheet.Cell(row, 2);
            if (!DateTime.TryParse(dateCell.GetString(), out var priceDate))
            {
                _logger.LogWarning("Skipping row {Row}: Invalid date format in column B", row);
                result.RecordsSkipped++;
                continue;
            }

            // Process each product column for this date
            foreach (var (colIndex, productMapping) in columnMappings)
            {
                var priceCell = worksheet.Cell(row, colIndex);

                // Try to parse price as decimal
                if (!TryParsePrice(priceCell, out var price) || price <= 0)
                {
                    continue; // Skip empty or invalid prices
                }

                // Check if price already exists for this date and product (Spot prices only)
                // Use GetSpotPriceAsync which properly checks the UNIQUE index constraint:
                // (ProductCode, ContractMonth, PriceDate, PriceType)
                // For Spot prices: ContractMonth=null, PriceType=Spot
                var existingPrice = await _marketDataRepository.GetSpotPriceAsync(
                    productMapping.ProductCode, priceDate, cancellationToken);

                if (existingPrice != null)
                {
                    // Update existing price if different
                    if (Math.Abs(existingPrice.Price - price) > 0.001m)
                    {
                        existingPrice.Price = price;
                        existingPrice.SetUpdatedBy(uploadedBy);
                        await _marketDataRepository.UpdateAsync(existingPrice, cancellationToken);
                        result.RecordsUpdated++;
                        _logger.LogDebug("Updated {Product} price on {Date}: {Price}",
                            productMapping.ProductCode, priceDate.ToString("yyyy-MM-dd"), price);
                    }
                }
                else
                {
                    // CRITICAL: Check if this product/date is already in the batch to avoid UNIQUE constraint violations
                    // This handles the case where the same product appears multiple times in the Excel file
                    var isDuplicateInBatch = recordsToAdd.Any(r =>
                        r.ProductCode == productMapping.ProductCode &&
                        r.PriceDate.Date == priceDate.Date &&
                        string.IsNullOrEmpty(r.ContractMonth) &&
                        r.PriceType == MarketPriceType.Spot);

                    if (isDuplicateInBatch)
                    {
                        result.Errors.Add($"Skipped duplicate entry in Excel file: {productMapping.ProductCode} on {priceDate:yyyy-MM-dd}");
                        _logger.LogWarning("Skipped duplicate in batch: {Product} on {Date}",
                            productMapping.ProductCode, priceDate.ToString("yyyy-MM-dd"));
                    }
                    else
                    {
                        // Create new price record (batch)
                        var marketPrice = MarketPrice.Create(
                            priceDate,
                            productMapping.ProductCode,
                            productMapping.ProductName,
                            MarketPriceType.Spot,
                            price,
                            "USD",
                            productMapping.HeaderText,
                            "Daily Prices Upload",
                            false,
                            DateTime.UtcNow,
                            uploadedBy,
                            null); // Spot prices don't have contract months

                        // Set unit if available
                        if (!string.IsNullOrEmpty(productMapping.Unit))
                        {
                            if (Enum.TryParse<MarketPriceUnit>(productMapping.Unit, out var unit))
                            {
                                marketPrice.Unit = unit;
                            }
                        }

                        recordsToAdd.Add(marketPrice);
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

                    // Batch commit
                    if (recordsToAdd.Count >= BATCH_COMMIT_SIZE)
                    {
                        foreach (var record in recordsToAdd)
                        {
                            await _marketDataRepository.AddAsync(record, cancellationToken);
                        }
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        recordsToAdd.Clear();
                        _logger.LogDebug("Committed batch of {Count} new price records", BATCH_COMMIT_SIZE);
                    }
                }

                result.RecordsProcessed++;
            }
        }

        // Commit remaining records
        if (recordsToAdd.Count > 0)
        {
            foreach (var record in recordsToAdd)
            {
                await _marketDataRepository.AddAsync(record, cancellationToken);
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Committed final batch of {Count} price records", recordsToAdd.Count);
            recordsToAdd.Clear();
        }

        result.Messages.Add($"Successfully processed {rowsToProcess} days of daily spot prices");
        result.Messages.Add($"Created {result.RecordsCreated} new price records, updated {result.RecordsUpdated} existing records");
        return result;
    }

    /// <summary>
    /// Builds intelligent column mappings by analyzing Excel headers using oil trading expertise
    /// Maps display headers to standardized product codes while preserving unit information
    /// </summary>
    private Dictionary<int, (string ProductCode, string ProductName, string HeaderText, string? Unit)> BuildSpotPriceColumnMappings(
        IXLWorksheet worksheet)
    {
        var mappings = new Dictionary<int, (string ProductCode, string ProductName, string HeaderText, string? Unit)>();
        var headerRow = 1;

        // Find the last column with data
        var lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 50; // Default to 50 if unable to detect

        // Process columns starting from column C (column 3)
        for (int col = 3; col <= lastColumn; col++)
        {
            var headerCell = worksheet.Cell(headerRow, col);
            var headerText = headerCell.GetString()?.Trim();

            if (string.IsNullOrEmpty(headerText))
                continue;

            // Intelligent product mapping using oil trading domain knowledge
            var productMapping = MapSpotPriceHeaderToProduct(headerText);

            if (productMapping != null)
            {
                mappings[col] = (productMapping.Value.ProductCode, productMapping.Value.ProductName, headerText, productMapping.Value.Unit);

                _logger.LogDebug(
                    "Mapped Column {Column}: '{HeaderText}' -> {ProductCode} ({Unit})",
                    col, headerText, productMapping.Value.ProductCode, productMapping.Value.Unit ?? "N/A");
            }
            else
            {
                _logger.LogDebug("Skipping unrecognized column {Column}: '{HeaderText}'", col, headerText);
            }
        }

        return mappings;
    }

    /// <summary>
    /// Expert-level product mapping for spot prices from Daily Prices.xlsx format
    /// Combines oil trading knowledge with system architecture to create flexible, maintainable mappings
    ///
    /// Oil Product Categories:
    /// - Crude Oils: Brent, WTI, Oman, Dubai, Murban
    /// - Fuel Oils: 180cst, 380cst, HSFO, LSFO, VLSFO, Heavy Fuel Oil variants
    /// - Distillates: Gasoil, Gas Oil, Marine Gas Oil (MGO), Jet Kero, Naphtha
    /// - Gasoline: 92 RON, 95 RON, 97 RON
    /// - Spreads & Premiums: Contango spreads, 3.5% premiums, arbitrage opportunities
    /// - Specialty: MTBE, bunker fuel variants, delivered products
    /// </summary>
    private (string ProductCode, string ProductName, string? Unit)? MapSpotPriceHeaderToProduct(string headerText)
    {
        if (string.IsNullOrEmpty(headerText))
            return null;

        var normalized = headerText.ToUpperInvariant().Trim();

        // CRUDE OILS - Foundation of oil trading
        if (normalized.Contains("ICE BRENT FUTURE"))
            return ("BRENT_CRUDE", "ICE Brent Crude Oil", "BBL");
        if (normalized.Contains("BRENT 1ST LINE"))
            return ("BRENT_1ST", "Brent 1st Line", "BBL");
        if (normalized.Contains("DME OMAN"))
            return ("OMAN_CRUDE", "DME Oman Crude", "BBL");
        if (normalized.Contains("DUBAI CRUDE"))
            return ("DUBAI_CRUDE", "Dubai Crude Oil", "BBL");
        if (normalized.Contains("MURBAN CRUDE"))
            return ("MURBAN_CRUDE", "Murban Crude Oil", "BBL");

        // FUEL OILS - Core commodities in shipping/bunker market
        // Singapore MOPS products - ordered by specificity (most specific first)
        if (normalized.Contains("MOPS") && (normalized.Contains("MGO") || normalized.Contains("MARINE GAS OIL") || normalized.Contains("MARINE GASOIL")))
            return ("MOPS_MGO", "MOPS Marine Gas Oil FOB Singapore", "MT");
        if (normalized.Contains("MOPS") && (normalized.Contains("MARINE FUEL 0.5%") || normalized.Contains("0.5%") || normalized.Contains("VLSFO")))
            return ("MOPS_MARINE_05", "MOPS Marine Fuel 0.5% (VLSFO)", "MT");
        if (normalized.Contains("MOPS FO 180CST") && !normalized.Contains("PREMIUM") && !normalized.Contains("HIGH"))
            return ("MOPS_180", "MOPS FO 180cst FOB Singapore", "MT");
        if (normalized.Contains("MOPS FO 380CST") && !normalized.Contains("PREMIUM"))
            return ("MOPS_380", "MOPS FO 380cst FOB Singapore", "MT");

        // Rotterdam products - ordered by specificity (most specific first)
        if (normalized.Contains("RTDM") && (normalized.Contains("MGO") || normalized.Contains("MARINE GAS OIL") || normalized.Contains("MARINE GASOIL") || normalized.Contains("GASOIL 0.1%")))
            return ("MGO_RTDM", "Marine Gas Oil FOB Rotterdam", "MT");
        if (normalized.Contains("MARINE FUEL 0.5%") && normalized.Contains("FOB RTDM"))
            return ("MARINE_FUEL_05_RTDM", "Marine Fuel 0.5% FOB Rotterdam", "MT");
        if (normalized.Contains("FUEL OIL 3.5%") && normalized.Contains("FOB RTDM"))
            return ("FUEL_OIL_35_RTDM", "Fuel Oil 3.5% FOB Rotterdam", "MT");

        // Generic fuel oil products (fallback - less specific)
        if (normalized.Contains("FUEL OIL 3.5%"))
            return ("FUEL_OIL_35", "Fuel Oil 3.5%", "MT");
        if (normalized.Contains("MARINE FUEL 0.5%"))
            return ("MARINE_FUEL_05", "Marine Fuel 0.5% (VLSFO)", "MT");
        if (normalized.Contains("HSFO 380") || (normalized.Contains("HSFO") && normalized.Contains("380")))
            return ("HSFO_380", "High Sulfur Fuel Oil 380cst", "MT");
        if (normalized.Contains("LSFO") || normalized.Contains("LOW SULFUR"))
            return ("LSFO_180", "Low Sulfur Fuel Oil 180cst", "MT");

        // GASOIL / DISTILLATES - Major trading hub
        if (normalized.Contains("IPE GASOIL FUTURES"))
            return ("GASOIL_FUTURES", "IPE Gasoil Futures", "MT");
        if (normalized.Contains("IPE GASOIL 1ST LINE"))
            return ("GASOIL_1ST", "IPE Gasoil 1st Line", "MT");
        if (normalized.Contains("GAS OIL 10PPM"))
            return ("GO_10PPM", "Gas Oil 10ppm", "BBL");
        if (normalized.Contains("GAS OIL 50PPM"))
            return ("GO_50PPM", "Gas Oil 50ppm", "BBL");
        if (normalized.Contains("GAS OIL 500PPM"))
            return ("GO_500PPM", "Gas Oil 500ppm", "BBL");
        if (normalized.Contains("GASOIL") && !normalized.Contains("PREMIUM"))
            return ("GASOIL", "Gasoil", "MT");

        // JET / KEROSENE
        if (normalized.Contains("JET KERO") || normalized.Contains("JET KEROSENE"))
            return ("JET_KERO", "Jet Kerosene", "BBL");

        // NAPHTHA - Petrochemical feedstock
        if (normalized.Contains("NAPHTHA") && normalized.Contains("JAPAN"))
            return ("NAPHTHA_JAPAN", "Naphtha C+F Japan", "MT");
        if (normalized.Contains("NAPHTHA") && normalized.Contains("SING"))
            return ("NAPHTHA_SING", "Naphtha FOB Singapore", "BBL");
        if (normalized.Contains("NAPHTHA"))
            return ("NAPHTHA", "Naphtha", "MT");

        // GASOLINE - Refined product market
        if (normalized.Contains("GASOLINE 92"))
            return ("GASOLINE_92", "Gasoline 92 RON FOB Singapore", "BBL");
        if (normalized.Contains("GASOLINE 95"))
            return ("GASOLINE_95", "Gasoline 95 RON FOB Singapore", "BBL");
        if (normalized.Contains("GASOLINE 97"))
            return ("GASOLINE_97", "Gasoline 97 RON FOB Singapore", "BBL");

        // SPECIALTY PRODUCTS
        if (normalized.Contains("MTBE"))
            return ("MTBE", "MTBE FOB Singapore", "MT");
        if (normalized.Contains("MOPJ"))
            return ("MOPJ", "MOPS Japan", "MT");
        if (normalized.Contains("VISCO"))
            return ("MOPS_VISCO", "MOPS Viscosity", "MT");

        // BUNKER PRODUCTS - Delivered variants with product differentiation
        // Hong Kong market bunker products (ordered by specificity - most specific first)
        if (normalized.Contains("HK") && (normalized.Contains("MGO") || normalized.Contains("MARINE GAS OIL") || normalized.Contains("MARINE GASOIL")))
            return ("MGO_HK", "Marine Gas Oil Delivered Hong Kong", "MT");
        if (normalized.Contains("HK") && (normalized.Contains("VLSFO") || normalized.Contains("0.5") || normalized.Contains("LOW SULFUR") || normalized.Contains("LSFO")))
            return ("VLSFO_HK", "VLSFO 0.5% Delivered Hong Kong", "MT");
        if (normalized.Contains("HK") && (normalized.Contains("HSFO") || normalized.Contains("380") || normalized.Contains("HIGH SULFUR")))
            return ("HSFO_HK", "HSFO 380 Delivered Hong Kong", "MT");
        if (normalized.Contains("BUNKER") && normalized.Contains("HK"))
            return ("BUNKER_HK", "Bunker Fuel Delivered Hong Kong", "MT");

        // Singapore market bunker products
        if (normalized.Contains("SPORE") && (normalized.Contains("MGO") || normalized.Contains("MARINE GAS OIL")))
            return ("MGO_SPORE", "Marine Gas Oil Delivered Singapore", "MT");
        if (normalized.Contains("SPORE") && (normalized.Contains("VLSFO") || normalized.Contains("0.5")))
            return ("VLSFO_SPORE", "VLSFO 0.5% Delivered Singapore", "MT");
        if (normalized.Contains("BUNKER") && normalized.Contains("SPORE"))
            return ("BUNKER_SPORE", "Bunker Fuel Delivered Singapore", "MT");

        // MOP ARAB GULF VARIANTS - Middle East trading hub (ordered by specificity)
        if ((normalized.Contains("MOPAG") || normalized.Contains("MOP ARAB GULF") || normalized.Contains("ARAB GULF")) && (normalized.Contains("MGO") || normalized.Contains("MARINE GAS OIL") || normalized.Contains("MARINE GASOIL")))
            return ("MOPAG_MGO", "MOP Arab Gulf Marine Gas Oil", "MT");
        if ((normalized.Contains("MOPAG") || normalized.Contains("MOP ARAB GULF") || normalized.Contains("ARAB GULF")) && (normalized.Contains("VLSFO") || normalized.Contains("0.5%") || normalized.Contains("MARINE FUEL 0.5")))
            return ("MOPAG_VLSFO", "MOP Arab Gulf VLSFO 0.5%", "MT");
        if (normalized.Contains("MOP ARAB GULF 180CST") || normalized.Contains("MOPAG 180"))
            return ("MOPAG_180", "MOP Arab Gulf 180cst", "MT");
        if (normalized.Contains("MOP ARAB GULF 380CST") || normalized.Contains("MOPAG 380"))
            return ("MOPAG_380", "MOP Arab Gulf 380cst", "MT");
        if (normalized.Contains("MOPAG 2500PPM"))
            return ("MOPAG_2500", "MOP Arab Gulf 2500ppm", "MT");
        if (normalized.Contains("MOPAG"))
            return ("MOPAG", "MOP Arab Gulf", "MT");

        // SPREADS & ARBITRAGE PRODUCTS - Advanced trading strategies
        if (normalized.Contains("MOPS 10PPM") && normalized.Contains("MOPAG 2500PPM"))
            return ("SPREAD_GO_MOPAG", "Spread: MOPS 10ppm vs MOPAG 2500ppm", "MT");
        if (normalized.Contains("MOPAG 380") && normalized.Contains("MOPS 380"))
            return ("SPREAD_ARAB_GULF", "Spread: MOP Arab Gulf 380 vs MOPS 380", "MT");
        if (normalized.Contains("MOPAG 180") && normalized.Contains("MOPS 380"))
            return ("SPREAD_MOPAG_MOPS", "Spread: MOP Arab Gulf 180 vs MOPS 380", "MT");
        if (normalized.Contains("FO 180") && normalized.Contains("PREMIUM"))
            return ("PREMIUM_FO180", "FO 180 3.5% Premium", "MT");
        if (normalized.Contains("FO 380") && normalized.Contains("PREMIUM"))
            return ("PREMIUM_FO380", "FO 380 3.5% Premium", "MT");
        if (normalized.Contains("MARINE FUEL") && normalized.Contains("PREMIUM"))
            return ("PREMIUM_MF", "Marine Fuel 0.5% Premium", "MT");
        if (normalized.Contains("GASOIL 10PPM") && normalized.Contains("PREMIUM"))
            return ("PREMIUM_GO10", "Gasoil 10ppm Premium", "MT");
        if (normalized.Contains("MOPS/MOPJ"))
            return ("SPREAD_MOPS_MOPJ", "Spread: MOPS vs MOPS Japan", "MT");

        // Fallback: Try to extract first meaningful word as product code
        _logger.LogWarning("No explicit mapping found for header: {HeaderText}. Attempting generic mapping.", headerText);

        // Generic mapping for unrecognized products
        var genericCode = normalized
            .Split(new[] { ' ', '(', ')' }, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()?
            .Replace("/", "_")
            .Replace("-", "_");

        if (!string.IsNullOrEmpty(genericCode) && genericCode.Length > 3)
        {
            return (genericCode, headerText, null);
        }

        return null;
    }

    /// <summary>
    /// Parse price value from Excel cell, handling both decimal values and string representations
    /// </summary>
    private bool TryParsePrice(IXLCell cell, out decimal price)
    {
        price = 0;

        try
        {
            // First try direct decimal parsing
            if (cell.TryGetValue<decimal>(out price))
            {
                return true;
            }

            // Try string parsing
            var strValue = cell.GetString();
            if (decimal.TryParse(strValue, out price))
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Error parsing price from cell: {Error}", ex.Message);
        }

        return false;
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
                        // Use the contract month already extracted from header (e.g., "AUG25")
                        var marketPrice = MarketPrice.Create(
                            priceDate,
                            productCode,
                            productName,
                            MarketPriceType.FuturesSettlement,
                            price,
                            "USD",
                            contractHeader,
                            "ICE Settlement",
                            true,
                            DateTime.UtcNow,
                            uploadedBy,
                            contractMonth); // Pass contract month already extracted from header

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

        // Track dates and products for summary
        var processedDates = new HashSet<DateTime>();
        var processedProducts = new HashSet<string>();

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

                // Track the date
                processedDates.Add(priceDate.Date);

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
                        // Use clean product code (no underscore concatenation)
                        // ContractMonth is stored separately in the entity
                        var fullProductCode = productCode;

                        // Track the product (use unique key for tracking)
                        var trackingKey = string.IsNullOrEmpty(contractMonth) ? productCode : $"{productCode}_{contractMonth}";
                        processedProducts.Add(trackingKey);

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
                            var marketPrice = MarketPrice.Create(
                                priceDate,
                                productCode,  // Clean product code
                                GenerateProductName(productCode, contractMonth, priceType),
                                priceType,
                                price,
                                "USD",
                                headers[colIndex],
                                "CSV Upload",
                                false,
                                DateTime.UtcNow,
                                request.UploadedBy,
                                priceType == MarketPriceType.Spot ? null : contractMonth); // Pass contract month for futures

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

            // Set date range information
            if (processedDates.Count > 0)
            {
                result.EarliestDate = processedDates.Min();
                result.LatestDate = processedDates.Max();
                result.UniqueDatesImported = processedDates.Count;
            }

            result.UniqueProductsImported = processedProducts.Count;

            result.Messages.Add($"Processed {result.RecordsProcessed} price records from unified CSV format");
            result.Messages.Add($"Found {columnMappings.Count} product columns");
            result.Messages.Add($"Data covers {result.UniqueDatesImported} unique dates from {result.EarliestDate:yyyy-MM-dd} to {result.LatestDate:yyyy-MM-dd}");
            result.Messages.Add($"Imported {result.UniqueProductsImported} unique products");
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

        // Handle abbreviated product codes from test_data.csv (GO, SG, MF, etc.)
        // These are market-standard shorthand codes
        if (normalizedName.StartsWith("GO ") || normalizedName == "GO" || normalizedName.Contains("GO ") && int.TryParse(normalizedName.Substring(3, 2), out _))
        {
            // GO 10ppm, GO 50ppm, etc.
            if (normalizedName.Contains("10"))
                return "GO_10PPM";
            if (normalizedName.Contains("50"))
                return "GO_50PPM";
            return "GAS_OIL";
        }

        // Singapore 180/380 products
        if (normalizedName.StartsWith("SG") || (normalizedName.Contains("SG") && int.TryParse(normalizedName.Substring(2, 3), out _)))
        {
            if (normalizedName.Contains("180"))
                return "SG_180";
            if (normalizedName.Contains("380"))
                return "SG_380";
            return "SINGAPORE";
        }

        // Marine Fuel products
        if (normalizedName.StartsWith("MF") || normalizedName.Contains("MARINE FUEL"))
        {
            if (normalizedName.Contains("0.5"))
                return "MARINE_FUEL_05";
            return "MARINE_FUEL";
        }

        // MOPJ - MOPS Japan products
        if (normalizedName.Contains("MOPJ"))
        {
            if (normalizedName.Contains("TS"))
                return "MOPJ_TS";
            if (normalizedName.Contains("BRT"))
                return "MOPJ_BRT";
            return "MOPJ";
        }

        // 92/95/97 Gasoline products
        if (normalizedName.StartsWith("92") || normalizedName.Contains("92R"))
        {
            if (normalizedName.Contains("TS"))
                return "GASOLINE_92_TS";
            if (normalizedName.Contains("BRT"))
                return "GASOLINE_92_BRT";
            return "GASOLINE_92";
        }

        // Kero/GO spread
        if (normalizedName.Contains("KERO") || normalizedName.Contains("KERO/GO"))
            return "KERO_GO_SPREAD";

        // Viscosity products (Visco)
        if (normalizedName.StartsWith("VISCO") || normalizedName.Contains("VISCOSITY"))
            return "VISCOSITY";

        // GO/180 and GO/380 spreads
        if (normalizedName.Contains("GO/") && normalizedName.Contains("180"))
            return "GO_180_SPREAD";
        if (normalizedName.Contains("GO/") && normalizedName.Contains("380"))
            return "GO_380_SPREAD";

        // Singapore Hi5
        if (normalizedName.Contains("SING HI5") || normalizedName.Contains("SING") && normalizedName.Contains("HI5"))
            return "SING_HI5";

        // Brent Futures/Swaps
        if (normalizedName.Contains("BRT FUT") || normalizedName.Contains("BRENT FUT"))
            return "BRENT_FUTURES";
        if (normalizedName.Contains("BRT SWP") || normalizedName.Contains("BRENT SWP"))
            return "BRENT_SWAPS";

        // Brent spreads
        if (normalizedName.Contains("GO BRT"))
            return "GO_BRENT_SPREAD";
        if (normalizedName.Contains("380 BRT"))
            return "FUEL_OIL_380_BRENT_SPREAD";
        if (normalizedName.Contains("MF") && normalizedName.Contains("BRT"))
            return "MF_05_BRENT_SPREAD";

        // EFS products (Exchange for Swaps)
        if (normalizedName.Contains("EFS") || normalizedName.Contains("EXCHANGE FOR SWAP"))
        {
            if (normalizedName.Contains("10"))
                return "GO_10PPM_EFS";
            if (normalizedName.Contains("0.5"))
                return "MARINE_05_EFS";
            return "EFS";
        }

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

    /// <summary>
    /// Extracts contract month from product code (e.g., "BRT_FUT_202511" -> "NOV25")
    /// </summary>
    private string? ExtractContractMonthFromCode(string productCode)
    {
        if (string.IsNullOrEmpty(productCode))
            return null;

        // Try to extract YYYYMM format from product code using regex
        var match = Regex.Match(productCode, @"(\d{6})$");
        if (!match.Success)
            return null;

        var yyyymm = match.Groups[1].Value;

        // Parse YYYYMM format
        if (!int.TryParse(yyyymm.Substring(0, 4), out var year) ||
            !int.TryParse(yyyymm.Substring(4, 2), out var month) ||
            month < 1 || month > 12)
            return null;

        // Format to MMM-YY (e.g., "NOV25")
        var monthNames = new[] { "", "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC" };
        var yy = year % 100;
        return $"{monthNames[month]}{yy:D2}";
    }
}