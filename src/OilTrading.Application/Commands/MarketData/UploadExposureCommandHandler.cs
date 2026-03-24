using MediatR;
using ClosedXML.Excel;
using OilTrading.Application.DTOs;
using OilTrading.Application.Services;
using Microsoft.Extensions.Logging;

namespace OilTrading.Application.Commands.MarketData;

public class UploadExposureCommandHandler : IRequestHandler<UploadExposureCommand, ExposureVaRResultDto>
{
    private readonly IVaRCalculationService _varService;
    private readonly ILogger<UploadExposureCommandHandler> _logger;

    // Valid product codes for exposure table
    private static readonly HashSet<string> ValidProductCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "SG180", "SG380", "MF 0.5", "GO 10ppm", "Brt Fut"
    };

    // Valid units
    private static readonly HashSet<string> ValidUnits = new(StringComparer.OrdinalIgnoreCase)
    {
        "MT", "BBL", "BBLS"
    };

    // Expected Chinese column headers
    private const string HEADER_PRODUCT = "品种";
    private const string HEADER_CONTRACT_MONTH = "合约月份";
    private const string HEADER_SPOT_POSITION = "现货持仓";
    private const string HEADER_FUTURES_POSITION = "期货持仓";
    private const string HEADER_UNIT = "单位";

    public UploadExposureCommandHandler(
        IVaRCalculationService varService,
        ILogger<UploadExposureCommandHandler> logger)
    {
        _varService = varService;
        _logger = logger;
    }

    public async Task<ExposureVaRResultDto> Handle(
        UploadExposureCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing exposure table upload: {FileName}", request.FileName);

        // Step 1: Parse Excel file
        var (exposureRows, parseWarnings) = ParseExcelFile(request.FileContent, request.FileName);

        if (!exposureRows.Any())
        {
            throw new InvalidOperationException(
                "No valid exposure data found in the uploaded file. " +
                "Please ensure the file has the correct headers: 品种, 合约月份, 现货持仓, 期货持仓, 单位");
        }

        _logger.LogInformation("Parsed {Count} exposure rows from {FileName}", exposureRows.Count, request.FileName);

        // Step 2: Convert rows into individual positions (split spot and futures)
        var positions = ConvertToPositions(exposureRows);

        _logger.LogInformation("Converted to {Count} individual positions for VaR calculation", positions.Count);

        // Step 3: Calculate VaR and CVaR
        var result = await _varService.CalculateExposureVaRAsync(
            positions, exposureRows, lookbackDays: 252, cancellationToken);

        // Add any parse warnings to the result
        if (parseWarnings.Any())
        {
            result.Warnings.InsertRange(0, parseWarnings);
        }

        _logger.LogInformation(
            "Exposure VaR calculation complete. Positions: {Positions}, VaR95: {VaR95:F2}, CVaR95: {CVaR95:F2}",
            result.PositionsProcessed, result.PortfolioVar1Day95, result.PortfolioCVaR1Day95);

        return result;
    }

    private (List<ExposureRowDto> rows, List<string> warnings) ParseExcelFile(byte[] fileContent, string fileName)
    {
        var rows = new List<ExposureRowDto>();
        var warnings = new List<string>();

        using var stream = new MemoryStream(fileContent);
        using var workbook = new XLWorkbook(stream);

        var worksheet = workbook.Worksheets.First();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

        if (lastRow < 2)
        {
            throw new InvalidOperationException("The file appears to be empty or has no data rows.");
        }

        // Detect column indices from header row
        var headerRow = worksheet.Row(1);
        var columnMap = DetectColumns(headerRow);

        if (!columnMap.TryGetValue("Product", out var productCol))
        {
            throw new InvalidOperationException(
                $"Required column '{HEADER_PRODUCT}' not found in header row. " +
                $"Expected headers: {HEADER_PRODUCT}, {HEADER_CONTRACT_MONTH}, {HEADER_SPOT_POSITION}, {HEADER_FUTURES_POSITION}, {HEADER_UNIT}");
        }

        columnMap.TryGetValue("ContractMonth", out var contractMonthCol);
        columnMap.TryGetValue("SpotPosition", out var spotPositionCol);
        columnMap.TryGetValue("FuturesPosition", out var futuresPositionCol);
        columnMap.TryGetValue("Unit", out var unitCol);

        // Parse data rows
        for (int rowNum = 2; rowNum <= lastRow; rowNum++)
        {
            var row = worksheet.Row(rowNum);

            // Skip empty rows
            var productCell = row.Cell(productCol).GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(productCell))
                continue;

            // Validate product code
            if (!ValidProductCodes.Contains(productCell))
            {
                warnings.Add($"Row {rowNum}: unknown product '{productCell}'. Valid products: {string.Join(", ", ValidProductCodes)}. Skipped.");
                continue;
            }

            // Parse contract month
            string? contractMonth = null;
            if (contractMonthCol > 0)
            {
                contractMonth = row.Cell(contractMonthCol).GetString()?.Trim();
                if (string.IsNullOrWhiteSpace(contractMonth))
                    contractMonth = null;
            }

            // Parse spot position
            decimal spotPosition = 0;
            if (spotPositionCol > 0)
            {
                spotPosition = ParseDecimalCell(row.Cell(spotPositionCol));
            }

            // Parse futures position
            decimal futuresPosition = 0;
            if (futuresPositionCol > 0)
            {
                futuresPosition = ParseDecimalCell(row.Cell(futuresPositionCol));
            }

            // Validate at least one position is non-zero
            if (spotPosition == 0 && futuresPosition == 0)
            {
                warnings.Add($"Row {rowNum}: both spot and futures positions are zero for '{productCell}'. Skipped.");
                continue;
            }

            // Parse unit
            string unit = "MT";
            if (unitCol > 0)
            {
                var unitValue = row.Cell(unitCol).GetString()?.Trim();
                if (!string.IsNullOrWhiteSpace(unitValue))
                {
                    if (!ValidUnits.Contains(unitValue))
                    {
                        warnings.Add($"Row {rowNum}: invalid unit '{unitValue}'. Using default 'MT'.");
                    }
                    else
                    {
                        unit = unitValue.ToUpperInvariant() == "BBLS" ? "BBL" : unitValue.ToUpperInvariant();
                    }
                }
            }

            // Validate: futures position requires contract month
            if (futuresPosition != 0 && contractMonth == null)
            {
                warnings.Add($"Row {rowNum}: futures position for '{productCell}' has no contract month. Using as generic futures.");
            }

            rows.Add(new ExposureRowDto
            {
                ProductCode = productCell,
                ContractMonth = contractMonth,
                SpotPosition = spotPosition,
                FuturesPosition = futuresPosition,
                Unit = unit,
            });
        }

        return (rows, warnings);
    }

    private Dictionary<string, int> DetectColumns(IXLRow headerRow)
    {
        var map = new Dictionary<string, int>();
        var lastCol = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;

        for (int col = 1; col <= lastCol; col++)
        {
            var header = headerRow.Cell(col).GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(header))
                continue;

            if (header.Contains(HEADER_PRODUCT) || header.Equals("Product", StringComparison.OrdinalIgnoreCase) || header.Equals("ProductCode", StringComparison.OrdinalIgnoreCase))
                map["Product"] = col;
            else if (header.Contains(HEADER_CONTRACT_MONTH) || header.Equals("ContractMonth", StringComparison.OrdinalIgnoreCase))
                map["ContractMonth"] = col;
            else if (header.Contains(HEADER_SPOT_POSITION) || header.Equals("SpotPosition", StringComparison.OrdinalIgnoreCase))
                map["SpotPosition"] = col;
            else if (header.Contains(HEADER_FUTURES_POSITION) || header.Equals("FuturesPosition", StringComparison.OrdinalIgnoreCase))
                map["FuturesPosition"] = col;
            else if (header.Contains(HEADER_UNIT) || header.Equals("Unit", StringComparison.OrdinalIgnoreCase))
                map["Unit"] = col;
        }

        return map;
    }

    private static List<ExposurePositionDto> ConvertToPositions(List<ExposureRowDto> rows)
    {
        var positions = new List<ExposurePositionDto>();

        foreach (var row in rows)
        {
            // Add spot position if non-zero
            if (row.SpotPosition != 0)
            {
                positions.Add(new ExposurePositionDto
                {
                    ProductCode = row.ProductCode,
                    ContractMonth = null, // Spot has no contract month
                    Quantity = row.SpotPosition,
                    PositionType = "Spot",
                    Unit = row.Unit,
                });
            }

            // Add futures position if non-zero
            if (row.FuturesPosition != 0)
            {
                positions.Add(new ExposurePositionDto
                {
                    ProductCode = row.ProductCode,
                    ContractMonth = row.ContractMonth,
                    Quantity = row.FuturesPosition,
                    PositionType = "Futures",
                    Unit = row.Unit,
                });
            }
        }

        return positions;
    }

    private static decimal ParseDecimalCell(IXLCell cell)
    {
        if (cell.IsEmpty())
            return 0;

        // Try direct numeric value first
        if (cell.DataType == XLDataType.Number)
            return (decimal)cell.GetDouble();

        // Try parsing string
        var text = cell.GetString()?.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        // Remove commas and whitespace
        text = text.Replace(",", "").Replace(" ", "");

        if (decimal.TryParse(text, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var value))
            return value;

        return 0;
    }
}
